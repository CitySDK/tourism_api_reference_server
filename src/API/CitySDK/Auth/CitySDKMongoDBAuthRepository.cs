using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ServiceStack.Common;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CitySDK.Auth
{
    public class CitySDKMongoDBAuthRepository : IUserAuthRepository, IClearable
    {
        private readonly Regex ValidUserNameRegEx = new Regex("^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);
        private readonly MongoDatabase mongoDatabase;

        static string UserAuth_Col
        {
            get
            {
                return typeof(UserAuth).Name;
            }
        }

        static string UserOAuthProvider_Col
        {
            get
            {
                return typeof(UserOAuthProvider).Name;
            }
        }

        static string Counters_Col
        {
            get
            {
                return typeof(Counters).Name;
            }
        }

        public CitySDKMongoDBAuthRepository(MongoDatabase mongoDatabase, bool createMissingCollections)
        {
            this.mongoDatabase = mongoDatabase;
            if (createMissingCollections)
                this.CreateMissingCollections();
            if (!this.CollectionsExists())
                throw new InvalidOperationException("One of the collections needed by MongoDBAuthRepository is missing.You can call MongoDBAuthRepository constructor with the parameter CreateMissingCollections set to 'true'  to create the needed collections.");
        }

        public bool CollectionsExists()
        {
            if (this.mongoDatabase.CollectionExists(CitySDKMongoDBAuthRepository.UserAuth_Col) && this.mongoDatabase.CollectionExists(CitySDKMongoDBAuthRepository.UserOAuthProvider_Col))
                return this.mongoDatabase.CollectionExists(CitySDKMongoDBAuthRepository.Counters_Col);
            else
                return false;
        }

        public void CreateMissingCollections()
        {
            if (!this.mongoDatabase.CollectionExists(CitySDKMongoDBAuthRepository.UserAuth_Col))
                this.mongoDatabase.CreateCollection(CitySDKMongoDBAuthRepository.UserAuth_Col);
            if (!this.mongoDatabase.CollectionExists(CitySDKMongoDBAuthRepository.UserOAuthProvider_Col))
                this.mongoDatabase.CreateCollection(CitySDKMongoDBAuthRepository.UserOAuthProvider_Col);
            if (this.mongoDatabase.CollectionExists(CitySDKMongoDBAuthRepository.Counters_Col))
                return;
            this.mongoDatabase.CreateCollection(CitySDKMongoDBAuthRepository.Counters_Col);
            this.mongoDatabase.GetCollection<CitySDKMongoDBAuthRepository.Counters>(CitySDKMongoDBAuthRepository.Counters_Col).Save(new CitySDKMongoDBAuthRepository.Counters());
        }

        public void DropAndReCreateCollections()
        {
            if (this.mongoDatabase.CollectionExists(CitySDKMongoDBAuthRepository.UserAuth_Col))
                this.mongoDatabase.DropCollection(CitySDKMongoDBAuthRepository.UserAuth_Col);
            if (this.mongoDatabase.CollectionExists(CitySDKMongoDBAuthRepository.UserOAuthProvider_Col))
                this.mongoDatabase.DropCollection(CitySDKMongoDBAuthRepository.UserOAuthProvider_Col);
            if (this.mongoDatabase.CollectionExists(CitySDKMongoDBAuthRepository.Counters_Col))
                this.mongoDatabase.DropCollection(CitySDKMongoDBAuthRepository.Counters_Col);
            this.CreateMissingCollections();
        }

        private void ValidateNewUser(UserAuth newUser, string password)
        {
            AssertExtensions.ThrowIfNull((object)newUser, "newUser");
            AssertExtensions.ThrowIfNullOrEmpty(password, "password");
            if (ServiceStack.Common.StringExtensions.IsNullOrEmpty(newUser.UserName) && ServiceStack.Common.StringExtensions.IsNullOrEmpty(newUser.Email))
                throw new ArgumentNullException("UserName or Email is required");
            if (!ServiceStack.Common.StringExtensions.IsNullOrEmpty(newUser.UserName) && !this.ValidUserNameRegEx.IsMatch(newUser.UserName))
                throw new ArgumentException("UserName contains invalid characters", "UserName");
        }

        public UserAuth CreateUserAuth(UserAuth newUser, string password)
        {
            this.ValidateNewUser(newUser, password);
            CitySDKMongoDBAuthRepository.AssertNoExistingUser(this.mongoDatabase, newUser, (UserAuth)null);
            string Hash;
            string Salt;
            new SaltedHash().GetHashAndSaltString(password, out Hash, out Salt);
            DigestAuthFunctions digestAuthFunctions = new DigestAuthFunctions();
            newUser.DigestHA1Hash = digestAuthFunctions.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
            newUser.PasswordHash = Hash;
            newUser.Salt = Salt;
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;
            this.SaveUser(newUser);
            return newUser;
        }

        private void SaveUser(UserAuth userAuth)
        {
            if (userAuth.Id == 0)
                userAuth.Id = this.IncUserAuthCounter();
            this.mongoDatabase.GetCollection<UserAuth>(CitySDKMongoDBAuthRepository.UserAuth_Col).Save(userAuth);
        }

        private int IncUserAuthCounter()
        {
            return this.IncCounter("UserAuthCounter").UserAuthCounter;
        }

        private int IncUserOAuthProviderCounter()
        {
            return this.IncCounter("UserOAuthProviderCounter").UserOAuthProviderCounter;
        }

        private CitySDKMongoDBAuthRepository.Counters IncCounter(string counterName)
        {
            MongoCollection<CitySDKMongoDBAuthRepository.Counters> collection = this.mongoDatabase.GetCollection<CitySDKMongoDBAuthRepository.Counters>(CitySDKMongoDBAuthRepository.Counters_Col);
            UpdateBuilder updateBuilder = Update.Inc(counterName, 1);
            IMongoQuery @null = Query.Null;
            return collection.FindAndModify(@null, SortBy.Null, (IMongoUpdate)updateBuilder, true).GetModifiedDocumentAs<CitySDKMongoDBAuthRepository.Counters>();
        }

        private static void AssertNoExistingUser(MongoDatabase mongoDatabase, UserAuth newUser, UserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                UserAuth userAuthByUserName = CitySDKMongoDBAuthRepository.GetUserAuthByUserName(mongoDatabase, newUser.UserName);
                if (userAuthByUserName != null && (exceptForExistingUser == null || userAuthByUserName.Id != exceptForExistingUser.Id))
                    throw new ArgumentException(ServiceStack.Text.StringExtensions.Fmt("User {0} already exists", new object[1]
          {
            (object) newUser.UserName
          }));
            }
            if (newUser.Email == null)
                return;
            UserAuth userAuthByUserName1 = CitySDKMongoDBAuthRepository.GetUserAuthByUserName(mongoDatabase, newUser.Email);
            if (userAuthByUserName1 == null || exceptForExistingUser != null && userAuthByUserName1.Id == exceptForExistingUser.Id)
                return;
            throw new ArgumentException(ServiceStack.Text.StringExtensions.Fmt("Email {0} already exists", new object[1]
      {
        (object) newUser.Email
      }));
        }

        public UserAuth UpdateUserAuth(UserAuth existingUser, UserAuth newUser, string password)
        {
            this.ValidateNewUser(newUser, password);
            CitySDKMongoDBAuthRepository.AssertNoExistingUser(this.mongoDatabase, newUser, existingUser);
            string Hash = existingUser.PasswordHash;
            string Salt = existingUser.Salt;
            if (password != null)
                new SaltedHash().GetHashAndSaltString(password, out Hash, out Salt);
            string str = existingUser.DigestHA1Hash;
            if (password != null || existingUser.UserName != newUser.UserName)
                str = new DigestAuthFunctions().CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
            newUser.Id = existingUser.Id;
            newUser.PasswordHash = Hash;
            newUser.Salt = Salt;
            newUser.DigestHA1Hash = str;
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;
            newUser.Roles = existingUser.Roles;
            this.SaveUser(newUser);
            return newUser;
        }

        public UserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            return CitySDKMongoDBAuthRepository.GetUserAuthByUserName(this.mongoDatabase, userNameOrEmail);
        }

        private static UserAuth GetUserAuthByUserName(MongoDatabase mongoDatabase, string userNameOrEmail)
        {
            bool flag = userNameOrEmail.Contains("@");
            return mongoDatabase.GetCollection<UserAuth>(CitySDKMongoDBAuthRepository.UserAuth_Col).FindOne(flag ? Query.EQ("Email", (BsonValue)userNameOrEmail) : Query.EQ("UserName", (BsonValue)userNameOrEmail));
        }

        public bool TryAuthenticate(string userName, string password, out UserAuth userAuth)
        {
            userAuth = this.GetUserAuthByUserName(userName);
            if (userAuth == null)
                return false;
            if (new SaltedHash().VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt))
                return true;
            userAuth = (UserAuth)null;
            return false;
        }

        public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string PrivateKey, int NonceTimeOut, string sequence, out UserAuth userAuth)
        {
            userAuth = this.GetUserAuthByUserName(digestHeaders["username"]);
            if (userAuth == null)
                return false;
            if (new DigestAuthFunctions().ValidateResponse(digestHeaders, PrivateKey, NonceTimeOut, userAuth.DigestHA1Hash, sequence))
                return true;
            userAuth = (UserAuth)null;
            return false;
        }

        public void LoadUserAuth(IAuthSession session, IOAuthTokens tokens)
        {
            AssertExtensions.ThrowIfNull((object)session, "session");
            UserAuth userAuth = this.GetUserAuth(session, tokens);
            this.LoadUserAuth(session, userAuth);
        }

        private void LoadUserAuth(IAuthSession session, UserAuth userAuth)
        {
            if (userAuth == null)
                return;
            ServiceStack.Common.ReflectionExtensions.PopulateWith<IAuthSession, UserAuth>(session, userAuth);
            session.UserAuthId = userAuth.Id.ToString((IFormatProvider)CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = this.GetUserOAuthProviders(session.UserAuthId).ConvertAll<IOAuthTokens>((Converter<UserOAuthProvider, IOAuthTokens>)(x => (IOAuthTokens)x));
        }

        public UserAuth GetUserAuth(string userAuthId)
        {
            return this.mongoDatabase.GetCollection<UserAuth>(CitySDKMongoDBAuthRepository.UserAuth_Col).FindOneById((BsonValue)int.Parse(userAuthId));
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            UserAuth userAuth = !ServiceStack.Common.StringExtensions.IsNullOrEmpty(authSession.UserAuthId) ? this.GetUserAuth(authSession.UserAuthId) : ServiceStack.Common.ReflectionExtensions.TranslateTo<UserAuth>((object)authSession);
            if (userAuth.Id == 0 && !ServiceStack.Common.StringExtensions.IsNullOrEmpty(authSession.UserAuthId))
                userAuth.Id = int.Parse(authSession.UserAuthId);
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == new DateTime())
                userAuth.CreatedDate = userAuth.ModifiedDate;
            this.mongoDatabase.GetCollection<UserAuth>(CitySDKMongoDBAuthRepository.UserAuth_Col);
            this.SaveUser(userAuth);
        }

        public void SaveUserAuth(UserAuth userAuth)
        {
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == new DateTime())
                userAuth.CreatedDate = userAuth.ModifiedDate;
            this.SaveUser(userAuth);
        }

        public List<UserOAuthProvider> GetUserOAuthProviders(string userAuthId)
        {
            int.Parse(userAuthId);
            return Enumerable.ToList<UserOAuthProvider>((IEnumerable<UserOAuthProvider>)this.mongoDatabase.GetCollection<UserOAuthProvider>(CitySDKMongoDBAuthRepository.UserOAuthProvider_Col).Find(Query.EQ("UserAuthId", (BsonValue)int.Parse(userAuthId))));
        }

        public UserAuth GetUserAuth(IAuthSession authSession, IOAuthTokens tokens)
        {
            if (!ServiceStack.Common.StringExtensions.IsNullOrEmpty(authSession.UserAuthId))
            {
                UserAuth userAuth = this.GetUserAuth(authSession.UserAuthId);
                if (userAuth != null)
                    return userAuth;
            }
            if (!ServiceStack.Common.StringExtensions.IsNullOrEmpty(authSession.UserAuthName))
            {
                UserAuth userAuthByUserName = this.GetUserAuthByUserName(authSession.UserAuthName);
                if (userAuthByUserName != null)
                    return userAuthByUserName;
            }
            if (tokens == null || ServiceStack.Common.StringExtensions.IsNullOrEmpty(tokens.Provider) || ServiceStack.Common.StringExtensions.IsNullOrEmpty(tokens.UserId))
                return (UserAuth)null;
            UserOAuthProvider one = this.mongoDatabase.GetCollection<UserOAuthProvider>(CitySDKMongoDBAuthRepository.UserOAuthProvider_Col).FindOne(Query.And(Query.EQ("Provider", (BsonValue)tokens.Provider), Query.EQ("UserId", (BsonValue)tokens.UserId)));
            if (one != null)
                return this.mongoDatabase.GetCollection<UserAuth>(CitySDKMongoDBAuthRepository.UserAuth_Col).FindOneById((BsonValue)one.UserAuthId);
            else
                return (UserAuth)null;
        }

        public string CreateOrMergeAuthSession(IAuthSession authSession, IOAuthTokens tokens)
        {
            UserAuth userAuth = this.GetUserAuth(authSession, tokens) ?? new UserAuth();
            IMongoQuery query = Query.And(Query.EQ("Provider", (BsonValue)tokens.Provider), Query.EQ("UserId", (BsonValue)tokens.UserId));
            MongoCollection<UserOAuthProvider> collection = this.mongoDatabase.GetCollection<UserOAuthProvider>(CitySDKMongoDBAuthRepository.UserOAuthProvider_Col);
            UserOAuthProvider userOauthProvider = collection.FindOne(query);
            if (userOauthProvider == null)
                userOauthProvider = new UserOAuthProvider()
                {
                    Provider = tokens.Provider,
                    UserId = tokens.UserId
                };
            userOauthProvider.PopulateMissing(tokens);
            userAuth.PopulateMissing(userOauthProvider);
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == new DateTime())
                userAuth.CreatedDate = userAuth.ModifiedDate;
            this.SaveUser(userAuth);
            if (userOauthProvider.Id == 0)
                userOauthProvider.Id = this.IncUserOAuthProviderCounter();
            userOauthProvider.UserAuthId = userAuth.Id;
            if (userOauthProvider.CreatedDate == new DateTime())
                userOauthProvider.CreatedDate = userAuth.ModifiedDate;
            userOauthProvider.ModifiedDate = userAuth.ModifiedDate;
            collection.Save(userOauthProvider);
            return userOauthProvider.UserAuthId.ToString((IFormatProvider)CultureInfo.InvariantCulture);
        }

        public void Clear()
        {
            this.DropAndReCreateCollections();
        }

        private class Counters
        {
            public int Id { get; set; }

            public int UserAuthCounter { get; set; }

            public int UserOAuthProviderCounter { get; set; }
        }
    }
}
