using System.Collections.Generic;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CitySDK.ServiceModel.Types
{
    public abstract class POIType : POIBaseType
    {
        [BsonIgnoreIfNull]
        public List<POITermType> label { get; set; }

        [BsonIgnoreIfNull]
        public List<POIBaseType> description { get; set; }

        [BsonIgnore]
        public List<category> category { get; set; }

        [BsonIgnore]
        public List<string> tags { get; set; }

        [BsonIgnoreIfNull]
        [IgnoreDataMember]
        public List<ObjectId> categoryIDs { get; set; }

        [BsonIgnoreIfNull]
        public List<POITermType> time { get; set; }

        [BsonIgnoreIfNull]
        public List<POITermType> link { get; set; }

        [BsonIgnoreIfNull]
        public string metadata { get; set; }
    }
}