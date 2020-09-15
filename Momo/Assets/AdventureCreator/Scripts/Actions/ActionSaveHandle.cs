/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionSaveHandle.cs"
 * 
 *	This Action saves and loads save game files
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
	public class ActionSaveHandle : Action
	{

		public SaveHandling saveHandling = SaveHandling.LoadGame;
		public SelectSaveType selectSaveType = SelectSaveType.Autosave;

		public int saveIndex = 0;
		public int saveIndexParameterID = -1;
		
		public int varID;
		public int slotVarID;
		
		public string menuName = "";
		public string elementName = "";

		public bool updateLabel = false;
		public bool customLabel = false;

		public bool doSelectiveLoad = false;
		public SelectiveLoad selectiveLoad = new SelectiveLoad ();
		protected bool recievedCallback;

		
		public ActionSaveHandle ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Save;
			title = "Save or load";
			description = "Saves and loads save-game files";
		}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			saveIndex = AssignInteger (parameters, saveIndexParameterID, saveIndex);
		}
		
		
		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;
				recievedCallback = false;

				PerformSaveOrLoad ();
			}

			if (recievedCallback)
			{
				isRunning = false;
				return 0f;
			}

			return defaultPauseTime;
		}


		protected void PerformSaveOrLoad ()
		{
			ClearAllEvents ();

			if (saveHandling == SaveHandling.ContinueFromLastSave || saveHandling == SaveHandling.LoadGame)
			{
				EventManager.OnFinishLoading += OnFinishLoading;
				EventManager.OnFailLoading += OnFail;
			}
			else if (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame)
			{
				EventManager.OnFinishSaving += OnFinishSaving;
				EventManager.OnFailSaving += OnFail;
			}

			if ((saveHandling == SaveHandling.LoadGame || saveHandling == SaveHandling.ContinueFromLastSave) && doSelectiveLoad)
			{
				KickStarter.saveSystem.SetSelectiveLoadOptions (selectiveLoad);
			}

			string newSaveLabel = "";
			if (customLabel && ((updateLabel && saveHandling == SaveHandling.OverwriteExistingSave) || saveHandling == AC.SaveHandling.SaveNewGame))
			{
				if (selectSaveType != SelectSaveType.Autosave)
				{
					GVar gVar = GlobalVariables.GetVariable (varID);
					if (gVar != null)
					{
						newSaveLabel = gVar.GetValue (Options.GetLanguage ());
					}
					else
					{
						LogWarning ("Could not " + saveHandling.ToString () + " - no variable found.");
						return;
					}
				}
			}
			
			int i = Mathf.Max (0, saveIndex);

			if (saveHandling == SaveHandling.ContinueFromLastSave)
			{
				SaveSystem.ContinueGame ();
				return;
			}

			if (saveHandling == SaveHandling.LoadGame || saveHandling == SaveHandling.OverwriteExistingSave)
			{
				if (selectSaveType == SelectSaveType.Autosave)
				{
					if (saveHandling == SaveHandling.LoadGame)
					{
						SaveSystem.LoadAutoSave ();
						return;
					}
					else
					{
						if (PlayerMenus.IsSavingLocked (this, true))
						{
							OnComplete ();
						}
						else
						{
							SaveSystem.SaveAutoSave ();
						}
						return;
					}
				}
				else if (selectSaveType == SelectSaveType.SlotIndexFromVariable)
				{
					GVar gVar = GlobalVariables.GetVariable (slotVarID);
					if (gVar != null)
					{
						i = gVar.val;
					}
					else
					{
						LogWarning ("Could not get save slot index - no variable found.");
						return;
					}
				}
			}

			if (selectSaveType != SelectSaveType.Autosave && selectSaveType != SelectSaveType.SetSaveID)
			{
				if (!string.IsNullOrEmpty (menuName) && !string.IsNullOrEmpty (elementName))
				{
					MenuElement menuElement = PlayerMenus.GetElementWithName (menuName, elementName);
					if (menuElement != null && menuElement is MenuSavesList)
					{
						MenuSavesList menuSavesList = (MenuSavesList) menuElement;
						i += menuSavesList.GetOffset ();
					}
					else
					{
						LogWarning ("Cannot find ProfilesList element '" + elementName + "' in Menu '" + menuName + "'.");
					}
				}
				else
				{
					LogWarning ("No SavesList element referenced when trying to find slot slot " + i.ToString ());
				}
			}
			
			if (saveHandling == SaveHandling.LoadGame)
			{
				if (selectSaveType == SelectSaveType.SetSaveID)
				{
					SaveSystem.LoadGame (i);
				}
				else
				{
					SaveSystem.LoadGame (i, -1, false);
				}
			}
			else if (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame)
			{
				if (PlayerMenus.IsSavingLocked (this, true))
				{
					OnComplete ();
				}
				else
				{
					if (saveHandling == SaveHandling.OverwriteExistingSave)
					{
						if (selectSaveType == SelectSaveType.SetSaveID)
						{
							SaveSystem.SaveGame (0, i, true, updateLabel, newSaveLabel);
						}
						else
						{
							SaveSystem.SaveGame (i, -1, false, updateLabel, newSaveLabel);
						}
					}
					else if (saveHandling == SaveHandling.SaveNewGame)
					{
						SaveSystem.SaveNewGame (updateLabel, newSaveLabel);
					}
				}
			}
		}


		protected void OnFinishLoading ()
		{
			OnComplete ();
		}


		protected void OnFinishSaving (SaveFile saveFile)
		{
			OnComplete ();
		}


		protected void OnComplete ()
		{
			ClearAllEvents ();
			recievedCallback = true;
		}


		protected void OnFail (int saveID)
		{
			OnComplete ();
		}


		protected void ClearAllEvents ()
		{
			EventManager.OnFinishLoading -= OnFinishLoading;
			EventManager.OnFailLoading -= OnFail;

			EventManager.OnFinishSaving -= OnFinishSaving;
			EventManager.OnFailSaving -= OnFail;
		}


		public override ActionEnd End (List<Action> actions)
		{
			if (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame)
			{
				return base.End (actions);
			}
			return GenerateStopActionEnd ();
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			saveHandling = (SaveHandling) EditorGUILayout.EnumPopup ("Method:", saveHandling);
			
			if (saveHandling == SaveHandling.LoadGame || saveHandling == SaveHandling.OverwriteExistingSave)
			{
				string _action = "load";
				if (saveHandling == SaveHandling.OverwriteExistingSave)
				{
					_action = "overwrite";
				}
				
				selectSaveType = (SelectSaveType) EditorGUILayout.EnumPopup ("Save to " + _action + ":", selectSaveType);
				if (selectSaveType == SelectSaveType.SetSlotIndex)
				{
					saveIndexParameterID = Action.ChooseParameterGUI ("Slot index to " + _action + ":", parameters, saveIndexParameterID, ParameterType.Integer);
					if (saveIndexParameterID == -1)
					{
						saveIndex = EditorGUILayout.IntField ("Slot index to " + _action + ":", saveIndex);
					}
				}
				else if (selectSaveType == SelectSaveType.SlotIndexFromVariable)
				{
					slotVarID = AdvGame.GlobalVariableGUI ("Integer variable:", slotVarID, VariableType.Integer);
				}
				else if (selectSaveType == SelectSaveType.SetSaveID)
				{
					saveIndexParameterID = Action.ChooseParameterGUI ("Save ID to " + _action + ":", parameters, saveIndexParameterID, ParameterType.Integer);
					if (saveIndexParameterID == -1)
					{
						saveIndex = EditorGUILayout.IntField ("Save ID to " + _action + ":", saveIndex);
					} 
				}

				if (selectSaveType != SelectSaveType.Autosave && selectSaveType != SelectSaveType.SetSaveID)
				{
					EditorGUILayout.Space ();
					menuName = EditorGUILayout.TextField ("Menu with SavesList:", menuName);
					elementName = EditorGUILayout.TextField ("SavesList element:", elementName);
				}
			}

			if ((saveHandling == SaveHandling.OverwriteExistingSave && selectSaveType != SelectSaveType.Autosave) || saveHandling == SaveHandling.SaveNewGame)
			{
				if (saveHandling == SaveHandling.OverwriteExistingSave)
				{
					EditorGUILayout.Space ();
					updateLabel = EditorGUILayout.Toggle ("Update label?", updateLabel);
				}
				if (updateLabel || saveHandling == SaveHandling.SaveNewGame)
				{
					customLabel = EditorGUILayout.Toggle ("With custom label?", customLabel);
					if (customLabel)
					{
						varID = AdvGame.GlobalVariableGUI ("Label as String variable:", varID, VariableType.String);
					}
				}
			}

			if (saveHandling == SaveHandling.LoadGame || saveHandling == SaveHandling.ContinueFromLastSave)
			{
				doSelectiveLoad = EditorGUILayout.Toggle ("Selective loading?", doSelectiveLoad);
				if (doSelectiveLoad)
				{
					EditorGUILayout.Space ();
					selectiveLoad.ShowGUI ();
				}
			}

			if (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame)
			{
				AfterRunningOption ();
			}
		}
		
		
		public override string SetLabel ()
		{
			return saveHandling.ToString ();
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Save: Save or load' Action, set to save a new file</summary>
		 * <param name = "customLabelGlobalVariableID">If non-negative, the ID number of a Global String variable whose value will be used as the file's label</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveHandle CreateNew_SaveNew (int customLabelGlobalStringVariableID = -1)
		{
			ActionSaveHandle newAction = (ActionSaveHandle) CreateInstance <ActionSaveHandle>();
			newAction.saveHandling = SaveHandling.SaveNewGame;
			newAction.customLabel = (customLabelGlobalStringVariableID >= 0);
			newAction.varID = customLabelGlobalStringVariableID;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Save or load' Action, set to save to the Autosave</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveHandle CreateNew_SaveAutosave ()
		{
			ActionSaveHandle newAction = (ActionSaveHandle) CreateInstance <ActionSaveHandle>();
			newAction.saveHandling = SaveHandling.OverwriteExistingSave;
			newAction.selectSaveType = SelectSaveType.Autosave;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Save or load' Action, set to load a save file from a slot</summary>
		 * <param name = "menuName">The name of the Menu with the SavesList element</param>
		 * <param name = "savesListElementName">The SavesList element used to list save files to load from</param>
		 * <param name = "saveSlotIndex">The index number of the SavesList element slot to load</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveHandle CreateNew_LoadFromSlot (string menuName, string savesListElementName, int saveSlotIndex)
		{
			ActionSaveHandle newAction = (ActionSaveHandle) CreateInstance <ActionSaveHandle>();
			newAction.saveHandling = SaveHandling.LoadGame;
			newAction.selectSaveType = SelectSaveType.SetSlotIndex;
			newAction.saveIndex = saveSlotIndex;
			newAction.menuName = menuName;
			newAction.elementName = savesListElementName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Save or load' Action, set to load the Autosave file</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveHandle CreateNew_LoadAutosave ()
		{
			ActionSaveHandle newAction = (ActionSaveHandle) CreateInstance <ActionSaveHandle>();
			newAction.saveHandling = SaveHandling.LoadGame;
			newAction.selectSaveType = SelectSaveType.Autosave;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Save or load' Action, set to load the last-saved file</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveHandle CreateNew_ContinueLast ()
		{
			ActionSaveHandle newAction = (ActionSaveHandle) CreateInstance <ActionSaveHandle>();
			newAction.saveHandling = SaveHandling.ContinueFromLastSave;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Save or load' Action, set to save the game in a specific slot</summary>
		 * <param name = "menuName">The name of the Menu with the SavesList element</param>
		 * <param name = "savesListElementName">The SavesList element used to list save files to save to</param>
		 * <param name = "saveSlotIndex">The index number of the SavesList element slot to save</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSaveHandle CreateNew_SaveInSlot (string menuName, string savesListElementName, int saveSlotIndex)
		{
			ActionSaveHandle newAction = (ActionSaveHandle) CreateInstance <ActionSaveHandle>();
			newAction.saveHandling = SaveHandling.OverwriteExistingSave;
			newAction.selectSaveType = SelectSaveType.SetSlotIndex;
			newAction.saveIndex = saveSlotIndex;
			newAction.menuName = menuName;
			newAction.elementName = savesListElementName;
			return newAction;
		}
		
	}
	
}