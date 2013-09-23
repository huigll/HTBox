using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Web.Mvc.Html;
using System.Collections.Specialized;
using System.Web.Routing;
using HTBox.Web.Models;
using HTBox.Web.Lan;

using System.Web.Caching;
using System.Collections;
using WebMatrix.WebData;
using Microsoft.Web.WebPages.OAuth;
namespace System.Web.Mvc
{
    public static class MvcHtmlStringExten
    {
        public static MvcHtmlString ActionImage(this HtmlHelper html, string action, object routeValues, 
            string imagePath, string alt="")
        {
            var url = new UrlHelper(html.ViewContext.RequestContext);

            // build the <img> tag
            var imgBuilder = new TagBuilder("img");
            
            imgBuilder.MergeAttribute("src", url.Content(imagePath));
            imgBuilder.MergeAttribute("alt", alt);
            string imgHtml = imgBuilder.ToString(TagRenderMode.SelfClosing);

            // build the <a> tag
            var anchorBuilder = new TagBuilder("a");
            anchorBuilder.MergeAttribute("href", url.Action(action, routeValues));
            anchorBuilder.InnerHtml = imgHtml; // include the <img> tag inside
            string anchorHtml = anchorBuilder.ToString(TagRenderMode.Normal);

            return MvcHtmlString.Create(anchorHtml);
        }

        public static MvcHtmlString ActionImage(this HtmlHelper html, string action, object routeValues,
          object htmlAttributes)
        {
            var url = new UrlHelper(html.ViewContext.RequestContext);
            var attribs = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            // build the <img> tag
            var imgBuilder = new TagBuilder("img");
            string imagePath = Convert.ToString(attribs["imagesrc"]) ;
            string alt = Convert.ToString(attribs["imagealt"]);
            attribs.Remove("imagesrc");
            attribs.Remove("imagealt");
            imgBuilder.MergeAttribute("src", url.Content(imagePath));
            imgBuilder.MergeAttribute("alt", alt);
            string imgHtml = imgBuilder.ToString(TagRenderMode.SelfClosing);

            // build the <a> tag
            var anchorBuilder = new TagBuilder("a");
            anchorBuilder.MergeAttribute("href", url.Action(action, routeValues));
            anchorBuilder.InnerHtml = imgHtml; // include the <img> tag inside
            anchorBuilder.MergeAttributes<string, object>(attribs);
            string anchorHtml = anchorBuilder.ToString(TagRenderMode.Normal);

            return MvcHtmlString.Create(anchorHtml);
        }

        public static MvcHtmlString Pager(this HtmlHelper helper, int startPage, int currentPage, int totalPages, int pagesToShow,
            string currentPageUrlParameter, NameValueCollection querystring)
        {
            System.Web.Routing.RouteData routeData = helper.ViewContext.RouteData;
            string actionName = routeData.GetRequiredString("action");
            string controller = routeData.GetRequiredString("controller");

            System.Web.Routing.RouteValueDictionary values = routeData.Values;
            if (!values.ContainsKey(currentPageUrlParameter))
                values.Add(currentPageUrlParameter, currentPage);
            values[currentPageUrlParameter] = 1;
            foreach (string k in querystring.AllKeys)
            {
                if(k != currentPageUrlParameter)
                    values[k] = querystring[k];
            }
            int left  = Math.Max(1,currentPage - pagesToShow/2);
            int right = Math.Min(left + pagesToShow-1, totalPages);
            if (right == totalPages)
                left = Math.Max(1, right - pagesToShow + 1);
            StringBuilder html =
            Enumerable.Range(startPage, totalPages)
            .Where(i => left <= i && i <= right)
            .Aggregate(new StringBuilder(string.Format(@"<div class=""pager-bar""><span>{0}</span>",
               HttpUtility.HtmlEncode( string.Format(WebResource.CurrentPageTotalPage,currentPage,totalPages))))
            .Append(currentPage == startPage ? "<span class=\"pagerPage currentPage pagerPrefix\"> " +
            HttpUtility.HtmlEncode(WebResource.FirstPage) + " </span>" :
            helper.ActionLink(WebResource.FirstPage, actionName, controller, values, new Dictionary<string, object> 
            { { "class", "pagerPage firstPage pagerPrefix" } }).ToHtmlString()),
            (seed, page) =>
            {
                values[currentPageUrlParameter] = page;
                string style = "pagerPage";
                if (page == startPage)
                    style += " firstPage";
                if (page == totalPages)
                    style += " lastPage";
                var htmlDic = new Dictionary<string, object>();
                htmlDic.Add("class", style);

                if (page == currentPage)
                    seed.AppendFormat("<span class=\"pagerPage currentPage\">{0}</span>", HttpUtility.HtmlEncode(page));
                else
                    seed.Append(helper.ActionLink(page.ToString(), actionName, controller, values, htmlDic).ToHtmlString());
                return seed;
            });
            values[currentPageUrlParameter] = totalPages;
            html.Append(currentPage == totalPages ? "<span class=\"pagerPage currentPage pagerSubfix\"> " + 
                HttpUtility.HtmlEncode(WebResource.LastPage) + " </span>" :
                helper.ActionLink(WebResource.LastPage, actionName, controller, 
                values, new Dictionary<string, object> { { "class", "pagerPage lastPage pagerSubfix" } }).ToHtmlString())
                .Append(@"</div>");

            return MvcHtmlString.Create(html.ToString());
        }


        public static MvcHtmlString ActionSortLink(this HtmlHelper helper, string linkText, string actionName,
            object routeValues, object htmlAttributes, NameValueCollection querystring)
        {
            System.Web.Routing.RouteData routeData = helper.ViewContext.RouteData;

            System.Web.Routing.RouteValueDictionary values = new RouteValueDictionary(routeData.Values);

            foreach (string k in querystring.AllKeys)
            {
                values[k] = querystring[k];
            }
            var curRouteValue = new System.Web.Routing.RouteValueDictionary(routeValues);
            foreach (var item in curRouteValue)
            {
                values[item.Key] = item.Value;
            }
            var htmlAttribs = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (querystring["orderby"] == Convert.ToString(values["orderby"]))
            {
                
                    if (Convert.ToString(values["desc"]) == "true")
                    {
                        values["desc"] = "false";
                    }
                    else
                    {
                        values["desc"] = "true";
                    }
            }
            else
            {
                values["desc"] = "false";
            }
            return helper.ActionLink(linkText, actionName, values, htmlAttribs);
        }
    }


    public static class MenuNavigation
    {
        public static MvcHtmlString GetParentNavigation(this HtmlHelper helper, WebMenu menu)
        {
            if (menu == null || !menu.ParentId.HasValue) return null;
            StringBuilder sb = new StringBuilder();
           
            MenuTree parent;
            int menuId = menu.ParentId.Value;
            using (var db = new WebPagesContext())
            {
                do
                {
                    parent = db.MenuTrees.First(o => o.MenuId == menuId);
                    sb.Insert(0," > " + helper.ActionLink(parent.MenuName, "Search", new { ParentID = parent.MenuId }));
                    menuId = parent.ParentId??0;

                } while (parent.ParentId.HasValue);
            }
            sb.Insert(0, helper.ActionLink("Root", "Search").ToString());
            return MvcHtmlString.Create(sb.ToString());
        }
        public static MvcHtmlString GetMenuHtml(this HtmlHelper helper, string userName)
        {
            string nouse;

            string html = BindMenu(userName, out nouse);
            return MvcHtmlString.Create(html);

        }
        public static MvcHtmlString GetMenuScript(this HtmlHelper helper, string userName)
        {
            string script;
            string nouse = BindMenu(userName, out script);
            return MvcHtmlString.Create(script);


        }

        public static void ClearMenuTreeCache()
        {
            List<string> cacheKeys = new List<string>();
            IDictionaryEnumerator cacheEnum = HttpContext.Current.Cache.GetEnumerator();
            while (cacheEnum.MoveNext())
            {
                cacheKeys.Add(cacheEnum.Key.ToString());
            }
            string cacheKeyPrefix = "UserMenuTree_";
            foreach (string cacheKey in cacheKeys)
            {
                if (cacheKey.StartsWith(cacheKeyPrefix) )
                    HttpContext.Current.Cache.Remove(cacheKey);
            }
        }
        private static string BindMenu(string userName, out string menuScript)
        {
            System.Web.UI.WebControls.TreeView menuTree;
            string menuHtml;
            string menuBodyHtml;
            string cacheKeyPrefix = "UserMenuTree_" + userName.ToUpper() + "_";
            string userMenuCacheKey = cacheKeyPrefix + "menuHtml";
            

            
            if (HttpContext.Current.Cache[userMenuCacheKey] == null ||
                HttpContext.Current.Cache[cacheKeyPrefix + "menuBodyHtml"] == null ||
                HttpContext.Current.Cache[cacheKeyPrefix + "menuScript"] == null)
            {
                #region
                menuTree = new System.Web.UI.WebControls.TreeView();
                string AppVirtualPath = HttpContext.Current.Request.ApplicationPath;
                Webpages_VUser vuser = null;
                if (!string.IsNullOrEmpty(userName))
                {
                    int userid = WebSecurity.GetUserId(userName);
                    
                    vuser = Webpages_VUser.CreateOrGetByUserId(userid);
                }
                MenuTreeCtrl.GetUserMenuTree(ref menuTree, AppVirtualPath, vuser,
                    MenuTreeCtrl.TreeRootID, false, true, false);

                StringBuilder sbMenuHtml = new StringBuilder();
                StringBuilder sbMenuBodyHtml = new StringBuilder();
                StringBuilder sbMenuScript = new StringBuilder();

                sbMenuScript.Append(
                    @"$(function() {$('.fg-button').hover(function(){{ $(this).removeClass('ui-state-default').addClass('ui-state-focus'); }},
    		            function(){{ $(this).removeClass('ui-state-focus').addClass('ui-state-default'); }}	);");
                for (int i = 0; i < menuTree.Nodes.Count; i++)
                {
                    System.Web.UI.WebControls.TreeNode node = menuTree.Nodes[i];
                    if (node.ChildNodes.Count > 0)
                    {
                        sbMenuHtml.AppendFormat(@"<a class='fg-button fg-button-icon-right ui-widget ui-state-default' id='menu{0}' href='{1}' target='{2}' menuName='{3}' ><span class='ui-icon ui-icon-triangle-1-s'></span>{3}</a>",
                            i,HttpUtility.HtmlAttributeEncode(string.IsNullOrEmpty(node.NavigateUrl) ? "#" : node.NavigateUrl),
                            HttpUtility.HtmlAttributeEncode(node.Target == MenuTreeCtrl.MainTarget ? "_self" : node.Target),
                            HttpUtility.HtmlAttributeEncode(node.Text));
                    }
                    else
                    {
                        sbMenuHtml.AppendFormat(@"<a class='fg-button fg-button-icon-right ui-widget ui-state-default' id='menu{0}' href='{1}' target='{2}' menuName='{3}' >{3}</a>",
                            i, HttpUtility.HtmlAttributeEncode(string.IsNullOrEmpty(node.NavigateUrl) ? "#" : node.NavigateUrl),
                            HttpUtility.HtmlAttributeEncode(node.Target == MenuTreeCtrl.MainTarget ? "_self" : node.Target),
                            HttpUtility.HtmlAttributeEncode(node.Text));
                    }
                    if (node.ChildNodes.Count > 0)
                    {
                        sbMenuScript.AppendFormat("$('#menu{0}').menu({{content: $('#menuItems{0}').html(),flyOut: true}});\n", i);
                        sbMenuBodyHtml.AppendFormat("<div id='menuItems{0}' class='hidden'>", i);
                        BindSubMenu(node, sbMenuBodyHtml);
                        sbMenuBodyHtml.Append("</div>");
                    }
                }
                sbMenuScript.Append("});");
                menuHtml = sbMenuHtml.ToString();
                menuBodyHtml = sbMenuBodyHtml.ToString();
                menuScript = sbMenuScript.ToString();
                HttpContext.Current.Cache.Add(cacheKeyPrefix + "menuHtml", menuHtml, null,
                    DateTime.MaxValue, new TimeSpan(0, HttpContext.Current.Session.Timeout, 0),
                    System.Web.Caching.CacheItemPriority.Normal, null);
                HttpContext.Current.Cache.Add(cacheKeyPrefix + "menuBodyHtml", menuBodyHtml, null,
                    DateTime.MaxValue, new TimeSpan(0, HttpContext.Current.Session.Timeout, 0), 
                    System.Web.Caching.CacheItemPriority.Normal, null);
                HttpContext.Current.Cache.Add(cacheKeyPrefix + "menuScript", menuScript, null,
                    DateTime.MaxValue, new TimeSpan(0, HttpContext.Current.Session.Timeout, 0), 
                    System.Web.Caching.CacheItemPriority.Normal, null);

                #endregion
            }
            else
            {
                menuHtml = HttpContext.Current.Cache[cacheKeyPrefix + "menuHtml"].ToString();
                menuBodyHtml = HttpContext.Current.Cache[cacheKeyPrefix + "menuBodyHtml"].ToString();
                menuScript = HttpContext.Current.Cache[cacheKeyPrefix + "menuScript"].ToString();
            }

            return  menuHtml + menuBodyHtml;



           
        }
        private static void BindSubMenu(System.Web.UI.WebControls.TreeNode parentNode, StringBuilder menuBodyHtml)
        {
            if (parentNode.ChildNodes.Count == 0)
            {
                return;
            }
            menuBodyHtml.Append("<ul>");
            for (int i = 0; i < parentNode.ChildNodes.Count; i++)
            {
                System.Web.UI.WebControls.TreeNode node = parentNode.ChildNodes[i];
                menuBodyHtml.AppendFormat(@"<li><a   href='{0}'  target='{1}' menuName='{2}'>{2}</a>",
                   HttpUtility.HtmlAttributeEncode(string.IsNullOrEmpty(node.NavigateUrl) ? "#" : node.NavigateUrl),
                   HttpUtility.HtmlAttributeEncode(node.Target == MenuTreeCtrl.MainTarget ? "_self" : node.Target),
                   HttpUtility.HtmlAttributeEncode(node.Text));

                BindSubMenu(node, menuBodyHtml);
                menuBodyHtml.Append("</li>");
            }

            menuBodyHtml.Append("</ul>");
        }

    }
}