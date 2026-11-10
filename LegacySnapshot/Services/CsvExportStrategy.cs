using System.Text;
using LegacySnapshot.Interfaces;
using LegacySnapshot.Models;

namespace LegacySnapshot.Services;

public sealed class CsvExportStrategy : IExportStrategy
{
    public string FileExtension => ".csv";
    public string ContentType => "text/csv";

    public string Export(QueryResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", result.Columns.Select(Escape)));

        foreach (var row in result.Rows)
        {
            builder.AppendLine(string.Join(",", row.Select(value => Escape(value ?? string.Empty))));
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            value = $"'{value}";
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
