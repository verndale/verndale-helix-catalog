using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Verndale.Feature.Redirects.Data
{
	[Table("UrlRedirects")]
	public class UrlRedirect
	{
		public int Id { get; set; }

		[Index]
		[StringLength(450)]
		public string OldUrl { get; set; }

		public string NewUrl { get; set; }

		public int? RedirectType { get; set; }
	}
}
