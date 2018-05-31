using System.Web;
using Constellation.Foundation.Contexts.Pipelines;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Web;

namespace Verndale.Feature.LanguageFallback.Pipelines.HttpRequest
{
	/// <summary>
	/// Overrides Sitecore's default behavior for language resolution. If the URL contains no language information, it is assumed
	/// that the site's default language should be used, and the context language is forced into the site's default language. This
	/// object also verifies that any url-supplied language is valid for the context site and will (again) force the language to
	/// the default site language if an inappropriate language is passed in the URL.
	/// </summary>
	public class DefaultSiteLanguageResolver : ContextSensitiveHttpRequestProcessor
	{
		protected override void Execute(HttpRequestArgs args)
		{
			if (Sitecore.Context.Site == null)
			{
				// We can't do anything, exit.
				Log.Debug($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: No context site for {HttpContext.Current.Request.Url.OriginalString}", this);
				return;
			}

			// Figure out if the language is in the URL already 
			var urlLanguage = GetLanguageFromUrl(args);

			if (urlLanguage != null)
			{
				// and if the supplied language is legal for this site.
				if (IsUrlLanguageValidForSite(urlLanguage))
				{
					Sitecore.Context.Language = urlLanguage; // yup, we're done.
					return;
				}

				// Language is not supported. We need to 404.
				SetLanguageToDefaultForSite(); // This ensures the generated link has the right language.

				var target = Get404Item();
				var options = LinkManager.GetDefaultUrlOptions();
				options.LanguageEmbedding = LanguageEmbedding.Always; // force the URL to switch language.

				var url = LinkManager.GetItemUrl(target, options);

				HttpContext.Current.Response.Redirect(url, true);
			}

			// No valid language in the URL, therefore we can force the site's default language
			SetLanguageToDefaultForSite();
		}

		protected override void Defer(HttpRequestArgs args)
		{
			Log.Debug($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: Execution deferred for {HttpContext.Current.Request.Url.OriginalString}", this);
		}

		/// <summary>Extracts the name of the language. Borrowed from Sitecore's LanguageResolver</summary>
		/// <param name="filePath">The file path.</param>
		/// <returns></returns>
		protected string GetLanguageFromPath(string filePath)
		{
			return WebUtil.ExtractLanguageName(filePath);
		}

		/// <summary>Extracts the language from the query string. Borrowed from Sitecore's LanguageResolver</summary>
		/// <param name="args">The arguments.</param>
		/// <returns>Language that is parsed from <see cref="F:Sitecore.Pipelines.HttpRequest.LanguageResolver.LanguageQueryStringKey" /> value specified in request.</returns>
		protected string GetLanguageFromQueryString(HttpRequestArgs args)
		{
			return GetQueryString(LanguageResolver.LanguageQueryStringKey, args);
		}

		private Language GetLanguageFromUrl(HttpRequestArgs args)
		{
			var urlLanguage = GetLanguageFromPath(HttpContext.Current.Request.FilePath);

			if (string.IsNullOrEmpty(urlLanguage))
			{
				urlLanguage = GetLanguageFromQueryString(args);
			}

			if (Language.TryParse(urlLanguage, out Language language))
			{
				Log.Debug($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: Language found in URL. {HttpContext.Current.Request.Url.OriginalString}", this);
				return language;
			}

			return null;
		}

		private bool IsUrlLanguageValidForSite(Language urlLanguage)
		{
			if (!Sitecore.Context.Site.SiteInfo.SupportsLanguage(urlLanguage.CultureInfo.IetfLanguageTag))
			{
				return false;
			}

			Log.Debug($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: Supplied Url language is valid for site. {HttpContext.Current.Request.Url.OriginalString}", this);
			return true;
		}

		private void SetLanguageToDefaultForSite()
		{
			var langCode = Sitecore.Context.Site.Language;

			if (string.IsNullOrEmpty(langCode))
			{
				Log.Warn($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: No default language set for site {Sitecore.Context.Site.Name}. Using Sitecore's defaultLanguage setting.", this);
				langCode = Sitecore.Configuration.Settings.DefaultLanguage;
			}

			if (!Language.TryParse(langCode, out Language language))
			{
				Log.Warn($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: Could not resolve language with code {langCode} for {HttpContext.Current.Request.Url.OriginalString} ", this);
				return;
			}

			Log.Debug($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: Forcing language to {language.Name} for {HttpContext.Current.Request.Url.OriginalString}", this);
			Sitecore.Context.Language = language;
		}

		private Item Get404Item()
		{
			var info = Sitecore.Context.Site.SiteInfo;

			var id = ID.Parse(info.Properties["NotFoundPageID"]);

			if (id.IsNull || id.IsGlobalNullId)
			{
				return null;
			}

			if (!info.EnforceVersionPresence)
			{
				return Sitecore.Context.Database.GetItem(id);
			}

			using (new EnforceVersionPresenceDisabler())
			{
				return Sitecore.Context.Database.GetItem(id);
			}
		}
	}
}
