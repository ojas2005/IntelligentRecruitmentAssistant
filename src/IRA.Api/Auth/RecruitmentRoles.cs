namespace IRA.Api.Auth;

/// <summary>Actors from the case study, expressed as authorization roles.</summary>
public static class RecruitmentRoles
{
    public const string Recruiter = "Recruiter";
    public const string HiringManager = "HiringManager";
    public const string Administrator = "RecruitmentAdministrator";

    /// <summary>A job applicant using the self-service candidate portal ("user").</summary>
    public const string Candidate = "Candidate";

    /// <summary>Every role that belongs to the recruiter-facing portal.</summary>
    public static readonly string[] RecruiterRoles = { Recruiter, HiringManager, Administrator };
}

/// <summary>Named authorization policies applied to the API endpoints.</summary>
public static class RecruitmentPolicies
{
    /// <summary>Any authenticated recruiting actor (recruiter portal).</summary>
    public const string Recruiters = "Recruiters";

    /// <summary>Recruitment administrators only (e.g. audit trail).</summary>
    public const string Administrators = "Administrators";

    /// <summary>Candidate self-service, also open to recruiters (upload own resume, browse jobs).</summary>
    public const string CandidatePortal = "CandidatePortal";
}
