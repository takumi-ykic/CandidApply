using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CandidApply.Data;
using CandidApply.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using CandidApply.Helpers;
using CandidApply.CommonMethods;

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
        /// <param name="filterKeyword"> A new search keyword as string
        /// <param name="filterStatus"> Selected status for search
        /// <param name="ordering"> The current condition of sorting application Date
        /// <param name="currentKeyword"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> The application history list
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
        /// <param name="filterStatus"> Selected status for search
        /// <param name="ordering"> The current condition of sorting application Date
        /// <param name="currentFilter"> The using search keyword
        /// <param name="pageNum"> Store page number
        /// <return> Application View Model to display detail
        public async Task<IActionResult> Details(string? applicationId, string? filterStatus, string? ordering, string? currentKeyword, int? pageNum)
        {
            // Check if application id is null
            if (applicationId.IsNullOrEmpty())
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

            // Move to detail page with full Application View Model
            return View(application);
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
