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
                ViewData["NoApplications"] = "You do not have any history yet.";
                return View();
            }

            if (searchKeyword != null)
            {
                pageNum = 1;
            }
            else
            {
                searchKeyword = currentFileter;
            }
            ViewData["SearchKeyword"] = searchKeyword;
            ViewData["CurrentPage"] = pageNum;

            int selStatus = 0;
            if (selSearchStatus != null)
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
            ViewData["SortDate"] = sortDate == "Date" ? "date_desc" : "Date";
            switch (sortDate)
            {
                case "Date":
                    applicationList = applicationList.OrderBy(a => a.applicationDate);
                    break;
                case "date_desc":
                    applicationList = applicationList.OrderByDescending(a => a.applicationDate);
                    break;
                default:
                    applicationList = applicationList.OrderByDescending(a => a.applicationDate);
                    break;
            }

            var listedApplications = await applicationList.ToListAsync();

            if (listedApplications.Any())
            {
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
                            .Include(a => a.ApplicationStatus)
                            .Include(a => a.ApplicationFile)
                            .Include(a => a.Interview)
                            .FirstOrDefaultAsync(j => j.applicationId == applicationId);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
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
