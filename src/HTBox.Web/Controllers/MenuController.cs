using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HTBox.Web.Models;

namespace HTBox.Web.Controllers
{
    public class MenuController : Controller
    {
        private WebPagesContext db = new WebPagesContext();
        //
        // GET: /Menu/
        public ActionResult Index(int pageIndex=0,int pageSize=10)
        {
            return View(db.MenuTrees.OrderBy(o=>o.MenuId).Skip(pageSize * pageIndex).Take(pageSize).ToList());
        }

        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(MenuTree menu)
        {
            using (var db = new WebPagesContext())
            {
                db.MenuTrees.Add(menu);
                db.SaveChanges();
                return RedirectToAction("index");
            }
        }
        public ActionResult Delete(int id)
        {
            using (var db = new WebPagesContext())
            {
                return View(db.MenuTrees.Find(id));
            }
        }

        [HttpPost]
        public ActionResult Delete(int id, MenuTree menu)
        {
            using (var db = new WebPagesContext())
            {
                db.Entry(menu).State = System.Data.EntityState.Deleted;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
