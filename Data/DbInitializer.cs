using CandidApply.Data;
using CandidApply.Models;

namespace CandidApply.Data
{
    public static class DbInitializer
    {
       public static void Initialize(ApplicationContext context)
        {
            if (context.status.Any())
            {
                return;
            }
            var statuses = new ApplicationStatus[]
            {
                new ApplicationStatus{statusName = "Apply"},
                new ApplicationStatus{statusName = "Interview"},
                new ApplicationStatus{statusName = "Offer"},
                new ApplicationStatus{statusName = "Hired"},
                new ApplicationStatus{statusName = "Rejected"}
            };
            foreach(ApplicationStatus s in statuses)
            {
                context.status.Add(s);
            }
            context.SaveChanges();
        }
    }
}
