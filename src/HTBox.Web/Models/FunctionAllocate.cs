using System;
using System.Data;
using System.Collections;
using System.Text;
using System.Data.Common;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;


namespace HTBox.Web.Models
{

    /// <summary>
    /// 系统功能管理：
    /// 采用把角色代码存储到功能节点的字段中方式；
    /// 把角色Role分配给功能Fun：
    /// 算法：
    ///	添加：
    ///	 检查功能Fun的角色列表中是否已经有此角色Role，或者有角色包含此角色；
    ///	 如果为真，则什么也不做，返回。
    ///	 如果为假：
    ///			如果角色列表中有几个是此角色的子集RoleChild[n]，
    ///				那么把这些角色替RoleChild[n]换成Role。
    ///			如果没有角色是此角色的子集，
    ///				那么把此角色加入到列表。
    ///			删除此功能节点的所有孩子角色列表中等于，或者包含于此角色的角色
    ///	删除：
    ///		检查功能Fun的角色列表中是否已经有角色Role：
    ///		如果有，则删除它，并返回。
    ///		如果有某角色RoleBig包含此角色Role：
    ///			则：找出此RoleBig的所有非此角色Role的角色列表OtherRole[n]；
    ///				把这些角色写入此节点，删除RoleBig
    ///		如果都没有，把此功能的父节点和所有子节点当作此节点继续检查
    ///显示树时：对于每一个由根节点到叶子的路径：
    ///		如果此路径上的某节点的角色列表中包含此用户，那么就显示此节点到Root的路径
    ///		和此节点的所有孩子
    ///直系树的含义:包含此节点一直到根的所有节点和本节点的所有子节点
    ///        Root
    ///	     /  |  \	
    ///	    /   |   \
    ///     C1   C2  C3
    ///    / |   |    \
    ///   /  |   |     \
    ///  C4  C5  C6     C7
    ///  那么C1的直系树的含义是树：
    ///         Root
    ///	     /  	
    ///	    /   
    ///     C1   
    ///    / |   
    ///   /  |   
    ///  C4  C5  	
    ///角色代码： 机构用户中的角色的机构代码，如：1-1 
    ///角色列表 或者 角色集合 的含义 ：
    ///	机构用户表中 角色的集合；例如 ： 1-1;2-4;
    ///是以;号分隔的角色代码字符串
    /// </summary>
    public abstract class FunctionAllocate
    {
        /// <summary>
        /// 枚举两个机构代码的关系
        /// </summary>
        public enum GpCodeRelation
        {
            /// <summary>
            /// 两个集合相离
            /// </summary>
            Separate,
            /// <summary>
            /// 两集合相等
            /// </summary>
            Equality,
            /// <summary>
            /// 集合A包含集合B
            /// </summary>
            Contain,
            /// <summary>
            /// 集合A被包含集合B
            /// </summary>
            Bycontain,
            /// <summary>
            /// 错误
            /// </summary>
            Error


        };

        /// <summary>
        /// 添加角色vuserId到功能节点FunNodeID
        /// </summary>
        /// <param name="vuserId">角色的机构用户代码</param>
        /// <param name="FunNodeID">功能的节点号</param>
        //逻辑：
        //	首先检查父节点和本节点是否已经存在此角色，或者存在某角色包含此角色，
        //		如果包含，那么就直接退出
        //		如果没有：那么添加角色到此节点（把两个角色集 合并）
        //				并把此节点的所有孩子的角色列表中等于或者包含于此角色的角色清空	
        public static void AllocateOneRoleToFun(int vuserId, int FunNodeID)
        {


            //此表中保存着要进行更新的节点的节点号和改变后的vuserId代码。
            List<MenuNodeInfo> hstbl = new List<MenuNodeInfo>();
            //取得所有父节点
            int[] parentNode = MenuTreeCtrl.GetParentNodeRoles(FunNodeID);
            List<int> parentList = new List<int>(parentNode);
            
            parentList.Add(FunNodeID);//把此节点也当父节来来一块处理
            //检查父节点和本节点是否已经包含本角色
            foreach (int NodeID in parentList)//从父节点或者本节点获取的权限
            {

                int[] NodeGroupCode =  MenuTreeCtrl.GetThisNodeVuserIdList(NodeID);
                if (NodeGroupCode == null || NodeGroupCode.Length == 0)
                    continue;
                foreach (int code in NodeGroupCode)
                {

                    GpCodeRelation AbRel = GetTwoRelaction(code, vuserId);
                    if (AbRel == GpCodeRelation.Equality ||//如果此节点中的角色列表已经有此角色
                        AbRel == GpCodeRelation.Contain)//如果此节点中的某角色包含此角色
                    {
                        return;//不处理
                    }
                }

            }
            //如果本节点和父节点中没有此角色，也没有包含此角色的角色
            //合并要添加的角色
            
            int[] vuserIdList = MenuTreeCtrl.GetThisNodeVuserIdList(FunNodeID);
            int[] CombinGroup = AddNewRoleToRole(vuserIdList, new int[] { vuserId });
            //保存到操作信息列表，一并进行保存操作
            hstbl.Add(new MenuNodeInfo()
            {
                Nodeid = FunNodeID,
                AddedVUserids =new List<int>( CombinGroup)
            }
               );//保存到本节点

            ClearChildrenRole(ref hstbl, FunNodeID, vuserId);//清空子节点中真包含于此角色的角色
            //进行保存操作
            SaveHashTableInfo(hstbl);
        }
        /// <summary>
        /// 分配权限Roles到除了ExceptNodeID直系以外的以parentNodeID为直系树的子节点，
        /// 不包括parentNodeID
        /// </summary>
        /// <param name="hstbl"></param>
        /// <param name="parentNodeID"></param>
        /// <param name="ExceptNodeID"></param>
        /// <param name="Roles"></param>
        private static void AllocateToChildNodeExceptThisNode(ref List<MenuNodeInfo> hstbl, int parentNodeID,
            int ExceptNodeID, int[] Roles)
        {
            if (parentNodeID != ExceptNodeID)//如果到了ExceptNodeID节点，就返回
            {
                bool IsOver = false;//本层节点中是否有ExceptNodeID节点的标志
                MenuTree[] trees = MenuTreeCtrl.GetOneFloorByRootID(parentNodeID);
                if (trees == null)
                    return;
                //查找本层中是否有ExceptNodeID节点
                foreach (MenuTree tree in trees)
                {
                    if (tree.MenuId == ExceptNodeID)
                    {
                        IsOver = true;
                        break;
                    }
                }
                int ParentID = -999;
                //对于本层中的节点
                foreach (MenuTree tree in trees)
                {
                    int NodeID = tree.MenuId;
                    //如果节点NodeID 和ExceptNodeID不是 直系关系
                    if (!IsMyChildrenNode(NodeID, ExceptNodeID) && NodeID != ExceptNodeID)
                    {
                        int[] GroupCode = MenuTreeCtrl.GetThisNodeVuserIdList(NodeID);

                        GroupCode = AddNewRoleToRole(GroupCode, Roles);
                        hstbl.Add(
                            new MenuNodeInfo()
                            {
                                Nodeid = NodeID,
                                AddedVUserids = new List<int>(GroupCode)
                            });
                    }
                    else if (NodeID != ExceptNodeID)
                    {
                        ParentID = NodeID;
                    }
                }
                if (!IsOver && ParentID != -999)//如果没有到ExceptNodeID所到层
                    //继续处理节点NodeID
                    AllocateToChildNodeExceptThisNode(ref hstbl, ParentID, ExceptNodeID, Roles);
            }
        }
        /// <summary>
        /// 合并两个角色（OldRoles，AddRoles）返回合并后的角色集合，
        /// 使得集合中所有角色都相互分离
        /// </summary>
        /// <param name="OldRoles">原来的角色集合</param>
        /// <param name="AddRoles">要添加的角色集合</param>
        /// <returns>合并后的角色集合（以;分隔）</returns>
        public static int[] AddNewRoleToRole(int[] OldRoles, int[] AddRoles)
        {
            if (AddRoles == null || AddRoles.Length == 0)
                return OldRoles;
            if (OldRoles == null || OldRoles.Length == 0)
                return AddRoles;
            //取得本节点的角色

            List<int> CombRole = new List<int>();
            CombRole.AddRange(OldRoles);//合并后的

            foreach (int code in AddRoles)
            {
                
                bool Isdone = false;
                foreach (int exitRole in OldRoles)
                {
                    
                    GpCodeRelation AbRel = GetTwoRelaction(exitRole, code);
                    if (AbRel == GpCodeRelation.Bycontain)//已经存在的某角色被要附加的角色包含
                    {
                        CombRole.Remove(exitRole);
                        //替换掉以前的，并不把办完标志置真，以便在全部替换掉后加上新的
                    }
                    else if (AbRel == GpCodeRelation.Contain)//如果已存在的包还要添加的。
                    {
                        Isdone = true;
                        break;
                    }
                }
                if (!Isdone)
                    CombRole.Add(code );
            }
            return CombRole.ToArray();

        }
        /// <summary>
        /// 取得大角色BigCode除去小角色smallCode的角色列表，以;分割
        /// （不管俩个相差多少级）
        /// </summary>
        /// <param name="BigCode">角色代码（如：1-1-1）</param>
        /// <param name="smallCode">属于BigCode的小角色代码（如：1-1-1-1）</param>
        /// <returns>角色BigCode除去smallCode的角色列表</returns>
        private static int[] GetRoleExceptThisRole(int BigCode, int smallCode)
        {
            using (var db = new WebPagesContext())
            {
                Webpages_VUser bigGroup = db.Webpages_VUsers.Find(BigCode);
                if (bigGroup == null)
                    throw new DataException("VuserId:" + BigCode);

                if (bigGroup.Type ==(int) VUserType.User)
                    return new int[0];

                var smGroup = db.Webpages_VUsers.Find(smallCode);
                if (smGroup == null)
                    throw new DataException("VuserId:" + smallCode);

                var group = bigGroup.Role;
                var list = group.GetOneFloorGroups(db);//取得角色BigCode的下一层子角色
                List<int> AllChildrenCode = new List<int>();
                if (list != null && list.Length > 0)
                {

                    foreach (var role in list)
                    {

                        var tmp = Webpages_VUser.CreateOrGetByGroupId(role.Code);

                        GpCodeRelation Rel = GetTwoRelaction(tmp.VUserId, smallCode);
                        if (Rel == GpCodeRelation.Equality)
                            continue;//如果相等，跳过
                        else if (Rel == GpCodeRelation.Separate)//如果相离
                            AllChildrenCode.Add(tmp.VUserId);//保存
                        else if (Rel == GpCodeRelation.Contain)//如果还包含
                        {
                            //递归取得下一层				
                            int[] rst = GetRoleExceptThisRole(tmp.VUserId, smallCode);
                            if (rst != null)
                                AllChildrenCode.AddRange(rst);
                        }

                    }

                }
                var users =  group.GetUsers(false);
                if (users != null && users.Length > 0)
                {
                    foreach (var user in users)
                    {

                        var tmp = Webpages_VUser.CreateOrGetByUserId(user.UserId);

                        GpCodeRelation Rel = GetTwoRelaction(tmp.VUserId, smallCode);
                        if (Rel == GpCodeRelation.Equality)
                            continue;//如果相等，跳过
                        else if (Rel == GpCodeRelation.Separate)//如果相离
                            AllChildrenCode.Add(tmp.VUserId);//保存
                        //用户不可能包含其他的用户或者机构

                    }
                }
                return AllChildrenCode.ToArray();
            }
        }

        /// <summary>
        /// 清空节点FunNodeID所有子节点中角色列表中等于或者包含于角色vuserId的角色列表，
        /// 并把改变的节点保存到哈希表hstbl中（按key NodeID ，value RolesList 方式）
        /// </summary>
        /// <param name="hstbl">引用的保存改变节点信息的哈系表</param>
        /// <param name="FunNodeID">节点ID</param>
        /// <param name="vuserId">角色代码</param>
        private static void ClearChildrenRole(ref List<MenuNodeInfo> hstbl, int FunNodeID, int vuserId)
        {
            //取得节点FunNodeID的所有子节点
            Dictionary<int, int[]> ChildrenNode = MenuTreeCtrl.GetChildrenNodeRoles(FunNodeID);

            if (ChildrenNode == null)
            {
                ChildrenNode = new Dictionary<int, int[]>();
            }
            int[] vusrs = MenuTreeCtrl.GetThisNodeVuserIdList(FunNodeID);
            if (vusrs != null)
            {
                ChildrenNode.Add(FunNodeID, vusrs);
            }
            foreach (var item in ChildrenNode)
            {
                int NodeID = item.Key;
                int[] vusers = item.Value;
                if (vusers != null)
                {
                    List<int> GourpCode = new List<int>(vusers);
                    List<int> removeVUserIds = new List<int>();
                    bool IsChange = DelRoleFromRoles(GourpCode, vuserId, ref removeVUserIds);
                    if (IsChange)
                        hstbl.Add(new MenuNodeInfo()
                        {
                            Nodeid = NodeID,
                            AddedVUserids = new List<int>(GourpCode),
                            RemovedVUserids = removeVUserIds
                        });
                }
            }

        }
        /// <summary>
        /// 清除角色列表GourpCode中等于或者包含于角色vuserId的角色列表，
        /// </summary>
        /// <param name="GourpCode"></param>
        /// <param name="vuserId"></param>
        /// <returns></returns>
        private static bool DelRoleFromRoles(List<int> list, int vuserId, ref List<int> removeVUserIds)
        {
            
            if (list == null || list.Count == 0)
                return false;
            
            bool IsChange = false;
            
            for(int i=0;i<list.Count;i++)
            {
                int code = list[i];
                GpCodeRelation AbRel = GetTwoRelaction(vuserId, code);
                if (AbRel == GpCodeRelation.Equality)//如果此节点中的角色列表中有此角色
                {
                    removeVUserIds.Add(code);
                    list.Remove(code);
                    //删除
                    IsChange = true;
                    break;//只有一个等于

                }
                else if (AbRel == GpCodeRelation.Contain)//如果此角色包含此节点中的某角色
                {
                    list.Remove(code);
                    removeVUserIds.Add(code);
                    //删除
                    IsChange = true;
                    i--;
                    continue;//可能有多个包含
                }
            }
            return IsChange;
        }

        //逻辑：
        //	FunNodeID的角色列表中一定是有包含此角色的角色，或者有完全相等的的角色
        /// <summary>
        /// 从节点FunNodeID的角色列表中删除vuserId。
        /// 注意：FunNodeID的角色列表中一定是有包含此角色的角色，或者有完全相等的的角色
        /// </summary>
        /// <param name="vuserId">要删除的角色代码</param>
        /// <param name="FunNodeID">节点ID</param>
        public static void DeleteOneRoleFromFun(int vuserId, int FunNodeID)
        {
           List<MenuNodeInfo> hstbl = new List<MenuNodeInfo>();

            int[] NodeGroupCode = MenuTreeCtrl.GetThisNodeVuserIdList(FunNodeID);
            List<int> list = new List<int>(NodeGroupCode);
            for(int i=0;i<list.Count;i++)
            
            {
                int code = list[i];
                GpCodeRelation AbRel = GetTwoRelaction(code, vuserId);
                if (AbRel == GpCodeRelation.Equality)//如果此节点中的角色列表已经有此角色
                {
                    //直接从列表中删除
                   list.Remove(code);
                   MenuNodeInfo m= MenuNodeInfo.GetByNodeId(hstbl, FunNodeID);
                   if (m == null)
                       hstbl.Add(new MenuNodeInfo()
                       {
                           Nodeid =
                           FunNodeID,
                           AddedVUserids = list,
                           RemovedVUserids = new List<int>(new int[] { code })
                       });//处理本节点
                   else
                   {
                       m.AddedVUserids.AddRange(list);
                       m.RemovedVUserids.Add(code);
                   }
                    //break;//退出循环
                }
                else if (AbRel == GpCodeRelation.Contain)//如果此节点中的某角色包含此角色
                {
                    //取得角色code集合中除去vuserId的集合
                    int[] RoleExceptThisRole = GetRoleExceptThisRole(code, vuserId);
                    //本节点：剔出掉大角色，
                    list.Remove(code);
                    //添加小角色到本节点角色列表
                    if (RoleExceptThisRole != null)
                    {
                        list.AddRange(RoleExceptThisRole);
                    }
                    MenuNodeInfo m = MenuNodeInfo.GetByNodeId(hstbl, FunNodeID);
                    if (m == null)
                        hstbl.Add(
                            new MenuNodeInfo()
                            {
                                Nodeid = FunNodeID,
                                AddedVUserids = list,
                                RemovedVUserids = new List<int>(new int[] { code })
                            });//处理本节点
                    else
                    {
                        
                        m.AddedVUserids.AddRange(list);
                        m.RemovedVUserids.Add(code);
                    }
                    //增加到本节点的孩子中除了FunNodeID直系的其他节点， 
                    AllocateToChildNodeExceptThisNode(ref hstbl, FunNodeID, FunNodeID, RoleExceptThisRole);
                    //   break;//退出循环
                }
            }
            SaveHashTableInfo(hstbl);
        }
        //逻辑：
        //	FunNodeID的角色列表中一定是有包含此角色的角色，或者有完全相等的的角色
        /// <summary>
        /// 从节点FunNodeID的角色列表中删除vuserId。FunNodeID不必是功能来源
        /// 于DeleteOneRoleFromFun相比,加了查找功能来源节点的功能,因此速度不比.
        /// </summary>
        /// <param name="vuserId">要删除的角色代码</param>
        /// <param name="FunNodeID">节点ID</param>
        public static void DeleteOneRoleFromFun_1(int vuserId, int FunNodeID)
        {
            bool isDone = false;
            List<MenuNodeInfo> hstbl = new List<MenuNodeInfo>();

            int[] parentNode = MenuTreeCtrl.GetParentNodeRoles(FunNodeID);
            List<int> parentList = new List<int>(parentNode);
            int[] ThisNodeGroupCode = MenuTreeCtrl.GetThisNodeVuserIdList(FunNodeID);
            parentList.Add(FunNodeID);//把此节点也当父节来来一块处理
            foreach (int NodeID in parentList)//从父节点或者本节点获取的权限
            {

                int[] NodeGroupCode = MenuTreeCtrl.GetThisNodeVuserIdList(NodeID);
                if (NodeGroupCode == null || NodeGroupCode.Length == 0) continue;
                foreach (int code in NodeGroupCode)
                {
                    GpCodeRelation AbRel = GetTwoRelaction(code, vuserId);
                    if (AbRel == GpCodeRelation.Equality ||//如果此节点中的角色列表已经有此角色
                        AbRel == GpCodeRelation.Contain)//如果此节点中的某角色包含此角色
                    {
                        //取得角色code集合中除去vuserId的集合
                        int[] RoleExceptThisRole = GetRoleExceptThisRole(code, vuserId);

                        List<int> tmpa = new List<int>(RoleExceptThisRole);
                        tmpa.Remove(code);
                        MenuNodeInfo m = MenuNodeInfo.GetByNodeId(hstbl, NodeID);
                        if (m == null)
                            hstbl.Add(new MenuNodeInfo()
                        {
                            Nodeid = NodeID,
                            AddedVUserids = tmpa,
                            RemovedVUserids = new List<int>(new int[] { code })
                        });//处理本节点
                        else
                        {
                            m.AddedVUserids.AddRange(tmpa);
                            m.RemovedVUserids.Add(code);
                        }


                        //增加到本节点的孩子中除了FunNodeID直系的其他节点， 
                        AllocateToChildNodeExceptThisNode(ref hstbl, NodeID, FunNodeID, new int[] { vuserId });
                        isDone = true;
                        //break;//退出循环
                    }
                }
                if (isDone) break;

            }
            if (!isDone)//从子节点获取的权限
            {
                ClearChildrenRole(ref hstbl, FunNodeID, vuserId);
            }
            SaveHashTableInfo(hstbl);


        }


        /// <summary>
        /// 保存 存有节点ID和本节点更改后的角色列表  信息 的Hashtable到数据库
        /// </summary>
        /// <param name="hstbl">存有节点ID和本节点更改后的角色列表  信息 的Hashtable</param>
        private static void SaveHashTableInfo(List<MenuNodeInfo> hstbl)
        {
            if (hstbl == null)
                return;

            using (var db = new WebPagesContext())
            {
                var tra = db.Database.Connection.BeginTransaction();
                try
                {

                    foreach (MenuNodeInfo item in hstbl)
                    {
                        int menuid = item.Nodeid;

                        foreach (int vuserid in item.AddedVUserids)
                        {
                            var right = db.MenuTreeRights.Find(menuid ,vuserid);
                            if (right == null)
                            {
                                right = new MenuTreeRight();
                                right.MenuId = menuid;
                                right.VuserID = vuserid;
                                db.MenuTreeRights.Add(right);
                            }
                           
                        }
                        if (item.RemovedVUserids != null)
                        {
                            StringBuilder strDeleteVuserids = new StringBuilder();
                            foreach (int vuserid in item.RemovedVUserids)
                            {
                                var menu = db.MenuTrees.Find(menuid, vuserid);
                                db.Entry(menu).State = System.Data.EntityState.Deleted;
                            }
                            
                        }
                    }
                    db.SaveChanges();
                    tra.Commit();
                }
                catch
                {
                    tra.Rollback();
                    throw;
                }
                finally
                {
                    db.Dispose();
                }
            }
        }
        
        /// <summary>
        /// 判断两个Vuser角色的关系
        /// </summary>
        /// <param name="CodeA">角色1</param>
        /// <param name="CodeB">角色2</param>
        /// <returns></returns>
        public static GpCodeRelation GetTwoRelaction(int CodeA, int CodeB)
        {
            if (CodeA == 0 || CodeB == 0)
                return GpCodeRelation.Error;
            if (CodeA == CodeB)
                return GpCodeRelation.Equality;
            using (var db = new WebPagesContext())
            {
                var a = db.Webpages_VUsers.Find(CodeA);
                var b = db.Webpages_VUsers.Find(CodeB);
                if (a== null)
                    return GpCodeRelation.Error;
                if (b == null)
                    return GpCodeRelation.Error;


                if (a.Type == (int)VUserType.User && b.Type ==(int) VUserType.User)//两个全是用户,那么可能是相等或者相离
                {
                    if (a.UserID ==  b.UserID)
                        return GpCodeRelation.Equality;
                    else
                        return GpCodeRelation.Separate;
                }
                else if (a.Type == (int)VUserType.User && b.Type == (int)VUserType.Group)//A是用户,B是机构，可能是A被B包含或者相离
                {
                    var u = a.User;
                    var g = b.Role;
                    if (System.Web.Security.Roles.IsUserInRole(u.UserName,g.RoleName))
                        return GpCodeRelation.Bycontain;
                    else
                        return GpCodeRelation.Separate;

                }
                else if (a.Type == (int)VUserType.Group && b.Type == (int)VUserType.Group)
                {

                    if (MenuTreeCtrl.IsGroupInGroup(a.Role.Code, b.Role.Code))
                        return GpCodeRelation.Bycontain;
                    else if (MenuTreeCtrl.IsGroupInGroup(b.Role.Code, a.Role.Code))
                        return GpCodeRelation.Contain;
                    else
                        return GpCodeRelation.Separate;

                }
                else if (a.Type ==(int) VUserType.Group && b.Type == (int)VUserType.User)
                {
                    var u = b.User;
                    var g = a.Role;
                    if (System.Web.Security.Roles.IsUserInRole(u.UserName,g.RoleName))
                        return GpCodeRelation.Contain;
                    else
                        return GpCodeRelation.Separate;

                }
                return GpCodeRelation.Error;
            }
        }
        /// <summary>
        /// 判断功能树中节点childNodeID是不是parentNodeID的子节点（不管多少级）
        /// </summary>
        /// <param name="parentNodeID"></param>
        /// <param name="childNodeID"></param>
        /// <returns></returns>
        public static bool IsMyChildrenNode(int parentNodeID, int childNodeID)
        {
            //取得parentNodeID的所有父节点列表
            int[] parentNodes = MenuTreeCtrl.GetParentNodeRoles(childNodeID);
            if (parentNodes == null)
                return false;
            //判断在否
            foreach (int NodeID in parentNodes)
            {
                if (NodeID == parentNodeID)
                    return true;
            }
            return false;
        }
        ///// <summary>
        ///// 更新节点的编辑角色列表
        ///// </summary>
        //public static bool RefreshEditRoles()
        //{
        //    FLOW_MENUTREE.TblQuery stu = new FLOW_MENUTREE.TblQuery(ConfigInfo.DataServerType);
        //    stu.ParameterSQL = " EDITGROUPCODE IS NOT NULL ";
        //    FLOW_MENUTREE treeHelper = new FLOW_MENUTREE(ConfigInfo.DataServerType);
        //    BaseTableClass[] list = treeHelper.ControlHelper.GetArrayListBy(stu, ConfigInfo.ConnectionString);
        //    if (list != null && list.Length > 0)
        //    {
        //        bool rtn = false;
        //        bool[] changeList = new bool[list.Length];
        //        int i = 0;
        //        foreach (FLOW_MENUTREE menu in list)
        //        {
        //            changeList[i] = false;
        //            string viewRoles = MenuTreeCtrl.GetThisNodeAllViewRoles(menu.MENUID.Value);
        //            if (viewRoles != null && viewRoles.Length > 0)
        //            {

        //                string tmpstr = menu.EDITGROUPCODE;
        //                string tmps = RolesCombine(tmpstr, viewRoles);

        //                if (tmpstr != tmps)
        //                {
        //                    menu.EDITGROUPCODE = tmps;
        //                    changeList[i] = true;
        //                    rtn = true;
        //                }

        //            }
        //            i++;
        //        }
        //        if (rtn)
        //        {
        //            DbConnection con = null;

        //            if (ConfigInfo.DataServerType == DataServerType.SqlServer)
        //            {
        //                con = new SqlConnection(ConfigInfo.ConnectionString);
        //            }
        //            else if (ConfigInfo.DataServerType == DataServerType.Oracle)
        //            {
        //                con = new OracleConnection(ConfigInfo.ConnectionString);
        //            }
        //            con.Open();
        //            DbTransaction Tra = con.BeginTransaction();


        //            i = 0;

        //            try
        //            {
        //                foreach (FLOW_MENUTREE menu in list)
        //                {
        //                    if (changeList[i++])
        //                    {
        //                        menu.Save(null,Tra);
        //                    }
        //                }
        //                Tra.Commit();
        //            }
        //            catch
        //            {
        //                Tra.Rollback();
        //                throw;
        //            }
        //            finally
        //            {
        //                con.Close();
        //            }
        //        }
        //        return rtn;
        //    }
        //    return false;
        //}
        /// <summary>
        /// 取两个角色集合的并集
        /// </summary>
        /// <param name="Roles1"></param>
        /// <param name="Roles2"></param>
        /// <returns></returns>
        public static int[] RolesCombine(int[] Roles1, int[] Roles2)
        {

            List<int> Rtn = new List<int>();
            foreach (int role1 in Roles1)
            {
                
                foreach (int role2 in Roles2)
                {
                    
                    GpCodeRelation rel = GetTwoRelaction(role1, role2);
                    switch (rel)
                    {
                        case GpCodeRelation.Bycontain://role1 > role2
                            Rtn.Add(role1);
                            break;
                        case GpCodeRelation.Contain:// role1 < role2
                            Rtn.Add(role2 );
                            break;
                        case GpCodeRelation.Equality://role1 = role2
                            Rtn.Add(role1 );
                            break;
                    }
                }
            }
            return Rtn.ToArray();
        }
    }
    public class MenuNodeInfo
    {
        public int Nodeid
        {
            get;
            set;
        }
        public List<int> AddedVUserids
        {
            get;
            set;
        }
        public List<int> RemovedVUserids
        {
            get;
            set;
        }
        
        public static MenuNodeInfo GetByNodeId(List<MenuNodeInfo> list, int NodeId)
        {
            foreach (var item in list)
                if (item.Nodeid == NodeId) return item;
            return null;
        }
        public override string ToString()
        {
            string rtn=Nodeid.ToString();
            if (AddedVUserids != null)
                rtn += " added:" + AddedVUserids.Count;
            if (RemovedVUserids != null)
                rtn += " removed:" + RemovedVUserids.Count;
            return rtn;
        }
    }
}
