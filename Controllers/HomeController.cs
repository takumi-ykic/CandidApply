using CandidApply.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CandidApply.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly UserManager<User> _userManager;

        public HomeController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if(User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Applications");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> MoveToLogin()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                return RedirectToAction("Index", "Applications");
            }
            return Redirect("/Identity/Account/Login");
        }

        public async Task<IActionResult> MoveToRegister()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                return RedirectToAction("Index", "Applications");
            }
            return Redirect("/Identity/Account/Register");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
