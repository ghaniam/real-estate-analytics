using Microsoft.AspNetCore.Mvc;

namespace RealEstateAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingController() : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProperties()
    {
        return Ok();
    }
}
