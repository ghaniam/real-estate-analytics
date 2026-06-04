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
    public async Task<IEnumerable<AgentListingsModel>> GetAgentListingsOrderedByCountAsync(ListingRequestModel requestModel, CancellationToken ct = default)
    {
        var cacheKey = $"listing:{requestModel.Type}:{requestModel.Area}:{requestModel.Attribute}";
        if (_cache.TryGetValue(cacheKey, out List<ResidentialObjectModel>? cachedObjectModels))
            return MapToAgentListingsModel(cachedObjectModels!, requestModel.Take);

        var pageNumber = 1;
        var requestDto = new ListingRequestDto
        {
            Area = requestModel.Area,
            Attribute = requestModel.Attribute,
            Type = requestModel.Type
        };
        var objectModels = new List<ResidentialObjectModel>();
        PagedResultModel<ResidentialObjectModel>? result;
        do
        {
            result = await GetListingAsync(requestDto, pageNumber++, ct);
            if (result is not null)
                objectModels.AddRange(result.Items);
        }
        while (result is not null && result.CurrentPage < result.TotalPages);

        if (objectModels.Count != 0)
            _cache.Set(cacheKey, objectModels, CacheDuration);

        return MapToAgentListingsModel(objectModels, requestModel.Take);
    }

    private static IEnumerable<AgentListingsModel> MapToAgentListingsModel(List<ResidentialObjectModel> objectModels, int take)
    {
        return objectModels
            .Where(x => x.AgentId.HasValue)
            .GroupBy(o => o.AgentId!)
            .Select(g => new AgentListingsModel
            {
                AgentName = g.FirstOrDefault()?.AgentName,
                ListingsCount = g.Count()
            })
            .OrderByDescending(m => m.ListingsCount)
            .Take(take);
    }

    private async Task<PagedResultModel<ResidentialObjectModel>?> GetListingAsync(ListingRequestDto requestDto, int pageNumber, CancellationToken ct)
    {
        PagedResultModel<ResidentialObjectModel>? result = null;
        const int maxRetries = 3;
        const int retryDelayMs = 30000;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                result = await _listingProvider.GetListingAsync(requestDto, pageNumber, ct);
                break;
            }
            catch (HttpRequestException ex)
                when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries) throw;
                Console.WriteLine($"Wait for {retryDelayMs} ms for {attempt} times.");
                await Task.Delay(retryDelayMs, ct);
            }
        }

        return result;
    }

// The rate limiter adds value when:
// Requests are fired concurrently (e.g. parallel paging, multiple users hitting an API)
// You want to prevent the 429 rather than recover from it
// The API has strict quotas where hitting the limit has consequences beyond a single rejected request (e.g. temporary bans)
}
