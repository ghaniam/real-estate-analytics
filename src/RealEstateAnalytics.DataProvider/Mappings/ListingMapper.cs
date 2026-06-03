using RealEstateAnalytics.Core.Models;
using RealEstateAnalytics.DataProvider.Models;

namespace RealEstateAnalytics.DataProvider.Mappings
{
    public static class ListingMapper
    {
        public static PagedResultModel<ResidentialObjectModel> MapToModel(this PartnerResponseDto source)
        {
            return new PagedResultModel<ResidentialObjectModel>
            {
                Items = source.Objects.Select(MapToModel),
                CurrentPage = source.Paging.CurrentPage,
                TotalPages = source.Paging.TotalPages,
                TotalCount = source.TotalCount,
            };
        }

        public static ResidentialObjectModel MapToModel(this PartnerResidentialObjectDto source)
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
}
