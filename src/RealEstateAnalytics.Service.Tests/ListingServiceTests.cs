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
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithOrderedRanks_ReturnsOk()
    {
        var requestModel = new ListingRequestModel { Area = "amsterdam", Attribute = "tuin", Type = "koop", Take = 10 };

        var objectsAgentX = Enumerable.Range(0, 25).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = 1, AgentName = "Agent X" }).ToList();
        var objectsAgentY = Enumerable.Range(0, 15).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = 2, AgentName = "Agent Y" }).ToList();
        var objectsAgentZ = Enumerable.Range(0, 10).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = 3, AgentName = "Agent Z" }).ToList();
        var page2Items = objectsAgentY.Concat(objectsAgentZ).ToList();

        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(p => p == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = objectsAgentX, TotalPages = 2, CurrentPage = 1, TotalCount = 50 });
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(p => p == 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = page2Items, TotalPages = 2, CurrentPage = 2, TotalCount = 50 });

        SetupCacheMiss();
        SetupCacheSet();

        var responseModels = (await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None)).ToList();

        Assert.Equal(3, responseModels.Count);
        Assert.Equal("Agent X", responseModels[0].AgentName);
        Assert.Equal(25, responseModels[0].ListingsCount);
        Assert.Equal("Agent Y", responseModels[1].AgentName);
        Assert.Equal(15, responseModels[1].ListingsCount);
        Assert.Equal("Agent Z", responseModels[2].AgentName);
        Assert.Equal(10, responseModels[2].ListingsCount);
        
        _mockListingProvider.Verify(p => p.GetListingsPageAsync(
            It.Is<ListingsRequestDto>(r => r.Area == requestModel.Area && r.Attribute == requestModel.Attribute && r.Type == requestModel.Type),
            It.IsAny<int>(),
            It.Is<CancellationToken>(ct => ct == CancellationToken.None)), Times.Exactly(2));
        _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once);
    }

    // GetAgentListingsOrderedByCountAsync_WithEqualRanks_ReturnsOk
    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithEqualRanks_ReturnsOk()
    {
        var requestModel = new ListingRequestModel { Area = "amsterdam", Attribute = "tuin", Type = "koop", Take = 10 };

        var objectsAgentX = Enumerable.Range(0, 10).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = 1, AgentName = "Agent X" }).ToList();
        var objectsAgentY = Enumerable.Range(0, 10).Select(_ => new ResidentialObjectModel { Id = Guid.NewGuid(), AgentId = 2, AgentName = "Agent Y" }).ToList();
        var pageItems = objectsAgentX.Concat(objectsAgentY).ToList();

        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.Is<int>(p => p == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel> { Items = pageItems, TotalPages = 1, CurrentPage = 1, TotalCount = 50 });

        SetupCacheMiss();
        SetupCacheSet();

        var responseModels = (await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None)).ToList();

        Assert.Equal(3, responseModels.Count);
        Assert.Equal("Agent X", responseModels[0].AgentName);
        Assert.Equal(25, responseModels[0].ListingsCount);
        Assert.Equal("Agent Y", responseModels[1].AgentName);
        Assert.Equal(15, responseModels[1].ListingsCount);
        Assert.Equal("Agent Z", responseModels[2].AgentName);
        Assert.Equal(10, responseModels[2].ListingsCount);
        
        _mockListingProvider.Verify(p => p.GetListingsPageAsync(
            It.Is<ListingsRequestDto>(r => r.Area == requestModel.Area && r.Attribute == requestModel.Attribute && r.Type == requestModel.Type),
            It.IsAny<int>(),
            It.Is<CancellationToken>(ct => ct == CancellationToken.None)), Times.Exactly(2));
        _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithCachedResults_ReturnsOk()
    {
        var requestModel = new ListingRequestModel { Area = "amsterdam", Type = "koop", Take = 5 };
        var cachedItems = new List<ResidentialObjectModel>
        {
            new() { Id = Guid.NewGuid(), AgentId = 1, AgentName = "Cached Agent" },
        };
        SetupCacheHit(cachedItems);

        var result = await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None);

        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal("Cached Agent", resultList[0].AgentName);
        _mockListingProvider.Verify(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithNoObjectsFromFirstPage_ReturnsOk()
    {
        var requestModel = new ListingRequestModel { Type = "koop", Area = "nowhere", Take = 10 };
        SetupCacheMiss();
        SetupListingProvider(new PagedResultModel<ResidentialObjectModel> { Items = [], TotalPages = 0, CurrentPage = 1, TotalCount = 0 });

        var result = await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None);

        Assert.Empty(result);
        _mockListingProvider.Verify(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
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
        SetupCacheMiss();
        SetupCacheSet();
        SetupListingProvider(new PagedResultModel<ResidentialObjectModel> { Items = items, TotalPages = 1, CurrentPage = 1, TotalCount = items.Count });

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
        SetupCacheMiss();
        SetupCacheSet();
        SetupListingProvider(new PagedResultModel<ResidentialObjectModel> { Items = items, TotalPages = 1, CurrentPage = 1, TotalCount = items.Count });

        var result = await _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None);

        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Null(resultList[0].AgentName);
        Assert.Equal(1, resultList[0].ListingsCount);
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithListingProviderError_ForFirstPageRetrieval_Throws()
    {
        var requestModel = new ListingRequestModel { Type = "koop", Area = "amsterdam", Take = 10 };
        SetupCacheMiss();
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable."));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None));
    }

    [Fact]
    public async Task GetAgentListingsOrderedByCountAsync_WithListingProviderError_ForNextPagesRetrieval_Throws()
    {
        var requestModel = new ListingRequestModel { Type = "koop", Area = "amsterdam", Take = 10 };
        SetupCacheMiss();
        _mockListingProvider
            .SetupSequence(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResultModel<ResidentialObjectModel>
            {
                Items = [new() { Id = Guid.NewGuid(), AgentId = 1, AgentName = "Agent A" }],
                TotalPages = 2,
                CurrentPage = 1,
                TotalCount = 2
            })
            .ThrowsAsync(new HttpRequestException("Service unavailable on page 2."));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _listingService.GetAgentListingsOrderedByCountAsync(requestModel, CancellationToken.None));
    }

    private void SetupCacheMiss()
    {
        object? cacheValue = null;
        _mockMemoryCache
            .Setup(m => m.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);
    }

    private void SetupCacheHit(object value)
    {
        object? cacheValue = value;
        _mockMemoryCache
            .Setup(m => m.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);
    }

    private void SetupCacheSet()
    {
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

    private void SetupListingProvider(PagedResultModel<ResidentialObjectModel> result)
    {
        _mockListingProvider
            .Setup(p => p.GetListingsPageAsync(It.IsAny<ListingsRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }
}
