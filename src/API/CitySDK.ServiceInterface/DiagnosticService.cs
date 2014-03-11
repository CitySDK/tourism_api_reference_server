using System;
using System.Collections.Generic;
using CitySDK.ServiceModel.Operations;
using MongoDB.Driver;
using ServiceStack.Text;
using CitySDK.ServiceModel.Types;

namespace CitySDK.ServiceInterface
{
    public class DiagnosticService : AService
    {
        public object Any(Diagnostic diag)
        {
            DiagnosticResponse response = new DiagnosticResponse();

            try
            {
              try
              {
                response.EventStats = GetStat(MongoDb.Events.GetStats());
              }
              catch
              {
                response.EventStats = new Dictionary<string, string>() {{ "Summary", "Events DB not created yet" }};
              }

              try
              {
                response.EventCategoryStats = GetStat(MongoDb.EventCategories.GetStats());
              }
              catch
              {
                response.EventCategoryStats = new Dictionary<string, string>() {{ "Summary", "Event categories DB not created yet" }};
              }

              try
              {
                response.POICategoryStats = GetStat(MongoDb.POICategories.GetStats());
              }
              catch
              {
                response.POICategoryStats = new Dictionary<string, string>() {{ "Summary", "POI categories DB not created yet" }};
              }

              try
              {
                response.POIStats = GetStat(MongoDb.POIs.GetStats());
              }
              catch
              {
                response.POIStats = new Dictionary<string, string>() {{ "Summary", "POI DB not created yet" }};
              }

              try
              {
                response.RouteCategoryStats = GetStat(MongoDb.RouteCategories.GetStats());
              }
              catch
              {
                response.RouteCategoryStats = new Dictionary<string, string>() {{ "Summary", "Route categories DB not created yet" }};
              }

              try
              {
                response.RouteStats = GetStat(MongoDb.Routes.GetStats());
              }
              catch
              {
                response.RouteStats = new Dictionary<string, string>() {{ "Summary", "Routes DB not created yet" }};
              }

              try
              {
                response.KMLFileStats = GetKMLStats();
              }
              catch
              {
                response.KMLFileStats = new Dictionary<string, string>() {{ "Summary", "KML Stats DB not created yet" }};
              }                
            }
            catch (Exception e)
            {
                return string.Concat("MongoDB Connection: Error - ", e.ToString());
            }

            return response;
        }

        public Dictionary<string,string> GetStat(CollectionStatsResult stat)
        {
            var json = new Dictionary<string, string>();

            json.Add("AverageObjectSize", stat.AverageObjectSize.ToString());

            json.Add("DataSize", stat.DataSize.ToString());
            json.Add("ErrorMessage", stat.ErrorMessage ?? "");
            json.Add("ExtentCount", stat.ExtentCount.ToString());
            json.Add("IndexCount", stat.IndexCount.ToString());
            json.Add("IndexSizes", stat.IndexSizes.ToJson());
            json.Add("IsCapped", stat.IsCapped.ToString());
            json.Add("LastExtentSize", stat.LastExtentSize.ToString());
            json.Add("MaxDocuments", stat.MaxDocuments.ToString());
            json.Add("Namespace", stat.Namespace ?? "");
            json.Add("ObjectCount", stat.ObjectCount.ToString());
            json.Add("Ok", stat.Ok.ToString());
            json.Add("PaddingFactor", stat.PaddingFactor.ToString());
            json.Add("StorageSize", stat.StorageSize.ToString());
            json.Add("TotalIndexSize", stat.TotalIndexSize.ToString());
            json.Add("UserFlags", stat.UserFlags.ToString());
            json.Add("SystemFlags", stat.SystemFlags.ToString());

            return json;
        }

        public Dictionary<string, string> GetKMLStats()
        {
          MongoDb MongoDb_local = new MongoDb();

          Dictionary<string, string> json = new Dictionary<string, string>();
          KmlFilesInfo kmlInfo = MongoDb_local.KmlInfo.FindOne();

          if(kmlInfo == null)
          {
            json.Add("Info","Debug Info not found");
            return json;
          }

          json.Add("Lisbon file", kmlInfo.lisbonFile);
          json.Add("Lisbon error", kmlInfo.lisbonError);
          json.Add("Lisbon loaded", kmlInfo.lisbonLoaded);

          json.Add("Parishes file", kmlInfo.fregFile);
          json.Add("Parishes error", kmlInfo.fregError);
          json.Add("Parishes loaded", kmlInfo.fregLoaded);

          json.Add("Importer runs", kmlInfo.runCount);
          json.Add("Last run", kmlInfo.lastRun);

          return json;
        }
    }
}
