using System.Collections.Generic;
using CitySDK.ServiceModel.Types;
using ServiceStack.ServiceHost;

namespace CitySDK.ServiceModel.Operations
{
    [Route("/events", Verbs = "GET, DELETE, POST, PUT")]
    [Route("/events/{Id}", Verbs = "GET, DELETE")]
    [Route("/events/search/", Verbs = "GET")]
    //[Route("/events/search/show/{Show}", Verbs = "GET")]
    [Route("/events/search/show/{Limit}", Verbs = "GET")]
    [Route("/events/search/show/{Offset}", Verbs = "GET")]
    [Route("/events/search/category/{Category}", Verbs = "GET")]
    [Route("/events/search/tag/{Tag}", Verbs = "GET")]
    [Route("/events/search/search/{Name}", Verbs = "GET")]
    [Route("/events/search/time/{Time}", Verbs = "GET")]
    [Route("/events/search/coords/{Coords}", Verbs = "GET")]
    public class Events
    {
        [ApiMember(Name = "Event ID", ParameterType = "Id", Description = "Returns an Event with the given Id.", DataType = "string", IsRequired = false)]
        public string Id { get; set; }

        // Removed by AO 17Mai13 according to WP2 guidelines - transformed in Offset and Limit
        //[ApiMember(Name = "Pagination", ParameterType = "Show", Description = "Limits the ammount of results returned. If not defined all results are returned.", DataType = "'A,B' qhere A = start result and B = end result. (e.g. 10,20)", IsRequired = false)]
        //public string Show { get; set; }

        [ApiMember(Name = "Pagination (offset)", ParameterType = "Offset", Description = "Introduces a pagination offset (skips the first <parameter> * <limit> results", DataType = "Number of the page", IsRequired = false)]
        public string Offset { get; set; }

        [ApiMember(Name = "Pagination (limit)", ParameterType = "Limit", Description = "Limits the ammount of results returned. If not defined 10 results are returned.", DataType = "Number of results of the page", IsRequired = false)]
        public string Limit { get; set; }

        [ApiMember(Name = "Category Filter", ParameterType = "Category", Description = "Searches all events with the specified category.", DataType = "string", IsRequired = false)]
        public string Category { get; set; }

        [ApiMember(Name = "Tag Filter", ParameterType = "Tag", Description = "Searches all events with the specified tag.", DataType = "string", IsRequired = false)]
        public string Tag { get; set; }

        [ApiMember(Name = "Name Filter", ParameterType = "Name", Description = "Searches all events with the specified name.", DataType = "string", IsRequired = false)]
        public string Name { get; set; }

        [ApiMember(Name = "Time Filter", ParameterType = "Search", Description = "Searches all events with the specified schedule.", DataType = "string", IsRequired = false)]
        public string Time { get; set; }

        [ApiMember(Name = "Coordinates Filter", ParameterType = "Coords", Description = "Searches all events with the specified coordinates.",
    DataType = "'x,y': Searches events with these exact coordinates. 'x,y,radius': Searches events within the radius. 'x1,y1 x2,y2': Searches events within the polygon.", IsRequired = false)]
        public string Coords { get; set; }

        [ApiMember(Name = "Event object", ParameterType = "event", Description = "This parameter is passed in the body content. It is used to Post or Put an event.",
    DataType = "Event JSON object.", IsRequired = false)]
        public @event @event { get; set; }
    }

    public class EventsResponse
    {
        public List<@event> @event { get; set; }
        public EventsResponse()
        {
            @event = new List<@event>();
        }
    }
}
