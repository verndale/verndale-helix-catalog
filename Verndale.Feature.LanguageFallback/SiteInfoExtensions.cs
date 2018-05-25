using System.Collections.Generic;
using System.Linq;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Web;

namespace Verndale.Feature.LanguageFallback
{
	public static class SiteInfoExtensions
	{
		public static bool ShouldAutoCreateLanguageVersions(this SiteInfo siteInfo)
		{
			string setting = siteInfo.Properties["autoCreateLanguageVersions"];

			if (string.IsNullOrEmpty(setting))
			{
				return false;
			}

			return bool.Parse(setting);
		}

		public static LanguageEmbedding GetLanguageEmbedding(this SiteInfo siteInfo)
		{
			string rawSetting = siteInfo.Properties["languageEmbedding"];

			if (string.IsNullOrEmpty(rawSetting))
			{
				return LinkManager.LanguageEmbedding;
			}

			var setting = rawSetting.ToLower();

			switch (setting)
			{
				case "never":
					return LanguageEmbedding.Never;

				case "always":
					return LanguageEmbedding.Always;

				case "asneeded":
					return LanguageEmbedding.AsNeeded;

				default:
					return LinkManager.LanguageEmbedding;
			}
		}

		public static bool SupportsLanguage(this SiteInfo siteInfo, string languageCode)
		{
			if (siteInfo.Language == languageCode)
			{
				return true;
			}

			var options = ParseSupportedLanguages(siteInfo);

			return options.Contains(languageCode);
		}

		public static ICollection<Language> GetSupportedLanguages(this SiteInfo siteInfo)
		{
			//TODO: Make sure the languages are actually supported by the system.

			var list = new List<Language>();

			var defaultLanguage = Language.Parse(siteInfo.Language);

			if (defaultLanguage != null)
			{
				list.Add(defaultLanguage);
			}

			var options = ParseSupportedLanguages(siteInfo);

			foreach (var option in options)
			{
				var language = Language.Parse(option);

				if (language != null && !list.Contains(language))
				{
					list.Add(language);
				}
			}

			return list;
		}

		private static string[] ParseSupportedLanguages(SiteInfo siteInfo)
		{
			var rawSetting = siteInfo.Properties["supportedLanguages"];

			if (string.IsNullOrEmpty(rawSetting))
			{
				Log.Warn($"Verndale.Feature.LanguageFallback: Site {siteInfo.Name} does not have a value for the 'supportedLangauges' attribute.", typeof(SiteInfoExtensions));
				return new string[] { };
			}

			if (rawSetting.Contains("|"))
			{
				return rawSetting.Split('|');
			}

			return rawSetting.Split(',');
		}
	}
}
