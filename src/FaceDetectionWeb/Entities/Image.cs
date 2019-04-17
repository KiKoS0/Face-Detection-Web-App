using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;


using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FaceDetectionWeb.Entities
{
    public class Image
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("Title")]
        public string Title { get; set; }

        [BsonElement("AltText")]
        public string AltText { get; set; }

        [BsonElement("Caption")]
        [DataType(DataType.Html)]
        public string Caption { get; set; }

        [BsonRequired]
        [BsonElement("ImageUrl")]
        public string ImageUrl { get; set; }

        private DateTime? createdDate;

        [BsonElement("CreatedDate")]
        [BsonRequired]
        [BsonDateTimeOptions]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDate
        {
            get { return createdDate ?? DateTime.UtcNow; }
            set { createdDate = value; }
        }
        [BsonElement("FaceDetect")]
        [BsonRequired]
        [BsonDefaultValue(false)]
        public bool FaceDetect { get; set; }
        [BsonElement("FaceDetectDone")]
        [BsonDefaultValue(false)]
        public bool FaceDetectDone { get; set; }

        [BsonElement("lastModified")]
        public DateTime LastModified {get;set;}

    }
}
