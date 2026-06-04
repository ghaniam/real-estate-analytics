using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.Core.Models;

namespace RealEstateAnalytics.Service;

public class ListingService : IListingService
{
    private readonly IListingProvider _listingProvider;
    public ListingService(IListingProvider listingProvider)
    {
        _listingProvider = listingProvider;
    }
    public async Task<IEnumerable<AgentListingsModel>> GetAgentListingsOrderedByCountAsync(ListingRequestModel requestModel, CancellationToken ct = default)
    {
        var requestDto = new ListingRequestDto
        {
            Area = requestModel.Area,
            Attribute = requestModel.SearchQuery,
            PageNumber = 1, 
            Type = requestModel.Type
        };
        var objectModels = new List<ResidentialObjectModel>();
        bool isLastPage = false;
        while (!isLastPage)
        {
            // needs throttle to avoid hitting API rate limits, adjust as necessary
            // needs cache to avoid redundant API calls for same queries, implement as needed
            var result = await _listingProvider.GetListingAsync(requestDto, ct);
            objectModels.AddRange(result.Items);
            if (result.CurrentPage < result.TotalPages)
            {
                requestDto.PageNumber++;
            }
            else
            {
                isLastPage = true;
            }
        }

        var agentDictionary = objectModels
            .Where(x => x.AgentId.HasValue && !string.IsNullOrEmpty(x.AgentName))
            .GroupBy(o => o.AgentId.Value)
            .ToDictionary(o => o.Key, o => o.First().AgentName);

        return objectModels
            .Where(x => x.AgentId.HasValue && !string.IsNullOrEmpty(x.AgentName))
            .GroupBy(o => o.AgentId.Value)
            .Select(g => new AgentListingsModel
            {
                AgentName = agentDictionary[g.Key],
                ListingsCount = g.Count()
            })
            .OrderByDescending(m => m.ListingsCount)
            .Take(requestModel.Take)
            .ToList();
    }
}
