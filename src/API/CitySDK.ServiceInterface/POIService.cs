using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using CitySDK.ServiceModel.Operations;
using CitySDK.ServiceModel.Types;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GeoJsonObjectModel;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using MoreLinq;
using ServiceStack.Common.Web;

namespace CitySDK.ServiceInterface
{
    public class POIService : AService
    {
        public object Get(POIs request)
        {
            // Try to get the page limit parameter
            int pageLimit = 10;
            if (!request.Limit.IsNullOrEmpty())
            {
              pageLimit = int.Parse(request.Limit);
            }
            
            // Try to get the offset
            int skipResults = 0;
            if (!request.Offset.IsNullOrEmpty())
            {
              // If pagelimit is -1, skip parameters skips unitary elements
              skipResults = int.Parse(request.Offset) * ((pageLimit != -1) ? pageLimit : 1);
            }

            if (!request.Id.IsNullOrEmpty())
            {
                var objectID = ObjectId.Parse(request.Id);

                var poi = MongoDb.POIs.FindOneById(objectID);

                if (poi == null)
                {
                  base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                  return "POI " + request.Id + " not found";
                }
                  
                // Simple request by id
                if (request.Relation.IsNullOrEmpty())
                {
                  return BuildPOI(MongoDb.POICategories, poi, false);
                }

                if (request.Relation == "parent")
                {
                  MongoCursor<poi> parentPois = MongoDb.POIs.Find(Query.And(Query.EQ("link.term", "child"), Query.EQ("link.id",objectID)));
                  POISResponse parentResponse = new POISResponse();

                  foreach (poi p in parentPois.DistinctBy(d => d._id))
                  {
                    poi tmpPoi = BuildPOI(MongoDb.POICategories, p, false);
                    
                    if(tmpPoi != null)
                      parentResponse.poi.Add(tmpPoi);
                  }

                  if (pageLimit != -1)
                    parentResponse.poi = parentResponse.poi.Skip(skipResults).Take(pageLimit).ToList();
                  else
                    parentResponse.poi = parentResponse.poi.Skip(skipResults).ToList();

                  return parentResponse;
                }

                if (request.Relation == "child")
                {

                  POISResponse childResponse = new POISResponse();
                  
                  if(poi.link.IsNullOrEmpty())
                    return childResponse;

                  foreach (var link in poi.link)
                  {
                    if (link.term != "child")
                      continue;

                    if (ObjectId.TryParse(link.id, out objectID))
                    {
                      poi = MongoDb.POIs.FindOneById(objectID);

                      if (poi != null)
                        childResponse.poi.Add(BuildPOI(MongoDb.POICategories, poi, false));
                    }
                  }

                  if (pageLimit != -1)
                    childResponse.poi = childResponse.poi.Skip(skipResults).Take(pageLimit).ToList();
                  else
                    childResponse.poi = childResponse.poi.Skip(skipResults).ToList();


                  return childResponse;
                }

                if (request.Relation == "events")
                {
                  MongoCursor<@event> events = MongoDb.Events.Find(Query.EQ("location.relationship.targetPOI", objectID));
                  EventsResponse eventResponse = new EventsResponse();

                  if(events != null)
                    foreach (@event ev in events)
                    {
                      @event completeEvent = EventService.BuildEvent(MongoDb.EventCategories, MongoDb.POIs, ev);

                      if(completeEvent != null)
                        eventResponse.@event.Add(completeEvent);
                    }

                  if (pageLimit != -1)
                    eventResponse.@event = eventResponse.@event.Skip(skipResults).Take(pageLimit).ToList();
                  else
                    eventResponse.@event = eventResponse.@event.Skip(skipResults).ToList();

                  return eventResponse;
                }

                if (request.Relation == "routes")
                {
                  MongoCursor<route> routes = MongoDb.Routes.Find(Query.EQ("pois.location.relationship.targetPOI", objectID));
                  RoutesResponse routeResponse = new RoutesResponse();

                  if(routes != null)
                    foreach(route rt in routes)
                    {
                      route completeRoute = RouteService.BuildRoute(MongoDb.RouteCategories, MongoDb.POIs, rt, false);
                      
                      if(completeRoute != null)
                        routeResponse.routes.Add(completeRoute);
                    }

                  if (pageLimit != -1)
                    routeResponse.routes = routeResponse.routes.Skip(skipResults).Take(pageLimit).ToList();
                  else
                    routeResponse.routes = routeResponse.routes.Skip(skipResults).ToList();

                  return routeResponse;
                }
                
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid relation: " + request.Relation;
            }

            List<IMongoQuery> queries = new List<IMongoQuery>();

            //Category
            if (!request.Category.IsNullOrEmpty())
            {
                List<IMongoQuery> categoriesList = new List<IMongoQuery>();

                foreach (string category in request.Category.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var catsTofind = MongoDb.POICategories.Find(
                      Query.And(
                        Query.EQ("term", "category"), 
                        Query.Matches("label.value", new BsonRegularExpression(".*" + category + ".*", "-i")),
                        Query.Or(Query.NotExists("deleted"), Query.GT("deleted", BsonValue.Create(DateTime.UtcNow)))
                      )
                    );
                    categoriesList.AddRange(catsTofind.Select(category1 => Query.Or(new[] { Query.EQ("categoryIDs", category1._id) })));
                }

                if (categoriesList.Count > 0)
                    queries.Add(Query.Or(categoriesList));
                else
                {
                    // If no category was found, we can already reply a empty response...
                    return new POISResponse();
                }
            }

            QueryDocument qDoc = new QueryDocument();

            if (request.Deleted.IsNullOrEmpty())
            {
              queries.Add(Query.Or(Query.NotExists("deleted"), Query.GT("deleted", BsonValue.Create(DateTime.UtcNow))));
            }

            //Tag
            if (!request.Tag.IsNullOrEmpty())
            {
                queries.Add(Query.And(request.Tag.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(tag => Query.Or(new[] { Query.EQ("tags", tag) }))));
            }

            //Relation
            if (!request.Relation.IsNullOrEmpty())
            {
                queries.Add(Query.And(request.Relation.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(rel => Query.Or(new[] { Query.EQ("location.relationship.targetPOI", rel) }))));
            }

            //Minimal
            if (!request.Minimal.IsNullOrEmpty())
            {
                queries.AddRange(request.Minimal.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(word => Query.And(Query.Matches("label.value", "/.*" + word + ".*/i"))));
            }
            else if (!request.Complete.IsNullOrEmpty())
            {
                queries.AddRange(request.Complete.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(word => Query.And(Query.Matches("label.value", "/.*" + word + ".*/i"))));
            }

            if (!string.IsNullOrEmpty(request.Coords))
            {
                qDoc.AddRange(Utilities.GetGeoIntersect(request.Coords).ToBsonDocument());
            }

            if (queries.Count > 0)
                qDoc.AddRange(Query.And(queries).ToBsonDocument());

            MongoCursor<poi> pois = MongoDb.POIs.Find(qDoc);
            POISResponse response = new POISResponse();

            bool minimal = !request.Minimal.IsNullOrEmpty();
            foreach (var p in pois.DistinctBy(d => d._id))
            {
              poi tmpPoi = BuildPOI(MongoDb.POICategories, p, minimal);
              
              if(tmpPoi != null)
                response.poi.Add(tmpPoi);
            }

            if (pageLimit != -1)
              response.poi = response.poi.Skip(skipResults).Take(pageLimit).ToList();
            else
              response.poi = response.poi.Skip(skipResults).ToList();

            return response;

        }

        [Authenticate]
        public object Put(POIs request)
        {
            #region Validate First Level

            if (request == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid request object";
            }

            if (request.poi == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Null poi object";
            }

            poi poi = request.poi;

            if (poi.@base.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty 'base' field";
            }

          /* Requirement dropped
            if (poi.lang.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty 'lang' field";
            }
          */
            #endregion

            #region Validate License

            if (poi.license != null)
            {
                if (poi.license.value.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'value' field";
                }

                if (poi.license.term.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'term' field";
                }

                poi.license.value = WebUtility.HtmlDecode(poi.license.value);
            }

            #endregion

            #region Validate Labels

            if (!poi.label.IsNullOrEmpty())
            {
                foreach (var label in poi.label)
                {
                    if (label.value.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Label 'value' field";
                    }

                    if (label.term.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Label 'term' field";
                    }

                    if (!LABEL_TERMS.Contains(label.term.ToLower()))
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Invalid label term";
                    }

                    if (poi.lang.IsNullOrEmpty() && label.lang.IsNullOrEmpty())
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Missing Label language (or root poi language)";
                    }

                    label.value = WebUtility.HtmlDecode(label.value);
                }
            }

            #endregion

            #region Validate Description

            if (!poi.description.IsNullOrEmpty())
            {
              foreach (var descr in poi.description)
              {
                if (poi.lang.IsNullOrEmpty() && descr.lang.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Missing description language (or poi root language)";
                }

                if (descr.value.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Missing description element value";
                }

                if (!descr.type.IsNullOrEmpty())
                {
                  if (!DESCRIPTION_TYPES.Contains(descr.type.ToLower()))
                  {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Invalid Description type";
                  }
                }

                descr.value = WebUtility.HtmlDecode(descr.value);
              }
            }
            #endregion
            
            #region  Validate Categories

            if (!poi.category.IsNullOrEmpty())
            {
                poi.categoryIDs = new List<ObjectId>();
                foreach (var category in poi.category)
                {
                    if (category._id == null || category._id == ObjectId.Empty)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Category 'Id' field";
                    }

                    if (MongoDb.POICategories.FindOneById(category._id) == null)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return "Missing Category object: {0}. All categories must be inserted before a POI insertion.".FormatWith(category._id.ToString());
                    }

                    poi.categoryIDs.Add(category._id.Value);
                }
            }

            #endregion

            #region Validate Links

            if (!poi.link.IsNullOrEmpty())
            {
                foreach (var l in poi.link)
                {
                    if (l.value.IsNullOrEmpty() && l.href.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Link 'value' or 'href' field";
                    }

                    if (l.term.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Link 'term' field";
                    }

                    if (!LINK_TERMS.Contains(l.term.ToLower()))
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Invalid Link term found";
                    }
                }
            }

            #endregion

            #region  Validate relationships
            if (poi.location != null && !poi.location.relationship.IsNullOrEmpty())
            {
                foreach (var rel in poi.location.relationship)
                {
                    if (rel.targetPOI == ObjectId.Empty)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Invalid Relationship TargetPOI Id";
                    }

                    if (rel.term.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Relationship term field";
                    }

                    if (MongoDb.POIs.FindOneById(rel.targetPOI) == null)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return "Relationship TargetPOI object with Id \"{0}\" not found".FormatWith(rel.targetPOI.ToString());
                    }

                    if (!RELATIONSHIP_TERMS.Contains(rel.term.ToLower()))
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Invalid relationship term found";
                    }
                }
            }
            #endregion

            #region Fill Coords

            if (poi.location != null)
            {
                poi.location.GeoJson = new List<BsonDocument>();

                if (!poi.location.point.IsNullOrEmpty())
                {
                    #region Points

                    foreach (var point in poi.location.point)
                    {
                        if (point != null && point.Point != null && !point.Point.posList.IsNullOrEmpty())
                        {
                            double xx, yy;
                            string[] pps = point.Point.posList.Split(' ');

                            if (pps.Length != 2
                                || !double.TryParse(pps[0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture, out yy)
                                || !double.TryParse(pps[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture, out xx))
                            {
                                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                return "Invalid posList.";
                            }

                            poi.location.GeoJson.Add(GeoJson.Point(new GeoJson2DGeographicCoordinates(xx, yy)).ToBsonDocument());
                        }
                    }

                    #endregion
                }

                if (!poi.location.polygon.IsNullOrEmpty())
                {
                    #region Polygon

                    foreach (var polygon in poi.location.polygon)
                    {
                        if (polygon != null && polygon.SimplePolygon != null && !polygon.SimplePolygon.posList.IsNullOrEmpty())
                        {
                            string[] points = polygon.SimplePolygon.posList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            if (points.Length <= 2)
                            {
                                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                return "Invalid Polygon posList. 3 or more points must be defined.";
                            }

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
                            poi.location.GeoJson.Add(new GeoJsonPolygon<GeoJson2DGeographicCoordinates>(new GeoJsonPolygonCoordinates<GeoJson2DGeographicCoordinates>(linearRing)).ToBsonDocument());
                        }
                    }

                    #endregion
                }

                if (poi.location.address != null && poi.location.address.value != null)
                {
                  poi.location.address.value = WebUtility.HtmlDecode(poi.location.address.value);
                }
            }

            #endregion

            #region Authors

            poi.author = new POITermType { term = "primary", value = this.GetSession().UserName };

            #endregion

            poi.created = DateTime.UtcNow;
            poi._id = ObjectId.GenerateNewId();

            var result = MongoDb.POIs.Save(poi);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return poi;
            }

            if (result.HasLastErrorMessage)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return result.LastErrorMessage;
            }

            base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return "Unknown error";
        }

        [Authenticate]
        public object Post(POIs request)
        {
            #region Validate First Level

            if (request == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid request object";
            }

            if (request.poi == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Null poi object";
            }

            poi poi = request.poi;

            if (poi._id == null || poi._id == ObjectId.Empty)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid 'id' field";
            }

            poi poiToUpdate = MongoDb.POIs.FindOneById(poi._id);

            if (poiToUpdate == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return "POI with 'id' {0} not found.".FormatWith(poi._id);
            }

            if (poiToUpdate.author == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "This POI cannot be update. Missing DB author information";
            }

            var user = this.GetSession();
            if (user.Roles.IsNullOrEmpty() && !poiToUpdate.author.value.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase))
            {
                base.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return "You do not have permissions for updating this POI.";
            }

            if (poi.@base.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty 'base' field";
            }

            #endregion

            #region Validate Authors

            if (poi.author != null)
            {
                if (poi.author.value.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty Author 'value' field";
                }

                if (poi.author.term.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty Author 'term' field";
                }

                poi.author.value = WebUtility.HtmlDecode(poi.author.value);
            }

            #endregion

            #region Validate License

            if (poi.license != null)
            {
                if (poi.license.value.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'value' field";
                }

                if (poi.license.term.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'term' field";
                }

                poi.license.value = WebUtility.HtmlDecode(poi.license.value);
            }

            #endregion

            #region Validate Labels

            if (!poi.label.IsNullOrEmpty())
            {
                foreach (var label in poi.label)
                {
                    if (label.value.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Label 'value' field";
                    }

                    if (label.term.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Label 'term' field";
                    }

                    if (!LABEL_TERMS.Contains(label.term.ToLower()))
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Invalid Laber term field";
                    }

                    if (poi.lang.IsNullOrEmpty() && label.lang.IsNullOrEmpty())
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Missing Label 'language' (or root language)";
                    }

                    label.value = WebUtility.HtmlDecode(label.value);
                }
            }

            #endregion

            #region Validate description

            if (!poi.description.IsNullOrEmpty())
            {
              foreach (var descr in poi.description)
              {
                if (poi.lang.IsNullOrEmpty() && descr.lang.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Missing poi 'description' language (or root languade)";
                }
                if (descr.value.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Missing 'value' on description element";
                }

                if (!descr.type.IsNullOrEmpty())
                {
                  if (!DESCRIPTION_TYPES.Contains(descr.type.ToLower()))
                  {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Invalid description type";
                  }
                }

                // Perform url decode on the received text
                descr.value = WebUtility.HtmlDecode(descr.value);
              
              }
            }

            #endregion

            #region  Validate Categories

            if (!poi.category.IsNullOrEmpty())
            {
                poi.categoryIDs = new List<ObjectId>();
                foreach (var category in poi.category)
                {
                    if (category._id == null || category._id == ObjectId.Empty)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Category 'Id' field";
                    }

                    if (MongoDb.POICategories.FindOneById(category._id) == null)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return "Missing Category object: {0}".FormatWith(category._id.ToString());
                    }

                    poi.categoryIDs.Add(category._id.Value);
                }
            }

            #endregion

            #region Validate Links

            if (!poi.link.IsNullOrEmpty())
            {
                foreach (var l in poi.link)
                {
                    if (l.value.IsNullOrEmpty() && l.href.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Link 'value' or 'href' field";
                    }

                    if (l.term.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Link 'term' field";
                    }

                    if (!LINK_TERMS.Contains(l.term.ToLower()))
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Invalid Link term value";
                    }

                }
            }

            #endregion

            #region  Validate relationships
            if (poi.location != null && !poi.location.relationship.IsNullOrEmpty())
            {
                foreach (var rel in poi.location.relationship)
                {
                    if (rel.targetPOI == ObjectId.Empty)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Invalid Relationship TargetPOI Id";
                    }

                    if (rel.term.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Relationship term field";
                    }

                    if (!RELATIONSHIP_TERMS.Contains(rel.term.ToLower()))
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Invalid Relationship term";
                    }

                    if (MongoDb.POIs.FindOneById(rel.targetPOI) == null)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return "Relationship TargetPOI object with Id \"{0}\" not found".FormatWith(rel.targetPOI.ToString());
                    }
                }
            }
            #endregion

            #region Fill Coords / Location processing

            if (poi.location != null)
            {
                poi.location.GeoJson = new List<BsonDocument>();

                if (!poi.location.point.IsNullOrEmpty())
                {
                    #region Points

                    foreach (var point in poi.location.point)
                    {
                        if (point != null && point.Point != null && !point.Point.posList.IsNullOrEmpty())
                        {
                            double xx, yy;
                            string[] pps = point.Point.posList.Split(' ');

                            if (pps.Length != 2
                                || !double.TryParse(pps[0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture, out yy)
                                || !double.TryParse(pps[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture, out xx))
                            {
                                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                return "Invalid posList.";
                            }

                            poi.location.GeoJson.Add(GeoJson.Point(new GeoJson2DGeographicCoordinates(xx, yy)).ToBsonDocument());
                        }
                    }

                    #endregion
                }

                if (!poi.location.polygon.IsNullOrEmpty())
                {
                    #region Polygon

                    foreach (var polygon in poi.location.polygon)
                    {
                        if (polygon != null && polygon.SimplePolygon != null && !polygon.SimplePolygon.posList.IsNullOrEmpty())
                        {
                            string[] points = polygon.SimplePolygon.posList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            if (points.Length <= 2)
                            {
                                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                return "Invalid Polygon posList. 3 or more points must be defined.";
                            }

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
                            poi.location.GeoJson.Add(new GeoJsonPolygon<GeoJson2DGeographicCoordinates>(new GeoJsonPolygonCoordinates<GeoJson2DGeographicCoordinates>(linearRing)).ToBsonDocument());
                        }
                    }

                    #endregion
                }

                if (poi.location.address != null)
                {
                  if(poi.location.address.value != null)
                    poi.location.address.value = WebUtility.HtmlDecode(poi.location.address.value);
                }
            }

            #endregion

            //maintain create delete timestamps
            poi.created = poiToUpdate.created;
            poi.deleted = poiToUpdate.deleted;
            poi.updated = DateTime.UtcNow;

            //maintain author
            poi.author = poiToUpdate.author;

            var result = MongoDb.POIs.Save(poi);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return poi;
            }

            if (result.HasLastErrorMessage)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return result.LastErrorMessage;
            }

            base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return "Unknown error";
        }

        [Authenticate]
        public object Delete(POIs request)
        {
            if (request.Id.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Missing Id parameter";
            }

            ObjectId id;

            if (!ObjectId.TryParse(request.Id, out id))
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid Id parameter";
            }

            poi poi = MongoDb.POIs.FindOneById(id);

            if (poi == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return "POI with id \"{0}\" not found.".FormatWith(request.Id);
            }

            if (poi.author == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "This Category cannot be deleted. Missing author information";
            }

            var user = this.GetSession();
            if (user.Roles.IsNullOrEmpty() && !poi.author.value.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase))
            {
                base.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return "You do not have permissions for deleting this POI.";
            }

            poi.deleted = DateTime.UtcNow;

            var result = MongoDb.POIs.Save(poi);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return "POI with id \"{0}\" removed successfully.".FormatWith(request.Id);
            }

            if (result.HasLastErrorMessage)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return result.LastErrorMessage;
            }

            base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return "Unknown error";
        }

        public static poi BuildPOI(MongoCollection<category> categories, poi poi, bool minimal)
        {
            if (poi != null)
            {
                if (poi.categoryIDs != null && poi.categoryIDs.Count > 0)
                {
                    poi.category = new List<category>();

                    foreach (var catID in poi.categoryIDs)
                    {
                        var cat = categories.FindOneById(catID);

                        if (cat.deleted != null && cat.deleted <= DateTime.UtcNow)
                            continue;

                        foreach (var l in cat.label)
                        {
                            poi.category.Add(new category
                                {
                                    lang = l.lang,
                                    value = l.value,
                                    term = "category"
                                });



                            if (cat.link != null)
                            {
                              if (poi.link == null)
                                poi.link = new List<POITermType>();

                              foreach (var link in cat.link)
                              {
                                if (link.term == "icon")
                                {
                                  poi.link.Add(link);
                                }
                              }
                            }
                        }

                    }
                }

                if (minimal)
                {
                    if (poi.location != null)
                    {
                        poi.location.address = null;
                        poi.location.relationship = null;
                    }

                    if (poi.link != null)
                        poi.link.RemoveAll(r => r.term != "icon");
                }
            }

            return poi;
        }
    }
}