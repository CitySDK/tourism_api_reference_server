using MongoDB.Bson;

namespace CitySDK.ServiceModel.Types
{
    public class relationship : POITermType
    {
        public ObjectId targetPOI { get; set; }
    }
}
