using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Web.Mvc.Html;

namespace HTBox.Web.Models
{
    public class WebMenu
    {
        public List<MenuTree> Menus { get; set; }
        public int StartPageNo { get; set; }
        public int CurrentPageNo { get; set; }
        public int TotalPageNo { get; set; }
        public int NeedToShow { get; set; }
        public int? ParentId { get; set; }
        public bool HasChildren(MenuTree menu)
        {
            using (var db = new WebPagesContext())
            {
                return db.MenuTrees.Where(o=>o.ParentId == menu.MenuId).Any();
            }
        }
    }
   
}