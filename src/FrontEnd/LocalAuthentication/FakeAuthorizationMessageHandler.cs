using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using ZgM.ProjectCoordinator.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace FrontEnd.Services
{
    public class FakeAuthorizationMessageHandler : DelegatingHandler
    {
        private static readonly string _staticKey = "0123456789abcdef0123456789abcdef"; // 32 chars = 256 bits
        private static readonly string authority = "https://fake-authority.local";
        private static readonly string clientId = "debug-clientid";
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public FakeAuthorizationMessageHandler(AuthenticationStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_staticKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>();
            foreach (var claim in user.Claims)
            {
                if (claim.Type == ClaimTypes.Role)
                {
                    claims.Add(new Claim("roles", claim.Value));
                }
                else
                {
                    claims.Add(claim);
                }
            }

            var jwt = new JwtSecurityToken(
                issuer: authority,
                audience: clientId,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );
            var token = tokenHandler.WriteToken(jwt);

            request.Headers.TryAddWithoutValidation(CustomHttpHeaders.SwaAuthorization, $"Bearer {token}");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
