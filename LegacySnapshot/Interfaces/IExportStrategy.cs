using LegacySnapshot.Models;

namespace LegacySnapshot.Interfaces;

public interface IExportStrategy
{
    string FileExtension { get; }
    string ContentType { get; }
    string Export(QueryResult result);
}
