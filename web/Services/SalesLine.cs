namespace VirginMedia.TechTests.SalesSummary.Services;

public record SalesLine(
    string Segment,
    string Country,
    string Product,
    string DiscountBand,
    decimal UnitsSold,
    decimal? ManufacturingPriceGbp,
    decimal? SalesPriceGbp,
    DateOnly Date);
