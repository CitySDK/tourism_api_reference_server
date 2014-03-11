using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitySDK.ServiceModel.Operations;
using CitySDK.ServiceModel.Types;
using ServiceStack.ServiceInterface;
using System.Configuration;

namespace CitySDK.ServiceInterface
{
  public class ResourcesService : AService
  {
    public object Get(Resources request)
    {
      //const string b = "http://tourism.citysdk.cm-lisboa.pt/";
      string b = ConfigurationManager.AppSettings["BaseURL"];

      #region Old Resources (Disabled)
      /*
      ResourcesResponse response = new ResourcesResponse {_links = new List<resource>()};

            

      response._links.Add(new resource { term = "poi-category", @base = string.Concat(b, "pois/search"), id = "category" });
      response._links.Add(new resource { term = "poi-tags", @base = string.Concat(b, "pois/search"), id = "tag" });
      response._links.Add(new resource { term = "poi-complete", @base = string.Concat(b, "pois/search"), id = "complete" });
      response._links.Add(new resource { term = "poi-minimal", @base = string.Concat(b, "pois/search"), id = "minimal" });
      response._links.Add(new resource { term = "poi-relation", @base = string.Concat(b, "pois/search"), id = "relation" });
      response._links.Add(new resource { term = "poi-coords", @base = string.Concat(b, "pois/search"), id = "coords" });
      response._links.Add(new resource { term = "poi-page", @base = string.Concat(b, "pois/search"), id = "show" });

      response._links.Add(new resource { term = "event-category", @base = string.Concat(b, "events/search"), id = "category" });
      response._links.Add(new resource { term = "event-tags", @base = string.Concat(b, "events/search"), id = "tag" });
      response._links.Add(new resource { term = "event", @base = string.Concat(b, "events/search"), id = "search" });
      response._links.Add(new resource { term = "event-time", @base = string.Concat(b, "events/search"), id = "time" });
      response._links.Add(new resource { term = "event-coords", @base = string.Concat(b, "events/search"), id = "coords" });
      response._links.Add(new resource { term = "event-page", @base = string.Concat(b, "events/search"), id = "show" });

      response._links.Add(new resource { term = "route-category", @base = string.Concat(b, "routes/search"), id = "category" });
      response._links.Add(new resource { term = "route-tags", @base = string.Concat(b, "routes/search"), id = "tag" });
      response._links.Add(new resource { term = "route", @base = string.Concat(b, "routes/search"), id = "search" });
      response._links.Add(new resource { term = "route-coords", @base = string.Concat(b, "routes/search"), id = "coords" });
      response._links.Add(new resource { term = "route-page", @base = string.Concat(b, "routes/search"), id = "show" });

      response._links.Add(new resource { term = "categories", @base = string.Concat(b, "categories"), id = "list" });
      response._links.Add(new resource { term = "categories-page", @base = string.Concat(b, "categories"), id = "show" });
      */
      #endregion

      return BuildResources(b);
    }

    private CitySDKResources BuildResources(string baseUrl)
    {

      bool skipPOIs = false;
      bool skipEvents = false;
      bool skipRoutes = false;

      try
      {
        MongoDB.Driver.CollectionStatsResult tmp = MongoDb.POIs.GetStats();
      }
      catch
      {
        skipPOIs = true;
      }

      try
      {
        MongoDB.Driver.CollectionStatsResult tmp = MongoDb.Events.GetStats();
      }
      catch
      {
        skipEvents = true;
      }

      try
      {
        MongoDB.Driver.CollectionStatsResult tmp = MongoDb.Routes.GetStats();
      }
      catch
      {
        skipRoutes = true;
      }
      return new CitySDKResources()
      {
        citysdk_tourism = new List<CitySDKResourceVersion>()
        {
          new CitySDKResourceVersion()
          {
              version = "1.0",
              _links =  new CitySDKResourceLink()
              {
                find_poi = skipPOIs ? null : new CitySDKResourceElement()
                {
                  href = string.Concat(baseUrl,"pois/search{?category,tag,complete,minimal,coords,limit,offset}"),
                  templated = "true"
                },
                find_poi_relation = skipPOIs ? null : new CitySDKResourceElement()
                {
                  href = string.Concat(baseUrl,"pois/{id}/search{?relation}"),
                  templated = "true"
                },
                find_event = skipEvents ? null : new CitySDKResourceElement()
                {
                  href = string.Concat(baseUrl,"events/search{?category,tag,name,coords,limit,offset,time}"),
                  templated = "true"
                },
                find_event_relation = skipEvents ? null : new CitySDKResourceElement()
                {
                  
                  href = string.Concat(baseUrl,"events/{id}/search{?relation}"),
                  templated = "true"
                },
                find_route = skipRoutes ? null : new CitySDKResourceElement()
                {
                  href = string.Concat(baseUrl,"routes/search{?category,tag,name,coords,limit,offset}"),
                  templated = "true"
                },
                find_categories = new CitySDKResourceElement()
                {
                  href = string.Concat(baseUrl,"categories/search{?list,limit,offset}"),
                  templated = "true"
                },
                find_code = new CitySDKResourceElement()
                {
                  href = string.Concat(baseUrl,"search{?code}"),
                  templated = "true"
                }
                /*
                find_tags = new CitySDKResourceElement()
                {
                  href = string.Concat(b,"tags/search{?list,show}"),
                  templated = "true"
                } 
                */
              }
          }
        }
      };
    }
  }
}
