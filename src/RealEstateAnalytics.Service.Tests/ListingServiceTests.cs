using Microsoft.Extensions.Caching.Memory;
using Moq;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.Core.Models;

namespace RealEstateAnalytics.Service.Tests;

public class ListingServiceTests
{
    private readonly Mock<IListingProvider> _mockListingProvider;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly IListingService _listingService;

    public ListingServiceTests()
    {
        _mockListingProvider = new Mock<IListingProvider>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _listingService = new ListingService(_mockListingProvider.Object, _mockMemoryCache.Object);

        var mockCacheEntry = new Mock<ICacheEntry>();
        mockCacheEntry.SetupGet(e => e.ExpirationTokens).Returns([]);
        mockCacheEntry.SetupGet(e => e.PostEvictionCallbacks).Returns([]);
        mockCacheEntry.SetupProperty(e => e.Value);
        mockCacheEntry.SetupProperty(e => e.AbsoluteExpiration);
        mockCacheEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
        mockCacheEntry.SetupProperty(e => e.SlidingExpiration);
        _mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithOrderedRanks_ReturnsOk()
    {
        var requestModel = new ListingRequestModel { Area = "amsterdam", Attribute = "tuin", Type = "koop", Take = 10 };
        var objectsAgentAId = 1;
        var objectsAgentBId = 2;
        var objectsAgentCId = 3;
        var objectsAgentDId = 4;
        var objectsAgentEId = 5;
        var objectsAgentA = Enumerable.Range(0, 25).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = objectsAgentAId, AgentName = "Agent A" }).ToList();
        var objectsAgentB = Enumerable.Range(0, 15).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = objectsAgentBId, AgentName = "Agent B" }).ToList();
        var objectsAgentC = Enumerable.Range(0, 4).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = objectsAgentCId, AgentName = "Agent C" }).ToList();
        var objectsAgentD = Enumerable.Range(0, 4).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = objectsAgentDId, AgentName = "Agent D" }).ToList();
        var objectsAgentE = Enumerable.Range(0, 2).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = objectsAgentEId, AgentName = "Agent E" }).ToList();
        var page2Items = objectsAgentB.Concat(objectsAgentC).Concat(objectsAgentD).Concat(objectsAgentE).ToList();

        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(p => p == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = objectsAgentA, TotalPages = 2, CurrentPage = 1, TotalCount = objectsAgentA.Count });
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(p => p == 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = page2Items, TotalPages = 2, CurrentPage = 2, TotalCount = page2Items.Count });

        SetupCacheRetrieval();

        var responseModels = (await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None)).ToList();

        Assert.Equal(5, responseModels.Count);
        
        Assert.Equal("Agent A", responseModels.SingleOrDefault(r => r.AgentId == objectsAgentAId)?.AgentName);
        Assert.Equal("Agent B", responseModels.SingleOrDefault(r => r.AgentId == objectsAgentBId)?.AgentName);
        Assert.Equal("Agent C", responseModels.SingleOrDefault(r => r.AgentId == objectsAgentCId)?.AgentName);
        Assert.Equal("Agent D", responseModels.SingleOrDefault(r => r.AgentId == objectsAgentDId)?.AgentName);
        Assert.Equal("Agent E", responseModels.SingleOrDefault(r => r.AgentId == objectsAgentEId)?.AgentName);

        Assert.Equal(25, responseModels.SingleOrDefault(r => r.AgentId == objectsAgentAId)?.ListingsCount);
        Assert.Equal(15, responseModels.SingleOrDefault(r => r.AgentId == objectsAgentBId)?.ListingsCount);
        Assert.Equal(4, responseModels.SingleOrDefault(r => r.AgentId == objectsAgentCId)?.ListingsCount);
        Assert.Equal(4, responseModels.SingleOrDefault(r => r.AgentId == objectsAgentDId)?.ListingsCount);
        Assert.Equal(2, responseModels.SingleOrDefault(r => r.AgentId == objectsAgentEId)?.ListingsCount);

        Assert.Equal(1, responseModels.SingleOrDefault(r => r.AgentId == objectsAgentAId)?.Ranking);
        Assert.Equal(2, responseModels.SingleOrDefault(r => r.AgentId == objectsAgentBId)?.Ranking);
        Assert.Equal(3, responseModels.SingleOrDefault(r => r.AgentId == objectsAgentCId)?.Ranking);
        Assert.Equal(3, responseModels.SingleOrDefault(r => r.AgentId == objectsAgentDId)?.Ranking);
        Assert.Equal(4, responseModels.SingleOrDefault(r => r.AgentId == objectsAgentEId)?.Ranking);

        _mockListingProvider.Verify(p => p.GetListingsPageAsync(
            It.Is<ListingsRequestDto>(r => r.Area == requestModel.Area && r.Attribute == requestModel.Attribute && r.Type == requestModel.Type),
            It.IsAny<int>(),
            It.Is<CancellationToken>(ct => ct == CancellationToken.None)), Times.Exactly(2));
        _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData(-1, 4)]
    [InlineData(0, 4)]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    public async Task GetAgentListingsOrderedByCountAsync_TakeAgents_ReturnsOk(int takeRank, int expectedCount)
    {
        var requestModel = new ListingRequestModel { Area = "amsterdam", Attribute = "tuin", Type = "koop", Take = takeRank };
        var objectsAgentAId = 1;
        var objectsAgentBId = 2;
        var objectsAgentCId = 3;
        var objectsAgentDId = 4;
        var objectsAgentA = Enumerable.Range(0, 10).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = objectsAgentAId, AgentName = "Agent A" }).ToList();
        var objectsAgentB = Enumerable.Range(0, 5).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = objectsAgentBId, AgentName = "Agent B" }).ToList();
        var objectsAgentC = Enumerable.Range(0, 5).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = objectsAgentCId, AgentName = "Agent C" }).ToList();
        var objectsAgentD = Enumerable.Range(0, 2).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = objectsAgentDId, AgentName = "Agent D" }).ToList();
        var pageItems = new List<ResidentialObjectModel>();
        pageItems.AddRange(objectsAgentA);
        pageItems.AddRange(objectsAgentB);
        pageItems.AddRange(objectsAgentC);
        pageItems.AddRange(objectsAgentD);

        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(p => p == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = pageItems, TotalPages = 1, CurrentPage = 1, TotalCount = pageItems.Count });
        
        SetupCacheRetrieval();

        var responseModels = (await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None)).ToList();

        Assert.Equal(expectedCount, responseModels.Count);
        if(takeRank > 0)
        {
            Assert.Contains(responseModels.Select(x => x.Ranking), r => Enumerable.Range(1, takeRank).Contains(r));
        }
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithCachedResults_ReturnsOk()
    {
        var requestModel = new ListingRequestModel { Area = "amsterdam", Type = "koop", Take = 5 };
        var cachedItems = new List<ResidentialObjectModel>
        {
            new() { Id = Guid.NewGuid(), AgentId = 1, AgentName = "Cached Agent" },
        };
        SetupCacheRetrieval(cachedItems);

        var result = await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None);

        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal("Cached Agent", resultList[0].AgentName);
        _mockListingProvider.Verify(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithNoObjectsFromFirstPage_ReturnsOk()
    {
        var requestModel = new ListingRequestModel { Type = "koop", Area = "nowhere", Take = 10 };
        SetupCacheRetrieval();
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = [], TotalPages = 0, CurrentPage = 1, TotalCount = 0 });

        var result = await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None);

        Assert.Empty(result);
        _mockListingProvider.Verify(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockListingProvider.Verify(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page != 1), It.IsAny<CancellationToken>()), Times.Never);
        _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithNoAgentId_ReturnsOk()
    {
        var requestModel = new ListingRequestModel { Type = "koop", Area = "amsterdam", Take = 10 };
        var items = new List<ResidentialObjectModel>
        {
            new() { Id = Guid.NewGuid(), AgentId = null, AgentName = "No ID Agent" },
            new() { Id = Guid.NewGuid(), AgentId = 1, AgentName = "Valid Agent" },
        };
        SetupCacheRetrieval();
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = items, TotalPages = 1, CurrentPage = 1, TotalCount = items.Count });

        var result = await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None);

        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal("Valid Agent", resultList[0].AgentName);
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithNoAgentName_ReturnsOk()
    {
        var requestModel = new ListingRequestModel { Type = "koop", Area = "amsterdam", Take = 10 };
        var items = new List<ResidentialObjectModel>
        {
            new() { Id = Guid.NewGuid(), AgentId = 1, AgentName = null },
        };
        SetupCacheRetrieval();
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = items, TotalPages = 1, CurrentPage = 1, TotalCount = items.Count });

        var result = await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None);

        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Null(resultList[0].AgentName);
        Assert.Equal(1, resultList[0].ListingsCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAgentListingsOrderedByCountAsync_WithDuplicateListing_ReturnsOk(bool hasMoreThanOnePage)
    {
        var requestModel = new ListingRequestModel { Type = "koop", Area = "amsterdam", Take = 10 };
        var residentialObjectModel = new ResidentialObjectModel 
        {
             Id = Guid.NewGuid(), 
             AgentId = 1, 
             AgentName = "Agent A",
             Address = "Some Address",
             PostalCode = "1234 AB",
             City = "Amsterdam",
                
        };
        var items = new List<ResidentialObjectModel>
        {
            residentialObjectModel,
            residentialObjectModel
        };
        SetupCacheRetrieval();
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(p => p == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = items, TotalPages = hasMoreThanOnePage ? 2 : 1, CurrentPage = 1, TotalCount = items.Count });
        if (hasMoreThanOnePage){

        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(p => p != 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = items, TotalPages = hasMoreThanOnePage ? 2 : 1, CurrentPage = 2, TotalCount = items.Count });
        }
        var responseModels = await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None);

        Assert.Single(responseModels);
        var responseModel = responseModels.Single();
        Assert.Equal(1, responseModel.ListingsCount);
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithListingProviderError_ForFirstPageRetrieval_Throws()
    {
        var requestModel = new ListingRequestModel { Type = "koop", Area = "amsterdam", Take = 10 };
        SetupCacheRetrieval();
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page == 1), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unknown error."));

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None));
        Assert.Equal("Unknown error.", exception.Message);
        _mockListingProvider.Verify(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page != 1), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithListingProviderError_ForNextPagesRetrieval_Throws()
    {
        var requestModel = new ListingRequestModel { Type = "koop", Area = "amsterdam", Take = 10 };
        SetupCacheRetrieval();
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel>
            {
                Items = [new() { Id = Guid.NewGuid(), AgentId = 1, AgentName = "Agent A" }],
                TotalPages = 2,
                CurrentPage = 1,
                TotalCount = 2
            });
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page == 2), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unknown error on page 2."));

        var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
            _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None));
        Assert.Equal("Unknown error on page 2.", exception.Message);
        _mockListingProvider.Verify(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockListingProvider.Verify(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(page => page != 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupCacheRetrieval(object? cacheValue = null)
    {
        _mockMemoryCache
            .Setup(m => m.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(cacheValue != null);
    }
}
