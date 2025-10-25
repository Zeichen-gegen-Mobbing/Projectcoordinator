using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Headers;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd;

public class CustomAuthorizationMessageHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly NavigationManager _navigation;
    private readonly List<Uri> _authorizedUris = new();

    public CustomAuthorizationMessageHandler(IAccessTokenProvider provider,
        NavigationManager navigation)
    {
        _tokenProvider = provider;
        _navigation = navigation;
    }

    public CustomAuthorizationMessageHandler ConfigureHandler(IEnumerable<string> authorizedUrls)
    {
        _authorizedUris.Clear();
        foreach (var url in authorizedUrls)
        {
            _authorizedUris.Add(new Uri(url));
        }
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null && IsAuthorizedUrl(request.RequestUri))
        {
            var result = await _tokenProvider.RequestAccessToken();
            if (result.TryGetToken(out var token))
            {
                request.Headers.TryAddWithoutValidation(CustomHttpHeaders.SwaAuthorization, $"Bearer {token.Value}");
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private bool IsAuthorizedUrl(Uri requestUri)
    {
        foreach (var authorizedUri in _authorizedUris)
        {
            if (requestUri.AbsoluteUri.StartsWith(authorizedUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
