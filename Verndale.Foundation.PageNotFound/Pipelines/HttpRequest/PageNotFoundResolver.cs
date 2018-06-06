using System.Web;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;
using Verndale.Foundation.PageNotFound.Repositories;

namespace Verndale.Foundation.PageNotFound.Pipelines.HttpRequest
{
	/// <summary>
	/// Custom Sitecore Http Request Pipeline step that switches the context item 
	/// to the friendly 404 page while maintaing the current request url.
	/// </summary>
	public class PageNotFoundResolver : Constellation.Foundation.Contexts.Pipelines.ContextSensitiveHttpRequestProcessor
	{
		protected override void Execute(HttpRequestArgs args)
		{
			if (Sitecore.Context.Item != null)
			{
				Log.Debug($"Verndale.Foundation.PageNotFound PageNotFoundResolver: Found Context Item. Exiting.");
				return;
			}

			if (Sitecore.Context.Site == null || Sitecore.Context.Language == null || Sitecore.Context.Database == null)
			{
				Log.Warn($"Verndale.Foundation.PageNotFound PageNotFoundResolver: Could not execute for: {HttpContext.Current.Request.Url.OriginalString}", this);
				return;
			}

			var page = PageNotFoundRepository.GetPageNotFoundItem(Sitecore.Context.Site, Sitecore.Context.Language, Sitecore.Context.Database);

			if (page == null)
			{
				Log.Warn($"Verndale.Foundation.PageNotFound PageNotFoundResolver: Site {Sitecore.Context.Site.Name} has no valid setting for the NotFoundPageID attribute.", this);
			}

			Sitecore.Context.Item = page; // assign the 404 page to the context.
		}

		protected override void Defer(HttpRequestArgs args)
		{
			Log.Debug(
				$"Verndale.Foundation.PageNotFound PageNotFoundResolver: Execution deferred for request {HttpContext.Current.Request.Url.OriginalString}",
				this);
		}
	}
}
