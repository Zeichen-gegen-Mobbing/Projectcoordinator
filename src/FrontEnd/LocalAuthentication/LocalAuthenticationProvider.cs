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
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.LocalAuthentication
{
    public class LocalAuthenticationProvider : AuthenticationStateProvider, IAccessTokenProvider, IAccessTokenProviderAccessor,
    IRemoteAuthenticationService<RemoteAuthenticationState>
    {
        private const string LocalStorageKey = "debug-selected-user";
        private static readonly string _staticKey = "0123456789abcdef0123456789abcdef"; // 32 chars = 256 bits
        private static readonly string authority = "https://fake-authority.local";
        private readonly AuthenticationOptions _authConfig;
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;
        private bool _initialized = false;

        // Base identities without roles - roles are added only in access tokens
        static readonly ClaimsPrincipal _admin = new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "TEST Admin"),
                new Claim(ClaimTypes.NameIdentifier, "test-admin-id"),
                new Claim("oid", "000-0000-0000-0000-000000000001")
        ], "LocalAuthentication"));
        static readonly ClaimsPrincipal _projectCoordination = new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "TEST Projectcoordination"),
                new Claim(ClaimTypes.NameIdentifier, "test-projectcoordination-id"),
                new Claim("oid", "000-0000-0000-0000-000000000002")
        ], "LocalAuthentication"));

        static readonly ClaimsPrincipal _socialVisionary = new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "TEST Social Visionary"),
                new Claim(ClaimTypes.NameIdentifier, "test-socialvisionary-id"),
                new Claim("oid", "000-0000-0000-0000-000000000003")
        ], "LocalAuthentication"));

        static readonly ClaimsPrincipal _user = new(new ClaimsIdentity(new[]
        {
                new Claim(ClaimTypes.Name, "TEST User"),
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim("oid", "000-0000-0000-0000-000000000004")
        }, "LocalAuthentication"));

        static readonly ClaimsPrincipal _unauthenticated = new(new ClaimsIdentity());


        // Map user identifiers to their roles (used when generating tokens)
        private static readonly Dictionary<string, string[]> _userRoles = new()
        {
            ["test-admin-id"] = ["admin"],
            ["test-projectcoordination-id"] = ["projectcoordination"],
            ["test-socialvisionary-id"] = ["socialvisionary"],
            ["test-user-id"] = []
        };

        ClaimsPrincipal _selected = _unauthenticated;

        public LocalAuthenticationProvider(NavigationManager navigationManager, IJSRuntime jsRuntime, AuthenticationOptions authConfig)
        {
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
            _authConfig = authConfig;
        }

        public static List<string?> GetNames()
        {
            return new List<string?>() {
            "None (Unauthenticated)",
            nameof(_user),
            nameof(_socialVisionary),
            nameof(_projectCoordination),
            nameof(_admin)
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
                case nameof(_admin):
                    _selected = _admin;
                    break;
                case nameof(_projectCoordination):
                    _selected = _projectCoordination;
                    break;
                case nameof(_socialVisionary):
                    _selected = _socialVisionary;
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
            return await RequestAccessToken(new AccessTokenRequestOptions());
        }

        public async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }

            if (_selected.Identity?.IsAuthenticated != true)
            {
                return new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, new AccessToken(), "", null);
            }

            var token = GenerateJwtToken(options);
            var accessToken = new AccessToken()
            {
                Value = token,
                Expires = DateTime.Now + new TimeSpan(1, 0, 0)
            };
            return new AccessTokenResult(AccessTokenResultStatus.Success, accessToken, "", null);
        }

        private string GenerateJwtToken(AccessTokenRequestOptions options)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_staticKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = _selected.Claims.ToList();

            if (options.Scopes is not null && options.Scopes.Any())
            {
                // Check if any of the requested scopes are for the API client
                var requestsApiScope = options.Scopes.Any(s => s.Contains(_authConfig.ApiClientId));

                if (requestsApiScope)
                {
                    // Add roles to the token only when requesting API access
                    var userId = _selected.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (userId != null && _userRoles.TryGetValue(userId, out var roles))
                    {
                        foreach (var role in roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }
                }

                // Azure only has one Scope Claim with spaces between
                var scope = String.Join(" ", options.Scopes.Select(s => s.Split("/").Last()));
                claims.Add(new Claim("scp", scope));
            }

            var jwt = new JwtSecurityToken(
                issuer: authority,
                audience: _authConfig.ApiClientId,
                claims: claims,
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