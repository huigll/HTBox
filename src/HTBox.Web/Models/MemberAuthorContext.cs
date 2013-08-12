using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;
using System.Data.Entity.Infrastructure;

namespace HTBox.Web.Models
{
    public class WebPagesContext : DbContext
    {
       
        public WebPagesContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<Webpages_UserProfile> UserProfiles { get; set; }
        public DbSet<Webpages_Roles> WebPagesRoles { get; set; }
        public DbSet<Webpages_UsersInRoles> WebPagesUsersInRoles { get; set; }
        public DbSet<Webpages_OAuthMembership> WebPagesOAuthMembership { get; set; }
        public DbSet<Webpages_Membership> WebPagesMembership { get; set; }
        public DbSet<Webpages_OAuthToken> WebPagesOAuthToken { get; set; }


        public DbSet<MenuTree> MenuTrees { get; set; }
        public DbSet<MenuTreeRight> MenuTreeRights { get; set; }
        public DbSet<Webpages_VUser> Webpages_VUsers { get; set; }
    }





}