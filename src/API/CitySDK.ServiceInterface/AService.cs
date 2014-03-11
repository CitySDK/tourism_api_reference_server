using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;

namespace CitySDK.ServiceInterface
{
    public abstract class AService : Service
    {
        protected static readonly string[] RELATIONSHIP_TERMS = new string[]
        {
          "equals", 
          "disjoint", 
          "intersects", 
          "crosses", 
          "overlaps", 
          "within", 
          "contains", 
          "touches"
        };

        protected static readonly string[] LINK_TERMS = new string[]
        {
          "alternate", 
          "canonical", 
          "copyright", 
          "describedby", 
          "edit", 
          "enclosure", 
          "icon", 
          "latest-version", 
          "license", 
          "related", 
          "search", 
          "parent", 
          "child", 
          "historic", 
          "future"
        };

        protected static readonly string[] LABEL_TERMS = new string[]
        {
           "primary",
           "secondary"
        };

        protected static readonly string[] TIME_TERMS = new string[]
        {
          "start", 
          "end", 
          "open",
          "instant"
        };

        protected static readonly string[] DESCRIPTION_TYPES = new string[]
        {
          "x-citysdk/price",
          "x-citysdk/waiting-time",
          "x-citysdk/occupation",
          "x-citysdk/accessibility-textual",
          "x-citysdk/accessibility-properties"
        };

        public IMongoDB MongoDb { get; set; }

        public IUserAuthRepository UserAuthRepository { get; set; }
    }
}