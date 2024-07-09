namespace FundaScraper.Models;

public class ListingModel
{
    public required string Title { get; init; }
    public required string ZipCode { get; init; }
    public required string Url { get; init; }
    public required string Price { get; init; }
    public required string Area { get; init; }
    public required string TotalRooms { get; init; }

    public DateTimeOffset DateTimeAdded { get; set; }
}
