using System.ComponentModel.DataAnnotations;

namespace CandidApply.Models
{
    public class ApplicationFile
    {
        public int fileId { get; set; }
        public int applicationId { get; set; }
        [Display(Name = "Resume")]
        public string? resume { get; set; }
        public string? resumePath { get; set; }
        [Display(Name = "Cover Letter")]
        public string? coverLetter { get; set; }
        public string? coverLetterPath { get; set; }

        public Application? Application { get; set; }
    }
}
