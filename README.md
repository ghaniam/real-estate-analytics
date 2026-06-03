# real-estate-analytics

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
