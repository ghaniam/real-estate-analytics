using RealEstateAnalytics.Core.Models;

namespace RealEstateAnalytics.Api.DataProvider;

public interface IListingProvider
{
    Task<PagedResultModel<ResidentialObjectModel>> GetPropertiesAsync(int page = 1);
}
