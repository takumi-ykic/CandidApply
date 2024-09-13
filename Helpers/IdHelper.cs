namespace CandidApply.Helpers
{
    /// <summary>
    /// Application ID Helper class
    /// </summary>
    public class IdHelper : IIdGenerator
    {
        private readonly Random _random = new Random();

        /// <summary>
        /// Get unique id
        /// </summary>
        /// <param name="ids"></param>
        /// <returns>Application ID</returns>
        public string GetApplicationId(List<string> ids)
        {
            string applicationId;

            // Generate id unitl it is unique
            do
            {
                applicationId = GenerateId();
            } while (ids.Contains(applicationId));

            return applicationId;
        }

        /// <summary>
        /// Generate random id
        /// </summary>
        /// <returns>id</returns>
        public string GenerateId()
        {
            const int length = 19;
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            // Generate id with random 19 chars
            return new string(Enumerable.Repeat(chars, length)
                .Select(x => x[_random.Next(x.Length)])
                .ToArray());
        }
    }

    /// <summary>
    /// Interface Id generator
    /// </summary>
    public interface IIdGenerator
    {
        string GetApplicationId(List<string> ids);
    }
}
