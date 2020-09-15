/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionContainerSet.cs"
 * 
 *	This action is used to add or remove items from a container,
 *	with items being defined in the Inventory Manager.
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
	public class ActionContainerSet : Action
	{
		
		public enum ContainerAction {Add, Remove, RemoveAll};
		public ContainerAction containerAction;

		public int invParameterID = -1;
		public int invID;
		protected int invNumber;

		public bool useActive = false;
		public int constantID = 0;
		public int parameterID = -1;
		public Container container;
		protected Container runtimeContainer;

		public bool setAmount = false;
		public int amountParameterID = -1;
		public int amount = 1;
		public bool transferToPlayer = false;
		public bool removeAllInstances = false;

		#if UNITY_EDITOR
		protected InventoryManager inventoryManager;
		#endif


		public ActionContainerSet ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Container;
			title = "Add or remove";
			description = "Adds or removes Inventory items from a Container.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			invID = AssignInvItemID (parameters, invParameterID, invID);
			amount = AssignInteger (parameters, amountParameterID, amount);

			if (useActive)
			{
				runtimeContainer = KickStarter.playerInput.activeContainer;
			}
			else
			{
				runtimeContainer = AssignFile <Container> (parameters, parameterID, constantID, container);
			}
		}

		
		public override float Run ()
		{
			if (runtimeContainer == null)
			{
				return 0f;
			}

			if (!setAmount)
			{
				amount = 1;
			}

			if (containerAction == ContainerAction.Add)
			{
				runtimeContainer.Add (invID, amount);
			}
			else if (containerAction == ContainerAction.Remove)
			{
				ContainerItem[] containerItems = runtimeContainer.GetItemsWithInvID (invID);

				foreach (ContainerItem containerItem in containerItems)
				{
					ContainerItem backUpItem = new ContainerItem (containerItem);

					// Prevent if player is already carrying one, and multiple can't be carried
					InvItem invItem = KickStarter.inventoryManager.GetItem (containerItem.linkedID);
					if (KickStarter.runtimeInventory.IsCarryingItem (invItem.id) && !invItem.canCarryMultiple)
					{
						continue;
					}

					if (transferToPlayer && KickStarter.runtimeInventory.CanTransferContainerItemsToInventory (containerItem))
					{
						int _amount = Mathf.Min (amount, containerItem.count);
						KickStarter.runtimeInventory.Add (containerItem.linkedID, _amount);
					}

					runtimeContainer.Remove (containerItem, amount);

					KickStarter.eventManager.Call_OnUseContainer (false, runtimeContainer, backUpItem);

					if (!removeAllInstances)
					{
						break;
					}
				}
			}
			else if (containerAction == ContainerAction.RemoveAll)
			{
				if (transferToPlayer)
				{
					for (int i=0; i<runtimeContainer.items.Count; i++)
					{
						ContainerItem containerItem = runtimeContainer.items[i];
						if (!containerItem.IsEmpty)
						{
							ContainerItem backUpItem = new ContainerItem (containerItem);

							// Prevent if player is already carrying one, and multiple can't be carried
							InvItem invItem = KickStarter.inventoryManager.GetItem (containerItem.linkedID);
							if (KickStarter.runtimeInventory.IsCarryingItem (invItem.id) && !invItem.canCarryMultiple)
							{
								continue;
							}

							if (!KickStarter.runtimeInventory.CanTransferContainerItemsToInventory (containerItem))
							{
								continue;
							}

							KickStarter.runtimeInventory.Add (containerItem.linkedID, containerItem.count, false, -1);
							runtimeContainer.items.Remove (containerItem);
							i=-1;

							KickStarter.eventManager.Call_OnUseContainer (false, runtimeContainer, backUpItem);
						}
					}
				}
				else
				{
					runtimeContainer.RemoveAll ();
				}
			}

			return 0f;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (AdvGame.GetReferences ().inventoryManager)
			{
				inventoryManager = AdvGame.GetReferences ().inventoryManager;
			}
			
			if (inventoryManager)
			{
				// Create a string List of the field's names (for the PopUp box)
				List<string> labelList = new List<string>();
				
				int i = 0;
				if (invParameterID == -1)
				{
					invNumber = -1;
				}
				
				if (inventoryManager.items.Count > 0)
				{
					foreach (InvItem _item in inventoryManager.items)
					{
						labelList.Add (_item.label);
						
						// If a item has been removed, make sure selected variable is still valid
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

					useActive = EditorGUILayout.Toggle ("Affect active container?", useActive);
					if (!useActive)
					{
						parameterID = Action.ChooseParameterGUI ("Container:", parameters, parameterID, ParameterType.GameObject);
						if (parameterID >= 0)
						{
							constantID = 0;
							container = null;
						}
						else
						{
							container = (Container) EditorGUILayout.ObjectField ("Container:", container, typeof (Container), true);
							
							constantID = FieldToID <Container> (container, constantID);
							container = IDToField <Container> (container, constantID, false);
						}
					}

					containerAction = (ContainerAction) EditorGUILayout.EnumPopup ("Method:", containerAction);

					if (containerAction == ContainerAction.RemoveAll)
					{
						transferToPlayer = EditorGUILayout.Toggle ("Transfer to Player?", transferToPlayer);
					}
					else
					{
						invParameterID = Action.ChooseParameterGUI ("Inventory item:", parameters, invParameterID, ParameterType.InventoryItem);
						if (invParameterID >= 0)
						{
							invNumber = Mathf.Min (invNumber, inventoryManager.items.Count-1);
							invID = -1;
						}
						else
						{
							invNumber = EditorGUILayout.Popup ("Inventory item:", invNumber, labelList.ToArray());
							invID = inventoryManager.items[invNumber].id;
						}
						
						if (containerAction == ContainerAction.Remove)
						{
							transferToPlayer = EditorGUILayout.Toggle ("Transfer to Player?", transferToPlayer);
							removeAllInstances = EditorGUILayout.Toggle ("Remove all instances?", removeAllInstances);
						}

						if (inventoryManager.items[invNumber].canCarryMultiple)
						{
							if (containerAction == ContainerAction.Remove && removeAllInstances)
							{}
							else
							{
								setAmount = EditorGUILayout.Toggle ("Set amount?", setAmount);
								if (setAmount)
								{
									string _label = (containerAction == ContainerAction.Add) ? "Increase count by:" : "Reduce count by:";

									amountParameterID = Action.ChooseParameterGUI (_label, parameters, amountParameterID, ParameterType.Integer);
									if (amountParameterID < 0)
									{
										amount = EditorGUILayout.IntField (_label, amount);
									}
								}
							}
						}
					}

					AfterRunningOption ();
				}
		
				else
				{
					EditorGUILayout.LabelField ("No inventory items exist!");
					invID = -1;
					invNumber = -1;
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberContainer> (container);
			}
			AssignConstantID <Container> (container, constantID, parameterID);
		}
		
		
		public override string SetLabel ()
		{
			string labelItem = string.Empty;

			if (inventoryManager == null)
			{
				inventoryManager = AdvGame.GetReferences ().inventoryManager;
			}

			if (inventoryManager != null)
			{
				if (inventoryManager.items.Count > 0)
				{
					if (invNumber > -1)
					{
						labelItem = " " + inventoryManager.items[invNumber].label;
					}
				}
			}
			
			if (containerAction == ContainerAction.Add)
			{
				return "Add" + labelItem;
			}
			else if (containerAction == ContainerAction.Remove)
			{
				return "Remove" + labelItem;
			}
			else if (containerAction == ContainerAction.RemoveAll)
			{
				return "Remove all";
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!useActive && parameterID < 0)
			{
				if (container != null && container.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			return false;
		}

		#endif


		/**
		* <summary>Creates a new instance of the 'Containter: Add or remove' Action, set to add an inventory item to a Constainer</summary>
		* <param name = "containerToModify">The Container to modify</param>
		* <param name = "itemIDToAdd">The ID of the inventory item to add to the Container</param>
		* <param name = "instancesToAdd">If multiple instances of the item can be held, the number to be added</param>
		* <returns>The generated Action</returns>
		*/
		public static ActionContainerSet CreateNew_Add (Container containerToModify, int itemIDToAdd, int instancesToAdd = 1)
		{
			ActionContainerSet newAction = (ActionContainerSet) CreateInstance <ActionContainerSet>();
			newAction.containerAction = ContainerAction.Add;
			newAction.container = containerToModify;
			newAction.invID = itemIDToAdd;
			newAction.setAmount = true;
			newAction.amount = instancesToAdd;

			return newAction;
		}


		/**
		* <summary>Creates a new instance of the 'Containter: Add or remove' Action, set to remove an inventory item from a Constainer</summary>
		* <param name = "containerToModify">The Container to modify</param>
		* <param name = "itemIDToRemove">The ID of the inventory item to remove from the Container</param>
		* <param name = "instancesToAdd">If multiple instances of the item can be held, the number to be from</param>
		* <param name = "transferToPlayer">If True, the current Player will receive the item</param>
		* <returns>The generated Action</returns>
		*/
		public static ActionContainerSet CreateNew_Remove (Container containerToModify, int itemIDToRemove, int instancesToRemove = 1, bool transferToPlayer = false)
		{
			ActionContainerSet newAction = (ActionContainerSet) CreateInstance <ActionContainerSet>();
			newAction.containerAction = ContainerAction.Remove;
			newAction.container = containerToModify;
			newAction.invID = itemIDToRemove;
			newAction.setAmount = true;
			newAction.amount = instancesToRemove;
			newAction.transferToPlayer = transferToPlayer;

			return newAction;
		}


		/**
		* <summary>Creates a new instance of the 'Containter: Add or remove' Action, set to remove all inventory items in a Constainer</summary>
		* <param name = "containerToModify">The Container to modify</param>
		* <param name = "transferToPlayer">If True, the current Player will receive the items</param>
		* <returns>The generated Action</returns>
		*/
		public static ActionContainerSet CreateNew_RemoveAll (Container containerToModify, bool transferToPlayer = false)
		{
			ActionContainerSet newAction = (ActionContainerSet) CreateInstance <ActionContainerSet>();
			newAction.containerAction = ContainerAction.RemoveAll;
			newAction.container = containerToModify;
			newAction.transferToPlayer = transferToPlayer;

			return newAction;
		}

	}

}