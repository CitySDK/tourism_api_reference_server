using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using ServiceStack.ServiceHost;
using System.Reflection;
using System.IO;

namespace CitySDK.ServiceModel.Operations
{
    [Route("/diagnostic", Verbs = "GET, DELETE, POST, PUT")]
    public class Diagnostic
    {
    }

    public class DiagnosticResponse
    {
        public Dictionary<string, string> POIStats { get; set; }
        public Dictionary<string, string> POICategoryStats { get; set; }
        public Dictionary<string, string> EventStats { get; set; }
        public Dictionary<string, string> EventCategoryStats { get; set; }
        public Dictionary<string, string> RouteStats { get; set; }
        public Dictionary<string, string> RouteCategoryStats { get; set; }
        public Dictionary<string, string> KMLFileStats { get; set; }
        public String BuildDate { get; set; }
        
        public DiagnosticResponse()
        {
            POIStats = new Dictionary<string, string>();
            POICategoryStats = new Dictionary<string, string>();
            EventStats = new Dictionary<string, string>();
            EventCategoryStats = new Dictionary<string, string>();
            RouteStats = new Dictionary<string, string>();
            RouteCategoryStats = new Dictionary<string, string>();
            KMLFileStats = new Dictionary<string, string>();
            BuildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime.ToString();
        }
    }
}
