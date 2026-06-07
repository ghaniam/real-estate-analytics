using RealEstateAnalytics.DataProvider.Mappings;
using RealEstateAnalytics.DataProvider.Models;

namespace RealEstateAnalytics.DataProvider.Tests;

public class ListingMapperTests
{
    [Fact]
    public void MapToModel_ReturnsOk()
    {
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
        var actualItem = result.Items.Single();
        Assert.Equal(expectedItem.Id, actualItem.Id);
        Assert.Equal(expectedItem.Address, actualItem.Address);
        Assert.Equal(expectedItem.City, actualItem.City);
        Assert.Equal(expectedItem.PostalCode, actualItem.PostalCode);
        Assert.Equal(expectedItem.ListingUrl, actualItem.ListingUrl);
        Assert.Equal(expectedItem.AgentId, actualItem.AgentId);
        Assert.Equal(expectedItem.AgentName, actualItem.AgentName);
        Assert.Equal(expectedItem.ListingType, actualItem.ListingType);
    }
}
