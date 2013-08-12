using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using HTBox.Web.Lan;
using System.Xml;

namespace HTBox.Web
{
    public class SiteSetting
    {
        static SiteSetting()
        {
            string xmlfile = System.Configuration.ConfigurationManager.AppSettings["configFile"];
            if (string.IsNullOrEmpty(xmlfile))
                throw (new ConfigurationErrorsException(WebResource.ConfigElementNotFind + "configFile"));
            else if (HttpContext.Current != null)
                xmlfile = HttpContext.Current.Server.MapPath(xmlfile);

            XmlDocument doc = new XmlDocument();
            doc.Load(xmlfile);
            var siteName = doc.DocumentElement.SelectSingleNode("Global/WebSiteName");
            if (siteName != null)
                m_siteName = siteName.InnerText;
        }
        private static readonly string m_siteName;
        public static string SiteName
        {
            get
            {
                return m_siteName;
            }
     
        }
    }
}