/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionVarCopy.cs"
 * 
 *	This action is used to transfer the value of one Variable to another
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
	public class ActionVarCopy : Action
	{
		
		public int oldParameterID = -1;
		public int oldVariableID;
		public VariableLocation oldLocation;

		public int newParameterID = -1;
		public int newVariableID;
		public VariableLocation newLocation;

		public Variables oldVariables;
		public int oldVariablesConstantID = 0;

		public Variables newVariables;
		public int newVariablesConstantID = 0;

		#if UNITY_EDITOR
		protected VariableType oldVarType = VariableType.Boolean;
		protected VariableType newVarType = VariableType.Boolean;
		#endif

		protected LocalVariables localVariables;
		protected GVar oldRuntimeVariable;
		protected GVar newRuntimeVariable;
		protected Variables newRuntimeVariables;


		public ActionVarCopy ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Variable;
			title = "Copy";
			description = "Copies the value of one Variable to another. This can be between Global and Local Variables, but only of those with the same type, such as Integer or Float.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			oldRuntimeVariable = null;
			switch (oldLocation)
			{
				case VariableLocation.Global:
					oldVariableID = AssignVariableID (parameters, oldParameterID, oldVariableID);
					oldRuntimeVariable = GlobalVariables.GetVariable (oldVariableID);
					break;

				case VariableLocation.Local:
					if (!isAssetFile)
					{
						oldVariableID = AssignVariableID (parameters, oldParameterID, oldVariableID);
						oldRuntimeVariable = LocalVariables.GetVariable (oldVariableID, localVariables);
					}
					break;

				case VariableLocation.Component:
					Variables oldRuntimeVariables = AssignFile <Variables> (oldVariablesConstantID, oldVariables);
					if (oldRuntimeVariables != null)
					{
						oldRuntimeVariable = oldRuntimeVariables.GetVariable (oldVariableID);
					}
					oldRuntimeVariable = AssignVariable (parameters, oldParameterID, oldRuntimeVariable);
					break;
			}

			newRuntimeVariable = null;
			switch (newLocation)
			{
				case VariableLocation.Global:
					newVariableID = AssignVariableID (parameters, newParameterID, newVariableID);
					newRuntimeVariable = GlobalVariables.GetVariable (newVariableID, true);
					break;

				case VariableLocation.Local:
					if (!isAssetFile)
					{
						newVariableID = AssignVariableID (parameters, newParameterID, newVariableID);
						newRuntimeVariable = LocalVariables.GetVariable (newVariableID, localVariables);
					}
					break;

				case VariableLocation.Component:
					newRuntimeVariables = AssignFile <Variables> (newVariablesConstantID, newVariables);
					if (newRuntimeVariables != null)
					{
						newRuntimeVariable = newRuntimeVariables.GetVariable (newVariableID);
					}
					newRuntimeVariable = AssignVariable (parameters, newParameterID, newRuntimeVariable);
					newRuntimeVariables = AssignVariablesComponent (parameters, newParameterID, newRuntimeVariables);
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
			if (oldRuntimeVariable != null && newRuntimeVariable != null)
			{
				CopyVariable (newRuntimeVariable, oldRuntimeVariable);
				newRuntimeVariable.Upload (newLocation, newRuntimeVariables);
			}

			return 0f;
		}

		
		protected void CopyVariable (GVar newVar, GVar oldVar)
		{
			if (newVar == null || oldVar == null)
			{
				LogWarning ("Cannot copy variable since it cannot be found!");
				return;
			}

			newVar.CopyFromVariable (oldVar, oldLocation);
			KickStarter.actionListManager.VariableChanged ();
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			// OLD

			oldLocation = (VariableLocation) EditorGUILayout.EnumPopup ("'From' source:", oldLocation);

			switch (oldLocation)
			{
				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager != null)
					{
						oldVariableID = ShowVarGUI (AdvGame.GetReferences ().variablesManager.vars, parameters, ParameterType.GlobalVariable, oldVariableID, oldParameterID, false);
					}
					break;

				case VariableLocation.Local:
					if (isAssetFile)
					{
						EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
					}
					else if (localVariables != null)
					{
						oldVariableID = ShowVarGUI (localVariables.localVars, parameters, ParameterType.LocalVariable, oldVariableID, oldParameterID, false);
					}
					else
					{
						EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
					}
					break;

				case VariableLocation.Component:
					oldParameterID = Action.ChooseParameterGUI ("'From' variable:", parameters, oldParameterID, ParameterType.ComponentVariable);
					if (oldParameterID >= 0)
					{
						oldVariables = null;
						oldVariablesConstantID = 0;	
					}
					else
					{
						oldVariables = (Variables) EditorGUILayout.ObjectField ("'Old' component:", oldVariables, typeof (Variables), true);
						if (oldVariables != null)
						{
							oldVariableID = ShowVarGUI (oldVariables.vars, null, ParameterType.ComponentVariable, oldVariableID, oldParameterID, false);
						}

						oldVariablesConstantID = FieldToID <Variables> (oldVariables, oldVariablesConstantID);
						oldVariables = IDToField <Variables> (oldVariables, oldVariablesConstantID, false);
					}
					break;
			}

			EditorGUILayout.Space ();

			// NEW

			newLocation = (VariableLocation) EditorGUILayout.EnumPopup ("'To' source:", newLocation);

			switch (newLocation)
			{
				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager != null)
					{
						newVariableID = ShowVarGUI (AdvGame.GetReferences ().variablesManager.vars, parameters, ParameterType.GlobalVariable, newVariableID, newParameterID, true);
					}
					break;

				case VariableLocation.Local:
					if (isAssetFile)
					{
						EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
					}
					else if (localVariables != null)
					{
						newVariableID = ShowVarGUI (localVariables.localVars, parameters, ParameterType.LocalVariable, newVariableID, newParameterID, true);
					}
					else
					{
						EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
					}
					break;

				case VariableLocation.Component:
					newParameterID = Action.ChooseParameterGUI ("'To' variable:", parameters, newParameterID, ParameterType.ComponentVariable);
					if (newParameterID >= 0)
					{
						newVariables = null;
						newVariablesConstantID = 0;	
					}
					else
					{
						newVariables = (Variables) EditorGUILayout.ObjectField ("'New' component:", newVariables, typeof (Variables), true);
						if (newVariables != null)
						{
							newVariableID = ShowVarGUI (oldVariables.vars, null, ParameterType.ComponentVariable, newVariableID, newParameterID, false);
						}

						newVariablesConstantID = FieldToID <Variables> (newVariables, newVariablesConstantID);
						newVariables = IDToField <Variables> (newVariables, newVariablesConstantID, false);
					}
					break;
			}

			// Types match?
			if (oldParameterID == -1 && newParameterID == -1 && newVarType != oldVarType)
			{
				EditorGUILayout.HelpBox ("The chosen Variables do not share the same Type - a conversion will be attemped", MessageType.Info);
			}

			AfterRunningOption ();
		}


		private int ShowVarGUI (List<GVar> vars, List<ActionParameter> parameters, ParameterType parameterType, int variableID, int parameterID, bool isNew)
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
				
				if (variableNumber == -1 && (parameters == null || parameters.Count == 0 || parameterID == -1))
				{
					// Wasn't found (variable was deleted?), so revert to zero
					ACDebug.LogWarning ("Previously chosen variable no longer exists!");
					variableNumber = 0;
					variableID = 0;
				}

				string label = "'From' variable:";
				if (isNew)
				{
					label = "'To' variable:";
				}

				parameterID = Action.ChooseParameterGUI (label, parameters, parameterID, parameterType);
				if (parameterID >= 0)
				{
					//variableNumber = 0;
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

			if (isNew)
			{
				newParameterID = parameterID;

				if (variableNumber >= 0)
				{
					newVarType = vars[variableNumber].type;
				}
			}
			else
			{
				oldParameterID = parameterID;

				if (variableNumber >= 0)
				{
					oldVarType = vars[variableNumber].type;
				}
			}

			return variableID;
		}


		public override string SetLabel ()
		{
			switch (newLocation)
			{
				case VariableLocation.Local:
					if (!isAssetFile && localVariables)
					{
						return GetLabelString (localVariables.localVars, newVariableID);
					}
					break;

				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager)
					{
						return GetLabelString (AdvGame.GetReferences ().variablesManager.vars, newVariableID);
					}
					break;

				case VariableLocation.Component:
					if (newVariables != null)
					{
						return GetLabelString (newVariables.vars, newVariableID);
					}
					break;
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

			if (oldLocation == VariableLocation.Local && oldVariableID == oldLocalID)
			{
				oldLocation = VariableLocation.Global;
				oldVariableID = newGlobalID;
				wasAmended = true;
			}

			if (newLocation == VariableLocation.Local && newVariableID == oldLocalID)
			{
				newLocation = VariableLocation.Global;
				newVariableID = newGlobalID;
				wasAmended = true;
			}

			return wasAmended;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool wasAmended = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			if (oldLocation == VariableLocation.Global && oldVariableID == oldGlobalID)
			{
				wasAmended = true;
				if (isCorrectScene)
				{
					oldLocation = VariableLocation.Local;
					oldVariableID = newLocalID;
				}
			}

			if (newLocation == VariableLocation.Global && newVariableID == oldGlobalID)
			{
				wasAmended = true;
				if (isCorrectScene)
				{
					newLocation = VariableLocation.Local;
					newVariableID = newLocalID;
				}
			}

			return wasAmended;
		}


		public override int GetVariableReferences (List<ActionParameter> parameters, VariableLocation _location, int varID, Variables _variables)
		{
			int thisCount = 0;

			if (oldLocation == _location && oldVariableID == varID)
			{
				if (_location != VariableLocation.Component || (_variables != null && oldVariables == _variables))
				{
					thisCount ++;
				}
			}

			if (newLocation == _location && newVariableID == varID)
			{
				if (_location != VariableLocation.Component || (_variables != null && newVariables == _variables))
				{
					thisCount ++;
				}
			}

			thisCount += base.GetVariableReferences (parameters, _location, varID, _variables);
			return thisCount;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (oldLocation == VariableLocation.Component)
			{
				AssignConstantID <Variables> (oldVariables, oldVariablesConstantID, oldParameterID);
			}

			if (newLocation == VariableLocation.Component)
			{
				AssignConstantID <Variables> (newVariables, newVariablesConstantID, newParameterID);
			}
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (oldParameterID < 0 && oldLocation == VariableLocation.Component)
			{
				if (oldVariables != null && oldVariables.gameObject == gameObject) return true;
				if (oldVariablesConstantID == id) return true;
			}
			if (newParameterID < 0 && oldLocation == VariableLocation.Component)
			{
				if (newVariables != null && newVariables.gameObject == gameObject) return true;
				if (newVariablesConstantID == id) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Variable: Copy' Action</summary>
		 * <param name = "fromVariableLocation">The location of the variable to copy from</param>
		 * <param name = "fromVariables">The Variables component of the variable to copy from, if a Component Variable</param>
		 * <param name = "fromVariableID">The ID number of the variable to copy from</param>
		 * <param name = "toVariableLocation">The location of the variable to copy to</param>
		 * <param name = "toVariables">The Variables component of the variable to copy to, if a Component Variable</param>
		 * <param name = "toVariableID">The ID number of the variable to copy to</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCopy CreateNew (VariableLocation fromVariableLocation, Variables fromVariables, int fromVariableID, VariableLocation toVariableLocation, Variables toVariables, int toVariableID)
		{
			ActionVarCopy newAction = (ActionVarCopy) CreateInstance <ActionVarCopy>();
			newAction.oldLocation = fromVariableLocation;
			newAction.oldVariables = fromVariables;
			newAction.oldVariableID = fromVariableID;
			newAction.newLocation = toVariableLocation;
			newAction.newVariables = toVariables;
			newAction.newVariableID = toVariableID;
			return newAction;
		}

	}

}