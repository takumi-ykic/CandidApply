using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandidApply.Models
{
    public class Application
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Job No.")]
        public int applicationId { get; set; }
        public string? userId { get; set; }
        [Required]
        [Display(Name = "Job Title")]
        [StringLength(60)]
        public string? jobTitle { get; set; }
        [Required]
        [Display(Name = "Company")]
        [StringLength(60)]
        public string? company { get; set; }
        [Required]
        [Display(Name = "Application Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime applicationDate { get; set; }
        public int status { get; set; } = 1;
        public DateTime createDate { get; set; } = DateTime.Now;
        public int deleteFlag { get; set; } = 0;

        public ApplicationStatus? ApplicationStatus { get; set; }
        public ApplicationFile? ApplicationFile { get; set; }
        public Interview? Interview { get; set; }
    }
}
