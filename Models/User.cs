using Microsoft.AspNetCore.Identity;

namespace CandidApply.Models
{
    public class User : IdentityUser
    {
        public string? resume { get; set; }
        public string? coverLetter { get; set; }
    }
}
