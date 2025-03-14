using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DigiLimbDesktop.Models
{
    public class Device
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        [BsonElement("deviceModel")]
        public string DeviceModel { get; set; }

        [BsonElement("manufacturer")]
        public string Manufacturer { get; set; }

        [BsonElement("platform")]
        public string Platform { get; set; }

        [BsonElement("osVersion")]
        public string OsVersion { get; set; }

        [BsonElement("deviceType")]
        public string DeviceType { get; set; }

        [BsonElement("macAddress")]
        public string MacAddress { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("idiom")]
        public string Idiom { get; set; }
    }
}
