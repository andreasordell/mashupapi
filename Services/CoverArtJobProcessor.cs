using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MashupApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MashupApi.Services
{
    public class CoverArtJobProcessor : IBackgroundJobProcessor<CoverArtJob>
    {
        private readonly ILogger<CoverArtJobProcessor> _logger;
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;
        private readonly MongoDbService _dataService;

        public CoverArtJobProcessor(
            IHttpClientFactory clientFactory, 
            MongoDbService dataService, 
            ILogger<CoverArtJobProcessor> logger,
            IMemoryCache cache)
        {
            _httpClient = clientFactory.CreateClient("coverArtClient");
            _logger = logger;
            _cache = cache;
            _dataService = dataService;
        }
        public async Task ProcessJob((CoverArtJob job, Action callback) data, CancellationToken cancellationToken)
        {
            var (job, callback) = data;

            try
            {
                var requestUrl = $"release-group/{job.Mbid}";
                _logger.LogInformation($"Downloading CoverArt data for {job.Mbid}.");

                var response = await _httpClient.GetAsync(requestUrl);

                CoverArtModel model;

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var document = JsonDocument.Parse(responseString);
                    var image = document.RootElement
                        .GetProperty("images")
                        .EnumerateArray()
                        .FirstOrDefault(img => img.GetProperty("front").GetBoolean())
                        .GetProperty("image")
                        .GetString();

                    _logger.LogInformation($"Saving CoverArt for {job.Mbid}");
                    model = new CoverArtModel
                    {
                        Mbid = job.Mbid,
                        FetchDate = DateTime.Now,
                        StatusCode = 200,
                        ImageUrl = image,
                        ArtistMbid = job.ArtistMbid
                    };
                    _logger.LogInformation("Adding CoverArt to cache.");
                    _cache.Set(job.Mbid, model, TimeSpan.FromMinutes(5));
                }
                else
                {
                    _logger.LogInformation($"{response.StatusCode} Error retrieving CoverArt for {job.Mbid}");
                    model = new CoverArtModel
                    {
                        Mbid = job.Mbid,
                        FetchDate = DateTime.Now,
                        StatusCode = (int)response.StatusCode,
                        ArtistMbid = job.ArtistMbid
                    };
                    _logger.LogInformation("Adding CoverArt to cache.");
                    _cache.Set(job.Mbid, model, TimeSpan.FromDays(1));
                }
                await _dataService.SaveCoverArt(model);
                
                
                _logger.LogDebug($"Saved CoverArt for {job.Mbid}.");
            }
            
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting {job.Mbid}");
            }
            finally
            {
                // Release our queue semaphore allowing an additional item to be processed.
                callback();
            }
        }
    }
}
