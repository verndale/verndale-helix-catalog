using System;
using System.Web;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;

namespace Verndale.Feature.LanguageFallback.Pipelines.HttpRequest
{
	/// <summary>
	/// CultureResolver implements the HttpRequestProcessor
	/// If the current Sitecore Context Language is set, it will set the .NET CurrentUICulture and CurrentCulture to the Language Name
	/// By setting it at this level, it will be used for anything that involves culture formatting, like currency, dates, strings, etc
	/// </summary>
	public class CultureResolver : Constellation.Foundation.Contexts.Pipelines.ContextSensitiveHttpRequestProcessor
	{
		protected override void Execute(HttpRequestArgs args)
		{
			if (Sitecore.Context.Language == null)
			{
				Log.Debug($"Verndale.Feature.LanguageFallback CultureResolver: No context language for {HttpContext.Current.Request.Url.OriginalString}", this);
			}

			try
			{
				System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Sitecore.Context.Language.Name);
				System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture(Sitecore.Context.Language.Name);
			}
			catch (Exception ex)
			{
				Log.Error("Verndale.Feature.LanguageFallback CultureResolver: Error setting Culture for thread. " + ex.Message, this);
			}
		}

		protected override void Defer(HttpRequestArgs args)
		{
			Log.Debug($"Verndale.Feature.LanguageFallback CultureResolver: Execution deferred for {HttpContext.Current.Request.Url.OriginalString}", this);
		}
	}
}
