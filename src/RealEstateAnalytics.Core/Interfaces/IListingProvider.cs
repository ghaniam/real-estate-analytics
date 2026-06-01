using RealEstateAnalytics.Core.Models;

namespace RealEstateAnalytics.Core.Interfaces;

public interface IListingProvider
{
    Task<PagedResultModel<ResidentialObjectModel>> GetListingAsync(int page = 1);
}
