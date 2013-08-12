using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Web.Mvc.Html;
using System.Collections.Specialized;
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
                if (k != currentPageUrlParameter)
                {
                    values.Add(k, querystring[k]);
                }
            }
            StringBuilder html =
            Enumerable.Range(startPage, totalPages)
            .Where(i => (currentPage - pagesToShow) < i & i < (currentPage + pagesToShow))
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
            html.Append(currentPage == totalPages ? "<span class=\"pagerPage currentPage pagerSubfix\"> 末页 </span>" : helper.ActionLink("末页", actionName, controller, values, new Dictionary<string, object> { { "class", "pagerPage lastPage pagerSubfix" } }).ToHtmlString())
                .Append(@"</div>");

            return MvcHtmlString.Create(html.ToString());
        }
    }
}