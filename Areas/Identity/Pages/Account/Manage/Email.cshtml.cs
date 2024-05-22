#nullable disable

using System.ComponentModel.DataAnnotations;
using CandidApply.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CandidApply.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Email class to implement change email function
    /// </summary>
    public class EmailModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public EmailModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Email { get; set; }
        public bool IsEmailConfirmed { get; set; }
        [TempData]
        public string StatusMessage { get; set; }
        [BindProperty]
        public InputModel Input { get; set; }
        /// <summary>
        /// Input Model for changing email
        /// </summary>
        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "New email")]
            public string NewEmail { get; set; }
        }

        /// <summary>
        /// Load task for email page
        /// </summary>
        /// <param name="user"> User information with User class
        private async Task LoadAsync(User user)
        {
            // Get email from login user information
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            // Create Input Model with new email
            Input = new InputModel
            {
                NewEmail = email,
            };

            // Check if email is confirmed. In this application, email confirmation is skip
            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        /// <summary>
        /// Get method, get login user information, then move to email page
        /// </summary>
        /// <return> Return to email page
        public async Task<IActionResult> OnGetAsync()
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);
            // Check if user login is proper
            if (user == null)
            {
                // User information is found, move to notfound page
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Load user information
            await LoadAsync(user);
            // Move to email apge
            return Page();
        }

        /// <summary>
        /// Post method, logic to change email
        /// </summary>
        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);
            // Check if user information exists
            if (user == null)
            {
                // Move to not found page
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Check if user input is valid
            if (!ModelState.IsValid)
            {
                // Load user
                await LoadAsync(user);
                // Return to email page without changing email
                return Page();
            }

            // Check if email duplicates with using Identity framework
            var IsEmailExisted = await _userManager.FindByEmailAsync(Input.NewEmail.TrimEnd());
            // Check if email is duplicated
            if (IsEmailExisted != null)
            {
                // Same email exists, add error message that indicate user input email cannot be used
                ModelState.AddModelError(string.Empty, Input.NewEmail.Trim() + " is already taken.");
                // Return to email page with errror message
                return Page();
            }

            // Get email from login user
            var email = await _userManager.GetEmailAsync(user);
            // Check if email is null
            if (Input.NewEmail != email)
            {
                // Generate token to change email
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);

                // Execute changing email function from Identity framework
                var result = await _userManager.ChangeEmailAsync(user, Input.NewEmail, code);
                // Check if changing email is success or not
                if (result.Succeeded)
                {
                    // Refresh signin information with new user information
                    await _signInManager.RefreshSignInAsync(user);
                    // Return to email page with new user information
                    return RedirectToPage();
                }
            }

            // Add status message that indicate failure to change email
            StatusMessage = "Your email is unchanged.";
            // Return email page with error message
            return RedirectToPage();
        }
    }
}
