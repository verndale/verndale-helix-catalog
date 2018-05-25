using Constellation.Foundation.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web;

namespace Verndale.Feature.LanguageFallback
{
	public static class ItemExtensions
	{
		/// <summary>
		/// Finds the hosting "site" for this item and uses the supportedLanguages setting to
		/// create language versions for each supported language if they do not exist.
		/// </summary>
		/// <param name="item">The Item to ensure version presence.</param>
		public static void CreateVersionForEachSupportedSiteLanguage(this Item item)
		{
			var site = item.GetSite();

			CreateVersionForEachSupportedSiteLanguage(item, site);
		}

		public static void CreateVersionForEachSupportedSiteLanguage(this Item item, string siteName)
		{
			var contextSite = Sitecore.Configuration.Factory.GetSite(siteName);

			CreateVersionForEachSupportedSiteLanguage(item, contextSite.SiteInfo);
		}

		public static void CreateVersionForEachSupportedSiteLanguage(this Item item, SiteInfo site)
		{
			if (!site.ShouldAutoCreateLanguageVersions())
			{
				Log.Warn(
					$"Verndale.Feature.LanguageFallback: Item Version Creation triggered for {site.Name} but automatic version creation is not enabled for this site.",
					typeof(ItemExtensions));
				return;
			}

			var languages = site.GetSupportedLanguages();

			foreach (Language language in languages)
			{
				Item localizedItem = item.Database.GetItem(item.ID, language);

				//if Versions.Count == 0 then no entries exist in the given language
				if (localizedItem.Versions.Count == 0)
				{
					localizedItem.Editing.BeginEdit();
					localizedItem.Versions.AddVersion();
					localizedItem.Editing.EndEdit();
				}
			}
		}
	}
}
