using System;
using System.Linq;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace Verndale.Feature.LanguageFallback.Commands
{
	/// <summary>
	/// Base class for processing custom multilingual commands for Sitecore templates.
	/// </summary>
	public abstract class MultilingualTemplateCommand : Command
	{
		#region instance variables

		/// <summary>
		/// Gets/sets the item selected by the user.
		/// </summary>
		private Item _selectedItem;

		#endregion

		#region Abstract methods

		/// <summary>
		/// Gets the name of the command.
		/// </summary>
		public abstract string CommandName { get; }

		/// <summary>
		/// Process the command with the given item.
		/// </summary>
		public abstract int Process(Item parentItem);

		#endregion

		#region Sitecore UI Methods

		/// <summary>
		/// Entry point for the Sitecore command.
		/// </summary>
		public override void Execute(CommandContext context)
		{
			Assert.IsNotNull("context", "Context cannot be null.");

			// Store the Sitecore item the user selected before they clicked the command button.
			_selectedItem = context.Items.FirstOrDefault();

			// Validate the selected item.
			string message = string.Empty;
			if (ValidateInput(_selectedItem, out message))
			{
				// Item was validated, show the confirm modal.
				Context.ClientPage.Start(this, "Confirm", context.Parameters);
			}
			else
			{
				// Sitecore item failed validation, show alert message.
				ShowAlert(message);
			}
		}

		/// <summary>
		/// Sitecore UI method that confirms to process
		/// </summary>
		public void Confirm(ClientPipelineArgs args)
		{
			if (!args.IsPostBack)
			{
				// Prompt the user to confirm they want to perform the action.
				SheerResponse.Confirm(
					string.Format("Execute \"{0}\" on\r\n\r\n{1}\r\n\r\nand all descendants?",
						CommandName,
						_selectedItem.Paths.FullPath
					)
				);

				// Pause the UI to force user to respond to the confirmation modal.
				args.WaitForPostBack();
			}
			else
			{
				// Current request is a post back 
				if (!args.Result.Equals("yes", StringComparison.InvariantCultureIgnoreCase))
				{
					// User did not click OK / Yes, so do nothing.
					return;
				}

				// Validate the user selection.
				string message = string.Empty;
				if (!ValidateInput(_selectedItem, out message))
				{
					// Selected sitecore item was not valid, show an alert.
					ShowAlert(message);
				}
				else
				{
					// Trigger the process method in the implementing / concrete command class.
					int changeCount = Process(_selectedItem);

					// Notify the user in the UI that the processing has completed.
					ShowAlert(BuildCompletionMessage(changeCount));
				}
			}
		}

		#endregion

		#region Virtual, Non Abstract Methods

		/// <summary>
		/// Validates the selected Item.
		/// </summary>
		protected virtual bool ValidateInput(Item item, out string message)
		{
			// Validate that the user is logged in.
			if (!Context.User.IsAuthenticated)
			{
				message = "User is not authenticated.";

				return false;
			}

			// Validate that the user is a Sitecore administrator.
			if (Configuration.RequiresAdminstrator && !Context.User.IsAdministrator)
			{
				message = "You must be an administrator to use this feature.";

				return false;
			}

			// Ensure an item was selected.
			if (item == null)
			{
				message = "Please select an item.";

				return false;
			}

			// Make sure the selected item is a template item (i.e. beneath /sitecore/templates).
			if (!item.Paths.FullPath.StartsWith("/sitecore/templates", StringComparison.InvariantCultureIgnoreCase))
			{
				message = "Please select an item beneath /sitecore/templates";

				return false;
			}

			// Successfully validated.
			message = string.Empty;
			return true;
		}

		/// <summary>
		/// Shows an alert Modal in the Sitecore UI.
		/// </summary>
		protected virtual void ShowAlert(string message)
		{
			if (!string.IsNullOrWhiteSpace(message))
			{
				Context.ClientPage.ClientResponse.Alert(message);
			}
		}

		/// <summary>
		/// Sets a Sitecore checkbox field value.
		/// </summary>
		/// <returns>True if the checkbox value CHANGED, otherwise false.</returns>
		protected virtual bool SetCheckboxFieldValue(Item item, ID fieldId, bool newCheckedValue = true)
		{
			bool valueChanged = false;

			if (item == null)
			{
				Log.Warn("Verndale.SharedSource.SitecoreCommands.Fallback.EnableLanguageFallbackOnTemplateFields.SetFieldValue(): item is null, skipping...", this);

				return false;
			}

			CheckboxField field = item.Fields[fieldId];

			if (field == null)
			{
				// Log that the field is missing on the item.
				Log.Warn(string.Format("Verndale.SharedSource.SitecoreCommands.Fallback.EnableLanguageFallbackOnTemplateFields.SetFieldValue(): Item {0} field '{1}' is null. skipping...",
					item.Paths.FullPath,
					fieldId),
				this);

				return false;
			}

			// Track if the value changed.
			valueChanged = (field.Checked != newCheckedValue);

			// Log the changes.
			Log.Info(
				string.Format("Verndale.SharedSource.SitecoreCommands.Fallback.EnableLanguageFallbackOnTemplateFields.SetFieldValue(): Item {0}: Old value: '{1}'; New value: '{2}'; valueChanged: {3}",
					item.Paths.FullPath,
					field.Checked,
					newCheckedValue,
					valueChanged),
				this
			);

			// Only bother to trigger edit mode on the item if we are going to make an actual update!
			if (valueChanged)
			{
				// Disable item security to avoid random security errors. 
				// If the user has access to the button to trigger this command, thats enough of a security check.

				using (new Sitecore.SecurityModel.SecurityDisabler())
				{
					try
					{
						// update the value.
						item.Editing.BeginEdit();

						// update the field value
						field.Checked = newCheckedValue;
					}
					catch (Exception ex)
					{
						// Log error to sitecore log.
						Log.Error(string.Format("Verndale.SharedSource.SitecoreCommands.Fallback.EnableLanguageFallbackOnTemplateFields.SetFieldValue(): Item {0} ERROR: '{1}'",
								item.Paths.FullPath,
								ex.Message),
							ex,
						this);
					}
					finally
					{
						// Ensure that even if there is an error that end edit editing mode on the item.
						item.Editing.EndEdit();
					}

				}
			}

			return valueChanged;
		}

		#endregion

		#region private methods

		/// <summary>
		/// Builds the completion message.
		/// </summary>
		private string BuildCompletionMessage(int changeCount)
		{
			if (changeCount == 1)
			{
				return "Processing completed. One (1) item was updated.";
			}

			return string.Format("Processing completed. {0:N0} items were updated.", changeCount);
		}

		#endregion

		#region Configuration

		/// <summary>
		/// The configuration for the MultilingualTemplateCommands
		/// </summary>
		protected static class Configuration
		{
			/// <summary>
			/// Determines if the user needs to be a sitecore administrator to execute the commands.
			/// </summary>
			public static bool RequiresAdminstrator
			{
				get
				{
					return Sitecore.Configuration.Settings.GetBoolSetting("Verndale.SharedSource.SitecoreCommands.Fallback.MultilingualTemplateCommand.RequiresAdministrator", true);
				}
			}

			/// <summary>
			/// The Item Name settings.
			/// </summary>
			public static class ItemNames
			{
				/// <summary>
				/// Gets the name of the standard values item, e.g. "__Standard Values"
				/// </summary>
				public static string StandardValues
				{
					get
					{
						return Sitecore.Configuration.Settings.GetSetting("Verndale.SharedSource.SitecoreCommands.Fallback.MultilingualTemplateCommand.ItemNames.StandardValues", "__Standard Values");
					}
				}
			}

			/// <summary>
			/// Gets Template IDs from configuration.
			/// </summary>
			public static class TemplateIDs
			{
				/// <summary>
				/// /sitecore/templates/System/Templates/Template field 
				/// </summary>
				public static ID TemplateFieldID
				{
					get
					{
						string settingVal = Sitecore.Configuration.Settings.GetSetting("Verndale.SharedSource.SitecoreCommands.Fallback.MultilingualTemplateCommand.TemplateIDs.Template", "{455A3E98-A627-4B40-8035-E683A0331AC7}");

						return new ID(settingVal);
					}
				}
			}

			/// <summary>
			/// Gets Template Field IDS from settings.
			/// </summary>
			public static class TemplateFieldIds
			{
				/// <summary>
				/// /sitecore/templates/System/Templates/Template field/Data/Enable Shared Language Fallback
				/// </summary>
				public static ID EnableSharedLanguageFallback
				{
					get
					{
						string settingVal = Sitecore.Configuration.Settings.GetSetting("Verndale.SharedSource.SitecoreCommands.Fallback.MultilingualTemplateCommand.TemplateFieldIds.EnableSharedLanguageFallback", "{24CB32F0-E364-4F37-B400-0F2899097B5B}");

						return new ID(settingVal);
					}
				}

				/// <summary>
				/// /sitecore/templates/System/Templates/Sections/Advanced/Advanced/__Enable item fallback
				/// </summary>
				public static ID EnableItemFallback
				{
					get
					{
						string settingVal = Sitecore.Configuration.Settings.GetSetting("Verndale.SharedSource.SitecoreCommands.Fallback.MultilingualTemplateCommand.TemplateFieldIds.EnableItemFallback", "{FD4E2050-186C-4375-8B99-E8A85DD7436E}");

						return new ID(settingVal);
					}
				}

				/// <summary>
				/// /sitecore/templates/System/Templates/Sections/Advanced/Advanced/__Enable item fallback
				/// </summary>
				public static ID EnforceVersionPresence
				{
					get
					{
						string settingVal = Sitecore.Configuration.Settings.GetSetting("Verndale.SharedSource.SitecoreCommands.Fallback.MultilingualTemplateCommand.TemplateFieldIds.EnforceVersionPresence", "{61CF7151-0CBD-4DB4-9738-D753A55A6E65}");

						return new ID(settingVal);
					}
				}
			}
		}

		#endregion
	}
}