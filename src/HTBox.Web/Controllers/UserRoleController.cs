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
                var root = Webpages_Roles.GetOrCreateRoot(db: db);
                list.Add(new ZTree(root));
            }
            else
            {

                foreach (var r in allRoles)
                {
                     list.Add( new ZTree(r));
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
