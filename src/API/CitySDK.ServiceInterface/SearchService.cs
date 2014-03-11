using CitySDK.ServiceModel.Operations;
using CitySDK.ServiceModel.Types;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitySDK.ServiceInterface
{
  public class SearchService : AService
  {
    public object Get(Search request)
    {
      SearchResponse searchResponse = new SearchResponse();
     
      if (!string.IsNullOrEmpty(request.Code))
      {
        IMongoQuery searchQuery = Query.And(Query.EQ("link.term", "alternative"), Query.EQ("link.value",request.Code));

        foreach (poi p in MongoDb.POIs.Find(searchQuery))
        {
          if (searchResponse.poi == null)
            searchResponse.poi = new List<poi>();

          poi finalPoi = POIService.BuildPOI(MongoDb.POICategories, p, false);

          if (finalPoi != null)
            searchResponse.poi.Add(finalPoi);
        }

        foreach (@event e in MongoDb.Events.Find(searchQuery))
        {
          if (searchResponse.@event == null)
            searchResponse.@event = new List<@event>();

          @event finalEvent = EventService.BuildEvent(MongoDb.EventCategories, MongoDb.POIs, e);

          if (finalEvent != null)
            searchResponse.@event.Add(finalEvent);
        }

        foreach (route r in MongoDb.Routes.Find(searchQuery))
        {
          if (searchResponse.route == null)
            searchResponse.route = new List<route>();

          route finalRoute = RouteService.BuildRoute(MongoDb.RouteCategories, MongoDb.POIs, r, false);

          if (finalRoute != null)
            searchResponse.route.Add(finalRoute);
        }
      }

      return searchResponse;
    }
  }
}
