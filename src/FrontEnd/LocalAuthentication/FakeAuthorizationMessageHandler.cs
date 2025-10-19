using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FrontEnd.LocalAuthentication
{
    public class FakeAuthorizationMessageHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "debug-token");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
