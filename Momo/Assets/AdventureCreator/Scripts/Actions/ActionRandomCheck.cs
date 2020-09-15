/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionRandomCheck.cs"
 * 
 *	This action checks the value of a random number
 *	and performs different follow-up Actions accordingly.
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
	public class ActionRandomCheck : ActionCheckMultiple
	{

		public bool disallowSuccessive = false;
		public bool saveToVariable = true;
		protected int ownVarValue = -1;

		public int parameterID = -1;
		public int variableID;
		public VariableLocation location = VariableLocation.Global;

		public Variables variables;
		public int variablesConstantID;

		protected LocalVariables localVariables;
		protected GVar runtimeVariable = null;


		public ActionRandomCheck ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Variable;
			title = "Check random number";
			description = "Picks a number at random between zero and a specified integer – the value of which determine which subsequent Action is run next.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeVariable = null;

			if (saveToVariable)
			{
				switch (location)
				{
					case VariableLocation.Global:
						variableID = AssignVariableID (parameters, parameterID, variableID);
						runtimeVariable = GlobalVariables.GetVariable (variableID, true);
						break;

					case VariableLocation.Local:
						if (!isAssetFile)
						{
							variableID = AssignVariableID (parameters, parameterID, variableID);
							runtimeVariable = LocalVariables.GetVariable (variableID, localVariables);
						}
						break;

					case VariableLocation.Component:
						Variables runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
						if (runtimeVariables != null)
						{
							runtimeVariable = runtimeVariables.GetVariable (variableID);
						}
						runtimeVariable = AssignVariable (parameters, parameterID, runtimeVariable);
						break;
				}
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
		
		
		public override ActionEnd End (List<Action> actions)
		{
			if (numSockets <= 0)
			{
				LogWarning ("Could not compute Random check because no values were possible!");
				return GenerateStopActionEnd ();
			}

			int randomResult = Random.Range (0, numSockets);
			if (numSockets > 1 && disallowSuccessive)
			{
				if (saveToVariable)
				{
					if (runtimeVariable != null && runtimeVariable.type == VariableType.Integer)
					{
						ownVarValue = runtimeVariable.val;
					}
					else
					{
						LogWarning ("No Integer variable found!");
					}
				}

				while (ownVarValue == randomResult)
				{
					randomResult = Random.Range (0, numSockets);
				}

				ownVarValue = randomResult;

				if (saveToVariable && runtimeVariable != null && runtimeVariable.type == VariableType.Integer)
				{
					runtimeVariable.SetValue (ownVarValue);
				}
			}

			return ProcessResult (randomResult, actions);
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			numSockets = EditorGUILayout.DelayedIntField ("# of possible values:", numSockets);
			numSockets = Mathf.Clamp (numSockets, 1, 100);

			disallowSuccessive = EditorGUILayout.Toggle ("Prevent same value twice?", disallowSuccessive);

			if (disallowSuccessive)
			{
				saveToVariable = EditorGUILayout.Toggle ("Save last value?", saveToVariable);
				if (saveToVariable)
				{
					location = (VariableLocation) EditorGUILayout.EnumPopup ("Variable source:", location);

					if (location == VariableLocation.Local && KickStarter.localVariables == null)
					{
						EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
					}
					else if (location == VariableLocation.Local && isAssetFile)
					{
						EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
					}

					if ((location == VariableLocation.Global && AdvGame.GetReferences ().variablesManager != null) ||
						(location == VariableLocation.Local && KickStarter.localVariables != null && !isAssetFile) ||
						(location == VariableLocation.Component))
					{
						ParameterType _parameterType = ParameterType.GlobalVariable;
						if (location == VariableLocation.Local)
						{
							_parameterType = ParameterType.LocalVariable;
						}
						else if (location == VariableLocation.Component)
						{
							_parameterType = ParameterType.ComponentVariable;
						}

						parameterID = Action.ChooseParameterGUI ("Integer variable:", parameters, parameterID, _parameterType);
						if (parameterID >= 0)
						{
							if (location == VariableLocation.Component)
							{
								variablesConstantID = 0;
								variables = null;
							}

							variableID = ShowVarGUI (variableID, false);
						}
						else
						{
							if (location == VariableLocation.Component)
							{
								variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
								variablesConstantID = FieldToID <Variables> (variables, variablesConstantID);
								variables = IDToField <Variables> (variables, variablesConstantID, false);

								if (variables != null)
								{
									variableID = ShowVarGUI (variableID, true);
								}
							}
							else
							{
								EditorGUILayout.BeginHorizontal ();
								variableID = ShowVarGUI (variableID, true);

								if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
								{
									SideMenu ();
								}
								EditorGUILayout.EndHorizontal ();
							}
						}
					}
				}
			}
		}


		protected void SideMenu ()
		{
			GenericMenu menu = new GenericMenu ();

			menu.AddItem (new GUIContent ("Auto-create " + location.ToString () + " variable"), false, Callback, "AutoCreate");
			menu.ShowAsContext ();
		}
		
		
		protected void Callback (object obj)
		{
			switch (obj.ToString ())
			{
				case "AutoCreate":
					AutoCreateVariableWindow.Init ("Random/New integer", location, VariableType.Integer, this);
					break;

				case "Show":
					if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().variablesManager != null)
					{
						AdvGame.GetReferences ().variablesManager.ShowVariable (variableID, location);
					}
					break;
			}
		}


		protected int ShowVarGUI (int ID, bool changeID)
		{
			if (changeID)
			{
				switch (location)
				{
					case VariableLocation.Global:
						ID = AdvGame.GlobalVariableGUI ("Global integer:", ID, VariableType.Integer);
						break;

					case VariableLocation.Local:
						ID = AdvGame.LocalVariableGUI ("Local integer:", ID, VariableType.Integer);
						break;

					case VariableLocation.Component:
						ID = AdvGame.ComponentVariableGUI ("Component integer:", ID, VariableType.Integer, variables);
						break;
				}
			}

			return ID;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			if (saveToVariable)
			{
				if (location == VariableLocation.Local && variableID == oldLocalID)
				{
					location = VariableLocation.Global;
					variableID = newGlobalID;
					wasAmended = true;
				}
			}

			return wasAmended;
		}


		public override int GetVariableReferences (List<ActionParameter> parameters, VariableLocation _location, int varID, Variables _variables)
		{
			int thisCount = 0;
			if (saveToVariable && location == _location && variableID == varID && parameterID < 0)
			{
				if (location != VariableLocation.Component || (variables != null && variables == _variables))
				{
					thisCount ++;
				}
			}
			thisCount += base.GetVariableReferences (parameters, _location, varID, _variables);
			return thisCount;
		}
 

		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool isAffected = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			if (saveToVariable)
			{
				if (location == VariableLocation.Global && variableID == oldGlobalID)
				{
					isAffected = true;

					if (isCorrectScene)
					{
						location = VariableLocation.Local;
						variableID = newLocalID;
					}
				}
			}

			return isAffected;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveToVariable &&
				location == VariableLocation.Component)
			{
				AssignConstantID <Variables> (variables, variablesConstantID, parameterID);
			}
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (disallowSuccessive && saveToVariable && location == VariableLocation.Component && parameterID < 0)
			{
				if (variables != null && variables.gameObject == gameObject) return true;
				if (variablesConstantID == id) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Variable: Check random number' Action</summary>
		 * <param name = "numOutcomes">The number of possible outcomes</param>
		 * <param name = "disallowSuccessive">If True, the same value cannot be used twice in a row</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionRandomCheck CreateNew (int numOutcomes, bool disallowSuccessive)
		{
			ActionRandomCheck newAction = (ActionRandomCheck) CreateInstance <ActionRandomCheck>();
			newAction.numSockets = numOutcomes;
			newAction.disallowSuccessive = disallowSuccessive;
			return newAction;
		}
		
	}

}