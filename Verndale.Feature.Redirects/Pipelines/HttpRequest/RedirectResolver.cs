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

			var redirect = CheckRedirect(localPath);

			if (string.IsNullOrEmpty(redirect?.NewUrl))
			{
				Log.Debug($"Verndale RedirectResolver: No redirect for {localPath}", this);
				return;
			}

			string hostname = Regex.Replace(Context.Site.TargetHostName, @"\/$", string.Empty); //Remove last slash in url if present

			if (redirect.IsPermanent)
			{
				Log.Info($"Verndale RedirectResolver: permanently redirecting from {localPath} to {redirect.NewUrl}", this);
				HttpContext.Current.Response.RedirectPermanent($"{hostname}{redirect.NewUrl}", true);

			}
			else
			{
				Log.Info($"Verndale RedirectResolver: basic redirecting from {localPath} to {redirect.NewUrl}", this);
				Sitecore.Web.WebUtil.Redirect($"{hostname}{redirect.NewUrl}");
			}
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
		private UrlRedirect CheckRedirect(string sourceUrl)
		{
			var site = Sitecore.Context.Site.SiteInfo;
			var repository = new Repository("sitecore_master_index");
			return repository.GetNewUrl(site, sourceUrl);
		}
	}
}
