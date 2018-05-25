using System.Linq;
using System.Web.Mvc;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Mvc.Presentation;

namespace Verndale.Feature.LanguageFallback.Controllers
{
	public class AlternateLanguageLinksController : Controller
	{
		public ActionResult Index()
		{
			var thisItem = RenderingContext.Current.ContextItem;

			//TODO: get the Item's site, get the languages, create the language links.


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
	}
}
