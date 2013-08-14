using System;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Configuration;

namespace HTBox.Web.Utility
{
    /// <summary>
    /// 
    /// </summary>
    sealed public class StringAnalyse
    {
        private StringAnalyse()
        {
        }
        /// <summary>
        /// 判断是不是可用于编程变量的字符，是字母、数字或者下划线
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool IsLetter(char ch)
        {
            if (Char.IsLetterOrDigit(ch))
                return true;
            if (ch == '_')
                return true;

            return false;
        }

        /// <summary>
        /// 在串source中找匹配对出现的位置
        /// </summary>
        /// <param name="source">预案串</param>
        /// <param name="leftch">左字符</param>
        /// <param name="rightch">右字符</param>
        /// <param name="isTransferredmeaning">是否是转义字符</param>
        /// <returns></returns>
        public static int GetPairChar(string source, char leftch, char rightch, bool isTransferredmeaning)
        {
            if (String.IsNullOrEmpty(source))
                return -1;
            int steps = 0;
            bool IsInQuo = source[0] == '"';//是否在""内,在""内的不算是,
            if (source[0] == rightch)//第一个就命中
                return 0;
            for (int i = 1; i < source.Length; i++)
            {
                if (source[i] == '"' && source[i - 1] != '\\' && leftch != '"')
                    IsInQuo = !IsInQuo;
                if (IsInQuo)
                    continue;
                if (source[i] == rightch &&
                    (!isTransferredmeaning ||
                    (isTransferredmeaning && source[i - 1] != '\\')))
                {
                    if (steps == 0)//找到匹配
                        return i;
                    else
                        steps--;
                }

                    //又发现左串
                else if (source[i] == leftch &&
                    (!isTransferredmeaning ||
                    (isTransferredmeaning && source[i - 1] != '\\')))
                    steps++;
            }
            return -1;
        }


        /// <summary>
        /// 把字符串按 ch 分隔 成列表 ，ch不能是 ( ) [ ] " \
        /// </summary>
        /// <param name="express"></param>
        /// <param name="ch">分割字符</param>
        /// <returns></returns>
        public static string[] SplitStrByChar(string express, char ch)
        {

            if (string.IsNullOrEmpty(express))
                throw new ArgumentNullException("express");
            //char[] chs = express.ToCharArray();
            List<string> lst = new  List<string>();
            int len = express.Length;
            StringBuilder tmpName = new StringBuilder();
            int flag = 0;
            bool IsEndQuo = true;//是否是关闭了的引号（前面有对应的了）
            for (int i = 0; i < len; i++)
            {
                if (express[i] == ch && flag == 0 && IsEndQuo)
                {
                    lst.Add(tmpName.ToString());
                    tmpName = new StringBuilder();

                    continue;
                }
                else if (IsEndQuo && (express[i] == '(' || express[i] == '[')) //不在字符串常量内的
                {
                    flag++;

                }
                else if (IsEndQuo && (express[i] == ')' || express[i] == ']'))
                {
                    flag--;
                }
                else if (express[i] == '\"' && (i == 0 || (i > 0 && express[i - 1] != '\\')))
                {
                    IsEndQuo = !IsEndQuo;
                }
                tmpName.Append(express[i]);

            }
            lst.Add(tmpName.ToString());
            return lst.ToArray();

        }
        /// <summary>
        /// 在字符串中查找字符 ch 在紧挨变量 chary 出现 的位置，
        /// 中间可以是 空格 等非字母、变量、和下划线的字母
        /// </summary>
        /// <param name="chary"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static int FindCharEndWithVar(string chary, char ch)
        {
            if (chary == null)
                return -1;
            int i = 0;

            bool StartFind = false;
            for (i = 0; i < chary.Length; i++)
            {
                if (!IsLetter(chary[i]))//不是字符
                {
                    if (!StartFind)
                    {
                        StartFind = true;

                    }
                    if (StartFind && ch == chary[i])
                    {
                        break;
                    }
                }
                else if (StartFind)//是字符并且已经开始查找了 
                {//说明不存在
                    i = -1;
                    break;
                }

            }
            if (StartFind)
                return i;
            else
                return -1;
        }


        /// <summary>
        /// 替换串SourStr中所有变量PraName为Val
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <param name="praName"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ReplaceVar(string sourceStr, string praName, string val)
        {
            if (String.IsNullOrEmpty(sourceStr) ||
                String.IsNullOrEmpty(praName))
                return sourceStr;
            int start = 0;
            if (val == null)//把null的值换成string.Empty以防取长度时失败。
                val = "";

            start = sourceStr.IndexOf(praName);
            while (start != -1)
            {
                if (start > 0 && sourceStr.Substring(start - 1, 1) == "\\")
                {
                    start = sourceStr.IndexOf(praName, start + praName.Length);
                    continue;
                }
                //变量相对应
                if (sourceStr.Length > start + praName.Length)
                {
                    if (!StringAnalyse.IsLetter(sourceStr[start + praName.Length]))
                    {
                        sourceStr = sourceStr.Substring(0, start) + val + sourceStr.Substring(start + praName.Length);
                        start = sourceStr.IndexOf(praName, start + val.Length);
                    }
                    else
                    {
                        start = sourceStr.IndexOf(praName, start + praName.Length);
                    }

                }
                else
                {//恰巧变量在末尾
                    sourceStr = sourceStr.Substring(0, start) + val;
                    break;
                }


            }
            return sourceStr;
        }
        /// <summary>
        /// 从字符串中分析出所有的属性-值对，放到哈西表中
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="splitchar"></param>
        /// <returns></returns>
        public static Hashtable GetAttributes(string attributes, char splitchar)
        {
            if (attributes == null || attributes.Length == 0)
                return null;
            Regex r = new Regex(@"(?<name>[a-zA-Z_0-9 ]+)=(?<value>[^\f\n\r\t\v=" + splitchar + "]+)[" + splitchar + "]?", RegexOptions.IgnoreCase);
            Match m = r.Match(attributes);


            Hashtable hsb = new Hashtable(10, StringComparer.OrdinalIgnoreCase);
            while (m.Success)
            {
                string name = m.Groups["name"].Value;
                string val = m.Groups["value"].Value;

                if (hsb.ContainsKey(name))
                {
                    hsb[name] = val;
                }
                else
                {
                    hsb.Add(name, val);
                }
                m = m.NextMatch();

            }
            return hsb;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="express"></param>
        /// <returns></returns>
        public static string[] SplitByBracket(string express)
        {
            Regex r = new Regex("\\[(?<item>[^\f\n\r\t\v\\[\\]]+)\\]", RegexOptions.IgnoreCase);
            Match m = r.Match(express);

            List<string> list = new List<string>();
            while (m.Success)
            {
                list.Add(m.Groups["item"].Value);

                m = m.NextMatch();
            }
            return list.ToArray();
        }
        #region//截取字符串
        //*********************************************************************
        /// <summary>
        /// 取得字符strValue的绝对长度（汉字占两个，字母和数字等其他字符占一个）
        /// </summary>
        /// <param name="length"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        //*********************************************************************
        public static string GetAbsSubstring(string str, int length)
        {
            if (str == null || str.Length == 0)
                return str;
            if (length <= 0)
                throw new ArgumentOutOfRangeException("length");

            System.Text.Encoding ed = System.Text.Encoding.Default;
            Byte[] clearBytes = ed.GetBytes(str);
            if (clearBytes.Length <= length)
                return str;

            byte[] bAry = new byte[2];
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < clearBytes.Length; i++)
            {
                if (i == length)
                    break;
                if (clearBytes[i] >> 7 == 1)
                {
                    if (i == length - 1)
                        break;//当正好有一个汉字卡在中间
                    bAry[0] = clearBytes[i];
                    bAry[1] = clearBytes[++i];
                    sb.Append(ed.GetString(bAry));
                }
                else
                    sb.Append((char)clearBytes[i]);

            }
            return sb.ToString();

        }
        #endregion
      


        /// <summary>
        /// 取得页面除去http，以及所传参数，并大写
        /// </summary>
        /// <param name="pageName">页面名</param>
        /// <returns></returns>
        public static string GetPagePurename(string pageName)
        {

            if (String.IsNullOrEmpty(pageName))
                return "";

            int n = pageName.LastIndexOf('?');
            if (n != -1)
                pageName = pageName.Substring(0, n);
            pageName = pageName.ToUpper(CultureInfo.CurrentCulture);
            pageName = pageName.Replace('\\', '/');
            return pageName;

        }
        /// <summary>
        /// 取得页面除去http，本虚拟，以及所传参数，并大写
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetPagePurename(System.Web.HttpRequest request)
        {
            if (request == null)
                return null;
            if (!String.IsNullOrEmpty(request.ApplicationPath))
            {
                return GetPagePurename(request.Path.Substring(request.ApplicationPath.Length + 1));
            }
            return GetPagePurename(request.Path.Substring(1));
        }
        /// <summary>
        /// 转换普通文本到Html格式
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ConvertCommonTextToHtml(string str)
        {
            if (str == null || str.Length == 0)
                return str;
            str = System.Web.HttpUtility.HtmlEncode(str);

            str = str.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
            str = str.Replace(" ", "&nbsp;");
            str = str.Replace("\r\n", "<br>");
            return str;
        }
        private static object lockObj = new object();
        private static Encoding m_ResponseEncoding;
        /// <summary>
        /// 
        /// </summary>
        public static Encoding ResponseEncoding
        {
            get
            {
                if (null != m_ResponseEncoding)
                    return m_ResponseEncoding;
                lock (lockObj)
                {
                    if (null != m_ResponseEncoding)
                        return m_ResponseEncoding;
                    GlobalizationSection gs = ConfigurationManager.GetSection("system.web/globalization")
                        as GlobalizationSection;

                    if (null != gs)
                    {
                        m_ResponseEncoding = gs.ResponseEncoding;
                    }
                    else
                    {
                        m_ResponseEncoding = UTF8Encoding.UTF8;
                    }
                }
                return m_ResponseEncoding;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string GetHtmlFromControl(System.Web.UI.Control c)
        {
            if (c == null)
                return null;
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            System.IO.StreamWriter writer = new System.IO.StreamWriter(stream,
                ResponseEncoding);

            writer.AutoFlush = true;
            System.Web.UI.HtmlTextWriter htmlwr = new System.Web.UI.HtmlTextWriter(writer);
            c.RenderControl(htmlwr);
            return ResponseEncoding.GetString(stream.ToArray());
        }
        /// <summary>
        /// Get Exception stack trace detail
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string EnhancedStackTrace(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ex.ToString());

            sb.Append(EnhancedStackTrace(new StackTrace(ex, true)));
            return sb.ToString();
        }
        private static string EnhancedStackTrace(StackTrace st)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Environment.NewLine);
            sb.Append("---- Stack Trace ----");
            sb.Append(Environment.NewLine);
            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);
                MemberInfo mi = sf.GetMethod();
                sb.Append(StackFrameToString(sf));
            }
            sb.Append(Environment.NewLine);
            return sb.ToString();
        }
        private static string StackFrameToString(StackFrame sf)
        {
            StringBuilder sb = new StringBuilder();
            int intParam;
            MemberInfo mi = sf.GetMethod();
            sb.Append("   ");
            sb.Append(mi.DeclaringType.Namespace);
            sb.Append(".");
            sb.Append(mi.DeclaringType.Name);
            sb.Append(".");
            sb.Append(mi.Name);
            // -- build method params          
            sb.Append("(");
            intParam = 0;
            foreach (ParameterInfo param in sf.GetMethod().GetParameters())
            {
                intParam += 1;
                sb.Append(param.Name);
                sb.Append(" As ");
                sb.Append(param.ParameterType.Name);
            }
            sb.Append(")");
            sb.Append(Environment.NewLine);
            // -- if source code is available, append location info         
            sb.Append("       ");
            if (string.IsNullOrEmpty(sf.GetFileName()))
            {
                sb.Append("(unknown file)");
                //-- native code offset is always available             
                sb.Append(": N ");
                sb.Append(String.Format("{0:#00000}", sf.GetNativeOffset()));
            }
            else
            {
                sb.Append(System.IO.Path.GetFileName(sf.GetFileName()));
                sb.Append(": line ");
                sb.Append(String.Format("{0:#0000}", sf.GetFileLineNumber()));
                sb.Append(", col ");
                sb.Append(String.Format("{0:#00}", sf.GetFileColumnNumber()));
                if (sf.GetILOffset() != StackFrame.OFFSET_UNKNOWN)
                {
                    sb.Append(", IL ");
                    sb.Append(String.Format("{0:#0000}", sf.GetILOffset()));
                }
            }
            sb.Append(Environment.NewLine);
            return sb.ToString();

        }
    }

}
