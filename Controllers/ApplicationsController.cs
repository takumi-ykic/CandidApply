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

        public ApplicationsController(ApplicationContext context, UserManager<User> userManager, ILogger<ApplicationsController> logger, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _configration = configuration;
            _path = _configration.GetValue<string>("AzureSettings:filestorage");
            _container = _configration.GetValue<string>("AzureSettings:applicationcontainer");
            
        }

        /// <summary>
        /// Application main page. This displays the application list.
        /// </summary>
        /// <param name="searchKeyword"> A new search keyword as string
        /// <param name="selSearchStatus"> Selected status for search
        /// <param name="sortDate"> The current condition of sorting application Date
        /// <param name="currentFilter"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> The application list page
        public async Task<IActionResult> Index(string? searchKeyword, string? selSearchStatus, string? sortDate, string? currentFileter, int? pageNum)
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

            // Get applications based on user id with using Entity Framework
            var applicationList = _context.applications
                    .Include(a => a.ApplicationStatus)
                    .Include(a => a.ApplicationFile)
                    .Include(a => a.Interview)
                    .Where(a => a.userId != null && a.userId.Contains(userId));

            // Check if any application in the list
            if (!applicationList.Any())
            {
                // Message for a new user
                ViewData["NoApplications"] = "Let's start adding your application!";
                // Return to the list page with message
                return View();
            }

            // Remove application that has staus of 5(Rejected) and delete flag
            applicationList = applicationList
                                .Where(a => a.status != 5)
                                .Where(a => a.deleteFlag == 0);

            // Check if search keyword is null
            if (searchKeyword != null)
            {
                // Page number is initialized
                pageNum = 1;
            }else
            {
                // Keep current using search keyword
                searchKeyword = currentFileter;
            }
            // Set ViewData to keep condition
            ViewData["SearchKeyword"] = searchKeyword;
            ViewData["CurrentPage"] = pageNum;

            int selStatus = 0;
            // Check if search status is selected
            if(selSearchStatus != null)
            {
                // Convert to int
                selStatus = int.Parse(selSearchStatus);
            }
            // Set ViewData to keep condition
            ViewData["SelSearchStatus"] = selSearchStatus;
            // Create status list
            var statusTable = await _context.status.ToListAsync();
            var searchStatusList = new List<SelectListItem>();
            // Set status information to ApplicationStatus ViewModel
            foreach (ApplicationStatus status in statusTable)
            {
                // Add status to the list
                searchStatusList.Add(new SelectListItem
                {
                    Value = status.statusId.ToString(),
                    Text = status.statusName,
                    // If status is the same as selected status for search, set true
                    Selected = status.statusId == selStatus
                });
            }
            // Set ViewData to keep condition
            ViewData["SearchStatusList"] = searchStatusList;

            // Check if searchkeyword is null
            if (!String.IsNullOrEmpty(searchKeyword))
            {
                // Add condition to make the application list based on search keyword
                applicationList = applicationList
                        .Where(a => (a.jobTitle != null && a.jobTitle.Contains(searchKeyword))
                        || (a.company != null && a.company.Contains(searchKeyword))
                        || (a.Interview != null && a.Interview.memo != null && a.Interview.memo.Contains(searchKeyword)));
            }

            // Check if search status is null
            if (selStatus != 0)
            {
                // Add condition for search status
                applicationList = applicationList.Where(a => a.status == selStatus);
            }
            // Set ViewData to keep sorting condition
            ViewData["CurrentSort"] = sortDate;
            // Set new sorting condition to ViewData
            ViewData["SortDate"] = sortDate == "Date" ? "date_asc" : "Date";
            // Change sorting
            switch (sortDate)
            {
                // Desc
                case "Date":
                    applicationList = applicationList.OrderByDescending(a => a.applicationDate);
                    break;
                // Asc
                case "date_asc":
                    applicationList = applicationList.OrderBy(a => a.applicationDate);
                    break;
                // Asc
                default:
                    applicationList = applicationList.OrderBy(a => a.applicationDate);
                    break;
            }

            // Conver to list
            var listedApplications = await applicationList.ToListAsync();

            // Check if any applications are existed in the list
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
                // Set Collection of status
                ViewData["StatusDictionary"] = statusDictionary;

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

            // Start transactions to insert new row for application, applicationFile, and interview
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Insert a new information for application
                    _context.Add(application);
                    await _context.SaveChangesAsync();

                    // Set userid for both applicationfile and interview
                    applicationFile.applicationId = application.applicationId;
                    interview.applicationId = application.applicationId;
                    // Insert a new row with only userid for applicationFile
                    _context.files.Add(applicationFile);
                    // Insert a new row with only userid for interview
                    _context.interviews.Add(interview);
                    await _context.SaveChangesAsync();

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

            // Upload files
            // Check if resume file is null
            if (upResume != null && upResume.Length != 0)
            {
                // Upload resume file to Azure Blob
                bool flag = await UploadResume(applicationFile.applicationId, upResume);
                // Check if uploading is success
                if (!flag)
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
                bool flag = await UploadCoverLetter(applicationFile.applicationId, upCoverLetter);
                // Check if uploading is success
                if (!flag)
                {
                    // Failure, add error message
                    ModelState.AddModelError(string.Empty, "Error. Uploading cover letter is failure.");
                    // Return to create page with error message
                    return View(application);
                }
            }

            // Every process is success, redirect to the application list page
            return RedirectToAction(nameof(Index), new { searchKeyword = "" });
        }

        /// <summary>
        /// Get method, get application detail information
        /// </summary>
        /// <param name="applicationId"> applicationId to get its detail
        /// <param name="selSearchStatus"> Selected status for search
        /// <param name="sortDate"> The current condition of sorting application Date
        /// <param name="currentFilter"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> Application View Model to display detail
        public async Task<IActionResult> Details(int? applicationId, string? selSearchStatus, string? sortDate, string? currentFileter, int? pageNum)
        {
            // Check if application id is null
            if (applicationId == null)
            {
                // Return to not found page
                return NotFound();
            }

            // Set ViewData to keep search and sort condition
            ViewData["SearchKeyword"] = currentFileter;
            ViewData["SelSearchStatus"] = selSearchStatus;
            ViewData["CurrentSort"] = sortDate;
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
        /// <param name="selSearchStatus"> Selected status for search
        /// <param name="sortDate"> The current condition of sorting application Date
        /// <param name="currentFilter"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> Move to edit page with Application View Model
        public async Task<IActionResult> Edit(int? applicationId, string? selSearchStatus, string? sortDate, string? currentFileter, int? pageNum)
        {
            // Check if application id is null
            if (applicationId == null)
            {
                // Move to not found page
                return NotFound();
            }

            // Set ViewData to keep current search and sort condition
            ViewData["SearchKeyword"] = currentFileter;
            ViewData["SelSearchStatus"] = selSearchStatus;
            ViewData["CurrentSort"] = sortDate;
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
                // Get status list
                Dictionary<int, SelectList> statusDictionary = new Dictionary<int, SelectList>();
                int selectedStatusId = application.status;
                var statusList = PopulateddlApplyStatus(selectedStatusId);
                statusDictionary.Add(application.applicationId, statusList);

                // Set status list to ViewData
                ViewData["StatusDictionary"] = statusDictionary;
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
        public async Task<IActionResult> Edit(int applicationId, int status, [Bind("applicationId,jobTitle,company,applicationDate")] Application application,
                                                IFormFile upResume, IFormFile upCoverLetter,
                                                DateTime? interviewDate, string? location, string? memo)
        {
            // Check if application id is null
            if (applicationId != application.applicationId)
            {
                // Move to not found page
                return NotFound();
            }

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
                // Check if resume file exists
                if (upResume != null && upResume.Length != 0)
                {
                    // Upload resume file to Azure Blob with returning boolean value
                    bool flag = await UploadResume(application.applicationId, upResume);
                    // Check if upload is success
                    if (!flag)
                    {
                        // Failure, add error message
                        ModelState.AddModelError(string.Empty, "Error. Uploading resume is failure.");
                        // Return to edit page with error message
                        return View(application);
                    }
                }

                // Check if cover letter file exsits
                if (upCoverLetter != null && upCoverLetter.Length != 0)
                {
                    // Upload cover letter file to Azure Blob with returning boolean value
                    bool flag = await UploadCoverLetter(application.applicationId, upCoverLetter);
                    // Check if upload is success
                    if (!flag)
                    {
                        // Failure, add error message
                        ModelState.AddModelError(string.Empty, "Error. Uploading resume is failure.");
                        // Return to edit page with error message
                        return View(application);
                    }
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
            return RedirectToAction(nameof(Details), new { applicationId = application.applicationId });
        }

        /// <summary>
        /// Post method, delete application logically
        /// </summary>
        /// <param name="applicationId"> Target application id
        /// <return> Redirect to application list page(index)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int applicationId)
        {
            // Check if applicationId exists
            if (applicationId == 0)
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
        /// <param name="selSearchStatus"> Selected status for search
        /// <param name="sortDate"> The current condition of sorting application Date
        /// <param name="currentFilter"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> Redirect to the application list page
        [HttpPost]
        public async Task<IActionResult> Status(Dictionary<int, int> selStatus, string? selSearchStatus, string? sortDate, string? currentFileter, int? pageNum)
        {
            // Set ViewData to keep search and sort conditions
            ViewData["SearchKeyword"] = currentFileter;
            ViewData["SelSearchStatus"] = selSearchStatus;
            ViewData["CurrentSort"] = sortDate;
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
            return RedirectToAction(nameof(Index), new { currentFileter = currentFileter, selSearchStatus = selSearchStatus, sortDate = sortDate, pageNum = pageNum });
        }

        /// <summary>
        /// Method to check if application exists
        /// </summary>
        /// <param name="id"> id is applicationid
        /// <return> Boolean
        private bool ApplicationExists(int id)
        {
            // Check if application exists or not
            return _context.applications.Any(e => e.applicationId == id);
        }

        /// <summary>
        /// Populate application status in Select List
        /// </summary>
        /// <param name="selectedStatus> Select status
        /// <return> SelectList
        private SelectList PopulateddlApplyStatus(int selectedStatus)
        {
            // Get all application status, and make it list
            var statusList = _context.status
                .Select(s => new SelectListItem
                {
                    Value = s.statusId.ToString(),
                    Text = s.statusName
                })
                .ToList();

            // Create SelectList to populate with selected status
            SelectList selectList = new SelectList(statusList, "Value", "Text", selectedStatus);

            //return Select List
            return selectList;
        }

        /// <summary>
        /// Logic to upload resume file to Azure Blob
        /// </summary>
        /// <param name="id"> ApplicationId
        /// <param name="uploadResume"> Resume file as IFormFile
        /// <return> Boolean, if proccess is success, flag is true, if not, flag is false
        public async Task<bool> UploadResume(int id, IFormFile uploadResume)
        {
            // Get application file information
            var applicationFile = await _context.files
                            .Where(a => a.applicationId == id)
                            .FirstOrDefaultAsync();

            // Check if application file exists
            if (applicationFile != null)
            {
                try
                {
                    // Create Azure BlobServiceClient
                    BlobServiceClient blobServiceClient = new BlobServiceClient(_path);

                    // Create Azure BlobContainerClient
                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);

                    // Check if target container exists
                    if (!await containerClient.ExistsAsync())
                    {
                        // If target container does not exist, create one
                        await containerClient.CreateIfNotExistsAsync();
                    }

                    // Set blobname, this is file name
                    string blobName = id.ToString() + "_resume.pdf";
                    // Set path to upload file
                    BlobClient blobClient = containerClient.GetBlobClient(blobName);
                    // Check if file exists
                    if (await blobClient.ExistsAsync())
                    {
                        // File exists, delete this to avoid duplicate the same file name
                        await blobClient.DeleteAsync();
                    }

                    // Upload file
                    await containerClient.UploadBlobAsync(blobName, uploadResume.OpenReadStream());

                    // Update file name in table and update
                    applicationFile.resume = blobName;
                    _context.files.Update(applicationFile);
                    // Save
                    await _context.SaveChangesAsync();

                    // Every process is completed, return true
                    return true;
                }
                catch (Exception ex)
                {
                    // Add error log
                    _logger.LogError(ex, "An error occurred during file upload or database update.");
                    // Return false
                    return false;
                }
            }
            else
            {
                // Associated application file does not exist, return false
                return false;
            }
        }

        /// <summary>
        /// Logic to upload cover letter file to Azure Blob
        /// </summary>
        /// <param name="id"> ApplicationId
        /// <param name="uploadCoverLetter"> Cover Letter file as IFormFile
        /// <return> Boolean, if proccess is success, flag is true, if not, flag is false
        public async Task<bool> UploadCoverLetter(int id, IFormFile uploadCoverLetter)
        {
            // Get application file information
            var applicationFile = await _context.files
                            .Where(a => a.applicationId == id)
                            .FirstOrDefaultAsync();

            // Check if application file exists
            if (applicationFile != null)
            {
                try
                {
                    // Create Azure BlobServiceClient
                    BlobServiceClient blobServiceClient = new BlobServiceClient(_path);

                    // Create Azure BlobContainerClient
                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);

                    // Check if target container exists
                    if (!await containerClient.ExistsAsync())
                    {
                        // If target container does not exist, create one
                        await containerClient.CreateIfNotExistsAsync();
                    }

                    // Set blobname, this is file name
                    string blobName = id.ToString() + "_coverletter.pdf";
                    // Set path to upload file
                    BlobClient blobClient = containerClient.GetBlobClient(blobName);
                    // Check if file exists
                    if (await blobClient.ExistsAsync())
                    {
                        // File exists, delete this to avoid duplicate the same file name
                        await blobClient.DeleteAsync();
                    }

                    // Upload file
                    await containerClient.UploadBlobAsync(blobName, uploadCoverLetter.OpenReadStream());

                    // Update file name in table and update
                    applicationFile.coverLetter = blobName;
                    _context.files.Update(applicationFile);
                    // Save
                    await _context.SaveChangesAsync();

                    // Every process is completed, return true
                    return true;
                }
                catch (Exception ex)
                {
                    // Add error log
                    _logger.LogError(ex, "An error occurred during file upload or database update.");
                    // Return false
                    return false;
                }
            }
            else
            {
                // Associated application file does not exist, return false
                return false;
            }
        }

        /// <summary>
        /// Get mehotd, download files from Azure Blob
        /// </summary>
        /// <param name="fileName"> File name
        /// <return> File
        public async Task<IActionResult> Download(string fileName)
        {
            // Check if filename is null
            if (fileName == null)
            {
                // Return to the application list page
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Create Azure BlobServiceClient instance with path
                BlobServiceClient blobServiceClient = new BlobServiceClient(_path);
                // Create Azure BlobContainerClient instance
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);
                // Crate Azure BlobClient with fileName
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                // Prepare to read file
                var response = await blobClient.OpenReadAsync();

                // Start transaction
                using (var memoryStream = new MemoryStream())
                {
                    // Copy to allocated memory
                    await response.CopyToAsync(memoryStream);
                    // Set content type
                    var contentType = "application/octet-stream";

                    // Return File, downloading file will begin
                    return File(memoryStream.ToArray(), contentType, fileName);
                }
            }
            catch (Exception ex)
            {
                // Display error message in console
                Console.WriteLine($"Error download file: {ex.Message}");
                // Return page
                return Page();
            }
        }
    }
}
