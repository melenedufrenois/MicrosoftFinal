using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace LoLProject.WebApp.Clients;

public class TokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        
        if (context != null)
        {
            // Récupérer le token stocké dans le cookie
            var accessToken = await context.GetTokenAsync("access_token");

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}