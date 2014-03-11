using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CitySDK.ServiceModel.Types
{
  public class KmlFilesInfo
  {
    [BsonId]
    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    [IgnoreDataMember]
    public ObjectId? _id { get; set; }

    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    public string lisbonFile { get; set; }

    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    public string lisbonError { get; set; }

    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    public string lisbonLoaded { get; set; }

    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    public string fregFile { get; set; }

    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    public string fregError { get; set; }

    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    public string fregLoaded { get; set; }

    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    public string lastRun { get; set; }

    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    public string runCount { get; set; }
  }
}
