using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CitySDK.ServiceModel.Types;
using ServiceStack.ServiceHost;

namespace CitySDK.ServiceModel.Operations
{
    [Route("/routes", Verbs = "GET, POST, PUT, DELETE")]
    [Route("/routes/{Id}", Verbs = "GET, DELETE")]
    [Route("/routes/search/", Verbs = "GET")]
    //[Route("/routes/search/show/{Show}", Verbs = "GET")]
    [Route("/routes/search/show/{Offset}", Verbs = "GET")]
    [Route("/routes/search/show/{Limit}", Verbs = "GET")]
    [Route("/routes/search/category/{Category}", Verbs = "GET")]
    [Route("/routes/search/tag/{Tag}", Verbs = "GET")]
    [Route("/routes/search/search/{Name}", Verbs = "GET")]
    [Route("/routes/search/coords/{Coords}", Verbs = "GET")]
    public class Routes
    {
        [ApiMember(Name = "Route ID", ParameterType="Id", Description = "Returns a Route with the given Id.", DataType = "string", IsRequired = false)]
        public string Id { get; set; }

        // Removed by AO 17Mai13 according to WP2 guidelines - transformed in Offset and Limit
        //[ApiMember(Name = "Pagination", ParameterType = "Show", Description = "Limits the ammount of results returned. If not defined all results are returned.", DataType = "'A,B' qhere A = start result and B = end result. (e.g. 10,20)", IsRequired = false)]
        //public string Show { get; set; }

        [ApiMember(Name = "Pagination (offset)", ParameterType = "Offset", Description = "Introduces a pagination offset (skips the first <parameter> * <limit> results", DataType = "Number of the page", IsRequired = false)]
        public string Offset { get; set; }

        [ApiMember(Name = "Pagination (limit)", ParameterType = "Limit", Description = "Limits the ammount of results returned. If not defined 10 results are returned.", DataType = "Number of results of the page", IsRequired = false)]
        public string Limit { get; set; }

        [ApiMember(Name = "Category Filter", ParameterType = "Category", Description = "Searches all routes with the specified category.", DataType = "string", IsRequired = false)]
        public string Category { get; set; }

        [ApiMember(Name = "Tag Filter", ParameterType = "Tag", Description = "Searches all routes with the specified tag.", DataType = "string", IsRequired = false)]
        public string Tag { get; set; }

        [ApiMember(Name = "Search Filter", ParameterType = "Name", Description = "Searches all routes with the specified text in the name.", DataType = "string", IsRequired = false)]
        public string Name { get; set; }

        [ApiMember(Name = "Coordinates Filter", ParameterType = "Coords", Description = "Searches all routes with the specified coordinates.",
            DataType = "'x,y': Searches Routes with these exact coordinates. 'x,y,radius': Searches Routes within the radius. 'x1,y1 x2,y2': Searches Routes within the polygon.", IsRequired = false)]
        public string Coords { get; set; }

        [ApiMember(Name = "Route object", ParameterType = "route", Description = "This parameter is passed in the body content. It is used to Post or Put a route.",
            DataType = "Route JSON object.", IsRequired = false)]
        public route route { get; set; }
    }

    public class RoutesResponse
    {
        public List<route> routes { get; set; }
        public RoutesResponse()
        {
            routes = new List<route>();
        }
    }
}
