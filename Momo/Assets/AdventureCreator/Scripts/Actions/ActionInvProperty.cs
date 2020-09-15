/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionVarProperty.cs"
 * 
 *	This action is used to set the value of a Variable as an Inventory item property.
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
	public class ActionInvProperty : Action
	{
		
		public int varParameterID = -1;
		public int variableID;
		public VariableLocation varLocation;

		#if UNITY_EDITOR
		private VariableType varType = VariableType.Boolean;
		private VariableType propertyType = VariableType.Boolean;
		#endif

		protected enum SetVarAsPropertyMethod { SpecificItem, SelectedItem };
		[SerializeField] protected SetVarAsPropertyMethod setVarAsPropertyMethod = SetVarAsPropertyMethod.SpecificItem;

		public bool multiplyByItemCount;
		public int invID;
		public int invParameterID;

		public int propertyID;

		protected LocalVariables localVariables;
		protected InventoryManager inventoryManager;

		public Variables variables;
		public int variablesConstantID = 0;

		protected GVar runtimeVariable;


		public ActionInvProperty ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Inventory;
			title = "Property to Variable";
			description = "Sets the value of a Variable as an Inventory item property.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			invID = AssignInvItemID (parameters, invParameterID, invID);

			runtimeVariable = null;
			switch (varLocation)
			{
				case VariableLocation.Global:
					variableID = AssignVariableID (parameters, varParameterID, variableID);
					runtimeVariable = GlobalVariables.GetVariable (variableID, true);
					break;

				case VariableLocation.Local:
					if (!isAssetFile)
					{
						variableID = AssignVariableID (parameters, varParameterID, variableID);
						runtimeVariable = LocalVariables.GetVariable (variableID, localVariables);
					}
					break;

				case VariableLocation.Component:
					Variables runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
					if (runtimeVariables != null)
					{
						runtimeVariable = runtimeVariables.GetVariable (variableID);
					}
					runtimeVariable = AssignVariable (parameters, varParameterID, runtimeVariable);
					break;
			}
		}


		public override void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
			}
			if (localVariables == null)
			{
				localVariables = KickStarter.localVariables;
			}

			base.AssignParentList (actionList);
		}


		public override float Run ()
		{
			int runtimeInvID = -1;

			if (setVarAsPropertyMethod == SetVarAsPropertyMethod.SelectedItem)
			{
				if (KickStarter.runtimeInventory.SelectedItem != null)
				{
					runtimeInvID = KickStarter.runtimeInventory.SelectedItem.id;
				}
			}
			else
			{
				runtimeInvID = invID;
			}

			InvVar invVar = null;
			if (runtimeInvID >= 0)
			{
				if (multiplyByItemCount)
				{
					invVar = KickStarter.runtimeInventory.GetPropertyTotals (propertyID, runtimeInvID);
				}
				else
				{
					InvItem invItem = KickStarter.inventoryManager.GetItem (runtimeInvID);
					if (invItem != null)
					{
						invVar = invItem.GetProperty (propertyID);
					}
				}
			}

			if (invVar == null)
			{
				LogWarning ("Cannot find property with ID " + propertyID + " on Inventory item ID " + runtimeInvID);
				return 0f;
			}

			if (runtimeVariable.type == VariableType.String)
			{
				runtimeVariable.textVal = invVar.GetDisplayValue (Options.GetLanguage ());
			}
			else if (runtimeVariable.type == invVar.type)
			{
				if (invVar.type == VariableType.Float)
				{
					runtimeVariable.FloatValue = invVar.FloatValue;
				}
				else if (invVar.type == VariableType.Vector3)
				{
					runtimeVariable.Vector3Value = invVar.Vector3Value;
				}
				else
				{
					runtimeVariable.IntegerValue = invVar.IntegerValue;
				}
			}
			else
			{
				LogWarning ("Cannot assign " + varLocation.ToString () + " Variable " + runtimeVariable.label + "'s value from '" + invVar.label + "' property because their types do not match.");
			}

			return 0;
		}


		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			inventoryManager = AdvGame.GetReferences ().inventoryManager;

			// Select Inventory item

			setVarAsPropertyMethod = (SetVarAsPropertyMethod) EditorGUILayout.EnumPopup ("Get property of:", setVarAsPropertyMethod);
			if (setVarAsPropertyMethod == SetVarAsPropertyMethod.SpecificItem)
			{
				ShowInvSelectGUI (parameters);
			}

			// Select item property
			ShowInvPropertyGUI ();

			EditorGUILayout.Space ();

			// Select variable
			varLocation = (VariableLocation) EditorGUILayout.EnumPopup ("Variable source:", varLocation);
			switch (varLocation)
			{
				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager)
					{
						ShowVarGUI (AdvGame.GetReferences ().variablesManager.vars, parameters, ParameterType.GlobalVariable);
					}
					break;

				case VariableLocation.Local:
					if (!isAssetFile)
					{
						if (localVariables)
						{
							ShowVarGUI (localVariables.localVars, parameters, ParameterType.LocalVariable);
						}
						else
						{
							EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
						}
					}
					else
					{
						EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
					}
					break;

				case VariableLocation.Component:
					varParameterID = Action.ChooseParameterGUI ("Component variable:", parameters, varParameterID, ParameterType.ComponentVariable);
					if (varParameterID >= 0)
					{
						variables = null;
						variablesConstantID = 0;	
					}
					else
					{
						variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
						variablesConstantID = FieldToID <Variables> (variables, variablesConstantID);
						variables = IDToField <Variables> (variables, variablesConstantID, false);

						if (variables != null)
						{
							ShowVarGUI (variables.vars, null, ParameterType.ComponentVariable);
						}
					}
					break;
			}

			if (inventoryManager != null && inventoryManager.invVars != null && inventoryManager.invVars.Count > 0)
			{
				if (varType != propertyType && varType != VariableType.String)
				{
					EditorGUILayout.HelpBox ("The chosen Inventory Item Property and Variable must share the same Type, or the Variable must be a String.", MessageType.Warning);
				}
			}

			AfterRunningOption ();
		}


		private void ShowInvPropertyGUI ()
		{
			if (inventoryManager != null)
			{
				// Create a string List of the field's names (for the PopUp box)
				List<string> labelList = new List<string>();

				int i = 0;
				int propertyNumber = 0;

				if (inventoryManager.invVars != null && inventoryManager.invVars.Count > 0)
				{
					foreach (InvVar invVar in inventoryManager.invVars)
					{
						labelList.Add (invVar.label);
						
						// If a property has been removed, make sure selected variable is still valid
						if (invVar.id == propertyID)
						{
							propertyNumber = i;
						}
						
						i++;
					}
					
					if (propertyNumber == -1)
					{
						// Wasn't found (property was possibly deleted), so revert to zero
						ACDebug.LogWarning ("Previously chosen property no longer exists!");
						
						propertyNumber = 0;
						propertyID = 0;
					}

					propertyNumber = EditorGUILayout.Popup ("Inventory property:", propertyNumber, labelList.ToArray());
					propertyID = inventoryManager.invVars[propertyNumber].id;
					propertyType = inventoryManager.invVars[propertyNumber].type;

					if (propertyType == VariableType.Integer || propertyType == VariableType.Float)
					{
						multiplyByItemCount = EditorGUILayout.Toggle ("Multiply by item count?", multiplyByItemCount);
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("No inventory properties exist!", MessageType.Info);
					propertyID = -1;
					propertyNumber = -1;
				}
			}
		}


		private void ShowInvSelectGUI (List<ActionParameter> parameters)
		{
			if (inventoryManager != null)
			{
				// Create a string List of the field's names (for the PopUp box)
				List<string> labelList = new List<string>();
				
				int i = 0;
				int invNumber = 0;
				if (invParameterID == -1)
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
				}
				else
				{
					EditorGUILayout.HelpBox ("No inventory items exist!", MessageType.Info);
					invID = -1;
					invNumber = -1;
				}
			}
		}


		private void ShowVarGUI (List<GVar> vars, List<ActionParameter> parameters, ParameterType parameterType)
		{
			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			
			int i = 0;
			int variableNumber = -1;

			if (vars.Count > 0)
			{
				foreach (GVar _var in vars)
				{
					labelList.Add (_var.label);
					
					// If a GlobalVar variable has been removed, make sure selected variable is still valid
					if (_var.id == variableID)
					{
						variableNumber = i;
					}
					
					i ++;
				}
				
				if (variableNumber == -1 && (parameters == null || parameters.Count == 0 || varParameterID == -1))
				{
					// Wasn't found (variable was deleted?), so revert to zero
					ACDebug.LogWarning ("Previously chosen variable no longer exists!");
					variableNumber = 0;
					variableID = 0;
				}

				string label = varLocation.ToString () + " variable:";
				varParameterID = Action.ChooseParameterGUI (label, parameters, varParameterID, parameterType);
				if (varParameterID >= 0)
				{
					variableNumber = Mathf.Min (variableNumber, vars.Count-1);
					variableID = -1;
				}
				else
				{
					variableNumber = EditorGUILayout.Popup (label, variableNumber, labelList.ToArray());
					variableID = vars [variableNumber].id;
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("No variables exist!", MessageType.Info);
				variableID = -1;
				variableNumber = -1;
			}

			if (variableNumber >= 0)
			{
				varType = vars[variableNumber].type;
			}
		}


		public override string SetLabel ()
		{
			if (varParameterID < 0)
			{
				switch (varLocation)
				{
					case VariableLocation.Global:
						if (AdvGame.GetReferences ().variablesManager)
						{
							return GetLabelString (AdvGame.GetReferences ().variablesManager.vars, variableID);
						}
						break;

					case VariableLocation.Local:
						if (!isAssetFile && localVariables != null)
						{
							return GetLabelString (localVariables.localVars, variableID);
						}
						break;

					case VariableLocation.Component:
						if (variables != null)
						{
							return GetLabelString (variables.vars, variableID);
						}
						break;
				}
			}

			return string.Empty;
		}


		private string GetLabelString (List<GVar> vars, int variableID)
		{
			if (vars.Count > 0)
			{
				foreach (GVar _var in vars)
				{
					if (_var.id == variableID)
					{
						return _var.label;
					}
				}
			}
			return string.Empty;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			if (varLocation == VariableLocation.Local && variableID == oldLocalID)
			{
				varLocation = VariableLocation.Global;
				variableID = newGlobalID;
				wasAmended = true;
			}

			return wasAmended;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool wasAmended = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			if (varLocation == VariableLocation.Global && variableID == oldGlobalID)
			{
				wasAmended = true;
				if (isCorrectScene)
				{
					varLocation = VariableLocation.Local;
					variableID = newLocalID;
				}
			}

			return wasAmended;
		}


		public override int GetVariableReferences (List<ActionParameter> parameters, VariableLocation _location, int varID, Variables _variables)
		{
			int thisCount = 0;

			if (varLocation == _location && variableID == varID && varParameterID < 0)
			{
				if (varLocation != VariableLocation.Component || (variables != null && variables == _variables))
				{
					thisCount ++;
				}
			}

			thisCount += base.GetVariableReferences (parameters, _location, varID, _variables);
			return thisCount;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (varLocation == VariableLocation.Component)
			{
				AssignConstantID <Variables> (variables, variablesConstantID, varParameterID);
			}
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (varLocation == VariableLocation.Component && varParameterID < 0)
			{
				if (variables != null && variables.gameObject == _gameObject) return true;
				if (variablesConstantID == id) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Inventory: Property to Variable' Action, set to transfer a property value to a Global variable</summary>
		 * <param name = "variableID">The ID number of the Global variable to update</param>
		 * <param name = "propertyID">The ID number of the inventory property to read</param>
		 * <param name = "itemID">If non-negative, the ID number of the inventory item to read.  Otherwise, the currently-selected item will be read</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInvProperty CreateNew_ToGlobalVariable (int variableID, int propertyID, int itemID = -1)
		{
			ActionInvProperty newAction = (ActionInvProperty) CreateInstance <ActionInvProperty>();
			newAction.setVarAsPropertyMethod = (itemID >= 0) ? SetVarAsPropertyMethod.SpecificItem : SetVarAsPropertyMethod.SelectedItem;
			newAction.propertyID = propertyID;
			newAction.varLocation = VariableLocation.Global;
			newAction.variableID = variableID;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Inventory: Property to Variable' Action, set to transfer a property value to a Local variable</summary>
		 * <param name = "variableID">The ID number of the Local variable to update</param>
		 * <param name = "propertyID">The ID number of the inventory property to read</param>
		 * <param name = "itemID">If non-negative, the ID number of the inventory item to read.  Otherwise, the currently-selected item will be read</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInvProperty CreateNew_ToLocalVariable (int variableID, int propertyID, int itemID = -1)
		{
			ActionInvProperty newAction = (ActionInvProperty) CreateInstance <ActionInvProperty>();
			newAction.setVarAsPropertyMethod = (itemID >= 0) ? SetVarAsPropertyMethod.SpecificItem : SetVarAsPropertyMethod.SelectedItem;
			newAction.propertyID = propertyID;
			newAction.varLocation = VariableLocation.Local;
			newAction.variableID = variableID;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Inventory: Property to Variable' Action, set to transfer a property value to a Component variable</summary>
		 * <param name = "variables">The Variables component that holds the variable</param>
		 * <param name = "variableID">The ID number of the Component variable to update</param>
		 * <param name = "propertyID">The ID number of the inventory property to read</param>
		 * <param name = "itemID">If non-negative, the ID number of the inventory item to read.  Otherwise, the currently-selected item will be read</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInvProperty CreateNew_ToComponentVariable (Variables variables, int variableID, int propertyID, int itemID = -1)
		{
			ActionInvProperty newAction = (ActionInvProperty) CreateInstance <ActionInvProperty>();
			newAction.setVarAsPropertyMethod = (itemID >= 0) ? SetVarAsPropertyMethod.SpecificItem : SetVarAsPropertyMethod.SelectedItem;
			newAction.propertyID = propertyID;
			newAction.varLocation = VariableLocation.Component;
			newAction.variableID = variableID;
			return newAction;
		}

	}

}