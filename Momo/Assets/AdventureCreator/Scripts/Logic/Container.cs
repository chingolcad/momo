/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Container.cs"
 * 
 *	This script is used to store a set of
 *	Inventory items in the scene, to be
 *	either taken or added to by the player.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component that is used to store a local set of inventory items within a scene.
	 * The items stored here are separate to those held by the player, who can retrieve or place items in here for safe-keeping.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_container.html")]
	public class Container : MonoBehaviour
	{

		#region Variables

		/** The list of inventory items held by the Container */
		public List<ContainerItem> items = new List<ContainerItem>();
		/** If True, only inventory items (InvItem) with a specific category will be displayed */
		public bool limitToCategory;
		/** The category IDs to limit the display of inventory items by, if limitToCategory = True */
		public List<int> categoryIDs = new List<int>();
		/** If > 0, the maximum number of item slots the Container can hold */
		public int maxSlots = 0;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			RemoveWrongItems ();
		}

		#endregion


		#region PublicFunctions

		/**
		 * Activates the Container.  If a Menu with an appearType = AppearType.OnContainer, it will be enabled and show the Container's contents.
		 */
		public void Interact ()
		{
			KickStarter.playerInput.activeContainer = this;
		}


		/**
		 * <summary>Adds an inventory item to the Container's contents.</summary>
		 * <param name = "containerItem">A data class with information about the inventory item to add</param>
		 * <returns>True if the addition was succesful</returns>
		 */
		public bool Add (ContainerItem containerItem)
		{
			if (containerItem != null && !containerItem.IsEmpty)
			{
				return Add (containerItem.linkedID, containerItem.count);
			}
			return false;
		}


		/**
		 * <summary>Adds an inventory item to the Container's contents.</summary>
		 * <param name = "_id">The ID number of the InvItem to add</param>
		 * <param name = "amount">How many instances of the inventory item to add</param>
		 * <returns>True if the addition was succesful</returns>
		 */
		public bool Add (int _id, int amount)
		{
			InvItem itemToAdd = KickStarter.inventoryManager.GetItem (_id);
			if (itemToAdd != null)
			{
				if (itemToAdd.canCarryMultiple && !itemToAdd.useSeparateSlots)
				{
					// Raise "count" by amount for appropriate ID
					foreach (ContainerItem containerItem in items)
					{
						if (containerItem != null && containerItem.linkedID == _id)
						{
							containerItem.count += amount;
							PlayerMenus.ResetInventoryBoxes ();
							return true;
						}
					}
				}

				// Not already carrying the item
				if (limitToCategory && !categoryIDs.Contains (itemToAdd.binID))
				{
					return false;
				}

				if (!itemToAdd.canCarryMultiple)
				{
					amount = 1;
				}

				ContainerItem containerItemToAdd = new ContainerItem (_id, amount, GetIDArray ());
				if (KickStarter.settingsManager.canReorderItems)
				{
					// Try to find empty slot
					for (int i=0; i<items.Count; i++)
					{
						if (items[i].IsEmpty)
						{
							items[i] = containerItemToAdd;
							PlayerMenus.ResetInventoryBoxes ();

							return true;
						}
					}
				}

				items.Add (containerItemToAdd);
				PlayerMenus.ResetInventoryBoxes ();

				return true;
			}
			return false;
		}


		/**
		 * <summary>Removes an inventory item from the Container's contents.</summary>
		 * <param name = "containerItem">A data class with information about the inventory item to remove</param>
		 * <param name = "amount">If >0, then that many instances of the inventory item will be removed.</param>
		 */
		public void Remove (ContainerItem containerItem, int amount = -1)
		{
			if (containerItem != null)
			{
				for (int i=0; i<items.Count; i++)
				{
					if (items[i] == containerItem)
					{
						if (amount < 0)
						{
							containerItem.count = 0;
						}
						else if (containerItem.count > 0)
						{
							containerItem.count -= amount;
						}

						if (containerItem.count < 1)
						{
							if (KickStarter.settingsManager.canReorderItems && i < (items.Count - 1))
							{
								containerItem.IsEmpty = true;
							}
							else
							{
								items.Remove (containerItem);
							}

							PlayerMenus.ResetInventoryBoxes ();
						}
					}
				}
			}
		}


		/**
		 * <summary>Removes an inventory item from the Container's contents.</summary>
		 * <param name = "_id">The ID number of the InvItem to remove</param>
		 * <param name = "amount">How many instances of the inventory item to remove</param>
		 * <param name = "removeAllInstances">If True, then all instances of the item will be removed. If False, only the first-found will be removed</param>
		 */
		public void Remove (int _id, int amount, bool removeAllInstances = false)
		{
			// Reduce "count" by 1 for appropriate ID
			
			for (int i=0; i<items.Count; i++)
			{
				ContainerItem item = items[i];
				if (item != null && item.linkedID == _id)
				{
					if (item.count > 0)
					{
						item.count -= amount;
					}
					if (item.count < 1)
					{
						if (KickStarter.settingsManager.canReorderItems)
						{
							items[i] = null;
						}
						else
						{
							items.Remove (item);
						}
					}

					PlayerMenus.ResetInventoryBoxes ();

					if (!removeAllInstances)
					{
						return;
					}
				}
			}
		}


		/**
		 * <summary>Removes all inventory items from the Container's contents.</summary>
		 */
		public void RemoveAll ()
		{
			items.Clear ();
			PlayerMenus.ResetInventoryBoxes ();
		}


		/**
		 * <summary>Gets the number of instances of a particular inventory item stored within the Container.</summary>
		 * <param name = "_id">The ID number of the InvItem to search for</param>
		 * <returns>The number of instances of the inventory item stored within the Container</returns>
		 */
		public int GetCount (int _id)
		{
			foreach (ContainerItem item in items)
			{
				if (item != null && item.linkedID == _id)
				{
					return item.count;
				}
			}
			return 0;
		}


		/**
		 * <summary>Adds an inventory item to the Container's contents, at a particular index.</summary>
		 * <param name = "_item">The InvItem to place within the Container</param>
		 * <param name = "_index">The index number within the Container's current contents to insert the new item</param>
		 * <param name = "count">If >0, the quantity of the item to be added. Otherwise, the same quantity as _item will be added</param>
		 * <returns>The ContainerItem instance of the added item</returns>
		 */
		public ContainerItem InsertAt (InvItem _item, int _index, int count = 0)
		{
			if (limitToCategory && !categoryIDs.Contains (_item.binID))
			{
				return null;
			}

			ContainerItem newContainerItem = new ContainerItem (_item.id, GetIDArray ());

			if (count > 0)
			{
				newContainerItem.count = count;
			}
			else
			{
				newContainerItem.count = _item.count;
			}
			if (_index < items.Count)
			{
				if (!items[_index].IsEmpty && items[_index].linkedID == _item.id && _item.canCarryMultiple && !_item.useSeparateSlots)
				{
					// Same item in the slot, so just add instead
					newContainerItem.count += items[_index].count;
					items[_index] = newContainerItem;
				}
				else
				{
					if (items[_index].IsEmpty && KickStarter.settingsManager.canReorderItems)
					{
						items[_index] = newContainerItem;
					}
					else
					{
						items.Insert (_index, newContainerItem);
					}
				}
			}
			else
			{
				if (KickStarter.settingsManager.canReorderItems)
				{
					while (items.Count < (_index-1))
					{
						ContainerItem emptySlotItem = new ContainerItem ();
						emptySlotItem.IsEmpty = true;
						items.Add (emptySlotItem);
					}
				}
				items.Add (newContainerItem);
			}

			PlayerMenus.ResetInventoryBoxes ();
			return newContainerItem;
		}


		/**
		 * <summmary>Gets an array of ID numbers of existing ContainerItem classes, so that a unique number can be generated.</summary>
		 * <returns>Gets an array of ID numbers of existing ContainerItem classes</returns>
		 */
		public int[] GetIDArray ()
		{
			List<int> idArray = new List<int>();
			
			foreach (ContainerItem item in items)
			{
				if (!item.IsEmpty)
				{
					idArray.Add (item.id);
				}
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}


		/**
		 * <summary>Gets all instances of a particular Inventory Item stored within the Container</summary>
		 * <param name = "invID">The ID number of the Inventory Item to search for</param>
		 * <returns>An array of all ContainerItems that hold the Inventory Item</returns>
		 */
		public ContainerItem[] GetItemsWithInvID (int invID)
		{
			List<ContainerItem> foundContainerItems = new List<ContainerItem> ();

			if (items != null)
			{
				foreach (ContainerItem item in items)
				{
					if (!item.IsEmpty && item.linkedID == invID)
					{
						foundContainerItems.Add (item);
					}
				}
			}

			return foundContainerItems.ToArray ();
		}


		#if UNITY_EDITOR

		public int GetInventoryReferences (int invID)
		{
			ContainerItem[] foundInstances = GetItemsWithInvID (invID);
			return foundInstances.Length;
		}

		#endif

		#endregion


		#region ProtectedFunctions

		protected void RemoveWrongItems ()
		{
			if (limitToCategory && categoryIDs.Count > 0)
			{
				for (int i=0; i<items.Count; i++)
				{
					if (!items[i].IsEmpty)
					{
						InvItem listedItem = KickStarter.inventoryManager.GetItem (items[i].linkedID);
						if (!categoryIDs.Contains (listedItem.binID))
						{
							if (KickStarter.settingsManager.canReorderItems)
							{
								items[i].IsEmpty = true;
							}
							else
							{
								items.RemoveAt (i);
							}
							i--;
						}
					}
				}
			}

			for (int i=items.Count-1; i>=0; i--)
			{
				if (!items[i].IsEmpty)
				{
					InvItem invItem = KickStarter.inventoryManager.GetItem (items[i].linkedID);
					if (invItem != null && invItem.canCarryMultiple && invItem.useSeparateSlots && items[i].count > 1)
					{
						while (items[i].count > 1)
						{
							ContainerItem newItem = new ContainerItem (items[i].linkedID, 1, GetIDArray ());
							items.Insert (i+1, newItem);
							items[i].count --;
						}
					}
				}
			}

			if (maxSlots > 0 && items.Count > maxSlots)
			{
				items.RemoveRange (maxSlots, items.Count - maxSlots);
			}
		}

		#endregion


		#region GetSet

		/** The total number of items */
		public int Count
		{
			get
			{
				int count = 0;
				foreach (ContainerItem item in items)
				{
					if (!item.IsEmpty)
					{
						count += item.count;
					}
				}
				return count;
			}
		}


		/** The total number of filled slots */
		public int FilledSlots
		{
			get
			{
				int count = 0;
				foreach (ContainerItem item in items)
				{
					if (!item.IsEmpty)
					{
						count ++;
					}
				}
				return count;
			}
		}

		#endregion

	}

}