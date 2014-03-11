using MongoDB.Bson.Serialization.Attributes;

namespace CitySDK.ServiceModel.Types
{
    public class POITermType : POIBaseType
    {
        [BsonIgnoreIfNull]
        public string term { get; set; }

        [BsonIgnoreIfNull]
        public string scheme { get; set; }
    }
}
