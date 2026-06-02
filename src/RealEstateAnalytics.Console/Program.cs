using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.DataProvider;
using RealEstateAnalytics.Service;

var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddUserSecrets<Program>(optional: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddDataProvider(context.Configuration);
        services.AddServices(context.Configuration);
    })
    .Build();

using var scope = host.Services.CreateScope();
var listingService = scope.ServiceProvider.GetRequiredService<IListingService>();
var results = await listingService.GetAgentListingsOrderedByCountAsync(new RealEstateAnalytics.Core.Models.ListingRequestModel
{
    Area = "Amsterdam",
    //SearchQuery = "Tuin",
    Type = "Koop",
    Take = 5
});

foreach (var agent in results)
{
    Console.WriteLine($"Rank {results.ToList().IndexOf(agent) + 1}: Agent: {agent.AgentName}, Listings Count: {agent.ListingsCount}");
}

Console.WriteLine($"ListingService resolved: {listingService.GetType().FullName}");
