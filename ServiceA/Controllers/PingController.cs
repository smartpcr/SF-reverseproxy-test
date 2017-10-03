using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ServiceA.Controllers
{
    using System.Net.Http;

    [Route("api/ping")]
    public class PingController : Controller
    {
        private readonly ReverseProxyOptions _reverseProxyOptions;
        private const string ForwardPath = "/ReverseProxyTest/ServiceB";

        public PingController(IOptions<ReverseProxyOptions> proxyOptions)
        {
            if (proxyOptions?.Value == null)
                throw new ArgumentNullException(nameof(proxyOptions));

            _reverseProxyOptions = proxyOptions.Value;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var requestMessage = new HttpRequestMessage();
            requestMessage.Headers.Host = _reverseProxyOptions.Host + ":" + _reverseProxyOptions.Port;
            string uriString = $"{_reverseProxyOptions.Scheme}://{_reverseProxyOptions.Host}:{_reverseProxyOptions.Port}{ForwardPath}";
            requestMessage.RequestUri = new Uri(uriString);
            requestMessage.Method = HttpMethod.Get;
            var httpClient = new HttpClient();
            using (HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead))
            {
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }
    }
}
