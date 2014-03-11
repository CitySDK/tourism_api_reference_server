using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitySDK.ServiceModel.Types
{
  public class CitySDKResourceVersion
  {
    public string version
    {
      get;
      set;
    }

    public CitySDKResourceLink _links
    {
      get;
      set;
    }
  }
}
