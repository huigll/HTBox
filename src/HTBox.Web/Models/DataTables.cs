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
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int RoleId { get; set; }
        [Required,MaxLength(128)] 
        public string RoleName { get; set; }

        public int Type { get; set; }
        [Required,MaxLength(128)] 
        public string Code { get; set; }
        [Required]
        public int Deep { get; set; }
        public int IndexOrder { get; set; }
        [MaxLength(128)] 
        public string Master { get; set; }
        [MaxLength(4000)] 
        public string Expression { get; set; }
        public string Remark { get; set; }
    }
    [Table("webpages_UsersInRoles")]
    public class Webpages_UsersInRoles
    {
        [Key, Column(Order = 0)]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }
        [Key, Column(Order = 1)]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int RoleId { get; set; }
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
        public int Order { get; set; }


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

        
        public int RoleID { get; set; }


        [ForeignKey("UserID")]
        public Webpages_UserProfile User { get; set; }

        
        public int UserID { get; set; }

        [Required]
        public int Type { get; set; }
    }
}