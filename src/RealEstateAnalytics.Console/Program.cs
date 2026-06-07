using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RealEstateAnalytics.Console");

Console.WriteLine("=== Real Estate Analytics ===");
Console.WriteLine();

var shouldContinue = true;
do
{
    Console.WriteLine("Listing type (e.g. koop, huur) [default: koop]:");
    var type = Console.ReadLine();

    Console.WriteLine("Area to search in (e.g. amsterdam, rotterdam) [default: nederland]:");
    var area = Console.ReadLine();

    Console.WriteLine("Search query / features (e.g. tuin, garage, balkon) [leave blank for none]:");
    var searchQuery = Console.ReadLine();

    Console.WriteLine();

    ListingRequestModel requestModel = new()
    {
        Type = string.IsNullOrWhiteSpace(type) ? "koop" : type,
        Area = string.IsNullOrWhiteSpace(area) ? null : area,
        Attribute = string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery
    };

    try
    {
        // (Optional) TO-DO: show a loading or wating animation
        Console.WriteLine("Processing your request, please wait...");
        var responseModels = await listingService.GetAgentListingsOrderedByCountAsync(requestModel);
        if (responseModels.Any())
        {
            Console.WriteLine($"Results fetched on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            // Display results in a table format via AI 
            Console.WriteLine($"{"Rank",-6} {"Agent",-40} {"Listings",8}");
            Console.WriteLine(new string('-', 56));

            foreach (var agent in responseModels)
                Console.WriteLine($"{agent.Ranking,-6} {agent.AgentName,-40} {agent.ListingsCount,8}");}
        else
        {
            Console.WriteLine("No results found for this query.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Something went wrong while processing the request.");
    }

    Console.WriteLine();
    Console.WriteLine("Do you want to continue? (Y/N) (continue by default unless stated otherwise)");
    var continueInput = Console.ReadLine()?.Trim();
    shouldContinue = string.IsNullOrEmpty(continueInput) || continueInput.Equals("Y", StringComparison.OrdinalIgnoreCase);
    Console.WriteLine();
}
while (shouldContinue);

Console.WriteLine("Real Estate Analytic has ended.");
