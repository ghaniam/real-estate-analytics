using System.Text.Json.Serialization;

namespace RealEstateAnalytics.DataProvider.Models;

public class PartnerPagingDto
{
    [JsonPropertyName("HuidigePagina")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("AantalPaginas")]
    public int TotalPages { get; set; }

    [JsonPropertyName("AantalResultaten")]
    public int ResultCount { get; set; }
}
