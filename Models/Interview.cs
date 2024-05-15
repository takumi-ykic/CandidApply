using System.ComponentModel.DataAnnotations;

namespace CandidApply.Models
{
    public class Interview
    {
        public int interviewId { get; set; }
        public int applicationId { get; set; }
        [Display(Name = "Interview Date")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? interviewDate { get; set; }
        [Display(Name = "Location")]
        [StringLength(60)]
        public string? location { get; set; }
        [Display(Name = "Memo")]
        [StringLength(150)]
        public string? memo { get; set; }

        public Application? Application { get; set; }
    }
}
