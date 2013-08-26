using System;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Transactions;
using System.Web;
using System.Web.Security;
using HTBox.Web.Models;

namespace HTBox.Web.Models
{
    public class MySqlSimpleRoleProvider : RoleProvider
    {
        private static string Security_NoUserFound = "No user was found that has the name \"{0}\".";

        private static string SimpleRoleProvder_UserNotInRole = "User \"{0}\" is not in role \"{1}\".";
        private static string SimpleRoleProvder_UserAlreadyInRole = "User \"{0}\" is already in role \"{1}\".";
        private static string SimpleRoleProvider_RoleExists = "Role \"{0}\" already exists.";
        private static string SimpleRoleProvder_RolePopulated = "The role \"{0}\" cannot be deleted because there are still users in the role.";
        private static string SimpleRoleProvider_NoRoleFound = "No role was found that has the name \"{0}\".";

        private static string DEFAULT_PROVIDER_NAME = "MySQLRoleProvider";
        private static string DEFAULT_NAME = "MySqlSimpleRoleProvider";
        private static string DEFAULT_PROVIDER_CONFIG_NAME = "provider";

        private RoleProvider preProvider;
        private WebPagesContext dbContext;

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (string.IsNullOrEmpty(name)) name = DEFAULT_NAME;
            base.Initialize(name, config);

            var providerName = config[DEFAULT_PROVIDER_CONFIG_NAME];
            if (!string.IsNullOrEmpty(providerName))
                this.preProvider = Roles.Providers[providerName] ?? Roles.Providers[DEFAULT_PROVIDER_NAME];

            this.dbContext = new WebPagesContext();
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            if (preProvider != null)
            {
                preProvider.AddUsersToRoles(usernames, roleNames);
                return;
            }

            var users = getUserFromNames(usernames);

            var roles = getRoleFromNames(roleNames);

            foreach (var user in users)
            {
                foreach (var role in roles)
                {
                    if (IsUserInRole(user.UserName, role.RoleName))
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        SimpleRoleProvder_UserAlreadyInRole, new object[] { user.UserName, role.RoleName }));

                    dbContext.WebPagesUsersInRoles.Add(new Webpages_UsersInRoles()
                    {
                        UserId = user.UserId,
                        RoleCode = role.Code
                    });

                }
            }
            dbContext.SaveChanges();
        }

        private List<Webpages_Roles> getRoleFromNames(string[] roleNames)
        {
            var roles = new List<Webpages_Roles>();
            foreach (var name in roleNames)
            {
                var role = (from r in dbContext.WebPagesRoles where r.RoleName == name select r).FirstOrDefault();
                if (role == null)
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    SimpleRoleProvider_NoRoleFound, new object[] { name }));
                roles.Add(role);
            }
            return roles;
        }

        private List<Webpages_UserProfile> getUserFromNames(string[] usernames)
        {
            var users = new List<Webpages_UserProfile>();
            foreach (var name in usernames)
            {
                var user = (from u in dbContext.UserProfiles where u.UserName == name select u).FirstOrDefault();
                if (user == null)
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    Security_NoUserFound, new object[] { name }));
                users.Add(user);
            }
            return users;
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

        public override void CreateRole(string roleName)
        {
            if (preProvider != null)
            {
                preProvider.CreateRole(roleName);
                return;
            }

            if ((from r in dbContext.WebPagesRoles where r.RoleName == roleName select r).Any())
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                SimpleRoleProvider_RoleExists, new object[] { roleName }));

            dbContext.WebPagesRoles.Add(new Webpages_Roles() { RoleName = roleName });
            dbContext.SaveChanges();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            if (preProvider != null)
                return preProvider.DeleteRole(roleName, throwOnPopulatedRole);

            var role = (from r in dbContext.WebPagesRoles where r.RoleName == roleName select r).FirstOrDefault();
            if (role == null)
                return false;

            var uirs = from uir in dbContext.WebPagesUsersInRoles where uir.RoleCode == role.Code select uir;
            if (throwOnPopulatedRole && uirs.Any())
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                SimpleRoleProvder_RolePopulated, new object[] { roleName }));

            using (TransactionScope ts = new TransactionScope())
            {
                foreach (var uir in uirs)
                    dbContext.WebPagesUsersInRoles.Remove(uir);
                dbContext.WebPagesRoles.Remove(role);
                dbContext.SaveChanges();
                ts.Complete();
            }
            return true;
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            if (preProvider != null)
                return preProvider.FindUsersInRole(roleName, usernameToMatch);

            return (from u in dbContext.UserProfiles
                    join uir in dbContext.WebPagesUsersInRoles on u.UserId equals uir.UserId
                    join r in dbContext.WebPagesRoles on uir.RoleCode equals r.Code
                    where r.RoleName == roleName && SqlMethods.Like(u.UserName, usernameToMatch)
                    select u.UserName).ToArray();
        }

        public override string[] GetAllRoles()
        {
            if (preProvider != null)
                return preProvider.GetAllRoles();

            return (from r in dbContext.WebPagesRoles select r.RoleName).ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            if (preProvider != null)
                return preProvider.GetRolesForUser(username);

            return (from r in dbContext.WebPagesRoles
                    join uir in dbContext.WebPagesUsersInRoles on r.Code equals uir.RoleCode
                    join u in dbContext.UserProfiles on uir.UserId equals u.UserId
                    where u.UserName == username
                    select r.RoleName).Distinct().ToArray();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            if (preProvider != null)
                return preProvider.GetUsersInRole(roleName);

            return (from u in dbContext.UserProfiles
                    join uir in dbContext.WebPagesUsersInRoles on u.UserId equals uir.UserId
                    join r in dbContext.WebPagesRoles on uir.RoleCode equals r.Code
                    where r.RoleName == roleName
                    select u.UserName).ToArray();
        }
       
        public override bool IsUserInRole(string username, string roleName)
        {
            if (preProvider != null)
                return preProvider.IsUserInRole(username, roleName);

            return (from u in dbContext.UserProfiles
                    join uir in dbContext.WebPagesUsersInRoles on u.UserId equals uir.UserId
                    join r in dbContext.WebPagesRoles on uir.RoleCode equals r.Code
                    where u.UserName == username && r.RoleName == roleName
                    select u).Any();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            if (preProvider != null)
            {
                preProvider.RemoveUsersFromRoles(usernames, roleNames);
                return;
            }

            foreach (var name in roleNames)
            {
                if (!RoleExists(name))
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    SimpleRoleProvider_NoRoleFound, new object[] { name }));
            }

            foreach (var user in usernames)
            {
                foreach (var role in roleNames)
                {
                    if (!IsUserInRole(user, role))
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        SimpleRoleProvder_UserNotInRole, new object[] { user, role }));
                }
            }

            var users = getUserFromNames(usernames);
            var roles = getRoleFromNames(roleNames);

            foreach (var user in users)
            {
                foreach (var role in roles)
                {
                    dbContext.WebPagesUsersInRoles.Remove((from uir in dbContext.WebPagesUsersInRoles
                                                            where uir.RoleCode == role.Code && 
                                                            uir.UserId == user.UserId
                                                            select uir).FirstOrDefault());
                }
            }
            dbContext.SaveChanges();
        }

        public override bool RoleExists(string roleName)
        {
            if (preProvider != null)
                return preProvider.RoleExists(roleName);

            return (from r in dbContext.WebPagesRoles where r.RoleName == roleName select r).Any();
        }

    }
}

