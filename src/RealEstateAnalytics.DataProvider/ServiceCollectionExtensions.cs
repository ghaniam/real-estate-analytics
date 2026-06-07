using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r =>
                        r.StatusCode == HttpStatusCode.TooManyRequests ||
                        r.StatusCode == HttpStatusCode.Unauthorized) // This seems to be the error it returned when there's too many requests
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    Console.WriteLine($"[Retry] Attempt {args.AttemptNumber + 1}, delay {args.RetryDelay.TotalSeconds}s, reason: {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}");
                    return ValueTask.CompletedTask;
                }
            });

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
