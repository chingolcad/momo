/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionInventorySelect.cs"
 * 
 *	This action is used to automatically-select an inventory item.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionInventorySelect : Action
	{

		public enum InventorySelectType { SelectItem, DeselectActive };
		public InventorySelectType selectType = InventorySelectType.SelectItem;
		public SelectItemMode selectItemMode = SelectItemMode.Use;

		public bool giveToPlayer = false;

		public int parameterID = -1;
		public int invID;
		protected int invNumber;

		#if UNITY_EDITOR
		private InventoryManager inventoryManager;
		private SettingsManager settingsManager;
		#endif


		public ActionInventorySelect ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Inventory;
			title = "Select";
			description = "Selects a chosen inventory item, as though the player clicked on it in the Inventory menu. Will optionally add the specified item to the inventory if it is not currently held.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			invID = AssignInvItemID (parameters, parameterID, invID);
		}
		
		
		public override float Run ()
		{
			if (KickStarter.runtimeInventory)
			{
				if (selectType == InventorySelectType.DeselectActive)
				{
					KickStarter.runtimeInventory.SetNull ();
				}
				else
				{
					if (!KickStarter.settingsManager.CanSelectItems (true))
					{
						return 0f;
					}

					if (giveToPlayer)
					{
						KickStarter.runtimeInventory.Add (invID, 1, false, -1);
					}

					KickStarter.runtimeInventory.SelectItemByID (invID, selectItemMode);
				}
			}
			
			return 0f;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			selectType = (InventorySelectType) EditorGUILayout.EnumPopup ("Select type:", selectType);
			if (selectType == InventorySelectType.DeselectActive)
			{
				AfterRunningOption ();
				return;
			}

			if (!inventoryManager)
			{
				inventoryManager = AdvGame.GetReferences ().inventoryManager;
			}
			if (!settingsManager)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
			
			if (inventoryManager)
			{
				// Create a string List of the field's names (for the PopUp box)
				List<string> labelList = new List<string>();
				
				int i = 0;
				if (parameterID == -1)
				{
					invNumber = -1;
				}
				
				if (inventoryManager.items.Count > 0)
				{
					foreach (InvItem _item in inventoryManager.items)
					{
						labelList.Add (_item.label);
						
						// If an item has been removed, make sure selected variable is still valid
						if (_item.id == invID)
						{
							invNumber = i;
						}
						
						i++;
					}
					
					if (invNumber == -1)
					{
						ACDebug.LogWarning ("Previously chosen item no longer exists!");
						invNumber = 0;
						invID = 0;
					}

					parameterID = Action.ChooseParameterGUI ("Inventory item:", parameters, parameterID, ParameterType.InventoryItem);
					if (parameterID >= 0)
					{
						invNumber = Mathf.Min (invNumber, inventoryManager.items.Count-1);
						invID = -1;
					}
					else
					{
						invNumber = EditorGUILayout.Popup ("Inventory item:", invNumber, labelList.ToArray());
						invID = inventoryManager.items[invNumber].id;
					}

					giveToPlayer = EditorGUILayout.Toggle ("Add if not held?", giveToPlayer);

					if (settingsManager && settingsManager.CanGiveItems ())
					{
						selectItemMode = (SelectItemMode) EditorGUILayout.EnumPopup ("Select item mode:", selectItemMode);
					}

				}
				else
				{
					EditorGUILayout.HelpBox ("No inventory items exist!", MessageType.Info);
					invID = -1;
					invNumber = -1;
				}
			}
			AfterRunningOption ();
		}
		
		
		public override string SetLabel ()
		{
			if (selectType == InventorySelectType.DeselectActive)
			{
				return "Deselect active";
			}

			if (inventoryManager)
			{
				return inventoryManager.GetLabel (invID);
			}
			return string.Empty;
		}


		public override int GetInventoryReferences (List<ActionParameter> parameters, int _invID)
		{
			if (selectType == InventorySelectType.SelectItem && invID == _invID)
			{
				return 1;
			}
			return 0;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Inventory: Select' Action, set to select a specific inventory item</summary>
		 * <param name = "itemID">The ID number of the inventory item to select</param>
		 * <param name = "addIfNotCarrying">If True, the item will be added to the player's inventory</param>
		 * <param name = "selectItemMode">The 'select mode' to be in (Use, Give), if supported</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventorySelect CreateNew_Select (int itemID, bool addIfNotCarrying = false, SelectItemMode selectItemMode = SelectItemMode.Use)
		{
			ActionInventorySelect newAction = (ActionInventorySelect) CreateInstance <ActionInventorySelect>();
			newAction.selectType = InventorySelectType.SelectItem;
			newAction.invID = itemID;
			newAction.giveToPlayer = addIfNotCarrying;
			newAction.selectItemMode = selectItemMode;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Inventory: Select' Action, set to deselect the current inventory item</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventorySelect CreateNew_DeselectActive ()
		{
			ActionInventorySelect newAction = (ActionInventorySelect) CreateInstance <ActionInventorySelect>();
			newAction.selectType = InventorySelectType.DeselectActive;
			return newAction;
		}

	}

}