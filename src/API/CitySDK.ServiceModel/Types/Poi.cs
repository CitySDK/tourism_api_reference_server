using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using ServiceStack.ServiceHost;

namespace CitySDK.ServiceModel.Types
{
    public class poi : POIType
    {
        [IgnoreDataMember]
        [BsonIgnoreIfNull]
        public string sourceID { get; set; }

        [BsonIgnoreIfNull]
        public location location { get; set; }
    }
}