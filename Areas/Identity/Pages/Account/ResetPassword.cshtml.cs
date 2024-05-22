#nullable disable

using System.ComponentModel.DataAnnotations;
using CandidApply.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CandidApply.Areas.Identity.Pages.Account
{
    /// <summary>
    /// ResetPassword class to implement reset password function
    /// </summary>
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public ResetPasswordModel(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Input Model for reset password
        /// </summary>
        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        /// <summary>
        /// Get method, load resetpassword page
        /// </summary>
        /// <return> Return resetpassword page
        public IActionResult OnGet()
        {
            // Instance Input Model
            Input = new InputModel();

            // Return to resetpassword
            return Page();
        }

        /// <summary>
        /// Post method, execute reset password function
        /// </summary>
        /// <return> If reset is failure,return to reset password page. If reset is success, move to login page
        public async Task<IActionResult> OnPostAsync()
        {
            // Check if all user input is valid or not
            if (!ModelState.IsValid)
            {
                // Return reset password page
                return Page();
            }

            // Get user information by email
            var user = await _userManager.FindByEmailAsync(Input.Email);

            //Check if user exists or not
            if (user == null)
            {
                // Add error message
                ModelState.AddModelError(string.Empty, "Invalid email address.");
                //Return to reset password page with error message
                return Page();
            }

            // Generate token to identity user credential
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Change password by user information, token, and new password
            var result = await _userManager.ResetPasswordAsync(user, code, Input.Password);
            // Check the result
            if (result.Succeeded)
            {
                // Success in reset password, move to login page
                return RedirectToPage("./Login");
            }

            
            foreach (var error in result.Errors)
            {
                // Set all error messages during reseting password process
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Return to reset password page with error messages
            return Page();
        }
    }
}
