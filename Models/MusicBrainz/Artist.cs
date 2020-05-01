using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MashupApi.Models.MusicBrainz
{
    public class Artist
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("life-span")]
        public LifeSpan LifeSpan { get; set; }
        [JsonPropertyName("release-groups")]
        public IEnumerable<Release> Releases { get; set; }
        [JsonPropertyName("relations")]
        public IEnumerable<Relation> Relations { get; set; }
        public virtual RelationUrl WikidataUrl => Relations.FirstOrDefault(r => r.Type == "wikidata")?.Url;
        public virtual RelationUrl WikipediaUrl => Relations.FirstOrDefault(r => r.Type == "wikipedia")?.Url;
        public virtual string WikimediaIdentifier => WikidataUrl?.Resource.Segments.LastOrDefault();
        public virtual IEnumerable<Release> Albums => Releases.Where(r => r.Type == "Album");//.AsEnumerable();
    }
    public class Release
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("primary-type")]
        public string Type { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
    }
    public class LifeSpan
    {
        [JsonPropertyName("ended")]
        public bool Ended { get; set; }
        // End & Begin stored as strings as they're not always full DateTime
        [JsonPropertyName("end")]
        public string End { get; set; }
        [JsonPropertyName("begin")]
        public string Begin { get; set; }
    }
    public class Relation
    {
        [JsonPropertyName("direction")]
        public string Direction { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("url")]
        public RelationUrl Url { get; set; }
    }
    public class RelationUrl
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("resource")]
        public Uri Resource { get; set; }
    }

    /*

{
"end": null,
"target-credit": "",
"attribute-ids": {},
"attributes": [],
"direction": "forward",
"type": "allmusic",
"url": {
"resource": "https://www.allmusic.com/artist/mn0000357406",
"id": "4a425cd3-641d-409c-a282-2334935bf1bd"
},
"target-type": "url",
"ended": false,
"type-id": "6b3e3c85-0002-4f34-aca6-80ace0d7e846",
"attribute-values": {},
"source-credit": "",
"begin": null
},
    */
}
