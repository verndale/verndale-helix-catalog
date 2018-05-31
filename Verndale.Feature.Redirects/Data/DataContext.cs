using System.Data.Entity;

namespace Verndale.Feature.Redirects.Data
{
	public class DataContext : DbContext
	{
		public DataContext() : base("name=redirect")
		{
		}

		public virtual DbSet<UrlRedirect> UrlRedirects { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{

		}
	}
}
