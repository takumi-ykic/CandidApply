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

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        private async Task LoadAsync(User user)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            string resume = null;
            string resumeUrl = null;
            string coverLetter = null;
            string coverLetterUrl = null;

            if (user.resume != null)
            {
                resume = user.resume;
            }

            if (user.coverLetter != null)
            {
                coverLetter = user.coverLetter;
            }

            Profile = new ProfileModel
            {
                UserId = userId,
                PhoneNumber = phoneNumber,
                Username = userName,
                Resume = resume,
                ResumeUrl = resumeUrl,
                CoverLetter = coverLetter,
                CoverLetterUrl = coverLetterUrl
            };
        }

        public async Task<IActionResult> OnPostAsync(IFormFile uploadResume, IFormFile uploadCoverLetter)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (Profile.Username == null)
            {
                ModelState.AddModelError(string.Empty, "Username is not allowed empty.");
                return Page();
            }
            if (Profile.Username.Trim().Length != 0)
            {
                var username = await _userManager.GetUserNameAsync(user);
                var newUsername = Profile.Username.Trim();

                if (username != newUsername)
                {
                    var setUsernameResult = await _userManager.SetUserNameAsync(user, newUsername);
                    if (!setUsernameResult.Succeeded)
                    {
                        ModelState.AddModelError(string.Empty, newUsername.ToString() + " is taken by another user.");
                        return Page();
                    }

                    var oldUsernameClaim = new Claim(ClaimTypes.Name, username);
                    await _userManager.RemoveClaimAsync(user, oldUsernameClaim);
                    var newUsernameClaim = new Claim(ClaimTypes.Name, newUsername);
                    await _userManager.AddClaimAsync(user, newUsernameClaim);
                }
            }

            if (Profile.PhoneNumber != null)
            {
                if (Profile.PhoneNumber.Trim().Length != 0)
                {
                    var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
                    var newPhoneNumber = Profile.PhoneNumber.Trim();

                    if (phoneNumber != newPhoneNumber)
                    {
                        var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, newPhoneNumber);
                        if (!setPhoneResult.Succeeded)
                        {
                            ModelState.AddModelError(string.Empty, "Unexpected error when trying to set phone number.");
                            return Page();
                        }
                    }
                }
            }

            if (uploadResume != null && uploadResume.Length != 0)
            {
                bool flag = await UploadResume(user, uploadResume);
                if (!flag)
                {
                    ModelState.AddModelError(string.Empty, "Fail to upload resume.");
                    return Page();
                }
            }

            if (uploadCoverLetter != null && uploadCoverLetter.Length != 0)
            {
                bool flag = await UploadCoverLetter(user, uploadCoverLetter);
                if (!flag)
                {
                    ModelState.AddModelError(string.Empty, "Fail to upload cover letter.");
                    return Page();
                }
            }

            await _signInManager.SignOutAsync();
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }

        public async Task<bool> UploadResume(User user, IFormFile uploadResume)
        {
            var userId = await _userManager.GetUserIdAsync(user);

            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(_path);

                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);

                if(!await containerClient.ExistsAsync())
                {
                    await containerClient.CreateIfNotExistsAsync();
                }

                string blobName = userId.Substring(0, Math.Min(5, userId.Length)) + "_resume.pdf";
                BlobClient blobClient = containerClient.GetBlobClient(blobName);
                if(await blobClient.ExistsAsync())
                {
                    await blobClient.DeleteAsync();
                }

                await containerClient.UploadBlobAsync(blobName, uploadResume.OpenReadStream());

                user.resume = blobName;
                await _userManager.UpdateAsync(user);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading resume: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UploadCoverLetter(User user, IFormFile uploadCoverLetter)
        {
            var userId = await _userManager.GetUserIdAsync(user);

            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(_path);

                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);

                if (!await containerClient.ExistsAsync())
                {
                    await containerClient.CreateIfNotExistsAsync();
                }

                string blobName = userId.Substring(0, Math.Min(5, userId.Length)) + "_coverletter.pdf";
                BlobClient blobClient = containerClient.GetBlobClient(blobName);
                if (await blobClient.ExistsAsync())
                {
                    await blobClient.DeleteAsync();
                }

                await containerClient.UploadBlobAsync(blobName, uploadCoverLetter.OpenReadStream());

                user.coverLetter = blobName;
                await _userManager.UpdateAsync(user);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading resume: {ex.Message}");
                return false;
            }
        }

        [HttpGet]
        public async Task<IActionResult> OnGetDownload(string fileName)
        {
            if (fileName == null)
            {
                return Page();
            }

            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(_path);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_container);
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                var response = await blobClient.OpenReadAsync();

                using(var memoryStream = new MemoryStream())
                {
                    await response.CopyToAsync(memoryStream);
                    var contentType = "application/octet-stream";

                    return File(memoryStream.ToArray(), contentType, fileName);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error uploading resume: {ex.Message}");
                return Page();
            }  
        }
    }
}
