#nullable disable

using System.ComponentModel.DataAnnotations;
using CandidApply.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CandidApply.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// ChangePassword class to implement change password function
    /// </summary>
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        [TempData]
        public string StatusMessage { get; set; }
        /// <summary>
        /// Input Model for changing password
        /// </summary>
        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string OldPassword { get; set; }
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; }
            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        /// <summary.
        /// Get method, get user information by User Identity, check if a user has password
        /// </summary>
        /// <return> If user information is set properly, move to change password page. If user information is not valid, move to Not Found page or SetPassword page
        public async Task<IActionResult> OnGetAsync()
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // If fail to get login user information, move to notfound page
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            //var hasPassword = await _userManager.HasPasswordAsync(user);
            //if (!hasPassword)
            //{
            //    return RedirectToPage("./SetPassword");
            //}

            // Move to change password page
            return Page();
        }

        /// <summary>
        /// Post method, execute change password function
        public async Task<IActionResult> OnPostAsync()
        {
            // Check if user input is valid
            if (!ModelState.IsValid)
            {
                // If user input is invalid, return to change password page
                return Page();
            }

            // Get login user information
            var user = await _userManager.GetUserAsync(User);
            // Check if user information get or not
            if (user == null)
            {
                // Move to not found page
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Execute change password function with using Identity framework
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            // Check if change password is success
            if (!changePasswordResult.Succeeded)
            {
                // Change password is failure
                foreach (var error in changePasswordResult.Errors)
                {
                    // Add error message
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                // Return to change password page with error message
                return Page();
            }

            // Refresh signin user information
            await _signInManager.RefreshSignInAsync(user);
            // Add log
            _logger.LogInformation("User changed their password successfully.");
            // Add status messageg indicate success change password
            StatusMessage = "Your password has been changed.";

            // Redirect to change password with status message
            return RedirectToPage();
        }
    }
}
