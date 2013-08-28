using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HTBox.Web.Models;
using Newtonsoft.Json;
using System.Collections;
using System.Transactions;

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

        public ActionResult GetData(string id = null)//此参数名需要与 tree setting 属性 相同
        {
            IEnumerable<Webpages_Roles> allRoles;
            if (id == null)
                allRoles = db.WebPagesRoles.Where(o => o.Deep == 0);
            else
                allRoles = db.WebPagesRoles.Find(id).GetOneFloorGroups(db);
            List<ZTree> list = new List<ZTree>();
            if (allRoles.Count() == 0)
            {
                var root = Webpages_Roles.GetOrCreateRoot();
                list.Add(new ZTree(root));
            }
            else
            {
                Func<IEnumerable<Webpages_Roles>, string, bool> f = null;

                f = (n, parent) =>
                    {
                        foreach (var r in n)
                        {
                            var z = new ZTree(r);
                            z.ParentId = parent;
                            list.Add(z);
                            foreach (var user in r.GetUsers(false))
                            {
                                var u = new ZTree(user);
                                u.ParentId = r.Code;
                                list.Add(u);
                            }
                            f(r.GetOneFloorGroups(), r.Code);

                        }
                        return true;
                    };
                f(allRoles, null);
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
        public ActionResult CreateUser(string code)
        {
            ViewBag.RoleCode = code;
            Webpages_UserProfile user=new Webpages_UserProfile()
            {
                IndexOrder = 1
            };
            return View(user);
        }
        [HttpPost]
        public ActionResult CreateUser(Webpages_UserProfile user, string roleCode)
        {
            using (TransactionScope ts = new TransactionScope())
            {
                if (db.UserProfiles.Where(o => o.UserName == user.UserName).Any())
                {
                    return Content("User Exist");
                }
                db.UserProfiles.Add(user);
                db.SaveChanges();
                Webpages_UsersInRoles userRole = new Webpages_UsersInRoles()
                {
                    RoleCode = roleCode,
                    UserId = user.UserId
                };
                db.WebPagesUsersInRoles.Add(userRole);
                db.SaveChanges();
                ts.Complete();
                return Content(Boolean.TrueString);
            }
        }
        [HttpPost]
        public ActionResult EditUser(Webpages_UserProfile user)
        {
            if (ModelState.IsValid)
            {
                if (db.UserProfiles.Where(o => o.UserName == user.UserName && o.UserId != user.UserId).Any())
                {
                    return Content("User Exist");
                }
                db.Entry(user).State = System.Data.EntityState.Modified;
                db.SaveChanges();
            }
            return Content(Boolean.TrueString);
        }
        public ActionResult CreateRole(string parentNodeCode = null)
        {
            if (!string.IsNullOrEmpty(parentNodeCode))
            {
                var parent = db.WebPagesRoles.Find(parentNodeCode);
                if (parent == null)
                    return HttpNotFound();
                ViewBag.ParentCode = parent.Code;
                ViewBag.ParentName = parent.RoleName;
            }
            Webpages_Roles role = new Webpages_Roles()
            {
                IndexOrder = 1
            };
            return View(role);
        }
        [HttpPost]
        public ActionResult CreateRole(Webpages_Roles role, string parentNodeCode = null)
        {
            if (!string.IsNullOrEmpty(parentNodeCode))
            {
                var parent = db.WebPagesRoles.Find(parentNodeCode);
                if (parent == null)
                {
                    return HttpNotFound();
                }
                //生成新的编码
                string codeHead = parent.Code + "-";
                var curLevelCodes = (from r in db.WebPagesRoles
                                     where r.Code.IndexOf(codeHead) == 0
                                     orderby r.Code
                                     select r.Code).ToList();

                if (curLevelCodes.Count == 0)
                {
                    role.Code = codeHead + "1";
                }
                else
                {
                    //只申请这么多个(groups.Length)标志位足够了,
                    //因为,如果全占了,就返回groups.Length+1,
                    //如果没有全占,那么中间肯定有空位
                    System.Collections.BitArray ba = new BitArray(curLevelCodes.Count);
                    //找空号
                    int ValidID = -1;
                    foreach (var c in curLevelCodes)
                    {
                        string[] ary = c.Split('-');
                        int tmp = Convert.ToInt32(ary[ary.Length - 1]);
                        if (tmp > curLevelCodes.Count)//超出的不予理会
                            continue;
                        ba[tmp - 1] = true;//打标
                    }
                    for (int i = 0; i < ba.Length; i++)
                    {//从中查找空位
                        if (!ba[i])
                        {
                            ValidID = i + 1;
                            break;
                        }

                    }
                    if (ValidID == -1)//位全占
                        ValidID = curLevelCodes.Count + 1;
                    role.Code = codeHead + ValidID;
                }
            }
            else
            {
                role.Code = role.Type.ToString();
            }
            var tmpAry = role.Code.Split('-');
            role.Type = Convert.ToInt32(tmpAry[0]);
            role.Deep = tmpAry.Length - 1;
            db.WebPagesRoles.Add(role);
            db.SaveChanges();
            return Content(Boolean.TrueString);
        }
        public ActionResult EditRole(string roleCode)
        {
            var role = db.WebPagesRoles.Find(roleCode);
            if (role == null)
                return HttpNotFound();
            return PartialView(role);
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

        public ActionResult DeleteUser(int userid)
        {
            using (TransactionScope ts = new TransactionScope())
            {
                var menu = db.UserProfiles.Find(userid);
                db.Entry(menu).State = System.Data.EntityState.Deleted;
                foreach (var vuser in db.Webpages_VUsers.Where(o => o.UserID == userid))
                {
                    foreach (var tree in db.MenuTreeRights.Where(o => o.VuserID == vuser.VUserId))
                    {
                        db.Entry(tree).State = System.Data.EntityState.Deleted;
                    }
                    db.Entry(vuser).State = System.Data.EntityState.Deleted;

                }
                foreach (var vuser in db.WebPagesUsersInRoles.Where(o => o.UserId == userid))
                {
                    db.Entry(vuser).State = System.Data.EntityState.Deleted;
                }
                db.SaveChanges();
                ts.Complete();
                return Content(Boolean.TrueString);
            }

        }

        public ActionResult DeleteRole(string roleCode)
        {
            using (TransactionScope ts = new TransactionScope())
            {
                var menu = db.WebPagesRoles.Find(roleCode);
                db.Entry(menu).State = System.Data.EntityState.Deleted;
                foreach (var vuser in db.Webpages_VUsers.Where(o => o.RoleID == roleCode))
                {
                    foreach (var tree in db.MenuTreeRights.Where(o => o.VuserID == vuser.VUserId))
                    {
                        db.Entry(tree).State = System.Data.EntityState.Deleted;
                    }
                    db.Entry(vuser).State = System.Data.EntityState.Deleted;

                }
                foreach (var vuser in db.WebPagesUsersInRoles.Where(o => o.RoleCode == roleCode))
                {
                    db.Entry(vuser).State = System.Data.EntityState.Deleted;
                }
                db.SaveChanges();
                ts.Complete();
                return Content(Boolean.TrueString);
            }

        }
    }
}
