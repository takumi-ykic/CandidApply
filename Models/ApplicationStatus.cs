using System.ComponentModel.DataAnnotations;

namespace CandidApply.Models
{
    public class ApplicationStatus
    {
        public int statusId { get; set; }
        [Display(Name = "Current Status")]
        public string? statusName { get; set; }
    }
}
