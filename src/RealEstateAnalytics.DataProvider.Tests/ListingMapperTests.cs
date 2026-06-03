using RealEstateAnalytics.Core.Models;
using RealEstateAnalytics.DataProvider.Mappings;
using RealEstateAnalytics.DataProvider.Models;

namespace RealEstateAnalytics.DataProvider.Tests;

public class ListingMapperTests
{
    [Fact]
    public void MapToModel_ReturnsOk()
    {
        var request = new ListingRequestDto
        {
            Type = "koop",
            Area = "amsterdam",
            SearchQuery = "tuin",
            PageNumber = 1
        };
        PartnerResponseDto dto = new()
        {
            Objects =
        [
            new PartnerResidentialObjectDto
            {
                Id = new Guid("b999a53f-57ea-4e23-98b6-bbb70027251d"),
                Address = "Keizersgracht 74-K",
                City = "Amsterdam",
                PostalCode = "1015CT",
                ListingUrl = "http://www.funda.nl/appartement-123/",
                AgentId = 123,
                AgentName = "Test Makelaar",
                ListingType = "appartement"
            }
        ],
            Paging = new PartnerPagingDto
            {
                TotalPages = 39,
                CurrentPage = 2,
            },
            TotalCount = 66
        };

        var result = dto.MapToModel();

        Assert.Equal(dto.Paging.CurrentPage, result.CurrentPage);
        Assert.Equal(dto.Paging.TotalPages, result.TotalPages);
        Assert.Equal(dto.TotalCount, result.TotalCount);

        Assert.Equal(dto.Objects.Count, result.Items.Count());
        var expectedItem = dto.Objects.Single();
        var item = result.Items.Single();
        Assert.Equal(expectedItem.Id, item.Id);
        Assert.Equal(expectedItem.Address, item.Address);
        Assert.Equal(expectedItem.City, item.City);
        Assert.Equal(expectedItem.PostalCode, item.PostalCode);
        Assert.Equal(expectedItem.ListingUrl, item.ListingUrl);
        Assert.Equal(expectedItem.AgentId, item.AgentId);
        Assert.Equal(expectedItem.AgentName, item.AgentName);
        Assert.Equal(expectedItem.ListingType, item.ListingType);
    }
}
