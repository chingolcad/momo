/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"RuntimeInventory.cs"
 * 
 *	This script creates a local copy of the InventoryManager's items.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component is where inventory items (see InvItem) are stored at runtime.
	 * When the player aquires an item, it is transferred here (into _localItems) from the InventoryManager asset.
	 * It should be placed on the PersistentEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_runtime_inventory.html")]
	public class RuntimeInventory : MonoBehaviour
	{

		#region Variables

		protected List<InvItem> _localItems = new List<InvItem>();
		/** A List of inventory items (InvItem) being used in the current Recipe being crafted */
		[HideInInspector] public List<InvItem> craftingItems = new List<InvItem>();
		/** The default ActionListAsset to run if an inventory combination is unhandled */
		[HideInInspector] public ActionListAsset unhandledCombine;
		/** The default ActionListAsset to run if using an inventory item on a Hotspot is unhandled */
		[HideInInspector] public ActionListAsset unhandledHotspot;
		/** The default ActionListAsset to run if giving an inventory item to an NPC is unhandled */
		[HideInInspector] public ActionListAsset unhandledGive;

		protected InvItem selectedItem = null;
		protected InvItem lastSelectedItem = null;
		/** The ContainerItem of if the currently-selected item, if it is from a Container */
		public ContainerItem selectedContainerItem { get; protected set; }
		/** The Container of if the currently-selected item, if it is from a Container */
		public Container selectedContainerItemContainer { get; protected set; }
		/** The inventory item that is currently being hovered over by the cursor */
		[HideInInspector] public InvItem hoverItem = null;
		/** The inventory item that is currently being highlighted within an MenuInventoryBox element */
		[HideInInspector] public InvItem highlightItem = null;
		/** If True, then the Hotspot label will show the name of the inventory item that the mouse is hovering over */ 
		[HideInInspector] public bool showHoverLabel = true;
		/** A List of index numbers within a Button's invButtons List that represent inventory interactions currently available to the player */
		[HideInInspector] public List<int> matchingInvInteractions = new List<int>();
		protected List<SelectItemMode> matchingItemModes = new List<SelectItemMode>();

		/** The last inventory item that the player clicked on, in any MenuInventoryBox element type */
		[HideInInspector] public InvItem lastClickedItem;

		protected SelectItemMode selectItemMode = SelectItemMode.Use;
		protected GUIStyle countStyle;
		protected TextEffects countTextEffects;
		
		protected HighlightState highlightState = HighlightState.None;
		protected float pulse = 0f;
		protected int pulseDirection = 0; // 0 = none, 1 = in, -1 = out

		protected string prefix1 = string.Empty;
		protected string prefix2 = string.Empty;

		#endregion


		#region UnityStandards

		protected void OnApplicationQuit ()
		{
			if (KickStarter.inventoryManager != null)
			{
				foreach (InvItem invItem in KickStarter.inventoryManager.items)
				{
					if (invItem.cursorIcon != null)
					{
						invItem.cursorIcon.ClearCache ();
					}
				}
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * Transfers any relevant data from InventoryManager when the game begins or restarts.
		 */
		public void OnStart ()
		{
			SetNull ();
			hoverItem = null;
			showHoverLabel = true;
			
			craftingItems.Clear ();
			_localItems.Clear ();
			GetItemsOnStart ();

			if (KickStarter.inventoryManager)
			{
				unhandledCombine = KickStarter.inventoryManager.unhandledCombine;
				unhandledHotspot = KickStarter.inventoryManager.unhandledHotspot;
				unhandledGive = KickStarter.inventoryManager.unhandledGive;
			}
		}
		

		/**
		 * Initialises the inventory after a scene change. This is called manually by SaveSystem so that the order is correct.
		 */
		public void AfterLoad ()
		{
			if (!KickStarter.settingsManager.IsInLoadingScene () && KickStarter.sceneSettings != null)
			{
				SetNull ();
				lastSelectedItem = null;
			}
		}


		/**
		 * De-selects the active inventory item.
		 */
		public void SetNull ()
		{
			if (selectedItem != null && _localItems.Contains (selectedItem))
			{
				KickStarter.eventManager.Call_OnChangeInventory (selectedItem, InventoryEventType.Deselect);
			}
			selectedItem = null;
			highlightItem = null;
			lastClickedItem = null;
			selectedContainerItem = null;
			selectedContainerItemContainer = null;
			PlayerMenus.ResetInventoryBoxes ();
		}
		

		/**
		 * <summary>Selects an inventory item (InvItem) by referencing its ID number.</summary>
		 * <param name = "_id">The inventory item's ID number</param>
		 * <param name = "_mode">What mode the item is selected in (Use, Give)</param>
		 */
		public void SelectItemByID (int _id, SelectItemMode _mode = SelectItemMode.Use, bool ignoreInventory = false)
		{
			if (_id == -1)
			{
				SetNull ();
				return;
			}

			if (ignoreInventory)
			{
				foreach (InvItem item in KickStarter.inventoryManager.items)
				{
					if (item != null && item.id == _id)
					{
						SetSelectItemMode (_mode);
						lastSelectedItem = selectedItem = new InvItem (item);
						PlayerMenus.ResetInventoryBoxes ();
						KickStarter.eventManager.Call_OnChangeInventory (selectedItem, InventoryEventType.Select);
						return;
					}
				}
				return;
			}

			foreach (InvItem item in _localItems)
			{
				if (item != null && item.id == _id)
				{
					SetSelectItemMode (_mode);
					lastSelectedItem = selectedItem = item;
					selectedContainerItem = null;
					selectedContainerItemContainer = null;
					
					PlayerMenus.ResetInventoryBoxes ();
					KickStarter.eventManager.Call_OnChangeInventory (selectedItem, InventoryEventType.Select);
					return;
				}
			}
			
			SetNull ();
			ACDebug.LogWarning ("Want to select inventory item " + KickStarter.inventoryManager.GetLabel (_id) + " but player is not carrying it.");
		}


		/**
		 * <summary>Re-selects the last-selected inventory item, if available.</summary>
		 */
		public void ReselectLastItem ()
		{
			if (lastSelectedItem != null && localItems.Contains (lastSelectedItem))
			{
				SelectItem (lastSelectedItem, selectItemMode);
			}
		}
		

		/**
		 * <summary>Selects an inventory item (InvItem)</summary>
		 * <param name = "_id">The inventory item to selet</param>
		 * <param name = "_mode">What mode the item is selected in (Use, Give)</param>
		 */
		public void SelectItem (InvItem item, SelectItemMode _mode = SelectItemMode.Use)
		{
			if (item == null)
			{
				SetNull ();
			}
			else if (selectedItem == item)
			{
				SetNull ();
				KickStarter.playerCursor.ResetSelectedCursor ();
			}
			else
			{
				SetSelectItemMode (_mode);
				lastSelectedItem = selectedItem = item;

				selectedContainerItem = null;
				selectedContainerItemContainer = null;

				KickStarter.eventManager.Call_OnChangeInventory (selectedItem, InventoryEventType.Select);
				PlayerMenus.ResetInventoryBoxes ();
			}
		}


		/**
		 * <summary>Selects an inventory item placed in a Container</summary>
		 * <param name = "containerItem">The item to select</param>
		 */
		public void SelectItem (Container container, ContainerItem containerItem)
		{
			SetNull ();
			SelectItemByID (containerItem.linkedID, SelectItemMode.Use, true);
			selectedContainerItemContainer = container;
			selectedContainerItem = containerItem;
			selectedItem.count = containerItem.count;
		}


		/**
		 * <summary>Updates the item selection mode according to a given item</summary>
		 * <param name = "inventoryBox">The menu element containing the item</param>
		 * <param name = "slotIndex">The slot index within the element that the item appears at</param>
		 */
		public void UpdateSelectItemModeForMenu (MenuInventoryBox inventoryBox, int slotIndex)
		{
			int i = slotIndex + inventoryBox.GetOffset ();
			if (selectedItem == null && matchingItemModes != null && i < matchingItemModes.Count)
			{
				SetSelectItemMode (matchingItemModes[i]);
			}
		}
		

		/**
		 * <summary>Forces the item selection mode</param>
		 * <param name = "_mode">The item selection mode to set</param>
		 */
		public void SetSelectItemMode (SelectItemMode _mode)
		{
			if (_mode == SelectItemMode.Give && KickStarter.settingsManager.CanGiveItems ())
			{
				selectItemMode = SelectItemMode.Give;
			}
			else
			{
				selectItemMode = SelectItemMode.Use;
			}
		}
		

		/**
		 * <summary>Checks if the currently-selected item is in "give" mode, as opposed to "use".</summary>
		 * <returns>True if the currently-selected item is in "give" mode, as opposed to "use"</returns>
		 */
		public bool IsGivingItem ()
		{
			return (selectItemMode == SelectItemMode.Give);
		}
		

		/**
		 * <summary>Replaces one inventory item carried by the player with another, retaining its position in its MenuInventoryBox element.</summary>
		 * <param name = "_addID">The ID number of the inventory item (InvItem) to add</param>
		 * <param name = "_removeID">The ID number of the inventory item (InvItem) to remove</param>
		 * <param name = "addAmount">The amount if the new inventory item to add, if the InvItem's canCarryMultiple = True</param>
		 */
		public void Replace (int _addID, int _removeID, int addAmount = 1)
		{
			int _index = -1;
			InvItem removeItem = null;

			foreach (InvItem item in _localItems)
			{
				if (item == null) continue;

				if (item.id == _removeID && _index == -1)
				{
					_index = _localItems.IndexOf (item);
					removeItem = item;
				}

				if (item.id == _addID)
				{
					// Already carrying
					return;
				}
			}

			if (_index == -1)
			{
				// Not carrying
				Add (_addID, addAmount, false, -1);
				return;
			}
			else if (removeItem != null)
			{
				KickStarter.eventManager.Call_OnChangeInventory (removeItem, InventoryEventType.Remove, removeItem.count);
			}

			foreach (InvItem item in KickStarter.inventoryManager.items)
			{
				if (item.id == _addID)
				{
					InvItem newItem = new InvItem (item);
					if (!newItem.canCarryMultiple)
					{
						addAmount = 1;
					}
					newItem.count = addAmount;
					_localItems [_index] = newItem;
					PlayerMenus.ResetInventoryBoxes ();
					KickStarter.eventManager.Call_OnChangeInventory (newItem, InventoryEventType.Add, addAmount);
					return;
				}
			}
		}


		/**
		 * <summary>Adds an inventory item to the player's inventory.</summary>
		 * <param name = "_name">The name of the inventory item (InvItem) to add</param>
		 * <param name = "amount">The amount if the inventory item to add, if the InvItem's canCarryMultiple = True</param>
		 * <param name = "selectAfter">If True, then the inventory item will be automatically selected</param>
		 * <param name = "playerID">The ID number of the Player to receive the item, if multiple Player prefabs are supported. If playerID = -1, the current player will receive the item</param>
		 * <param name = "addToFront">If True, the new item will be added to the front of the inventory</param>
		 */
		public void Add (string _name, int amount = 1, bool selectAfter = false, int playerID = -1, bool addToFront = false)
		{
			if (amount <= 0) return;

			InvItem newItem = KickStarter.inventoryManager.GetItem (_name);
			if (newItem != null)
			{
				Add (newItem.id, amount, selectAfter, playerID, addToFront);
			}
		}


		/**
		 * <summary>Adds an inventory item to the player's inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to add</param>
		 * <param name = "amount">The amount if the inventory item to add, if the InvItem's canCarryMultiple = True</param>
		 * <param name = "selectAfter">If True, then the inventory item will be automatically selected</param>
		 * <param name = "playerID">The ID number of the Player to receive the item, if multiple Player prefabs are supported. If playerID = -1, the current player will receive the item</param>
		 * <param name = "addToFront">If True, the new item will be added to the front of the inventory</param>
		 */
		public void Add (int _id, int amount = 1, bool selectAfter = false, int playerID = -1, bool addToFront = false)
		{
			if (amount <= 0) return;

			if (playerID >= 0 && KickStarter.player.ID != playerID)
			{
				AddToOtherPlayer (_id, amount, playerID, addToFront);
			}
			else
			{
				_localItems = Add (_id, amount, _localItems, selectAfter, addToFront);
				KickStarter.eventManager.Call_OnChangeInventory (GetItem (_id), InventoryEventType.Add, amount);
			}
		}


		/**
		 * <summary>Adds an inventory item to a generic inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to add</param>
		 * <param name = "amount">The amount if the inventory item to add, if the InvItem's canCarryMultiple = True</param>
		 * <param name = "itemList">The list of inventory items to add the new item to</param>
		 * <param name = "selectAfter">If True, then the inventory item will be automatically selected</param>
		 * <param name = "addToFront">If True, the new item will be added to the front of the inventory</param>
		 * <returns>The modified List of inventory items</returns>
		 */
		public List<InvItem> Add (int _id, int amount, List<InvItem> itemList, bool selectAfter, bool addToFront = false)
		{
			itemList = ReorderItems (itemList);
			
			// Raise "count" by 1 for appropriate ID
			foreach (InvItem item in itemList)
			{
				if (item != null && item.id == _id)
				{
					if (item.canCarryMultiple)
					{
						if (item.useSeparateSlots)
						{
							break;
						}
						else
						{
							item.count += amount;
						}
					}
					
					if (selectAfter)
					{
						SelectItem (item, SelectItemMode.Use);
					}

					PlayerMenus.ResetInventoryBoxes ();
					return itemList;
				}
			}

			// Not already carrying the item
			foreach (InvItem assetItem in KickStarter.inventoryManager.items)
			{
				if (assetItem.id == _id)
				{
					InvItem newItem = new InvItem (assetItem);
					if (!newItem.canCarryMultiple)
					{
						amount = 1;
					}
					newItem.recipeSlot = -1;
					newItem.count = amount;
					
					if (KickStarter.settingsManager.canReorderItems)
					{
						if (addToFront && itemList.Count > 0 && itemList[0] != null)
						{
							itemList.Insert (0, newItem);

							if (newItem.canCarryMultiple && newItem.useSeparateSlots)
							{
								int count = newItem.count-1;
								newItem.count = 1;
								for (int j=0; j<count; j++)
								{
									itemList.Insert (0, newItem);
								}
							}

							PlayerMenus.ResetInventoryBoxes ();
							return itemList;	
						}

						// Insert into first "blank" space
						for (int i=0; i<itemList.Count; i++)
						{
							if (itemList[i] == null)
							{
								itemList[i] = newItem;
								if (selectAfter)
								{
									SelectItem (newItem, SelectItemMode.Use);
								}
								
								if (newItem.canCarryMultiple && newItem.useSeparateSlots)
								{
									int count = newItem.count-1;
									newItem.count = 1;
									for (int j=0; j<count; j++)
									{
										itemList.Add (newItem);
									}
								}

								PlayerMenus.ResetInventoryBoxes ();
								return itemList;
							}
						}
					}
					
					if (newItem.canCarryMultiple && newItem.useSeparateSlots)
					{
						int count = newItem.count;
						newItem.count = 1;
						for (int i=0; i<count; i++)
						{
							if (addToFront)
							{
								itemList.Insert (0, newItem);
							}
							else
							{
								itemList.Add (newItem);
							}
						}
					}
					else
					{
						if (addToFront)
						{
							itemList.Insert (0, newItem);
						}
						else
						{
							itemList.Add (newItem);
						}
					}
					
					if (selectAfter)
					{
						SelectItem (newItem, SelectItemMode.Use);
					}

					PlayerMenus.ResetInventoryBoxes ();
					return itemList;
				}
			}

			ACDebug.LogWarning ("Cannot add inventory with ID=" + _id + ", because it cannot be found in the Inventory Manager.");
			
			itemList = RemoveEmptySlots (itemList);
			PlayerMenus.ResetInventoryBoxes ();
			return itemList;
		}


		/**
		 * <summary>Removes an inventory item from the player's inventory. If multiple instances of the item can be held, all instances will be removed.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to remove</param>
		 */
		public void Remove (int _id)
		{
			int count = GetCount (_id);
			if (count > 0)
			{
				_localItems = Remove (_id, count, false, _localItems);
				KickStarter.eventManager.Call_OnChangeInventory (GetItem (_id), InventoryEventType.Remove, count);
			}
		}


		/**
		 * <summary>Removes some instances of an inventory items from the player's inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to remove</param>
		 * <param name = "amount">The amount if the inventory item to remove, if the InvItem's canCarryMultiple = True</param>
		 */
		public void Remove (int _id, int amount)
		{
			int count = GetCount (_id);
			if (count > 0)
			{
				_localItems = Remove (_id, amount, true, _localItems);
				KickStarter.eventManager.Call_OnChangeInventory (GetItem (_id), InventoryEventType.Remove, amount);
			}
		}


		/**
		 * <summary>Removes an inventory item from a player's inventory. If multiple instances of the item can be held, all instances will be removed.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to remove</param>
		 * <param name = "playerID">The ID number of the player to affect, if player-switching is enabled</param>
		 */
		public void RemoveFromOtherPlayer (int _id, int playerID)
		{
			if (playerID >= 0 && KickStarter.player.ID != playerID)
			{
				RemoveFromOtherPlayer (_id, 1, false, playerID);
			}
			else
			{
				Remove (_id);
			}
		}


		/**
		 * <summary>Removes some instances of an inventory item from a player's inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to remove</param>
		 * <param name = "amount">The amount if the inventory item to remove, if the InvItem's canCarryMultiple = True.</param>
		 * <param name = "playerID">The ID number of the player to affect, if player-switching is enabled</param>
		 */
		public void RemoveFromOtherPlayer (int _id, int amount, int playerID)
		{
			if (playerID >= 0 && KickStarter.player.ID != playerID)
			{
				RemoveFromOtherPlayer (_id, amount, true, playerID);
			}
			else
			{
				 Remove (_id, amount);
			}
		}


		/**
		 * <summary>Removes an inventory item from the player's inventory.</summary>
		 * <param name = "_item">The inventory item (InvItem) to remove. Note that this refers to an instance of the item, generated at runtime - not the item stored within the InventoryManager.  This instance is assumed to be present in the player's current inventory, occupying a single slot</param>
		 * <param name = "amount">If >0, then only that quantity will be removed, if the item's canCarryMultiple property is True. Otherwise, the item instance will be removed.</param>
		 */
		public void Remove (InvItem _item, int amount = 0)
		{
			if (_item != null && _localItems.Contains (_item))
			{
				if (_item == selectedItem)
				{
					SetNull ();
				}

				if (amount > 0 && _item.canCarryMultiple && _item.count > amount)
				{
					_item.count -= amount;
				}
				else
				{
					_localItems [_localItems.IndexOf (_item)] = null;
					
					_localItems = ReorderItems (_localItems);
					_localItems = RemoveEmptySlots (_localItems);

					KickStarter.eventManager.Call_OnChangeInventory (_item, InventoryEventType.Remove);
				}

				PlayerMenus.ResetInventoryBoxes ();
			}
		}


		/**
		 * <summary>Removes an inventory item from the player's inventory.</summary>
		 * <param name = "_name">The name of the item to remove</param>
		 * <param name = "amount">If >0, then only that quantity will be removed, if the item's canCarryMultiple property is True. Otherwise, all instances will be removed</param>
		 */
		public void Remove (string _name, int amount = 0)
		{
			InvItem itemToRemove = KickStarter.inventoryManager.GetItem (_name);
			if (itemToRemove != null)
			{
				if (amount > 0)
				{
					Remove (itemToRemove.id, amount);
				}
				else
				{
					Remove (itemToRemove.id, amount);
				}
			}
		}
		

		/**
		 * <summary>Removes all items from the player's inventory</summary>
		 */
		public void RemoveAll ()
		{
			foreach (InvItem invItem in _localItems)
			{
				Remove (invItem);
			}
		}


		/**
		 * <summary>Removes all items in a given category from the player's inventory</summary>
		 * <param name = "categoryID">The ID number of the category</param>
		 */
		public void RemoveAllInCategory (int categoryID)
		{
			for (int i=0; i<_localItems.Count; i++)
			{
				if (_localItems[i].binID == categoryID)
				{
					Remove (_localItems[i]);
					i = -1;
				}
			}
		}


		/**
		 * <summary>Gets the full prefix to a Hotpsot label when an item is selected, e.g. "Use X on " / "Give X to ".</summary>
		 * <param name = "item">The inventory item that is selected</param>
		 * <param name = "itemName">The display name of the inventory item, in the current language</param>
		 * <param name = "languageNumber">The index of the current language, as set in SpeechManager</param>
		 * <param name = "canGive">If True, the the item is assumed to be in "give" mode, as opposed to "use".</param>
		 * <returns>The full prefix to a Hotspot label when the item is selected</returns>
		 */
		public string GetHotspotPrefixLabel (InvItem item, string itemName, int languageNumber, bool canGive = false)
		{
			prefix1 = string.Empty;
			prefix2 = string.Empty;
			
			if (canGive && IsGivingItem ())
			{
				prefix1 = KickStarter.runtimeLanguages.GetTranslation (KickStarter.cursorManager.hotspotPrefix3.label, KickStarter.cursorManager.hotspotPrefix3.lineID, languageNumber, KickStarter.cursorManager.hotspotPrefix3.GetTranslationType (0));
				prefix2 = KickStarter.runtimeLanguages.GetTranslation (KickStarter.cursorManager.hotspotPrefix4.label, KickStarter.cursorManager.hotspotPrefix4.lineID, languageNumber, KickStarter.cursorManager.hotspotPrefix4.GetTranslationType (0));
			}
			else
			{
				if (item != null && item.overrideUseSyntax)
				{
					prefix1 = KickStarter.runtimeLanguages.GetTranslation (item.hotspotPrefix1.label, item.hotspotPrefix1.lineID, languageNumber, item.hotspotPrefix1.GetTranslationType (0));
					prefix2 = KickStarter.runtimeLanguages.GetTranslation (item.hotspotPrefix2.label, item.hotspotPrefix2.lineID, languageNumber, item.hotspotPrefix2.GetTranslationType (0));
				}
				else
				{
					prefix1 = KickStarter.runtimeLanguages.GetTranslation (KickStarter.cursorManager.hotspotPrefix1.label, KickStarter.cursorManager.hotspotPrefix1.lineID, languageNumber, KickStarter.cursorManager.hotspotPrefix1.GetTranslationType (0));
					prefix2 = KickStarter.runtimeLanguages.GetTranslation (KickStarter.cursorManager.hotspotPrefix2.label, KickStarter.cursorManager.hotspotPrefix2.lineID, languageNumber, KickStarter.cursorManager.hotspotPrefix2.GetTranslationType (0));
				}
			}

			if (string.IsNullOrEmpty (prefix1) && !string.IsNullOrEmpty (prefix2))
			{
				return prefix2;
			}
			if (!string.IsNullOrEmpty (prefix1) && string.IsNullOrEmpty (prefix2))
			{
				return AdvGame.CombineLanguageString (prefix1, itemName, languageNumber);
			}
			if (prefix1 == " " && !string.IsNullOrEmpty (prefix2))
			{
				return AdvGame.CombineLanguageString (itemName, prefix2, languageNumber);
			}

			if (KickStarter.runtimeLanguages.LanguageReadsRightToLeft (languageNumber))
			{
				return (prefix2 + " " + itemName + " " + prefix1);
			}
			else
			{
				return (prefix1 + " " + itemName + " " + prefix2);
			}
		}


		/**
		 * <summary>Removes the null entries from the end of a list of inventory items</summary>
		 * <param name = "itemList">The list of inventory items</param>
		 * <returns>The list, with null entries from the end removed</returns>
		 */
		public List<InvItem> RemoveEmptySlots (List<InvItem> itemList)
		{
			// Remove empty slots on end
			for (int i=itemList.Count-1; i>=0; i--)
			{
				if (itemList[i] == null)
				{
					itemList.RemoveAt (i);
				}
				else
				{
					return itemList;
				}
			}
			return itemList;
		}
		

		/**
		 * <summary>Gets an inventory item's display name.</summary>
		 * <param name = "item">The inventory item to get the display name of</param>
		 * <param name = "languageNumber">The index of the current language, as set in SpeechManager</param>
		 * <returns>The inventory item's display name</returns>
		 */
		public string GetLabel (InvItem item, int languageNumber)
		{
			return item.GetLabel (languageNumber);
		}
		

		/**
		 * <summary>Gets the amount of a particular inventory item within the player's inventory.</summary>
		 * <param name = "_invID">The ID number of the inventory item (InvItem) in question</param>
		 * <returns>The amount of the inventory item within the player's inventory.</returns>
		 */
		public int GetCount (int _invID)
		{
			int count = 0;
			for (int i=0; i<_localItems.Count; i++)
			{
				if (_localItems[i] != null && _localItems[i].id == _invID)
				{
					count += _localItems[i].count;
				}
			}
			
			return count;
		}
		

		/**
		 * <summary>Gets the amount of a particular inventory item within any player's inventory, if multiple Player prefabs are supported.</summary>
		 * <param name = "_invID">The ID number of the inventory item (InvItem) in question</param>
		 * <param name = "playerID">The ID number of the Player to refer to</param>
		 * <returns>The amount of the inventory item within the player's inventory.</returns>
		 */
		public int GetCount (int _invID, int _playerID)
		{
			List<InvItem> otherPlayerItems = GetComponent <SaveSystem>().GetItemsFromPlayer (_playerID);
			int count = 0;

			if (otherPlayerItems != null)
			{
				foreach (InvItem item in otherPlayerItems)
				{
					if (item != null && item.id == _invID)
					{
						count += item.count;
					}
				}
			}
			return count;
		}


		/**
		 * <summary>Gets the total number of inventory items currently held by the active Player.</summary>
		 * <returns>The total number of inventory items currently held by the active Player</returns>
		 */
		public int GetNumberOfItemsCarried (bool includeMultipleInSameSlot = false)
		{
			return GetNumberOfItemsCarriedInCategory (-1, includeMultipleInSameSlot);
		}


		/**
		 * <summary>Gets the total number of inventory items currently held by a given Player, if multiple Players are supported.</summary>
		 * <param name = "playerID">The ID number of the Player to refer to</param>
		 * <returns>The total number of inventory items currently held by the given Player</returns>
		 */
		public int GetNumberOfItemsCarried (int _playerID, bool includeMultipleInSameSlot = false)
		{
			return GetNumberOfItemsCarriedInCategory (-1, _playerID, includeMultipleInSameSlot);
		}


		/**
		 * <summary>Gets the total number of inventory items currently held by the active Player.</summary>
		 * <param name = "categoryID">If >=0, then only items placed in the category with that ID will be counted</param>
		 * <returns>The total number of inventory items currently held by the active Player</returns>
		 */
		public int GetNumberOfItemsCarriedInCategory (int categoryID, bool includeMultipleInSameSlot = false)
		{
			int numCarried = 0;
			for (int i=0; i<_localItems.Count; i++)
			{
				if (_localItems[i] != null)
				{
					if (categoryID < 0 || _localItems[i].binID == categoryID)
					{
						if (includeMultipleInSameSlot && _localItems[i].canCarryMultiple)
						{
							numCarried += _localItems[i].count;
						}
						else
						{
							numCarried ++;
						}
					}
				}
			}
			return numCarried;
		}


		/**
		 * <summary>Gets the total number of inventory items currently held by a given Player, if multiple Players are supported.</summary>
		 * <param name = "categoryID">If >=0, then only items placed in the category with that ID will be counted</param>
		 * <param name = "playerID">The ID number of the Player to refer to</param>
		 * <returns>The total number of inventory items currently held by the given Player</returns>
		 */
		public int GetNumberOfItemsCarriedInCategory (int categoryID, int _playerID, bool includeMultipleInSameSlot = false)
		{
			int numCarried = 0;

			List<InvItem> otherPlayerItems = GetComponent <SaveSystem>().GetItemsFromPlayer (_playerID);
			if (otherPlayerItems != null)
			{
				for (int i=0; i<otherPlayerItems.Count; i++)
				{
					if (otherPlayerItems[i] != null)
					{
						if (includeMultipleInSameSlot && otherPlayerItems[i].canCarryMultiple)
						{
							numCarried += otherPlayerItems[i].count;
						}
						else
						{
							numCarried ++;
						}
					}
				}
			}

			return numCarried;
		}
		

		/**
		 * <summary>Gets an inventory item within the current Recipe being crafted.</summary>
		 * <param name "_id">The ID number of the inventory item</param>
		 * <returns>The inventory item, if it is within the current Recipe being crafted</returns>
		 */
		public InvItem GetCraftingItem (int _id)
		{
			foreach (InvItem item in craftingItems)
			{
				if (item.id == _id)
				{
					return item;
				}
			}
			
			return null;
		}


		/**
		 * <summary>Gets an inventory item within the player's current inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item</param>
		 * <returns>The inventory item, if it is held by the player</returns>
		 */
		public InvItem GetItem (int _id)
		{
			foreach (InvItem item in _localItems)
			{
				if (item != null && item.id == _id)
				{
					return item;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the first-found instance of an inventory item within the player's current inventory.</summary>
		 * <param name = "_name">The name of the InvItem to find</param>
		 * <returns>The inventory item, if it is held by the player</returns>
		 */
		public InvItem GetItem (string _name)
		{
			foreach (InvItem item in _localItems)
			{
				if (item.label == _name)
				{
					return item;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets all instances of an inventory item within the player's current inventory.</summary>
		 * <param name = "_id">The ID number of the InvItem to find</param>
		 * <returns>All instances of the inventory item</returns>
		 */
		public InvItem[] GetItems (int _id)
		{
			List<InvItem> foundItems = new List<InvItem>();
			foreach (InvItem item in _localItems)
			{
				if (item.id == _id)
				{
					foundItems.Add (item);
				}
			}
			return foundItems.ToArray ();
		}


		/**
		 * <summary>Gets all instances of an inventory item within the player's current inventory.</summary>
		 * <param name = "_name">The name of the InvItem to find</param>
		 * <returns>All instances of the inventory item</returns>
		 */
		public InvItem[] GetItems (string _name)
		{
			List<InvItem> foundItems = new List<InvItem>();
			foreach (InvItem item in _localItems)
			{
				if (item.label == _name)
				{
					foundItems.Add (item);
				}
			}
			return foundItems.ToArray ();
		}


		/**
		 * <summary>Checks if an inventory item is within the player's current inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item</param>
		 * <returns>True if the inventory item is within the player's current inventory</returns>
		 */
		public bool IsCarryingItem (int _id)
		{
			foreach (InvItem item in _localItems)
			{
				if (item != null && item.id == _id)
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Runs an inventory item's "Examine" interaction.</summary>
		 * <param name = "item">The inventory item to examine</param>
		 */
		public void Look (InvItem item)
		{
			if (item == null || item.recipeSlot > -1) return;
			if (item.lookActionList)
			{
				KickStarter.eventManager.Call_OnUseInventory (item, KickStarter.cursorManager.lookCursor_ID);
				AdvGame.RunActionListAsset (item.lookActionList);
			}
		}
		

		/**
		 * <summary>Runs an inventory item's "Use" interaction.</summary>
		 * <param name ="item">The inventory item to use</param>
		 */
		public void Use (InvItem item)
		{
			if (item == null || item.recipeSlot > -1) return;

			if (item.useActionList)
			{
				SetNull ();
				KickStarter.eventManager.Call_OnUseInventory (item, 0);
				AdvGame.RunActionListAsset (item.useActionList);
			}
			else if (KickStarter.settingsManager.CanSelectItems (true))
			{
				SelectItem (item, SelectItemMode.Use);
			}
		}
		

		/**
		 * <summary>Runs an inventory item's interaction, when multiple "use" interactions are defined.</summary>
		 * <param name = "invItem">The relevant inventory item</param>
		 * <param name = "iconID">The ID number of the interaction's icon, defined in CursorManager</param>
		 */
		public void RunInteraction (InvItem invItem, int iconID)
		{
			if (KickStarter.stateHandler.gameState == GameState.DialogOptions && !KickStarter.settingsManager.allowInventoryInteractionsDuringConversations && !KickStarter.settingsManager.allowGameplayDuringConversations)
			{
				return;
			}

			if (invItem == null || invItem.recipeSlot > -1) return;
			
			foreach (InvInteraction interaction in invItem.interactions)
			{
				if (interaction.icon.id == iconID)
				{
					if (interaction.actionList)
					{
						KickStarter.eventManager.Call_OnUseInventory (invItem, iconID);
						AdvGame.RunActionListAsset (interaction.actionList);
						return;
					}
					break;
				}
			}
			
			// Unhandled
			if (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ContextSensitive && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple && KickStarter.settingsManager.CanSelectItems (false))
			{
				// Auto-select
				if (KickStarter.settingsManager.selectInvWithUnhandled && iconID == KickStarter.settingsManager.selectInvWithIconID)
				{
					SelectItem (invItem, SelectItemMode.Use);
					return;
				}
				if (KickStarter.settingsManager.giveInvWithUnhandled && iconID == KickStarter.settingsManager.giveInvWithIconID)
				{
					SelectItem (invItem, SelectItemMode.Give);
					return;
				}
			}
			
			KickStarter.eventManager.Call_OnUseInventory (invItem, iconID);
			AdvGame.RunActionListAsset (KickStarter.cursorManager.GetUnhandledInteraction (iconID));
		}
		

		/**
		 * <summary>Runs an interaction on the "hoverItem" inventory item, when multiple "use" interactions are defined.</summary>
		 * <param name = "iconID">The ID number of the interaction's icon, defined in CursorManager</param>
		 * <param name = "clickedItem">If assigned, hoverItem will be become this before the interaction is run</param>
		 */
		public void RunInteraction (int iconID, InvItem clickedItem = null)
		{
			if (clickedItem != null)
			{
				hoverItem = clickedItem;
			}
			RunInteraction (hoverItem, iconID);
		}
		

		/**
		 * <summary>Sets up all "Interaction" menus according to a specific inventory item.</summary>
		 * <param name = "item">The relevant inventory item</param>
		 */
		public void ShowInteractions (InvItem item)
		{
			hoverItem = item;
			if (KickStarter.settingsManager.SeeInteractions != SeeInteractions.ViaScriptOnly)
			{
				KickStarter.playerMenus.EnableInteractionMenus (item);
			}
		}


		/**
		 * <summary>Sets the item currently being hovered over by the mouse cursor.</summary>
		 * <param name = "item">The item to set</param>
		 * <param name = "menuInventoryBox">The MenuInventoryBox that the item is displayed within</param>
		 */
		public void SetHoverItem (InvItem item, MenuInventoryBox menuInventoryBox)
		{
			hoverItem = item;

			if (menuInventoryBox.displayType == ConversationDisplayType.IconOnly)
			{
				if (menuInventoryBox.inventoryBoxType == AC_InventoryBoxType.Container && selectedItem != null)
				{
					showHoverLabel = false;
				}
				else
				{
					showHoverLabel = true;
				}
			}
			else
			{
				showHoverLabel = menuInventoryBox.updateHotspotLabelWhenHover;
			}
		}


		/**
		 * <summary>Sets the item currently being hovered over by the mouse cursor.</summary>
		 * <param name = "item">The item to set</param>
		 * <param name = "menuCrafting">The MenuInventoryBox that the item is displayed within</param>
		 */
		public void SetHoverItem (InvItem item, MenuCrafting menuCrafting)
		{
			hoverItem = item;

			if (menuCrafting.displayType == ConversationDisplayType.IconOnly)
			{
				showHoverLabel = true;
			}
			else
			{
				showHoverLabel = false;
			}
		}


		/**
		 * <summary>Combines two inventory items.</summary>
		 * <param name = "item1">The first inventory item to combine</param>
		 * <param name = "item2ID">The ID number of the second inventory item to combine</param>
		 */
		public void Combine (InvItem item1, int item2ID)
		{
			Combine (item1, GetItem (item2ID));
		}
		

		/**
		 * <summary>Combines two inventory items.</summary>
		 * <param name = "item1">The first inventory item to combine</param>
		 * <param name = "item2ID">The second inventory item to combine</param>
		 * <param name = "allowSelfCombining">If True, then an item can be combined with itself</param>
		 */
		public void Combine (InvItem item1, InvItem item2, bool allowSelfCombining = false)
		{
			if (item2 == null || item1 == null || item2.recipeSlot > -1)
			{
				return;
			}

			if (item2 == item1 && !allowSelfCombining)
			{
				if ((KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction || KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Single) && KickStarter.settingsManager.InventoryDragDrop && KickStarter.settingsManager.inventoryDropLook)
				{
					Look (item2);
				}
				SetNull ();
				KickStarter.eventManager.Call_OnUseInventory (item1, 0, item2);
			}
			else
			{
				if (selectedItem == null)
				{
					InvItem tempItem = item1;
					item1 = item2;
					item2 = tempItem;
				}

				KickStarter.eventManager.Call_OnUseInventory (item1, 0, item2);

				for (int i=0; i<item2.combineID.Count; i++)
				{
					if (item2.combineID[i] == item1.id && item2.combineActionList[i] != null)
					{
						if (KickStarter.settingsManager.inventoryDisableDefined)
						{
							selectedItem = null;
						}

						//PlayerMenus.ForceOffAllMenus (true);
						AdvGame.RunActionListAsset (item2.combineActionList [i]);
						return;
					}
				}
				
				if (KickStarter.settingsManager.reverseInventoryCombinations || (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple))
				{
					// Try opposite: search selected item instead
					for (int i=0; i<item1.combineID.Count; i++)
					{
						if (item1.combineID[i] == item2.id && item1.combineActionList[i] != null)
						{
							if (KickStarter.settingsManager.inventoryDisableDefined)
							{
								selectedItem = null;
							}

							ActionListAsset assetFile = item1.combineActionList[i];
							//PlayerMenus.ForceOffAllMenus (true);
							AdvGame.RunActionListAsset (assetFile);
							return;
						}
					}
				}
				
				// Found no combine match
				if (KickStarter.settingsManager.inventoryDisableUnhandled)
				{
					selectedItem = null;
				}

				if (item1.unhandledCombineActionList)
				{
					ActionListAsset unhandledActionList = item1.unhandledCombineActionList;
					AdvGame.RunActionListAsset (unhandledActionList);	
				}
				else if (unhandledCombine)
				{
					//PlayerMenus.ForceOffAllMenus (true);
					AdvGame.RunActionListAsset (unhandledCombine);
				}
			}
			
			KickStarter.playerCursor.ResetSelectedCursor ();
		}


		/**
		 * <summary>Checks if a particular inventory item is currently held by the player.</summary>
		 * <param name = "_item">The inventory item to check for</param>
		 * <returns>True if the inventory item is currently held by the player</returns>
		 */
		public bool IsItemCarried (InvItem _item)
		{
			if (_item == null) return false;
			foreach (InvItem item in _localItems)
			{
				if (item == _item)
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * Resets any active recipe, and clears all MenuCrafting elements.
		 */
		public void RemoveRecipes ()
		{
			while (craftingItems.Count > 0)
			{
				for (int i=0; i<craftingItems.Count; i++)
				{
					Add (craftingItems[i].id, craftingItems[i].count, false, -1);
					craftingItems.RemoveAt (i);
				}
			}
			PlayerMenus.ResetInventoryBoxes ();
		}
		

		/**
		 * <summary>Moves an ingredient from a crafting recipe back into the player's inventory.</summary>
		 * <param name = "_recipeSlot">The index number of the MenuCrafting slot that the ingredient was placed in</param>
		 * <param name = "selectAfter">If True, the inventory item will be selected once the transfer is complete</param>
		 * <param name = "forceAll">If True, then all instances of the item will be transferred regardless of the value of its own CanSelectSingle method</param>
		 */
		public void TransferCraftingToLocal (int _recipeSlot, bool selectAfter, bool forceAll = false)
		{
			for (int i=0; i<craftingItems.Count; i++)
			{
				InvItem craftingItem = craftingItems[i];

				if (craftingItem.recipeSlot == _recipeSlot)
				{
					if (!forceAll && craftingItem.CanSelectSingle ())
					{
						craftingItem.count --;
						Add (craftingItem.id, 1, selectAfter, -1);
					}
					else
					{
						craftingItems.Remove (craftingItem);
						Add (craftingItem.id, craftingItem.count, selectAfter, -1);
					}
					SelectItemByID (craftingItem.id, SelectItemMode.Use);
					return;
				}
			}
		}


		/**
		 * <summary>Moves an ingredient from the player's inventory into a crafting recipe as an ingredient.</summary>
		 * <param name = "_item">The inventory item to transfer</param>
		 * <param name = "_slot">The index number of the MenuCrafting slot to place the item in</param>
		 */
		public void TransferLocalToCrafting (InvItem _item, int _slot)
		{
			if (_item != null && _localItems.Contains (_item))
			{
				for (int i=0; i<craftingItems.Count; i++)
				{
					InvItem craftingItem = craftingItems[i];

					if (craftingItem.recipeSlot == _slot)
					{
						// Space is filled already

						if (craftingItem.id == _item.id && _item.canCarryMultiple)
						{
							// Filled with same item, so add
							if (_item.CanSelectSingle ())
							{
								craftingItem.count ++;
								_item.count --;
							}
							else
							{
								craftingItem.count += _item.count;
								_localItems [_localItems.IndexOf (_item)] = null;
								_localItems = ReorderItems (_localItems);
								_localItems = RemoveEmptySlots (_localItems);
							}
							SetNull ();
							return;
						}
						else
						{
							// Filled with different item / can't be added
							TransferCraftingToLocal (_slot, false, true);
						}
					}
				}

				// Insert new item
				InvItem newCraftingItem = new InvItem (_item);
				newCraftingItem.recipeSlot = _slot;

				if (_item.CanSelectSingle ())
				{
					newCraftingItem.count = 1;
					_localItems [localItems.IndexOf (_item)].count --;
				}
				else
				{
					_localItems [_localItems.IndexOf (_item)] = null;
					_localItems = ReorderItems (_localItems);
					_localItems = RemoveEmptySlots (_localItems);
				}

				craftingItems.Add (newCraftingItem);
				
				SetNull ();
			}
		}
		

		/**
		 * <summary>Gets a list of inventory items associated with the interactions of the current Hotspot or item being hovered over.</summary>
		 * <returns>A list of inventory items associated with the interactions of the current Hotspot or item being hovered over</returns>
		 */
		public List<InvItem> MatchInteractions ()
		{
			List<InvItem> items = new List<InvItem>();
			matchingInvInteractions = new List<int>();
			matchingItemModes = new List<SelectItemMode>();
			
			if (!KickStarter.settingsManager.cycleInventoryCursors)
			{
				return items;
			}
			
			if (hoverItem != null)
			{
				items = MatchInteractionsFromItem (items, hoverItem);
			}
			else if (KickStarter.playerInteraction.GetActiveHotspot ())
			{
				List<Button> invButtons = KickStarter.playerInteraction.GetActiveHotspot ().invButtons;
				foreach (Button button in invButtons)
				{
					foreach (InvItem item in _localItems)
					{
						if (item != null && item.id == button.invID && !button.isDisabled)
						{
							matchingInvInteractions.Add (invButtons.IndexOf (button));
							matchingItemModes.Add (button.selectItemMode);
							items.Add (item);
							break;
						}
					}
				}
			}
			return items;
		}


		/**
		 * <summary>Works out which Recipe, if any, for which all ingredients have been correctly arranged.</summary>
		 * <returns>The Recipe, if any, for which all ingredients have been correctly arranged</returns>
		 */
		public Recipe CalculateRecipe ()
		{
			if (KickStarter.inventoryManager == null)
			{
				return null;
			}
			
			foreach (Recipe recipe in KickStarter.inventoryManager.recipes)
			{
				if (IsRecipeInvalid (recipe) || recipe.ingredients.Count == 0)
				{
					continue;
				}
				
				bool canCreateRecipe = true;
				while (canCreateRecipe)
				{
					foreach (Ingredient ingredient in recipe.ingredients)
					{
						// Is ingredient present (and optionally, in correct slot)
						InvItem ingredientItem = GetCraftingItem (ingredient.itemID);
						if (ingredientItem == null)
						{
							canCreateRecipe = false;
							break;
						}

						int ingredientCount = GetCraftingItemCount (ingredient.itemID);

						if ((recipe.useSpecificSlots && ingredientItem.recipeSlot == (ingredient.slotNumber -1)) || !recipe.useSpecificSlots)
						{
							if ((ingredientItem.canCarryMultiple && ingredient.amount <= ingredientCount) || !ingredientItem.canCarryMultiple)
							{
								if (canCreateRecipe && recipe.ingredients.IndexOf (ingredient) == (recipe.ingredients.Count -1))
								{
									return recipe;
								}
							}
							else canCreateRecipe = false;
						}
						else canCreateRecipe = false;
					}
				}
			}
			
			return null;
		}


		/**
		 * <summary>Crafts a new inventory item, and removes the relevent ingredients, according to a Recipe.</summary>
		 * <param name = "recipe">The Recipe to perform</param>
		 * <param name = "selectAfter">If True, then the resulting inventory item will be selected once the crafting is complete</param>
		 */
		public void PerformCrafting (Recipe recipe, bool selectAfter)
		{
			foreach (Ingredient ingredient in recipe.ingredients)
			{
				int ingredientAmount = ingredient.amount;

				for (int i=0; i<craftingItems.Count; i++)
				{
					if (craftingItems [i].id == ingredient.itemID)
					{
						if (craftingItems [i].canCarryMultiple && ingredientAmount > 0)
						{
							if (craftingItems[i].count < ingredientAmount)
							{
								// Remove all, and remove more elsewhere
								ingredientAmount -= craftingItems[i].count;
								craftingItems.RemoveAt (i);
								i=-1;
							}
							else
							{
								craftingItems [i].count -= ingredientAmount;
								if (craftingItems [i].count < 1)
								{
									craftingItems.RemoveAt (i);
									i=-1;
								}
							}
						}
						else
						{
							craftingItems.RemoveAt (i);
						}
					}
				}
			}
			
			RemoveEmptyCraftingSlots ();
			Add (recipe.resultID, 1, selectAfter, -1);
		}


		/**
		 * <summary>Moves an item already in the current player's inventory to a different slot.</summary>
		 * <param name = "item">The inventory item to move</param>
		 * <param name = "index">The index number of the MenuInventoryBox slot to move the item to</param>
		 */
		public void MoveItemToIndex (InvItem item, int index)
		{
			if (item != null && _localItems.Contains (item))
			{
				// Check nothing in place already
				int oldIndex = _localItems.IndexOf (item);
				while (_localItems.Count <= Mathf.Max (index, oldIndex))
				{
					_localItems.Add (null);
				}
				
				if (_localItems [index] == null)
				{
					_localItems [index] = item;
					_localItems [oldIndex] = null;
				}
				else
				{
					// Item already in its spot

					_localItems [oldIndex] = null;
					_localItems.Insert (index, item);
				}
				
				SetNull ();
				_localItems = RemoveEmptySlots (_localItems);
			}
			else if (item != null && selectedContainerItem != null)
			{
				while (_localItems.Count <= index)
				{
					_localItems.Add (null);
				}

				if (_localItems [index] == null)
				{
					_localItems [index] = item;
				}
				else
				{
					// Item already in its spot
					_localItems.Insert (index, item);
				}
				
				_localItems = RemoveEmptySlots (_localItems);

				selectedContainerItemContainer.Remove (selectedContainerItem);
				SetNull ();
			}
		}


		/**
		 * <summary>Assign's the player's current inventory in bulk</summary>
		 * <param name = "newInventory">A list of the InvItem classes that make up the new inventory</param>
		 */
		public void AssignPlayerInventory (List<InvItem> newInventory)
		{
			_localItems = newInventory;
		}
		

		/**
		 * <summary>Moves an item already in an inventory to a different slot.</summary>
		 * <param name = "item">The inventory item to move</param>
		 * <param name = "items">The List of inventory items that the item is to be moved within</param>
		 * <param name = "index">The index number of the MenuInventoryBox slot to move the item to</param>
		 * <returns>The re-ordered List of inventory items</returns>
		 */
		public List<InvItem> MoveItemToIndex (InvItem item, List<InvItem> items, int index)
		{
			if (item != null && items.Contains (item))
			{
				// Check nothing in place already
				int oldIndex = items.IndexOf (item);
				while (items.Count <= Mathf.Max (index, oldIndex))
				{
					items.Add (null);
				}
				
				if (items [index] == null)
				{
					items [index] = item;
					items [oldIndex] = null;
				}
				else
				{
					// Item already in its spot

					items [oldIndex] = null;
					items.Insert (index, item);
				}
				
				SetNull ();
				items = RemoveEmptySlots (items);
			}
			return items;
		}
		

		/**
		 * <summary>Sets the font style of the "amount" numbers displayed over an inventory item in OnGUI menus</summary>
		 * <param name = "font">The font to use<param>
		 * <param name = "size">The font's size</param>
		 * <param name = "color">The colour to set the font</param>
		 * <param name = "textEffects">What text effect to apply (Outline, Shadow, OutlineAndShadow)</param>
		 */
		public void SetFont (Font font, int size, Color color, TextEffects textEffects)
		{
			countStyle = new GUIStyle();
			countStyle.font = font;
			countStyle.fontSize = size;
			countStyle.normal.textColor = color;
			countStyle.alignment = TextAnchor.MiddleCenter;
			countTextEffects = textEffects;
		}
		

		/**
		 * <summary>Draws the currently-highlight item across a set region of the screen.</summary>
		 * <param name = "_rect">The Screen-Space co-ordinates at which to draw the highlight item</param>
		 */
		public void DrawHighlighted (Rect _rect)
		{
			if (highlightItem == null || highlightItem.activeTex == null) return;
			
			if (highlightState == HighlightState.None)
			{
				GUI.DrawTexture (_rect, highlightItem.activeTex, ScaleMode.StretchToFill, true, 0f);
				return;
			}
			
			if (pulseDirection == 0)
			{
				pulse = 0f;
				pulseDirection = 1;
			}
			else if (pulseDirection == 1)
			{
				pulse += KickStarter.settingsManager.inventoryPulseSpeed * Time.deltaTime;
			}
			else if (pulseDirection == -1)
			{
				pulse -= KickStarter.settingsManager.inventoryPulseSpeed * Time.deltaTime;
			}
			
			if (pulse > 1f)
			{
				pulse = 1f;
				
				if (highlightState == HighlightState.Normal)
				{
					highlightState = HighlightState.None;
					GUI.DrawTexture (_rect, highlightItem.activeTex, ScaleMode.StretchToFill, true, 0f);
					return;
				}
				else
				{
					pulseDirection = -1;
				}
			}
			else if (pulse < 0f)
			{
				pulse = 0f;
				
				if (highlightState == HighlightState.Pulse)
				{
					pulseDirection = 1;
				}
				else
				{
					highlightState = HighlightState.None;
					GUI.DrawTexture (_rect, highlightItem.tex, ScaleMode.StretchToFill, true, 0f);
					highlightItem = null;
					return;
				}
			}

			Color backupColor = GUI.color;
			Color tempColor = GUI.color;
			
			tempColor.a = pulse;
			GUI.color = tempColor;
			GUI.DrawTexture (_rect, highlightItem.activeTex, ScaleMode.StretchToFill, true, 0f);
			GUI.color = backupColor;
			GUI.DrawTexture (_rect, highlightItem.tex, ScaleMode.StretchToFill, true, 0f);
		}
		

		/**
		 * <summary>Fully highlights an inventory item instantly.</summary>
		 * <param name = "_id">The ID number of the inventory item (see InvItem) to highlight</param>
		 */
		public void HighlightItemOnInstant (int _id)
		{
			highlightItem = GetItem (_id);
			highlightState = HighlightState.None;
			pulse = 1f;
		}
		

		/**
		 * Removes all highlighting from the inventory item curently being highlighted.
		 */
		public void HighlightItemOffInstant ()
		{
			highlightItem = null;
			highlightState = HighlightState.None;
			pulse = 0f;
		}
		

		/**
		 * <summary>Highlights an inventory item.</summary>
		 * <param name = "_id">The ID number of the inventory item (see InvItem) to highlight</param>
		 * <param name = "_type">The type of highlighting effect to perform (Enable, Disable, PulseOnce, PulseContinuously)</param>
		 */
		public void HighlightItem (int _id, HighlightType _type)
		{
			highlightItem = GetItem (_id);
			if (highlightItem == null) return;
			
			if (_type == HighlightType.Enable)
			{
				highlightState = HighlightState.Normal;
				pulseDirection = 1;
			}
			else if (_type == HighlightType.Disable)
			{
				highlightState = HighlightState.Normal;
				pulseDirection = -1;
			}
			else if (_type == HighlightType.PulseOnce)
			{
				highlightState = HighlightState.Flash;
				pulse = 0f;
				pulseDirection = 1;
			}
			else if (_type == HighlightType.PulseContinually)
			{
				highlightState = HighlightState.Pulse;
				pulse = 0f;
				pulseDirection = 1;
			}
		}
		

		/**
		 * <summary>Draws a number at the cursor position. This should be called within an OnGUI function.</summary>
		 * <param name = "cursorPosition">The position of the cursor</param>
		 * <param name = "cursorSize">The size to draw the number<param>
		 * <param name = "count">The number to display</param>
		 */
		public void DrawInventoryCount (Vector2 cursorPosition, float cursorSize, int count)
		{
			if (count > 1)
			{
				if (countTextEffects != TextEffects.None)
				{
					AdvGame.DrawTextEffect (AdvGame.GUIBox (cursorPosition, cursorSize), count.ToString (), countStyle, Color.black, countStyle.normal.textColor, 2, countTextEffects);
				}
				else
				{
					GUI.Label (AdvGame.GUIBox (cursorPosition, cursorSize), count.ToString (), countStyle);
				}
			}
		}


		/**
		 * <summary>Processes the clicking of an inventory item within a MenuInventoryBox element</summary>
		 * <param name = "_menu">The Menu that contains the MenuInventoryBox element</param>
		 * <param name = "inventoryBox">The MenuInventoryBox element that was clicked on</param>
		 * <param name = "_slot">The index number of the MenuInventoryBox slot that was clicked on</param>
		 * <param name = "_mouseState">The state of the mouse when the click occured (Normal, SingleClick, RightClick, DoubleClick, HeldDown, LetGo)</param>
		 */
		public void ProcessInventoryBoxClick (AC.Menu _menu, MenuInventoryBox inventoryBox, int _slot, MouseState _mouseState)
		{
			switch (inventoryBox.inventoryBoxType)
			{
				case AC_InventoryBoxType.Default:
				case AC_InventoryBoxType.DisplayLastSelected:
					{
						if (KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple && KickStarter.playerMenus.IsInteractionMenuOn ())
						{
							KickStarter.playerMenus.CloseInteractionMenus ();
							ClickInvItemToInteract ();
						}
						else if (KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple && KickStarter.settingsManager.SelectInteractionMethod () == AC.SelectInteractions.CyclingCursorAndClickingHotspot)
						{
							if (KickStarter.settingsManager.autoCycleWhenInteract && _mouseState == MouseState.SingleClick && (selectedItem == null || KickStarter.settingsManager.cycleInventoryCursors))
							{
								int originalIndex = KickStarter.playerInteraction.GetInteractionIndex ();
								KickStarter.playerInteraction.SetNextInteraction ();
								KickStarter.playerInteraction.SetInteractionIndex (originalIndex);
							}

							if (!KickStarter.settingsManager.cycleInventoryCursors && selectedItem != null)
							{
								inventoryBox.HandleDefaultClick (_mouseState, _slot, KickStarter.settingsManager.interactionMethod);
							}
							else if (_mouseState != MouseState.RightClick)
							{
								KickStarter.playerMenus.CloseInteractionMenus ();
								ClickInvItemToInteract ();
							}

							if (KickStarter.settingsManager.autoCycleWhenInteract && _mouseState == MouseState.SingleClick)
							{
								KickStarter.playerInteraction.RestoreInventoryInteraction ();
							}
						}
						else if (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ContextSensitive && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Single)
						{
							inventoryBox.HandleDefaultClick (_mouseState, _slot, AC_InteractionMethod.ContextSensitive);
						}
						else
						{
							inventoryBox.HandleDefaultClick (_mouseState, _slot, KickStarter.settingsManager.interactionMethod);

							if (KickStarter.settingsManager.autoCycleWhenInteract && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple)
							{
								if (selectedItem == null)
								{
									KickStarter.playerCursor.ResetSelectedCursor ();
								}
							}
						}

						_menu.Recalculate ();
					}
					break;

				case AC_InventoryBoxType.Container:
					{
						inventoryBox.ClickContainer (_mouseState, _slot);
						_menu.Recalculate ();
					}
					break;

				case AC_InventoryBoxType.HotspotBased:
					{
						if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
						{
							if (_menu.TargetInvItem != null)
							{
								Combine (_menu.TargetInvItem, inventoryBox.items[_slot + inventoryBox.GetOffset ()], true);
							}
							else if (_menu.TargetHotspot != null)
							{
								InvItem _item = inventoryBox.items[_slot + inventoryBox.GetOffset ()];
								if (_item != null)
								{
									_menu.TurnOff (false);
									KickStarter.playerInteraction.UseInventoryOnHotspot (_menu.TargetHotspot, _item.id, true);
									KickStarter.playerCursor.ResetSelectedCursor ();
								}
							}
							else
							{
								ACDebug.LogWarning ("Cannot handle inventory click since there is no active Hotspot.");
							}
						}
						else
						{
							ACDebug.LogWarning ("This type of InventoryBox only works with the Choose Hotspot Then Interaction method of interaction.");
						}
					}
					break;
			}
		}


		/**
		 * <summary>Gets the total value of all instances of an Integer inventory property (e.g. currency) within the player's inventory.</summary>
		 * <param name = "ID">The ID number of the Inventory property (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Integer inventory property within the player's inventory</returns>
		 */
		public int GetTotalIntProperty (int ID)
		{
			return GetTotalIntProperty (_localItems.ToArray (), ID);
		}


		/**
		 * <summary>Gets the total value of all instances of an Integer inventory property (e.g. currency) within a set of inventory items.</summary>
		 * <param name = "items">The inventory items to get the total value from</param>
		 * <param name = "ID">The ID number of the Inventory property (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Integer inventory property within the set of inventory items</returns>
		 */
		public int GetTotalIntProperty (InvItem[] items, int ID)
		{
			int result = 0;
			foreach (InvItem item in items)
			{
				foreach (InvVar var in item.vars)
				{
					if (var.id == ID && var.type == VariableType.Integer)
					{
						result += var.val;
						break;
					}
				}
			}
			return result;
		}


		/**
		 * <summary>Gets the total value of all instances of an Float inventory property (e.g. weight) within the player's inventory.</summary>
		 * <param name = "ID">The ID number of the Inventory Float (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Float inventory property within the player's inventory</returns>
		 */
		public float GetTotalFloatProperty (int ID)
		{
			return GetTotalFloatProperty (_localItems.ToArray (), ID);
		}
		
		
		/**
		 * <summary>Gets the total value of all instances of an Float inventory property (e.g. weight) within a set of inventory items.</summary>
		 * <param name = "items">The inventory items to get the total value from</param>
		 * <param name = "ID">The ID number of the Inventory property (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Float inventory property within the set of inventory items</returns>
		 */
		public float GetTotalFloatProperty (InvItem[] items, int ID)
		{
			float result = 0f;
			foreach (InvItem item in items)
			{
				foreach (InvVar var in item.vars)
				{
					if (var.id == ID && var.type == VariableType.Float)
					{
						result += var.floatVal;
						break;
					}
				}
			}
			return result;
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			if (selectedItem != null)
			{
				mainData.selectedInventoryID = selectedItem.id;
				mainData.isGivingItem = IsGivingItem ();
			}
			else
			{
				mainData.selectedInventoryID = -1;
			}

			return mainData;
		}


		/**
		 * <summary>Gets the total amount of given integer or float inventory property, found in the current inventory<summary>
		 * <param name = "propertyID">The ID of the integer or float inventory property</param>
		 * <returns>The total amount of the property's value</returns>
		 */
		public InvVar GetPropertyTotals (int propertyID)
		{
			InvVar originalVar = KickStarter.inventoryManager.GetProperty (propertyID);
			if (originalVar == null) return null;

			InvVar totalVar = new InvVar (propertyID, originalVar.type);

			foreach (InvItem item in _localItems)
			{
				if (item != null)
				{
					InvVar var = item.GetProperty (propertyID);
					if (var != null)
					{
						totalVar.TransferValues (var);
					}
				}
			}
			return totalVar;
		}


		/**
		 * <summary>Gets the total amount a property, found in the current inventory<summary>
		 * <param name = "propertyID">The ID of the inventory property</param>
		 * <param name = 
		 * <returns>The total amount of the property's value</returns>
		 */
		public InvVar GetPropertyTotals (int propertyID, int itemID)
		{
			InvVar originalVar = KickStarter.inventoryManager.GetProperty (propertyID);
			if (originalVar == null) return null;
			InvVar totalVar = new InvVar (propertyID, originalVar.type);

			foreach (InvItem item in _localItems)
			{
				if (item != null && item.id == itemID)
				{
					InvVar var = item.GetProperty (propertyID, true);
					if (var != null)
					{
						totalVar.TransferValues (var);
					}
				}
			}
			return totalVar;
		}


		/**
		 * <summary>Gets an array of all carried inventory items in a given category</summary>
		 * <param name = "categoryID">The ID number of the category in question</param>
		 * <returns>An array of all carried inventory items in the category</returns>
		 */
		public InvItem[] GetItemsInCategory (int categoryID)
		{
			List<InvItem> itemsList = new List<InvItem>();
			foreach (InvItem item in _localItems)
			{
				if (item.binID == categoryID)
				{
					itemsList.Add (item);
				}
			}

			return itemsList.ToArray ();
		}


		/**
		 * <summary>Checks if an item can be transferred from a Container to the current Player's inventory</summary>
		 * <param name = "containerItem">The ContainerItem to transfer</param>
		 * <returns>True if the item can be tranferred.  This is always True, provided containerItem is not null, but the method can be overridden through subclassing</returns>
		 */
		public virtual bool CanTransferContainerItemsToInventory (ContainerItem containerItem)
		{
			return (containerItem != null && !containerItem.IsEmpty);
		}

		#endregion


		#region ProtectedFunctions

		protected void GetItemsOnStart ()
		{
			if (KickStarter.inventoryManager)
			{
				foreach (InvItem item in KickStarter.inventoryManager.items)
				{
					if (item.carryOnStart)
					{
						int playerID = -1;
						if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && !KickStarter.settingsManager.shareInventory && item.carryOnStartNotDefault && KickStarter.player != null && item.carryOnStartID != KickStarter.player.ID)
						{
							playerID = item.carryOnStartID;
						}

						if (!item.canCarryMultiple)
						{
							item.count = 1;
						}
						
						if (item.count < 1)
						{
							continue;
						}
						
						item.recipeSlot = -1;
						
						if (item.canCarryMultiple && item.useSeparateSlots)
						{
							for (int i=0; i<item.count; i++)
							{
								InvItem newItem = new InvItem (item);
								newItem.count = 1;
								
								if (playerID != -1)
								{
									Add (newItem.id, newItem.count, false, playerID);
								}
								else
								{
									_localItems.Add (newItem);
								}
							}
						}
						else
						{
							if (playerID != -1)
							{
								Add (item.id, item.count, false, playerID);
							}
							else
							{
								_localItems.Add (new InvItem (item));
							}
						}
					}
				}
			}
			else
			{
				ACDebug.LogError ("No Inventory Manager found - please use the Adventure Creator window to create one.");
			}
		}


		protected void AddToOtherPlayer (int invID, int amount, int playerID, bool addToFront)
		{
			SaveSystem saveSystem = GetComponent <SaveSystem>();
			
			List<InvItem> otherPlayerItems = saveSystem.GetItemsFromPlayer (playerID);
			otherPlayerItems = Add (invID, amount, otherPlayerItems, false, addToFront);
			saveSystem.AssignItemsToPlayer (otherPlayerItems, playerID);
		}
		
		
		protected void RemoveFromOtherPlayer (int invID, int amount, bool setAmount, int playerID)
		{
			SaveSystem saveSystem = GetComponent <SaveSystem>();
			
			List<InvItem> otherPlayerItems = saveSystem.GetItemsFromPlayer (playerID);
			otherPlayerItems = Remove (invID, amount, setAmount, otherPlayerItems);
			saveSystem.AssignItemsToPlayer (otherPlayerItems, playerID);
		}


		protected List<InvItem> Remove (int _id, int amount, bool setAmount, List<InvItem> itemList)
		{
			if (amount <= 0)
			{
				return itemList;
			}
			
			foreach (InvItem item in itemList)
			{
				if (item != null && item.id == _id)
				{
					KickStarter.eventManager.Call_OnChangeInventory (item, InventoryEventType.Remove, amount);

					if (item.canCarryMultiple && item.useSeparateSlots)
					{
						itemList [itemList.IndexOf (item)] = null;
						amount --;
						
						if (amount == 0)
						{
							break;
						}
						
						continue;
					}
					
					if (!item.canCarryMultiple || !setAmount)
					{
						itemList [itemList.IndexOf (item)] = null;
						amount = 0;
					}
					else
					{
						if (item.count > 0)
						{
							int numLeft = item.count - amount;
							item.count -= amount;
							amount = numLeft;
						}
						if (item.count < 1)
						{
							itemList [itemList.IndexOf (item)] = null;
						}
					}
					
					itemList = ReorderItems (itemList);
					itemList = RemoveEmptySlots (itemList);

					if (itemList.Count == 0)
					{
						PlayerMenus.ResetInventoryBoxes ();
						return itemList;
					}
					
					if (amount <= 0)
					{
						PlayerMenus.ResetInventoryBoxes ();
						return itemList;
					}
				}
			}
			
			itemList = ReorderItems (itemList);
			itemList = RemoveEmptySlots (itemList);

			PlayerMenus.ResetInventoryBoxes ();
			return itemList;
		}


		protected List<InvItem> ReorderItems (List<InvItem> invItems)
		{
			if (!KickStarter.settingsManager.canReorderItems)
			{
				for (int i=0; i<invItems.Count; i++)
				{
					if (invItems[i] == null)
					{
						invItems.RemoveAt (i);
						i=0;
					}
				}
			}
			return invItems;
		}
		
		
		protected void RemoveEmptyCraftingSlots ()
		{
			// Remove empty slots on end
			for (int i=craftingItems.Count-1; i>=0; i--)
			{
				if (_localItems.Count > i && _localItems[i] == null)
				{
					_localItems.RemoveAt (i);
				}
				else
				{
					return;
				}
			}
		}
		

		protected int GetCraftingItemCount (int _id)
		{
			int count = 0;

			for (int i=0; i<craftingItems.Count; i++)
			{
				if (craftingItems[i].id == _id)
				{
					if (craftingItems[i].canCarryMultiple)
					{
						count += craftingItems[i].count;
					}
					else
					{
						count ++;
					}
				}
			}
			return count;
		}

		
		protected List<InvItem> MatchInteractionsFromItem (List<InvItem> items, InvItem _item)
		{
			if (_item != null && _item.combineID != null)
			{
				foreach (int combineID in _item.combineID)
				{
					foreach (InvItem item in _localItems)
					{
						if (item != null && item.id == combineID)
						{
							matchingInvInteractions.Add (_item.combineID.IndexOf (combineID));
							matchingItemModes.Add (SelectItemMode.Use);
							items.Add (item);
							break;
						}
					}
				}
			}

			return items;
		}
		

		protected bool IsRecipeInvalid (Recipe recipe)
		{
			// Are any invalid ingredients present?
			for (int i=0; i<craftingItems.Count; i++)
			{
				bool found = false;
				for (int j=0; j<recipe.ingredients.Count; j++)
				{
					if (recipe.ingredients[j].itemID == craftingItems[i].id)
					{
						found = true;
					}
				}
				if (!found)
				{
					// Not present in recipe
					return true;
				}
			}
			return false;
		}


		protected void ClickInvItemToInteract ()
		{
			int invID = KickStarter.playerInteraction.GetActiveInvButtonID ();
			if (invID == -1)
			{
				RunInteraction (KickStarter.playerInteraction.GetActiveUseButtonIconID ());
			}
			else
			{
				Combine (hoverItem, invID);
			}
		}

		#endregion


		#region GetSet

		/** The inventory item that is currently selected */
		public InvItem SelectedItem
		{
			get
			{
				return selectedItem;
			}
		}


		/** A List of inventory items (InvItem) carried by the player */
		public List<InvItem> localItems
		{
			get
			{
				return _localItems;
			}
		}


		/** The last inventory item to be selected.  This will return the currently-selected item if one exists */ 
		public InvItem LastSelectedItem
		{
			get
			{
				return lastSelectedItem;
			}
		}

		#endregion

	}
	
}