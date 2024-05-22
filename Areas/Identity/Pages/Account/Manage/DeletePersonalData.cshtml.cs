#nullable disable

using System.ComponentModel.DataAnnotations;
using CandidApply.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CandidApply.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// DeletePersonal class to implement delete user information
    /// </summary>
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;

        public DeletePersonalDataModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<DeletePersonalDataModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Input Model to confirm deletion
        /// </summary>
        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public bool RequirePassword { get; set; }

        /// <summary>
        /// Get method, get login user information, move to delete personal page
        /// </summary>
        /// <return> Move to delete personal page
        public async Task<IActionResult> OnGet()
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);
            // Check if login properly
            if (user == null)
            {
                // User doesn't exist, move to not found page
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Set user password as require password
            RequirePassword = await _userManager.HasPasswordAsync(user);
            // Move to delete personal page
            return Page();
        }

        /// <summary>
        /// Post method, proccess to delete user information
        /// </summary>
        /// <return> Return to top page
        public async Task<IActionResult> OnPostAsync()
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);
            // Check if login properly
            if (user == null)
            {
                // Return to not found page, if login user is invalid
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Set require passwrod
            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                // Execute checking password with using Identity framework
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    // Add error message
                    ModelState.AddModelError(string.Empty, "Incorrect password.");
                    // Return to delete personal page with error message
                    return Page();
                }
            }

            // Execute delete user information with using Identity framework
            var result = await _userManager.DeleteAsync(user);
            // Get user ID who has been deleted
            var userId = await _userManager.GetUserIdAsync(user);
            // Check if Delete user success
            if (!result.Succeeded)
            {
                // Fail to delete user information, throw exception
                throw new InvalidOperationException($"Unexpected error occurred deleting user.");
            }

            // Sign out
            await _signInManager.SignOutAsync();

            // Add log
            _logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

            // Return to top page
            return Redirect("~/");
        }
    }
}
