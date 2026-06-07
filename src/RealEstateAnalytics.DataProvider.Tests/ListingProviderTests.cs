using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using RealEstateAnalytics.Core.Configuration;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.Core.Models;
using RealEstateAnalytics.DataProvider.Models;

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

    public ListingProviderTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var options = Options.Create(Config);
        _listingProvider = new ListingProvider(httpClient, options);
    }

    [Fact]
    public async Task GetListingAsync_ReturnsOk()
    {
        var request = new ListingsRequestDto
        {
            Type = "koop",
            Area = "amsterdam",
            Attribute = "tuin"
        };
        const int pageNumber = 1;
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
        SetupSendAsync(JsonSerializer.Serialize(dto));

        var result = await _listingProvider.GetListingsPageAsync(request, pageNumber);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        VerifySendAsync(Times.Once(), req =>
            req.Method == HttpMethod.Get &&
            req.RequestUri!.ToString().StartsWith(Config.BaseUrl!) &&
            req.RequestUri.ToString().Contains(Config.ApiKey!) &&
            req.RequestUri.ToString().Contains($"type={request.Type}") &&
            req.RequestUri.ToString().Contains($"zo=/{request.Area}/{request.Attribute}/") &&
            req.RequestUri.ToString().Contains($"page={pageNumber}") &&
            req.RequestUri.ToString().Contains($"pagesize={Config.PageSize}"));
    }

    [Fact]
    public async Task GetListingAsync_WithEmptyObjects_ReturnsOk()
    {
        var request = new ListingsRequestDto
        {
            Type = "koop",
            Area = "Non-existing Area"
        };
        var dto = new PartnerResponseDto()
        {
            Objects = [],
            Paging = new PartnerPagingDto
            {
                TotalPages = 0,
                CurrentPage = 1,
            },
            TotalCount = 0
        };
        SetupSendAsync(JsonSerializer.Serialize(dto));

        var result = await _listingProvider.GetListingsPageAsync(request, pageNumber: 1);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(dto.TotalCount, result.TotalCount);
        Assert.Equal(dto.Paging.TotalPages, result.TotalPages);
        Assert.Equal(dto.Paging.CurrentPage, result.CurrentPage);
        VerifySendAsync(Times.Once());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task GetListingAsync_WithNonSuccessfulHttpResponse_Throws(HttpStatusCode statusCode)
    {
        var request = new ListingsRequestDto
        {
            Type = "koop",
            Area = "amsterdam",
            Attribute = "tuin"
        };
        SetupSendAsync(null, statusCode);

        await Assert.ThrowsAsync<HttpRequestException>(() => _listingProvider.GetListingsPageAsync(request, pageNumber: 1));

        VerifySendAsync(Times.Once());
    }

    [Fact]
    public async Task GetListingAsync_WithHttpClientException_Throws()
    {
        var request = new ListingsRequestDto
        {
            Type = "koop",
            Area = "amsterdam",
            Attribute = "tuin"
        };
        var exception = new Exception("Unknown Error.");
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        var thrown = await Assert.ThrowsAnyAsync<Exception>(() => _listingProvider.GetListingsPageAsync(request, pageNumber: 1));

        Assert.IsType(exception.GetType(), thrown);
        Assert.Equal(exception.Message, thrown.Message);
        VerifySendAsync(Times.Once());
    }

    private void SetupSendAsync(string? jsonString, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = httpStatusCode,
                Content = !string.IsNullOrEmpty(jsonString) ? new StringContent(jsonString, Encoding.UTF8, MediaTypeNames.Application.Json) : default
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
}
