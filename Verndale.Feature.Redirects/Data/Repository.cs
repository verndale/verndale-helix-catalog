using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sitecore;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Web;

namespace Verndale.Feature.Redirects.Data
{
	/// <summary>
	/// Provides access to the SQL redirect database.
	/// </summary>
	public class Repository
	{
		/// <summary>
		/// Creates a new instance of Repository
		/// </summary>
		/// <param name="indexName"></param>
		public Repository(string indexName)
		{
			IndexName = indexName;
		}

		/// <summary>
		/// Gets the name of the Search Index to use.
		/// </summary>
		protected string IndexName { get; }


		private ISearchIndex _index;

		/// <summary>
		/// Gets the current instance of ISearchIndex
		/// </summary>
		protected ISearchIndex Index
		{
			get
			{
				if (_index == null)
				{
					_index = ContentSearchManager.GetIndex(IndexName);
				}

				return _index;
			}
		}


		/// <summary>
		/// Gets all redirects.
		/// </summary>
		/// <returns></returns>
		public List<UrlRedirect> GetAll()
		{
			using (IProviderSearchContext context = Index.CreateSearchContext())
			{
				var search = context.GetQueryable<UrlRedirect>()
					.Where(IsRedirect<UrlRedirect>());
				return search.ToList();
			}
		}

		/// <summary>
		/// Finds a redirect by id.
		/// </summary>
		public UrlRedirect GetById(string id)
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
				IsPermanent = MainUtil.GetBool(redirect[Constants.FieldNames.TypeField], false)
			};
		}

		/// <summary>
		/// Deletes a redirect by ID.
		/// </summary>
		public void Delete(string id)
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
		public void DeleteAll()
		{
			Item redirectBucket = Constants.Dbs.Database.GetItem(Constants.Ids.RedirectBucketItemId);
			redirectBucket?.DeleteChildren();
		}

		/// <summary>
		/// Inserts a new redirect.
		/// </summary>
		public ID Insert(string siteName, string oldUrl, string newUrl, bool type)
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
		public void Update(ID id, string siteName, string oldUrl, string newUrl, bool type)
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
		public bool RedirectExists(string oldUrl)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				return false;
			}

			using (IProviderSearchContext context = Index.CreateSearchContext())
			{
				return context.GetQueryable<UrlRedirect>().Where(IsRedirect<UrlRedirect>()).Any(x => x.OriginalUrlString == oldUrl);
			}
		}

		/// <summary>
		/// Checks if the old url exists.
		/// </summary>
		public bool CheckUrlExists(ID id, string oldUrl)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				return false;
			}

			using (IProviderSearchContext context = Index.CreateSearchContext())
			{
				return context.GetQueryable<UrlRedirect>().Where(IsRedirect<UrlRedirect>()).Any(x => x.OriginalUrlString == oldUrl && x.ItemId != id);
			}
		}

		/// <summary>
		/// Gets the mapped redirect (i..e. new URL) for the given old / request url.
		/// </summary>
		public UrlRedirect GetNewUrl(SiteInfo site, string requestUrl, bool endsWithWildcard)
		{
			Assert.ArgumentNotNull(site, "site");
			Assert.ArgumentNotNullOrEmpty(requestUrl, "requestUrl");

			using (IProviderSearchContext context = Index.CreateSearchContext())
			{
				UrlRedirect redirect = null;
				IQueryable<UrlRedirect> query = context.GetQueryable<UrlRedirect>();
				query = query.Filter(i => i.Paths.Contains(Constants.Ids.RedirectBucketItemId))
							.Filter(i => i.SiteName == site.Name)
							.Filter(i => i.TemplateId == Constants.Ids.RedirectItemTemplateId);


				if (endsWithWildcard)
				{
					return query.Filter(i => i.NoWildCardUrl != string.Empty)
						.FirstOrDefault(i => requestUrl.Contains(i.NoWildCardUrl));
				}

				return query.FirstOrDefault(i => requestUrl.Contains(i.OldUrl));
			}
		}

		/// <summary>
		/// Checks for the new URL.
		/// </summary>
		public UrlRedirect CheckNewRedirect(string newUrl)
		{
			if (string.IsNullOrWhiteSpace(newUrl))
			{
				return null;
			}

			using (IProviderSearchContext context = Index.CreateSearchContext())
			{
				return context.GetQueryable<UrlRedirect>().Where(IsRedirect<UrlRedirect>()).FirstOrDefault(x => x.NewUrl == newUrl);
			}
		}

		/// <summary>
		/// Checks for the old url.
		/// </summary>
		public UrlRedirect CheckOldRedirect(string oldUrl)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				return null;
			}

			using (IProviderSearchContext context = Index.CreateSearchContext())
			{
				return context.GetQueryable<UrlRedirect>().Where(IsRedirect<UrlRedirect>()).FirstOrDefault(x => x.OriginalUrlString == oldUrl);
			}
		}

		#region infrastructure


		private static Expression<Func<T, bool>> IsRedirect<T>() where T : SearchResultItem
		{
			return searchResultItem => searchResultItem.TemplateId == Constants.Ids.RedirectItemTemplateId
									   && searchResultItem.Paths.Contains(Constants.Ids.RedirectBucketItemId);
		}
		#endregion

	}
}

