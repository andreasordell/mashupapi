using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MashupApi.Models;
using MashupApi.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Conventions;
using Polly;
using Polly.Registry;
using Polly.Timeout;

namespace MashupApi.Http.Clients
{
    public class CoverArtService
    {
        private readonly IBackgroundQueue<CoverArtJob> _queue;
        private readonly ILogger<CoverArtService> _logger;
        private readonly MongoDbService _dataService;
        private IMemoryCache _memoryCache;

        public CoverArtService(IBackgroundQueue<CoverArtJob> queue, ILogger<CoverArtService> logger, IMemoryCache memoryCache, MongoDbService dataService)
        {
            _queue = queue;
            _logger = logger;
            _dataService = dataService;
            _memoryCache = memoryCache;
        }

        public async Task<CoverArtModel> GetOrQueueCoverArt(Guid mbid, Guid artistMbid)
        {
            CoverArtModel coverArt;
            if (_memoryCache.TryGetValue(mbid, out coverArt))
            {
                _logger.LogInformation($"Getting cover art {coverArt.Mbid} from memory cache.");
            }
            else
            {
                _logger.LogInformation($"Cover art {mbid} is not in memory cache.");
                coverArt = await _dataService.GetCoverArt(mbid);

                // If there's no CoverArtModel, fetch it
                if (coverArt == null || coverArt.ShouldReFetch)
                {
                    // Set the background worker to retrieve the artwork
                    await _queue.EnqueueAsync(new CoverArtJob() {Mbid = mbid, ArtistMbid = artistMbid}, CancellationToken.None);
                }
                else
                {
                    // Add the response to cache
                    _memoryCache.Set(mbid, coverArt, coverArt.Exists ? TimeSpan.FromMinutes(5) : TimeSpan.FromDays(1));
                }
            }
            return coverArt;
        }

        public async Task<CoverArtModel[]> GetArtistCoverArt(Guid mbid)
        {
            var coverArts = await _dataService.GetCoverArtForArtist(mbid);
            var coverArtModels = coverArts as CoverArtModel[] ?? coverArts.ToArray();
            
            foreach (var coverArtModel in coverArtModels)
            {
                _memoryCache.Set(mbid, coverArtModel, TimeSpan.FromMinutes(5));
            }

            return coverArtModels;
        }
    }
}
