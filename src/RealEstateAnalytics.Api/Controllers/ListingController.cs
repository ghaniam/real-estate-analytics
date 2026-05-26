using Microsoft.AspNetCore.Mvc;
using RealEstateAnalytics.Api.DataProvider;

namespace RealEstateAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingController(IListingProvider listingProvider) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProperties([FromQuery] int page = 1)
    {
        var result = await listingProvider.GetPropertiesAsync(page);
        return Ok(result);
    }
}
