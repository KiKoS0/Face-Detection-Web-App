using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using FaceDetectionWeb.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceDetectionWeb.Services
{
    public class ImageService
    {

        private readonly IMongoCollection<Image> _images;

        public ImageService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetConnectionString("ImageDb"));
            var database = client.GetDatabase("ImageDb");
            _images = database.GetCollection<Image>("Images");
        }

        public List<Image> Get()
        {
            return _images.Find(image => true).ToList();
        }

        public Image Get(string id)
        {
            return _images.Find<Image>(image => image.Id == id).FirstOrDefault();
        }

        public async Task<List<Image>> GetAsync()
        {
            var found = await _images.FindAsync(Image => true);
            return await found.ToListAsync();
        }

        public async Task<Image> GetAsync(string id)
        {
            var found = await _images.FindAsync<Image>(image => image.Id == id);
            return await found.FirstOrDefaultAsync();
        }

        public Image Create(Image image)
        {
            _images.InsertOne(image);
            return image;
        }

        public async Task<Image> CreateAsync(Image image)
        {
            await _images.InsertOneAsync(image);
            return image;
        }
    }
}
