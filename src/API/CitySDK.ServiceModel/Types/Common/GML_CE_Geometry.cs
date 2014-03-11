namespace CitySDK.ServiceModel.Types
{
    public class GML_CE_Geometry
    {
        public GML_CE_Geometry()
        {
            srsName = "http://www.opengis.net/def/crs/EPSG/0/4326";
        }

        /// <summary>
        /// Gets and sets coordinate set containner
        /// </summary>
        public string posList { get; set; }

        /// <summary>
        /// Gets and sets coordinate reference system (CRS) URI being used. The World Geodetic System 84 (WGS84)36 in 2 di-mensions - latitude and longitude - is used by default.
        /// </summary>
        public string srsName { get; set; }
    }
}
