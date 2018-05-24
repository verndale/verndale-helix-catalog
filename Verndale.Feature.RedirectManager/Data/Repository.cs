using System;
using System.Collections.Generic;
using System.Linq;

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
			using (DataContext db = BuildRedirectContext())
			{
				return db.UrlRedirects.ToList();
			}
		}

		/// <summary>
		/// Finds a redirect by id.
		/// </summary>
		public static UrlRedirect GetById(int id)
		{
			using (DataContext db = BuildRedirectContext())
			{
				return db.UrlRedirects.Find(id);
			}
		}

		/// <summary>
		/// Deletes a redirect by ID.
		/// </summary>
		public static void Delete(int id)
		{
			using (DataContext db = BuildRedirectContext())
			{
				var entity = db.UrlRedirects.Find(id);
				if (entity != null)
				{
					db.UrlRedirects.Remove(entity);
					db.SaveChanges();
				}
			}
		}

		/// <summary>
		/// Deletes a redirect by ID.
		/// </summary>
		public static void Delete(string id)
		{
			int idInt = -1;
			if (!int.TryParse(id, out idInt))
			{
				throw new FormatException(string.Format("Value '{0}' for parameter 'id' could not be converted to an integer.", id));
			}

			Delete(idInt);
		}

		/// <summary>
		/// Deletes all redirects.
		/// </summary>
		public static void DeleteAll()
		{
			using (DataContext db = BuildRedirectContext())
			{
				db.UrlRedirects.RemoveRange(db.UrlRedirects);
				db.SaveChanges();
			}
		}

		/// <summary>
		/// Inserts a new redirect.
		/// </summary>
		public static int Insert(string oldUrl, string newUrl, int type)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				throw new ArgumentException("oldUrl");
			}

			if (string.IsNullOrWhiteSpace(newUrl))
			{
				throw new ArgumentException("newUrl");
			}

			var entity = new UrlRedirect()
			{
				OldUrl = oldUrl,
				NewUrl = newUrl,
				RedirectType = type
			};

			using (DataContext db = BuildRedirectContext())
			{
				db.UrlRedirects.Add(entity);
				db.SaveChanges();
			}

			return entity.Id;
		}

		/// <summary>
		/// Updates an existing redirect.
		/// </summary>
		public static void Update(int id, string oldUrl, string newUrl, int? type)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				throw new ArgumentException("oldUrl");
			}

			if (string.IsNullOrWhiteSpace(newUrl))
			{
				throw new ArgumentException("newUrl");
			}

			using (DataContext db = BuildRedirectContext())
			{
				var entity = db.UrlRedirects.Find(id);
				if (entity != null)
				{
					entity.OldUrl = oldUrl;
					entity.NewUrl = newUrl;
					entity.RedirectType = type;

					db.SaveChanges();
				}
			}
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

			using (DataContext db = BuildRedirectContext())
			{
				return db.UrlRedirects.Any(x => x.OldUrl == oldUrl);
			}
		}

		/// <summary>
		/// Checks if the old url exists.
		/// </summary>
		public static bool CheckUrlExists(int id, string oldUrl)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				return false;
			}

			using (DataContext db = BuildRedirectContext())
			{
				return db.UrlRedirects.Any(x => x.Id != id && x.OldUrl == oldUrl);
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

			using (DataContext db = BuildRedirectContext())
			{
				UrlRedirect redirect = null;

				if (performEndsWithWildcard)
				{
					// perform a SQL like. Swap the * in the oldURL database with a SQL wildcard %

					/*                     
					   SELECT *
						FROM [dbo].[application_UrlRedirects] u
						where @RequestURL Like REPLACE(u.OldUrl, '*', '%') 
					 */

					// Execute SQL, and work in memory.                
					List<UrlRedirect> potentialResults = db.UrlRedirects
						.Where(sqlRow => requestUrl.Contains(sqlRow.OldUrl.Replace("*", "")))
						.ToList();

					// Ensure the redirect ends with wildcard.
					redirect = potentialResults.FirstOrDefault(r => r.OldUrl.EndsWith("*"));
				}
				else
				{
					redirect = db.UrlRedirects.FirstOrDefault(x => x.OldUrl == requestUrl);
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

			using (DataContext db = BuildRedirectContext())
			{
				return db.UrlRedirects.FirstOrDefault(x => x.NewUrl == newUrl);
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

			using (DataContext db = BuildRedirectContext())
			{
				return db.UrlRedirects.FirstOrDefault(x => x.OldUrl == oldUrl);
			}
		}

		#region infrastructure

		/// <summary>
		/// Builds the redirect SQL Entities context.
		/// </summary>
		private static DataContext BuildRedirectContext()
		{
			return new DataContext();
		}

		#endregion

	}
}
