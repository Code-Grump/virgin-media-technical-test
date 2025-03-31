using CsvHelper;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace VirginMedia.TechTests.SalesSummary.Services;

public class CsvFileSalesSourceOptions
{
    [Required]
    public string? File { get; set; }

    public string? Encoding { get; set; }

    public Encoding EncodingObject { get; set; } = System.Text.Encoding.UTF8;
}

public class PostConfigureCsvFileSalesSourceOptions : IPostConfigureOptions<CsvFileSalesSourceOptions>
{
    public void PostConfigure(string? name, CsvFileSalesSourceOptions options)
    {
        if (!string.IsNullOrEmpty(options.Encoding))
        {
            options.EncodingObject = Encoding.GetEncoding(options.Encoding);
        }
    }
}

public partial class CsvFileSalesSource(IOptions<CsvFileSalesSourceOptions> options) : ISalesSource
{
    public async IAsyncEnumerable<SalesLine> GetAllSalesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sourceOptions = options.Value;

        using var reader = new CsvReader(
            new StreamReader(
                File.Open(sourceOptions.File!, FileMode.Open, FileAccess.Read, FileShare.Read),
                sourceOptions.EncodingObject),
            CultureInfo.InvariantCulture);

        int lineNumber = 1;

        string RequireStringField(string name) => reader.GetField(name) ??
            throw new InvalidOperationException($"Field \"{name}\" is missing from record on line {lineNumber}");

        decimal RequireDecimal(string name)
        {
            try
            {
                return decimal.Parse(
                    RemoveWhitespace(
                        RequireStringField(name)));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Field \"{name}\" on line {lineNumber} could not be read as a float. " + ex.Message,
                    ex);
            }
        }

        decimal? ReadGbp(string name)
        {
            var value = reader.GetField(name);

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (value[0] != '£')
            {
                throw new InvalidOperationException(
                    $"Field \"{name}\" on line {lineNumber} could not be read as a GBP value. " +
                    "The field does not start with a £ symbol.");
            }

            try
            {
                return decimal.Parse(RemoveWhitespace(value[1..]));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Field \"{name}\" on line {lineNumber} could not be read as a GBP value. " + ex.Message,
                    ex);
            }
        }

        DateOnly RequireDate(string name)
        {
            var value = RequireStringField(name);

            try
            {
                return DateOnly.ParseExact(value, "MM/dd/yyyy");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Field \"{name}\" on line {lineNumber} could not be read as a date value. " + ex.Message,
                    ex);
            }
        }

        if (!await reader.ReadAsync() || !reader.ReadHeader())
        {
            throw new InvalidOperationException("Expected source CSV to contain a header but none was found.");
        }

        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            lineNumber++;

            yield return new SalesLine(
                RequireStringField("Segment"),
                RequireStringField("Country"),
                RequireStringField(" Product ").Trim(),
                RequireStringField(" Discount Band ").Trim(),
                RequireDecimal("Units Sold"),
                ReadGbp("Manufacturing Price"),
                ReadGbp("Sale Price"),
                RequireDate("Date"));
        }
    }

    private static string RemoveWhitespace(string value) => AnyWhitespaceRegex().Replace(value, "");

    [GeneratedRegex(@"\s+")]
    private static partial Regex AnyWhitespaceRegex();
}
