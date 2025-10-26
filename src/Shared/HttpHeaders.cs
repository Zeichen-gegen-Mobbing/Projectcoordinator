namespace ZgM.ProjectCoordinator.Shared;

public static class CustomHttpHeaders
{
    /// <summary>
    /// Custom header name used by Azure Static Web Apps to pass the authorization token.
    /// The SWA platform replaces the standard Authorization header with this custom header.
    /// </summary>
    public const string SwaAuthorization = "ZgM-SWA-Authorization";
}
