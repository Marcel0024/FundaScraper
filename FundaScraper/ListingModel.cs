namespace FundaScraper;

public class ListingModel
{
    public required string Title { get; init; }
    public required string ZipCode { get; init; }
    public required string Price { get; init; }
    public required string Area { get; init; }
    public required string TotalRooms { get; init; }

    public string Url { get; set; } = default!;
    public DateTimeOffset DateTimeAdded { get; set; }
}
