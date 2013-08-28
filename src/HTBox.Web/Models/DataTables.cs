using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;
using System.Linq;

namespace HTBox.Web.Models
{

    [Table("webpages_UserProfile")]
    public class Webpages_UserProfile
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        [Required,MaxLength(128)] 
        public string UserName { get; set; }
        [MaxLength(128)]
        public string Email { get; set; }
        public int IndexOrder { get; set; }
    }

    [Table("webpages_Roles")]
    public class Webpages_Roles
    {
        [Required,MaxLength(128)] 
        public string RoleName { get; set; }
        public int Type { get; set; }
        [Key]
        [MaxLength(128)]
        public string Code { get; set; }
        [Required]
        public int Deep { get; set; }
        public int IndexOrder { get; set; }
        [MaxLength(128)] 
        public string Master { get; set; }
        [MaxLength(4000)] 
        public string Expression { get; set; }
        public string Remark { get; set; }


        public Webpages_Roles[] GetOneFloorGroups(WebPagesContext db=null)
        {
            bool flag = db == null;
            try
            {
                if (flag) db = new WebPagesContext();
                string[] tmp = this.Code.Split('-');
                int TreeDeep = tmp.Length;
                int type = Convert.ToInt32(tmp[0]);

                return (from m in db.WebPagesRoles
                            where m.Deep == TreeDeep &&
                            m.Type == type &&
                            m.Code.IndexOf(this.Code + "-") == 0
                            orderby m.IndexOrder
                            select m).ToArray();


            }
            finally
            {
                if (flag)
                    db.Dispose();
            }
        }
        public Webpages_Roles GetSubRoleByName(string roleName, WebPagesContext db = null)
        {
            bool flag = db == null;
            try
            {
                if (flag) db = new WebPagesContext();
                string[] tmp = this.Code.Split('-');
                int TreeDeep = tmp.Length;
                int type = Convert.ToInt32(tmp[0]);

                return (from m in db.WebPagesRoles
                        where m.RoleName == roleName &&  m.Deep == TreeDeep &&
                        m.Type == type &&
                        m.Code.IndexOf(this.Code + "-") == 0
                        orderby m.IndexOrder
                        select m).FirstOrDefault();
            }
            finally
            {
                if (flag)
                    db.Dispose();
            }
        }
        public static Webpages_Roles GetOrCreateRoot(int type=1,string rootName="Root",WebPagesContext db=null)
        {
            bool flag = db == null;
            try
            {
                string code = type.ToString();
                if (flag) db = new WebPagesContext();
                var root = db.WebPagesRoles.Where(o => o.Type == type && o.RoleName == rootName &&
                    o.Deep == 0 && o.Code == code).FirstOrDefault();
                if (root == null)
                {
                    root = new Webpages_Roles()
                    {
                        Type = type,
                        RoleName = rootName,
                        Deep = 0,
                        Code = type.ToString(),
                    };
                    db.WebPagesRoles.Add(root);
                    db.SaveChanges();
                }
                return root;


            }
            finally
            {
                if (flag)
                    db.Dispose();
            }
        }

        public Webpages_UserProfile[] GetUsers(bool isWhole, WebPagesContext db = null)
        {
            bool flag = db == null;
            try
            {
                if (flag) db = new WebPagesContext();
                string[] tmp = this.Code.Split('-');
                int TreeDeep = tmp.Length;
                int type = Convert.ToInt32(tmp[0]);
                IQueryable<Webpages_Roles> allRoles;
                if (isWhole)
                {
                    allRoles = from g in db.WebPagesRoles
                               where g.Code == Code ||
                               g.Code.IndexOf(Code + "-") == 0

                               select g;
                }
                else
                {
                    allRoles = from g in db.WebPagesRoles
                               where g.Code == Code select g;
                }
                var allUser = (from u in db.UserProfiles
                               from map in db.WebPagesUsersInRoles
                               from al in allRoles
                               where u.UserId == map.UserId &&
                               map.RoleCode == al.Code
                               orderby u.IndexOrder
                               select u).ToArray();
                return allUser;

            }
            finally
            {
                if (flag)
                    db.Dispose();
            }
        }
    }
    [Table("webpages_UsersInRoles")]
    public class Webpages_UsersInRoles
    {
        [Key, Column(Order = 0)]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }
        [Key, Column(Order = 1)]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public String RoleCode { get; set; }
    }
    [Table("webpages_OAuthMembership")]
    public class Webpages_OAuthMembership
    {
        [Key, Column(Order = 0)]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public string Provider { get; set; }
        [Key, Column(Order = 1)]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public string ProviderUserId { get; set; }
        public int UserId { get; set; }
    }
    [Table("webpages_Membership")]
    public class Webpages_Membership
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }
        public DateTime? CreateDate { get; set; }
        public string ConfirmationToken { get; set; }
        public bool IsConfirmed { get; set; }
        public DateTime? LastPasswordFailureDate { get; set; }
        public int PasswordFailuresSinceLastSuccess { get; set; }
        public string Password { get; set; }

        public DateTime? PasswordChangedDate { get; set; }

        public string PasswordSalt { get; set; }

        public string PasswordVerificationToken { get; set; }
        public DateTime? PasswordVerificationTokenExpirationDate { get; set; }
    }


    [Table("webpages_OAuthToken")]
    public class Webpages_OAuthToken
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public string Token { get; set; }
        public string Secret { get; set; }

    }


    [Table("webpages_MenuTree")]
    public class MenuTree
    {

        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int MenuId { get; set; }
        [Required,MaxLength(128)]
        public string MenuName { get; set; }
        public int? ParentId { get; set; }
        public string PageUrl { get; set; }
        public bool IsHidden { get; set; }
        
        public int OrderIndex { get; set; }
        public bool IsPublic { get; set; }
        [MaxLength(50)]
        public string OpenTarget { get; set; }

    }
    [Table("webpages_MenuTreeRight")]
    public class MenuTreeRight
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        [Key, Column(Order = 0)]
        public int MenuId { get; set; }

        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        [Key, Column(Order = 1)]
        public int VuserID { get; set; }

        [ForeignKey("VuserID")]
        public Webpages_VUser Vuser { get; set; }
        [ForeignKey("MenuId")]
        public MenuTree Menu { get; set; }

        public int RightType { get; set; }
    }

    [Table ("webpages_VUser")]
    public class Webpages_VUser
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]

        public int VUserId { get; set; }
        [ForeignKey("RoleID")]
        public Webpages_Roles Role { get; set; }

        
        public string RoleID { get; set; }


        [ForeignKey("UserID")]
        public Webpages_UserProfile User { get; set; }

        
        public int UserID { get; set; }

        [Required]
        public int Type { get; set; }



        public static Webpages_VUser Find(int vuserid)
        {
            using (var db = new WebPagesContext())
            {
                return db.Webpages_VUsers.FirstOrDefault(o => o.VUserId == vuserid);
            }
        }

        public static Webpages_VUser CreateOrGetByUserId(int userid)
        {
            using (var db = new WebPagesContext())
            {
                var vuser = db.Webpages_VUsers.FirstOrDefault(o => o.UserID == userid);
                if (vuser != null)
                    return vuser;
                if (db.UserProfiles.Find(userid) == null)
                    return null;
                vuser = new Webpages_VUser();
                vuser.UserID = userid;
                vuser.Type = (int)VUserType.User;
                db.Webpages_VUsers.Add(vuser);
                db.SaveChanges();
                return vuser;
            }
        }
        public static Webpages_VUser CreateOrGetByGroupId(string groupCode)
        {
            using (var db = new WebPagesContext())
            {
                var vuser = db.Webpages_VUsers.FirstOrDefault(o => o.RoleID == groupCode);
                if (vuser != null)
                    return vuser;
                vuser = new Webpages_VUser();
                vuser.RoleID = groupCode;
                vuser.Type = (int)VUserType.Group;
                db.Webpages_VUsers.Add(vuser);
                db.SaveChanges();
                return vuser;
            }
        }
        
    }

    /// <summary>
    /// Virtual user type
    /// </summary>
    public enum VUserType
    {
        /// <summary>
        /// User
        /// </summary>
        User = 1,
        /// <summary>
        /// Group
        /// </summary>
        Group = 2
    }
}