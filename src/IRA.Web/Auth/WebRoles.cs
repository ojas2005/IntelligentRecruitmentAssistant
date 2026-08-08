namespace IRA.Web.Auth;

/// <summary>Role names mirrored from the API so the MVC portal can gate screens.</summary>
public static class WebRoles
{
    public const string Recruiter = "Recruiter";
    public const string HiringManager = "HiringManager";
    public const string Administrator = "RecruitmentAdministrator";
    public const string Candidate = "Candidate";

    /// <summary>Comma-separated recruiter roles for <c>[Authorize(Roles = ...)]</c>.</summary>
    public const string RecruiterPortal = Recruiter + "," + HiringManager + "," + Administrator;
}
