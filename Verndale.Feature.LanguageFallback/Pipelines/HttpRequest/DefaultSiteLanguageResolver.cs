using System.Web;
using Constellation.Foundation.Contexts.Pipelines;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Web;

namespace Verndale.Feature.LanguageFallback.Pipelines.HttpRequest
{
	/// <summary>
	/// Overrides Sitecore's default behavior of leaving the last selected language active if there's no language
	/// in the URL. This will force the agnostic URL to use the site's default language.
	/// </summary>
	public class DefaultSiteLanguageResolver : ContextSensitiveHttpRequestProcessor
	{
		protected override void Defer(HttpRequestArgs args)
		{
			Log.Debug($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: Execution deferred for {HttpContext.Current.Request.Url.OriginalString}", this);
		}

		protected override void Execute(HttpRequestArgs args)
		{
			if (Sitecore.Context.Site == null)
			{
				// We can't do anything, exit.
				Log.Debug($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: No context site for {HttpContext.Current.Request.Url.OriginalString}", this);
				return;
			}

			// Figure out if the language is in the URL already
			var queryStringLanguage = GetLanguageFromQueryString(args);
			var pathLanguage = GetLanguageFromPath(HttpContext.Current.Request.FilePath);

			if (queryStringLanguage != null || pathLanguage != null)
			{
				Log.Debug($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: Language found in URL. {HttpContext.Current.Request.Url.OriginalString}", this);
				return;
			}

			// No language in the URL, therefore we can force the site's default language
			var langCode = Sitecore.Context.Site.Language;

			if (string.IsNullOrEmpty(langCode))
			{
				Log.Warn($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: No default language set for site {Sitecore.Context.Site.Name}. Using Sitecore's defaultLanguage setting.", this);
				langCode = Sitecore.Configuration.Settings.DefaultLanguage;
			}

			if (Language.TryParse(langCode, out Language language))
			{
				Log.Debug($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: Forcing language to {langCode} for {HttpContext.Current.Request.Url.OriginalString}", this);
				Sitecore.Context.Language = language;
				return;
			}

			Log.Warn($"Verndale.Feature.LanguageFallback DefaultSiteLanguageResolver: Could not force language with code {langCode} for {HttpContext.Current.Request.Url.OriginalString} ", this);
		}

		/// <summary>Extracts the name of the language. Borrowed from Sitecore's LanguageResolver</summary>
		/// <param name="filePath">The file path.</param>
		/// <returns></returns>
		protected string GetLanguageFromPath(string filePath)
		{
			string languageName = WebUtil.ExtractLanguageName(filePath);
			if (!string.IsNullOrEmpty(languageName))
				return languageName;
			return (string)null;
		}

		/// <summary>Extracts the language from the query string. Borrowed from Sitecore's LanguageResolver</summary>
		/// <param name="args">The arguments.</param>
		/// <returns>Language that is parsed from <see cref="F:Sitecore.Pipelines.HttpRequest.LanguageResolver.LanguageQueryStringKey" /> value specified in request.</returns>
		protected Language GetLanguageFromQueryString(HttpRequestArgs args)
		{
			var queryString = GetQueryString(LanguageResolver.LanguageQueryStringKey, args);
			if (string.IsNullOrEmpty(queryString))
			{
				return null;
			}

			return !Language.TryParse(queryString, out var language) ? null : language;
		}
	}
}
