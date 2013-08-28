using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;
using Newtonsoft.Json;
using System.Net.Http;

namespace HTBox.Web.Models
{
    public class ZTree
    {
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("pId")]
        public String ParentId { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("icon")]
        public string ICON { get; set; }
        [JsonProperty("open")]
        public bool Open { get; set; }
        [JsonProperty("checked")]
        public bool Checked { get; set; }
        [JsonProperty("nodeType")]
        public string NodeType { get;private set; }


        public ZTree(Webpages_Roles role)
        {
            ID = role.Code;
            Name = role.RoleName;
            NodeType = "Role";
            ICON = "Content/zTreeStyle/img/role.gif";
        }

        public ZTree(Webpages_UserProfile user)
        {
            ID = user.UserId.ToString();
            Name = user.UserName;
            NodeType = "User";
            ICON = "Content/zTreeStyle/img/user.gif";
        }
    }

}
