using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CitySDK.ServiceModel.Types
{
  [DataContract]
  public class CitySDKResourceLink
  {
    [DataMember(Name="find-poi")]
    public CitySDKResourceElement find_poi
    {
      get;
      set;
    }

    [DataMember(Name = "find-poi-relation")]
    public CitySDKResourceElement find_poi_relation
    {
      get;
      set;
    }

    [DataMember(Name = "find-event")]
    public CitySDKResourceElement find_event
    {
      get;
      set;
    }

    [DataMember(Name = "find-event-relation")]
    public CitySDKResourceElement find_event_relation
    {
      get;
      set;
    }

    [DataMember(Name = "find-route")]
    public CitySDKResourceElement find_route
    {
      get;
      set;
    }

    [DataMember(Name = "find-categories")]
    public CitySDKResourceElement find_categories
    {
      get;
      set;
    }

    [DataMember(Name = "find-tags")]
    public CitySDKResourceElement find_tags
    {
      get;
      set;
    }

    [DataMember(Name = "find-code")]
    public CitySDKResourceElement find_code
    {
      get;
      set;
    }
  }
}
