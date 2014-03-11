using System.Collections.Generic;
using System.Drawing;

namespace DataImporter
{
    public class Details
    {
        public int OBJECTID { get; set; }
        public string COD_SIG { get; set; }
        public string IDTIPO { get; set; }
        public string CAT_NOME { get; set; }
        public int CAT_ID { get; set; }
        public string INF_NOME { get; set; }
        public string INF_MORADA { get; set; }
        public object INF_TELEFONE { get; set; }
        public object INF_FAX { get; set; }
        public object INF_EMAIL { get; set; }
        public object INF_SITE { get; set; }
        public string INF_DESCRICAO { get; set; }
        public object INF_AUTOR_DESCRICAO { get; set; }
        public string INF_FONTE { get; set; }
        public object INF_OBS { get; set; }
        public int INF_ACTIVO { get; set; }
        public int INF_MUNICIPAL { get; set; }
    }

    public class Geometry
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class POI
    {
        public Details attributes { get; set; }
        public Geometry geometry { get; set; }
    }

    public class RootPOI
    {
        public POI feature { get; set; }
    }

    public class RootObject
    {
        public string objectIdFieldName { get; set; }
        public List<int> objectIds { get; set; }
    }

    public class MapServer
    {
        public List<Layer> layers { get; set; }
        public List<object> tables { get; set; }
    }

    public class Layer
    {
        public double currentVersion { get; set; }

        public int id { get; set; }

        public string name { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public string definitionExpression { get; set; }
        public string geometryType { get; set; }
        public string copyrightText { get; set; }
        public object parentLayer { get; set; }
        public List<object> subLayers { get; set; }
        public int minScale { get; set; }
        public int maxScale { get; set; }
        public bool defaultVisibility { get; set; }
        public Extent extent { get; set; }
        public bool hasAttachments { get; set; }
        public string htmlPopupType { get; set; }
        public DrawingInfo drawingInfo { get; set; }
        public string displayField { get; set; }
        public List<Field> fields { get; set; }
        public object typeIdField { get; set; }
        public object types { get; set; }
        public List<object> relationships { get; set; }
        public string capabilities { get; set; }

        public List<POI> POIS { get; set; }
    }

    public class Field
    {
        public string name { get; set; }
        public string type { get; set; }
        public string alias { get; set; }
        public int? length { get; set; }
    }

    public class DrawingInfo
    {
        public Renderer renderer { get; set; }
        public int transparency { get; set; }
        public object labelingInfo { get; set; }
    }

    public class Extent
    {
        public double xmin { get; set; }
        public double ymin { get; set; }
        public double xmax { get; set; }
        public double ymax { get; set; }
        public SpatialReference spatialReference { get; set; }
    }

    public class SpatialReference
    {
        public int wkid { get; set; }
    }

    public class Symbol
    {
        public Bitmap Image { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public string imageData { get; set; }
        public string contentType { get; set; }
        public object color { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public int angle { get; set; }
        public int xoffset { get; set; }
        public int yoffset { get; set; }
    }

    public class Renderer
    {
        public string type { get; set; }
        public Symbol symbol { get; set; }
        public string label { get; set; }
        public string description { get; set; }
    }
}
