using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HTBox.Web.Models;
using HTBox.Web.App_Start;
namespace HTBox.Web.Controllers
{
    public class MenuController : Controller
    {
        private WebPagesContext db = new WebPagesContext();
        //
        // GET: /Menu/
        public ActionResult Index(int p = 1,int? parentID=null, int pageSize = 10,
            string orderby = "MenuId", bool desc = false)
        {
            WebMenu m = new WebMenu();
            m.ParentId = parentID;
            m.CurrentPageNo = p;
            m.StartPageNo = 1;
            m.NeedToShow = 10;
            if (p > 0) p--;
            m.Menus = db.MenuTrees
                .Where(o=> parentID.HasValue ?o.ParentId == parentID.Value:o.ParentId == null)
                .OrderBy(orderby, desc).Skip(pageSize * p).Take(pageSize).ToList();
            m.TotalPageNo = CountTotalPage(db.MenuTrees.Where(o => parentID.HasValue ? o.ParentId == parentID.Value : o.ParentId == null).Count(), pageSize);
            return View(m);
        }
        private static int CountTotalPage(int rowCount, int pageSize)
        {
            if (rowCount % pageSize == 0) { return rowCount / pageSize; } else { return rowCount / pageSize + 1; }
        }
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(MenuTree menu)
        {

            db.MenuTrees.Add(menu);
            db.SaveChanges();
            string parentid = Request.QueryString["parentid"];
            if (!string.IsNullOrEmpty(parentid))
                return RedirectToAction("index", new {@parentId=parentid });
            return RedirectToAction("index");
            

        }
        public ActionResult Edit(int id)
        {

            var menu = db.MenuTrees.Find(id);
            if (menu == null)
                return HttpNotFound();
            return View(menu);

        }
        [HttpPost]
        public ActionResult Edit(MenuTree menu)
        {
            if (ModelState.IsValid)
            {

                db.Entry(menu).State = System.Data.EntityState.Modified;
                db.SaveChanges();
                string parentid = Request.QueryString["parentid"];
                if(!string.IsNullOrEmpty(parentid))
                    return RedirectToAction("index", new { @parentId = parentid });
                return RedirectToAction("index");

            }
            MenuNavigation.ClearMenuTreeCache();
            return View(menu);
        }

        public ActionResult Delete(int id)
        {

            var menu = db.MenuTrees.Find(id);
            db.Entry(menu).State = System.Data.EntityState.Deleted;
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }


        public ActionResult Search(string name="",int?parentID=null, int p = 1, int pageSize = 10,string orderby="MenuId",bool desc=false)
        {

            WebMenu m = new WebMenu();
            m.ParentId = parentID;
            m.CurrentPageNo = p;
            m.StartPageNo = 1;
            m.NeedToShow = 10;
            if (p > 0) p--;
            var query = db.MenuTrees.Where(o=> parentID.HasValue ?o.ParentId == parentID.Value:o.ParentId == null);
            if (!string.IsNullOrEmpty(name))
            {
                
                m.TotalPageNo = CountTotalPage(query.Where
                    (o => o.MenuName.IndexOf(name) != -1).Count(), pageSize);
                m.Menus = query.Where
                    (o => o.MenuName.IndexOf(name) != -1)
                    .OrderBy(orderby,desc).Skip(pageSize * p).Take(pageSize).ToList();
            }
            else
            {
                m.TotalPageNo = CountTotalPage(query.Count(), pageSize);
                m.Menus = query.OrderBy(orderby, desc).
                    Skip(pageSize * p).Take(pageSize).ToList();
            }

            return View(m);
        }

    }
}
