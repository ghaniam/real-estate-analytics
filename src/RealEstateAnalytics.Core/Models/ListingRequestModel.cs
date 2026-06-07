namespace RealEstateAnalytics.Core.Models;

public class ListingRequestModel
{
    public string? Type { get; set; }
    public string? Area { get; set; } 
    public string? Attribute { get; set; }
    // Description: how many top agents to return. 
    // For example, if there are 100 agents, but the user only wants to see the top 10, 
    // then Take will be 10. 
    // If Take is 0 or negative, it will return all agents.
    public int Take { get; set; } = 10;
}
