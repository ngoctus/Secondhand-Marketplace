using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Common;

/// <summary>Creates Excel-friendly CSV files and neutralizes spreadsheet formulas in text fields.</summary>
public static class CsvExport
{
    public static FileContentResult Create(string fileName, IEnumerable<IEnumerable<string?>> rows)
    {
        var csv = new StringBuilder();
        foreach (var row in rows)
        {
            csv.AppendJoin(',', row.Select(Escape));
            csv.Append("\r\n");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return new FileContentResult(bytes, "text/csv; charset=utf-8") { FileDownloadName = fileName };
    }

    public static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        var firstMeaningful = value.TrimStart(' ', '\t', '\r', '\n');
        if (firstMeaningful.Length > 0 && firstMeaningful[0] is '=' or '+' or '-' or '@')
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
