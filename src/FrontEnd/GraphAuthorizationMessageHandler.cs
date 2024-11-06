using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace FrontEnd;

public class GraphAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public GraphAuthorizationMessageHandler(IAccessTokenProvider provider,
        NavigationManager navigation, IConfiguration config)
        : base(provider, navigation)
    {
        ConfigureHandler(
            authorizedUrls: [
                string.Join("/",
                    config.GetSection("MicrosoftGraph")["BaseUrl"] ??
                        "https://graph.microsoft.com",
                    config.GetSection("MicrosoftGraph")["Version"] ??
                        "v1.0")
            ],
            scopes: config.GetSection("MicrosoftGraph:Scopes")
                        .Get<List<string>>() ?? ["user.read"]);
    }
}