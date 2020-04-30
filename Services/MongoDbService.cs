using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MashupApi.Models;
using MongoDB.Driver;

namespace MashupApi.Services
{
    public class MongoDbService
    {
        private IMongoDatabase Database => _mongoClient.GetDatabase("mashup");
        private readonly string _collectionName = "coverArt";
        private readonly IMongoClient _mongoClient;
        private readonly IMongoCollection<CoverArtModel> _collection;

        public MongoDbService(IMongoClient mongoClient)
        {
            _mongoClient = mongoClient;
            _collection = Database.GetCollection<CoverArtModel>(_collectionName);
        }

        public async Task<CoverArtModel> GetCoverArt(Guid id)
        {
            var cursor = await _collection.FindAsync(cover => cover.Mbid == id);
            
            return cursor.FirstOrDefault();
        }

        public async Task SaveCoverArt(CoverArtModel model)
        {
            var existingCoverArt = await GetCoverArt(model.Mbid);
            
            // Check if a CoverArt already exists, if so, replace it
            if (existingCoverArt == null)
                await InsertCoverArt(model);
            else
                await UpdateCoverArt(existingCoverArt);

        }

        private async Task UpdateCoverArt(CoverArtModel model)
        {
            var filter = Builders<CoverArtModel>.Filter.Eq(x => x.Mbid, model.Mbid);
            
            await _collection.ReplaceOneAsync(filter, model);
        }

        private async Task InsertCoverArt(CoverArtModel model)
        {
            await _collection.InsertOneAsync(model);
        }

        public async Task<IEnumerable<CoverArtModel>> GetCoverArtForArtist(Guid mbid)
        {
            var cursor = await _collection.FindAsync(x => x.ArtistMbid == mbid);
            return cursor.ToEnumerable();
        }
    }
}