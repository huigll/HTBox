using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Data;
using System.Reflection;
using System.Globalization;
using System.Web.UI.WebControls;
using System.Collections.Generic;
namespace HTBox.Web.Utility
{
    /// <summary>
    /// 
    /// </summary>
	public abstract class UtilityArray
	{
		
		/// <summary>
		/// Convert array to string split by split char
		/// </summary>
		/// <param name="array"></param>
		/// <param name="splitChr"></param>
		/// <returns></returns>
		public static string ArrayToString(Array array,char splitChr)
		{
            if (array == null)
                throw new ArgumentNullException("array");
            if( array.Length==0)
			    return "";
			StringBuilder rtn = new StringBuilder();
			for(int i=0;i<array.Length;i++)
			{
				object str = array.GetValue(i);
				if (str != null)
					rtn.Append(str.ToString());
				
				rtn.Append(splitChr);
			}
			string Str = rtn.ToString();
			int tmp = Str.LastIndexOf(splitChr);
			if (tmp!=-1)
				Str = Str.Substring(0,tmp);
			return Str;
		}
		/// <summary>
        /// Convert array to string split by split ;
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		public static string ArrayToString(Array array)
		{
			return ArrayToString(array,';');
		}
		/// <summary>
        /// Convert list to string split by split char
		/// </summary>
		/// <param name="list"></param>
		/// <param name="splitChr"></param>
		/// <returns></returns>
		public static string ArrayToString(IList list,char splitChr)
		{
			StringBuilder rtn = new StringBuilder();
			if (list == null)
				return string.Empty;
			for(int i=0;i<list.Count;i++)
			{
				rtn.Append(list[i].ToString());
				rtn.Append(splitChr);
			}
			string str = rtn.ToString();
			int tmp = str.LastIndexOf(splitChr);
			if (tmp!=-1)
				str = str.Substring(0,tmp);
			return str;
		}
		
		/// <summary>
		/// find one item in array
		/// </summary>
		/// <param name="strs"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		public static int FindInArray(Array strs,string val)
		{
			if (strs == null || strs.Length == 0)
				return -1;
			
			for(int i=0;i<strs.Length;i++)
			{
				object str = strs.GetValue(i);
				if (str != null)
				{
					if (str.ToString() == val)
						return i;
				}
				else if (val == null)
				{
					return i;
				}
			}
			return -1;
		}
		/// <summary>
		/// delete one item from array
		/// </summary>
		/// <param name="ary"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		public static string[] DeleteFromArray(Array ary,string val)
		{
			if (ary == null )
				return null;
			else if ( ary.Length== 0 )
				return new string[0];
			List<string> lst = new  List<string>();
            lock (ary.SyncRoot)
			{
				for(int i=0;i<ary.Length;i++)
				{
					object obj = ary.GetValue(i);
					if (obj == null && val == null)
						continue;
					if (obj.ToString() != val)
						lst.Add(obj.ToString());
				}
			}
			return lst.ToArray();
		}
        /// <summary>
        /// delete repeat items from array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T[] DeleteRepeatItem<T>(T[] items)
        {
            if (items == null || items.Length == 0)
                return items;
            ArrayList list = new ArrayList();
            for (int i = 0; i < items.Length; i++)
            {
                T va1 = items[i];
                bool Ishave = false;
                for (int j = i + 1; j < items.Length; j++)
                {
                    if (va1.Equals(items[j]))
                    {
                        Ishave = true;
                        break;
                    }
                }
                if (!Ishave)
                    list.Add(va1);
            }
            return list.ToArray(typeof(T)) as T[];
        }
	
		
		
		
		
			
		
		
		/// <summary>
        /// half find one item in sorted list
		/// </summary>
		/// <param name="list"></param>
		/// <param name="date"></param>
		/// <returns></returns>
		public static bool HalfFindInArrayList(ArrayList list,DateTime date)
		{
          if (list == null || list.Count == 0)
				return false;
			int h = 0,//head
				e = list.Count-1;//end
			date = Convert.ToDateTime(date.ToShortDateString(),NumberFormatInfo.CurrentInfo);
			int p = ( h + e)/2;
			DateTime tmp;
			while( h <= p )
			{
				tmp = (DateTime)list[p] ;
				if (tmp == date )
				{
					return true;
				}
				else if ( tmp > date)//中间的大，往前移
				{
					e = p -1 ;
					if (e < 0)
						break;
				}
				else//中间的小，往后移
				{
					h = p + 1;
				}
				
				p = ( h + e)/2;
			}
			return false;
		}
    }
    /// <summary>
    /// 
    /// </summary>
    public class UtilityWeb
    {
        /// <summary>
        /// select one item in dropdownlist.
        /// </summary>
        /// <param name="listControl"></param>
        /// <param name="selVal"></param>
        /// <param name="isValue"></param>
        public static void SetDDlistSelect(
            System.Web.UI.WebControls.ListControl listControl,
            string selVal, bool isValue)
        {
            if (listControl == null || listControl.Items.Count == 0 ||
                selVal == null)
                return;
            lock (listControl)
            {
                listControl.SelectedIndex = -1;//清空原先选项
                foreach (System.Web.UI.WebControls.ListItem item in listControl.Items)
                {
                    if (!isValue)
                    {
                        if (item.Text == selVal)
                        {
                            item.Selected = true;
                            break;
                        }
                    }
                    else
                    {
                        if (item.Value == selVal)
                        {
                            item.Selected = true;
                            break;
                        }

                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="listControl"></param>
        public static void AddEnumToDropDownList(Type enumType, System.Web.UI.WebControls.ListControl listControl)
        {
            listControl.Items.Clear();
            string[] names = Enum.GetNames(enumType);
            Array values = Enum.GetValues(enumType);
            for (int i = 0; i < names.Length; i++)
            {
                listControl.Items.Add(new ListItem(names[i], ((int)values.GetValue(i)).ToString()));
            }
        }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dg"></param>
        /// <param name="totalCount"></param>
		/// <returns></returns>
		public static int FixDataGridCurrentPageIndex(
			System.Web.UI.WebControls.DataGrid dg,int totalCount)
		{
			if (dg == null)
				return 0;
			if (dg.PageSize == 0)
				return 0;
			if (totalCount  <= 0)
				dg.CurrentPageIndex = 0;
			
			if (dg.CurrentPageIndex >0 &&
				(float)totalCount/dg.PageSize <= 
				dg.CurrentPageIndex )
			{
				int index = totalCount/dg.PageSize;
				if (index >= 1)
					index = index - 1;
				dg.CurrentPageIndex = index;
				return index;
			}
			return 0;

		}
        /// <summary>
        /// 合并两个QueryString中的键值对，如果有重复的以第一个出现的为准。
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static string CombineQueryString(string str1, string str2)
        {
            string CombString = null;
            #region 合并两个串
            if (str1 != null && str1.Length > 0)
            {
                int p = str1.IndexOf('?');
                if (p != -1)
                {
                    str1 = str1.Substring(p + 1);
                    if (str1.Length > 0)
                        CombString = str1;
                }
                else
                {
                    CombString = str1;
                }
            }

            if (str2 != null && str2.Length > 0)
            {
                int p = str2.IndexOf('?');
                if (p != -1)
                {
                    str2 = str2.Substring(p + 1);
                    if (str2.Length > 0)
                    {
                        if (CombString != null && CombString.Length > 0)
                            CombString += '&' + str2;
                        else
                            CombString = str2;

                    }
                }
                else
                {
                    if (CombString != null && CombString.Length > 0)
                        CombString += '&' + str2;
                    else
                        CombString = str2;
                }
            }
            #endregion
            if (CombString == null || CombString.Length == 0)
                return null;
            //消除重复的
            string[] strAry = CombString.Split('&');

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strAry.Length; i++)
            {
                string pair1 = strAry[i];
                if (pair1 == null) continue;
                string[] nmVal1 = pair1.Split('=');

                for (int j = i + 1; j < strAry.Length; j++)
                {
                    string pair2 = strAry[j];
                    if (pair2 == null) continue;
                    string[] nmVal2 = pair2.Split('=');
                    if (string.Compare(nmVal1[0], nmVal2[0], true, CultureInfo.InvariantCulture) == 0)
                    {//有相同的，只添加第一个。
                        strAry[j] = null;
                    }
                }
                sb.Append(pair1);
                sb.Append('&');
            }
            if (sb[sb.Length - 1] == '&')
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();


        }
        /// <summary>
        /// myQuery
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paraname"></param>
        /// <returns></returns>
        public static string MyQueryString(System.Web.HttpRequest request, string paraname)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            string[] ary = request.QueryString.GetValues(paraname);
            if (ary != null && ary.Length > 0)
                return ary[0];
            return null;
        }
	}

}
