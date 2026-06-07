namespace RealEstateAnalytics.Core.Models;

public class AgentListingsResponseModel
{
    public int? AgentId { get; set; }
    public string? AgentName { get; set; }
    public int ListingsCount { get; set; }
}
