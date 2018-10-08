using System;
using System.Collections.Generic;
using System.Linq;
using Constellation.Foundation.ModelMapping;
using Sitecore;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Extensions;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;

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
		        var search = context.GetQueryable<UrlRedirect>().Where(x => x.TemplateId == Constants.Ids.RedirectItemTemplateId && x.Paths.Contains(Constants.Ids.RedirectBucketItemId));
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
		public static void Update(string id, string siteName, string oldUrl, string newUrl, bool type)
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
		        return context.GetQueryable<UrlRedirect>().Any(x => x.TemplateId == Constants.Ids.RedirectItemTemplateId && x.Paths.Contains(Constants.Ids.RedirectBucketItemId) && x.OldUrl == oldUrl);
		    }
		}

		/// <summary>
		/// Checks if the old url exists.
		/// </summary>
		public static bool CheckUrlExists(string id, string oldUrl)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				return false;
			}
            ID itemId = new ID(id);
		    ISearchIndex index = BuildSearchIndex();
		    using (IProviderSearchContext context = index.CreateSearchContext())
		    {
		        return context.GetQueryable<UrlRedirect>().Any(x => x.TemplateId == Constants.Ids.RedirectItemTemplateId && x.Paths.Contains(Constants.Ids.RedirectBucketItemId) && x.ItemId!=itemId && x.OldUrl == oldUrl);
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
				        .Where(x => x.TemplateId == Constants.Ids.RedirectItemTemplateId
				                    && x.Paths.Contains(Constants.Ids.RedirectBucketItemId)
				                    && x.OldUrl.EndsWith("*")).ToList();

				    redirect = potentialResults.FirstOrDefault(x => requestUrl.StartsWith(x.NoWildCardUrl));
				}
				else
				{
					redirect = context.GetQueryable<UrlRedirect>()
                        .FirstOrDefault(x => x.TemplateId == Constants.Ids.RedirectItemTemplateId 
                        && x.Paths.Contains(Constants.Ids.RedirectBucketItemId) 
                        && x.OldUrl == requestUrl);
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
		        return context.GetQueryable<UrlRedirect>().FirstOrDefault(x => x.TemplateId == Constants.Ids.RedirectItemTemplateId && x.Paths.Contains(Constants.Ids.RedirectBucketItemId) && x.NewUrl == newUrl);
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
		        return context.GetQueryable<UrlRedirect>().FirstOrDefault(x => x.TemplateId == Constants.Ids.RedirectItemTemplateId && x.Paths.Contains(Constants.Ids.RedirectBucketItemId) && x.OldUrl == oldUrl);
		    }
		}

		#region infrastructure

	    /// <summary>
	    /// Builds the search index.
	    /// </summary>
	    public static ISearchIndex BuildSearchIndex()
	    {
	        return ContentSearchManager.GetIndex("sitecore_master_index");
	    }

        #endregion

    }
}

