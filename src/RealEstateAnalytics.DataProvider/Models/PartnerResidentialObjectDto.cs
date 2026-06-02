using System.Text.Json.Serialization;

namespace RealEstateAnalytics.DataProvider.Models;

public class PartnerResidentialObjectDto
{
    [JsonPropertyName("Adres")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("Woonplaats")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("Postcode")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonPropertyName("URL")]
    public string? ListingUrl { get; set; }

    [JsonPropertyName("MakelaarId")]
    public int? AgentId { get; set; }

    [JsonPropertyName("MakelaarNaam")]
    public string? AgentName { get; set; }

    [JsonPropertyName("Soort-aanbod")]
    public string? ListingType { get; set; }

}
