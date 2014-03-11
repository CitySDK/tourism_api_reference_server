using System.Collections.Generic;
using CitySDK.ServiceModel.Types;
using ServiceStack.ServiceHost;

namespace CitySDK.ServiceModel.Operations
{
    [Route("/resources", Verbs = "GET")]
    public class Resources { }

    public class ResourcesResponse
    {
      /*
       public List<resource> _links { get; set; }
       public ResourcesResponse()
       {
           _links = new List<resource>();
       }
       */

      public CitySDKResources citysdk_tourism;

      public ResourcesResponse()
      {
        citysdk_tourism = new CitySDKResources();
      }
    }
}
