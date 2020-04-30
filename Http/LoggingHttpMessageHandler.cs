using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MashupApi.Http {
    public class LoggingHttpMessageHandler : DelegatingHandler {
        private readonly ILogger<LoggingHttpMessageHandler> _logger;

        public LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger)
        {
            _logger = logger;
        }
        public LoggingHttpMessageHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
            
        }
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            System.Threading.CancellationToken cancellationToken) 
        {
            var sw = new Stopwatch();
            sw.Start();

            _logger.LogDebug(">> Outbound Request");
            _logger.LogDebug(request.ToString());
            if (request.Content != null) {
                _logger.LogDebug(await request.Content.ReadAsStringAsync());
            }
            var responseMessage = await base.SendAsync(request, cancellationToken);

            _logger.LogDebug("<< Incoming Response");
            _logger.LogDebug(responseMessage.ToString());

            // if (responseMessage.Content != null) {
            //     logger.LogDebug(await responseMessage.Content.ReadAsStringAsync());
            // }

            sw.Stop();
            _logger.LogDebug($"Time elapsed {sw.Elapsed}.");

            return responseMessage;
        }
    }
}
