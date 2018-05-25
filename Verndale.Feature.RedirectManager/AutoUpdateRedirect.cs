using System;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.Publishing.Pipelines.PublishItem;
using Verndale.Feature.Redirects.Data;

namespace Verndale.Feature.Redirects
{
	public class AutoUpdateRedirect
	{
		enum EventType
		{
			Rename,
			Move,
			Delete
		};

		/// <summary>
		/// Handles Item Rename, Move and Delete events 
		/// The URLRedirect table is updaed based on the event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public void OnItemModified(object sender, EventArgs args)
		{
			EventType updateType;
			var theArgs = args as ItemProcessingEventArgs;

			var currentItem = theArgs.Context.PublishHelper.GetSourceItem(theArgs.Context.ItemId);

			//Checks if the item is not null and has presentation 
			if (currentItem != null && currentItem.Visualization.Layout != null)
			{
				var web = Factory.GetDatabase("web");
				var oldItem = web.GetItem(currentItem.ID);
				if (oldItem != null)
				{
					//Check if there any change in name (Item Rename) 
					//if so , then update the Redirect manager with the new  details
					if (String.CompareOrdinal(oldItem.Name, currentItem.Name) != 0)
					{
						updateType = EventType.Rename;
						UpdateUrlRedirect(oldItem, currentItem, updateType);
					}
					//Check if there any change in Path (Item Moved)  
					else if (String.CompareOrdinal(oldItem.Paths.Path, currentItem.Paths.Path) != 0)
					{
						updateType = EventType.Move;
						UpdateUrlRedirect(oldItem, currentItem, updateType);
					}
				}
			}
			if (currentItem == null)
			{
				//Checks if the item is present in the Web database
				var web = Factory.GetDatabase("web");
				var oldItem = web.GetItem(theArgs.Context.ItemId);

				// In this case, the item is deleted from the master DB 
				//Remove the entries from the Redirect manager table
				if (oldItem != null)
				{
					updateType = EventType.Delete;
					UpdateUrlRedirect(oldItem, null, updateType);
				}
			}
		}

		/// <summary>
		/// Update the Redirect Manager database with the new changes
		/// If the item is Renamed or Moved , then curresponding entries are updated
		/// If the item is deleted, then the entry is removed form the table 
		/// </summary>
		/// <param name="oldItem">Old Item</param>
		/// <param name="newItem">New Item</param>
		/// <param name="updateType">Update type</param>
		///
		///
		// TODO: Replace this with a Rule Action
		private void UpdateUrlRedirect(Item oldItem, Item newItem, EventType updateType)
		{

			var redirect = Repository.CheckNewRedirect(LinkManager.GetItemUrl(oldItem));

			if ((updateType == EventType.Rename && Settings.GetSetting("Verndale.Feature.RedirectsEnableAutoRedirect.Rename") == "true")
				|| (updateType == EventType.Move && Settings.GetSetting("Verndale.Feature.RedirectsEnableAutoRedirect.Move") == "true"))
			{
				// If an item was renamed
				// - if this item url was present in the new url column of a redirect - update redirect
				// - else - add new redirect
				if (redirect != null)
					Repository.Update(redirect.Id, redirect.OldUrl, LinkManager.GetItemUrl(newItem), redirect.RedirectType);
				else
					Repository.Insert(LinkManager.GetItemUrl(oldItem), LinkManager.GetItemUrl(newItem), 301);
			}
			else if (updateType == EventType.Delete && Settings.GetSetting("Verndale.Feature.RedirectsEnableAutoRedirect.Delete") == "true")
			{
				// An entry is added to redirect manager with old path and new path set to site Home
				// Also if there is any existing redirect to this item, the new url for this redirect is set to home
				if (redirect != null)
					Repository.Update(redirect.Id, redirect.OldUrl, "/", redirect.RedirectType);
				else
					Repository.Insert(LinkManager.GetItemUrl(oldItem), "/", 301);
			}
		}
	}
}