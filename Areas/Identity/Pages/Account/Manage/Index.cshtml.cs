#nullable disable

using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Claims;
using Azure.Storage.Blobs;
using CandidApply.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CandidApply.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Index class for user information main page
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configration;
        private readonly string _path;
        private readonly string _container;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configration = configuration;
            _path = _configration.GetValue<string>("AzureSettings:filestorage");
            _container = _configration.GetValue<string>("AzureSettings:usercontainer");
        }

        public string Username { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public ProfileModel Profile { get; set; }
        public string resumeUrl { get; set; }
        public string coverLetterUrl { get; set; }

        /// <suumary>
        /// Profile Model for user information
        /// </summary>
        public class ProfileModel
        {
            public string UserId { get; set; }
            [Display(Name = "User Name")]
            public string Username { get; set; }
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
            [Display(Name = "Resume")]
            public string Resume { get; set; }
            public string ResumeUrl { get; set; }
            [Display(Name = "Cover Letter")]
            public string CoverLetter { get; set; }
            public string CoverLetterUrl { get; set; }
        }

        /// <summary>
        /// Get method, get login user information and render them on user information main page
        /// </summary>
        /// <return> Move to user information main page
        public async Task<IActionResult> OnGetAsync()
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);
            // Check if user is null
            if (user == null)
            {
                // User information is null, move to notfound page
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Load user information
            await LoadAsync(user);
            // Return to user information main page
            return Page();
        }

        /// <summary>
        /// Set user information which is coming from UserManager based on login user
        /// </summary>
        /// <param name="user"> User class
        private async Task LoadAsync(User user)
        {
            // Get user information based UserManager method
            var userId = await _userManager.GetUserIdAsync(user);
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            string resume = null;
            string resumeUrl = null;
            string coverLetter = null;
            string coverLetterUrl = null;

            // Check if resume is null
            if (user.resume != null)
            {
                // Set resume filename
                resume = user.resume;
            }

            // Check if cover letter is null
            if (user.coverLetter != null)
            {
                // Set cover letter filename
                coverLetter = user.coverLetter;
            }

            // Create new Profile Model
            Profile = new ProfileModel
            {
                // Set all new user information to Profile model
                UserId = userId,
                PhoneNumber = phoneNumber,
                Username = userName,
                Resume = resume,
                ResumeUrl = resumeUrl,
                CoverLetter = coverLetter,
                CoverLetterUrl = coverLetterUrl
            };
        }

        /// <summary>
        /// Post method, update user information
        /// </summary>
        /// <param name="uploadResume"> Input resume file as IFormFile
        /// <param name="uploadCoverLetter"> Input cover letter file as IFormFile
        /// <return> User information main page
        public async Task<IActionResult> OnPostAsync(IFormFile uploadResume, IFormFile uploadCoverLetter)
        {
            // Get login user information
            var user = await _userManager.GetUserAsync(User);
            // Check if user is null
            if (user == null)
            {
                // User is null, move to not found page
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Check if user input is valid
            if (!ModelState.IsValid)
            {
                // Re-load user
                await LoadAsync(user);
                // Return to main page without any change
                return Page();
            }

            // Check if username is null
            if (Profile.Username == null)
            {
                // Add error message
                ModelState.AddModelError(string.Empty, "Username is not allowed empty.");
                // Return to main page with error message
                return Page();
            }
            // Check if username's length
            if (Profile.Username.Trim().Length != 0)
            {
                // Lneght is not 0, get current username from login user
                var username = await _userManager.GetUserNameAsync(user);
                // Set new username
                var newUsername = Profile.Username.Trim();

                // Check if current user is the same as new username
                if (username != newUsername)
                {
                    // Update new username with using Identity framework
                    var setUsernameResult = await _userManager.SetUserNameAsync(user, newUsername);
                    // Check if changing username success
                    if (!setUsernameResult.Succeeded)
                    {
                        // Fail to change username, add error message
                        ModelState.AddModelError(string.Empty, newUsername.ToString() + " is taken by another user.");
                        // Retunr to main page with error message
                        return Page();
                    }
                    
                    // Update User Claim
                    var oldUsernameClaim = new Claim(ClaimTypes.Name, username);
                    await _userManager.RemoveClaimAsync(user, oldUsernameClaim);
                    var newUsernameClaim = new Claim(ClaimTypes.Name, newUsername);
                    await _userManager.AddClaimAsync(user, newUsernameClaim);
                }
            }

            // Check if phonenumber is null
            if (Profile.PhoneNumber != null)
            {
                // Check if username's length
                if (Profile.PhoneNumber.Trim().Length != 0)
                {
                    // Get current phone number
                    var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
                    // Set new phone number
                    var newPhoneNumber = Profile.PhoneNumber.Trim();

                    // Check if current phone number is the same as new phone number
                    if (phoneNumber != newPhoneNumber)
                    {
                        // Update new phone number by using Identity framework
                        var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, newPhoneNumber);
                        // Check if process is success
                        if (!setPhoneResult.Succeeded)
                        {
                            // Failure, add error message
                            ModelState.AddModelError(string.Empty, "Unexpected error when trying to set phone number.");
                            // Return to main page with error message
                            return Page();
                        }
                    }
                }
            }

            // Check if resume is null
            if (uploadResume != null && uploadResume.Length != 0)
            {
                // Execute uploading resume to Azure Blob
                bool flag = await UploadResume(user, uploadResume);
                // Check if uploading is success
                if (!flag)
                {
                    // Failure, add error message
                    ModelState.AddModelError(string.Empty, "Fail to upload resume.");
                    // Return to main page with error message
                    return Page();
                }
            }

            // Check if colver letter is null
            if (uploadCoverLetter != null && uploadCoverLetter.Length != 0)
            {
                // Execute uploading cover letter to Azure Blob
                bool flag = await UploadCoverLetter(user, uploadCoverLetter);
                // Check if uploading is success
                if (!flag)
                {
                    // Failure, add error message
                    ModelState.AddModelError(string.Empty, "Fail to upload cover letter.");
                    // Return to main page with error message
                    return Page();
                }
            }

            // Update signin information with new user information
            await _signInManager.SignOutAsync();
            await _signInManager.RefreshSignInAsync(user);
            // Add suucess message
            StatusMessage = "Your profile has been updated";
            // Redirect to main page with status message
            return RedirectToPage();
        }

        /// <summary>
        /// Logic to upload resume file to Azure Blob
        /// </summary>
        /// <param name="user"> User class
        /// <param name="uploadResume"> Resume file as IFormFile
        /// <return> Boolean, if proccess is success, flag is true, if not, flag is false
        public async Task<bool> UploadResume(User user, IFormFile uploadResume)
        {
            // Get userId
            var userId = await _userManager.GetUserIdAsync(user);

            try
            {
                // Create Azure BlobServiceClient
                BlobServiceClient blobServiceClient = new BlobServiceClient(_path);

                // Create Azure BlobContainerClient
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);

                // Check if target container exists
                if(!await containerClient.ExistsAsync())
                {
                    // If target container does not exist, create one
                    await containerClient.CreateIfNotExistsAsync();
                }

                // Set blobname, this is file name
                string blobName = userId.Substring(0, Math.Min(5, userId.Length)) + "_resume.pdf";
                // Set path to upload file
                BlobClient blobClient = containerClient.GetBlobClient(blobName);
                // Check if file exists
                if(await blobClient.ExistsAsync())
                {
                    // File exists, delete this to avoid duplicate the same file name
                    await blobClient.DeleteAsync();
                }

                // Upload file
                await containerClient.UploadBlobAsync(blobName, uploadResume.OpenReadStream());

                // Update file name in table
                user.resume = blobName;
                // Update user information with using Identity framework
                await _userManager.UpdateAsync(user);

                // Return true
                return true;
            }
            catch (Exception ex)
            {
                // Display error message in console
                Console.WriteLine($"Error uploading resume: {ex.Message}");
                // Return false
                return false;
            }
        }

        /// <summary>
        /// Logic to upload cover letter file to Azure Blob
        /// </summary>
        /// <param name="user"> User class
        /// <param name="uploadCoverLetter"> Cover Letter file as IFormFile
        /// <return> Boolean, if proccess is success, flag is true, if not, flag is false
        public async Task<bool> UploadCoverLetter(User user, IFormFile uploadCoverLetter)
        {
            // Get userId
            var userId = await _userManager.GetUserIdAsync(user);

            try
            {
                // Create Azure BlobServiceClient
                BlobServiceClient blobServiceClient = new BlobServiceClient(_path);

                // Create Azure BlobContainerClient
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);

                // Check if target container exists
                if (!await containerClient.ExistsAsync())
                {
                    // If target container does not exist, create one
                    await containerClient.CreateIfNotExistsAsync();
                }

                // Set blobname, this is file name
                string blobName = userId.Substring(0, Math.Min(5, userId.Length)) + "_coverletter.pdf";
                // Set path to upload file
                BlobClient blobClient = containerClient.GetBlobClient(blobName);
                // Check if file exists
                if (await blobClient.ExistsAsync())
                {
                    // File exists, delete this to avoid duplicate the same file name
                    await blobClient.DeleteAsync();
                }

                // Upload file
                await containerClient.UploadBlobAsync(blobName, uploadCoverLetter.OpenReadStream());

                // Update file name in table
                user.coverLetter = blobName;
                // Update user information with using Identity framework
                await _userManager.UpdateAsync(user);

                // Return true
                return true;
            }
            catch (Exception ex)
            {
                // Display error message in console
                Console.WriteLine($"Error uploading resume: {ex.Message}");
                // Return false
                return false;
            }
        }

        /// <summary>
        /// Get mehotd, download files from Azure Blob
        /// </summary>
        /// <param name="fileName"> File name
        /// <return> File
        [HttpGet]
        public async Task<IActionResult> OnGetDownload(string fileName)
        {
            // Check if filename is null
            if (fileName == null)
            {
                // Return to main page without doing anything
                return Page();
            }

            try
            {
                // Create Azure BlobServiceClient instance with path
                BlobServiceClient blobServiceClient = new BlobServiceClient(_path);
                // Create Azure BlobContainerClient instance
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);
                // Crate Azure BlobClient with fileName
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                // Prepare to read file
                var response = await blobClient.OpenReadAsync();

                // Start transaction
                using(var memoryStream = new MemoryStream())
                {
                    // Copy to allocated memory
                    await response.CopyToAsync(memoryStream);
                    // Set content type
                    var contentType = "application/octet-stream";

                    // Return File, downloading file will begin
                    return File(memoryStream.ToArray(), contentType, fileName);
                }
            }
            catch(Exception ex)
            {
                // Display error message in console
                Console.WriteLine($"Error download file: {ex.Message}");
                // Return page
                return Page();
            }  
        }
    }
}
