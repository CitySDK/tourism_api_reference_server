using System;
using System.Configuration;
using CitySDK.Auth;
using CitySDK.Registration;
using CitySDK.ServiceInterface;
using CitySDK.ServiceInterface.Validators;
using Funq;
using MongoDB.Driver;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.MiniProfiler;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Cors;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Formats;
using System.Text;
using System.Net;
using System.Linq;
using ServiceStack.Common;

namespace CitySDK
{
    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("CitySDK REST Service", typeof(POIService).Assembly) { }

        public override void Configure(Container container)
        {
          // Set default JSON
          SetConfig(new EndpointHostConfig
          {
            DefaultContentType = ContentType.Json,
            EnableFeatures = Feature.Json,
            DefaultRedirectPath = "/resources",
            MetadataRedirectPath = "/resources"
          });

          // Override asked format (if any)
          PreRequestFilters.Add((req, res) =>
          {
            if (req.AcceptTypes.Length > 0)
              req.AcceptTypes.SetValue(ContentType.Json, 0);
          });

          
            JsConfig.DateHandler = JsonDateHandler.ISO8601;

            InitializeDB(container);

            Plugins.Add(new CorsFeature(allowedHeaders: "*", allowCredentials: true));
            Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] { new CredentialsAuthProvider() })
            {
                HtmlRedirect = null,
                IncludeAssignRoleServices = false,
                RegisterPlugins = new System.Collections.Generic.List<IPlugin> { new CitySDKRegistrationFeature() }
            });

            container.Register<ICacheClient>(new MemoryCacheClient());

            InitializeValidators(container);
        }

        private void InitializeDB(Container container)
        {
            container.Register<IMongoDB>(new MongoDb());

            var server = new MongoClient(ConfigurationManager.AppSettings["MongoServer"]).GetServer();
            var db = server.GetDatabase(ConfigurationManager.AppSettings["MongoDatabase"]);

            CitySDKMongoDBAuthRepository userRep;
            container.Register<IUserAuthRepository>(userRep = new CitySDKMongoDBAuthRepository(db, true));

            if (userRep.GetUserAuthByUserName("admin") == null)
            {
                //Add a user for testing purposes
                string hash;
                string salt;
                new SaltedHash().GetHashAndSaltString("defaultCitySDKPassword", out hash, out salt);
                userRep.CreateUserAuth(new UserAuth
                {
                    Id = 0,
                    DisplayName = "Administrator",
                    Email = "",
                    UserName = "admin",
                    FirstName = "CitySDK",
                    LastName = "Administrator",
                    PasswordHash = hash,
                    Salt = salt,
                    Roles = { RoleNames.Admin }
                }, "defaultCitySDKPassword");
            }
        }

        private void InitializeValidators(Container container)
        {
            container.Register(new GetCategoriesValidator());
            container.Register(new UpdateCategoryValidator());
            container.Register(new DeleteCategoryValidator());
        }

        public override IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
          return new CitySDKServiceRunner<TRequest>(this, actionContext);
        }
    }

    public class CitySDKServiceRunner<TRequest> : ServiceRunner<TRequest>
    {
      public CitySDKServiceRunner(IAppHost appHost, ActionContext actionContext)
        : base(appHost, actionContext)
      {
      }

      public override void OnBeforeExecute(IRequestContext requestContext, TRequest request)
      {
        base.OnBeforeExecute(requestContext, request);
      }

      public override object OnAfterExecute(IRequestContext requestContext, object response)
      {
        if ((response != null) && !(response is CompressedResult))
          response = requestContext.ToOptimizedResult(response);

        return base.OnAfterExecute(requestContext, response);
      }

      public override object HandleException(IRequestContext requestContext, TRequest request, Exception ex)
      {
        return base.HandleException(requestContext, request, ex);
      }
    }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            new AppHost().Init();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (Request.IsLocal)
                Profiler.Start();
        }

        protected void Application_EndRequest()
        {
            Profiler.Stop();
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}