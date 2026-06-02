namespace RealEstateAnalytics.Core.Models;

public class ResidentialObjectModel
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? ListingUrl { get; set; }
    public int? AgentId { get; set; }
    public string? AgentName { get; set; }
    public string? ListingType { get; set; }
}
