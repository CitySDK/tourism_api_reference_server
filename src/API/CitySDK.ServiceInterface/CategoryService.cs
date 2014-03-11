using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CitySDK.ServiceInterface.Validators;
using CitySDK.ServiceModel.Operations;
using CitySDK.ServiceModel.Types;
using MongoDB.Bson;
using MongoDB.Driver;
using ServiceStack.Common;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using MongoDB.Driver.Builders;

namespace CitySDK.ServiceInterface
{
    public class CategoryService : AService
    {
        private const int MAX_DEPTH = 30;

        public UpdateCategoryValidator UpdateCategoryValidator { get; set; }
        public DeleteCategoryValidator DeleteCategoryValidator { get; set; }
        public GetCategoriesValidator GetCategoriesValidator { get; set; }

        public object Get(Categories request)
        {
            string msg;
            if (!GetCategoriesValidator.Validate("GET", request, out msg))
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return msg;
            }

            int pageLimit = 10;
            int skipResults = 0;

            if (!request.Limit.IsNullOrEmpty())
            {
              pageLimit = int.Parse(request.Limit);
            }

            if (!request.Offset.IsNullOrEmpty())
            {
              skipResults = int.Parse(request.Offset) * ((pageLimit != -1) ? pageLimit : 1);
            }

            
            CategoriesResponse response = new CategoriesResponse();
            IMongoQuery searchQuery = Query.And(Query.NE("link.term", "parent"), Query.Or(Query.NotExists("deleted"), Query.GT("deleted", BsonValue.Create(DateTime.UtcNow))));

            if (request.List.EqualsIgnoreCase("poi"))
            {
                if (pageLimit == -1)
                {
                    foreach (var p in MongoDb.POICategories.Find(searchQuery).SetSkip(skipResults))
                    {
                        FillSubCategories(MongoDb.POICategories, p);
                        response.categories.Add(p);
                    }
                }
                else
                {
                    foreach (var p in MongoDb.POICategories.Find(searchQuery).SetSkip(skipResults).SetLimit(pageLimit))
                    {
                        FillSubCategories(MongoDb.POICategories, p);
                        response.categories.Add(p);
                    }
                }
            }
            else if (request.List.EqualsIgnoreCase("route"))
            {
                if (pageLimit == -1)
                {
                    foreach (var p in MongoDb.RouteCategories.Find(searchQuery).SetSkip(skipResults))
                    {
                        FillSubCategories(MongoDb.RouteCategories, p);
                        response.categories.Add(p);
                    }
                }
                else
                {
                    foreach (var p in MongoDb.RouteCategories.Find(searchQuery).SetSkip(skipResults).SetLimit(pageLimit))
                    {
                        FillSubCategories(MongoDb.RouteCategories, p);
                        response.categories.Add(p);
                    }
                }
            }
            else if (request.List.EqualsIgnoreCase("event"))
            {
                if (pageLimit == -1)
                {
                  foreach (var p in MongoDb.EventCategories.Find(searchQuery).SetSkip(skipResults))
                  {
                    FillSubCategories(MongoDb.EventCategories, p);
                    response.categories.Add(p);
                  }
                }
                else
                {
                  foreach (var p in MongoDb.EventCategories.Find(searchQuery).SetSkip(skipResults).SetLimit(pageLimit))
                  {
                    FillSubCategories(MongoDb.EventCategories, p);
                    response.categories.Add(p);
                  }
                }
            }

            
            return response;
        }

        private void FillSubCategories(MongoCollection<category> database, category cat)
        {
          FillSubCategories(database, 0, cat);
        }

        private void FillSubCategories(MongoCollection<category> database, int depth, category cat)
        {
          // Sanity check of recursiveness check
          if (depth >= MAX_DEPTH)
            return;

          if(cat.categories == null)
          {
            cat.categories = new System.Collections.Generic.List<ServiceModel.Types.category>();
            if(!cat.categoryIDs.IsNullOrEmpty())
            {
              foreach(ObjectId subCatId in cat.categoryIDs)
              {
                category subCat = database.FindOneById(subCatId);

                if(subCat != null)
                  FillSubCategories(database, depth + 1, subCat);

                cat.categories.Add(subCat);
              }
            }
          }
        }


        [Authenticate]
        public object Put(Categories request)
        {
            string msg;
            if (!UpdateCategoryValidator.Validate("PUT", request, out msg))
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return msg;
            }

            category cat = request.category;
            MongoCollection<category> catRep;

            if (request.List.EqualsIgnoreCase("poi"))
              catRep = MongoDb.POICategories;
            else if (request.List.EqualsIgnoreCase("route"))
              catRep = MongoDb.RouteCategories;
            else
              catRep = MongoDb.EventCategories;

            

            if (!cat.categories.IsNullOrEmpty())
            {
                cat.categoryIDs = new List<ObjectId>();

                foreach (var category in cat.categories)
                {
                    cat.categoryIDs.Add(category._id.Value);
                }
            }

            cat.author = new POITermType {term = "primary", value = this.GetSession().UserName};

            cat.created = DateTime.UtcNow;
            cat._id = ObjectId.GenerateNewId();

            
            var result = catRep.Save(cat);

            // Now that we have the category inserted, update subcategories
            if (result.Ok)
            {
              if (!cat.categoryIDs.IsNullOrEmpty())
              {
                foreach (ObjectId subCatId in cat.categoryIDs)
                {
                  category subCat = catRep.FindOneById(subCatId);
                  if (subCat.link == null)
                    subCat.link = new List<POITermType>();

                  //Sanity check, this should be already checked
                  subCat.link.RemoveAll(s => s.term == "parent");

                  subCat.link.Add(new POITermType()
                    {
                      term = "parent",
                      value = cat._id.Value.ToString()
                    });

                  var subCatResult = catRep.Save(subCat);

                  if (!subCatResult.Ok)
                  {
                    base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return "Error updating sub-category";
                  }

                }
              }

              base.Response.StatusCode = (int)HttpStatusCode.OK;
              return cat._id;
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
        public object Post(Categories request)
        {
            string msg;
            if (!UpdateCategoryValidator.Validate("POST", request, out msg))
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return msg;
            }

            MongoCollection<category> catRep;

            if (request.List.EqualsIgnoreCase("poi"))
                catRep = MongoDb.POICategories;
            else if (request.List.EqualsIgnoreCase("route"))
                catRep = MongoDb.RouteCategories;
            else
                catRep = MongoDb.EventCategories;

            var catToUpdate = catRep.FindOneById(request.category._id);
            if (catToUpdate.author == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "This Category cannot be update. Missing DB author information";
            }

            var user = this.GetSession();
            if (user.Roles.IsNullOrEmpty() && !catToUpdate.author.value.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase))
            {
                base.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return "You do not have permissions for deleting this Category.";
            }

            //maintain create delete timestamps
            request.category.created = catToUpdate.created;
            request.category.deleted = catToUpdate.deleted;
            request.category.updated = DateTime.UtcNow;

            //maintain author
            request.category.author = catToUpdate.author;
          
            // If subcategories dissapear, update them
            if (catToUpdate.categoryIDs != null)
            {
              foreach (ObjectId oldSubCategory in catToUpdate.categoryIDs)
              {
                bool found = false;
                if (!request.category.categories.IsNullOrEmpty())
                  foreach (var newSubcat in request.category.categories)
                  {
                    if (oldSubCategory.Equals(newSubcat._id))
                    {
                      found = true;
                      break;
                    }
                  }

                // For categories not found anymore, remove parent link
                if (!found)
                {
                  category tmpCat = catRep.FindOneById(oldSubCategory);

                  if (tmpCat.link != null)
                  {
                    tmpCat.link.RemoveAll(p => p.term == "parent" && p.value == request.category._id.Value.ToString());

                    catRep.Save(tmpCat);
                  }
                }
              }
            }

            // If the category to update has subcategories, add this category as parent
            if (!request.category.categories.IsNullOrEmpty())
            {
              if (request.category.categoryIDs == null)
                request.category.categoryIDs = new List<ObjectId>();

              foreach (var category in request.category.categories)
              {
                request.category.categoryIDs.Add(category._id.Value);

                category subCat = catRep.FindOneById(category._id.Value);

                if (subCat.link == null)
                  subCat.link = new List<POITermType>();

                //Sanity procedure (should be already checked in the validation)
                subCat.link.RemoveAll(s => s.term == "parent");


                subCat.link.Add(new POITermType()
                {
                  term = "parent",
                  value = request.category._id.Value.ToString()
                });


                var subResult = catRep.Save(subCat);
                if (!subResult.Ok)
                {
                  base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                  return "Error updating subcategory";
                }

              }

              request.category.categories = null;
            }

            //If category is already a subcategory, maintain it in the updated category
            if (catToUpdate.link != null)
            {
              POITermType parent = catToUpdate.link.FirstOrDefault(s => s.term == "parent");

              // If category is already a subcategory, add it to the updated category
              if (parent != null)
              {
                if (request.category.link == null)
                  request.category.link = new List<POITermType>();

                request.category.link.Add(parent);
              }
            }

            var result = catRep.Save(request.category);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return "OK";
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
        public object Delete(Categories request)
        {
            string msg;
            if (!DeleteCategoryValidator.Validate("DELETE", request, out msg))
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return msg;
            }

            MongoCollection<category> catRep;

            if (request.List.EqualsIgnoreCase("poi"))
                catRep = MongoDb.POICategories;
            else if (request.List.EqualsIgnoreCase("route"))
                catRep = MongoDb.RouteCategories;
            else
                catRep = MongoDb.EventCategories;

            category cat = catRep.FindOneById(ObjectId.Parse(request.Id));

            if(cat.author == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "This Category cannot be deleted. Missing author information";
            }

            var user = this.GetSession();
            if (user.Roles.IsNullOrEmpty() && !cat.author.value.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase))
            {
                base.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return "You do not have permissions for deleting this Category.";
            }

            cat.deleted = DateTime.UtcNow;

            // Bring the subCategories to root level
            if(!cat.categoryIDs.IsNullOrEmpty())
              foreach (ObjectId subCatId in cat.categoryIDs)
              {
                category subCat = catRep.FindOneById(subCatId);

                subCat.link.RemoveAll(s => s.term == "parent" && s.value == cat._id.Value.ToString());

                catRep.Save(subCat);
              }

            var result = catRep.Save(cat);

            if (result.Ok)
            {
                base.Response.StatusCode = (int)HttpStatusCode.OK;
                return "Category with id \"{0}\" removed successfully.".FormatWith(request.Id);
            }

            if (result.HasLastErrorMessage)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return result.LastErrorMessage;
            }

            base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return "Unknown error";
        }
    }
}