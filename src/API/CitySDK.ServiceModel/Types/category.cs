using System.Collections.Generic;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ServiceStack.ServiceHost;

namespace CitySDK.ServiceModel.Types
{
    public class category : POITermType
    {
        [IgnoreDataMember]
        [BsonIgnoreIfNull]
        public string sourceID { get; set; }

        [BsonIgnore]
        public List<category> categories { get; set; }

        [BsonIgnoreIfNull]
        [IgnoreDataMember]
        public List<ObjectId> categoryIDs { get; set; }

        [BsonIgnoreIfNull]
        public List<POITermType> label { get; set; }

        [BsonIgnoreIfNull]
        public List<POITermType> link { get; set; }
    }
}
