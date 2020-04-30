using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MashupApi.Models {
    public class MashupResponse
    {
        public Guid MBID { get; set; }
        public string Description { get; set; }
        public IEnumerable<Album> Albums { get; set; }
        internal int Images { get; set; }
    }
    public class Album
    {
        public Album()
        {
            
        }
        public Album(Models.MusicBrainz.Release release)
        {
            Title = release.Title;
            Id = release.Id;
        }
        public string Title { get; set; }
        public Guid Id { get; set; }
        public string Image { get; set; }
    }
}
