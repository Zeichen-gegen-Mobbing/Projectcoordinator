#if DEBUG
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;

namespace FrontEnd.LocalAuthentication
{
    public class LocalAuthenticationProvider : AuthenticationStateProvider, IAccessTokenProvider, IAccessTokenProviderAccessor
    {
        static ClaimsPrincipal _projectCoordination = new(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "TEST Projectcoordination"),
                new Claim(ClaimTypes.Role, "projectcoordination")
        }, "LocalAuthentication"));

        static ClaimsPrincipal _user = new(new ClaimsIdentity(new[]
        {
                new Claim(ClaimTypes.Name, "TEST User")
        }, "LocalAuthentication"));

        static ClaimsPrincipal _unauthenticated = new(new ClaimsIdentity());

        ClaimsPrincipal _selected = _unauthenticated; // Start unauthenticated
        public static List<string?> GetNames()
        {
            return new List<string?>() {
            "None (Unauthenticated)",
            nameof(_projectCoordination),
            nameof(_user)
            };
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_selected));
        }
        public Task<AuthenticationState> ChangeIdentityAsync(string username)
        {
            switch (username)
            {
                case nameof(_projectCoordination):
                    _selected = _projectCoordination;
                    break;
                case nameof(_user):
                    _selected = _user;
                    break;
                case "None (Unauthenticated)":
                    _selected = _unauthenticated;
                    break;
                default:
                    _selected = _unauthenticated;
                    break;
            }

            var task = GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(task);
            return task;
        }

        public ValueTask<AccessTokenResult> RequestAccessToken()
        {
            return ValueTask.FromResult(new AccessTokenResult(AccessTokenResultStatus.Success, new AccessToken() { Expires = DateTime.Now + new TimeSpan(365, 0, 0, 0) }, "", null));
        }

        public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions _)
        {
            return RequestAccessToken();
        }

        public IAccessTokenProvider TokenProvider => this;

        public static void AddLocalAuthentication(IServiceCollection services)
        {
            //https://github.com/dotnet/aspnetcore/blob/c925f99cddac0df90ed0bc4a07ecda6b054a0b02/src/Components/WebAssembly/WebAssembly.Authentication/src/WebAssemblyAuthenticationServiceCollectionExtensions.cs#L28

            services.AddOptions();
            services.AddAuthorizationCore();
            services.TryAddScoped<AuthenticationStateProvider, LocalAuthenticationProvider>();


            services.TryAddTransient<BaseAddressAuthorizationMessageHandler>();
            services.TryAddTransient<AuthorizationMessageHandler>();

            services.TryAddScoped(sp =>
            {
                return (IAccessTokenProvider)sp.GetRequiredService<AuthenticationStateProvider>();
            });

            services.TryAddScoped(sp =>
            {
                return (IAccessTokenProviderAccessor)sp.GetRequiredService<AuthenticationStateProvider>();
            });

            //services.TryAddScoped<SignOutSessionStateManager>();
        }
    }
}
#endif