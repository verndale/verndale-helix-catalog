using System.Text;
using System.Web.Mvc;
using Constellation.Foundation.Data;
using Sitecore.Links;
using Sitecore.Mvc.Presentation;

namespace Verndale.Feature.LanguageFallback.Controllers
{
	public class AlternateLanguageLinksController : Controller
	{
		public ActionResult Index()
		{
			var item = RenderingContext.Current.ContextItem;

			var site = item.GetSite();

			var content = new StringBuilder();

			foreach (var language in site.GetSupportedLanguages())
			{
				if (item.Language == language)
				{
					continue;
				}

				var alternateItem = item.Database.GetItem(item.ID, language);
				var options = LinkManager.GetDefaultUrlOptions();
				options.LanguageEmbedding = LanguageEmbedding.Always;

				content.AppendLine($"<link rel=\"alternate\" hreflang=\"{LinkManager.GetItemUrl(alternateItem, options)}\" />");
			}

			return Content(content.ToString());
		}
	}
}
