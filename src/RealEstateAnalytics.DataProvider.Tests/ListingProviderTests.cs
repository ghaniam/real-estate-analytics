using System.Net;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using RealEstateAnalytics.Core.Configuration;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.Core.Models;
using RealEstateAnalytics.DataProvider.Providers;

namespace RealEstateAnalytics.DataProvider.Tests;

public class ListingProviderTests
{
    private readonly IListingProvider _listingProvider;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

    private static readonly PartnerApiConfiguration Config = new()
    {
        BaseUrl = "http://fake-api.com/",
        ApiKey = "test-key",
        PageSize = 25
    };

    private static readonly ListingRequestModel DefaultRequest = new()
    {
        Type = "koop",
        Area = "amsterdam",
        SearchQuery = "tuin",
        PageNumber = 1
    };

    public ListingProviderTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var options = Options.Create(Config);
        _listingProvider = new ListingProvider(httpClient, options);
    }

    [Fact]
    public async Task GetListingAsync_SuccessResponse_ReturnsNonEmptyItems()
    {
        SetupSendAsync(GetJsonString());

        var result = await _listingProvider.GetListingAsync(DefaultRequest);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        VerifySendAsync(Times.Once());
    }

    [Fact]
    public async Task GetListingAsync_SuccessResponse_MapsItemFieldsCorrectly()
    {
        var json = """
            {
                "Objects": [{
                    "Adres": "Keizersgracht 74-K",
                    "Woonplaats": "Amsterdam",
                    "Postcode": "1015CT",
                    "URL": "http://www.funda.nl/appartement-123/",
                    "MakelaarNaam": "Test Makelaar",
                    "Soort-aanbod": "appartement"
                }],
                "Paging": { "HuidigePagina": 1, "AantalPaginas": 1 },
                "TotaalAantalObjecten": 1
            }
            """;
        SetupSendAsync(json);

        var result = await _listingProvider.GetListingAsync(DefaultRequest);
        var item = result.Items.Single();

        Assert.Equal("Keizersgracht 74-K", item.Address);
        Assert.Equal("Amsterdam", item.City);
        Assert.Equal("1015CT", item.PostalCode);
        Assert.Equal("http://www.funda.nl/appartement-123/", item.ListingUrl);
        Assert.Equal("Test Makelaar", item.AgentName);
        Assert.Equal("appartement", item.ListingType);
        VerifySendAsync(Times.Once());
    }

    [Fact]
    public async Task GetListingAsync_SuccessResponse_MapsPagingCorrectly()
    {
        var json = """
            {
                "Objects": [{ "Adres": "Test", "Woonplaats": "Amsterdam", "Postcode": "1000AA" }],
                "Paging": { "HuidigePagina": 2, "AantalPaginas": 10 },
                "TotaalAantalObjecten": 250
            }
            """;
        SetupSendAsync(json);

        var result = await _listingProvider.GetListingAsync(DefaultRequest);

        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(10, result.TotalPages);
        Assert.Equal(250, result.TotalCount);
        VerifySendAsync(Times.Once());
    }

    [Fact]
    public async Task GetListingAsync_BuildsUrlFromRequestModel()
    {
        SetupSendAsync(GetJsonString());
        var request = new ListingRequestModel { Type = "huur", Area = "rotterdam", SearchQuery = "park", PageNumber = 3 };

        await _listingProvider.GetListingAsync(request);

        VerifySendAsync(Times.Once(), req =>
            req.Method == HttpMethod.Get &&
            req.RequestUri!.ToString().StartsWith(Config.BaseUrl) &&
            req.RequestUri.ToString().Contains(Config.ApiKey) &&
            req.RequestUri.ToString().Contains("type=huur") &&
            req.RequestUri.ToString().Contains("rotterdam/park") &&
            req.RequestUri.ToString().Contains("page=3") &&
            req.RequestUri.ToString().Contains($"pagesize={Config.PageSize}"));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetListingAsync_NonSuccessHttpResponse_ThrowsHttpRequestException(HttpStatusCode statusCode)
    {
        SetupSendAsync(GetJsonString(), statusCode);

        await Assert.ThrowsAsync<HttpRequestException>(() => _listingProvider.GetListingAsync(DefaultRequest));

        VerifySendAsync(Times.Once());
    }

    [Fact]
    public async Task GetListingAsync_EmptyObjectsList_ReturnsEmptyItems()
    {
        var json = """
            { "Objects": [], "Paging": { "HuidigePagina": 1, "AantalPaginas": 0 }, "TotaalAantalObjecten": 0 }
            """;
        SetupSendAsync(json);

        var result = await _listingProvider.GetListingAsync(DefaultRequest);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        VerifySendAsync(Times.Once());
    }

    [Fact]
    public async Task GetListingAsync_NullResponseBody_ReturnsEmptyResult()
    {
        SetupSendAsync("null");

        var result = await _listingProvider.GetListingAsync(DefaultRequest);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.CurrentPage);
        Assert.Equal(0, result.TotalPages);
        Assert.Equal(0, result.TotalCount);
        VerifySendAsync(Times.Once());
    }

    [Fact]
    public async Task GetListingAsync_CancelledToken_ThrowsTaskCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _listingProvider.GetListingAsync(DefaultRequest, cts.Token));

        VerifySendAsync(Times.Once());
    }

    // AI Generated
    private void SetupSendAsync(string jsonString, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = httpStatusCode,
                Content = new StringContent(jsonString, Encoding.UTF8, MediaTypeNames.Application.Json)
            });
    }

    private void VerifySendAsync(Times times, Func<HttpRequestMessage, bool>? requestPredicate = null)
    {
        var requestExpr = requestPredicate is not null
            ? ItExpr.Is<HttpRequestMessage>(req => requestPredicate(req))
            : ItExpr.IsAny<HttpRequestMessage>();

        _mockHttpMessageHandler
            .Protected()
            .Verify("SendAsync", times, requestExpr, ItExpr.IsAny<CancellationToken>());
    }

    private static string GetJsonString(string filename = "partnerapi-sample.json")
    {
        return File.ReadAllText(Path.Combine(".\\Resources", filename));
    }
}
