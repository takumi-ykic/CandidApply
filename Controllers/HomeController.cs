using CandidApply.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CandidApply.Controllers
{
    /// <summary>
    /// Home Controller for top page
    /// </summary>
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly UserManager<User> _userManager;

        public HomeController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Route to top page
        /// </summary>
        /// <return> View
        public IActionResult Index()
        {
            // Check if user is login
            if(User.Identity.IsAuthenticated)
            {
                // When user is already login, route to the application list page
                return RedirectToAction("Index", "Applications");
            }

            // Move to top page
            return View();
        }

        /// <summary>
        /// Move to login page through button
        /// </summary>
        /// <return> Redirect to login page
        public async Task<IActionResult> MoveToLogin()
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);

            // Check if login user exsits
            if (user != null)
            {
                // User is login in, move to the application list page
                return RedirectToAction("Index", "Applications");
            }
            // Move to login page
            return Redirect("/Identity/Account/Login");
        }

        /// <summary>
        /// Move to register page through button
        /// </summary>
        /// <return> Redirect to register page
        public async Task<IActionResult> MoveToRegister()
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);

            // Check if login user exsits
            if (user != null)
            {
                // User is login in, move to the application list page
                return RedirectToAction("Index", "Applications");
            }
            // Move to login page
            return Redirect("/Identity/Account/Register");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
