using System.Net;
using System.Net.Http.Headers;

namespace BearWordsMaui.Services.Helpers;

public partial class JwtAuthHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public JwtAuthHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetValidTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}


//public partial class JwtAuthHandler : DelegatingHandler
//{
//    private readonly IAuthService _authService;

//    public JwtAuthHandler(IAuthService authService)
//    {
//        _authService = authService;
//    }

//    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
//        CancellationToken cancellationToken)
//    {
//        var token = await _authService.GetValidTokenAsync();
//        if (!string.IsNullOrEmpty(token))
//        {
//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
//        }

//        var response = await base.SendAsync(request, cancellationToken);

//        if (response.StatusCode == HttpStatusCode.Unauthorized)
//        {
//            try
//            {
//                await _authService.RefreshTokenAsync();
//                var newToken = await _authService.GetValidTokenAsync();

//                if (!string.IsNullOrEmpty(newToken))
//                {
//                    // Clone the request for retry
//                    var clonedRequest = await JwtAuthHandler.CloneRequestAsync(request);
//                    clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

//                    response.Dispose(); // Dispose the 401 response
//                    response = await base.SendAsync(clonedRequest, cancellationToken);
//                }
//            }
//            catch (Exception ex)
//            {
//            }
//        }

//        return response;
//    }

//    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
//    {
//        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
//        {
//            Version = original.Version
//        };

//        foreach (var header in original.Headers)
//        {
//            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
//        }

//        if (original.Content is not null)
//        {
//            var content = await original.Content.ReadAsByteArrayAsync();
//            clone.Content = new ByteArrayContent(content);

//            foreach (var header in original.Content.Headers)
//            {
//                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
//            }
//        }

//        return clone;
//    }
//}
