using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CitySDK.ServiceModel.Operations;
using CitySDK.ServiceModel.Types;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;

namespace CitySDK.ServiceInterface
{
    public class RouteService : AService
    {
        public object Get(Routes request)
        {
            if (!request.Id.IsNullOrEmpty())
            {
                var objectID = ObjectId.Parse(request.Id);

                var ev = MongoDb.Routes.FindOneById(objectID);

                return BuildRoute(MongoDb.RouteCategories, MongoDb.POIs, ev, true);
            }

            List<IMongoQuery> queries = new List<IMongoQuery>();

            //Category
            if (!request.Category.IsNullOrEmpty())
            {
                List<IMongoQuery> categoriesList = new List<IMongoQuery>();

                foreach (string category in request.Category.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var catsTofind = MongoDb.RouteCategories.Find(
                      Query.And(
                        Query.EQ("term", "category"), 
                        Query.Matches("label.value", BsonRegularExpression.Create(".*" + category + ".*", "-i")),
                        Query.Or(Query.NotExists("deleted"), Query.GT("deleted", BsonValue.Create(DateTime.UtcNow)))
                      )
                    );
                    categoriesList.AddRange(catsTofind.Select(category1 => Query.Or(new[] { Query.EQ("categoryIDs", category1._id) })));
                }

                if (categoriesList.Count > 0)
                    queries.Add(Query.Or(categoriesList));
                else
                {
                    return new RoutesResponse();
                }
            }

            //Tag
            if (!request.Tag.IsNullOrEmpty())
            {
                queries.Add(Query.And(request.Tag.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(tag => Query.Or(new[] { Query.EQ("tags", tag) }))));
            }

            //Search
            if (!request.Name.IsNullOrEmpty())
            {
                queries.Add(Query.And(Query.Matches("label.value", "/.*" + request.Name + ".*/i")));
            }

            QueryDocument qDoc = new QueryDocument();

            queries.Add(Query.Or(Query.NotExists("deleted"), Query.GT("deleted", BsonValue.Create(DateTime.UtcNow))));
            qDoc.Add(Query.And(queries).ToBsonDocument());

            MongoCursor<route> routes = MongoDb.Routes.Find(qDoc);

            List<route> FinalRoutes = routes.ToList();

            if (!string.IsNullOrEmpty(request.Coords))
            {
                FinalRoutes = new List<route>();

                foreach (var route in routes)
                {

                    List<IMongoQuery> qs = new List<IMongoQuery>();
                    foreach (var p in route.pois)
                    {
                        if (p.location != null && p.location.relationship != null && p.location.relationship.Count > 0)
                        {
                            var rel = p.location.relationship.AsQueryable().FirstOrDefault(w => w.term == "equals");
                            if (rel != null)
                            {
                                qs.Add(Query.EQ("_id", rel.targetPOI));
                            }
                        }
                    }

                    QueryDocument q1 = new QueryDocument();
                    q1.AddRange(Utilities.GetGeoIntersect(request.Coords).ToBsonDocument());
                    q1.AddRange(Query.Or(qs).ToBsonDocument());

                    if (MongoDb.POIs.Find(q1).Any())
                    {
                        FinalRoutes.Add(route);
                    }
                }
            }

            /*
            if (!request.Show.IsNullOrEmpty())
            {
                int start = int.Parse(request.Show.SplitOnFirst(',')[0]);
                int end = int.Parse(request.Show.SplitOnFirst(',')[1]);

                FinalRoutes = FinalRoutes.Skip(start).Take(end - start).ToList();
            }
            */

            int pageLimit = 10;
            if (!request.Limit.IsNullOrEmpty())
            {
                pageLimit = int.Parse(request.Limit);
            }

            int skipResults = 0;
            if (!request.Offset.IsNullOrEmpty())
            {
              skipResults = int.Parse(request.Offset) * ((pageLimit != -1) ? pageLimit : 1);
            }

            if (pageLimit != -1)
              FinalRoutes = FinalRoutes.Skip(skipResults).Take(pageLimit).ToList();
            else
              FinalRoutes = FinalRoutes.Skip(skipResults).ToList();

            RoutesResponse response = new RoutesResponse();
            foreach (var p in FinalRoutes)
            {
                //response.routes.Add(new RouteResponse { route = BuildRoute(p) });
              response.routes.Add(BuildRoute(MongoDb.RouteCategories, MongoDb.POIs, p, false));
            }

            return response;
        }

        [Authenticate]
        public object Put(Routes request)
        {
            #region Validate First Level

            if (request == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid request object";
            }

            if (request.route == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Null route object";
            }

            route route = request.route;

            if (route.@base.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty 'base' field";
            }

            #endregion

            #region Validate License

            if (route.license != null)
            {
                if (route.license.value.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'value' field";
                }

                if (route.license.term.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'term' field";
                }

                route.license.value = WebUtility.HtmlDecode(route.license.value);
            }

            #endregion

            #region Validate Labels

            if (!route.label.IsNullOrEmpty())
            {
                foreach (var label in route.label)
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
                      return "Invalid Label term";
                    }

                    if (route.label.IsNullOrEmpty() && label.lang.IsNullOrEmpty())
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Empty Label 'lang' field (or base route lang)";
                    }

                    label.value = WebUtility.HtmlDecode(label.value);
                }
            }

            #endregion

            #region Validate description
            if (!route.description.IsNullOrEmpty())
            {
              foreach (var descr in route.description)
              {
                if (descr.value.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Empty Description 'value' field";
                }

                if (route.lang.IsNullOrEmpty() && descr.lang.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Empty Description 'language' field (or empty root language)";
                }

                if (!descr.type.IsNullOrEmpty())
                {
                  if (!DESCRIPTION_TYPES.Contains(descr.type.ToLower()))
                  {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Invalid Description type found";
                  }
                }

                descr.value = WebUtility.HtmlDecode(descr.value);

              }
            }

            #endregion

            #region  Validate Categories

            if (!route.category.IsNullOrEmpty())
            {
                route.categoryIDs = new List<ObjectId>();
                foreach (var category in route.category)
                {
                    if (category._id == null || category._id == ObjectId.Empty)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Category 'Id' field";
                    }

                    if (MongoDb.RouteCategories.FindOneById(category._id) == null)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return "Missing Category object: {0}. All categories must be inserted before a Route insertion.".FormatWith(category._id.ToString());
                    }

                    route.categoryIDs.Add(category._id.Value);
                }
            }

            #endregion

            #region Validate Links

            if (!route.link.IsNullOrEmpty())
            {
                foreach (var l in route.link)
                {
                    if (l.value.IsNullOrEmpty() && l.href.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Link 'value'  or 'href' field";
                    }

                    if (l.term.IsNullOrEmpty())
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Link 'term' field";
                    }

                    if (LINK_TERMS.Contains(l.term.ToLower()))
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Invalid Link term field";
                    }
                }
            }

            #endregion

            #region  Validate relationships
            if (route.pois.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty pois collection field";
            }

            foreach (var rel in route.pois)
            {
                if (rel.location == null || rel.location.relationship.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "POI with no location";
                }

                if (rel.location.relationship.All(w => w.term != "equals"))
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "POI must have a relationship with a \"equals\" term.";
                }

                var targetPoi = rel.location.relationship.First(f=>f.term == "equals").targetPOI;

                if (MongoDb.POIs.FindOneById(targetPoi) == null)
                {
                    base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return "Relationship TargetPOI object with Id \"{0}\" not found".FormatWith(targetPoi.ToString());
                }
            }

            #endregion

            route.author = new POITermType();
            route.author.term = "primary";
            route.author.value = this.GetSession().UserName;

            route.created = DateTime.UtcNow;
            route._id = ObjectId.GenerateNewId();

            var result = MongoDb.Routes.Save(route);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return route;
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
        public object Post(Routes request)
        {
            #region Validate First Level

            if (request == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid request object";
            }

            if (request.route == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Null route object";
            }

            route route = request.route;

            if (route._id == null || route._id == ObjectId.Empty)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid 'id' field";
            }

            route routeToUpdate = MongoDb.Routes.FindOneById(route._id);

            if (routeToUpdate == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return "Route with 'id' {0} not found.".FormatWith(route._id);
            }

            if (routeToUpdate.author == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "This Route cannot be update. Missing DB author information";
            }

            var user = this.GetSession();
            if (user.Roles.IsNullOrEmpty() && !routeToUpdate.author.value.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase))
            {
                base.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return "You do not have permissions for updating this Route.";
            }

            if (route.@base.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty 'base' field";
            }

          /*
            if (route.lang.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty 'lang' field";
            }
          */
            #endregion

            #region Validate License

            if (route.license != null)
            {
                if (route.license.value.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'value' field";
                }

                if (route.license.term.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'term' field";
                }

                route.license.value = WebUtility.HtmlDecode(route.license.value);
            }

            #endregion

            #region Validate Labels

            if (!route.label.IsNullOrEmpty())
            {
                foreach (var label in route.label)
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
                      return "Invalid Label term";
                    }

                    if (route.label.IsNullOrEmpty() && label.lang.IsNullOrEmpty())
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Empty Label 'lang' field (or base route lang)";
                    }

                    label.value = WebUtility.HtmlDecode(label.value);
                }
            }

            #endregion

            #region Validate description
            if (!route.description.IsNullOrEmpty())
            {
              foreach (var descr in route.description)
              {
                if (descr.value.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Empty Description 'value' field";
                }

                if (route.lang.IsNullOrEmpty() && descr.lang.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Empty Description 'language' field (or empty root language)";
                }

                if (!descr.type.IsNullOrEmpty())
                {
                  if (DESCRIPTION_TYPES.Contains(descr.type.ToLower()))
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

            if (!route.category.IsNullOrEmpty())
            {
                route.categoryIDs = new List<ObjectId>();
                foreach (var category in route.category)
                {
                    if (category._id == null || category._id == ObjectId.Empty)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Category 'Id' field";
                    }

                    if (MongoDb.RouteCategories.FindOneById(category._id) == null)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return "Missing Category object: {0}. All categories must be inserted before a Route insertion.".FormatWith(category._id.ToString());
                    }

                    route.categoryIDs.Add(category._id.Value);
                }
            }

            #endregion

            #region Validate Links

            if (!route.link.IsNullOrEmpty())
            {
                foreach (var l in route.link)
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
                      return "Invalid Link term field";
                    }
                }
            }

            #endregion

            #region  Validate relationships
            if (route.pois.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty pois collection field";
            }

            foreach (var rel in route.pois)
            {
                if (rel.location == null || rel.location.relationship.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "POI with no location";
                }

                if (rel.location.relationship.All(w => w.term != "equals"))
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "POI must have a relationship with a \"equals\" term.";
                }

                var targetPoi = rel.location.relationship.First(f => f.term == "equals").targetPOI;

                if (MongoDb.POIs.FindOneById(targetPoi) == null)
                {
                    base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return "Relationship TargetPOI object with Id \"{0}\" not found".FormatWith(targetPoi.ToString());
                }
            }

            #endregion

            //maintain create delete timestamps
            route.created = routeToUpdate.created;
            route.deleted = routeToUpdate.deleted;
            route.updated = DateTime.UtcNow;

            //maintain author
            route.author = routeToUpdate.author;

            var result = MongoDb.Routes.Save(route);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return route;
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
        public object Delete(Routes request)
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

            route route = MongoDb.Routes.FindOneById(id);

            if (route == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return "Route with id \"{0}\" not found.".FormatWith(request.Id);
            }

            if (route.author == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "This Route cannot be deleted. Missing author information";
            }

            var user = this.GetSession();
            if (user.Roles.IsNullOrEmpty() && !route.author.value.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase))
            {
                base.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return "You do not have permissions for deleting this Route.";
            }

            route.deleted = DateTime.UtcNow;

            var result = MongoDb.Routes.Save(route);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return "Route with id \"{0}\" removed successfully.".FormatWith(request.Id);
            }

            if (result.HasLastErrorMessage)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return result.LastErrorMessage;
            }

            base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return "Unknown error";
        }

        public static route BuildRoute(MongoCollection<category> categories, MongoCollection<poi> pois, route r, bool includePois)
        {
            if (r != null)
            {
                if (r.categoryIDs != null && r.categoryIDs.Count > 0)
                {
                    r.category = new List<category>();

                    foreach (var catID in r.categoryIDs)
                    {
                      var cat = categories.FindOneById(catID);

                      if (cat.deleted <= DateTime.Now)
                        continue;

                      foreach (var l in cat.label)
                      {
                        r.category.Add(new category()
                        {
                          term = "category",
                          value = l.value,
                          lang = l.lang
                        });
                      }

                      if (cat.link != null)
                      {
                        if (r.link == null)
                          r.link = new List<POITermType>();

                        foreach (var link in cat.link)
                        {
                          if (link.term == "icon")
                            r.link.Add(link);

                        }
                      }

                        
                     
                    }
                }

                if (!includePois)
                {
                  r.pois = null;
                }
                else
                {
                  foreach (var p in r.pois)
                  {
                    if (p.location != null && !p.location.relationship.IsNullOrEmpty())
                    {
                      var parentPOIR = p.location.relationship.FirstOrDefault(w => w.term == "equals");

                      if (parentPOIR != null && parentPOIR.targetPOI != ObjectId.Empty)
                      {
                        var pPoi = pois.FindOneById(parentPOIR.targetPOI);

                        if (pPoi != null && pPoi.location != null)
                        {
                          if (pPoi.location.point != null)
                          {
                            p.location.point = pPoi.location.point;
                          }
                          else if (pPoi.location.line != null)
                          {
                            p.location.line = pPoi.location.line;
                          }
                          else if (pPoi.location.polygon != null)
                          {
                            p.location.polygon = pPoi.location.polygon;
                          }
                        }
                      }
                    }
                  }
                }
            }

            return r;
        }
    }
}