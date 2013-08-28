//#define MYDEBUG
using System;
using System.Data;
using System.Collections;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Web.Security;
using HTBox.Web.Utility;

namespace HTBox.Web.Models
{
    public abstract class MenuTreeCtrl
    {
        /// <summary>
        /// 树的主根代码
        /// </summary>
        public const int TreeRootID = -1;
        /// <summary>
        /// 管理员所在的角色代码
        /// </summary>
        public static readonly string AdminUserRoleCode = ConfigurationManager.AppSettings["AdminRoles"];
        /// <summary>
        /// 系统管理顶级节点代码
        /// </summary>
        public static readonly string SysManageTopTreeCode = ConfigurationManager.AppSettings["SysManageTopTreeCode"];
        
        //private const string m_NulParentUrl = "~/DesktopModules/ReceiveAndSendDoc/MainBox.aspx";
        /// <summary>
        /// 页面弹出时的target
        /// </summary>
        private const string _Target = "main";
        /// <summary>
        /// 主界面中中间框架的名称
        /// </summary>
        public static string MainTarget { get { return _Target; } }

        private const string emptyPageUrl = "WORKFLOWUI/EMPTYPAGE.ASPX";

        /// <summary>
        /// 取得功能库中节点RootID下一层的数据集
        /// </summary>
        /// <param name="RootID">节点ID</param>
        /// <returns>节点RootID的下一层节点的数据集</returns>
        public static MenuTree[] GetOneFloorByRootID(int RootID,bool isPublic=false)
        {
            if (RootID < 0 && RootID != TreeRootID)//当节点值<0并且又不是主根节点时
                return null;
            using (var db = new WebPagesContext())
            {
                if (RootID != TreeRootID)
                    return db.MenuTrees.Where(o => o.ParentId == RootID && o.IsPublic == isPublic).OrderBy(o => o.OrderIndex).ToArray();
                
                var query=db.MenuTrees.Where(o => o.ParentId == null && o.IsPublic == isPublic).OrderBy(o => o.OrderIndex);
                var sql = query.ToString();
                return query.ToArray();
            }
        }
        /// <summary>
        /// 取得用户userid的功能库中拥有的节点RootID下一层的数据集
        /// </summary>
        /// <param name="RootID">节点ID</param>
        /// <param name="userid">用户ID</param>
        /// <returns>节点RootID的下一层节点的数据集</returns>
        public static MenuTree[] GetOneFloorByRootID(int RootID, int userid)
        {
            if (RootID < 0 && RootID != TreeRootID)//当节点值<0并且又不是主根节点时
                return null;
            MenuTree[] trees = GetOneFloorByRootID(RootID);
            List<MenuTree> list = new List<MenuTree>(trees);
            for (int i = 0; i < list.Count; )
            {
                if (!IsUserHaveNodeInThisFuntion(list[i].MenuId, userid))
                {
                    list.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
            return list.ToArray();

        }
        public static void GetPublicMenuTree(ref TreeView treeview, string AppVirtualPath, int rootID, bool chechBox,
            bool isAddUrl, bool isShowAllAndSelectOwner)
        {
            MenuTree[] trees = GetOneFloorByRootID(rootID, true);

            Func<TreeNode,int, bool> f = null;

            f = (node, rootId) =>
            {
                var subTrees = GetOneFloorByRootID(rootId, true);
                foreach (MenuTree tree in subTrees)
                {
                    bool tmpflag = isShowAllAndSelectOwner;
                    if (!isShowAllAndSelectOwner && tree.IsHidden)
                        continue;

                    //创建存有必要信息的TReeNode
                    TreeNode newNode = newTreeNode(tree, AppVirtualPath, chechBox, isAddUrl, false);
                    if (isShowAllAndSelectOwner)
                    {
                        newNode.Checked = true;
                    }
                    node.ChildNodes.Add(newNode);
                    f(node, tree.MenuId);
                }
                return true;
            };
           
            foreach (MenuTree tree in trees)
            {
                bool tmpflag = isShowAllAndSelectOwner;
                if (!isShowAllAndSelectOwner && tree.IsHidden)
                    continue;
                //创建存有必要信息的TReeNode
                TreeNode node = newTreeNode(tree, AppVirtualPath, chechBox, isAddUrl, false);
                if (isShowAllAndSelectOwner )
                {
                    node.Checked = true;
                }
                treeview.Nodes.Add(node);
                f(node, tree.MenuId);
            }
        }
      
        public static void GetUserMenuTree(ref TreeView treeview, string appVirtualPath,
            Webpages_VUser vuser, int rootID, bool chechBox, bool isAddUrl,
            bool isShowAllAndSelectOwner)
        {
            if (treeview == null)
                return;
            if (vuser == null)
            {
                GetPublicMenuTree(ref treeview, appVirtualPath, rootID, chechBox, isAddUrl, isShowAllAndSelectOwner);
                return;
            }
            MenuTree[] trees = GetOneFloorByRootID(rootID);
            using (var db = new WebPagesContext())
            {
                if (trees != null && trees.Length > 0)
                {
                    treeview.Target = _Target;
                    int i = 0;
                    bool IsAdmin = false;//管理员
                    Webpages_Roles group = db.WebPagesRoles.FirstOrDefault(o=>o.Code == AdminUserRoleCode);
                    if (group != null)
                    {
                        if (vuser.Type == (int)VUserType.User)
                        {
                            Webpages_UserProfile user = db.UserProfiles.FirstOrDefault(o=>o.UserId == vuser.UserID);
                         
                            //if (user.IsUserInGroup(group))
                            if(Roles.IsUserInRole(user.UserName,group.RoleName))
                            {
                                IsAdmin = true;
                            }
                        }
                        else
                        {
                            Webpages_Roles vGroup = db.WebPagesRoles.FirstOrDefault(o => o.Code == vuser.RoleID);
                            if (IsGroupInGroup(vuser.Role.Code, group.Code))
                            {
                                IsAdmin = true;
                            }
                        }
                    }
                    if (IsAdmin)
                    {
                        foreach (MenuTree tree in trees)
                        {
                            if (!isShowAllAndSelectOwner && tree.IsHidden )
                                continue;

                            //创建存有必要信息的TReeNode
                            TreeNode node = newTreeNode(tree, appVirtualPath, chechBox, isAddUrl, false);

                            if (isShowAllAndSelectOwner)
                            {
                                //if (!IsParentChecked(node))
                                node.Checked = true;
                            }

                            treeview.Nodes.Add(node);
                            bool IsThisNodeContainShisUser = true;//管理员拥有所有 
                            AddChildrenToNode(node, null,
                                tree.MenuId,
                                appVirtualPath,
                                ref IsThisNodeContainShisUser, chechBox, isAddUrl,
                                //isShowAllAndSelectOwner && !node.Checked);
                                  isShowAllAndSelectOwner, node.Checked);
                        }
                    }
                    else
                    {


                        bool IsThisNodeContainShisUser = false;
                        int[] parentRls = GetThisNodeAllViewRoles(rootID);
                        if (parentRls != null && parentRls.Length > 0)
                        {//判断父节点是否包含

                            if (CheckThoseVuserContainThisGroup(parentRls, vuser))
                            {
                                IsThisNodeContainShisUser = true;
                            }
                        }

                        foreach (MenuTree tree in trees)
                        {
                            bool tmpflag = IsThisNodeContainShisUser;
                            if (!isShowAllAndSelectOwner && tree.IsHidden)
                                continue;

                            //string Roles = row["OWNERGROUPCODE"].ToString();
                            int[] Roles = GetThisNodeAllViewRoles(tree.MenuId);
                            //此节点的角色列表中是不是包含此用户或者角色的标志
                            if (!tmpflag && Roles != null && Roles.Length > 0)
                            {
                                if (CheckThoseVuserContainThisGroup(Roles, vuser))
                                {
                                    tmpflag = true;
                                }

                            }
                            //创建存有必要信息的TReeNode
                            TreeNode node = newTreeNode(tree, appVirtualPath, chechBox, isAddUrl, false);

                            if (isShowAllAndSelectOwner && tmpflag)
                            {
                                //if (!IsParentChecked(node))
                                node.Checked = true;
                            }

                            treeview.Nodes.Add(node);
                            AddChildrenToNode(node, vuser.VUserId,
                                tree.MenuId,
                                appVirtualPath,
                                ref tmpflag, chechBox, isAddUrl,
                                //isShowAllAndSelectOwner && !node.Checked);
                                  isShowAllAndSelectOwner, node.Checked);

                            if (!tmpflag && !isShowAllAndSelectOwner)
                            {
                                treeview.Nodes[i].ChildNodes.Clear();
                                treeview.Nodes.RemoveAt(i);
                            }
                            else
                                i++;

                        }
                    }
                }
            }
        }
       
        /// <summary>
        /// 格式化strfile成可用的虚拟路径
        /// </summary>
        /// <param name="AppVirtualPath">web程序运行的虚拟目录</param>
        /// <param name="strfile">文件名</param>
        /// <returns>可用的虚拟路径</returns>
        private static string FillPath(string AppVirtualPath, string strUrl)
        {
            if (string.IsNullOrEmpty(strUrl))
                return strUrl;
            if (strUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                strUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                strUrl.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)||
                strUrl.StartsWith("/"))
                return strUrl;
            if(AppVirtualPath !="/")
                return string.Format("{0}/{1}",AppVirtualPath,strUrl); 
            else
                return  string.Format("/{0}",strUrl);  

        }
        /// <summary>
        /// 添加功能树中RootID所有子节点到树节点parentnode上
        /// </summary>
        /// <param name="parentnode">引用的 要添加到树种的父节点的ID</param>
        /// <param name="GroupCodes">角色的机构编码列表</param>
        /// <param name="RootID">功能代码</param>
        /// <param name="AppVirtualPath">程序虚拟路径</param>
        /// <param name="IsThisNodeContainShisUser">是不是父节点中有节点的角色列表中包含次角色或者用户</param>
        /// <param name="ChechBox">树中是不是显示ChechBox</param>
        /// <param name="isAddUrl">是不是在树节点上添加Url</param>
        /// <param name="isShowAllAndSelectOwner">是不是选中节点角色列表中包含此角色的节点</param>

        public static void AddChildrenToNode(TreeNode parentnode,
            int? vuserId, int RootID, string AppVirtualPath,
            ref bool IsThisNodeContainShisUser, bool ChechBox,
            bool isAddUrl, bool isShowAllAndSelectOwner,bool isParentChecked)
        {
            if (parentnode == null)
                throw new ArgumentNullException("parentnode");
            MenuTree[] trees = GetOneFloorByRootID(RootID);
            bool tmpflag = IsThisNodeContainShisUser;
            bool ReturnFlag = IsThisNodeContainShisUser;
            if (trees != null && trees.Length > 0)
            {
                int i = 0;
                foreach (MenuTree tree in trees)
                {
                    if (!isShowAllAndSelectOwner && tree.IsHidden)
                        continue;
                    IsThisNodeContainShisUser = tmpflag;
                    bool checkedThisNode = false;//如果拥有本节点权限，是不是选中本节点
                    TreeNode node = newTreeNode(tree, AppVirtualPath, ChechBox, isAddUrl, false);
                    int[] Roles = GetThisNodeAllViewRoles(tree.MenuId);
                    if (!IsThisNodeContainShisUser && Roles != null && Roles.Length > 0 && vuserId != null)
                    {
                        if (CheckThoseVuserContainThisGroup(Roles, vuserId.Value))//如果此层中有一个是，那么返回值就是真
                        {
                            IsThisNodeContainShisUser = true;
                            ReturnFlag = IsThisNodeContainShisUser;
                            checkedThisNode = true;
                        }
                    }
                    //if (isShowAllAndSelectOwner && checkedThisNode)
                    if (!isParentChecked && checkedThisNode)
                    {
                        //if (!IsParentChecked(node))
                        node.Checked = true;
                        
                    }
                    parentnode.ChildNodes.Add(node);
                    if (node.Checked)
                    {
                        TreeNode tmpNode = node.Parent;
                        while (tmpNode != null && tmpNode.Expanded != true)
                        {
                            tmpNode.Expanded = true;
                            tmpNode = tmpNode.Parent;
                        }
                    }
                    AddChildrenToNode(parentnode.ChildNodes[i], vuserId,
                        tree.MenuId,
                        AppVirtualPath,
                        ref IsThisNodeContainShisUser, ChechBox, isAddUrl,
                        //isShowAllAndSelectOwner && !node.Checked);
                        isShowAllAndSelectOwner, node.Checked);
                    if (IsThisNodeContainShisUser)//保存下级传上来的标记
                        ReturnFlag = true;
                    if (!IsThisNodeContainShisUser && !isShowAllAndSelectOwner)
                    {
                        parentnode.ChildNodes[i].ChildNodes.Clear();
                        parentnode.ChildNodes.RemoveAt(i);
                    }
                    else
                        i++;
                }
            }
            IsThisNodeContainShisUser = ReturnFlag;
        }
        /// <summary>
        /// 创建新的TreeNode，并按选项赋值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="AppVirtualPath"></param>
        /// <param name="ChechBox"></param>
        /// <param name="isAddUrl"></param>
        /// <param name="Expand"></param>
        /// <returns></returns>
        public static TreeNode newTreeNode(MenuTree tree, string AppVirtualPath, bool ChechBox,
            bool isAddUrl, bool Expand)
        {
            TreeNode node = new TreeNode();
            node.ShowCheckBox = ChechBox;
            node.Expanded = Expand;
            node.Target = _Target;
            

            node.Text = tree.MenuName;

            node.Value = tree.MenuId.ToString();
            if (isAddUrl)
            {

                node.NavigateUrl = FillPath(AppVirtualPath,tree.PageUrl);
                if (node.NavigateUrl == null || node.NavigateUrl.Length == 0)
                    node.SelectAction = TreeNodeSelectAction.Expand;
                //如果没有指定Url那么就把罗列子节点的页面加上
                // if (node.NavigateUrl == null || node.NavigateUrl.Length == 0)
                //     node.NavigateUrl = m_NulParentUrl + "?SYS__NodeID=" + node.Value;
            }
            else
            {
                node.SelectAction = TreeNodeSelectAction.Expand;
            }
            
            if(!string.IsNullOrEmpty(tree.OpenTarget))
                node.Target = tree.OpenTarget;
            return node;
        }
      
        public static bool IsGroupInGroup(string groupCodeX, string groupCodeY)
        {
            if (groupCodeX == groupCodeY)
                return true;
            else
                return (groupCodeX.StartsWith(groupCodeY + "-"));
        }
        public static bool CheckThoseVuserContainThisGroup(int[] vuserIds, int vuserId)
        {
            return CheckThoseVuserContainThisGroup(vuserIds, Webpages_VUser.Find(vuserId));
        }
        public static bool CheckThoseVuserContainThisGroup(int[] vuserIds, Webpages_VUser vuser)
        {
            if (vuserIds == null || vuserIds.Length == 0)
                return false;
            using (var db = new WebPagesContext())
            {
                
                if (vuser.Type == (int)VUserType.Group)
                {
                    foreach (int vuserid in vuserIds)
                    {
                        Webpages_VUser vu = db.Webpages_VUsers.FirstOrDefault(o => o.VUserId == vuserid);
                        
                        if (vu.Type == (int)VUserType.Group)
                        {
                            if (IsGroupInGroup(vuser.Role.Code, vu.Role.Code))
                            {
                                return true;
                            }
                        }

                    }
                }
                else
                {
                    foreach (int vuserid in vuserIds)
                    {
                        Webpages_VUser vu = db.Webpages_VUsers.FirstOrDefault(o=>o.VUserId ==vuserid);
                        
                        if (vu.Type == (int)VUserType.Group)
                        {
                            var user = vuser.User;
                            //if (user.IsUserInGroup(new ADGroup(vu.GroupId)))
                            if(Roles.IsUserInRole(user.UserName,vu.Role.RoleName))
                            {
                                return true;
                            }
                        }
                        else if (vu.UserID == vuser.UserID)
                            return true;
                    }

                }
                return false;
            }
        }
        /// <summary>
        /// 检查节点RootID是不是有子节点 
        /// </summary>
        /// <param name="RootID"></param>
        /// <returns></returns>
        public static bool IsThisNodeHaveChildren(int RootID)
        {
            //MenuTree.TblQuery stu = new MenuTree.TblQuery(ConfigInfo.DataServerType);
            //stu.PARENTID = RootID;
            //MenuTree menu = new MenuTree(ConfigInfo.DataServerType);
            //object obj = menu.ControlHelper.GetOneFieldBy("count(*)", stu, ConfigInfo.ConnectionString);
            //return obj != null && obj != DBNull.Value && Convert.ToInt32(obj) > 0;

            using (var db = new WebPagesContext())
            {
                return db.MenuTrees.Any(o => o.ParentId == RootID);
            }
        }
        /// <summary>
        /// 取得节点menuid的角色列表
        /// </summary>
        /// <param name="menuid">节点ID</param>
        /// <returns>节点menuid的角色列表</returns>
        public static int[] GetThisNodeVuserIdList(int menuid)
        {
           
            using (var db = new WebPagesContext())
            {
                
                return (from n in db.MenuTrees 
                         from r in db.MenuTreeRights
                         where n.MenuId == r.MenuId && n.MenuId == menuid
                         select r.VuserID).ToArray();
            }

        }
        /// <summary>
        /// 取得节点menuid的某一列
        /// </summary>
        /// <param name="menuid">节点ID</param>
        /// <param name="columnName"></param>
        /// <returns>节点menuid的角色列表</returns>
        public static string GetThisNodePageUrl(int menuid)
        {
            using (var db = new WebPagesContext())
            {

                return db.MenuTrees.Where(o => o.MenuId == menuid).Select(o => o.PageUrl).FirstOrDefault();
            }
        }
        /// <summary>
        /// 取得节点menuid所有的父节点列表
        /// </summary>
        /// <param name="menuid">节点ID</param>
        /// <returns></returns>
        public static int[] GetParentNodeRoles(int menuid)
        {
            using (var db = new WebPagesContext())
            {
                List<int> parentList = new List<int>();
                
                int? parentNodeID = (from m in db.MenuTrees
                                where m.MenuId == menuid
                                    select m.ParentId).FirstOrDefault();
                if (parentNodeID == null) return parentList.ToArray();
                while (true)
                {
                    var parentNode = db.MenuTrees.Where(o => o.MenuId == parentNodeID).FirstOrDefault();
                    int ThisNodeID = parentNode.MenuId;

                    parentList.Add(ThisNodeID);
                    if (!parentNode.ParentId.HasValue)
                        break;
                    int ParentNodeID = parentNode.ParentId.Value;
                    if (ParentNodeID == TreeRootID)//到达了根节点
                        break;
                    else
                        parentNodeID = ParentNodeID;//继续循环
                }
                return parentList.ToArray();
            }

        }
        /// <summary>
        /// 根据节点号取得功能库中有此节点所有孩子所有节点的浏览角色
        /// 不包含此节点
        /// </summary>
        /// <param name="menuid">此节点的节点号</param>
        /// <returns>按 key=节点号 value=浏览角色列表 顺序存放的哈希表</returns>
        public static Dictionary<int, int[]> GetChildrenNodeRoles(int menuid)
        {
            Dictionary<int, int[]> hstbl = new Dictionary<int, int[]>();
            MenuTree[] trees = MenuTreeCtrl.GetOneFloorByRootID(menuid);
            if (trees != null && trees.Length > 0)
            {
                foreach (MenuTree tree in trees)
                {
                    int childNodeID = tree.MenuId;
                    int[] vuserids = MenuTreeCtrl.GetThisNodeVuserIdList(childNodeID);
                    hstbl.Add(childNodeID, vuserids);
                    AddChildrenToTbl(ref hstbl, childNodeID);
                }
            }
            return hstbl;
        }
        /// <summary>
        /// 取得节点为menuid的所有孩子的浏览角色列表，
        /// 并按key=节点号 value=浏览角色列表 顺序存放的哈希表
        /// </summary>
        /// <param name="hstbl">存放 key=节点号 value=浏览角色列表的 哈希表</param>
        /// <param name="menuid">节点ID</param>
        private static void AddChildrenToTbl(ref Dictionary<int, int[]> hstbl, int menuid)
        {
            MenuTree[] trees = MenuTreeCtrl.GetOneFloorByRootID(menuid);
            if (trees != null && trees.Length > 0)
            {
                foreach (MenuTree tree in trees)
                {
                    int childNodeID = tree.MenuId;
                    int[] vuserids = MenuTreeCtrl.GetThisNodeVuserIdList(childNodeID);
                    hstbl.Add(childNodeID, vuserids);
                    AddChildrenToTbl(ref hstbl, childNodeID);
                }
            }

        }
       
        /// <summary>
        /// 根据节点号取得本节点的所有浏览权限，包括本节点和父节点继承来的
        /// </summary>
        /// <param name="menuid"></param>
        /// <returns></returns>
        public static int[] GetThisNodeAllViewRoles(int menuid)
        {
            if (menuid < 0 && menuid != TreeRootID)
                return null;

            int[] parentNodes = GetParentNodeRoles(menuid);
            int[] ThisNodeRoles = GetThisNodeVuserIdList(menuid);
            int[] rtn = ThisNodeRoles;
            if (parentNodes != null)
            {
                foreach (int nodeid in parentNodes)
                {
                    int[] rights = GetThisNodeVuserIdList(nodeid);
                    if (rights != null)
                    {
                        rtn = FunctionAllocate.AddNewRoleToRole(rtn, rights);
                    }
                }
            }
            return rtn;
        }

        public static bool IsUserHaveNodeInThisFuntion(int RootID, int userid)
        {
            return IsUserHaveNodeInThisFuntion(RootID, Webpages_VUser.CreateOrGetByUserId(userid));
        }
        public static bool IsUserHaveNodeInThisFuntion(int RootID, Webpages_VUser vuser)
        {

            int[] ThisNodeGroupCodes = GetThisNodeAllViewRoles(RootID);

            if (CheckThoseVuserContainThisGroup(ThisNodeGroupCodes, vuser))
                return true;


            MenuTree[] trees = GetOneFloorByRootID(RootID);
            if (trees != null && trees.Length > 0)
            {
                foreach (MenuTree tree in trees)
                {
                    int[] Roles = GetThisNodeVuserIdList(tree.MenuId);
                    if (CheckThoseVuserContainThisGroup(Roles, vuser))
                        return true;

                    //不能不管真假就return！！！假的时候还要判断其他的兄弟节点呢！
                    if (IsUserHaveNodeInThisFuntion(tree.MenuId,
                        vuser))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 取得用户不能浏览的页面列表,页面Url不包含参数,包含相对路径
        /// </summary>
        /// <param name="userGroupCode"></param>
        /// <returns></returns>
        public static List<string> GetUserCannotViewPages(string userId)
        {

            List<string> outlist = new  List<string>();//包含不能浏览的页面
            List<int> inIdlist = new List<int>();//包含能浏览的节点ID
            AddUserCannotViewPages(ref outlist, ref inIdlist, MenuTreeCtrl.TreeRootID, userId);

            foreach (int nodeid in inIdlist)
            {
                RemoveUrlFromList(nodeid, ref outlist);
            }
            if (outlist.Contains(emptyPageUrl))
            {
                outlist.Remove(emptyPageUrl);
            }
            outlist.Sort();
            return outlist;

        }
        public static List<int> GetUserViewNodes(int userId)
        {
            List<int> inIdlist = new List<int>();//包含能浏览的节点ID
            TreeView tree = new TreeView();
            GetUserMenuTree(ref tree, null, Webpages_VUser.CreateOrGetByUserId(userId), 
                MenuTreeCtrl.TreeRootID, false, false, false);
            AddTreeToList(tree.Nodes, inIdlist);
            return inIdlist;

        }
        static void AddTreeToList(TreeNodeCollection nodes,List<int> inIdlist)
        {
            if(nodes==null || nodes.Count == 0)
            {
                return;
            }
            foreach(TreeNode n in nodes)
            {
                inIdlist.Add(Convert.ToInt32(n.Value));
                AddTreeToList(n.ChildNodes,inIdlist);
            }
        }
        /// <summary>
        /// 取得管理员不能浏览的页面
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAdminCannotViewPages()
        {
            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(SysManageTopTreeCode))
            {
                return list;
            }
            string[] TopTreeAry = SysManageTopTreeCode.Split(',');

            MenuTree[] trees = MenuTreeCtrl.GetOneFloorByRootID(MenuTreeCtrl.TreeRootID);
            if (trees != null && trees.Length > 0)
            {
                foreach (MenuTree tree in trees)
                {
                    if (UtilityArray.FindInArray(TopTreeAry, tree.MenuId.ToString()) != -1)
                        //如果配置文件中指定管理员可以浏览，那么就不用检查字节点
                        continue;
                    string url = tree.PageUrl;
                    if (url != null && url.Length > 0)
                    {//添加不为空的页面
                        list.Add(StringAnalyse.GetPagePurename(url));
                    }
                    AddUserCannotViewPages(ref list, tree.MenuId);

                }
            }
            if (list.Contains(emptyPageUrl))
            {
                list.Remove(emptyPageUrl);
            }
            return list;
        }
        private static void AddUserCannotViewPages(ref  List<string> list, int rootID)
        {
            MenuTree[] trees = MenuTreeCtrl.GetOneFloorByRootID(rootID);
            if (trees != null && trees.Length > 0)
            {
                foreach (MenuTree tree in trees)
                {
                    string url = tree.PageUrl;
                    if (url != null && url.Length > 0)
                    {//添加不为空的页面
                        list.Add(StringAnalyse.GetPagePurename(url));
                    }
                    //寻找此节点的字节点
                    AddUserCannotViewPages(ref list, tree.MenuId);
                }
            }
        }

        private static void RemoveUrlFromList(int rootID, ref List<string> list)
        {
            string PageUrl = GetThisNodePageUrl(rootID);
            PageUrl = StringAnalyse.GetPagePurename(PageUrl);
            list.Remove(PageUrl);
            MenuTree[] trees = MenuTreeCtrl.GetOneFloorByRootID(rootID);
            if (trees != null && trees.Length > 0)
            {
                foreach (MenuTree tree in trees)
                {
                    string url = tree.PageUrl;
                    if (url != null && url.Length > 0)
                    {//添加不为空的页面
                        list.Remove(StringAnalyse.GetPagePurename(url));
                    }
                    RemoveUrlFromList(tree.MenuId, ref list);
                }
            }
        }
        private static void AddUserCannotViewPages(ref List<string> list, ref List<int> inIdlist, int rootID,
        string userId)
        {
            MenuTree[] nodes = MenuTreeCtrl.GetOneFloorByRootID(rootID);
            using (var db = new WebPagesContext())
            {
                if (nodes != null)
                {
                    foreach (MenuTree menu in nodes)
                    {
                        bool CanViewIt = false;

                        int[] groups = GetThisNodeVuserIdList(menu.MenuId);
                        if (groups != null)
                        {

                            foreach (int id in groups)
                            {
                                Webpages_VUser tmpv = db.Webpages_VUsers.FirstOrDefault(o=>o.VUserId == id);
                                

                                if (tmpv.Type == (int)VUserType.Group)
                                {
                                    //ADUser user = new ADUser(userId);
                                    //if (user.IsUserInGroup(new ADGroup(tmpv.GroupId)))
                                    if(Roles.IsUserInRole(userId,tmpv.Role.RoleName))
                                    {
                                        CanViewIt = true;
                                        break;
                                    }
                                }
                                else
                                {

                                    if (string.Compare(tmpv.UserID.ToString(), userId, true) == 0)
                                    {
                                        CanViewIt = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (CanViewIt)
                        {//如果此节点可以被次用户浏览。
                            inIdlist.Add(menu.MenuId);
                            continue;//停止找自节点，继续找兄弟节点
                        }
                        else
                        {//此节点此用户不能浏览
                            string url = menu.PageUrl;

                            if (url != null && url.Length > 0)
                            {//添加不为空的页面
                                url = StringAnalyse.GetPagePurename(url);
                                if (!list.Contains(url))
                                    list.Add(url);
                            }
                            //寻找此节点的字节点
                            AddUserCannotViewPages(ref list, ref inIdlist, menu.MenuId, userId);
                        }
                    }

                }
            }
        }
     
    }
}
