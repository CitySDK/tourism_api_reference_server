using System;
using System.Configuration;
using System.Globalization;
using System.Runtime.Serialization;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceInterface.Validation;
using ServiceStack.WebHost.Endpoints;

namespace CitySDK.Registration
{
    public class CitySDKRegistrationFeature : IPlugin
    {
        private string AtRestPath { get; set; }

        public CitySDKRegistrationFeature()
        {
            this.AtRestPath = "/register";
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<RegistrationService>(AtRestPath);
            appHost.RegisterAs<RegistrationValidator, IValidator<Registration>>();
        }
    }

    [DataContract]
    public class Registration : IReturn<RegistrationResponse>
    {
        [DataMember(Order = 1)]
        public string UserName { get; set; }
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        [DataMember(Order = 3)]
        public string LastName { get; set; }
        [DataMember(Order = 4)]
        public string DisplayName { get; set; }
        [DataMember(Order = 5)]
        public string Email { get; set; }
        [DataMember(Order = 6)]
        public string Password { get; set; }
    }

    [DataContract]
    public class RegistrationResponse
    {
        public RegistrationResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        [DataMember(Order = 1)]
        public string UserId { get; set; }
        [DataMember(Order = 2)]
        public string SessionId { get; set; }
        [DataMember(Order = 3)]
        public string UserName { get; set; }
        [DataMember(Order = 4)]
        public string ReferrerUrl { get; set; }
        [DataMember(Order = 5)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class FullRegistrationValidator : RegistrationValidator
    {
        public FullRegistrationValidator()
        {
            RuleSet(ApplyTo.Post, () => RuleFor(x => x.DisplayName).NotEmpty());
        }
    }

    public class RegistrationValidator : AbstractValidator<Registration>
    {
        public IUserAuthRepository UserAuthRepo { get; set; }

        public RegistrationValidator()
        {
            RuleSet(ApplyTo.Post, () =>
            {
                RuleFor(x => x.Password).NotEmpty();
                RuleFor(x => x.UserName).NotEmpty().When(x => x.Email.IsNullOrEmpty());
                RuleFor(x => x.Email).NotEmpty().EmailAddress().When(x => x.UserName.IsNullOrEmpty());
                RuleFor(x => x.UserName)
                    .Must(x => UserAuthRepo.GetUserAuthByUserName(x) == null)
                    .WithErrorCode("AlreadyExists")
                    .WithMessage("UserName already exists")
                    .When(x => !x.UserName.IsNullOrEmpty());
                RuleFor(x => x.Email)
                    .Must(x => x.IsNullOrEmpty() || UserAuthRepo.GetUserAuthByUserName(x) == null)
                    .WithErrorCode("AlreadyExists")
                    .WithMessage("Email already exists")
                    .When(x => !x.Email.IsNullOrEmpty());
            });
            RuleSet(ApplyTo.Put, () =>
            {
                RuleFor(x => x.UserName).NotEmpty();
                RuleFor(x => x.Email).NotEmpty();
            });
        }
    }

    [DefaultRequest(typeof(Registration))]
    public class RegistrationService : Service
    {
        public IUserAuthRepository UserAuthRepo { get; set; }
        public static ValidateFn ValidateFn { get; set; }

        public IValidator<Registration> RegistrationValidator { get; set; }

        private void AssertUserAuthRepo()
        {
            if (UserAuthRepo == null)
                throw new ConfigurationErrorsException("No IUserAuthRepository has been registered in your AppHost.");
        }

        /// <summary>
        /// Create new Registration
        /// </summary>
        [Authenticate]
        [RequiredRole("Admin")]
        public object Post(Registration request)
        {
            AssertUserAuthRepo();

            if (ValidateFn != null)
            {
                var validateResponse = ValidateFn(this, HttpMethods.Post, request);
                if (validateResponse != null) return validateResponse;
            }

            //var session = this.GetSession();
            var newUserAuth = ToUserAuth(request);
            var existingUser = UserAuthRepo.GetUserAuthByUserName(newUserAuth.UserName);
            
            var registerNewUser = existingUser == null;
            var user = registerNewUser
                ? this.UserAuthRepo.CreateUserAuth(newUserAuth, request.Password)
                : this.UserAuthRepo.UpdateUserAuth(existingUser, newUserAuth, request.Password);

            RegistrationResponse response = new RegistrationResponse
            {
                UserId = user.Id.ToString(CultureInfo.InvariantCulture)
            };

            return response;
        }

        public UserAuth ToUserAuth(Registration request)
        {
            var to = request.TranslateTo<UserAuth>();
            to.PrimaryEmail = request.Email;
            return to;
        }

        /// <summary>
        /// Logic to update UserAuth from Registration info, not enabled on OnPut because of security.
        /// </summary>
        [Authenticate]
        [RequiredRole("Admin")]
        public object UpdateUserAuth(Registration request)
        {
            if (ValidateFn != null)
            {
                var response = ValidateFn(this, HttpMethods.Put, request);
                if (response != null) return response;
            }

            var session = this.GetSession();
            var existingUser = UserAuthRepo.GetUserAuth(session, null);
            if (existingUser == null)
            {
                throw HttpError.NotFound("User does not exist");
            }

            var newUserAuth = ToUserAuth(request);
            UserAuthRepo.UpdateUserAuth(newUserAuth, existingUser, request.Password);

            return new RegistrationResponse
            {
                UserId = existingUser.Id.ToString(CultureInfo.InvariantCulture),
            };
        }
    }
}
