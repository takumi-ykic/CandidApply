using CandidApply.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CandidApply.Helpers
{
    /// <summary>
    /// Filter Hepler class
    /// </summary>
    public class FilterHelper
    {
        /// <summary>
        /// Get status list
        /// </summary>
        /// <param name="statusTable"></param>
        /// <param name="selStatus"></param>
        /// <returns>Status list</returns>
        public static List<SelectListItem> GetStatusList(List<ApplicationStatus> statusTable, int? selStatus)
        {
            var searchStatusList = new List<SelectListItem>();

            // Set status
            foreach(ApplicationStatus status in statusTable)
            {
                searchStatusList.Add(new SelectListItem
                {
                    Value = status.statusId.ToString(),
                    Text = status.statusName,
                    // If status is the same as selected status for search, set true
                    Selected = status.statusId == selStatus
                });
            }

            return searchStatusList;
        }

        /// <summary>
        /// Filtering application with keyword and status
        /// </summary>
        /// <param name="applicationList"></param>
        /// <param name="keyword"></param>
        /// <param name="selStatus"></param>
        /// <returns>Filtered application list</returns>
        public static IQueryable<Application> FilterApplication(IQueryable<Application> applicationList, string? keyword, int? selStatus)
        {
            // Check if searchkeyword is null
            if (!String.IsNullOrEmpty(keyword))
            {
                // Add condition to make the application list based on search keyword
                applicationList = applicationList
                        .Where(a => (a.jobTitle != null && a.jobTitle.Contains(keyword))
                        || (a.company != null && a.company.Contains(keyword))
                        || (a.Interview != null && a.Interview.memo != null && a.Interview.memo.Contains(keyword)));
            }

            // Check if search status is selected
            if (selStatus != 0)
            {
                // Add condition for search status
                applicationList = applicationList
                                  .Where(a => a.status == selStatus);
            }

            return applicationList;
        }
    }
}
