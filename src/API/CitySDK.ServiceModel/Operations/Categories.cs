using System.Collections.Generic;
using CitySDK.ServiceModel.Types;
using ServiceStack.ServiceHost;

namespace CitySDK.ServiceModel.Operations
{
    [Route("/categories/", Verbs = "GET, POST, PUT, DELETE")]
    [Route("/categories/{List}", Verbs = "GET, POST, PUT")]
    //[Route("/categories/{List}/{Show}", Verbs = "GET")]
    [Route("/categories/{List}/{Limit}", Verbs = "GET")]
    [Route("/categories/{List]/{Offset}", Verbs = "GET")]
    [Route("/categories/{List}/{Id}", Verbs = "DELETE")]
    public class Categories
    {
        [ApiMember(Name = "List Type", ParameterType = "List", Description = "Defines the type of categories to be returned.", DataType = "Values must be: 'poi', 'route' or 'event'.", IsRequired = false)]
        public string List { get; set; }

        //[ApiMember(Name = "Pagination", ParameterType = "Show", Description = "Limits the ammount of results returned. If not defined all results are returned.", DataType = "'A,B' qhere A = start result and B = end result. (e.g. 10,20)", IsRequired = false)]
        //public string Show { get; set; }

        [ApiMember(Name = "Pagination (offset)", ParameterType = "Offset", Description = "Introduces a pagination offset (skips the first <parameter> * <limit> results", DataType = "Number of the page", IsRequired = false)]
        public string Offset { get; set; }

        [ApiMember(Name = "Pagination (limit)", ParameterType = "Limit", Description = "Limits the ammount of results returned. If not defined 10 results are returned. If -1 then all results are displayed", DataType = "Number of results of the page", IsRequired = false)]
        public string Limit { get; set; }

        [ApiMember(Name = "Category object", ParameterType = "category", Description = "This parameter is passed in the body content. It is used to Post or Put a category.",
    DataType = "Category JSON object.", IsRequired = false)]
        public category category { get; set; }

        [ApiMember(Name = "Category ID", ParameterType = "Id", Description = "Returns a category with the given Id.", DataType = "string", IsRequired = false)]
        public string Id { get; set; }
    }

    public class CategoriesResponse
    {
        public List<category> categories { get; set; }
        public CategoriesResponse()
        {
            categories = new List<category>();
        }
    }
}
