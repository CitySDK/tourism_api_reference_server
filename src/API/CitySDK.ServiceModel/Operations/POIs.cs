using System.Collections.Generic;
using CitySDK.ServiceModel.Types;
using ServiceStack.ServiceHost;

namespace CitySDK.ServiceModel.Operations
{
    [Route("/pois", Verbs = "GET, DELETE, POST, PUT")]
    [Route("/pois/{Id}", Verbs = "GET, DELETE")]
    [Route("/pois/{Id}/search/", Verbs = "GET")]
    [Route("/pois/{Id}/search/relation/{Relation}", Verbs = "GET")]
    [Route("/pois/search/", Verbs = "GET")]
    //[Route("/pois/search/show/{Show}", Verbs = "GET")]
    [Route("/pois/search/show/{Limit}", Verbs = "GET")]
    [Route("/pois/search/show/{Offset}", Verbs = "GET")]
    [Route("/pois/search/category/{Category}", Verbs = "GET")]
    [Route("/pois/search/tag/{Tag}", Verbs = "GET")]
    [Route("/pois/search/minimal/{Minimal}", Verbs = "GET")]
    [Route("/pois/search/complete/{Complete}", Verbs = "GET")]
    [Route("/pois/search/coords/{Coords}", Verbs = "GET")]
    [Route("/pois/search/relation/{Relation}", Verbs = "GET")]
    [Route("/pois/search/deleted/{Deleted}", Verbs = "GET")]
    public class POIs
    {
        [ApiMember(Name = "POI ID", ParameterType = "Id", Description = "Returns a POI with the given Id.", DataType = "string", IsRequired = false)]
        public string Id { get; set; }

        // Removed by AO 17Mai13 according to WP2 guidelines - transformed in Offset and Limit
        //[ApiMember(Name = "Pagination (offset)", ParameterType = "Show", Description = "Limits the ammount of results returned. If not defined all results are returned.", DataType = "'A,B' qhere A = start result and B = end result. (e.g. 10,20)", IsRequired = false)]
        //public string Show { get; set; }

        [ApiMember(Name = "Pagination (offset)", ParameterType = "Offset", Description = "Introduces a pagination offset (skips the first <parameter> * <limit> results", DataType = "Number of the page", IsRequired = false)]
        public string Offset { get; set; }

        [ApiMember(Name = "Pagination (limit)", ParameterType = "Limit", Description = "Limits the ammount of results returned. If not defined 10 results are returned.", DataType = "Number of results of the page", IsRequired = false)]
        public string Limit { get; set; }

        [ApiMember(Name = "Category Filter", ParameterType = "Category", Description = "Searches all POIs with the specified category.", DataType = "string", IsRequired = false)]
        public string Category { get; set; }

        [ApiMember(Name = "Tag Filter", ParameterType = "Tag", Description = "Searches all POIs with the specified tag.", DataType = "string", IsRequired = false)]
        public string Tag { get; set; }

        [ApiMember(Name = "Minimal Search Filter", ParameterType = "Minimal", Description = "Searches all POIs with the specified text and returns the results with minimal information.", DataType = "string", IsRequired = false)]
        public string Minimal { get; set; }

        [ApiMember(Name = "Complete Search Filter", ParameterType = "Complete", Description = "Searches all POIs with the specified text and returns the results with complete information.", DataType = "string", IsRequired = false)]
        public string Complete { get; set; }

        [ApiMember(Name = "Relation Filter", ParameterType = "Relation", Description = "Searches all POIs with the specified relation.", DataType = "string", IsRequired = false)]
        public string Relation { get; set; }

        [ApiMember(Name = "Deleted POIs Filter", ParameterType = "Deleted", Description = "Shows also the deleted POIs retrieved from the DB", DataType = "string", IsRequired = false)]
        public string Deleted { get; set; }

        [ApiMember(Name = "Coordinates Filter", ParameterType = "Coords", Description = "Searches all POIs with the specified coordinates.",
    DataType = "'x,y': Searches POIs with these exact coordinates. 'x,y,radius': Searches POIs within the radius. 'x1,y1 x2,y2': Searches POIs within the polygon.", IsRequired = false)]
        public string Coords { get; set; }

        [ApiMember(Name = "POI object", ParameterType = "poi", Description = "This parameter is passed in the body content. It is used to Post or Put a POI.",
            DataType = "POI JSON object.", IsRequired = false)]
        public poi poi { get; set; }
    }

    public class POIResponse
    {
        public poi poi { get; set; }
    }

    public class POISResponse
    {
        public List<poi> poi { get; set; }
        public POISResponse()
        {
            poi = new List<poi>();
        }
    }
}
