using RealEstateAnalytics.DataProvider.Models;
using RealEstateAnalytics.Core.Interfaces;
using Microsoft.Extensions.Options;
using RealEstateAnalytics.Core.Configuration;
using RealEstateAnalytics.Core.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RealEstateAnalytics.DataProvider;

public class ListingProvider : IListingProvider
{
    private readonly HttpClient _httpClient;
    private readonly PartnerApiConfiguration _options;

    public ListingProvider(HttpClient httpClient, IOptions<PartnerApiConfiguration> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PagedResultModel<ResidentialObjectModel>> GetListingAsync(ListingRequestDto listingRequestModel, CancellationToken ct = default)
    {
        var fullSearchQuery = $"{listingRequestModel.Area?.ToLowerInvariant()}/{listingRequestModel.SearchQuery?.ToLowerInvariant()}";
        var url = $"{_options.BaseUrl}/{_options.ApiKey}/?type={listingRequestModel.Type?.ToLowerInvariant() }&zo=/{fullSearchQuery}/&page={listingRequestModel.PageNumber}&pagesize={_options.PageSize}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        Console.WriteLine($"Requesting URL: {url}");
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        response.EnsureSuccessStatusCode();
        var partnerResponse = JsonSerializer.Deserialize<PartnerResponseDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return partnerResponse is not null ? new PagedResultModel<ResidentialObjectModel>
        {
            Items = partnerResponse.Objects.Select(MapToModel),
            CurrentPage = partnerResponse.Paging.CurrentPage,
            TotalPages = partnerResponse.Paging.TotalPages,
            TotalCount = partnerResponse.TotalCount,
        } : new();
    }

    private static ResidentialObjectModel MapToModel(PartnerResidentialObjectDto source)
    {
        return new ResidentialObjectModel
        {
            Id = source.Id,
            Address = source.Address,
            City = source.City,
            PostalCode = source.PostalCode,
            ListingUrl = source.ListingUrl,
            AgentId = source.AgentId,
            AgentName = source.AgentName,
            ListingType = source.ListingType
        };
    }
}
