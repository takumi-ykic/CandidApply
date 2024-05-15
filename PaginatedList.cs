using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Diagnostics.Eventing.Reader;

namespace CandidApply
{
    public class PaginatedList<T> : List<T>
    {
        public int pageIndex { get; private set; }
        public int totalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int _pageIndex, int pageSize)
        {
            pageIndex = _pageIndex;
            totalPages = (int)Math.Ceiling(count / (double)pageSize);

            this.AddRange(items);
        }

        public bool hasPreviousPage => pageIndex > 1;
        public bool hasNextPage => pageIndex < totalPages;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int _pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((_pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedList<T>(items, count, _pageIndex, pageSize);
        }
    }

    
}
