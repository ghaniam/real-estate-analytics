using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RealEstateAnalytics.Core.Interfaces;
using RealEstateAnalytics.Core.Models;
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

Console.WriteLine("=== Real Estate Analytics ===");
Console.WriteLine();

do
{
    Console.WriteLine("Listing type (e.g. koop, huur) [default: koop]:");
    var type = Console.ReadLine();

    Console.WriteLine("Area to search in (e.g. amsterdam, rotterdam) [default: nederland]:");
    var area = Console.ReadLine();

    Console.WriteLine("Search query / features (e.g. tuin, garage, balkon) [leave blank for none]:");
    var searchQuery = Console.ReadLine();

    Console.WriteLine("How many top agents to show (Must be number. For example: 10):");
    var takeInput = Console.ReadLine();
    var take = int.TryParse(takeInput, out var parsed) ? parsed : 10;

    Console.WriteLine();

    ListingRequestModel requestModel = new()
    {
        Type = string.IsNullOrWhiteSpace(type) ? "koop" : type,
        Area = string.IsNullOrWhiteSpace(area) ? "nederland" : area,
        SearchQuery = string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery,
        Take = take
    };

    try
    {
        var results = await listingService.GetAgentListingsOrderedByCountAsync(requestModel);
        var resultList = results.ToList();
        if (resultList.Count > 0)
        {
            foreach (var agent in resultList)
                Console.WriteLine($"Rank {resultList.IndexOf(agent) + 1}: Agent: {agent.AgentName}, Listings Count: {agent.ListingsCount}");
        }
        else
        {
            Console.WriteLine("No results found for this query.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Something went wrong: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("Do you want to continue? (Y/N)");
}
while (Console.ReadLine()?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) == true);

Console.WriteLine("Real Estate Analytic has ended.");
