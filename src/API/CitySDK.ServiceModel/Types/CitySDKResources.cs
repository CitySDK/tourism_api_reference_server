using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CitySDK.ServiceModel.Types
{
  [DataContract]
  public class CitySDKResources
  {
    [DataMember(Name="citysdk-tourism")]
    public List<CitySDKResourceVersion> citysdk_tourism
    {
      get;
      set;
    }

    public CitySDKResources()
    {
      citysdk_tourism = new List<CitySDKResourceVersion>();
    }
  }
}
