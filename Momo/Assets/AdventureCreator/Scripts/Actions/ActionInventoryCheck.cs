/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionInventoryCheck.cs"
 * 
 *	This action checks to see if a particular inventory item
 *	is held by the player, and performs something accordingly.
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
	public class ActionInventoryCheck : ActionCheck
	{

		public int parameterID = -1;
		public int invID;
		protected int invNumber;

		[SerializeField] protected InvCheckType invCheckType = InvCheckType.CarryingSpecificItem;
		protected enum InvCheckType { CarryingSpecificItem, NumberOfItemsCarrying };

		public bool checkNumberInCategory;
		public int categoryIDToCheck;

		public bool doCount;
		public int intValueParameterID = -1;
		public int intValue = 1;
		public enum IntCondition { EqualTo, NotEqualTo, LessThan, MoreThan };
		public IntCondition intCondition;

		public bool setPlayer = false;
		public int playerID;

		#if UNITY_EDITOR
		private InventoryManager inventoryManager;
		private SettingsManager settingsManager;
		#endif

		
		public ActionInventoryCheck ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Inventory;
			title = "Check";
			description = "Queries whether or not the player is carrying an item. If the player can carry multiple amounts of the item, more options will show.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			invID = AssignInvItemID (parameters, parameterID, invID);
			intValue = AssignInteger (parameters, intValueParameterID, intValue);
		}

		
		public override bool CheckCondition ()
		{
			int count = 0;

			if (invCheckType == InvCheckType.CarryingSpecificItem)
			{
				if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && !KickStarter.settingsManager.shareInventory && setPlayer)
				{
					count = KickStarter.runtimeInventory.GetCount (invID, playerID);
				}
				else
				{
					count = KickStarter.runtimeInventory.GetCount (invID);
				}
			}
			else if (invCheckType == InvCheckType.NumberOfItemsCarrying)
			{
				if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && !KickStarter.settingsManager.shareInventory && setPlayer)
				{
					if (checkNumberInCategory)
					{
						count = KickStarter.runtimeInventory.GetNumberOfItemsCarriedInCategory (playerID, categoryIDToCheck);
					}
					else
					{
						count = KickStarter.runtimeInventory.GetNumberOfItemsCarried (playerID);
					}
				}
				else
				{
					if (checkNumberInCategory)
					{
						count = KickStarter.runtimeInventory.GetNumberOfItemsCarriedInCategory (categoryIDToCheck);
					}
					else
					{
						count = KickStarter.runtimeInventory.GetNumberOfItemsCarried ();
					}
				}
			}
			
			if (doCount || invCheckType == InvCheckType.NumberOfItemsCarrying)
			{
				if (intCondition == IntCondition.EqualTo)
				{
					if (count == intValue)
					{
						return true;
					}
				}
				
				else if (intCondition == IntCondition.NotEqualTo)
				{
					if (count != intValue)
					{
						return true;
					}
				}
				
				else if (intCondition == IntCondition.LessThan)
				{
					if (count < intValue)
					{
						return true;
					}
				}
				
				else if (intCondition == IntCondition.MoreThan)
				{
					if (count > intValue)
					{
						return true;
					}
				}
			}
			
			else if (count > 0)
			{
				return true;
			}
			
			return false;	
		}
		

		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (inventoryManager == null && AdvGame.GetReferences ().inventoryManager)
			{
				inventoryManager = AdvGame.GetReferences ().inventoryManager;
			}
			if (settingsManager == null && AdvGame.GetReferences ().settingsManager)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
			
			invCheckType = (InvCheckType) EditorGUILayout.EnumPopup ("Check to make:", invCheckType);
			if (invCheckType == InvCheckType.NumberOfItemsCarrying)
			{
				intCondition = (IntCondition) EditorGUILayout.EnumPopup ("Count is:", intCondition);
				
				intValueParameterID = Action.ChooseParameterGUI (intCondition.ToString () + ":", parameters, intValueParameterID, ParameterType.Integer);
				if (intValueParameterID < 0)
				{
					intValue = EditorGUILayout.IntField (intCondition.ToString () + ":", intValue);
				}

				if (inventoryManager != null && inventoryManager.bins != null && inventoryManager.bins.Count > 0)
				{
					checkNumberInCategory = EditorGUILayout.Toggle ("Check specific category?", checkNumberInCategory);
					if (checkNumberInCategory)
					{
						int categoryIndex = 0;
						string[] popupList = new string[inventoryManager.bins.Count];
						for (int i=0; i<inventoryManager.bins.Count; i++)
						{
							popupList[i] = inventoryManager.bins[i].label;

							if (inventoryManager.bins[i].id == categoryIDToCheck)
							{
								categoryIndex = i;
							}
						}

						categoryIndex = EditorGUILayout.Popup ("Limit to category:", categoryIndex, popupList);
						categoryIDToCheck = inventoryManager.bins[categoryIndex].id;
					}
				}

				SetPlayerGUI ();
				return;
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
						// Wasn't found (item was possibly deleted), so revert to zero
						ACDebug.LogWarning ("Previously chosen item no longer exists!");
						
						invNumber = 0;
						invID = 0;
					}

					//
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
					//
					
					if (inventoryManager.items[invNumber].canCarryMultiple)
					{
						doCount = EditorGUILayout.Toggle ("Query count?", doCount);
					
						if (doCount)
						{
							intCondition = (IntCondition) EditorGUILayout.EnumPopup ("Count is:", intCondition);
							intValueParameterID = Action.ChooseParameterGUI (intCondition.ToString () + ":", parameters, intValueParameterID, ParameterType.Integer);
							if (intValueParameterID < 0)
							{
								intValue = EditorGUILayout.IntField (intCondition.ToString () + ":", intValue);
						
								if (intValue < 1)
								{
									intValue = 1;
								}
							}
						}
					}
					else
					{
						doCount = false;
					}

					SetPlayerGUI ();
				}
				else
				{
					EditorGUILayout.LabelField ("No inventory items exist!");
					invID = -1;
					invNumber = -1;
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("An Inventory Manager must be assigned for this Action to work", MessageType.Warning);
			}
		}


		private void SetPlayerGUI ()
		{
			if (settingsManager != null && settingsManager.playerSwitching == PlayerSwitching.Allow && !settingsManager.shareInventory)
			{
				EditorGUILayout.Space ();
				
				setPlayer = EditorGUILayout.Toggle ("Check specific player?", setPlayer);
				if (setPlayer)
				{
					ChoosePlayerGUI ();
				}
			}
			else
			{
				setPlayer = false;
			}
		}

		
		public override string SetLabel ()
		{
			if (!inventoryManager)
			{
				inventoryManager = AdvGame.GetReferences ().inventoryManager;
			}

			if (invCheckType == InvCheckType.NumberOfItemsCarrying)
			{
				return invCheckType.ToString ();
			}
			if (inventoryManager)
			{
				return inventoryManager.GetLabel (invID);
			}
			
			return string.Empty;
		}


		private void ChoosePlayerGUI ()
		{
			List<string> labelList = new List<string>();
			int i = 0;
			int playerNumber = -1;
			
			if (settingsManager.players.Count > 0)
			{
				foreach (PlayerPrefab playerPrefab in settingsManager.players)
				{
					if (playerPrefab.playerOb != null)
					{
						labelList.Add (playerPrefab.playerOb.name);
					}
					else
					{
						labelList.Add ("(Undefined prefab)");
					}
					
					// If a player has been removed, make sure selected player is still valid
					if (playerPrefab.ID == playerID)
					{
						playerNumber = i;
					}
					
					i++;
				}
				
				if (playerNumber == -1)
				{
					// Wasn't found (item was possibly deleted), so revert to zero
					ACDebug.LogWarning ("Previously chosen Player no longer exists!");
					
					playerNumber = 0;
					playerID = 0;
				}

				playerNumber = EditorGUILayout.Popup ("Player to check:", playerNumber, labelList.ToArray());
				playerID = settingsManager.players[playerNumber].ID;
			}
		}


		public override int GetInventoryReferences (List<ActionParameter> parameters, int _invID)
		{
			if (invCheckType == InvCheckType.CarryingSpecificItem && invID == _invID)
			{
				return 1;
			}
			return 0;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Inventory: Check' Action, set to check if the player is carrying a specific item</summary>
		 * <param name = "itemID">The ID number of the inventory item to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventoryCheck CreateNew_CarryingSpecificItem (int itemID)
		{
			ActionInventoryCheck newAction = (ActionInventoryCheck) CreateInstance <ActionInventoryCheck>();
			newAction.invCheckType = InvCheckType.CarryingSpecificItem;
			newAction.invID = itemID;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Inventory: Check' Action, set to query how many items the player is carrying</summary>
		 * <param name = "numItems">The number of items to check for</param>
		 * <param name = "condition">The condition to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventoryCheck CreateNew_NumberOfItemsCarrying (int numItems, IntCondition condition = IntCondition.EqualTo)
		{
			ActionInventoryCheck newAction = (ActionInventoryCheck) CreateInstance <ActionInventoryCheck>();
			newAction.invCheckType = InvCheckType.NumberOfItemsCarrying;
			newAction.intValue = numItems;
			newAction.intCondition = condition;
			return newAction;
		}
		
	}

}