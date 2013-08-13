using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HTBox.Web.Models
{
    public class Menu
    {
        public List<MenuTree> Menus { get; set; }
        public int StartPageNo { get; set; }
        public int CurrentPageNo { get; set; }
        public int TotalPageNo { get; set; }
        public int NeedToShow { get; set; }

        public bool HasChildren(MenuTree menu)
        {
            using (var db = new WebPagesContext())
            {
                return db.MenuTrees.Where(o=>o.ParentId == menu.MenuId).Any();
            }
        }
    }
}