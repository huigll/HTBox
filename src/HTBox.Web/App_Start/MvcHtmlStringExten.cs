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
            .Aggregate(new StringBuilder(@"<div class=""pager-bar""><span>当前第 " + currentPage + " 页/共 " + totalPages + " 页</span>")
            .Append(currentPage == startPage ? "<span class=\"pagerPage currentPage pagerPrefix\"> 首页 </span>" :
            helper.ActionLink("首页", actionName, controller, values, new Dictionary<string, object> 
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
                    seed.AppendFormat("<span class=\"pagerPage currentPage\">{0}</span>", page);
                else
                    seed.Append(helper.ActionLink(page.ToString(), actionName, controller, values, htmlDic).ToHtmlString());
                return seed;
            });
            values[currentPageUrlParameter] = totalPages;
            html.Append(currentPage == totalPages ? "<span class=\"pagerPage currentPage pagerSubfix\"> 末页 </span>" :
                helper.ActionLink("末页", actionName, controller, 
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
        public static MvcHtmlString GetParentNavigation(this HtmlHelper helper, Menu menu)
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
    }
}