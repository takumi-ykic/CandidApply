using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CandidApply.Models;
using CandidApply.Data;
using Microsoft.AspNetCore.Identity;
using Application = CandidApply.Models.Application;
using Azure.Storage.Blobs;

namespace CandidApply.Controllers
{
    public class ApplicationsController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ApplicationsController> _logger;
        private readonly IConfiguration _configration;
        private readonly string _path;
        private readonly string _container;

        public ApplicationsController(ApplicationContext context, UserManager<User> userManager, ILogger<ApplicationsController> logger, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _configration = configuration;
            _path = _configration.GetValue<string>("AzureSettings:filestorage");
            _container = _configration.GetValue<string>("AzureSettings:applicationcontainer");
            
        }

        public async Task<IActionResult> Index(string? searchKeyword, string? selSearchStatus, string? sortDate, string? currentFileter, int? pageNum)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Redirect("/Identity/Account/Login");
            }

            var varUserId = await _userManager.GetUserIdAsync(user);
            string? userId = varUserId.ToString();

            var applicationList = _context.applications
                    .Include(a => a.ApplicationStatus)
                    .Include(a => a.ApplicationFile)
                    .Include(a => a.Interview)
                    .Where(a => a.userId != null && a.userId.Contains(userId));

            if (!applicationList.Any())
            {
                // For a new user
                ViewData["NoApplications"] = "Let's start adding your application!";
                return View();
            }

            applicationList = applicationList
                                .Where(a => a.status != 5)
                                .Where(a => a.deleteFlag == 0);

            if (searchKeyword != null)
            {
                pageNum = 1;
            }else
            {
                searchKeyword = currentFileter;
            }
            ViewData["SearchKeyword"] = searchKeyword;
            ViewData["CurrentPage"] = pageNum;

            int selStatus = 0;
            if(selSearchStatus != null)
            {
                selStatus = int.Parse(selSearchStatus);
            }
            ViewData["SelSearchStatus"] = selSearchStatus;
            var statusTable = await _context.status.ToListAsync();
            var searchStatusList = new List<SelectListItem>();
            foreach (ApplicationStatus status in statusTable)
            {
                searchStatusList.Add(new SelectListItem
                {
                    Value = status.statusId.ToString(),
                    Text = status.statusName,
                    Selected = status.statusId == selStatus
                });
            }
            ViewData["SearchStatusList"] = searchStatusList;

            if (!String.IsNullOrEmpty(searchKeyword))
            {
                applicationList = applicationList
                        .Where(a => (a.jobTitle != null && a.jobTitle.Contains(searchKeyword))
                        || (a.company != null && a.company.Contains(searchKeyword))
                        || (a.Interview != null && a.Interview.memo != null && a.Interview.memo.Contains(searchKeyword)));
            }

            if (selStatus != 0)
            {
                applicationList = applicationList.Where(a => a.status == selStatus);
            }

            ViewData["CurrentSort"] = sortDate;
            ViewData["SortDate"] = sortDate == "Date" ? "date_asc" : "Date";
            switch (sortDate)
            {
                case "Date":
                    applicationList = applicationList.OrderByDescending(a => a.applicationDate);
                    break;
                case "date_asc":
                    applicationList = applicationList.OrderBy(a => a.applicationDate);
                    break;
                default:
                    applicationList = applicationList.OrderBy(a => a.applicationDate);
                    break;
            }

            var listedApplications = await applicationList.ToListAsync();

            if (listedApplications.Any())
            {
                // Get collection for status
                Dictionary<int, SelectList> statusDictionary = new Dictionary<int, SelectList>();
                foreach (var appplication in listedApplications)
                {
                    int selectedStatusId = appplication.status;
                    var statusList = PopulateddlApplyStatus(selectedStatusId);
                    statusDictionary.Add(appplication.applicationId, statusList);
                }
                ViewData["StatusDictionary"] = statusDictionary;

                int pageSize = 10;
                var paginatedApplicationList = await PaginatedList<Application>.CreateAsync(applicationList, pageNum ?? 1, pageSize);
                return View(paginatedApplicationList);
            }
            else
            {
                ViewData["NoApplications"] = "Not found any application.";
                return View();
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("jobTitle,company,applicationDate")] Application application,
                                                IFormFile upResume, IFormFile upCoverLetter)
        {
            var user = await _userManager.GetUserAsync(User);
            ApplicationFile? applicationFile = new ApplicationFile();
            Interview? interview = new Interview();

            if (user == null)
            {
                return Redirect("/Identity/Account/Login");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            application.userId = userId.ToString();

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Add(application);
                    await _context.SaveChangesAsync();

                    applicationFile.applicationId = application.applicationId;
                    interview.applicationId = application.applicationId;
                    _context.files.Add(applicationFile);
                    _context.interviews.Add(interview);
                    await _context.SaveChangesAsync();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Error. Please try again");
                    _logger.LogError(ex, "Error happened at inserting new ApplyFile data");
                    transaction.Rollback();

                    return View(application);
                }
            }

            //File
            if (upResume != null && upResume.Length != 0)
            {
                bool flag = await UploadResume(applicationFile.applicationId, upResume);
                if (!flag)
                {
                    ModelState.AddModelError(string.Empty, "Error. Uploading resume is failure.");
                    return View(application);
                }
            }

            if (upCoverLetter != null && upCoverLetter.Length != 0)
            {
                bool flag = await UploadCoverLetter(applicationFile.applicationId, upCoverLetter);
                if (!flag)
                {
                    ModelState.AddModelError(string.Empty, "Error. Uploading cover letter is failure.");
                    return View(application);
                }
            }

            return RedirectToAction(nameof(Index), new { searchKeyword = "" });
        }

        public async Task<IActionResult> Details(int? applicationId, string? selSearchStatus, string? sortDate, string? currentFileter, int? pageNum)
        {
            if (applicationId == null)
            {
                return NotFound();
            }

            ViewData["SearchKeyword"] = currentFileter;
            ViewData["SelSearchStatus"] = selSearchStatus;
            ViewData["CurrentSort"] = sortDate;
            ViewData["CurrentPage"] = pageNum;

            var application = await _context.applications
                            .Include(j => j.ApplicationStatus)
                            .Include(j => j.ApplicationFile)
                            .Include(j => j.Interview)
                            .FirstOrDefaultAsync(j => j.applicationId == applicationId);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        public async Task<IActionResult> Edit(int? applicationId, string? selSearchStatus, string? sortDate, string? currentFileter, int? pageNum)
        {
            if (applicationId == null)
            {
                return NotFound();
            }

            ViewData["SearchKeyword"] = currentFileter;
            ViewData["SelSearchStatus"] = selSearchStatus;
            ViewData["CurrentSort"] = sortDate;
            ViewData["CurrentPage"] = pageNum;

            var application = await _context.applications
                            .Include(j => j.ApplicationStatus)
                            .Include(j => j.ApplicationFile)
                            .Include(j => j.Interview)
                            .FirstOrDefaultAsync(j => j.applicationId == applicationId);

            if (application == null)
            {
                return NotFound();
            }
            else
            {
                Dictionary<int, SelectList> statusDictionary = new Dictionary<int, SelectList>();
                int selectedStatusId = application.status;
                var statusList = PopulateddlApplyStatus(selectedStatusId);
                statusDictionary.Add(application.applicationId, statusList);

                ViewData["StatusDictionary"] = statusDictionary;
            }
            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int applicationId, int status, [Bind("applicationId,jobTitle,company,applicationDate")] Application application,
                                                IFormFile upResume, IFormFile upCoverLetter,
                                                DateTime? interviewDate, string? location, string? memo)
        {
            if (applicationId != application.applicationId)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            var userId = await _userManager.GetUserIdAsync(user);
            application.userId = userId;

            application.status = status;

            var interviewInfo = await _context.interviews
                                .Where(i => i.applicationId == application.applicationId)
                                .FirstOrDefaultAsync();
            if (interviewInfo != null)
            {
                if (interviewDate != null)
                {
                    interviewInfo.interviewDate = interviewDate;
                }

                if (location != null)
                {
                    interviewInfo.location = location;
                }

                if (memo != null)
                {
                    interviewInfo.memo = memo;
                }
            }

            try
            {
                _context.Update(application);
                if (upResume != null && upResume.Length != 0)
                {
                    bool flag = await UploadResume(application.applicationId, upResume);
                    if (!flag)
                    {
                        ModelState.AddModelError(string.Empty, "Error. Uploading resume is failure.");
                        return View(application);
                    }
                }

                if (upCoverLetter != null && upCoverLetter.Length != 0)
                {
                    bool flag = await UploadCoverLetter(application.applicationId, upCoverLetter);
                    if (!flag)
                    {
                        ModelState.AddModelError(string.Empty, "Error. Uploading resume is failure.");
                        return View(application);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApplicationExists(application.applicationId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Details), new { applicationId = application.applicationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int applicationId)
        {
            if (applicationId == 0)
            {
                return BadRequest("No ID provided.");
            }

            var application = await _context.applications
                                    .FirstOrDefaultAsync(a => a.applicationId == applicationId);

            if (application != null)
            {
                application.deleteFlag = 1;
                _context.Update(application);
                await _context.SaveChangesAsync();
            }
            else
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Status(Dictionary<int, int> selStatus, string? selSearchStatus, string? sortDate, string? currentFileter, int? pageNum)
        {
            ViewData["SearchKeyword"] = currentFileter;
            ViewData["SelSearchStatus"] = selSearchStatus;
            ViewData["CurrentSort"] = sortDate;
            ViewData["CurrentPage"] = pageNum;

            if (selStatus != null)
            {
                foreach (var applicationId in selStatus.Keys)
                {
                    var application = await _context.applications
                                   .FirstOrDefaultAsync(j => j.applicationId == applicationId);
                    if (application != null)
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            try
                            {
                                application.status = selStatus[applicationId];

                                if (selStatus[applicationId] != 1)
                                {
                                    var interviewInfo = await _context.interviews
                                                        .FirstOrDefaultAsync(i => i.applicationId == applicationId);
                                    if (interviewInfo == null)
                                    {
                                        Interview newInterview = new Interview();
                                        newInterview.applicationId = application.applicationId;
                                        _context.interviews.Add(newInterview);
                                    }
                                }
                                await _context.SaveChangesAsync();
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error happened at updateing status");
                                transaction.Rollback();

                                return View();
                            }
                        }
                    }
                }
            }
            return RedirectToAction(nameof(Index), new { currentFileter = currentFileter, selSearchStatus = selSearchStatus, sortDate = sortDate, pageNum = pageNum });
        }

        private bool ApplicationExists(int id)
        {
            return _context.applications.Any(e => e.applicationId == id);
        }

        private SelectList PopulateddlApplyStatus(int selectedStatus)
        {
            var statusList = _context.status
                .Select(s => new SelectListItem
                {
                    Value = s.statusId.ToString(),
                    Text = s.statusName
                })
                .ToList();

            SelectList selectList = new SelectList(statusList, "Value", "Text", selectedStatus);

            return selectList;
        }

        public async Task<bool> UploadResume(int id, IFormFile uploadResume)
        {
            var applicationFile = await _context.files
                            .Where(a => a.applicationId == id)
                            .FirstOrDefaultAsync();

            if (applicationFile != null)
            {
                try
                {
                    BlobServiceClient blobServiceClient = new BlobServiceClient(_path);

                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);

                    if (!await containerClient.ExistsAsync())
                    {
                        await containerClient.CreateIfNotExistsAsync();
                    }

                    string blobName = id.ToString() + "_resume.pdf";
                    BlobClient blobClient = containerClient.GetBlobClient(blobName);
                    if (await blobClient.ExistsAsync())
                    {
                        await blobClient.DeleteAsync();
                    }

                    await containerClient.UploadBlobAsync(blobName, uploadResume.OpenReadStream());

                    applicationFile.resume = blobName;
                    _context.files.Update(applicationFile);
                    await _context.SaveChangesAsync();

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during file upload or database update.");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> UploadCoverLetter(int id, IFormFile uploadCoverLetter)
        {
            var applicationFile = await _context.files
                            .Where(a => a.applicationId == id)
                            .FirstOrDefaultAsync();

            if (applicationFile != null)
            {
                try
                {
                    BlobServiceClient blobServiceClient = new BlobServiceClient(_path);

                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);

                    if (!await containerClient.ExistsAsync())
                    {
                        await containerClient.CreateIfNotExistsAsync();
                    }

                    string blobName = id.ToString() + "_coverletter.pdf";
                    BlobClient blobClient = containerClient.GetBlobClient(blobName);
                    if (await blobClient.ExistsAsync())
                    {
                        await blobClient.DeleteAsync();
                    }

                    await containerClient.UploadBlobAsync(blobName, uploadCoverLetter.OpenReadStream());

                    applicationFile.coverLetter = blobName;
                    _context.files.Update(applicationFile);
                    await _context.SaveChangesAsync();

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during file upload or database update.");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task<IActionResult> Download(string fileName)
        {
            if (fileName == null)
            {
                return RedirectToAction(nameof(Index));
            }

            BlobServiceClient blobServiceClient = new BlobServiceClient(_path);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            var response = await blobClient.OpenReadAsync();

            using (var memoryStream = new MemoryStream())
            {
                await response.CopyToAsync(memoryStream);
                var contentType = "application/octet-stream";

                return File(memoryStream.ToArray(), contentType, fileName);
            }
        }
    }
}
