using Microsoft.Extensions.Options;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.Core.Configuration;
using RealEstateAnalytics.Core.Models;
using RealEstateAnalytics.DataProvider.Models;
using System.Net.Http.Json;

namespace RealEstateAnalytics.DataProvider.Providers;

public class ListingProvider(HttpClient httpClient, IOptions<PartnerApiConfiguration> options) : IListingProvider
{
    private readonly PartnerApiConfiguration _options = options.Value;

    public async Task<PagedResultModel<ResidentialObjectModel>> GetListingAsync(ListingRequestModel listingRequestModel, CancellationToken ct = default)
    {
        var fullSearchQuery = $"{listingRequestModel.Area}/{listingRequestModel.SearchQuery}";
        var url = $"{_options.BaseUrl}{_options.ApiKey}/?type={listingRequestModel.Type}&zo={fullSearchQuery}&page={listingRequestModel.PageNumber}&pagesize={_options.PageSize}";
        var response = await httpClient.GetFromJsonAsync<PartnerResponseDto>(url, ct);
        return response is not null ? new PagedResultModel<ResidentialObjectModel>
        {
            Items = response.Objects.Select(MapToModel),
            CurrentPage = response.Paging.CurrentPage,
            TotalPages = response.Paging.TotalPages,
            TotalCount = response.TotalCount
        } : new();
    }

    private static ResidentialObjectModel MapToModel(PartnerResidentialObjectDto source)
    {
        return new ResidentialObjectModel
        {
            Address = source.Address,
            City = source.City,
            PostalCode = source.PostalCode,
            ListingUrl = source.ListingUrl,
            AgentName = source.AgentName,
            ListingType = source.ListingType
        };
    }
}
