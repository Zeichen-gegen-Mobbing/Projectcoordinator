#if DEBUG
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FrontEnd.LocalAuthentication
{
    public class LocalAuthenticationProvider : AuthenticationStateProvider, IAccessTokenProvider, IAccessTokenProviderAccessor,
    IRemoteAuthenticationService<RemoteAuthenticationState>
    {
        private const string LocalStorageKey = "debug-selected-user";
        private static readonly string _staticKey = "0123456789abcdef0123456789abcdef"; // 32 chars = 256 bits
        private static readonly string authority = "https://fake-authority.local";
        private static readonly string clientId = "debug-clientid";
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;
        private bool _initialized = false;

        static ClaimsPrincipal _projectCoordination = new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "TEST Projectcoordination"),
                new Claim(ClaimTypes.NameIdentifier, "test-projectcoordination-id"),
                new Claim(ClaimTypes.Role, "projectcoordination"),
                new Claim("scp", "Trips.Calculate")
        ], "LocalAuthentication"));

        static ClaimsPrincipal _user = new(new ClaimsIdentity(new[]
        {
                new Claim(ClaimTypes.Name, "TEST User"),
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "LocalAuthentication"));

        static ClaimsPrincipal _unauthenticated = new(new ClaimsIdentity());

        ClaimsPrincipal _selected = _unauthenticated;

        public LocalAuthenticationProvider(NavigationManager navigationManager, IJSRuntime jsRuntime)
        {
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
        }

        public static List<string?> GetNames()
        {
            return new List<string?>() {
            "None (Unauthenticated)",
            nameof(_projectCoordination),
            nameof(_user)
            };
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }

            return new AuthenticationState(_selected);
        }

        private async Task InitializeAsync()
        {
            try
            {
                var storedUser = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", LocalStorageKey);
                if (!string.IsNullOrEmpty(storedUser))
                {
                    SelectUser(storedUser);
                }
            }
            catch
            {
                // localStorage might not be available yet
            }
            finally
            {
                _initialized = true;
            }
        }

        public async Task<AuthenticationState> ChangeIdentityAsync(string username)
        {
            SelectUser(username);

            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, username);
            }
            catch
            {
                // Ignore localStorage errors
            }

            var task = GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(task);
            return await task;
        }

        private void SelectUser(string username)
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
        }

        public async ValueTask<AccessTokenResult> RequestAccessToken()
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }

            if (_selected.Identity?.IsAuthenticated != true)
            {
                return new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, new AccessToken(), "", null);
            }

            var token = GenerateJwtToken(_selected);
            var accessToken = new AccessToken()
            {
                Value = token,
                Expires = DateTime.Now + new TimeSpan(1, 0, 0)
            };
            return new AccessTokenResult(AccessTokenResultStatus.Success, accessToken, "", null);
        }

        public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions _)
        {
            return RequestAccessToken();
        }

        private static string GenerateJwtToken(ClaimsPrincipal user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_staticKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: authority,
                audience: clientId,
                claims: user.Claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return tokenHandler.WriteToken(jwt);
        }

        public Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        {
            // Redirect to user selection page
            _navigationManager.NavigateTo("/debug-select-user?returnUrl=" + Uri.EscapeDataString(context.State?.ReturnUrl ?? "/"));

            var result = new RemoteAuthenticationResult<RemoteAuthenticationState>
            {
                Status = RemoteAuthenticationStatus.Redirect
            };
            return Task.FromResult(result);
        }

        public Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        {
            var result = new RemoteAuthenticationResult<RemoteAuthenticationState>
            {
                Status = RemoteAuthenticationStatus.Success,
                State = context.State
            };
            return Task.FromResult(result);
        }

        public async Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        {
            Console.WriteLine("DEBUG: SignOutAsync called");
            // Clear the authentication state immediately
            await ChangeIdentityAsync("None (Unauthenticated)");
            
            // Also clear from localStorage
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", LocalStorageKey);
                Console.WriteLine("DEBUG: localStorage cleared in SignOutAsync");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: localStorage error in SignOutAsync: {ex.Message}");
            }

            // Force page reload to clear all state
            _navigationManager.NavigateTo("/", forceLoad: true);

            var result = new RemoteAuthenticationResult<RemoteAuthenticationState>
            {
                Status = RemoteAuthenticationStatus.Success,
                State = context.State
            };
            return result;
        }

        public async Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        {
            Console.WriteLine("DEBUG: CompleteSignOutAsync called");
            // Clear the authentication state
            await ChangeIdentityAsync("None (Unauthenticated)");
            
            // Also clear from localStorage
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", LocalStorageKey);
                Console.WriteLine("DEBUG: localStorage cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: localStorage error: {ex.Message}");
            }

            Console.WriteLine("DEBUG: CompleteSignOutAsync returning Success");
            var result = new RemoteAuthenticationResult<RemoteAuthenticationState>
            {
                Status = RemoteAuthenticationStatus.Success,
                State = context.State
            };
            return result;
        }

        public IAccessTokenProvider TokenProvider => this;
    }
}
#endif