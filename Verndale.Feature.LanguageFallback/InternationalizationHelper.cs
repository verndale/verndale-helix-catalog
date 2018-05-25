using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using System.Linq;
using System.Web;

namespace Verndale.SharedSource.Helpers
{
    public class InternationalizationHelper
    {

        public static string GetLanguageFlagIconUrl(string languageName)
        {
            var url = "/~/icon/Flags/16x16/flag_usa.png";

            var thisLanguage = LanguageManager.GetLanguage(languageName);
            if (thisLanguage != null)
                url = "/~/icon/" + thisLanguage.GetIcon(Sitecore.Context.Database);

            return url;
        }

        // get the visitor's ip address
        private static string GetVisitorIP()
        {
            string visitorsIPAddr = string.Empty;
            if (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            {
                visitorsIPAddr = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            }
            else if (!string.IsNullOrEmpty(HttpContext.Current.Request.UserHostAddress))
            {
                visitorsIPAddr = HttpContext.Current.Request.UserHostAddress;
            }

            // HTTP_X_FORWARDED_FOR may contain a list of IPs delimited by ', '
            // remove the space and then split on the ,
            // get the last item, which should be correct
            if (visitorsIPAddr != null && visitorsIPAddr.Contains(","))
            {
                visitorsIPAddr = visitorsIPAddr.Replace(" ", "");
                var ipList = visitorsIPAddr.Split(',');
                visitorsIPAddr = ipList.Last();
            }

            return visitorsIPAddr;
        }

        public static bool IsValidForCountry(Item item)
        {
            // default is to return true, 
            // if session is null or country has not yet be set into session or the item is null 
            // or doesn't have any countries set in the Limit to Countries field, then should be valid
            // only is not valid if country code set in session is not among countries explicitly selected for the current item.
            if (HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session["Country Code"] != null && item != null)
            {
                var countries = SitecoreHelper.ItemRenderMethods.GetMultilistValueByFieldName("Limit To Countries", item);
                // only try to match if the current item has a value set in a Limit To Countries field
                if (countries != null && countries.Count > 0)
                {
                    var matchingCountry =
                        countries.Where(
                            x => x.Fields["Code"].ToString() == HttpContext.Current.Session["Country Code"].ToString());

                    return matchingCountry.Any();
                }
            }

            return true;
        }
    }
}