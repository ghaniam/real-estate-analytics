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

    // TO-DO: Implement unit tests
    public async Task<IEnumerable<AgentListingsModel>> GetAgentListingsOrderedByCountAsync(ListingRequestModel requestModel, CancellationToken ct = default)
    {
        var cacheKey = $"listing:{requestModel.Type}:{requestModel.Area}:{requestModel.Attribute}";
        if (_cache.TryGetValue(cacheKey, out List<ResidentialObjectModel>? cachedObjectModels))
            return MapToAgentListingsModel(cachedObjectModels!, requestModel.Take);

        var requestDto = new ListingRequestDto
        {
            Area = requestModel.Area,
            Attribute = requestModel.Attribute,
            Type = requestModel.Type
        };
        var objectModels = await GetListingsFromAllPagesAsync(requestDto, ct);

        if (objectModels.Any())
            _cache.Set(cacheKey, objectModels, CacheDuration);

        return MapToAgentListingsModel(objectModels, requestModel.Take);
    }

    private async Task<IEnumerable<ResidentialObjectModel>> GetListingsFromAllPagesAsync(ListingRequestDto requestDto, CancellationToken ct)
    {
        var pageNumber = 1;
        var objectModels = new List<ResidentialObjectModel>();

        var firstPageResult = await GetListingAsync(requestDto, pageNumber, ct);
        objectModels.AddRange(firstPageResult.Items);
        if(firstPageResult.TotalPages <= pageNumber) return objectModels;
        
        var pageResults = new List<ResidentialObjectModel>();
        await Parallel.ForEachAsync(
            Enumerable.Range(2, firstPageResult.TotalPages - 1),
            new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct },
            async (page, token) =>
            {
                var result = await GetListingAsync(requestDto, pageNumber, ct);
                lock (pageResults) objectModels.AddRange(result.Items);
            });

        return objectModels.Distinct();
    }

    private static IEnumerable<AgentListingsModel> MapToAgentListingsModel(IEnumerable<ResidentialObjectModel> objectModels, int take)
    {
        return objectModels
            .Where(o => o.AgentId.HasValue)
            .GroupBy(o => o.AgentId!)
            .Select(g => new AgentListingsModel
            {
                AgentName = g.FirstOrDefault()?.AgentName,
                ListingsCount = g.Count()
            })
            .OrderByDescending(m => m.ListingsCount)
            .Take(take);
    }

    private async Task<PagedResultModel<ResidentialObjectModel>> GetListingAsync(ListingRequestDto requestDto, int pageNumber, CancellationToken ct)
    {
        return await _listingProvider.GetListingAsync(requestDto, pageNumber, ct);
    }
}
