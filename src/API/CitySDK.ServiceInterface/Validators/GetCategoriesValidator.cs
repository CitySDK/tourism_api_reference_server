using CitySDK.ServiceModel.Operations;
using CitySDK.ServiceModel.Types;
using MongoDB.Bson;
using MongoDB.Driver;
using ServiceStack.Common;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using System.Net;

namespace CitySDK.ServiceInterface.Validators
{
    public class GetCategoriesValidator : IValidator<Categories>
    {
        public IMongoDB MongoDB { get; set; }
        public bool Validate(string verb, Categories obj, out string msg)
        {
            if (MongoDB == null)
                MongoDB = EndpointHost.Config.ServiceManager.Container.TryResolve<IMongoDB>();

            msg = string.Empty;

            if (obj.List.IsNullOrEmpty())
            {
                msg = "List parameter cannot be empty.";
                return false;
            }

            if (!obj.List.Equals("poi", System.StringComparison.InvariantCultureIgnoreCase)
                && !obj.List.Equals("route", System.StringComparison.InvariantCultureIgnoreCase)
                && !obj.List.Equals("event", System.StringComparison.InvariantCultureIgnoreCase))
            {
                msg = "List parameter must be either 'poi', 'route' or 'event'.";
                return false;
            }

            return true;
        }
    }

    public class UpdateCategoryValidator : IValidator<Categories>
    {
        public IMongoDB MongoDB { get; set; }
        public bool Validate(string verb, Categories obj, out string msg)
        {
            if (MongoDB == null)
                MongoDB = EndpointHost.Config.ServiceManager.Container.TryResolve<IMongoDB>();

            msg = string.Empty;

            if (obj.List.IsNullOrEmpty())
            {
                msg = "'List' parameter cannot be empty.";
                return false;
            }

            if (!obj.List.Equals("poi", System.StringComparison.InvariantCultureIgnoreCase)
                && !obj.List.Equals("route", System.StringComparison.InvariantCultureIgnoreCase)
                && !obj.List.Equals("event", System.StringComparison.InvariantCultureIgnoreCase))
            {
                msg = "'List' parameter must be either 'poi', 'route' or 'event'.";
                return false;
            }

            if (obj.category == null)
            {
                msg = "category object is null";
                return false;
            }

            if (obj.category.term.IsNullOrEmpty())
            {
                msg = "'category.term' parameter cannot be empty.";
                return false;
            }

            #region Validate License

            if (obj.category.license != null)
            {
                if (obj.category.license.value.IsNullOrEmpty())
                {
                    msg = "'category.license.value' parameter cannot be empty.";
                    return false;
                }

                if (obj.category.license.term.IsNullOrEmpty())
                {
                    msg = "'category.license.term' parameter cannot be empty.";
                    return false;
                }

                obj.category.license.value = WebUtility.HtmlDecode(obj.category.license.value);
            }

            #endregion

            #region Validate Labels

            if (!obj.category.label.IsNullOrEmpty())
            {
                foreach (var label in obj.category.label)
                {
                    if (label.value.IsNullOrEmpty())
                    {
                        msg = "'category.label.value' parameter cannot be empty.";
                        return false;
                    }

                    if (label.term.IsNullOrEmpty())
                    {
                        msg = "'category.label.term' parameter cannot be empty.";
                        return false;
                    }
                    if (obj.category.lang.IsNullOrEmpty() && label.lang.IsNullOrEmpty())
                    {
                        msg = "'category.lang' and 'category.label.lang' parameter cannot be both empty.";
                        return false;
                    }

                    label.value = WebUtility.HtmlDecode(label.value);
                }
            }

            #endregion

            #region Validate Links

            if (!obj.category.link.IsNullOrEmpty())
            {
                foreach (var link in obj.category.link)
                {
                    if (link.href.IsNullOrEmpty())
                    {
                        msg = "'category.link.href' parameter cannot be empty.";
                        return false;
                    }

                    if (link.term.IsNullOrEmpty())
                    {
                        msg = "'category.link.term' parameter cannot be empty.";
                        return false;
                    }

                    if (link.term == "parent" || link.term == "child")
                    {
                      msg = "category link relationships (parent/child) is managed automaticaly. Please use sub-categories mechanism";
                      return false;
                    }
                }

                
            }

            #endregion

            #region  Validate Categories

            if (!obj.category.categories.IsNullOrEmpty())
            {
                MongoCollection<category> catRep;

                if (obj.List.EqualsIgnoreCase("poi"))
                    catRep = MongoDB.POICategories;
                else if (obj.List.EqualsIgnoreCase("route"))
                    catRep = MongoDB.RouteCategories;
                else
                    catRep = MongoDB.EventCategories;

                foreach (var category in obj.category.categories)
                {
                    if (category._id == null || category._id.Value == ObjectId.Empty)
                    {
                        msg = "'category.categories.id' parameter cannot be empty.";
                        return false;
                    }

                    if (category._id == obj.category._id)
                    {
                      msg = "Circular sub-category reference found (Category can't be its own sub-category).";
                      return false;
                    }

                    category subCat = catRep.FindOneById(category._id);
                    if (subCat == null)
                    {
                        msg = "Missing category.categories object: {0}. All related categories must be inserted before a Category insertion.".FormatWith(category._id.ToString());
                        return false;
                    }

                    
                    if (subCat.link != null)
                    {
                      foreach (var l in subCat.link)
                      {
                        if (l.term == "parent")
                        {
                          if (verb == "PUT")
                          {
                            msg = "Trying to assing sub-category already belonging to another category";
                            return false;
                          }

                          if (verb == "POST" && obj.category._id != null)
                          {
                            if (obj.category._id.Value.ToString() != l.value)
                            {
                              msg = "Trying to assing sub-category already belonging to another category";
                              return false;
                            }
                          }
                          
                        }
                      }
                    }
                }
            }

            #endregion

            #region Validate Deleted

            if (obj.category.deleted != null)
            {
              if(verb == "PUT")
                msg = "Can not insert a deleted category";

              if (verb == "POST")
                msg = "Can not update a deleted category object";

              return false;
            }

            #endregion

            if (verb == "POST")
            {
                if (obj.category._id == null || obj.category._id == ObjectId.Empty)
                {
                    msg = "category._id cannot be empty";
                    return false;
                }

                MongoCollection<category> catRep;

                if (obj.List.EqualsIgnoreCase("poi"))
                    catRep = MongoDB.POICategories;
                else if (obj.List.EqualsIgnoreCase("route"))
                    catRep = MongoDB.RouteCategories;
                else
                    catRep = MongoDB.EventCategories;

                if (catRep.FindOneById(obj.category._id) == null)
                {
                    msg = "Missing category object: {0}. You cannot update an unexisting category.".FormatWith(obj.category._id.ToString());
                    return false;
                }
            }
            
            return true;
        }
    }

    public class DeleteCategoryValidator : IValidator<Categories>
    {
        public IMongoDB MongoDB { get; set; }
        public bool Validate(string verb, Categories obj, out string msg)
        {
            if (MongoDB == null)
                MongoDB = EndpointHost.Config.ServiceManager.Container.TryResolve<IMongoDB>();

            msg = string.Empty;

            if (obj.Id.IsNullOrEmpty())
            {
                msg = "'id' parameter cannot be empty.";
                return false;
            }

            if (obj.List.IsNullOrEmpty())
            {
                msg = "'List' parameter cannot be empty.";
                return false;
            }

            if (!obj.List.Equals("poi", System.StringComparison.InvariantCultureIgnoreCase)
                && !obj.List.Equals("route", System.StringComparison.InvariantCultureIgnoreCase)
                && !obj.List.Equals("event", System.StringComparison.InvariantCultureIgnoreCase))
            {
                msg = "'List' parameter must be either 'poi', 'route' or 'event'.";
                return false;
            }

            ObjectId id;

            if (!ObjectId.TryParse(obj.Id, out id))
            {
                msg = "'id' parameter is invalid.";
                return false;
            }

            MongoCollection<category> catRep;

            if (obj.List.EqualsIgnoreCase("poi"))
                catRep = MongoDB.POICategories;
            else if (obj.List.EqualsIgnoreCase("route"))
                catRep = MongoDB.RouteCategories;
            else
                catRep = MongoDB.EventCategories;

            category cat = catRep.FindOneById(id);

            if (cat == null)
            {
                msg = "Category with id \"{0}\" not found.".FormatWith(obj.Id);
                return false;
            }

            return true;
        }
    }
}