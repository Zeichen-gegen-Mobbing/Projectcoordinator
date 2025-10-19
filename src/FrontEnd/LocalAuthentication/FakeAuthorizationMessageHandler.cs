using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace FrontEnd.Services
{
    public class FakeAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly IConfiguration _config;
        private static readonly string _staticKey = "0123456789abcdef0123456789abcdef"; // 32 chars = 256 bits
        private static readonly string authority = "https://fake-authority.local";
        private static readonly string clientId = "debug-clientid";
        public FakeAuthorizationMessageHandler(IConfiguration config)
        {
            _config = config;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_staticKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: authority,
                audience: clientId,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );
            var token = tokenHandler.WriteToken(jwt);

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
