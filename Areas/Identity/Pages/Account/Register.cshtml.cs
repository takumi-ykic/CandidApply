#nullable disable

using System.ComponentModel.DataAnnotations;
using CandidApply.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CandidApply.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Register class to implement register function
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = (IUserEmailStore<User>)GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        /// Input Model to bind user input information and set condition for each column
        /// </summary>
        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        /// <summary>
        /// Get method, when register page is loaded
        /// </summary>
        /// <param name="returnUrl">
        /// <return> Return to register page
        public Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Post method, check if user input is valid or not
        /// </summary>
        /// <param name="returnUrl">
        /// <return> Return page to the application list page with user information or register page with error message
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            //Check if all user input is valid or not based on Input class
            if (ModelState.IsValid)
            {
                // Look for the user who is using same email.
                var existedUser = await _userManager.FindByEmailAsync(Input.Email.Trim());
                // Check if the user who is using same email exists or not
                if (existedUser != null)
                {
                    // When the existed user is found, add error message that indicate inputed email is taken by someone else
                    ModelState.AddModelError(string.Empty, Input.Email.Trim() + " is already taken");
                    // Return to register page with error message
                    return Page();
                }

                // If inputed email is never used by other users, proceed to user creation proccess
                var user = CreateUser();

                // Set inputted email as username
                await _userStore.SetUserNameAsync((User)user, Input.Email, CancellationToken.None);
                // Set email
                await _emailStore.SetEmailAsync((User)user, Input.Email, CancellationToken.None);
                // In this application, email confirmed proccess is skip, so set flag as emailconfired is done
                user.EmailConfirmed = true;
                // Create user with using Identity framework function
                var result = await _userManager.CreateAsync((User)user, Input.Password);

                if (result.Succeeded)
                {
                    // Add log
                    _logger.LogInformation("User created a new account with password.");

                    // Signin user based on created user instance right now
                    await _signInManager.SignInAsync((User)user, isPersistent: false);

                    // Move to the application list
                    return RedirectToAction("Index", "Applications");
                }
                foreach (var error in result.Errors)
                {
                    // Set error messages happened during user registration processes
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                // When errors happen at some points, add error messages
                ModelState.AddModelError(string.Empty, "Invalid registration.");
                // Return to register page with error message
                return Page();
            }
            // Return to register page
            return Page();
        }

        /// <summary>
        /// Create a new user instance with using IdentityUser
        /// </summary>
        /// <return> Activator.CreateInstance
        private IdentityUser CreateUser()
        {
            try
            {
                // Return CreateInstance
                return Activator.CreateInstance<User>();
            }
            catch
            {
                // Throw exception
                throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
                    $"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        /// <summary>
        /// Get email store function provided by Identity framework
        /// </summary>
        /// <return> User store
        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
    }
}
