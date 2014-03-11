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
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using DDay.iCal;
using System.IO;
using System.Text;

namespace CitySDK.ServiceInterface
{
    public class EventService : AService
    {
        public object Get(Events request)
        {
            if (!request.Id.IsNullOrEmpty())
            {
                var objectID = ObjectId.Parse(request.Id);

                var ev = MongoDb.Events.FindOneById(objectID);

                return BuildEvent(MongoDb.EventCategories, MongoDb.POIs, ev);
            }

            List<IMongoQuery> queries = new List<IMongoQuery>();

            //Category
            if (!request.Category.IsNullOrEmpty())
            {
                List<IMongoQuery> categoriesList = new List<IMongoQuery>();

                foreach (string category in request.Category.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var catsTofind = MongoDb.EventCategories.Find(
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
                    return new EventsResponse();
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

            //Time
            if (!request.Time.IsNullOrEmpty())
            {
                string[] dates = request.Time.Split(' ');

                if (dates.Length == 2)
                {
                    DateTime start = DateTime.Parse(dates[0], null, DateTimeStyles.RoundtripKind);
                    DateTime end = DateTime.Parse(dates[1], null, DateTimeStyles.RoundtripKind);

                    //queries.Add(Query.And(Query.GTE("Start", start), Query.LTE("End", end)));
                    queries.Add(Query.And(Query.GTE("End", start), Query.LTE("Start", end)));
                }
            }

            QueryDocument qDoc = new QueryDocument();

            queries.Add(Query.Or(Query.NotExists("deleted"), Query.GT("deleted", BsonValue.Create(DateTime.UtcNow))));
            qDoc.AddRange(Query.And(queries).ToBsonDocument());

            MongoCursor<@event> events = MongoDb.Events.Find(qDoc);

            List<@event> finalEvents = events.ToList();

            if (!string.IsNullOrEmpty(request.Coords))
            {
                finalEvents = new List<@event>();

                var validPois = MongoDb.POIs.Find(Utilities.GetGeoIntersect(request.Coords)).ToList();

                foreach (var ev in events.Where(e => e.location != null && e.location.relationship != null && e.location.relationship.Any()))
                {
                    var rel = ev.location.relationship.AsQueryable().FirstOrDefault(w => w.term == "within" || w.term == "equals");
                    if (rel != null)
                    {
                        if (validPois.Any(a => a._id == rel.targetPOI))
                        {
                            finalEvents.Add(ev);
                        }
                    }
                }
            }

            /*
            if (!request.Show.IsNullOrEmpty())
            {
                int start = int.Parse(request.Show.SplitOnFirst(',')[0]);
                int end = int.Parse(request.Show.SplitOnFirst(',')[1]);

                finalEvents = finalEvents.Skip(start).Take(end - start).ToList();
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
              finalEvents = finalEvents.Skip(skipResults).Take(pageLimit).ToList();
            else
              finalEvents = finalEvents.Skip(skipResults).ToList();

            EventsResponse response = new EventsResponse();
            foreach (var p in finalEvents)
            {
              @event e = BuildEvent(MongoDb.EventCategories, MongoDb.POIs, p);

                if (e != null)
                    response.@event.Add(e);
            }

            return response;
        }

        [Authenticate]
        public object Put(Events request)
        {
            #region Validate First Level

            if (request == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid request object";
            }

            if (request.@event == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Null event object";
            }

            @event ev = request.@event;

            if (ev.@base.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty 'base' field";
            }

          /*
            if (ev.lang.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty 'lang' field";
            }
          */
            #endregion

            #region Validate License

            if (ev.license != null)
            {
                if (ev.license.value.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'value' field";
                }

                if (ev.license.term.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'term' field";
                }

                ev.license.value = WebUtility.HtmlDecode(ev.license.value);
            }

            #endregion

            #region Validate Labels

            if (!ev.label.IsNullOrEmpty())
            {
                foreach (var label in ev.label)
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

                    if (ev.lang.IsNullOrEmpty() && label.lang.IsNullOrEmpty())
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Empty Label 'language' or root event language";
                    }

                    label.value = WebUtility.HtmlDecode(label.value);
                }
            }

            #endregion

            #region Validate Description
            if (!ev.description.IsNullOrEmpty())
            {
              foreach (var descr in ev.description)
              {
                if (descr.value.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Missing Description 'value'";
                }

                if (ev.lang.IsNullOrEmpty() && descr.lang.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Empty Description 'language' or root event language";
                }

                if (!descr.type.IsNullOrEmpty())
                {
                  if (DESCRIPTION_TYPES.Contains(descr.type.ToLower()))
                  {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Description type present and is not recognized";
                  }
                }

                descr.value = WebUtility.HtmlDecode(descr.value);
              }
            }
            #endregion

            #region  Validate Categories

            if (!ev.category.IsNullOrEmpty())
            {
                ev.categoryIDs = new List<ObjectId>();
                foreach (var category in ev.category)
                {
                    if (category._id == null || category._id == ObjectId.Empty)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Category 'Id' field";
                    }

                    if (MongoDb.EventCategories.FindOneById(category._id) == null)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return "Missing Category object: {0}. All categories must be inserted before an Event insertion.".FormatWith(category._id.ToString());
                    }

                    ev.categoryIDs.Add(category._id.Value);
                }
            }

            #endregion

            #region Validate Links

            if (!ev.link.IsNullOrEmpty())
            {
                foreach (var l in ev.link)
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
                }
            }

            #endregion

            #region  Validate relationships
            if (ev.location != null && !ev.location.relationship.IsNullOrEmpty())
            {
                foreach (var rel in ev.location.relationship)
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
                    
                    if(!RELATIONSHIP_TERMS.Contains(rel.term.ToLower()))
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Invalid relationship term";
                    }

                    if (MongoDb.POIs.FindOneById(rel.targetPOI) == null)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return "Relationship TargetPOI object with Id \"{0}\" not found".FormatWith(rel.targetPOI.ToString());
                    }
                }
            }
            #endregion

            #region Fill Coords

            if (ev.location != null && !ev.location.point.IsNullOrEmpty())
            {
                foreach (var point in ev.location.point)
                {
                    if (point != null && point.Point != null && !point.Point.posList.IsNullOrEmpty())
                    {
                        double xx, yy;
                        string[] pps = point.Point.posList.Split(' ');

                        if (pps.Length != 2 ||
                            !double.TryParse(pps[0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture, out yy) ||
                            !double.TryParse(pps[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture, out xx))
                        {
                            base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return "Invalid posList.";
                        }

                        //point.Point.Coordinates = new List<Geometry>();
                        //point.Point.Coordinates.Add(new Geometry { @long = xx, lat = yy });
                    }
                }
            }

            #endregion

            #region Calendar Validation

            if (ev.time.IsNullOrEmpty() || ev.time.Count > 0)
            {

              DateTime eventStart = DateTime.MaxValue;
              DateTime eventEnd = DateTime.MinValue;

              foreach (POITermType time in ev.time)
              {
                if (time.type.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Missing time element type";
                }

                if (!time.type.Equals("text/icalendar"))
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Invalid time element type: " + time.type;
                }

                if (time.term.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Missing time element term";
                }

                if (!TIME_TERMS.Contains(time.term.ToLower()))
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Invalid time element term: " + time.term;
                }

                iCalendarCollection loadedICals = (iCalendarCollection)iCalendar.LoadFromStream(new MemoryStream(UTF8Encoding.Default.GetBytes(time.value)));

                foreach (iCalendar ical in loadedICals)
                {
                  foreach (Event evCal in ical.Events)
                  {
                    DateTime tmpStart = new DateTime(evCal.Start.Year, evCal.Start.Month, evCal.Start.Day, evCal.Start.Hour, evCal.Start.Minute, evCal.Start.Second);
                    DateTime tmpEnd = new DateTime(evCal.End.Year, evCal.End.Month, evCal.End.Day, evCal.End.Hour, evCal.End.Minute, evCal.End.Second);

                    if (evCal.Start != null)
                    {
                      if (eventStart.CompareTo(tmpStart) > 0)
                        eventStart = tmpStart;
                    }
                    else
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Unable to retrieve event start time (DTSTART) of iCal element";
                    }

                    if (evCal.End != null)
                    {
                      if (eventEnd.CompareTo(tmpEnd) < 0)
                        eventEnd = tmpEnd;
                    }
                    else
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Unable to retrieve event end time (DTEND) of iCal element";
                    }
                  }
                }

              }

              if (eventStart != DateTime.MaxValue && eventEnd != DateTime.MinValue)
              {
                ev.Start = eventStart;
                ev.End = eventEnd;
              }
            }

            #endregion
            
            string currentUsername = this.GetSession().UserName;
            
            if(currentUsername != "admin")
              ev.author = new POITermType { term = "primary", value = currentUsername };

            ev.created = DateTime.UtcNow;
            ev._id = ObjectId.GenerateNewId();

            var result = MongoDb.Events.Save(ev);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return ev;
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
        public object Post(Events request)
        {
            #region Validate First Level

            if (request == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid request object";
            }

            if (request.@event == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Null event object";
            }

            @event ev = request.@event;

            if (ev._id == null || ev._id == ObjectId.Empty)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Invalid 'id' field";
            }

            @event eventToUpdate = MongoDb.Events.FindOneById(ev._id);

            if (eventToUpdate == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return "Event with 'id' {0} not found.".FormatWith(ev._id);
            }

            var user = this.GetSession();
            string currentUsername = user.UserName;

            if (eventToUpdate.author == null && currentUsername != "admin")
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "This Event can only be updated by admin user. Missing DB author information";
            }

            if (user.Roles.IsNullOrEmpty() && !eventToUpdate.author.value.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase))
            {
                base.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return "You do not have permissions for updating this Event.";
            }

            if (ev.@base.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty 'base' field";
            }

          /* Dropped requirement
            if (ev.lang.IsNullOrEmpty())
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "Empty 'lang' field";
            }
          */

            #endregion

            #region Validate License

            if (ev.license != null)
            {
                if (ev.license.value.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'value' field";
                }

                if (ev.license.term.IsNullOrEmpty())
                {
                    base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return "Empty License 'term' field";
                }

                ev.license.value = WebUtility.HtmlDecode(ev.license.value);
            }

            #endregion

            #region Validate Labels

            if (!ev.label.IsNullOrEmpty())
            {
                foreach (var label in ev.label)
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

                    if (ev.lang.IsNullOrEmpty() && label.lang.IsNullOrEmpty())
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Empty Label 'language' or root event language";
                    }

                    label.value = WebUtility.HtmlDecode(label.value);
                }
            }

            #endregion

            #region Validate Description
            if (!ev.description.IsNullOrEmpty())
            {
              foreach (var descr in ev.description)
              {
                if (descr.value.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Missing Description 'value'";
                }

                if (ev.lang.IsNullOrEmpty() && descr.lang.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Empty Description 'language' or root event language";
                }

                descr.value = WebUtility.HtmlDecode(descr.value);

              }
            }
            #endregion

            #region  Validate Categories

            if (!ev.category.IsNullOrEmpty())
            {
                ev.categoryIDs = new List<ObjectId>();
                foreach (var category in ev.category)
                {
                    if (category._id == null || category._id == ObjectId.Empty)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return "Empty Category 'Id' field";
                    }

                    if (MongoDb.EventCategories.FindOneById(category._id) == null)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return "Missing Category object: {0}. All categories must be inserted before an Event insertion.".FormatWith(category._id.ToString());
                    }

                    ev.categoryIDs.Add(category._id.Value);
                }
            }

            #endregion

            #region Validate Links

            if (!ev.link.IsNullOrEmpty())
            {
                foreach (var l in ev.link)
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
                        return "Invalid link term";
                    }
                }
            }

            #endregion

            #region  Validate relationships
            if (ev.location != null && !ev.location.relationship.IsNullOrEmpty())
            {
                foreach (var rel in ev.location.relationship)
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
                      return "Invalid relationship term";
                    }

                    if (MongoDb.POIs.FindOneById(rel.targetPOI) == null)
                    {
                        base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return "Relationship TargetPOI object with Id \"{0}\" not found".FormatWith(rel.targetPOI.ToString());
                    }
                }
            }
            #endregion

            #region Fill Coords

            if (ev.location != null && !ev.location.point.IsNullOrEmpty())
            {
                foreach (var point in ev.location.point)
                {
                    if (point != null && point.Point != null && !point.Point.posList.IsNullOrEmpty())
                    {
                        double xx, yy;
                        string[] pps = point.Point.posList.Split(' ');

                        if (pps.Length != 2 ||
                            !double.TryParse(pps[0], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture, out yy) ||
                            !double.TryParse(pps[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.Number, CultureInfo.InvariantCulture, out xx))
                        {
                            base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return "Invalid posList.";
                        }

                        //point.Point.Coordinates = new List<Geometry>();
                        //point.Point.Coordinates.Add(new Geometry { @long = xx, lat = yy });
                    }
                }
            }

            #endregion

            #region Validate Time/Calendar

            if (ev.time.IsNullOrEmpty() || ev.time.Count > 0)
            {

              DateTime eventStart = DateTime.MaxValue;
              DateTime eventEnd = DateTime.MinValue;

              foreach (POITermType time in ev.time)
              {
                if (time.type.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Missing time element type";
                }

                if (!time.type.Equals("text/icalendar"))
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Invalid time element type: " + time.type;
                }

                if (time.term.IsNullOrEmpty())
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Missing time element term";
                }

                if (!TIME_TERMS.Contains(time.term.ToLower()))
                {
                  base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return "Invalid time element term: " + time.term;
                }

                iCalendarCollection loadedICals = (iCalendarCollection)iCalendar.LoadFromStream(new MemoryStream(UTF8Encoding.Default.GetBytes(time.value)));

                foreach (iCalendar ical in loadedICals)
                {
                  foreach (Event evCal in ical.Events)
                  {
                    DateTime tmpStart = new DateTime(evCal.Start.Year, evCal.Start.Month, evCal.Start.Day, evCal.Start.Hour, evCal.Start.Minute, evCal.Start.Second);
                    DateTime tmpEnd = new DateTime(evCal.End.Year, evCal.End.Month, evCal.End.Day, evCal.End.Hour, evCal.End.Minute, evCal.End.Second);

                    if (evCal.Start != null)
                    {
                      if (eventStart.CompareTo(tmpStart) > 0)
                        eventStart = tmpStart;
                    }
                    else
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Unable to retrieve event start time (DTSTART) of iCal element";
                    }

                    if (evCal.End != null)
                    {
                      if (eventEnd.CompareTo(tmpEnd) < 0)
                        eventEnd = tmpEnd;
                    }
                    else
                    {
                      base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      return "Unable to retrieve event end time (DTEND) of iCal element";
                    }
                  }
                }
              }

              if (eventStart != DateTime.MaxValue && eventEnd != DateTime.MinValue)
              {
                ev.Start = eventStart;
                ev.End = eventEnd;
              }
              else
              {
                ev.Start = null;
                ev.End = null;
              }
            }


            #endregion

            //maintain create delete timestamps
            ev.created = eventToUpdate.created;
            ev.deleted = eventToUpdate.deleted;
            ev.updated = DateTime.UtcNow;

            //maintain author
            ev.author = eventToUpdate.author;

            var result = MongoDb.Events.Save(ev);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return ev;
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
        public object Delete(Events request)
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

            @event @event = MongoDb.Events.FindOneById(id);

            if (@event == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return "Event with id \"{0}\" not found.".FormatWith(request.Id);
            }

            if (@event.author == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "This Event cannot be deleted. Missing author information";
            }

            var user = this.GetSession();
            if (user.Roles.IsNullOrEmpty() && !@event.author.value.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase))
            {
                base.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return "You do not have permissions for deleting this Event.";
            }

            @event.deleted = DateTime.UtcNow;

            var result = MongoDb.Events.Save(@event);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return "Event with id \"{0}\" removed successfully.".FormatWith(request.Id);
            }

            if (result.HasLastErrorMessage)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return result.LastErrorMessage;
            }

            base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return "Unknown error";
        }

        public static @event BuildEvent(MongoCollection<category> categories, MongoCollection<poi> pois, @event ev)
        {
            if (ev != null)
            {
                if (ev.deleted < DateTime.UtcNow)
                    return null;

                if (ev.categoryIDs != null && ev.categoryIDs.Count > 0)
                {
                    ev.category = new List<category>();

                    foreach (var catID in ev.categoryIDs)
                    {
                      var cat = categories.FindOneById(catID);

                      if (cat.deleted <= DateTime.Now)
                        continue;

                      foreach (var l in cat.label)
                      {
                        ev.category.Add(new category()
                          {
                            lang = l.lang,
                            value = l.value,
                            term = "category"
                          }
                        );

                        
                        if (cat.link != null)
                        {
                          if (ev.link == null)
                            ev.link = new List<POITermType>();

                          foreach (var link in cat.link)
                          {
                            if (link.term == "icon")
                              ev.link.Add(link);
                          }
                        }
                      }
                    }
                }

                // Fill the coordinates
                if (ev.location.relationship != null && ev.location.relationship.Count > 0)
                {
                  List<location> poiLocations = new List<location>();

                  // Find related pois
                  foreach (var relation in ev.location.relationship)
                  {
                    if (relation.targetPOI == null)
                      continue;

                    poi eventPoi = pois.FindOneById(relation.targetPOI);
                    if (eventPoi == null)
                      continue;
                    
                    // Store related pois locations
                    poiLocations.Add(eventPoi.location);
                  }

                  // Add coordinates to the event
                  foreach (location loc in poiLocations)
                  {
                    if (loc.line != null && loc.line.Count > 0)
                    {
                      if (ev.location.line == null)
                        ev.location.line = loc.line;
                      else
                        ev.location.line.AddRange(loc.line);
                    }

                    if (loc.point != null && loc.point.Count > 0)
                    {
                      if (ev.location.point == null)
                        ev.location.point = loc.point;
                      else
                        ev.location.point.AddRange(loc.point);
                    }

                    if (loc.polygon != null && loc.polygon.Count > 0)
                    {
                      if (ev.location.polygon == null)
                        ev.location.polygon = loc.polygon;
                      else
                        ev.location.polygon.AddRange(loc.polygon);
                    }
                  }
                }
            }

            return ev;
        }
    }
}