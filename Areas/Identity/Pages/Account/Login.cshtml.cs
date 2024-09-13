#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CandidApply.Models;

namespace CandidApply.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Login class to implement login function
    /// </summary>
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<User> signInManager,
                            UserManager<User> userManager,
                              ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Input model for login page
        /// </summary>
        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        /// <summary>
        /// Get method when page is loaded
        /// </summary>
        /// <param name="returnUrl">
        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                // Add error message to ModelState
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        /// <summary>
        /// Post method when a user input user information to try to login
        /// </summary>
        /// <param name="returnUrl">
        /// <return> If login is success, application page. If not, return login page with error message
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Action("Index", "Applications");

            if (ModelState.IsValid)
            {
                // Get user information by email
                var user = await _userManager.FindByEmailAsync(Input.Email.Trim());
                if (user != null)
                {
                    // If user is existed, login with username, password, rememberMe, and lockout information by using Identity framework
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        // Success login
                        _logger.LogInformation("User logged in.");
                        // Move to the application list page
                        return RedirectToAction("Index", "Applications");
                    }
                    else
                    {
                        // Fail login, set error message
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        // Return login page with error message
                        return Page();
                    }
                }
                else
                {
                    // If user was not found by email, add error messsage
                    ModelState.AddModelError(string.Empty, "Not found user");
                    // Return login page with error message
                    return Page();
                }
            }
            else
            {
                // Invalid login, it is caused by mistaking email, password, or both
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                // Return login page with error message
                return Page();
            }
        }
    }
}
