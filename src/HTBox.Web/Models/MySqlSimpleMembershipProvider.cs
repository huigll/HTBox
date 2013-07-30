using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Transactions;
using System.Web;
using System.Web.Helpers;
using System.Web.Security;
using WebMatrix.WebData;

namespace HTBox.Web.Models
{
    public class MySqlSimpleMembershipProvider : ExtendedMembershipProvider
    {
        private static string DEFAULT_PROVIDER_NAME = "MySQLMembershipProvider";
        private static string DEFAULT_NAME = "MySqlExtendedMembershipProvider";
        private static string DEFAULT_PROVIDER_CONFIG_NAME = "provider";

        private MembershipProvider preProvider;
        private MemberAuthorContext dbContext;
        private System.Data.Entity.DbSet<UserProfile> userProfiles;
        private System.Data.Entity.DbSet<Webpages_OAuthMembership> oAuthMemberships;
        private System.Data.Entity.DbSet<Webpages_Roles> roles;
        private System.Data.Entity.DbSet<Webpages_UsersInRoles> usersInRoles;
        private System.Data.Entity.DbSet<Webpages_Membership> memberships;

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (string.IsNullOrEmpty(name)) name = DEFAULT_NAME;
            base.Initialize(name, config);

            var providerName = config[DEFAULT_PROVIDER_CONFIG_NAME];
            if (!string.IsNullOrEmpty(providerName))
                this.preProvider = Membership.Providers[providerName] ?? Membership.Providers[DEFAULT_PROVIDER_NAME];
            if (this.preProvider != null)
                this.preProvider.ValidatingPassword += delegate(object sender, ValidatePasswordEventArgs args)
                {
                    this.OnValidatingPassword(args);
                };

            this.dbContext = new MemberAuthorContext();
            this.userProfiles = this.dbContext.UserProfiles;
            this.oAuthMemberships = this.dbContext.WebPagesOAuthMembership;
            this.roles = this.dbContext.WebPagesRoles;
            this.usersInRoles = this.dbContext.WebPagesUsersInRoles;
            this.memberships = this.dbContext.WebPagesMembership;
        }

        public override bool ConfirmAccount(string accountConfirmationToken)
        {
            var rsl = from m in memberships
                      where m.ConfirmationToken == accountConfirmationToken
                      select new { UserId = m.UserId, ConfirmationToken = m.ConfirmationToken };

            if (!rsl.Any()) return false;

            var user = (from m in memberships where m.UserId == rsl.First().UserId select m).FirstOrDefault();
            if (user == null) return false;

            user.IsConfirmed = true;
            dbContext.SaveChanges();
            return true;
        }

        public override bool ConfirmAccount(string userName, string accountConfirmationToken)
        {
            var rsl = from membership in memberships
                      join userProfile in userProfiles on membership.UserId equals userProfile.UserId
                      where membership.ConfirmationToken == accountConfirmationToken &&
                      userProfile.UserName == userName
                      select new { UserId = membership.UserId, ConfirmationToken = membership.ConfirmationToken };

            if (!rsl.Any()) return false;

            var user = (from membership in memberships where membership.UserId == rsl.First().UserId select membership).FirstOrDefault();
            if (user == null) return false;

            user.IsConfirmed = true;
            dbContext.SaveChanges();
            return true;
        }

        public override string CreateAccount(string userName, string password, bool requireConfirmationToken)
        {
            if (String.IsNullOrEmpty(password))
                throw new MembershipCreateUserException(MembershipCreateStatus.InvalidPassword);
            string passwordHash = Crypto.HashPassword(password);
            if (passwordHash.Length > 128)
                throw new MembershipCreateUserException(MembershipCreateStatus.InvalidPassword);

            if (String.IsNullOrEmpty(userName))
                throw new MembershipCreateUserException(MembershipCreateStatus.InvalidUserName);
            
            var user = (from u in userProfiles where u.UserName == userName select u).FirstOrDefault();
            if (user == null)
                throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);
            if ((from m in memberships where m.UserId == user.UserId select m).Any())
                throw new MembershipCreateUserException(MembershipCreateStatus.DuplicateUserName);

            var token = requireConfirmationToken ? GenerateToken() : null;

            memberships.Add(new Webpages_Membership()
            {
                UserId = user.UserId,
                Password = passwordHash,
                CreateDate = DateTime.UtcNow,
                IsConfirmed = !requireConfirmationToken,
                PasswordFailuresSinceLastSuccess = 0,
                PasswordSalt = string.Empty,
                ConfirmationToken = token,
                PasswordChangedDate = DateTime.UtcNow
            });
            
            try
            {
                
             
                dbContext.SaveChanges();
            }
            catch (Exception e)
            {
                throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError.ToString(),e);
            }
            return token;
        }

        private string GenerateToken()
        {
            using (RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[16];
                provider.GetBytes(bytes);
                return HttpServerUtility.UrlTokenEncode(bytes);
            }
        }

        public override string CreateUserAndAccount(string userName, string password, bool requireConfirmation, IDictionary<string, object> values)
        {

            // memberships.Add(new Webpages_Membership()
            //{
            //    UserId = 1111,
            //    Password = "passwordHash",
            //    CreateDate = DateTime.UtcNow,
            //    IsConfirmed = false,
            //    PasswordFailuresSinceLastSuccess = 0,
            //    PasswordSalt = string.Empty,
            //    ConfirmationToken = "token",
            //    PasswordChangedDate = DateTime.UtcNow
            //});
            
            
            //    dbContext.SaveChanges();


            using (TransactionScope ts = new TransactionScope())
            {
                if ((from u in userProfiles where u.UserName == userName select u).FirstOrDefault() != null)
                    throw new MembershipCreateUserException(MembershipCreateStatus.DuplicateUserName);

                UserProfile user = new UserProfile() { UserName = userName };
                userProfiles.Add(user);
                dbContext.SaveChanges();
                




                var rsl = CreateAccount(userName, password, requireConfirmation);
                ts.Complete();
                return rsl;
            }
        }

        public override bool DeleteAccount(string userName)
        {
            var user = (from u in userProfiles where u.UserName == userName select u).FirstOrDefault();
            if (user == null) return false;

            foreach (var membership in from m in memberships where m.UserId == user.UserId select m)
                memberships.Remove(membership);

            dbContext.SaveChanges();
            return true;
        }

        public override string GeneratePasswordResetToken(string userName, int tokenExpirationInMinutesFromNow)
        {
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentNullException("userName");

            var user = getConfirmedUser(userName);
            if (user == null)
                throw new InvalidOperationException();

            var membership = (from m in memberships
                              where m.UserId == user.UserId && m.PasswordVerificationTokenExpirationDate > DateTime.UtcNow
                              select m).FirstOrDefault();

            if (membership.PasswordVerificationToken == null)
            {
                membership.PasswordVerificationToken = GenerateToken();
                membership.PasswordVerificationTokenExpirationDate = DateTime.UtcNow.AddMinutes(tokenExpirationInMinutesFromNow);
                dbContext.SaveChanges();
            }
            return membership.PasswordVerificationToken;
        }

        private Webpages_Membership getConfirmedUser(string userName)
        {
            var rsl = (from u in userProfiles
                       join m in memberships on u.UserId equals m.UserId
                       where m.IsConfirmed == true && u.UserName == userName
                       select m).FirstOrDefault();
            return rsl;
        }

        public override ICollection<OAuthAccountData> GetAccountsForUser(string userName)
        {
            var rsl = new List<OAuthAccountData>();

            foreach (var oAuth in from o in oAuthMemberships
                                  join u in userProfiles on o.UserId equals u.UserId
                                  where u.UserName == userName
                                  select o)
            {
                rsl.Add(new OAuthAccountData(oAuth.Provider, oAuth.ProviderUserId));
            }

            return rsl;
        }

        public override DateTime GetCreateDate(string userName)
        {
            var membership = (from m in memberships
                              join u in userProfiles on m.UserId equals u.UserId
                              where u.UserName == userName
                              select m).FirstOrDefault();
            return membership == null ? DateTime.MinValue : membership.CreateDate.Value;
        }

        public override DateTime GetLastPasswordFailureDate(string userName)
        {
            var membership = (from m in memberships
                              join u in userProfiles on m.UserId equals u.UserId
                              where u.UserName == userName
                              select m).FirstOrDefault();
            return membership == null || membership.LastPasswordFailureDate.HasValue ?
            DateTime.MinValue : membership.LastPasswordFailureDate.Value;
        }

        public override DateTime GetPasswordChangedDate(string userName)
        {
            var membership = (from m in memberships
                              join u in userProfiles on m.UserId equals u.UserId
                              where u.UserName == userName
                              select m).FirstOrDefault();
            return membership == null || membership.PasswordChangedDate.HasValue ?
            DateTime.MinValue : membership.PasswordChangedDate.Value;
        }

        public override int GetPasswordFailuresSinceLastSuccess(string userName)
        {
            var membership = (from m in memberships
                              join u in userProfiles on m.UserId equals u.UserId
                              where u.UserName == userName
                              select m).FirstOrDefault();
            return membership == null ? -1 : membership.PasswordFailuresSinceLastSuccess;
        }

        public override int GetUserIdFromPasswordResetToken(string token)
        {
            var user = (from m in memberships
                        where m.PasswordVerificationToken == token
                        select m).FirstOrDefault();
            return user == null ? -1 : user.UserId;
        }

        public override bool IsConfirmed(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentNullException("userName");

            return getConfirmedUser(userName) != null;
        }

        public override bool ResetPasswordWithToken(string token, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword))
                throw new ArgumentNullException("newPassword");

            var user = (from m in memberships
                        where m.PasswordVerificationToken == token && m.PasswordVerificationTokenExpirationDate > DateTime.UtcNow
                        select m).FirstOrDefault();

            if (user == null)
                return false;

            user.Password = Crypto.HashPassword(newPassword);
            user.PasswordSalt = string.Empty;
            user.PasswordChangedDate = DateTime.UtcNow;
            user.PasswordVerificationToken = null;
            user.PasswordVerificationTokenExpirationDate = null;
            dbContext.SaveChanges();

            return true;
        }

        public override string ApplicationName
        {
            get
            {
                if (preProvider == null)
                    throw new NotSupportedException();
                return preProvider.ApplicationName;
            }
            set
            {
                if (preProvider == null)
                    throw new NotSupportedException();
                preProvider.ApplicationName = value;
            }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            if (preProvider == null) throw new NotSupportedException();
            return preProvider.ChangePassword(username, oldPassword, newPassword);
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            if (preProvider == null) throw new NotSupportedException();
            return preProvider.ChangePasswordQuestionAndAnswer(username, password, newPasswordQuestion, newPasswordAnswer);
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out System.Web.Security.MembershipCreateStatus status)
        {
            if (preProvider == null) throw new NotSupportedException();
            return preProvider.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, providerUserKey, out status);
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            if (preProvider != null)
                return preProvider.DeleteUser(username, deleteAllRelatedData);

            foreach (var u in (from u in userProfiles where u.UserName == username select u))
                userProfiles.Remove(u);

            dbContext.SaveChanges();
            return true;
        }

        public override bool EnablePasswordReset
        {
            get { return preProvider != null && preProvider.EnablePasswordReset; }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return preProvider != null && preProvider.EnablePasswordRetrieval; }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            if (preProvider != null)
                return preProvider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
            else
                throw new NotSupportedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            if (preProvider != null)
                return preProvider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
            else
                throw new NotSupportedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            if (preProvider != null)
                return preProvider.GetAllUsers(pageIndex, pageSize, out totalRecords);
            else
                throw new NotSupportedException();
        }

        public override int GetNumberOfUsersOnline()
        {
            if (preProvider != null)
                return preProvider.GetNumberOfUsersOnline();
            else
                throw new NotSupportedException();
        }

        public override string GetPassword(string username, string answer)
        {
            if (preProvider != null)
                return preProvider.GetPassword(username, answer);
            else
                throw new NotSupportedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            if (preProvider != null)
                return preProvider.GetUser(username, userIsOnline);

            var user = (from u in userProfiles
                        join m in memberships on u.UserId equals m.UserId
                        where u.UserName == username
                        select new
                        {
                            UserId = u.UserId,
                            UserName = u.UserName,
                            CreateDate = m.CreateDate,
                            PasswordChangeDate = m.PasswordChangedDate
                        }).FirstOrDefault();
            return user == null ? null : new MembershipUser(Membership.Provider.Name,
            username, user.UserId, null, null, null, true, false, user.CreateDate.Value,
            DateTime.MinValue, DateTime.MinValue, user.PasswordChangeDate.Value, DateTime.MinValue);
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            if (preProvider != null)
                return preProvider.GetUser(providerUserKey, userIsOnline);

            throw new NotSupportedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            if (preProvider == null)
                throw new NotSupportedException();

            return preProvider.GetUserNameByEmail(email);
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return preProvider == null ? int.MaxValue : preProvider.MaxInvalidPasswordAttempts; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return preProvider == null ? 0 : preProvider.MinRequiredNonAlphanumericCharacters; }
        }

        public override int MinRequiredPasswordLength
        {
            get { return preProvider == null ? 0 : preProvider.MinRequiredPasswordLength; }
        }

        public override int PasswordAttemptWindow
        {
            get { return preProvider == null ? int.MaxValue : preProvider.PasswordAttemptWindow; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return preProvider == null ? MembershipPasswordFormat.Hashed : preProvider.PasswordFormat; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return preProvider == null ? string.Empty : preProvider.PasswordStrengthRegularExpression; }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return preProvider != null && preProvider.RequiresQuestionAndAnswer; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return preProvider != null && preProvider.RequiresUniqueEmail; }
        }

        public override string ResetPassword(string username, string answer)
        {
            if (preProvider == null)
                throw new NotSupportedException();

            return preProvider.ResetPassword(username, answer);
        }

        public override bool UnlockUser(string userName)
        {
            if (preProvider == null)
                throw new NotSupportedException();

            return preProvider.UnlockUser(userName);
        }

        public override void UpdateUser(MembershipUser user)
        {
            if (preProvider == null)
                throw new NotSupportedException();

            preProvider.UpdateUser(user);
        }

        public override bool ValidateUser(string username, string password)
        {
            if (preProvider != null)
                return preProvider.ValidateUser(username, password);

            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException("username");

            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            var user = getConfirmedUser(username);
            if (user == null)
                return false;

            var rsl = user.Password != null && Crypto.VerifyHashedPassword(user.Password, password);
            if (rsl)
                user.PasswordFailuresSinceLastSuccess = 0;
            else
            {
                user.PasswordFailuresSinceLastSuccess += 1;
                user.LastPasswordFailureDate = DateTime.UtcNow;
            }
            dbContext.SaveChanges();
            return rsl;
        }

        public override bool HasLocalAccount(int userId)
        {
            return (from m in memberships where m.UserId == userId select m).Any();
        }

        public override void CreateOrUpdateOAuthAccount(string provider, string providerUserId, string userName)
        {
            if (string.IsNullOrEmpty(userName))
                throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);

            var user = (from u in userProfiles where u.UserName == userName select u).FirstOrDefault();
            if (user == null)
                throw new MembershipCreateUserException(MembershipCreateStatus.InvalidUserName);
            provider = provider.ToUpperInvariant();
            providerUserId = providerUserId.ToUpperInvariant();
            var oAuthUser = (from o in oAuthMemberships where o.Provider == provider 
                                 && o.ProviderUserId == providerUserId select o).FirstOrDefault();
            if (oAuthUser == null)
            {
                oAuthMemberships.Add(new Webpages_OAuthMembership()
                {
                    Provider = provider,
                    ProviderUserId = providerUserId,
                    UserId = user.UserId
                });
            }
            else
            {
                oAuthUser.UserId = user.UserId;
            }
            dbContext.SaveChanges();
        }

        public override void DeleteOAuthAccount(string provider, string providerUserId)
        {
            providerUserId = providerUserId.ToUpperInvariant();
            provider = provider.ToUpperInvariant();
            foreach (var oauth in (from o in oAuthMemberships where o.ProviderUserId == providerUserId
                                       && o.Provider == provider
                                   select o))
                oAuthMemberships.Remove(oauth);
            dbContext.SaveChanges();
        }

        public override void DeleteOAuthToken(string token)
        {
            foreach (var t in (from o in dbContext.WebPagesOAuthToken where o.Token == token select o))
                dbContext.WebPagesOAuthToken.Remove(t);

            dbContext.SaveChanges();
        }

        public override string GetOAuthTokenSecret(string token)
        {
            var secret = (from o in dbContext.WebPagesOAuthToken where o.Token == token select o).FirstOrDefault();
            return secret == null ? null : secret.Secret;
        }

        public override string GetUserNameFromId(int userId)
        {
            var user = (from u in userProfiles where u.UserId == userId select u).FirstOrDefault();
            return user == null ? null : user.UserName;
        }

        public override int GetUserIdFromOAuth(string provider, string providerUserId)
        {
            provider = provider.ToUpperInvariant();
            providerUserId = providerUserId.ToUpperInvariant();
            var user = (from u in oAuthMemberships
                        where u.Provider == provider && u.ProviderUserId == providerUserId
                        select u).FirstOrDefault();
            return user == null ? -1 : user.UserId;
        }

        public override void ReplaceOAuthRequestTokenWithAccessToken(string requestToken, string accessToken, string accessTokenSecret)
        {
            using (TransactionScope ts = new TransactionScope())
            {
                foreach (var oauth in (from o in dbContext.WebPagesOAuthToken where o.Token == requestToken select o))
                    dbContext.WebPagesOAuthToken.Remove(oauth);

                dbContext.SaveChanges();

                StoreOAuthRequestToken(accessToken, accessTokenSecret);
                ts.Complete();
            }
        }

        public override void StoreOAuthRequestToken(string requestToken, string requestTokenSecret)
        {
            var secret = (from oauth in dbContext.WebPagesOAuthToken where oauth.Token == requestToken select oauth).FirstOrDefault();
            if (secret == null)
            {
                dbContext.WebPagesOAuthToken.Add(new Webpages_OAuthToken()
                {
                    Token = requestToken,
                    Secret = requestTokenSecret
                });
            }
            else
            {
                secret.Secret = requestTokenSecret;
            }
            dbContext.SaveChanges();
        }

    }
}