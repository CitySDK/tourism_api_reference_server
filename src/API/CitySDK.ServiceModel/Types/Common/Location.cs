using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;

namespace CitySDK.ServiceModel.Types
{
    /// <summary>
    /// Descrbes POI localization
    /// </summary>
    public class location : POIBaseType
    {
        [BsonIgnoreIfNull]
        public List<point> point { get; set; }

        [BsonIgnoreIfNull]
        public List<line> line { get; set; }

        [BsonIgnoreIfNull]
        public List<polygon> polygon { get; set; }

        [BsonIgnoreIfNull]
        public POIBaseType address { get; set; }

        [BsonIgnoreIfNull]
        public string undetermined { get; set; }

        [BsonIgnoreIfNull]
        public List<relationship> relationship { get; set; }

        [BsonIgnoreIfNull]
        [IgnoreDataMember]
        public List<MongoDB.Bson.BsonDocument> GeoJson { get; set; }
    }


    //public class GeoJSONPolygon
    //{
    //    public GeoJSONPolygon()
    //    {
    //        type = "Polygon";
    //    }

    //    public string type { get; private set; }

    //    public double[][][] coordinates { get; set; }
    //}

    //public class GeoJSONPoint
    //{
    //    public GeoJSONPoint()
    //    {
    //        type = "Point";
    //    }

    //    public string type { get; private set; }

    //    public double[] coordinates { get; set; }
    //}
}