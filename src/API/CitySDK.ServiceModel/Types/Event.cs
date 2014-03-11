using System;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace CitySDK.ServiceModel.Types
{
    public class @event : POIType
    {
        [IgnoreDataMember]
        [BsonIgnoreIfNull]
        public string sourceID { get; set; }

        [IgnoreDataMember]
        public DateTime? Start { get; set; }

        [IgnoreDataMember]
        public DateTime? End { get; set; }

        [BsonIgnoreIfNull]
        public location location { get; set; }
    }
}