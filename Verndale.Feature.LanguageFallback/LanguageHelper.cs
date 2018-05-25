using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Sites;
using RestSharp.Extensions;

namespace Verndale.SharedSource.Helpers
{
    /// <summary>
    /// The LanguageHelper class provides properties and methods used by Verndale's customized language
    /// fallback modules. Depending on the configuration values set on a particular site element in web.config 
    /// items will either not be returned if a language version does not exist in a particular language 
    /// or the items will fallback to another language version and return that version instead.
    /// </summary>
    public class LanguageHelper
    {
        public static string GetAlternateLinksForPage()
        {
            var alternateLinkOutputStr = new StringBuilder();

            var targetHostName = "";
            var sites = SiteManager.GetSites();
            var thisSite = Sitecore.Context.Site;
            var thisItem = Sitecore.Context.Item;

            try
            {
                if (thisItem != null && sites.Any())
                {
                    string url = LinkManager.GetItemUrl(thisItem);
                    if (Sitecore.Context.Language != null)
                    {
                        var langCode = Sitecore.Context.Language.ToString().ToLower();
                        url = url.Replace("/" + langCode, "");
                    }
                    foreach (Language itemVersion in thisItem.Languages)
                    {
                        var thisLangCode = itemVersion.Name.ToLower();

                        var matchingSite =
                            sites.Where(
                                x =>
                                    x.Properties["startItem"] == thisSite.Properties["startItem"] &&
                                    x.Properties["mappedLanguages"].HasValue() &&
                                    x.Properties["mappedLanguages"].ToLower().Split('|').Any(y => y.Equals(thisLangCode)))
                                .FirstOrDefault();

                        if (matchingSite != null)
                        {
                            if (matchingSite.Properties["targetHostName"].HasValue())
                            {
                                targetHostName = matchingSite.Properties["targetHostName"];
                                var fullUrl = "http://" + targetHostName + "/" + thisLangCode + url;
                                alternateLinkOutputStr.AppendLine("<link rel=\"alternate\" hreflang=\"" + thisLangCode +
                                                                  "\" href=\"" + fullUrl + "\" />");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error getting alternate language links for page (GetAlternateLinksForPage): " + ex.Message, ex);
            }


            return alternateLinkOutputStr.ToString();
        }
    }
}
