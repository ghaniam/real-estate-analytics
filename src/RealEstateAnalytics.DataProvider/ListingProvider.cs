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

    public async Task<PagedResultModel<ResidentialObjectModel>?> GetListingAsync(ListingRequestDto listingRequestModel, CancellationToken ct = default)
    {
        var url = $"{_options.BaseUrl}/{_options.ApiKey}/?{BuildQueryParams(listingRequestModel)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        Console.WriteLine($"Requesting URL: {url}");
        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();
        var partnerResponse = JsonSerializer.Deserialize<PartnerResponseDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return partnerResponse?.MapToModel();
    }

    private string BuildQueryParams(ListingRequestDto listingRequestModel)
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
                var searchQuery = string.Join("/", queryParams);
                queryParams.Add($"zo=/{searchQuery}/");
            }
        }
        queryParams.Add($"page={listingRequestModel.PageNumber}");
        queryParams.Add($"pagesize={_options.PageSize}");
        return string.Join("&", queryParams);

    }
}
