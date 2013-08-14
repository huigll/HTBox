using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HTBox.Web.Models;
using Newtonsoft.Json;

namespace HTBox.Web.Controllers
{
    public class UserRoleController : Controller
    {
        private WebPagesContext db = new WebPagesContext();
        //
        // GET: /UserRole/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetData(string code = null)
        {
            IQueryable<Webpages_Roles> allRoles;
            if (code == null)
                allRoles = db.WebPagesRoles.Where(o => o.Deep == 0);
            else
                allRoles = db.WebPagesRoles.Where(o => o.Code == code);
            List<ZTree> list = new List<ZTree>();
            if (allRoles.Count() == 0)
            {
                var root = Webpages_Roles.GetOrCreateRoot();
                list.Add(new ZTree(root));
            }
            else
            {

                foreach (var r in allRoles)
                {
                    list.Add(new ZTree(r));
                    foreach (var r1 in r.GetOneFloorGroups())
                    {
                        var z1 = new ZTree(r1);
                        z1.ParentId = r.RoleId;
                        list.Add(z1);
                    }
                }
            }

            return Content(JsonConvert.SerializeObject(list), "application/json");
        }

        public ActionResult EditUser(int userid)
        {
            var user = db.UserProfiles.Find(userid);
            if (user == null)
                return HttpNotFound();
            return PartialView(user);
        }
        public ActionResult CreateUser()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CreateUser(Webpages_UserProfile user)
        {
            db.UserProfiles.Add(user);
            db.SaveChanges();
            return Content(Boolean.TrueString);
        }
        [HttpPost]
        public ActionResult EditUser(Webpages_UserProfile user)
        {
            if (ModelState.IsValid)
            {

                db.Entry(user).State = System.Data.EntityState.Modified;
                db.SaveChanges();
            }
            return Content(Boolean.TrueString);
        }
        public ActionResult CreateRole()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CreateRole(Webpages_Roles role)
        {
            db.WebPagesRoles.Add(role);
            db.SaveChanges();
            return Content(Boolean.TrueString);
        }
        public ActionResult EditRole(int roleId)
        {
            var user = db.WebPagesRoles.Find(roleId);
            if (user == null)
                return HttpNotFound();
            return PartialView(user);
        }

        [HttpPost]
        public ActionResult EditRole(Webpages_Roles role)
        {
            if (ModelState.IsValid)
            {
                db.Entry(role).State = System.Data.EntityState.Modified;
                db.SaveChanges();
            }
            return Content(Boolean.TrueString);
        }
    }
}
