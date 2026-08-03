using System.Text;
using IRA.Application.Abstractions.Documents;

namespace IRA.Infrastructure.Documents;

/// <summary>
/// Fallback document extractor used when Azure Document Intelligence is not configured.
/// Reads text-based documents directly. Adequate for .txt/.md/.json resumes and for tests.
/// </summary>
public class PlainTextDocumentExtractor : IDocumentExtractor
{
    public async Task<string> ExtractTextAsync(Stream document, string fileName, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(document, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        return text.Trim();
    }
}
