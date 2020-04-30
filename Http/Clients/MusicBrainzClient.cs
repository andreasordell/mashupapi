using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MashupApi.Http.Clients.Exceptions;
using MashupApi.Models.MusicBrainz;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace MashupApi.Http.Clients
{
    public class MusicBrainzClient
    {
        private readonly HttpClient _client;
        private readonly IAsyncPolicy<HttpResponseMessage> _cachePolicy;
        private readonly ILogger<MusicBrainzClient> _logger;
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };

        public MusicBrainzClient(HttpClient httpClient, IReadOnlyPolicyRegistry<string> policyRegistry, ILogger<MusicBrainzClient> logger)
        {
            // This isn't a proper UserAgent string according to MusicBrainz as it doesn't include proper contact information
            // Contact Information however, not following the proper standards, so.. this has been reformatted.
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CygniMashup/1.0");
            httpClient.BaseAddress = new Uri("http://musicbrainz.org");
            _client = httpClient;

            _cachePolicy = policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("MusicBrainzCachePolicy");

            _logger = logger;

            logger.LogDebug(_cachePolicy.PolicyKey);
        }
        public async Task<Artist> GetArtist(Guid mbid)
        {
            try
            {
                var requestUrl = $"/ws/2/artist/{mbid}?fmt=json&inc=url-rels+release-groups";
                var result = await this._cachePolicy.ExecuteAsync(
                        async context => await this._client.GetAsync(requestUrl), 
                        new Context(requestUrl));

                result.EnsureSuccessStatusCode();

                await result.Content.ReadAsStringAsync();
                var responseStream = await result.Content.ReadAsStreamAsync();
                responseStream.Seek(0, SeekOrigin.Begin);
                
                var artist = await JsonSerializer
                    .DeserializeAsync<Artist>(responseStream, _serializerOptions);

                return artist;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Artist not found.");
                throw new MusicBrainzNotFoundException($"Artist with id {mbid} not found.", ex);
            }
        }
    }
}
