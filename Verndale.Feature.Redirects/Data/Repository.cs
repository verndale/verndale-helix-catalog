using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sitecore;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Verndale.Feature.Redirects.Data
{
	/// <summary>
	/// Provides access to the SQL redirect database.
	/// </summary>
	public static class Repository
	{

		/// <summary>
		/// Gets all redirects.
		/// </summary>
		/// <returns></returns>
		public static List<UrlRedirect> GetAll()
		{
			ISearchIndex index = BuildSearchIndex();
			using (IProviderSearchContext context = index.CreateSearchContext())
			{
				var search = context.GetQueryable<UrlRedirect>()
					.Where(IsRedirect<UrlRedirect>());
				return search.ToList();
			}
		}

		/// <summary>
		/// Finds a redirect by id.
		/// </summary>
		public static UrlRedirect GetById(string id)
		{
			Item redirect = Constants.Dbs.Database.GetItem(id);
			if (redirect == null)
			{
				return null;
			}
			return new UrlRedirect()
			{
				SiteName = redirect[Constants.FieldNames.SiteNameField],
				OldUrl = redirect[Constants.FieldNames.OldUrlField],
				NewUrl = redirect[Constants.FieldNames.NewUrlField],
				RedirectType = MainUtil.GetBool(redirect[Constants.FieldNames.TypeField], false)
			};
		}

		/// <summary>
		/// Deletes a redirect by ID.
		/// </summary>
		public static void Delete(string id)
		{
			Item redirect = Constants.Dbs.Database.GetItem(id);

			if (redirect == null)
			{
				return;
			}

			if (Sitecore.Configuration.Settings.RecycleBinActive)
			{
				redirect.Recycle();
			}
			else
			{
				redirect.Delete();
			}
		}

		/// <summary>
		/// Deletes all redirects.
		/// </summary>
		public static void DeleteAll()
		{
			Item redirectBucket = Constants.Dbs.Database.GetItem(Constants.Ids.RedirectBucketItemId);
			redirectBucket?.DeleteChildren();
		}

		/// <summary>
		/// Inserts a new redirect.
		/// </summary>
		public static ID Insert(string siteName, string oldUrl, string newUrl, bool type)
		{
			if (string.IsNullOrWhiteSpace(siteName))
			{
				throw new ArgumentException("siteName");
			}

			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				throw new ArgumentException("oldUrl");
			}

			if (string.IsNullOrWhiteSpace(newUrl))
			{
				throw new ArgumentException("newUrl");
			}

			Item redirectBucket = Constants.Dbs.Database.GetItem(Constants.Ids.RedirectBucketItemId);

			if (redirectBucket == null)
			{
				return null;
			}

			Item newItem = redirectBucket.Add(ItemUtil.ProposeValidItemName(siteName), new TemplateID(Constants.Ids.RedirectItemTemplateId));
			newItem.Editing.BeginEdit();
			newItem.Fields[Constants.FieldNames.SiteNameField].Value = siteName;
			newItem.Fields[Constants.FieldNames.OldUrlField].Value = oldUrl;
			newItem.Fields[Constants.FieldNames.NewUrlField].Value = newUrl;
			newItem.Fields[Constants.FieldNames.TypeField].Value = System.Convert.ToInt32(type).ToString();
			newItem.Editing.EndEdit();

			return newItem.ID;
		}

		/// <summary>
		/// Updates an existing redirect.
		/// </summary>
		public static void Update(ID id, string siteName, string oldUrl, string newUrl, bool type)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				throw new ArgumentException("oldUrl");
			}

			if (string.IsNullOrWhiteSpace(newUrl))
			{
				throw new ArgumentException("newUrl");
			}

			if (string.IsNullOrWhiteSpace(siteName))
			{
				throw new ArgumentException("siteName");
			}

			Item redirect = Constants.Dbs.Database.GetItem(id);

			if (redirect == null)
			{
				return;
			}

			redirect.Editing.BeginEdit();
			redirect.Name = ItemUtil.ProposeValidItemName(siteName);
			redirect.Fields[Constants.FieldNames.SiteNameField].Value = siteName;
			redirect.Fields[Constants.FieldNames.OldUrlField].Value = oldUrl;
			redirect.Fields[Constants.FieldNames.NewUrlField].Value = newUrl;
			redirect.Fields[Constants.FieldNames.TypeField].Value = System.Convert.ToInt32(type).ToString();
			redirect.Editing.EndEdit();
		}

		/// <summary>
		/// Determines if a redirect exists.
		/// </summary>
		public static bool RedirectExists(string oldUrl)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				return false;
			}

			ISearchIndex index = BuildSearchIndex();
			using (IProviderSearchContext context = index.CreateSearchContext())
			{
				return context.GetQueryable<UrlRedirect>().Where(IsRedirect<UrlRedirect>()).Any(x => x.OriginalUrlString == oldUrl);
			}
		}

		/// <summary>
		/// Checks if the old url exists.
		/// </summary>
		public static bool CheckUrlExists(ID id, string oldUrl)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				return false;
			}

			ISearchIndex index = BuildSearchIndex();
			using (IProviderSearchContext context = index.CreateSearchContext())
			{
				return context.GetQueryable<UrlRedirect>().Where(IsRedirect<UrlRedirect>()).Any(x => x.OriginalUrlString == oldUrl && x.ItemId != id);
			}
		}

		/// <summary>
		/// Gets the mapped redirect (i..e. new URL) for the given old / request url.
		/// </summary>
		public static UrlRedirect GetNewUrl(string requestUrl, bool performEndsWithWildcard)
		{
			if (string.IsNullOrWhiteSpace(requestUrl))
			{
				return null;
			}

			ISearchIndex index = BuildSearchIndex();
			using (IProviderSearchContext context = index.CreateSearchContext())
			{
				UrlRedirect redirect = null;

				if (performEndsWithWildcard)
				{
					List<UrlRedirect> potentialResults = context.GetQueryable<UrlRedirect>()
						.Where(IsRedirect<UrlRedirect>())
						.Where(x => x.NoWildCardUrl != String.Empty).ToList();

					redirect = potentialResults.FirstOrDefault(x => requestUrl.Contains(x.NoWildCardUrl));
				}
				else
				{
					redirect = context.GetQueryable<UrlRedirect>().Where(IsRedirect<UrlRedirect>())
						.FirstOrDefault(x => x.OriginalUrlString == requestUrl);
				}

				return redirect;
			}
		}

		/// <summary>
		/// Checks for the new URL.
		/// </summary>
		public static UrlRedirect CheckNewRedirect(string newUrl)
		{
			if (string.IsNullOrWhiteSpace(newUrl))
			{
				return null;
			}

			ISearchIndex index = BuildSearchIndex();
			using (IProviderSearchContext context = index.CreateSearchContext())
			{
				return context.GetQueryable<UrlRedirect>().Where(IsRedirect<UrlRedirect>()).FirstOrDefault(x => x.NewUrl == newUrl);
			}
		}

		/// <summary>
		/// Checks for the old url.
		/// </summary>
		public static UrlRedirect CheckOldRedirect(string oldUrl)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				return null;
			}

			ISearchIndex index = BuildSearchIndex();
			using (IProviderSearchContext context = index.CreateSearchContext())
			{
				return context.GetQueryable<UrlRedirect>().Where(IsRedirect<UrlRedirect>()).FirstOrDefault(x => x.OriginalUrlString == oldUrl);
			}
		}

		#region infrastructure

		/// <summary>
		/// Builds the search index.
		/// </summary>
		private static ISearchIndex BuildSearchIndex()
		{
			return ContentSearchManager.GetIndex("sitecore_master_index");
		}

		private static Expression<Func<T, bool>> IsRedirect<T>() where T : SearchResultItem
		{
			return searchResultItem => searchResultItem.TemplateId == Constants.Ids.RedirectItemTemplateId
									   && searchResultItem.Paths.Contains(Constants.Ids.RedirectBucketItemId);
		}
		#endregion

	}
}

