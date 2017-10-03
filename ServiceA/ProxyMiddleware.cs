using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceA
{
    using System.Net.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// This is a middleware component that forwards an HTTP request to the Service Fabric reverse proxy service.
    /// This class is heavily inspired by the the https://github.com/aspnet/Proxy library.
    /// At the moment, only HTTP is supported.
    /// </summary>
    public class ProxyMiddleware
    {
        private const string ForwardedHostHeaderName = "X-Forwarded-Host";
        private const string ForwardPath = "/ReverseProxyTest/ServiceB";

        private readonly HttpClient _httpClient;
        private readonly ReverseProxyOptions _reverseProxyOptions;

        public ProxyMiddleware(RequestDelegate next, IOptions<ReverseProxyOptions> proxyOptions, HttpMessageHandler httpMessageHandler)
        {
            if (proxyOptions?.Value==null)
                throw new ArgumentNullException(nameof(proxyOptions));
            _reverseProxyOptions = proxyOptions.Value;
            _httpClient = new HttpClient(httpMessageHandler ?? new HttpClientHandler());
        }

        public async Task Invoke(HttpContext context)
        {
            var requestMessage = new HttpRequestMessage();
            requestMessage.Headers.Host = _reverseProxyOptions.Host + ":" + _reverseProxyOptions.Port;
            string uriString = $"{_reverseProxyOptions.Scheme}://{_reverseProxyOptions.Host}:{_reverseProxyOptions.Port}{ForwardPath}{context.Request.QueryString}";

            requestMessage.RequestUri = new Uri(uriString);
            requestMessage.Method = HttpMethod.Get;

            //Copy the request headers
            foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            //make sure the original host is added to a header so that the downstream service can know what the original host was.

            requestMessage.Headers.TryAddWithoutValidation(ForwardedHostHeaderName, context.Request.Host.Value);

            using (HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
            {
                context.Response.StatusCode = (int)responseMessage.StatusCode;
                foreach (KeyValuePair<string, IEnumerable<string>> header in responseMessage.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (KeyValuePair<string, IEnumerable<string>> header in responseMessage.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                // SendAsync removes chunking from the response.This removes the header so it doesn't expect a chunked response.
                context.Response.Headers.Remove("transfer-encoding");

                await responseMessage.Content.CopyToAsync(context.Response.Body);
            }
        }
    }
}
