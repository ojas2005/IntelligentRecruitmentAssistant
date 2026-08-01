using IRA.Domain.Common;
using IRA.Domain.Enums;

namespace IRA.Domain.Entities;

/// <summary>
/// An uploaded resume document and the state of its journey through the
/// Extract -> Parse -> Embed -> Evaluate pipeline.
/// </summary>
public class Resume : Entity
{
    public Guid? CandidateId { get; private set; }
    public string FileName { get; private set; }
    public string BlobPath { get; private set; }
    public string? RawText { get; private set; }
    public ResumeStatus Status { get; private set; } = ResumeStatus.Uploaded;
    public string? FailureReason { get; private set; }

    public Resume(string fileName, string blobPath)
    {
        FileName = string.IsNullOrWhiteSpace(fileName)
            ? throw new DomainException("Resume file name is required.")
            : fileName;
        BlobPath = blobPath ?? string.Empty;
    }

    private Resume()
    {
        FileName = string.Empty;
        BlobPath = string.Empty;
    }

    public void MarkExtracted(string rawText)
    {
        RawText = rawText;
        Status = ResumeStatus.Extracted;
    }

    public void MarkParsed(Guid candidateId)
    {
        CandidateId = candidateId;
        Status = ResumeStatus.Parsed;
    }

    public void MarkEmbedded() => Status = ResumeStatus.Embedded;

    public void MarkEvaluated() => Status = ResumeStatus.Evaluated;

    public void MarkFailed(string reason)
    {
        Status = ResumeStatus.Failed;
        FailureReason = reason;
    }
}
