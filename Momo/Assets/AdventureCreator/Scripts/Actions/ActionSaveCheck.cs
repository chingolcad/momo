/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionSaveCheck.cs"
 * 
 *	This Action creates and deletes save game profiles
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
	public class ActionSaveCheck : ActionCheck
	{

		public SaveCheck saveCheck = SaveCheck.NumberOfSaveGames;
		public bool includeAutoSaves = true;
		public bool checkByElementIndex = false;
		public bool checkByName = false;

		public int intValue;
		public int checkParameterID = -1;
		public IntCondition intCondition;

		public string menuName = "";
		public string elementName = "";

		public int profileVarID;


		public ActionSaveCheck ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Save;
			title = "Check";
			description = "Queries the number of save files or profiles created, or if saving is possible.";
		}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			intValue = AssignInteger (parameters, checkParameterID, intValue);
		}
		
		
		public override ActionEnd End (List<Action> actions)
		{
			int actualNumber = 0;

			if (saveCheck == SaveCheck.NumberOfSaveGames)
			{
				actualNumber = KickStarter.saveSystem.GetNumSaves (includeAutoSaves);
			}
			else if (saveCheck == SaveCheck.NumberOfProfiles)
			{
				actualNumber = KickStarter.options.GetNumProfiles ();
			}
			else if (saveCheck == SaveCheck.IsSlotEmpty)
			{
				return ProcessResult (!SaveSystem.DoesSaveExist (intValue, intValue, !checkByElementIndex), actions);
			}
			else if (saveCheck == SaveCheck.DoesProfileExist)
			{
				if (checkByElementIndex)
				{
					int i = Mathf.Max (0, intValue);
					bool includeActive = true;
					if (menuName != "" && elementName != "")
					{
						MenuElement menuElement = PlayerMenus.GetElementWithName (menuName, elementName);
						if (menuElement != null && menuElement is MenuProfilesList)
						{
							MenuProfilesList menuProfilesList = (MenuProfilesList) menuElement;

							if (menuProfilesList.fixedOption)
							{
								LogWarning ("Cannot refer to ProfilesList " + elementName + " in Menu " + menuName + ", as it lists a fixed profile ID only!");
								return ProcessResult (false, actions);
							}

							i += menuProfilesList.GetOffset ();
							includeActive = menuProfilesList.showActive;
						}
						else
						{
							LogWarning ("Cannot find ProfilesList element '" + elementName + "' in Menu '" + menuName + "'.");
						}
					}
					else
					{
						LogWarning ("No ProfilesList element referenced when trying to delete profile slot " + i.ToString ());
					}

					bool result = KickStarter.options.DoesProfileExist (i, includeActive);
					return ProcessResult (result, actions);
				}
				else
				{
					// intValue is the profile ID
					bool result = Options.DoesProfileIDExist (intValue);
					return ProcessResult (result, actions);
				}
			}
			else if (saveCheck == SaveCheck.DoesProfileNameExist)
			{
				bool result = false;

				GVar gVar = GlobalVariables.GetVariable (profileVarID);
				if (gVar != null)
				{
					string profileName = gVar.textVal;
					result = KickStarter.options.DoesProfileExist (profileName);
				}
				else
				{
					LogWarning ("Could not check for profile name - no variable found.");
				}

				return ProcessResult (result, actions);
			}
			else if (saveCheck == SaveCheck.IsSavingPossible)
			{
				return ProcessResult (!PlayerMenus.IsSavingLocked (this), actions);
			}

			return ProcessResult (CheckCondition (actualNumber), actions);
		}
		
		
		protected bool CheckCondition (int fieldValue)
		{
			if (intCondition == IntCondition.EqualTo)
			{
				if (fieldValue == intValue)
				{
					return true;
				}
			}
			else if (intCondition == IntCondition.NotEqualTo)
			{
				if (fieldValue != intValue)
				{
					return true;
				}
			}
			else if (intCondition == IntCondition.LessThan)
			{
				if (fieldValue < intValue)
				{
					return true;
				}
			}
			else if (intCondition == IntCondition.MoreThan)
			{
				if (fieldValue > intValue)
				{
					return true;
				}
			}

			return false;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			saveCheck = (SaveCheck) EditorGUILayout.EnumPopup ("Check to make:", saveCheck);
			if (saveCheck == SaveCheck.NumberOfSaveGames)
			{
				includeAutoSaves = EditorGUILayout.Toggle ("Include auto-save?", includeAutoSaves);
			}

			if (saveCheck == SaveCheck.IsSlotEmpty)
			{
				checkByElementIndex = EditorGUILayout.Toggle ("Check by menu slot index?", checkByElementIndex);

				string intValueLabel = (checkByElementIndex) ? "SavesList slot index:" : "Save ID:";
				checkParameterID = Action.ChooseParameterGUI (intValueLabel, parameters, checkParameterID, ParameterType.Integer);
				if (checkParameterID < 0)
				{
					intValue = EditorGUILayout.IntField (intValueLabel, intValue);
				}
			}
			else if (saveCheck == SaveCheck.DoesProfileExist)
			{
				checkByElementIndex = EditorGUILayout.ToggleLeft ("Check by menu slot index?", checkByElementIndex);

				string intValueLabel = (checkByElementIndex) ? "ProfilesList slot index:" : "Profile ID:";
				checkParameterID = Action.ChooseParameterGUI (intValueLabel, parameters, checkParameterID, ParameterType.Integer);
				if (checkParameterID < 0)
				{
					intValue = EditorGUILayout.IntField (intValueLabel, intValue);
				}

				if (checkByElementIndex)
				{
					EditorGUILayout.Space ();
					menuName = EditorGUILayout.TextField ("Menu with ProfilesList:", menuName);
					elementName = EditorGUILayout.TextField ("ProfilesList element:", elementName);
				}
			}
			else if (saveCheck == SaveCheck.DoesProfileNameExist)
			{
				profileVarID = AdvGame.GlobalVariableGUI ("String variable with name:", profileVarID, VariableType.String);
			}
			else if (saveCheck != SaveCheck.IsSavingPossible)
			{
				intCondition = (IntCondition) EditorGUILayout.EnumPopup ("Value is:", intCondition);
				checkParameterID = Action.ChooseParameterGUI ("Integer:", parameters, checkParameterID, ParameterType.Integer);
				if (checkParameterID < 0)
				{
					intValue = EditorGUILayout.IntField ("Integer:", intValue);
				}
			}
		}
		
		
		public override string SetLabel ()
		{
			return saveCheck.ToString ();
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Save: Check' Action, set to query the number of save files</summary>
		 * <param name = "numSaves">The number of save files to check for</param>
		 * <param name = "includeAutosave">If True, the Autosave will be included in the total number of save files</param>
		 * <param name = "condition">The condition to query</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveCheck CreateNew_NumberOfSaveGames (int numSaves, bool includeAutosave = true, IntCondition condition = IntCondition.EqualTo)
		{
			ActionSaveCheck newAction = (ActionSaveCheck) CreateInstance <ActionSaveCheck>();
			newAction.saveCheck = SaveCheck.NumberOfSaveGames;
			newAction.intValue = numSaves;
			newAction.intCondition = condition;
			newAction.includeAutoSaves = includeAutosave;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Check' Action, set to query the number of profiles</summary>
		 * <param name = "numSaves">The number of profiles to check for</param>
		 * <param name = "condition">The condition to query</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveCheck CreateNew_NumberOfProfiles (int numProfiles, IntCondition condition = IntCondition.EqualTo)
		{
			ActionSaveCheck newAction = (ActionSaveCheck) CreateInstance <ActionSaveCheck>();
			newAction.saveCheck = SaveCheck.NumberOfProfiles;
			newAction.intValue = numProfiles;
			newAction.intCondition = condition;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Check' Action, set to query if a save slot is empty</summary>
		 * <param name = "saveSlotID">The ID number of the save slot to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveCheck CreateNew_IsSlotEmpty (int saveSlotID)
		{
			ActionSaveCheck newAction = (ActionSaveCheck) CreateInstance <ActionSaveCheck>();
			newAction.saveCheck = SaveCheck.IsSlotEmpty;
			newAction.intValue = saveSlotID;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Check' Action, set to query if saving is currently possible</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveCheck CreateNew_IsSavingPossible ()
		{
			ActionSaveCheck newAction = (ActionSaveCheck) CreateInstance <ActionSaveCheck>();
			newAction.saveCheck = SaveCheck.IsSavingPossible;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Check' Action, set to query if a particular profile exists</summary>
		 * <param name = "profileSlotID">The ID number of the profile to check</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveCheck CreateNew_DoesProfileExist (int profileSlotID)
		{
			ActionSaveCheck newAction = (ActionSaveCheck) CreateInstance <ActionSaveCheck>();
			newAction.saveCheck = SaveCheck.DoesProfileExist;
			newAction.intValue = profileSlotID;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Check' Action, set to query if a particular profile exists</summary>
		 * <param name = "globalStringVariableIDWithName">The ID number of a Global String variable whose value matches the profile's name</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveCheck DoesProfileNameExist (int globalStringVariableIDWithName)
		{
			ActionSaveCheck newAction = (ActionSaveCheck) CreateInstance <ActionSaveCheck>();
			newAction.saveCheck = SaveCheck.DoesProfileNameExist;
			newAction.profileVarID = globalStringVariableIDWithName;
			return newAction;
		}

	}
	
}