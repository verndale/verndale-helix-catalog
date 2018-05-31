using System;
using System.Web;
using Sitecore.Abstractions;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Sites;
using Sitecore.Web;

namespace Verndale.Foundation.Linking
{
	/// <summary>
	/// Allows the user to provide a custom LinkProvider on a site-by-site basis, which includes changing the behavior of generated links.
	/// </summary>
	/// <remarks>
	/// Taken with permission from https://jammykam.wordpress.com/2017/09/22/switching-link-manager/
	/// Original code: https://gist.github.com/jammykam/daa151abeeac8a7029bf
	/// </remarks>
	public class SwitchingLinkManager : BaseLinkManager
	{
		private readonly ProviderHelper<LinkProvider, LinkProviderCollection> _providerHelper;

		public SwitchingLinkManager(ProviderHelper<LinkProvider, LinkProviderCollection> providerHelper)
		{
			_providerHelper = providerHelper;
		}

		protected virtual LinkProvider Provider
		{
			get
			{
				var siteLinkProvider = (Sitecore.Context.Site != null)
					? Sitecore.Context.Site.Properties["linkProvider"] : String.Empty;

				if (String.IsNullOrEmpty(siteLinkProvider))
					return _providerHelper.Provider;

				return _providerHelper.Providers[siteLinkProvider]
					   ?? _providerHelper.Provider;
			}
		}

		/* below is copy/paste from Sitecore.Links.DefaultLinkManager cos Provider is marked internal :'( */

		public override bool AddAspxExtension => this.Provider.AddAspxExtension;
		public override bool AlwaysIncludeServerUrl => this.Provider.AlwaysIncludeServerUrl;
		public override LanguageEmbedding LanguageEmbedding => this.Provider.LanguageEmbedding;
		public override LanguageLocation LanguageLocation => this.Provider.LanguageLocation;
		public override bool LowercaseUrls => this.Provider.LowercaseUrls;
		public override bool ShortenUrls => this.Provider.ShortenUrls;
		public override bool UseDisplayName => this.Provider.UseDisplayName;

		public override string ExpandDynamicLinks(string text)
		{
			Assert.ArgumentNotNull(text, nameof(text));
			return this.ExpandDynamicLinks(text, false);
		}

		public override string ExpandDynamicLinks(string text, bool resolveSites)
		{
			Assert.ArgumentNotNull(text, nameof(text));
			return Assert.ResultNotNull<string>(this.Provider.ExpandDynamicLinks(text, resolveSites));
		}

		public override UrlOptions GetDefaultUrlOptions()
		{
			return Assert.ResultNotNull<UrlOptions>(this.Provider.GetDefaultUrlOptions());
		}

		public override string GetDynamicUrl(Item item)
		{
			return this.GetDynamicUrl(item, LinkUrlOptions.Empty);
		}

		public override string GetDynamicUrl(Item item, LinkUrlOptions options)
		{
			return this.Provider.GetDynamicUrl(item, options);
		}

		public override string GetItemUrl(Item item)
		{
			return this.Provider.GetItemUrl(item, this.GetDefaultUrlOptions());
		}

		public override string GetItemUrl(Item item, UrlOptions options)
		{
			return this.Provider.GetItemUrl(item, options);
		}

		public override bool IsDynamicLink(string linkText)
		{
			return this.Provider.IsDynamicLink(linkText);
		}

		public override DynamicLink ParseDynamicLink(string linkText)
		{
			return this.Provider.ParseDynamicLink(linkText);
		}

		public override RequestUrl ParseRequestUrl(HttpRequest request)
		{
			return this.Provider.ParseRequestUrl(request);
		}

		public override SiteInfo ResolveTargetSite(Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			return this.Provider.ResolveTargetSite(item);
		}

		public override SiteContext GetPreviewSiteContext(Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			return this.Provider.GetPreviewSiteContext(item);
		}
	}
}
