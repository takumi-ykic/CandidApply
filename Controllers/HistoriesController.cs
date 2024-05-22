using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CandidApply.Data;
using CandidApply.Models;
using Microsoft.AspNetCore.Identity;
using CandidApply;
using Microsoft.AspNetCore.Mvc.Rendering;
using Azure.Storage.Blobs;

namespace CandidApply.Controllers
{
    /// <summary>
    /// Histories Controller
    /// </summary>
    public class HistoriesController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configration;
        private readonly string _path;
        private readonly string _container;


        public HistoriesController(ApplicationContext context, UserManager<User> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configration = configuration;
            _path = _configration.GetValue<string>("AzureSettings:filestorage");
            _container = _configration.GetValue<string>("AzureSettings:applicationcontainer");
        }

        /// <summary>
        /// History main page. This displays the application list.
        /// </summary>
        /// <param name="searchKeyword"> A new search keyword as string
        /// <param name="selSearchStatus"> Selected status for search
        /// <param name="sortDate"> The current condition of sorting application Date
        /// <param name="currentFilter"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> The application history list
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

            // Get applications based on user id
            var applicationList = _context.applications
                    .Include(a => a.ApplicationStatus)
                    .Include(a => a.ApplicationFile)
                    .Include(a => a.Interview)
                    .Where(a => a.userId != null && a.userId.Contains(userId));

            // Check if any application in the list
            if (!applicationList.Any())
            {
                // Message for a new user
                ViewData["NoApplications"] = "You do not have any history yet.";
                // Return to the list page with message
                return View();
            }

            // Check if search keyword is null
            if (searchKeyword != null)
            {
                // Page number is initialized
                pageNum = 1;
            }
            else
            {
                // Keep current using search keyword
                searchKeyword = currentFileter;
            }
            // Set ViewData to keep condition
            ViewData["SearchKeyword"] = searchKeyword;
            ViewData["CurrentPage"] = pageNum;

            int selStatus = 0;
            // Check if search status is selected
            if (selSearchStatus != null)
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
            ViewData["SortDate"] = sortDate == "Date" ? "date_desc" : "Date";
            // Change sorting
            switch (sortDate)
            {
                // Desc
                case "Date":
                    applicationList = applicationList.OrderBy(a => a.applicationDate);
                    break;
                // Asc
                case "date_desc":
                    applicationList = applicationList.OrderByDescending(a => a.applicationDate);
                    break;
                // Asc
                default:
                    applicationList = applicationList.OrderByDescending(a => a.applicationDate);
                    break;
            }

            // Conver to list
            var listedApplications = await applicationList.ToListAsync();

            // Check if any applications are existed in the list
            if (listedApplications.Any())
            {
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
                            .Include(a => a.ApplicationStatus)
                            .Include(a => a.ApplicationFile)
                            .Include(a => a.Interview)
                            .FirstOrDefaultAsync(j => j.applicationId == applicationId);

            // Check if application is null
            if (application == null)
            {
                // Return to not found page
                return NotFound();
            }

            / Move to detail page with full Application View Model
            return View(application);
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
