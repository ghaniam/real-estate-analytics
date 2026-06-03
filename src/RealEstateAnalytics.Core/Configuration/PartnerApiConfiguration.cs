namespace RealEstateAnalytics.Core.Configuration;

public class PartnerApiConfiguration
{
    public const string SectionName = "PartnerApi";
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public int PageSize { get; set; } = 25;
}
