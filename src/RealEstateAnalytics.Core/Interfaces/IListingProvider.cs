using RealEstateAnalytics.Core.Models;

namespace RealEstateAnalytics.Core.Interfaces;

public interface IListingProvider
{
    Task<PagedResultModel<ResidentialObjectModel>> GetListingsPageAsync(ListingsRequestDto listingsRequestModel, int pageNumber, CancellationToken ct = default);
}
