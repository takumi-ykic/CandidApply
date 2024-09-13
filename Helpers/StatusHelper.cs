using CandidApply.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CandidApply.Helpers
{
    /// <summary>
    /// Status Helper class
    /// </summary>
    public class StatusHelper
    {
        /// <summary>
        /// Generate status dictionary for one application
        /// </summary>
        /// <param name="allStatus"></param>
        /// <param name="application"></param>
        /// <returns>Status dictionary</returns>
        public static Dictionary<string, SelectList> GenerateStatusDictionary(IQueryable<ApplicationStatus> allStatus,
                                                                            Application application)
        {
            // Get collection for status
            var statusDictionary = new Dictionary<string, SelectList>();
            int selectedStatus = application.status;
            var statusList = PopulatedStatus(allStatus, selectedStatus);
            statusDictionary.Add(application.applicationId, statusList);

            // Return Status Dictionary
            return statusDictionary;
        }

        /// <summary>
        /// Generate status dictionary for each application in list
        /// </summary>
        /// <param name="allStatus"></param>
        /// <param name="applicationList"></param>
        /// <returns>Status dictionary</returns>
        public static Dictionary<string, SelectList> GenerateStatusDictionary(IQueryable<ApplicationStatus> allStatus,
                                                                            IEnumerable<Application> applicationList)
        {
            var statusDictionary = new Dictionary<string, SelectList>();

            // Get collection for status
            foreach (var application in applicationList)
            {
                int selectedStatus = application.status;
                var statusList = PopulatedStatus(allStatus, selectedStatus);
                statusDictionary.Add(application.applicationId, statusList);
            }

            // Return Status Dictionary
            return statusDictionary;
        }

        /// <summary>
        /// Populate application status in Select List
        /// </summary>
        /// <param name="selectedStatus> Select status
        /// <return> SelectList
        private static SelectList PopulatedStatus(IQueryable<ApplicationStatus> allStatus, int selectedStatus)
        {
            // Get all application status, and make it list
            var statusList = allStatus
                             .Select(s => new SelectListItem
                             {
                                 Value = s.statusId.ToString(),
                                 Text = s.statusName
                             }).ToList();

            // Create SelectList to populate with selected status
            SelectList selectList = new SelectList(statusList, "Value", "Text", selectedStatus);
            //return Select List
            return selectList;
        }
    }
}
