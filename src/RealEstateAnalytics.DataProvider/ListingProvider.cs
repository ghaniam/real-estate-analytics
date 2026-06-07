using Microsoft.Extensions.Options;
using RealEstateAnalytics.Core.Configuration;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.Core.Models;
using RealEstateAnalytics.DataProvider.Mappings;
using RealEstateAnalytics.DataProvider.Models;
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

    public async Task<PagedResultModel<ResidentialObjectModel>> GetListingsPageAsync(ListingsRequestDto listingsRequestModel, int pageNumber, CancellationToken ct = default)
    {
        var url = $"{_options.BaseUrl}/{_options.ApiKey}/?{BuildQueryParams(listingsRequestModel, pageNumber)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();
        var partnerResponse = JsonSerializer.Deserialize<PartnerResponseDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return partnerResponse!.MapToModel();
    }

    private string BuildQueryParams(ListingsRequestDto listingRequestModel, int pageNumber)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(listingRequestModel.Type))
            queryParams.Add($"type={listingRequestModel.Type.ToLowerInvariant()}");
        if (!string.IsNullOrWhiteSpace(listingRequestModel.Area) || !string.IsNullOrWhiteSpace(listingRequestModel.Attribute))
        {
            var searchQueryParams = new List<string>();
            if(!string.IsNullOrEmpty(listingRequestModel.Area))
                searchQueryParams.Add(listingRequestModel.Area.ToLowerInvariant());
            if(!string.IsNullOrEmpty(listingRequestModel.Attribute))
                searchQueryParams.Add(listingRequestModel.Attribute.ToLowerInvariant());
            if (searchQueryParams.Any())
            {
                var searchQuery = string.Join("/", searchQueryParams);
                queryParams.Add($"zo=/{searchQuery}/");
            }
        }
        queryParams.Add($"page={pageNumber}");
        queryParams.Add($"pagesize={_options.PageSize}");
        return string.Join("&", queryParams);

    }
}
