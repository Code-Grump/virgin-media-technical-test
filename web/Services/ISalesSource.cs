
namespace VirginMedia.TechTests.SalesSummary.Services;

public interface ISalesSource
{
    IAsyncEnumerable<SalesLine> GetAllSalesAsync(CancellationToken cancellationToken = default);
}
