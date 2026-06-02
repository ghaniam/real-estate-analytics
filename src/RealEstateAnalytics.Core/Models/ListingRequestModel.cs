namespace RealEstateAnalytics.Core.Models;

public class ListingRequestModel
{
    public string? Type { get; set; }
    public string? Area { get; set; }
    public string? SearchQuery { get; set; }
    public int PageNumber { get; set; }
}
