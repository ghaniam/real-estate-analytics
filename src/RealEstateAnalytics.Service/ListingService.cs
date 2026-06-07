using Microsoft.Extensions.Caching.Memory;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.Core.Models;

namespace RealEstateAnalytics.Service;

public class ListingService : IListingService
{
    private readonly IListingProvider _listingProvider;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ListingService(IListingProvider listingProvider, IMemoryCache cache)
    {
        _listingProvider = listingProvider;
        _cache = cache;
    }

    public async Task<IEnumerable<AgentListingsResponseModel>> GetAgentListingsOrderedByCountAsync(ListingRequestModel requestModel, CancellationToken ct = default)
    {
        IEnumerable<ResidentialObjectModel> objectModels;

        var cacheKey = $"listing:{requestModel.Type}:{requestModel.Area}:{requestModel.Attribute}";
        if (_cache.TryGetValue(cacheKey, out List<ResidentialObjectModel>? cachedObjectModels))
            objectModels = cachedObjectModels!;
        else
        {   
            var requestDto = new ListingsRequestDto
            {
                Area = requestModel.Area,
                Attribute = requestModel.Attribute,
                Type = requestModel.Type
            };
            objectModels = await GetListingsFromAllPagesAsync(requestDto, ct);

            if (objectModels.Any())
                _cache.Set(cacheKey, objectModels, CacheDuration);
        }

        var orderedAgentListings = MapToAgentListingsModel(objectModels)
            .OrderByDescending(a => a.ListingsCount)
            .ThenBy(a => a.AgentName) // To ensure consistent ordering for agents with the same listing count
            .ToList();

        // Assign ranking based on the order after sorting by ListingsCount
        int rank = 1;
        for (var i = 0; i < orderedAgentListings.Count; i++)
        {
            if (i > 0 && orderedAgentListings[i].ListingsCount == orderedAgentListings[i - 1].ListingsCount)
            {
                orderedAgentListings[i].Ranking = orderedAgentListings[i - 1].Ranking;
            }    
            else
            {
                orderedAgentListings[i].Ranking = rank;
                rank++;
            }

        }
        return orderedAgentListings.Where(a => requestModel.Take <= 0 || a.Ranking <= requestModel.Take);
    }

    private async Task<IEnumerable<ResidentialObjectModel>> GetListingsFromAllPagesAsync(ListingsRequestDto requestDto, CancellationToken ct)
    {
        var pageNumber = 1;
        var objectModels = new List<ResidentialObjectModel>();

        var firstPageResult = await GetListingAsync(requestDto, pageNumber, ct);
        objectModels.AddRange(firstPageResult.Items);
        if (firstPageResult.TotalPages > pageNumber)
        {
            var pageResults = new List<ResidentialObjectModel>();
            await Parallel.ForEachAsync(
                Enumerable.Range(2, firstPageResult.TotalPages - 1),
                new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct },
                async (page, token) =>
                {
                    var result = await GetListingAsync(requestDto, page, ct);
                    lock (pageResults) objectModels.AddRange(result.Items);
                });
        }

        // For the time being I assume Id is the object identifier, 
        // but if that's not the case we can use a combination of other properties 
        // to determine uniqueness
        return objectModels.DistinctBy(o => o.Id);
    }

    private static IEnumerable<AgentListingsResponseModel> MapToAgentListingsModel(IEnumerable<ResidentialObjectModel> objectModels)
    {
        return objectModels
            .Where(o => o.AgentId.HasValue)
            .GroupBy(o => o.AgentId!)
            .Select(g => new AgentListingsResponseModel
            {
                AgentId = g.Key!.Value,
                AgentName = g.FirstOrDefault()?.AgentName,
                ListingsCount = g.Count(),
                Ranking = 0 // Ranking will be assigned later based on the order
            });
    }

    private async Task<PagedResultModel<ResidentialObjectModel>> GetListingAsync(ListingsRequestDto requestDto, int pageNumber, CancellationToken ct) 
        => await _listingProvider.GetListingsPageAsync(requestDto, pageNumber, ct);
}
