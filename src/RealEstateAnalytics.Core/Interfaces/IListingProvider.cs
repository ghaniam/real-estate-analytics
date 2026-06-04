using RealEstateAnalytics.Core.Models;

namespace RealEstateAnalytics.Core.Interfaces;

public interface IListingProvider
{
    Task<PagedResultModel<ResidentialObjectModel>> GetListingAsync(ListingRequestDto listingRequestModel, int pageNumber, CancellationToken ct = default);
}
