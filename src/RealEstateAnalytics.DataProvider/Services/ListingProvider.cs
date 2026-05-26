using Microsoft.Extensions.Options;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.Core.Configuration;
using RealEstateAnalytics.Core.Models;
using RealEstateAnalytics.DataProvider.Models;
using System.Net.Http.Json;

namespace RealEstateAnalytics.DataProvider.Services;

public class ListingProvider(HttpClient httpClient, IOptions<PartnerApiOptions> options) : IListingProvider
{
    private readonly PartnerApiOptions _options = options.Value;

    public async Task<PagedResultModel<ResidentialObjectModel>> GetListingAsync(int page = 1)
    {
        var url = $"{_options.BaseUrl}{_options.ApiKey}/?type={_options.SearchType}&zo={_options.SearchZone}&page={page}&pagesize={_options.PageSize}";
        var response = await httpClient.GetFromJsonAsync<PartnerResponseDto>(url);

        if (response is null)
            return new PagedResultModel<ResidentialObjectModel>();

        return new PagedResultModel<ResidentialObjectModel>
        {
            Items = response.Objects.Select(MapToModel),
            CurrentPage = response.Paging.CurrentPage,
            TotalPages = response.Paging.TotalPages,
            TotalCount = response.TotalCount
        };
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
