using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;

namespace HTBox.Web.Models
{
    public class MemberAuthorContext : DbContext
    {
        public MemberAuthorContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Webpages_Roles> WebPagesRoles { get; set; }
        public DbSet<Webpages_UsersInRoles> WebPagesUsersInRoles { get; set; }
        public DbSet<Webpages_OAuthMembership> WebPagesOAuthMembership { get; set; }
        public DbSet<Webpages_Membership> WebPagesMembership { get; set; }
        public DbSet<Webpages_OAuthToken> WebPagesOAuthToken { get; set; }

       
       
    }





}