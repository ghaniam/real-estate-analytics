namespace RealEstateAnalytics.Core.Configuration;

public class PartnerApiOptions
{
    public const string SectionName = "ExternalApi";

    public string BaseUrl { get; set; } = "http://partnerapi.funda.nl/feeds/Aanbod.svc/json/";
    public string ApiKey { get; set; } = string.Empty;
    public string SearchType { get; set; } = "koop";
    public string SearchZone { get; set; } = "/amsterdam/";
    public int PageSize { get; set; } = 25;
}
