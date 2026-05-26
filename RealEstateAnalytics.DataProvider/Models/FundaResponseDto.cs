using System.Text.Json.Serialization;

namespace RealEstateAnalytics.DataProvider.Models;

public class FundaResponseDto
{
    [JsonPropertyName("Objects")]
    public List<PartnerResidentialObjectDto> Objects { get; set; } = [];

    [JsonPropertyName("Paging")]
    public PartnerPagingDto Paging { get; set; } = new();

    [JsonPropertyName("TotaalAantalObjecten")]
    public int TotalCount { get; set; }
}
