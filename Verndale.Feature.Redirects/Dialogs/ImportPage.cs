using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using Sitecore;
using Sitecore.Controls;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Verndale.Feature.Redirects.Data;

namespace Verndale.Feature.Redirects.Dialogs
{
	public class ImportPage : DialogPage
	{
		protected FileUpload fileImport;
		protected Button Upload;
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
			//if (!XamlControl.AjaxScriptManager.IsEvent)
			//{
			Upload.Click += new EventHandler(Upload_Click);
			// }
		}

		protected void Upload_Click(object sender, EventArgs e)
		{
			string filename = fileImport.FileName;
			string[] fileExt = filename.Split('.');
			string fileEx = fileExt[fileExt.Length - 1];

			if (fileEx.ToLower() == "csv")
			{
				Stream theStream = fileImport.PostedFile.InputStream;
				using (StreamReader sr = new StreamReader(theStream))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						UrlRedirect urlRedirect = new UrlRedirect();

						var regex = new Regex("(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)");
						var matches = regex.Matches(line);

						var checkOldUrl = Repository.CheckOldRedirect(matches[0].Value.Replace("\"", string.Empty));

						if (checkOldUrl == null && !string.IsNullOrEmpty(matches[0].Value))
						{
							var oldUrl = matches[1].Value.Replace("\"", "");
							var newUrl = matches[2].Value.Replace("\t", "").Replace("\"", "");
							var redirectType = matches[3].Value.Replace("\t", "").Replace("\"", "").Equals("301");
							var siteName = ItemUtil.ProposeValidItemName(matches[0].Value);
							Repository.Insert(siteName, oldUrl, newUrl, MainUtil.GetBool(redirectType, false));
						}
					}
				}

				fileImport.Visible = false;
				Upload.Visible = false;
				lblSuccessMessage.Text = "Your Records has been successfully uploaded.Please click OK to see the Records.";
				lblSuccessMessage.Visible = true;
			}
			else
			{
				fileImport.Visible = false;
				this.Upload.Visible = false;
				lblSuccessMessage.Text = "Please Select the .csv Extension File";
				lblSuccessMessage.Visible = true;
			}
		}
	}
}