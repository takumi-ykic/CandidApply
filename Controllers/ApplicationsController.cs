using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CandidApply.Models;
using CandidApply.Data;
using Microsoft.AspNetCore.Identity;
using Application = CandidApply.Models.Application;
using CandidApply.Helpers;
using CandidApply.CommonMethods;
using Microsoft.IdentityModel.Tokens;

namespace CandidApply.Controllers
{
    /// <summary>
    /// Application Controller class.
    /// </summary>
    public class ApplicationsController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ApplicationsController> _logger;
        private readonly IConfiguration _configration;
        private readonly string _path;
        private readonly string _container;
        private readonly IIdGenerator _idGenerator;

        public ApplicationsController(ApplicationContext context,
                                        UserManager<User> userManager, 
                                        ILogger<ApplicationsController> logger, 
                                        IConfiguration configuration,
                                        IIdGenerator idGenerator)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _configration = configuration;
            _path = _configration.GetValue<string>("AzureSettings:filestorage");
            _container = _configration.GetValue<string>("AzureSettings:applicationcontainer");
            _idGenerator = idGenerator;
        }

        /// <summary>
        /// Application main page. This displays the application list.
        /// </summary>
        /// <param name="filterKeyword"> A new search keyword as string
        /// <param name="filterStatus"> Selected status for search
        /// <param name="ordering"> The current condition of sorting application Date
        /// <param name="currentKeyword"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> The application list page
        public async Task<IActionResult> Index(string? filterKeyword, string? filterStatus, string? ordering, string? currentKeyword, int? pageNum)
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);

            // Check if user is null
            if (user == null)
            {
                // If user is not logined, move to login page
                return Redirect("/Identity/Account/Login");
            }

            // Get user id
            var varUserId = await _userManager.GetUserIdAsync(user);
            string? userId = varUserId.ToString();

            // Get applications based on user id with using Entity Framework, and remove 5(Rejected) and delete flag
            var applicationList = _context.applications
                    .Include(a => a.ApplicationStatus)
                    .Include(a => a.ApplicationFile)
                    .Include(a => a.Interview)
                    .Where(a => a.userId != null && a.userId.Contains(userId))
                    .Where(a => a.status != 5)
                    .Where(a => a.deleteFlag == 0);

            // Check if any application in the list
            if (!applicationList.Any())
            {
                // Message for a new user
                ViewData["NoApplications"] = "Let's start adding your application!";
                // Return to the list page with message
                return View();
            }

            // Check if search keyword is null
            if (filterKeyword != null)
            {
                // Page number is initialized
                pageNum = 1;
            }
            else
            {
                // Keep current using search keyword
                filterKeyword = currentKeyword;
            }
            // Set ViewData to keep condition
            ViewData["FilterKeyword"] = filterKeyword;
            ViewData["CurrentPage"] = pageNum;

            // Check if search status is selected
            int selStatus = filterStatus != null ? int.Parse(filterStatus) : 0;

            // Set ViewData to keep condition
            ViewData["FilterStatus"] = filterStatus;
            // Create status list
            var statusTable = await _context.status.ToListAsync();
            // Set status information to ApplicationStatus ViewModel
            var searchStatusList = FilterHelper.GetStatusList(statusTable, selStatus);
            // Set ViewData to keep condition
            ViewData["StatusList"] = searchStatusList;

            // Filter application list with keyword and status
            applicationList = FilterHelper.FilterApplication(applicationList, filterKeyword, selStatus);
            
            // Set ViewData to keep sorting condition
            ViewData["CurrentOrder"] = ordering;
            // Set new sorting condition to ViewData
            ViewData["Ordering"] = ordering == "desc" ? "asc" : "desc";
            // Change sorting
            switch (ViewData["Ordering"])
            {
                // Asc
                case "asc":
                    applicationList = applicationList.OrderBy(a => a.applicationDate);
                    break;
                // Desc
                default:
                    applicationList = applicationList.OrderByDescending(a => a.applicationDate);
                    break;
            }

            // Conver to list
            var listedApplications = await applicationList.ToListAsync();

            // Check if any applications are existed in the list
            if (listedApplications.Any())
            {
                // Get and Set Collection of status
                var allStatus = _context.status;
                ViewData["StatusDictionary"] = StatusHelper.GenerateStatusDictionary(allStatus, listedApplications);

                // Initialize page size
                int pageSize = 10;
                // Separate list basedd on page size
                var paginatedApplicationList = await PaginatedList<Application>.CreateAsync(applicationList, pageNum ?? 1, pageSize);
                // Return to the application list page
                return View(paginatedApplicationList);
            }
            else
            {
                // Add Not found application message
                ViewData["NoApplications"] = "Not found any application.";
                // Return to the application list page with message
                return View();
            }
        }

        /// <summary>
        /// Get method, initialize create page
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Post method, create a new application
        /// </summary>
        /// <param name="application"> Binding jobtitle, company, applicationDate from user input
        /// <param name="upResume"> Resume file as IFormFile
        /// <param name="upCoverLetter"> Cover letter file as IFormFile
        /// <return> The application list page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("jobTitle,company,applicationDate")] Application application,
                                                IFormFile upResume, IFormFile upCoverLetter)
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);
            // Create new ApplicationFile View Model instance
            ApplicationFile? applicationFile = new ApplicationFile();
            // Create new Interview instance
            Interview? interview = new Interview();

            // Check if user is null
            if (user == null)
            {
                // Redirect to login page
                return Redirect("/Identity/Account/Login");
            }

            // Get userid
            var userId = await _userManager.GetUserIdAsync(user);
            application.userId = userId.ToString();

            // Get applicationId
            var allApplicationIds = _context.applications.Select(a => a.applicationId).ToList();
            string applicationId = _idGenerator.GetApplicationId(allApplicationIds);
            application.applicationId = applicationId;

            // Start transactions to insert new row for application, applicationFile, and interview
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Insert a new information for application
                    _context.Add(application);
                    await _context.SaveChangesAsync();

                    // Set userid for both applicationfile and interview
                    applicationFile.applicationId = applicationId;
                    interview.applicationId = applicationId;
                    // Insert a new row with only userid for applicationFile
                    _context.files.Add(applicationFile);
                    // Insert a new row with only userid for interview
                    _context.interviews.Add(interview);
                    await _context.SaveChangesAsync();

                    // Upload files
                    // Check if resume file is null
                    bool isUpdate = false;
                    if (upResume != null && upResume.Length != 0)
                    {
                        // Upload resume file to Azure Blob
                        string fileName = applicationFile.applicationId + "_resume.pdf";
                        bool flag = await FileHelper.UploadFileAsync(fileName, upResume, _path, _container);

                        // Check if uploading is success
                        if (flag)
                        {
                            // Save file information
                            applicationFile.resume = fileName;
                            isUpdate = true;
                        }
                        else
                        {
                            // Failure, add error message
                            ModelState.AddModelError(string.Empty, "Error. Uploading resume is failure.");
                            // Return to create page with error message
                            return View(application);
                        }
                    }

                    // Check if cover letter file is null
                    if (upCoverLetter != null && upCoverLetter.Length != 0)
                    {
                        // Upload cover leter file to Azure Blob
                        string fileName = applicationFile.applicationId + "_coverletter.pdf";
                        bool flag = await FileHelper.UploadFileAsync(fileName, upCoverLetter, _path, _container);

                        // Check if uploading is success
                        if (flag)
                        {
                            // Save file information
                            applicationFile.coverLetter = fileName;
                            isUpdate = true;
                        }
                        else
                        {
                            // Failure, add error message
                            ModelState.AddModelError(string.Empty, "Error. Uploading cover letter is failure.");
                            // Return to create page with error message
                            return View(application);
                        }
                    }

                    // Update ApplicationFile table
                    if (isUpdate)
                    {
                        _context.files.Update(applicationFile);
                        await _context.SaveChangesAsync();
                    }

                    // Commit transaction
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // Add error messages
                    ModelState.AddModelError(string.Empty, "Error. Please try again");
                    // Add log
                    _logger.LogError(ex, "Error happened at inserting new ApplyFile data");
                    // Rollback transaction
                    transaction.Rollback();

                    // Return to create page
                    return View(application);
                }
            }

            // Every process is success, redirect to the application list page
            return RedirectToAction(nameof(Index), new { filterKeyword = "" });
        }

        /// <summary>
        /// Get method, get application detail information
        /// </summary>
        /// <param name="applicationId"> applicationId to get its detail
        /// <param name="filterStatus"> Selected status for search
        /// <param name="ordering"> The current condition of sorting application Date
        /// <param name="currentKeyword"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> Application View Model to display detail
        public async Task<IActionResult> Details(string? applicationId, string? filterStatus, string? ordering, string? currentKeyword, int? pageNum)
        {
            // Check if application id is null
            if (applicationId == null)
            {
                // Return to not found page
                return NotFound();
            }

            // Set ViewData to keep search and sort condition
            ViewData["FilterKeyword"] = currentKeyword;
            ViewData["FilterStatus"] = filterStatus;
            ViewData["CurrentOrder"] = ordering;
            ViewData["CurrentPage"] = pageNum;

            // Get application detail information
            var application = await _context.applications
                            .Include(j => j.ApplicationStatus)
                            .Include(j => j.ApplicationFile)
                            .Include(j => j.Interview)
                            .FirstOrDefaultAsync(j => j.applicationId == applicationId);

            // Check if application is null
            if (application == null)
            {
                // Return to not found page
                return NotFound();
            }
            // Move to detail page with full Application View Model
            return View(application);
        }

        /// <summary>
        /// Get method, get application for edit
        /// </summary>
        /// <param name="applicationId"> applicationId to get its detail
        /// <param name="filterStatus"> Selected status for search
        /// <param name="ordering"> The current condition of sorting application Date
        /// <param name="currentKeyword"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> Move to edit page with Application View Model
        public async Task<IActionResult> Edit(string? applicationId, string? filterStatus, string? ordering, string? currentKeyword, int? pageNum)
        {
            // Check if application id is null
            if (applicationId == null)
            {
                // Move to not found page
                return NotFound();
            }

            // Set ViewData to keep current search and sort condition
            ViewData["FilterKeyword"] = currentKeyword;
            ViewData["FilterStatus"] = filterStatus;
            ViewData["CurrentOrder"] = ordering;
            ViewData["CurrentPage"] = pageNum;

            // Get application information with Linq query
            var application = await _context.applications
                            .Include(j => j.ApplicationStatus)
                            .Include(j => j.ApplicationFile)
                            .Include(j => j.Interview)
                            .FirstOrDefaultAsync(j => j.applicationId == applicationId);

            // Check if application is null
            if (application == null)
            {
                // Move to not found page
                return NotFound();
            }
            else
            {
                // Set Collection of status
                var allStatus = _context.status;
                ViewData["StatusDictionary"] = StatusHelper.GenerateStatusDictionary(allStatus, application);
            }

            // Move to eidt page with application information
            return View(application);
        }

        /// <summary>
        /// Post method, update application information
        /// </summary>
        /// <param name="applicationId"> applicationId to get its detail
        /// <param name="status"> selected status for application
        /// <param name="application"> Application View Model with binding applicationId, jobTitle, company, and appluicationDate
        /// <param name="upResume"> Resume file as IFormFile
        /// <param name="upCoverLetter"> Cover letter file as IFormFile
        /// <param name="interviewDate"> Interview date
        /// <param name="location"> Interview location
        /// <param name="memo"> Memo about interview
        /// <return> Redirect to detail page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string applicationId, int status, [Bind("applicationId,jobTitle,company,applicationDate")] Application application,
                                                IFormFile upResume, IFormFile upCoverLetter,
                                                DateTime? interviewDate, string? location, string? memo)
        {
            // Check if application id is null
            if (applicationId != application.applicationId)
            {
                // Move to not found page
                return NotFound();
            }

            // Set ViewData to keep current search and sort condition
            var currentKeyword = Request.Form["CurrentKeyword"];
            var filterStatus = Request.Form["FilterStatus"];
            var ordering = Request.Form["CurrentOrder"];
            var pageNum = Request.Form["CurrentPage"];

            // Get login user information
            var user = await _userManager.GetUserAsync(User);
            // Check if user is null
            if (user == null)
            {
                // Move to not found page
                return NotFound();
            }
            // Get userid
            var userId = await _userManager.GetUserIdAsync(user);
            application.userId = userId;

            // Set application status
            application.status = status;

            // Get applicaation file information, if exists
            var applicationFile = await _context.files
                                  .Where(i => i.applicationId == application.applicationId)
                                  .FirstOrDefaultAsync();

            // Get interview information for this application, if existed
            var interviewInfo = await _context.interviews
                                .Where(i => i.applicationId == application.applicationId)
                                .FirstOrDefaultAsync();

            // Check if interview information is null
            if (interviewInfo != null)
            {
                // Check if interview date is null
                if (interviewDate != null)
                {
                    // Set iterview date to column
                    interviewInfo.interviewDate = interviewDate;
                }

                // Check if interview location is null
                if (location != null)
                {
                    // Set interview location 
                    interviewInfo.location = location;
                }

                // Check if memo is null
                if (memo != null)
                {
                    // Set interview memo
                    interviewInfo.memo = memo;
                }
            }

            try
            {
                // Update target application information
                _context.Update(application);
                bool isUpdate = false;

                // Check if resume file exists
                if (upResume != null && upResume.Length != 0 && applicationFile != null)
                {
                    // Upload resume file to Azure Blob with returning boolean value
                    string fileName = applicationId + "_resume.pdf";
                    bool flag = await FileHelper.UploadFileAsync(fileName, upResume, _path, _container);
                    // Check if upload is success
                    if (flag)
                    {
                        // Save file information
                        applicationFile.resume = fileName;
                        isUpdate = true;
                    }
                    else
                    {
                        // Failure, add error message
                        ModelState.AddModelError(string.Empty, "Error. Uploading resume is failure.");
                        // Return to edit page with error message
                        return View(application);
                    }
                }

                // Check if cover letter file exsits
                if (upCoverLetter != null && upCoverLetter.Length != 0 && applicationFile != null)
                {
                    // Upload cover letter file to Azure Blob with returning boolean value
                    string fileName = applicationId + "_coverletter.pdf";
                    bool flag = await FileHelper.UploadFileAsync(fileName, upCoverLetter, _path, _container);
                    // Check if upload is success
                    if (flag)
                    {
                        // Save file information
                        applicationFile.coverLetter = fileName;
                        isUpdate = true;
                    }
                    else
                    {
                        // Failure, add error message
                        ModelState.AddModelError(string.Empty, "Error. Uploading resume is failure.");
                        // Return to edit page with error message
                        return View(application);
                    }
                }

                // Update ApplicationFile table
                if (isUpdate)
                {
                    _context.files.Update(applicationFile);
                    await _context.SaveChangesAsync();
                }

                // Every process is completed without error, then save them
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Catch exception about Updating db
                if (!ApplicationExists(application.applicationId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Redirect to detail page
            return RedirectToAction(nameof(Details), new { applicationId = application.applicationId, filterStatus = filterStatus, ordering = ordering, currentKeyword = currentKeyword, pageNum = pageNum });
        }

        /// <summary>
        /// Post method, delete application logically
        /// </summary>
        /// <param name="applicationId"> Target application id
        /// <return> Redirect to application list page(index)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string applicationId)
        {
            // Check if applicationId exists
            if (applicationId.IsNullOrEmpty())
            {
                // Display error message
                return BadRequest("No ID provided.");
            }

            // Get target application by application id
            var application = await _context.applications
                                    .FirstOrDefaultAsync(a => a.applicationId == applicationId);

            // Check if application exists
            if (application != null)
            {
                // Update deleteFlag and save
                application.deleteFlag = 1;
                _context.Update(application);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Move to not found page
                return NotFound();
            }

            // Redirect to the application list page
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Update application status when selection is changed
        /// </summary>
        /// <param name="selStatus"> Dictionary for status name and status value
        /// <param name="filterStatus"> Selected status for search
        /// <param name="ordering"> The current condition of sorting application Date
        /// <param name="currentKeyword"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> Redirect to the application list page
        [HttpPost]
        public async Task<IActionResult> Status(Dictionary<string, int> selStatus, string? filterStatus, string? ordering, string? currentKeyword, int? pageNum)
        {
            // Set ViewData to keep search and sort conditions
            ViewData["FilterKeyword"] = currentKeyword;
            ViewData["FilterStatus"] = filterStatus;
            ViewData["CurrentOrder"] = ordering;
            ViewData["CurrentPage"] = pageNum;

            // Check if selected status exsits
            if (selStatus != null)
            {
                // Search which application will be targeted
                foreach (var applicationId in selStatus.Keys)
                {
                    // Get target application information
                    var application = await _context.applications
                                   .FirstOrDefaultAsync(j => j.applicationId == applicationId);
                    // Check if application exists
                    if (application != null)
                    {
                        // Start transaction
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            try
                            {
                                // Set new status
                                application.status = selStatus[applicationId];

                                // Check new status is 1(apply)
                                if (selStatus[applicationId] != 1)
                                {
                                    // status is not 1(apply), insert new interview row for this application, if not exist
                                    var interviewInfo = await _context.interviews
                                                        .FirstOrDefaultAsync(i => i.applicationId == applicationId);
                                    if (interviewInfo == null)
                                    {
                                        Interview newInterview = new Interview();
                                        newInterview.applicationId = application.applicationId;
                                        _context.interviews.Add(newInterview);
                                    }
                                }
                                // Save changes
                                await _context.SaveChangesAsync();
                                // Commit transaction
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                // Add error log
                                _logger.LogError(ex, "Error happened at updateing status");
                                // Rollback transaction
                                transaction.Rollback();

                                // return to the application list page without changes
                                return View();
                            }
                        }
                    }
                }
            }

            // Move to the application list page
            return RedirectToAction(nameof(Index), new { currentKeyword = currentKeyword, filterStatus = filterStatus, ordering = ordering, pageNum = pageNum });
        }

        /// <summary>
        /// Method to check if application exists
        /// </summary>
        /// <param name="id"> id is applicationid
        /// <return> Boolean
        private bool ApplicationExists(string id)
        {
            // Check if application exists or not
            return _context.applications.Any(e => e.applicationId == id);
        }

        /// <summary>
        /// Get mehotd, download files from Azure Blob
        /// </summary>
        /// <param name="fileName"> File name</param>
        /// <return> File
        public async Task<IActionResult> Download(string fileName)
        {
            // Check if filename is null
            if (fileName == null)
            {
                // Return to the application list page
                return RedirectToAction(nameof(Index));
            }

            // Call FileHelper class to download file, it returns FileStreamResult
            var result = await FileHelper.DownloadFileAsync(fileName, _path, _container);
            // Download file
            return result;
        }
    }
}
