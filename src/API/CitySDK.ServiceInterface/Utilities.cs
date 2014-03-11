using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GeoJsonObjectModel;

namespace CitySDK.ServiceInterface
{
    public static class Utilities
    {
        internal static IMongoQuery GetGeoIntersect(string coords)
        {
            const double MetersToEarthDegrees = 111.12e3;

            if (String.IsNullOrEmpty(coords))
            {
                return Query.Null;
            }

            string[] points = coords.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (points.Length == 0) { return Query.Null; }

            GeoJsonGeometry<GeoJson2DGeographicCoordinates> geometry = null;

            if (points.Length == 1)
            {
                #region Point

                string[] xyr = coords.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // Point with radius
                if (xyr.Length == 3)
                {
                    double latitude = Double.Parse(xyr[0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);
                    double longitude = Double.Parse(xyr[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);
                    double radius = Double.Parse(xyr[2], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);

                    if (radius <= 0)
                        radius = 1;

                    var polyPoints = GetPolygon(longitude, latitude, radius / MetersToEarthDegrees);
                    var linearRing = GeoJson.LinearRingCoordinates(polyPoints.ToArray());

                    geometry = new GeoJsonPolygon<GeoJson2DGeographicCoordinates>(new GeoJsonPolygonCoordinates<GeoJson2DGeographicCoordinates>(linearRing));
                }
                else if (xyr.Length == 2)
                {
                    double latitude = Double.Parse(xyr[0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);
                    double longitude = Double.Parse(xyr[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);

                    geometry = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(longitude, latitude));
                }

                #endregion
            }
            else
            {
                #region Polygon

                double[,] p = new double[points.Length, 2];

                List<GeoJson2DGeographicCoordinates> polyPoints = new List<GeoJson2DGeographicCoordinates>();

                //polygon
                for (int k = 0; k < points.Length; k++)
                {
                    string[] xy = points[k].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    var lat = Double.Parse(xy[0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);
                    var lon = Double.Parse(xy[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);

                    polyPoints.Add(new GeoJson2DGeographicCoordinates(lon, lat));
                }

                if (polyPoints.First().Latitude != polyPoints.Last().Latitude || polyPoints.First().Longitude != polyPoints.Last().Longitude)
                    polyPoints.Add(polyPoints.First());

                var linearRing = GeoJson.LinearRingCoordinates(polyPoints.ToArray());
                geometry = new GeoJsonPolygon<GeoJson2DGeographicCoordinates>(new GeoJsonPolygonCoordinates<GeoJson2DGeographicCoordinates>(linearRing));

                #endregion
            }

            if (geometry != null)
                return Query.GeoIntersects("location.GeoJson", geometry);
            else
                return Query.Null;
        }

        private static List<GeoJson2DGeographicCoordinates> GetPolygon(double longitude, double latitude, double radius)
        {
            int number = 36;
            //latitude in radians
            var lat = (latitude * Math.PI) / 180;
            var lon = (longitude * Math.PI) / 180;
            radius = (radius * Math.PI) / 180;

            List<GeoJson2DGeographicCoordinates> points = new List<GeoJson2DGeographicCoordinates>();
            for (int i = 0; i < number; i++)
            {
                //var point = new VELatLong(0, 0)
                var bearing = (i * 360 / number) * Math.PI / 180; //rad
                double pointLatitude = Math.Asin(Math.Sin(lat) * Math.Cos(radius) + Math.Cos(lat) * Math.Sin(radius) * Math.Cos(bearing));
                double pointLongitude = ((lon + Math.Atan2(Math.Sin(bearing) * Math.Sin(radius) * Math.Cos(lat), Math.Cos(radius) - Math.Sin(lat) * Math.Sin(pointLatitude))) * 180) / Math.PI;
                pointLatitude = (pointLatitude * 180) / Math.PI;

                points.Add(new GeoJson2DGeographicCoordinates(pointLongitude, pointLatitude));
            }

            points.Add(points.First());

            return points;
        }

        [Obsolete]
        internal static IMongoQuery GetIntersectCoords(string coords)
        {
            if (string.IsNullOrEmpty(coords))
            {
                return Query.Null;
            }

            const double MetersToEarthDegrees = 111.12e3;
            string[] points = coords.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (points.Length == 0)
            {
                return Query.Null;
            }

            if (points.Length == 1)
            {
                string[] xyr = coords.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // single point
                if (xyr.Length == 3)
                {
                    double latitude = double.Parse(xyr[0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);
                    double longitude = double.Parse(xyr[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);
                    double radius = double.Parse(xyr[2], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);

                    if (radius <= 0)
                        radius = 1;

                    string polygon = "[";
                    var polyPoints = GetPolygonDoubles(longitude, latitude, radius / MetersToEarthDegrees);
                    foreach (var point in polyPoints)
                    {
                        polygon += string.Concat("[", point[0].ToString(CultureInfo.InvariantCulture), ",", point[1].ToString(CultureInfo.InvariantCulture), "],");
                    }
                    polygon += string.Concat("[", polyPoints[0][0].ToString(CultureInfo.InvariantCulture), ",", polyPoints[0][1].ToString(CultureInfo.InvariantCulture), "]");
                    polygon += "]";

                    var jsonQuery2 = string.Format("{{ $geoIntersects : {{ $geometry : {{ type : \"Polygon\" , coordinates : [ {0} ] }} }} }}", polygon);
                    BsonDocument document2 = BsonSerializer.Deserialize<BsonDocument>(jsonQuery2);

                    return Query.EQ("location.GeoJson", document2);
                }

                if (xyr.Length == 2)
                {
                    double latitude = double.Parse(xyr[0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);
                    double longitude = double.Parse(xyr[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);

                    var document = new BsonDocument
                                    {
                                        { "long", longitude },
                                        { "lat", latitude }
                                    };

                    var jsonQuery2 = string.Format("{{ $geoIntersects : {{ $geometry : {{ type : \"Point\" , coordinates : [ {0} , {1} ] }} }} }}", longitude.ToString(CultureInfo.InvariantCulture), latitude.ToString(CultureInfo.InvariantCulture));
                    BsonDocument document2 = BsonSerializer.Deserialize<BsonDocument>(jsonQuery2);

                    return Query.EQ("location.GeoJson", document2);
                }
            }

            if (points.Length > 1)
            {
                double[,] p = new double[points.Length, 2];

                //polygon
                for (int k = 0; k < points.Length; k++)
                {
                    string[] xy = points[k].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    p[k, 1] = double.Parse(xy[0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);
                    p[k, 0] = double.Parse(xy[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture);
                }

                //var jsonQuery2 = string.Format("{{ $geoIntersects : {{ $geometry : {{ type : \"Point\" , coordinates : [ {0} ] }} }} }}", );
                //BsonDocument document2 = BsonSerializer.Deserialize<BsonDocument>(jsonQuery2);

                //return Query.EQ("location.GeoJSON", document2);
            }

            return Query.Null;
        }

        [Obsolete]
        private static List<double[]> GetPolygonDoubles(double longitude, double latitude, double radius)
        {
            int number = 36;
            //latitude in radians
            var lat = (latitude * Math.PI) / 180;
            var lon = (longitude * Math.PI) / 180;
            radius = (radius * Math.PI) / 180;

            List<double[]> points = new List<double[]>();
            for (int i = 0; i < number; i++)
            {
                //var point = new VELatLong(0, 0)
                var bearing = (i * 360 / number) * Math.PI / 180; //rad
                double pointLatitude = Math.Asin(Math.Sin(lat) * Math.Cos(radius) + Math.Cos(lat) * Math.Sin(radius) * Math.Cos(bearing));
                double pointLongitude = ((lon + Math.Atan2(Math.Sin(bearing) * Math.Sin(radius) * Math.Cos(lat), Math.Cos(radius) - Math.Sin(lat) * Math.Sin(pointLatitude))) * 180) / Math.PI;
                pointLatitude = (pointLatitude * 180) / Math.PI;

                var cc = new[] { pointLongitude, pointLatitude };
                points.Add(cc);
            }

            return points;
        }
    }
}
