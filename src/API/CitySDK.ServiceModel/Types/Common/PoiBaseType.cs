using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CitySDK.ServiceModel.Types
{
    public class POIBaseType
    {
        [BsonId]
        [BsonIgnoreIfNull]
        [BsonIgnoreIfDefault]
        [IgnoreDataMember]
        public ObjectId? _id { get; set; }

        [IgnoreDataMember]
        public string strId { get; set; }

        [BsonIgnore]
        public string id
        {
            get
            {
              if (_id != null && _id != ObjectId.Empty)
              {
                return _id.ToString();
              }
              else
                return strId;

                //return null;
            }
            set
            {
                ObjectId idd;
                if (ObjectId.TryParse(value, out idd))
                  _id = idd;
                else
                  strId = value;

            }
        }

        [BsonIgnoreIfNull]
        public string href { get; set; }

        [BsonIgnoreIfNull]
        public string value { get; set; }

        [BsonIgnoreIfNull]
        public string @base { get; set; }

        [BsonIgnoreIfNull]
        public string type { get; set; }

        [BsonIgnoreIfNull]
        public string lang { get; set; }

        [BsonIgnoreIfNull]
        public System.DateTime? updated { get; set; }

        [BsonIgnoreIfNull]
        public System.DateTime? created { get; set; }

        [BsonIgnoreIfNull]
        public System.DateTime? deleted { get; set; }

        [BsonIgnoreIfNull]
        public POITermType author { get; set; }

        [BsonIgnoreIfNull]
        public POITermType license { get; set; }
    }
}