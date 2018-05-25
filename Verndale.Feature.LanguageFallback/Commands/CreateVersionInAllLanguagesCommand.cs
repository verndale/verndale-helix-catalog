using System.Collections.Specialized;
using System.Text;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace Verndale.Feature.LanguageFallback.Commands
{
	/// <summary>
	/// CreateVersionInAllLanguages implements Sitecore Command
	/// it is registered in the Commands.config in item:addversiontoalllanguages
	/// add it is added to the Versions tab in the Sitecore content editor ribbon.
	/// Upon click of the button, this will first prompt the user to verify they want to add all language versions, 
	/// and then will loop through all Languages in the system 
	/// and if the current item does not yet have a version in that language, will add it.
	/// </summary>
	public class CreateVersionInAllLanguagesCommand : Command
	{
		public override void Execute(CommandContext context)
		{
			Assert.ArgumentNotNull(context, "context");

			if (context.Items.Length == 1)
			{
				NameValueCollection parameters = new NameValueCollection();
				parameters["items"] = base.SerializeItems(context.Items);
				Context.ClientPage.Start(this, "Run", parameters);
			}
		}

		protected void Run(ClientPipelineArgs args)
		{
			Item item = base.DeserializeItems(args.Parameters["items"])[0];
			if (item == null)
			{
				return;
			}

			if (SheerResponse.CheckModified())
			{
				//prompt user they want to add versions
				if (args.IsPostBack)
				{
					if (args.Result == "yes")
					{
						LanguageHelper.CreateVersionInEachLanguage(item);

						Sitecore.Web.UI.HtmlControls.DataContext contentEditorDataContext = Sitecore.Context.ClientPage.FindControl("ContentEditorDataContext") as Sitecore.Web.UI.HtmlControls.DataContext;
						contentEditorDataContext.SetFolder(item.Uri);
					}
				}
				else
				{
					StringBuilder builder = new StringBuilder();
					builder.Append("Create version for each language?");
					SheerResponse.Confirm(builder.ToString());
					args.WaitForPostBack();
				}
			}
		}
	}
}
