using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MashupApi.Models.MusicBrainz;
using MashupApi.Http.Clients;
using MashupApi.Http.Clients.Exceptions;
using MashupApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MashupApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArtistController
    {
        private readonly MusicBrainzClient _musicBrainzClient;
        private readonly WikiDataClient _wikiDataClient;
        private readonly WikipediaClient _wikipediaClient;
        private readonly CoverArtService _coverArtService;
        private readonly ILogger<ArtistController> _logger;

        public ArtistController(MusicBrainzClient musicBrainzClient,
                                WikiDataClient wikiDataClient,
                                WikipediaClient wikipediaClient,
                                CoverArtService coverArtService,
                                ILogger<ArtistController> logger)
        {
            _musicBrainzClient = musicBrainzClient;
            _wikiDataClient = wikiDataClient;
            _wikipediaClient = wikipediaClient;
            _coverArtService = coverArtService;
            _logger = logger;
        }

        [Route("albums/{mbid}")]
        public async Task<ActionResult<MashupResponse>> GetMashupResult(Guid mbid)
        {
            _logger.LogInformation("Retrieving Mashup data for {mbid}", mbid);
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var artistData = await _musicBrainzClient.GetArtist(mbid);
                _logger.LogDebug("Got artist in {Elapsed}, getting titles.", sw.Elapsed);
                sw.Restart();
                
                var wikipediaTitle = await _wikiDataClient.GetTitle(artistData.WikimediaIdentifier);
                _logger.LogDebug("Got wikimedia data in {Elapsed}, getting wikipedia.", sw.Elapsed);
                sw.Restart();

                var wikipediaExcerpt = await _wikipediaClient.Get(wikipediaTitle);
                _logger.LogDebug("Got wikipedia reference in {Elapsed}", sw.Elapsed);
                sw.Restart();

                // Grab all artist artworks
                var artworks = await _coverArtService.GetArtistCoverArt(mbid);
                var artworkIds = artworks.Select(a => a.Mbid);
                
                // Get all artist's albums not in local db cache
                var albumsToFetchArtworkFor = artistData.Albums.Where(album => !artworkIds.Contains(album.Id));

                foreach (var artistDataAlbum in albumsToFetchArtworkFor)
                {
                    _logger.LogInformation("Album {mbid} not in local database.");
                    // Add album to fetch
                    await _coverArtService.GetOrQueueCoverArt(artistDataAlbum.Id, mbid);
                }
                
                
                sw.Stop();

                // Convert the artworks to a dictionary for easier access
                var images = artworks
                    .ToDictionary(a => a.Mbid, 
                        a => a.ImageUrl);

                return new JsonResult(new MashupResponse
                {
                    MBID = mbid,
                    Description = wikipediaExcerpt,
                    Albums = artistData.Albums.Select(album => new Album(album) {
                        Image = images.FirstOrDefault(i => i.Key == album.Id).Value
                    })
                });
            }
            catch (MusicBrainzNotFoundException ex)
            {
                _logger.LogError(ex, "Artist does not exist.");
                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving artist data.");
                return new StatusCodeResult(500);
            }
        }
    }
}
