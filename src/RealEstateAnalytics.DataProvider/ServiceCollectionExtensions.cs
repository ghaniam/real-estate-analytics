using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstateAnalytics.Core.Configuration;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.DataProvider.Providers;

namespace RealEstateAnalytics.DataProvider;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PartnerApiConfiguration>(configuration.GetSection(PartnerApiConfiguration.SectionName));

        services.AddHttpClient<IListingProvider, ListingProvider>(client =>
        {
            var baseUrl = configuration[$"{PartnerApiConfiguration.SectionName}:BaseUrl"];
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }
}
