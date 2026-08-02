namespace IRA.Application.Abstractions.Documents;

/// <summary>
/// Extracts plain text from an uploaded document using Azure Document Intelligence
/// (with a plain-text fallback extractor).
/// </summary>
public interface IDocumentExtractor
{
    Task<string> ExtractTextAsync(Stream document, string fileName, CancellationToken cancellationToken = default);
}
