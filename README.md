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

### API call efficiency
To avoid the API from rejecting the request as well as to achieve the best performance, a number of apporaches are taken:
- I started with sending the API requests in a sequential order, but this is not the best performance as it goes one by one. If the query targets large amount of returned data this is not the best approach. This apporach is not taken.
- Tackling the API request limit (100 requests/minute), a reactive retrial mechanism is implemented to handle error that resulted in calling more than 100 requests per minute. This apporach is maintained for now at 30 seconds after trying 5/10/60 seconds.
- Sending API requests in parallel. This performs faster, but also faster to reach the API limit so it is efficient but introduce a new issue.
- Throttling API calls per 100 requests. Once 100 requests are sent, the next requests are delayed so the Partner API can have a breathing room

## Use of AI

I used Claude Code (Anthropic) as a coding assistant during this exercise. Here is an honest breakdown of where I leaned on it, what it helped with, and where I took over.

### `src/RealEstateAnalytics.DataProvider.Tests/ListingProviderTests.cs`
**How:** AI took care of the test setup and the two helper methods.
**Why:** Getting `HttpMessageHandler` mocking to work with Moq Protected is the kind of setup that takes time to get right the first time. I had AI handle that part (both helpers are marked `// AI Generated` in source) so I could focus on writing the actual test cases and deciding what to assert.

### `src/RealEstateAnalytics.DataProvider.Tests/ListingMappingTests.cs`
**How:** AI suggested which properties to assert in `MapToModel_ReturnsOk`.
**Why:** I wanted a second pair of eyes to make sure I had not missed any mapped fields. I went through each one myself and confirmed it matched what the mapper actually does.

### `src/RealEstateAnalytics.Console/Program.cs`
**How:** AI wrote the first draft of the user-facing prompts - the labels, defaults, and examples.
**Why:** Mostly to move faster on the less interesting parts. I tweaked the wording and reworked the loop logic to behave the way I wanted.

### `src/RealEstateAnalytics.Service/ListingService.cs`
**How:** AI gave me a starting point for caching with `IMemoryCache`.
**Why:** I knew I wanted caching but was not sure where to draw the line. After seeing what AI came up with (caching per page), I realised it made more sense to cache the full result at the query level - so I reworked it to do that instead.

### `src/RealEstateAnalytics.DataProvider/ServiceCollectionExtensions.cs`
**How:** AI walked me through the Polly rate limiter options and put together an initial setup.
**Why:** I had not used the newer `Microsoft.Extensions.Http.Resilience` API before, so I used AI to get up to speed on what each option does. The actual decisions - using a sliding window, setting `QueueLimit = int.MaxValue` to delay rather than reject, and settling on a 1 minute window - were mine after understanding the trade-offs.