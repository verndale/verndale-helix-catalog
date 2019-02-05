using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Sitecore.Controls;
using Sitecore.Diagnostics;
using Sitecore.Web.UI.XamlSharp.Xaml;
using Verndale.Feature.Redirects.Data;

namespace Verndale.Feature.Redirects.Dialogs
{
	public class ExportPage : DialogPage
	{
		protected Button btndownload;
		protected Label lblSuccessMessage;

		private Repository _repository;
		protected Repository Repository
		{
			get
			{
				if (_repository == null)
				{
					_repository = new Repository("sitecore_master_index");
				}

				return _repository;
			}
		}




		protected override void OnLoad(EventArgs e)
		{
			Assert.ArgumentNotNull(e, "e");
			base.OnLoad(e);
			if (!XamlControl.AjaxScriptManager.IsEvent)
			{
				btndownload.Click += new EventHandler(btndownload_Click);
			}
		}
		protected void btndownload_Click(object sender, EventArgs e)
		{
			var allRecords = Repository.GetAll();

			if (allRecords.Any())
			{
				StringBuilder csvHeader = new StringBuilder();
				StringBuilder csv = new StringBuilder(10 * allRecords.Count * 3);

				for (int recordCount = 0; recordCount < allRecords.Count; recordCount++)
				{
					UrlRedirect urlRedirect = allRecords[recordCount];
					StringBuilder csvRow = new StringBuilder(10 * allRecords.Count() * 3);

					for (int c = 0; c < 4; c++)
					{
						object columnValue;

						if (c != 0)
							csvRow.Append(",");

						switch (c)
						{
							case 0:
								columnValue = urlRedirect.SiteName;
								break;
							case 1:
								columnValue = urlRedirect.OldUrl;
								break;
							case 2:
								columnValue = urlRedirect.NewUrl;
								break;
							default:
								columnValue = urlRedirect.IsPermanent ? "301" : "302";
								break;
						}
						if (columnValue == null)
							csvRow.Append("");
						else
						{
							string columnStringValue = columnValue.ToString();
							string cleanedColumnValue = CleanCSVString(columnStringValue);
							csvRow.Append(cleanedColumnValue);
						}
					}
					csv.AppendLine(csvRow.ToString());
				}

				HttpContext context = HttpContext.Current;
				context.Response.Clear();

				context.Response.Write(csvHeader);
				context.Response.Write(csv.ToString());
				context.Response.Write(Environment.NewLine);

				context.Response.ContentType = "text/csv";
				context.Response.AppendHeader("Content-Disposition", "attachment; filename=" + "RedirectUrls" + ".csv");
				context.Response.End();
			}
			else
			{
				this.btndownload.Visible = false;
				lblSuccessMessage.Text = "No Records found to Export Redirects.";
				lblSuccessMessage.Visible = true;
			}

		}


		protected string CleanCSVString(string input)
		{
			string output = "\"" + input.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\r", " ").Replace("\n", "") + "\"";
			return output;
		}
	}
}

