using Constellation.Foundation.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;

namespace Verndale.Feature.LanguageFallback.Links
{
	/// <summary>
	/// Removes the language tag from links for the default language of an Item's site.
	/// </summary>
	public class SelectiveLanguageEmbedLinkProvider : LinkProvider
	{
		/// <summary>Gets the (friendly) URL of an item.</summary>
		/// <param name="item">The item.</param>
		/// <param name="options">The options.</param>
		/// <returns>The item URL.</returns>
		public override string GetItemUrl(Item item, UrlOptions options)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(options, nameof(options));

			var site = options.Site;

			if (site == null)
			{
				item.GetSite();
			}

			if (options.LanguageEmbedding == LanguageEmbedding.Always)
			{
				if (options.Language.CultureInfo.IetfLanguageTag == site.Language)
				{
					options.LanguageEmbedding = LanguageEmbedding.Never;
				}
			}

			var url = CreateLinkBuilder(options).GetItemUrl(item);

			if (options.LowercaseUrls)
			{
				url = url.ToLowerInvariant();
			}

			return url;
		}

	}
}
