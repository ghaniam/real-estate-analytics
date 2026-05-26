namespace RealEstateAnalytics.Core.Models;

public class PagedResultModel<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
