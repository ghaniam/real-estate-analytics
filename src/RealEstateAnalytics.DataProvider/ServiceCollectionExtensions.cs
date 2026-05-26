using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstateAnalytics.Core.Configuration;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.DataProvider.Services;

namespace RealEstateAnalytics.DataProvider;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PartnerApiOptions>(configuration.GetSection(PartnerApiOptions.SectionName));

        services.AddHttpClient<IListingProvider, ListingProvider>(client =>
        {
            var baseUrl = configuration[$"{PartnerApiOptions.SectionName}:BaseUrl"]
                ?? PartnerApiOptions.DefaultBaseUrl;
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }
}
