using System;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;
using Verndale.Feature.Redirects.Data;

namespace Verndale.Feature.Redirects.Pipelines.HttpRequest
{
	/// <summary>
	/// Custom Sitecore Http Request Pipeline step that looks up the current request URL in the redirect manager.
	/// </summary>
	public class RedirectResolver : Constellation.Foundation.Contexts.Pipelines.ContextSensitiveHttpRequestProcessor
	{
		protected override void Execute(HttpRequestArgs args)
		{
			if (Context.Item != null)
			{
				Log.Debug($"Verndale RedirectResolver: Found Context Item. Exiting.");
				return;
			}

			Uri url = new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path));
			var localPath = url.LocalPath;

			Log.Debug($"Verndale RedirectResolver processing: '{url}'", this);


			CheckRedirect(localPath);
		}

		protected override void Defer(HttpRequestArgs args)
		{
			Log.Debug(
				$"Verndale RedirectResolver: Execution deferred for request {HttpContext.Current.Request.Url.OriginalString}",
				this);
		}

		/// <summary>
		/// Checks the redirect.
		/// </summary>
		private void CheckRedirect(string sourceUrl)
		{
			// Check for the redirect without a wildcard.
			var redirect = Repository.GetNewUrl(sourceUrl, false);

			if (redirect == null)
			{
				// Check for redirect WITH wildcard.
				redirect = Repository.GetNewUrl(sourceUrl, true);
			}

			if (string.IsNullOrEmpty(redirect?.NewUrl))
			{
				Log.Debug($"Verndale RedirectResolver: No redirect for {sourceUrl}", this);
				return;
			}

			Log.Info($"Verndale RedirectResolver: redirecting from {sourceUrl} to {redirect.NewUrl}", this);
			string hostname = Regex.Replace(Context.Site.TargetHostName, @"\/$", string.Empty); //Remove last slash in url if present

			if (redirect.RedirectType)
			{
				HttpContext.Current.Response.RedirectPermanent($"{hostname}{redirect.NewUrl}", true);

			}
			else
			{
				Sitecore.Web.WebUtil.Redirect($"{hostname}{redirect.NewUrl}");
			}
		}
	}
}
