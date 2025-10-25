namespace api.Options;

public record AuthenticationOptions : ZgM.ProjectCoordinator.Shared.AuthenticationOptions
{
    internal const string Title = "Authentication";

    internal ZgM.ProjectCoordinator.Shared.AuthenticationOptions ToSharedOptions()
    {
        return new ZgM.ProjectCoordinator.Shared.AuthenticationOptions
        {
            FrontEndClientId = this.FrontEndClientId,
            ApiScope = this.ApiScope
        };
    }
}