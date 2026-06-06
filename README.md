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
During testing I observed that the API consistently returns a maximum of 25 objects per response, regardless of the `pagesize` parameter. When `pagesize` exceeds 25, `VolgendeUrl` appears to skip objects, it advances to the next logical page rather than the next physical response, which can result in missing records. Therefore, I set a fixed `pagesize` to 25, as this aligns with the API's actual behaviour, avoids data loss during pagination, and complicates pagination logic.

### API call efficiency
The partner API enforces a limit of 100 requests per minute. To stay within that limit while fetching all pages as fast as possible, a few approaches were explored:

**Sequential requests** were the starting point. Simple but slow, one page at a time means large result sets take a long time. Not taken forward.

**Retry on Too Many Request or "Unauthozied"** acts as a reactive safety net for when the API rejects a request despite the rate limiter. It waits 30 seconds before retrying.

**A sliding window rate limiter** sits on top to proactively stay under the 100/minute API limit. It tracks requests across 4 segments of 15 seconds each, releasing queued requests gradually as old segments expire rather than waiting for a full 60-second reset. This is the approach taken.

**Parallel requests with `Task.WhenAll`** fired all page requests at once. Fast, but caused timeout issues. All requests entered the rate limiter queue simultaneously, and `HttpClient.Timeout` starts counting from when a request is created, not when it is sent. Requests sitting in the queue expired before being dispatched. Not taken forward.

**Parallel requests with `Parallel.ForEachAsync`** and a bounded `MaxDegreeOfParallelism` keeps at most N requests in flight at a time. As one finishes, the next starts. This keeps the queue shallow enough that `HttpClient.Timeout` is never an issue. This is the approach taken.

### Caching
I implemented caching mechanism to avoid hammering the partner api further everytime the console application tries to retrieve data. Since for now, there are no concern with having live data all the time, this is the approach that I taken.

## Use of AI

I used Claude Code (Anthropic) as a thinking partner and coding assistant throughout this exercise. I leaned on AI for mechanical or repetitive work so I could spend my time on the 
decisions that actually matter. Below are the honest account of where AI was involved

### Test Setup and Boilerplate
Getting `HttpMessageHandler` mocking to work correctly with Moq Protected is tedious to set up from scratch. I had AI handle that scaffolding so I could focus on how to setup/arrange the tests and its verification. Every test case, every assertion, and every decision about what to cover was mine.
Implementation: `src/RealEstateAnalytics.DataProvider.Tests/ListingProviderTests.cs`

### Mapping Assertions 
Asserting every mapped field one by one is mechanical work. I used AI to generate those assertions faster and then went through each one against the actual mapper to confirm they were correct. AI wrote it, I reviewed and approved it.
Implementation: `src/RealEstateAnalytics.DataProvider.Tests/ListingMappingTests.cs`

### Console Prompts
I used AI to write the first draft of the user facing prompts to move faster on the less interesting parts. The wording were reworked by me to behave the way I intended.
Implementation: `src/RealEstateAnalytics.Console/Program.cs`

### Cache
I knew what I wanted to implement is a simple caching mechanism, so I used AI to generate the initial caching code with IMemoryCache and tweaked it to fit my needs.
Implementation:
- `src/RealEstateAnalytics.Service/ListingService.cs`
- `src/RealEstateAnalytics.Service/ServiceCollectionExtensions.cs`

### Rate Limiter Configuration
AI walked me through the available options in Microsoft.Extensions.Http.Resilience so I could understand what each setting does. Once I understood the trade offs I decide to use a sliding window, setting QueueLimit to delay rather than reject requests, and settling on a one minute window.
- `src/RealEstateAnalytics.DataProvider/ServiceCollectionExtensions.cs`

**Example**
Manual:         |──40s requests──|──60s delay──|  next batch
Rate limiter:   |──40s requests──|──20s  wait──|  next batch

### Parallel Request Outbound
This is where AI was most useful as a thinking partner rather than a code writer. 
Despite using rate limiter when sending concurrent requests, I still experience timeout issues. I worked through the problem with AI and we identified that HttpClient.Timeout starts counting when a request is created, not when it is sent. This meant requests sitting in the queue were expiring before they were ever dispatched.
AI suggested me to increase the `HttpClient.Timeout`, but that will not solve the problem as it will also impact actual requests that were sent and having actual timeouts. The right fix is not to rely on rate limiter to send requests in batches because rate limiter purpose is to limit the request, but to introduce a mechanism that can send the request in batches. AI suggested to use `Parallel.ForEachAsync` with a bounded concurrency limit keeps the queue shallow enough.
Implementation: 
- `src/RealEstateAnalytics.Service/ListingService.cs`

## How to run it
TO-DO

## Results
TO-DO