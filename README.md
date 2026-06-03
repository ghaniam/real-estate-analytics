# real-estate-analytics

## About

Real Estate Analytics is a .NET 9 console application that queries a real estate partner API to identify which agents have the most active listings in a given area. Given a listing type, location, and optional search criteria, the application fetches all matching listings across multiple pages, groups them by agent, and prints a ranked leaderboard of agents ordered by listing count.

### How it works

1. The user is prompted for a listing type (e.g. `koop`, `huur`), an area, an optional search query (e.g. `tuin`, `balkon`), and how many top agents to display.
2. The application pages through the partner API until all matching listings are collected.
3. Listings are grouped by agent and sorted by count in descending order.
4. The top N agents are shown with their rank and listing count.

### Project structure

| Project | Purpose |
|---|---|
| `RealEstateAnalytics.Core` | Shared interfaces, models, and configuration |
| `RealEstateAnalytics.DataProvider` | HTTP client and mapping layer for the partner API |
| `RealEstateAnalytics.Service` | Business logic — aggregation and ranking |
| `RealEstateAnalytics.Console` | Interactive console entry point |
| `RealEstateAnalytics.DataProvider.Tests` | Unit tests for the data provider layer |

## Rationale

### Paging
During testing I observed that the API consistently returns a maximum of 25 objects per response, regardless of the `pagesize` parameter. When `pagesize` exceeds 25, `VolgendeUrl` appears to skip objects, it advances to the next logical page rather than the next physical response, which can result in missing records. Therefore, I set a fixed `pagesize` to 25, as this aligns with the API's actual behaviour, avoids data loss during pagination, and complicated pagination logic.

## AI Generated Code

The following was produced with AI assistance (Claude Code):

### `src/RealEstateAnalytics.DataProvider.Tests/ListingProviderTests.cs`
The test file was AI-assisted, including:
- `SetupSendAsync` — mocking `HttpMessageHandler.SendAsync` via Moq Protected (already marked `// AI Generated` in source)
- `VerifySendAsync` — assertion helper that wraps `Protected().Verify(...)` (already marked `// AI Generated` in source)

### `src/RealEstateAnalytics.DataProvider.Tests/ListingMappingTests.cs`
The test file was AI-assisted, including:
- `MapToModel_ReturnsOk` - Listing properties needed to be asserted

### `src/RealEstateAnalytics.Console/Program.cs`
- User-input prompts (field explanations, defaults, examples)
