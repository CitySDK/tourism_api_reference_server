using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitySDK.ServiceModel.Operations;
using MongoDB.Bson;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Common;

namespace CitySDK.ServiceInterface.Validators
{
    public class POIsValidator : Validators.IValidator<POIs>
    {
        public IMongoDB MongoDB { get; set; }
        public bool Validate(string verb, POIs obj, out string msg)
        {
            if (MongoDB == null)
                MongoDB = EndpointHost.Config.ServiceManager.Container.TryResolve<IMongoDB>();

            msg = string.Empty;

            if (!obj.Id.IsNullOrEmpty())
            {
                ObjectId id;
                if (!ObjectId.TryParse(obj.Id, out id))
                {
                    msg = "'Id' parameter is invalid.";
                    return false;
                }

                return true;
            }



            return true;
        }
    }
}
