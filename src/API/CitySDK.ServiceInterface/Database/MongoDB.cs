using System;
using System.Configuration;
using CitySDK.ServiceModel.Types;
using MongoDB.Driver;

namespace CitySDK.ServiceInterface
{
    public interface IMongoDB
    {
        MongoCollection<poi> POIs { get; set; }
        MongoCollection<route> Routes { get; set; }
        MongoCollection<@event> Events { get; set; }
        MongoCollection<category> POICategories { get; set; }
        MongoCollection<category> RouteCategories { get; set; }
        MongoCollection<category> EventCategories { get; set; }
        MongoCollection<KmlFilesInfo> KmlInfo { get; set; }
    }

    public class MongoDb : IMongoDB
    {
        public MongoCollection<poi> POIs { get; set; }
        public MongoCollection<route> Routes { get; set; }
        public MongoCollection<@event> Events { get; set; }
        public MongoCollection<category> POICategories { get; set; }
        public MongoCollection<category> RouteCategories { get; set; }
        public MongoCollection<category> EventCategories { get; set; }
        public MongoCollection<KmlFilesInfo> KmlInfo { get; set; }

        public MongoDb()
        {
            var server = new MongoClient(ConfigurationManager.AppSettings["MongoServer"]).GetServer();

            var db = server.GetDatabase(ConfigurationManager.AppSettings["MongoDatabase"]);

            EventCategories = db.GetCollection<category>("event_categories");
            POICategories = db.GetCollection<category>("poi_categories");
            RouteCategories = db.GetCollection<category>("route_categories");

            POIs = db.GetCollection<poi>("pois");
            Events = db.GetCollection<@event>("events");
            Routes = db.GetCollection<route>("routes");
            KmlInfo = db.GetCollection<KmlFilesInfo>("kml_info");
            
        }
    }
}
