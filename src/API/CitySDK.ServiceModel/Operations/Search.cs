using CitySDK.ServiceModel.Types;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitySDK.ServiceModel.Operations
{
  [Route("/search", Verbs = "GET")]
  [Route("/search/{Code}", Verbs = "GET")]
  public class Search
  {
    [ApiMember(Name = "Alternative Code", ParameterType = "Code", Description = "Returns a element with the alternative code", DataType = "string", IsRequired = false)]
    public string Code { get; set; }

  }

  public class SearchResponse
  {
    public List<poi> poi { get; set; }

    public List<@event> @event { get; set; }

    public List<route> route { get; set; }


    public SearchResponse()
    {
      //pois = new List<poi>();
      //events = new List<@event>();
      //routes = new List<route>();
    }
  }


}
