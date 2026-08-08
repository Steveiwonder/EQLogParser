using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using EQLogParser.Contracts;

namespace EQLogParser
{
    public class RemoteStatusPublisher : IStatusPublisher
    {
        private readonly HttpClient _httpClient;
        private readonly RemoteStatusOptions _options;

        public RemoteStatusPublisher(HttpClient httpClient, RemoteStatusOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task PublishAsync(ParserStatusUpdate status, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.StatusEndpoint))
            {
                return;
            }

            try
            {
                using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(_options.StatusEndpoint, status, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                // Keep parsing even when the remote dashboard is temporarily unavailable.
            }
        }
    }
}
