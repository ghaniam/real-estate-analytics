using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using RealEstateAnalytics.Core.Configuration;
using RealEstateAnalytics.Core.Interfaces;

namespace RealEstateAnalytics.DataProvider;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PartnerApiConfiguration>(configuration.GetSection(PartnerApiConfiguration.SectionName));

        services.AddHttpClient<IListingProvider, ListingProvider>(client =>
        {
            var baseUrl = configuration[$"{PartnerApiConfiguration.SectionName}:BaseUrl"]
                ?? throw new InvalidOperationException($"Configuration key '{PartnerApiConfiguration.SectionName}:BaseUrl' is required.");
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddResilienceHandler("partner-api", pipeline =>
        {
            pipeline.AddRateLimiter(new SlidingWindowRateLimiter(
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit          = 100,
                    Window               = TimeSpan.FromSeconds(60),
                    SegmentsPerWindow    = 4,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit           = int.MaxValue
                }));
        });

        return services;
    }
}
