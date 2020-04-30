using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MashupApi.Models
{
    public class CoverArtModel
    {
        [BsonId]
        [BsonElement("_id")]
        public BsonObjectId Id { get; set; }
        
        [BsonElement("mbid")]
        public Guid Mbid { get; set; }
        
        [BsonElement("artistMbid")] 
        public Guid ArtistMbid { get; set; }

        [BsonElement("imageUrl")]
        public string ImageUrl { get; set; }
        
        [BsonElement("fetchDate")]
        public DateTime FetchDate { get; set; }
        
        [BsonElement("statusCode")]
        public int StatusCode { get; set; }
        
        // Convenience property
        public virtual bool Exists => StatusCode == 200;
        public virtual bool ShouldReFetch => !Exists && DateTime.Compare(FetchDate.AddDays(1), DateTime.Now) == -1;
    }
}