/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"VariablesManager.cs"
 * 
 *	This script handles the "Variables" tab of the main wizard.
 *	Boolean and integer, which can be used regardless of scene, are defined here.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * Handles the "Variables" tab of the Game Editor window.
	 * All global variables are defined here. Local variables are also managed here, but they are stored within the LocalVariables component on the GameEngine prefab.
	 * When the game begins, global variables are transferred to the RuntimeVariables component on the PersistentEngine prefab.
	 */
	[System.Serializable]
	public class VariablesManager : ScriptableObject
	{

		/** A List of the game's global variables */
		public List<GVar> vars = new List<GVar>();
		/** A List of preset values that the variables can be bulk-assigned to */
		public List<VarPreset> varPresets = new List<VarPreset>();
		/** If True, then the Variables Manager GUI will show the live values of each variable, rather than their default values */
		public bool updateRuntime = true;
		/** Data for shared popup variable label */
		public List<PopUpLabelData> popUpLabelData = new List<PopUpLabelData>();

		
		#if UNITY_EDITOR

		private int chosenPresetID = 0;

		private GVar selectedGlobalVar;
		private GVar selectedLocalVar;

		private static int sideVar = -1;
		private static VariableLocation sideVarLocation = VariableLocation.Global;
		private static Variables sideVarComponent = null;
		private static GVar selectedSideVar;

		private static string[] boolType = {"False", "True"};
		private string filter = "";
		private VariableType typeFilter;
		private VarFilter varFilter;

		public Vector2 scrollPos;
		private bool showGlobalTab = true;
		private bool showLocalTab = false;

		private bool showSettings = true;
		private bool showPresets = true;
		private bool showVariablesList = true;
		private bool showVariablesProperties = true;


		/**
		 * Shows the GUI.
		 */
		public void ShowGUI ()
		{
			string sceneName = MultiSceneChecker.EditActiveScene ();
			if (sceneName != "")
			{
				EditorGUILayout.LabelField ("Editing scene: '" + sceneName + "'",  CustomStyles.subHeader);
				EditorGUILayout.Space ();
			}

			EditorGUILayout.Space ();
			GUILayout.BeginHorizontal ();

			string label = (vars.Count > 0) ? ("Global (" + vars.Count + ")") : "Global";
			if (GUILayout.Toggle (showGlobalTab, label, "toolbarbutton"))
			{
				SetTab (0);
			}

			label = (KickStarter.localVariables != null && KickStarter.localVariables.localVars.Count > 0) ? ("Local (" +  KickStarter.localVariables.localVars.Count + ")") : "Local";
			if (GUILayout.Toggle (showLocalTab, label, "toolbarbutton"))
			{
				SetTab (1);
			}

			GUILayout.EndHorizontal ();
			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSettings = CustomGUILayout.ToggleHeader (showSettings, "Editor settings");
			if (showSettings)
			{
				updateRuntime = CustomGUILayout.Toggle ("Show runtime values?", updateRuntime, "AC.KickStarter.variablesManager.updateRuntime", "If True, then the Variables Manager GUI will show the live values of each variable, rather than their default values");

				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Filter by:", GUILayout.Width (65f));
				varFilter = (VarFilter) EditorGUILayout.EnumPopup (varFilter, GUILayout.MaxWidth (100f));
				if (varFilter == VarFilter.Type)
				{
					typeFilter = (VariableType) EditorGUILayout.EnumPopup (typeFilter);
				}
				else
				{
					filter = EditorGUILayout.TextField (filter);
				}
				EditorGUILayout.EndHorizontal ();
			}
		
			EditorGUILayout.EndVertical ();

			if (Application.isPlaying && updateRuntime && KickStarter.runtimeVariables != null)
			{
				EditorGUILayout.HelpBox ("Showing realtime values - changes made will not be saved.", MessageType.Warning);
			}
			else if (Application.isPlaying && KickStarter.runtimeVariables == null)
			{
				EditorGUILayout.HelpBox ("Variables cannot be modified or accessed without a GameEngine present in the scene.", MessageType.Warning);
			}
			else
			{
				EditorGUILayout.Space ();
			}

			if (showGlobalTab)
			{
				varPresets = ShowPresets (varPresets, vars, VariableLocation.Global);

				if (Application.isPlaying && updateRuntime && KickStarter.runtimeVariables != null)
				{
					selectedGlobalVar = ShowVariableListAndHeader (selectedGlobalVar, KickStarter.runtimeVariables.globalVars, VariableLocation.Global, varFilter, filter, typeFilter, false);
				}
				else
				{
					selectedGlobalVar = ShowVariableListAndHeader (selectedGlobalVar, vars, VariableLocation.Global, varFilter, filter, typeFilter, true);

					foreach (VarPreset varPreset in varPresets)
					{
						varPreset.UpdateCollection (vars);
					}
				}
			}
			else if (showLocalTab)
			{
				if (KickStarter.localVariables != null)
				{
					KickStarter.localVariables.varPresets = ShowPresets (KickStarter.localVariables.varPresets, KickStarter.localVariables.localVars, VariableLocation.Local);

					if (Application.isPlaying && updateRuntime)
					{
						selectedLocalVar = ShowVariableListAndHeader (selectedLocalVar, KickStarter.localVariables.localVars, VariableLocation.Local, varFilter, filter, typeFilter, false);
					}
					else
					{
						selectedLocalVar = ShowVariableListAndHeader (selectedLocalVar, KickStarter.localVariables.localVars, VariableLocation.Local, varFilter, filter, typeFilter, true);
					}
				}
				else
				{
					EditorGUILayout.LabelField ("Local variables",  CustomStyles.subHeader);
					EditorGUILayout.HelpBox ("A GameEngine prefab must be present in the scene before Local variables can be defined", MessageType.Info);
				}
			}

			EditorGUILayout.Space ();
			bool canEdit = !(Application.isPlaying && updateRuntime && KickStarter.runtimeVariables != null);
			if (canEdit && showGlobalTab && selectedGlobalVar != null && vars.Contains (selectedGlobalVar))
			{
				ShowVarGUIAndHeader (selectedGlobalVar, VariableLocation.Global, canEdit, varPresets, "AC.GlobalVariables.GetVariable (" + selectedGlobalVar.id + ")");
			}
			else if (!canEdit && showGlobalTab && selectedGlobalVar != null && KickStarter.runtimeVariables.globalVars.Contains (selectedGlobalVar))
			{
				ShowVarGUIAndHeader (selectedGlobalVar, VariableLocation.Global, canEdit, varPresets, "AC.GlobalVariables.GetVariable (" + selectedGlobalVar.id + ")");
			}
			else if (showLocalTab && selectedLocalVar != null && KickStarter.localVariables != null && KickStarter.localVariables.localVars.Contains (selectedLocalVar))
			{
				ShowVarGUIAndHeader (selectedLocalVar, VariableLocation.Local, canEdit, KickStarter.localVariables.varPresets, "AC.LocalVariables.GetVariable (" + selectedLocalVar.id + ")");
			}

			if (GUI.changed)
			{
				EditorUtility.SetDirty (this);

				if (showLocalTab && KickStarter.localVariables != null)
				{
					UnityVersionHandler.CustomSetDirty (KickStarter.localVariables);
				}
			}
		}


		private static void SideMenu (GVar _var, List<GVar> _vars, VariableLocation _location, Variables _variables)
		{
			GenericMenu menu = new GenericMenu ();
			sideVar = _vars.IndexOf (_var);
			sideVarLocation = _location;
			sideVarComponent = _variables;
			selectedSideVar = _var;

			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			if (_vars.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			}

			if (sideVar > 0 || sideVar < _vars.Count-1)
			{
				menu.AddSeparator ("");
			}

			if (sideVar > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (sideVar < (_vars.Count - 1))
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom");
			}
			menu.AddSeparator ("");

			if (sideVarLocation == VariableLocation.Local)
			{
				menu.AddItem (new GUIContent ("Convert to Global"), false, Callback, "Convert to Global");
				menu.AddItem (new GUIContent ("Find local references"), false, Callback, "Find local references");
			}
			else if (sideVarLocation == VariableLocation.Global)
			{
				menu.AddItem (new GUIContent ("Convert to Local"), false, Callback, "Convert to Local");
				menu.AddItem (new GUIContent ("Find global references"), false, Callback, "Find global references");
			}
			else if (sideVarLocation == VariableLocation.Component)
			{
				menu.AddItem (new GUIContent ("Find references"), false, Callback, "Find component references");
			}
			
			menu.ShowAsContext ();
		}
		
		
		private static void Callback (object obj)
		{
			if (sideVar >= 0)
			{
				List<GVar> _vars = new List<GVar>();
				Object objectToRecord = null;

				if (sideVarLocation == VariableLocation.Global)
				{
					_vars = KickStarter.variablesManager.vars;
					KickStarter.variablesManager.filter = string.Empty;
					objectToRecord = KickStarter.variablesManager;
				}
				else if (sideVarLocation == VariableLocation.Local)
				{
					if (KickStarter.localVariables != null)
					{
						_vars = KickStarter.localVariables.localVars;
						KickStarter.variablesManager.filter = string.Empty;
						objectToRecord = KickStarter.localVariables;
					}
				}
				else if (sideVarLocation == VariableLocation.Component)
				{
					if (sideVarComponent != null)
					{
						_vars = sideVarComponent.vars;
						sideVarComponent.filter = string.Empty;
						objectToRecord = sideVarComponent;
					}
				}

				GVar tempVar = _vars[sideVar];

				switch (obj.ToString ())
				{
					case "Insert after":
						Undo.RecordObject (objectToRecord, "Insert Variable");
						_vars.Insert (sideVar+1, new GVar (GetIDArray (_vars)));
						selectedSideVar = DeactivateAllVars (_vars, selectedSideVar);
						break;
						
					case "Delete":
						Undo.RecordObject (objectToRecord, "Delete Variable");
						_vars.RemoveAt (sideVar);
						selectedSideVar = DeactivateAllVars (_vars, selectedSideVar);
						break;

					case "Move to top":
						Undo.RecordObject (objectToRecord, "Move Variable to top");
						_vars.RemoveAt (sideVar);
						_vars.Insert (0, tempVar);
						break;

					case "Move up":
						Undo.RecordObject (objectToRecord, "Move Variable up");
						_vars.RemoveAt (sideVar);
						_vars.Insert (sideVar-1, tempVar);
						break;

					case "Move down":
						Undo.RecordObject (objectToRecord, "Move Variable down");
						_vars.RemoveAt (sideVar);
						_vars.Insert (sideVar+1, tempVar);
						break;

					case "Move to bottom":
						Undo.RecordObject (objectToRecord, "Move Variable to bottom");
						_vars.RemoveAt (sideVar);
						_vars.Insert (_vars.Count, tempVar);
						break;

					case "Convert to Global":
						ConvertLocalToGlobal (_vars[sideVar], sideVar);
						break;

					case "Convert to Local":
						ConvertGlobalToLocal (_vars[sideVar]);
						break;

					case "Find local references":
						FindLocalReferences (tempVar);
						break;

					case "Find global references":
						FindGlobalReferences (tempVar);
						break;

					case "Find component references":
						FindComponentReferences (tempVar, sideVarComponent);
						break;
				}
			}

			sideVar = -1;

			if (sideVarLocation == AC.VariableLocation.Global)
			{
				EditorUtility.SetDirty (KickStarter.variablesManager);
				AssetDatabase.SaveAssets ();
			}
			else if (sideVarLocation == VariableLocation.Local)
			{
				if (KickStarter.localVariables)
				{
					EditorUtility.SetDirty (KickStarter.localVariables);
				}
			}
			else if (sideVarLocation == VariableLocation.Component)
			{
				if (sideVarComponent != null)
				{
					EditorUtility.SetDirty (sideVarComponent);
				}
			}
		}


		private static void ConvertLocalToGlobal (GVar localVariable, int localIndex)
		{
			if (localVariable == null) return;

			if (EditorUtility.DisplayDialog ("Convert " + localVariable.label + " to Global Variable?", "This will update all Actions and Managers that refer to this Variable.  This is a non-reversible process, and you should back up your project first. Continue?", "OK", "Cancel"))
			{
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					// Create new Global
					KickStarter.variablesManager.selectedGlobalVar = DeactivateAllVars (KickStarter.variablesManager.vars, KickStarter.variablesManager.selectedGlobalVar);
					GVar newGlobalVariable = new GVar (localVariable);
					int newGlobalID = newGlobalVariable.AssignUniqueID (GetIDArray (KickStarter.variablesManager.vars));
					KickStarter.variablesManager.vars.Add (newGlobalVariable);

					// Update current scene
					bool updatedScene = false;
					ActionList[] actionLists = FindObjectsOfType <ActionList>();
					foreach (ActionList actionList in actionLists)
					{
						foreach (Action action in actionList.actions)
						{
							if (action != null)
							{
								bool updatedActionList = action.ConvertLocalVariableToGlobal (localVariable.id, newGlobalID);
								if (updatedActionList)
								{
									updatedScene = true;
									UnityVersionHandler.CustomSetDirty (actionList, true);
									ACDebug.Log ("Updated Action " + actionList.actions.IndexOf (action) + " of ActionList '" + actionList.name + "'", actionList);
								}
							}
						}
					}

					Conversation[] conversations = FindObjectsOfType <Conversation>();
					foreach (Conversation conversation in conversations)
					{
						bool updatedConversation = conversation.ConvertLocalVariableToGlobal (localVariable.id, newGlobalID);
						if (updatedConversation)
						{
							updatedScene = true;
							UnityVersionHandler.CustomSetDirty (conversation, true);
							ACDebug.Log ("Updated Conversation '" + conversation + "'");
						}
					}

					if (updatedScene)
					{
						UnityVersionHandler.SaveScene ();
					}

					// Update Speech Manager
					if (KickStarter.speechManager)
					{
						KickStarter.speechManager.ConvertLocalVariableToGlobal (localVariable, newGlobalID);
					}

					// Remove old Local
					KickStarter.localVariables.localVars.RemoveAt (localIndex);
					EditorUtility.SetDirty (KickStarter.localVariables);
					UnityVersionHandler.SaveScene ();

					// Mark for saving
					EditorUtility.SetDirty (KickStarter.variablesManager);

					AssetDatabase.SaveAssets ();
				}
			}
		}


		private static void FindLocalReferences (GVar localVariable)
		{
			if (localVariable == null) return;

			int totalNumReferences = 0;

			ActionList[] actionLists = FindObjectsOfType <ActionList>();
			foreach (ActionList actionList in actionLists)
			{
				totalNumReferences += actionList.GetVariableReferences (VariableLocation.Local, localVariable);
			}

			Conversation[] conversations = FindObjectsOfType <Conversation>();
			foreach (Conversation conversation in conversations)
			{
				int thisNumReferences = conversation.GetVariableReferences (VariableLocation.Local, localVariable.id);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
					ACDebug.Log ("Found " + thisNumReferences + " references to local variable '" + localVariable.label + "' in Conversation '" + conversation.name + "'", conversation);
				}
			}

			EditorUtility.DisplayDialog ("Variable search complete", "In total, found " + totalNumReferences + " references to local variable '" + localVariable.label + "' in the scene.  Please see the Console window for full details.", "OK");
		}


		private static void FindComponentReferences (GVar componentVariable, Variables _variables)
		{
			if (componentVariable == null || _variables == null) return;

			int totalNumReferences = 0;

			ActionList[] actionLists = FindObjectsOfType <ActionList>();
			foreach (ActionList actionList in actionLists)
			{
				totalNumReferences += actionList.GetVariableReferences (VariableLocation.Component, componentVariable, _variables);
			}

			EditorUtility.DisplayDialog ("Variable search complete", "In total, found " + totalNumReferences + " references to component variable '" + componentVariable.label + "' in the scene.  Please see the Console window for full details.", "OK");
		}


		private static void FindGlobalReferences (GVar globalVariable)
		{
			if (globalVariable == null) return;

			if (EditorUtility.DisplayDialog ("Search '" + globalVariable.label + "' references?", "The Editor will search assets, and active scenes listed in the Build Settings, for references to the variable.  The current scene will need to be saved and listed to be included in the search process. Continue?", "OK", "Cancel"))
			{
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					int totalNumReferences = 0;

					// Search scenes
					string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();
					string[] sceneFiles = AdvGame.GetSceneFiles ();

					foreach (string sceneFile in sceneFiles)
					{
						UnityVersionHandler.OpenScene (sceneFile);

						ActionList[] actionLists = FindObjectsOfType <ActionList>();
						foreach (ActionList actionList in actionLists)
						{
							totalNumReferences += actionList.GetVariableReferences (VariableLocation.Global, globalVariable, null, sceneFile);
						}

						Conversation[] conversations = FindObjectsOfType <Conversation>();
						foreach (Conversation conversation in conversations)
						{
							int thisNumReferences = conversation.GetVariableReferences (VariableLocation.Global, globalVariable.id);
							if (thisNumReferences > 0)
							{
								totalNumReferences += thisNumReferences;
								ACDebug.Log ("Found " + thisNumReferences + " references to global variable '" + globalVariable.label + "' in Conversation '" + conversation.name + "' in scene '" + sceneFile + "'", conversation);
							}
						}
					}

					UnityVersionHandler.OpenScene (originalScene);

					// Search assets
					if (AdvGame.GetReferences ().speechManager != null)
					{
						ActionListAsset[] allActionListAssets = AdvGame.GetReferences ().speechManager.GetAllActionListAssets ();
						foreach (ActionListAsset actionListAsset in allActionListAssets)
						{
							totalNumReferences += actionListAsset.GetVariableReferences (globalVariable);
						}
					}

					// Search menus
					if (AdvGame.GetReferences ().menuManager != null && AdvGame.GetReferences ().menuManager.menus != null)
					{
						foreach (Menu menu in AdvGame.GetReferences ().menuManager.menus)
						{
							if (menu != null)
							{
								int thisNumReferences = menu.GetVariableReferences (globalVariable.id);
								if (thisNumReferences > 0)
								{
									totalNumReferences += thisNumReferences;
									ACDebug.Log ("Found " + thisNumReferences + " references to global variable '" + globalVariable.label + "' in Menu '" + menu.title + "'");
								}
							}
						}
					}

					EditorUtility.DisplayDialog ("Variable search complete", "In total, found " + totalNumReferences + " references to global variable '" + globalVariable.label + "' in the project.  Please see the Console window for full details.", "OK");
				}
			}
		}


		private static void ConvertGlobalToLocal (GVar globalVariable)
		{
			if (globalVariable == null) return;

			if (KickStarter.localVariables == null)
			{
				ACDebug.LogWarning ("Cannot convert variable to local since the scene has not been prepared for AC.");
				return;
			}

			if (EditorUtility.DisplayDialog ("Convert " + globalVariable.label + " to Local Variable?", "This will update all Actions and Managers that refer to this Variable.  This is a non-reversible process, and you should back up your project first, and make sure the current scene is added to your Build Settings. Continue?", "OK", "Cancel"))
			{
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					// Create new Local
					KickStarter.variablesManager.selectedLocalVar = DeactivateAllVars (KickStarter.localVariables.localVars, KickStarter.variablesManager.selectedLocalVar);
					GVar newLocalVariable = new GVar (globalVariable);
					int newLocalID = newLocalVariable.AssignUniqueID (GetIDArray (KickStarter.localVariables.localVars));
					KickStarter.localVariables.localVars.Add (newLocalVariable);
					UnityVersionHandler.CustomSetDirty (KickStarter.localVariables, true);
					UnityVersionHandler.SaveScene ();

					// Update current scene
					bool updatedScene = false;
					string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();

					ActionList[] actionLists = FindObjectsOfType <ActionList>();
					foreach (ActionList actionList in actionLists)
					{
						foreach (Action action in actionList.actions)
						{
							bool updatedActionList = action.ConvertGlobalVariableToLocal (globalVariable.id, newLocalID, true);
							if (updatedActionList)
							{
								updatedScene = true;
								UnityVersionHandler.CustomSetDirty (actionList, true);
								ACDebug.Log ("Updated Action " + actionList.actions.IndexOf (action) + " of ActionList '" + actionList.name + "' in scene '" + originalScene + "'", actionList);
							}
						}
					}

					Conversation[] conversations = FindObjectsOfType <Conversation>();
					foreach (Conversation conversation in conversations)
					{
						bool updatedConversation = conversation.ConvertGlobalVariableToLocal (globalVariable.id, newLocalID, true);
						if (updatedConversation)
						{
							updatedScene = true;
							UnityVersionHandler.CustomSetDirty (conversation, true);
							ACDebug.Log ("Updated Conversation " + conversation + ") in scene '" + originalScene + "'");
						}
					}

					if (updatedScene)
					{
						UnityVersionHandler.SaveScene ();
					}

					// Update other scenes
					string[] sceneFiles = AdvGame.GetSceneFiles ();
					foreach (string sceneFile in sceneFiles)
					{
						if (sceneFile == originalScene)
						{
							continue;
						}
						UnityVersionHandler.OpenScene (sceneFile);

						actionLists = FindObjectsOfType <ActionList>();
						foreach (ActionList actionList in actionLists)
						{
							foreach (Action action in actionList.actions)
							{
								if (action != null)
								{
									bool isAffected = action.ConvertGlobalVariableToLocal (globalVariable.id, newLocalID, false);
									if (isAffected)
									{
										ACDebug.LogWarning ("Cannot update Action " + actionList.actions.IndexOf (action) + " in ActionList '" + actionList.name + "' in scene '" + sceneFile + "' because it cannot access the Local Variable in scene '" + originalScene + "'.");
									}
								}
							}
						}

						conversations = FindObjectsOfType <Conversation>();
						foreach (Conversation conversation in conversations)
						{
							bool isAffected = conversation.ConvertGlobalVariableToLocal (globalVariable.id, newLocalID, false);
							if (isAffected)
							{
								ACDebug.LogWarning ("Cannot update Conversation " + conversation + ") in scene '" + sceneFile + "' because it cannot access the Local Variable in scene '" + originalScene + "'.");
							}
						}
					}

					UnityVersionHandler.OpenScene (originalScene);

					// Update Menu Manager
					if (KickStarter.menuManager)
					{
						KickStarter.menuManager.CheckConvertGlobalVariableToLocal (globalVariable.id, newLocalID);
					}

					//  Update Speech Manager
					if (KickStarter.speechManager)
					{
						// Search asset files
						ActionListAsset[] allActionListAssets = KickStarter.speechManager.GetAllActionListAssets ();
						UnityVersionHandler.OpenScene (originalScene);

						if (allActionListAssets != null)
						{
							foreach (ActionListAsset actionListAsset in allActionListAssets)
							{
								foreach (Action action in actionListAsset.actions)
								{
									if (action != null)
									{
										bool isAffected = action.ConvertGlobalVariableToLocal (globalVariable.id, newLocalID, false);
										if (isAffected)
										{
											ACDebug.LogWarning ("Cannot update Action " + actionListAsset.actions.IndexOf (action) + " in ActionList asset '" + actionListAsset.name + "' because asset files cannot refer to Local Variables.");
										}
									}
								}
							}
						}

						KickStarter.speechManager.ConvertGlobalVariableToLocal (globalVariable, UnityVersionHandler.GetCurrentSceneName ());
					}

					// Remove old Global
					KickStarter.variablesManager.vars.Remove (globalVariable);

					// Mark for saving
					EditorUtility.SetDirty (KickStarter.variablesManager);
					if (KickStarter.localVariables != null)
					{
						UnityVersionHandler.CustomSetDirty (KickStarter.localVariables);
					}

					AssetDatabase.SaveAssets ();
				}
			}
		}


		/**
		 * <summary>Selects a Variable for editing</summary>
		 * <param name = "variableID">The ID of the Variable to select</param>
		 * <param name = "location">The Variable's location (Global, Local)</param>
		 */
		public void ShowVariable (int variableID, VariableLocation location)
		{
			if (location == VariableLocation.Global)
			{
				GVar varToActivate = GetVariable (variableID);
				if (varToActivate != null)
				{
					selectedGlobalVar = DeactivateAllVars (vars, selectedGlobalVar);
					selectedGlobalVar = ActivateVar (varToActivate, selectedGlobalVar);
				}
				SetTab (0);
			}
			else if (location == VariableLocation.Local)
			{
				GVar varToActivate = LocalVariables.GetVariable (variableID);
				if (varToActivate != null)
				{
					selectedLocalVar = DeactivateAllVars (KickStarter.localVariables.localVars, selectedLocalVar);
					selectedLocalVar = ActivateVar (varToActivate, selectedLocalVar);
				}
				SetTab (1);
			}
		}


		private static GVar ActivateVar (GVar varToActivate, GVar _selectedVar)
		{
			if (varToActivate != null && _selectedVar != varToActivate)
			{
				_selectedVar = varToActivate;
				EditorGUIUtility.editingTextField = false;
			}

			return _selectedVar;
		}
		
		
		private static GVar DeactivateAllVars (List<GVar> _vars, GVar _selectedVar)
		{
			_selectedVar = null;
			EditorGUIUtility.editingTextField = false;
			return _selectedVar;
		}


		private static int[] GetIDArray (List<GVar> _vars)
		{
			// Returns a list of id's in the list
			
			List<int> idArray = new List<int>();
			
			foreach (GVar variable in _vars)
			{
				idArray.Add (variable.id);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}


		private static int[] GetIDArray (List<VarPreset> _varPresets)
		{
			// Returns a list of id's in the list
			
			List<int> idArray = new List<int>();
			
			foreach (VarPreset _varPreset in _varPresets)
			{
				idArray.Add (_varPreset.ID);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}


		public static bool VarMatchesFilter (GVar _var, VarFilter _varFilter, string _filter, VariableType _typeFilter)
		{
			if (_var != null)
			{
				switch (_varFilter)
				{
					case VarFilter.Label:
						return (string.IsNullOrEmpty (_filter) || _var.label.ToLower ().Contains (_filter.ToLower ()));

					case VarFilter.Description:
						return (string.IsNullOrEmpty (_filter) || _var.description.ToLower ().Contains (_filter.ToLower ()));

					case VarFilter.Type:
						return (_var.type == _typeFilter);
				}
			}

			return false;
		}


		private GVar ShowVariableListAndHeader (GVar _selectedVar, List<GVar> _vars, VariableLocation _location, VarFilter _varFilter, string _filter, VariableType _typeFilter, bool allowEditing, Variables _variables = null)
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showVariablesList = CustomGUILayout.ToggleHeader (showVariablesList, _location + " variables");
			if (showVariablesList)
			{
				_selectedVar = ShowVarList (_selectedVar, _vars, _location, _varFilter, _filter, _typeFilter, allowEditing, _variables);
			}
			EditorGUILayout.EndVertical ();
			return _selectedVar;
		}


		public static GVar ShowVarList (GVar _selectedVar, List<GVar> _vars, VariableLocation _location, VarFilter _varFilter, string _filter, VariableType _typeFilter, bool allowEditing, Variables _variables = null)
		{
			int numInFilter = 0;
			foreach (GVar _var in _vars)
			{
				_var.showInFilter = VarMatchesFilter (_var, _varFilter, _filter, _typeFilter);
				if (_var.showInFilter)
				{
					numInFilter ++;
				}
			}

			Vector2 _scrollPos = (_variables != null) ? _variables.scrollPos : KickStarter.variablesManager.scrollPos;
			_scrollPos = EditorGUILayout.BeginScrollView (_scrollPos, GUILayout.Height (Mathf.Min (numInFilter * 21, ACEditorPrefs.MenuItemsBeforeScroll * 21) +5));
			if (numInFilter > 0)
			{
				foreach (GVar _var in _vars)
				{
					if (_var.showInFilter)
					{
						EditorGUILayout.BeginHorizontal ();
						
						string buttonLabel = _var.id + ": ";
						if (buttonLabel == "")
						{
							_var.label += "(Untitled)";	
						}
						else
						{
							buttonLabel += _var.label;

							if (buttonLabel.Length > 30)
							{
								buttonLabel = buttonLabel.Substring (0, 30);
							}
						}

						string varValue = _var.GetValue ();
						varValue = varValue.Replace("\r\n", "");
						varValue = varValue.Replace("\n", "");
						varValue = varValue.Replace("\r", "");
						if (varValue.Length > 20)
						{
							varValue = varValue.Substring (0, 20);
						}

						buttonLabel += " (" + _var.type.ToString () + " - " + varValue + ")";

						if (GUILayout.Toggle (_selectedVar == _var, buttonLabel, "Button"))
						{
							if (_selectedVar != _var)
							{
								_selectedVar = DeactivateAllVars (_vars, _selectedVar);
								_selectedVar = ActivateVar (_var, _selectedVar);
							}
						}
						
						if (allowEditing && GUILayout.Button (string.Empty, CustomStyles.IconCog))
						{
							SideMenu (_var, _vars, _location, _variables);
						}
						
						EditorGUILayout.EndHorizontal ();
					}
				}

				EditorGUILayout.EndScrollView ();

				if (_varFilter == VarFilter.Type || !string.IsNullOrEmpty (_filter))
				{
					if (numInFilter != _vars.Count)
					{
						EditorGUILayout.HelpBox ("Filtering " + numInFilter + " out of " + _vars.Count + " variables.", MessageType.Info);
					}
				}
			}
			else if (_vars.Count > 0)
			{
				EditorGUILayout.EndScrollView ();

				if (_varFilter != VarFilter.Type && !string.IsNullOrEmpty (_filter))
				{
					EditorGUILayout.HelpBox ("No variables with '" + _filter + "' in their " + _varFilter.ToString () + " found.", MessageType.Info);
				}
				else if (_varFilter == VarFilter.Type)
				{
					EditorGUILayout.HelpBox ("No variables of type '" + _typeFilter + " found.", MessageType.Info);
				}
			}
			else
			{
				EditorGUILayout.EndScrollView ();
			}

			if (allowEditing)
			{
				EditorGUILayout.Space ();
				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Create new " + _location + " variable"))
				{
					_filter = string.Empty;

					if (_location == VariableLocation.Global)
					{
						Undo.RecordObject (KickStarter.variablesManager, "Add " + _location + " variable");
					}
					else if (_location == VariableLocation.Local)
					{
						Undo.RecordObject (KickStarter.localVariables, "Add " + _location + " variable");
					}
					else if (_location == VariableLocation.Component)
					{
						Undo.RecordObject (_variables, "Add " + _location + " variable");
					}

					_vars.Add (new GVar (GetIDArray (_vars)));
					_selectedVar = DeactivateAllVars (_vars, _selectedVar);
					_selectedVar = ActivateVar (_vars [_vars.Count-1], _selectedVar);
				}

				if (_vars != null && _vars.Count > 0)
				{
					if (GUILayout.Button ("", CustomStyles.IconCog))
					{
						ExportSideMenu (_location, _variables);
					}
				}
				EditorGUILayout.EndHorizontal ();
			}

			if (_variables != null) _variables.scrollPos = _scrollPos;
			else KickStarter.variablesManager.scrollPos = _scrollPos;

			return _selectedVar;
		}


		private static void ExportSideMenu (VariableLocation _location, Variables _variables)
		{
			sideVarLocation = _location;
			sideVarComponent = _variables;

			GenericMenu menu = new GenericMenu ();

			if (_location == VariableLocation.Local)
			{
				menu.AddItem (new GUIContent ("Export Local variables (all scenes)..."), false, ExportCallback, "Export");
				menu.AddItem (new GUIContent ("Export Local variables (this scene)..."), false, ExportCallback, "ExportSingleLocal");
			}
			else
			{
				menu.AddItem (new GUIContent ("Export " + _location.ToString () + " variables..."), false, ExportCallback, "Export");
			}
			menu.ShowAsContext ();
		}


		private static void ExportCallback (object obj)
		{
			switch (obj.ToString ())
			{
				case "Import":
					//ImportItems ();
					break;

				case "ExportSingleLocal":
					VarExportWizardWindow.Init (sideVarLocation, false, null);
					break;

				case "Export":
					VarExportWizardWindow.Init (sideVarLocation, true, sideVarComponent);
					break;
			}
		}


		private void ShowVarGUIAndHeader (GVar selectedVar, VariableLocation location, bool canEdit, List<VarPreset> _varPresets = null, string apiPrefix = "")
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showVariablesProperties = CustomGUILayout.ToggleHeader (showVariablesProperties, location + " variable '" + selectedVar.label + "' properties");
			if (showVariablesProperties)
			{
				ShowVarGUI (selectedVar, location, canEdit, _varPresets, apiPrefix);
			}
			EditorGUILayout.EndVertical ();
		}


		public static void ShowVarGUI (GVar selectedVar, VariableLocation location, bool canEdit, List<VarPreset> _varPresets = null, string apiPrefix = "", Variables _variables = null)
		{
			string labelPrefix = (canEdit) ? "Initial value:" : "Current value:";
			string helpText = (canEdit) ? "Its initial value" : "Its current value";

			if (!canEdit && selectedVar.HasTranslations () && Options.GetLanguage () > 0)
			{
				labelPrefix = "Original language value:";
			}

			if (canEdit)
			{
				selectedVar.label = CustomGUILayout.TextField ("Label:", selectedVar.label, apiPrefix + ".label", "Its editor name");
				selectedVar.type = (VariableType) CustomGUILayout.EnumPopup ("Type:", selectedVar.type, apiPrefix + ".type", "Its variable type");
			}
			else
			{
				EditorGUILayout.LabelField ("Label: " + selectedVar.label);
				EditorGUILayout.LabelField ("Type: " + selectedVar.type.ToString ());
			}

			switch (selectedVar.type)
			{
				case VariableType.Boolean:
					if (selectedVar.val != 1)
					{
						selectedVar.val = 0;
					}
					selectedVar.val = CustomGUILayout.Popup (labelPrefix, selectedVar.val, boolType, apiPrefix + ".val", helpText);
					break;

				case VariableType.Float:
					selectedVar.floatVal = CustomGUILayout.FloatField (labelPrefix, selectedVar.floatVal, apiPrefix + ".floatVal", helpText);
					break;

				case VariableType.Integer:
					selectedVar.val = CustomGUILayout.IntField (labelPrefix, selectedVar.val, apiPrefix + ".val", helpText);
					break;

				case VariableType.PopUp:
					Object objectToRecord = null;
					if (location == VariableLocation.Global) objectToRecord = KickStarter.variablesManager;
					if (location == VariableLocation.Local) objectToRecord = KickStarter.localVariables;
					if (location == VariableLocation.Component) objectToRecord = _variables;

					ShowPopUpLabelsGUI (selectedVar, canEdit, objectToRecord);

					if (selectedVar.GetNumPopUpValues () > 0)
					{
						string[] popUpLabels = selectedVar.GenerateEditorPopUpLabels ();
						selectedVar.val = CustomGUILayout.Popup (labelPrefix, selectedVar.val, popUpLabels, apiPrefix + ".val", helpText);
					}
					else
					{
						selectedVar.val = 0;
					}

					if (selectedVar.popUpID > 0)
					{
						if (Application.isPlaying && selectedVar.canTranslate)
						{
							EditorGUILayout.LabelField ("Values can be translated");
						}
					}
					else
					{
						if (canEdit)
						{
							selectedVar.canTranslate = CustomGUILayout.Toggle ("Values can be translated?", selectedVar.canTranslate, apiPrefix + ".canTranslate", "If True, the variable's value can be translated");
						}
						else if (selectedVar.canTranslate)
						{
							EditorGUILayout.LabelField ("Values can be translated");
						}
					}
					break;

				case VariableType.String:
					EditorGUILayout.BeginHorizontal ();
					CustomGUILayout.LabelField (labelPrefix, GUILayout.Width (140f), apiPrefix + ".textVal");
					EditorStyles.textField.wordWrap = true;
					selectedVar.textVal = CustomGUILayout.TextArea (selectedVar.textVal, GUILayout.MaxWidth (800f), apiPrefix + ".textVal");
					EditorGUILayout.EndHorizontal ();

					if (canEdit)
					{
						selectedVar.canTranslate = CustomGUILayout.Toggle ("Values can be translated?", selectedVar.canTranslate, apiPrefix + ".canTranslate", "If True, the variable's value can be translated");
					}
					else if (selectedVar.canTranslate)
					{
						EditorGUILayout.LabelField ("Values can be translated");
					}
					break;

				case VariableType.Vector3:
					selectedVar.vector3Val = CustomGUILayout.Vector3Field (labelPrefix, selectedVar.vector3Val, apiPrefix + ".vector3Val", helpText);
					break;
			}

			switch (location)
			{
				case VariableLocation.Global:
					CustomGUILayout.TokenLabel ("[var:" + selectedVar.id.ToString () + "]");
					break;

				case VariableLocation.Local:
					CustomGUILayout.TokenLabel ("[localvar:" + selectedVar.id.ToString () + "]");
					break;

				case VariableLocation.Component:
					if (_variables != null)
					{
						ConstantID _constantID = _variables.GetComponent <ConstantID>();
						if (_constantID != null && _constantID.constantID != 0)
						{
							CustomGUILayout.TokenLabel ("[compvar:" + _constantID.constantID.ToString () + ":" + selectedVar.id.ToString () + "]");
						}
					}
					break;
			}

			if (_varPresets != null)
			{
				EditorGUILayout.Space ();
				foreach (VarPreset _varPreset in _varPresets)
				{
					// Local
					string apiPrefix2 = (location == VariableLocation.Local) ? 
										"AC.KickStarter.localVariables.GetPreset (" + _varPreset.ID + ").GetPresetValue (" + selectedVar.id + ")" :
										"AC.KickStarter.runtimeVariables.GetPreset (" + _varPreset.ID + ").GetPresetValue (" + selectedVar.id + ")";

					_varPreset.UpdateCollection (selectedVar);

					string label = "'" +
									(!string.IsNullOrEmpty (_varPreset.label) ? _varPreset.label : ("Preset #" + _varPreset.ID.ToString ())) +
									"' value:";

					PresetValue presetValue = _varPreset.GetPresetValue (selectedVar);
					switch (selectedVar.type)
					{
						case VariableType.Boolean:
							presetValue.val = CustomGUILayout.Popup (label, presetValue.val, boolType, apiPrefix2 + ".val");
							break;

						case VariableType.Float:
							presetValue.floatVal = CustomGUILayout.FloatField (label, presetValue.floatVal, apiPrefix2 + ".floatVal");
							break;

						case VariableType.Integer:
							presetValue.val = CustomGUILayout.IntField (label, presetValue.val, apiPrefix2 + ".val");
							break;

						case VariableType.PopUp:
							presetValue.val = CustomGUILayout.Popup (label, presetValue.val, selectedVar.popUps, apiPrefix2 + ".val");
							break;

						case VariableType.String:
							presetValue.textVal = CustomGUILayout.TextField (label, presetValue.textVal, apiPrefix2 + ".textVal");
							break;

						case VariableType.Vector3:
							presetValue.vector3Val = CustomGUILayout.Vector3Field (label, presetValue.vector3Val, apiPrefix2 + ".vector3Val");
							break;
					}
				}
			}

			EditorGUILayout.Space ();
			if (canEdit)
			{
				switch (location)
				{
					case VariableLocation.Local:
						selectedVar.link = VarLink.None;
						break;

					case VariableLocation.Global:
					case VariableLocation.Component:
						selectedVar.link = (VarLink) CustomGUILayout.EnumPopup ("Link to:", selectedVar.link, apiPrefix + ".link", "What it links to");
						if (selectedVar.link == VarLink.PlaymakerVariable)
						{
							if (PlayMakerIntegration.IsDefinePresent ())
							{
								if (location == VariableLocation.Global)
								{
									selectedVar.pmVar = CustomGUILayout.TextField ("Playmaker Global Variable:", selectedVar.pmVar, apiPrefix + ".pmVar", "The name of the Playmaker variable to link to.");
								}
								else if (location == VariableLocation.Component)
								{
									if (_variables != null && PlayMakerIntegration.HasFSM (_variables.gameObject))
									{
										selectedVar.pmVar = CustomGUILayout.TextField ("Playmaker Local Variable:", selectedVar.pmVar, apiPrefix + ".pmVar", "The name of the Playmaker variable to link to. It is assumed to be placed on the same GameObject as this Variables component.");
									}
									else
									{
										EditorGUILayout.HelpBox ("A Playmaker FSM component must be present on the Variables GameObject.", MessageType.Info);
									}
								}

								if (!string.IsNullOrEmpty (selectedVar.pmVar))
								{
									selectedVar.updateLinkOnStart = CustomGUILayout.Toggle ("Use PM for initial value?", selectedVar.updateLinkOnStart, apiPrefix + ".updateLinkOnStart", "If True, then Playmaker will be referred to for the initial value");
								}
							}
							else
							{
								EditorGUILayout.HelpBox ("The 'PlayMakerIsPresent' Scripting Define Symbol must be listed in the\nPlayer Settings. Please set it from Edit -> Project Settings -> Player", MessageType.Warning);
							}
						}
						else if (selectedVar.link == VarLink.OptionsData)
						{
							if (location == VariableLocation.Global)
							{
								EditorGUILayout.HelpBox ("This Variable will be stored in PlayerPrefs, and not in saved game files.", MessageType.Info);
							}
							else
							{
								EditorGUILayout.HelpBox ("Component variables cannot be linked to Options Data - use Global variables instead.", MessageType.Warning);
							}
						}
						else if (selectedVar.link == VarLink.CustomScript)
						{
							selectedVar.updateLinkOnStart = CustomGUILayout.Toggle ("Script sets initial value?", selectedVar.updateLinkOnStart, apiPrefix + ".updateLinkOnStart", "If True, then a custom script will be referred to for the initial value");
							EditorGUILayout.HelpBox ("See the Manual's 'Global variable linking' chapter for details on how to synchronise values.", MessageType.Info);
						}
						break;
				}
			}
			else
			{
				if (selectedVar.link != VarLink.None)
				{
					EditorGUILayout.LabelField ("Links to: " + selectedVar.link.ToString ());
					if (selectedVar.link == VarLink.PlaymakerVariable && !string.IsNullOrEmpty (selectedVar.pmVar))
					{
						EditorGUILayout.LabelField ("Linked PM variable: " + selectedVar.pmVar);
					}
					if (selectedVar.link == VarLink.PlaymakerVariable || selectedVar.link == VarLink.CustomScript)
					{
						if (selectedVar.updateLinkOnStart)
						{
							EditorGUILayout.LabelField ("Script sets initial value");
						}
					}
				}
			}

			if (canEdit)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("Internal description:", "An Editor-only description to aid designers"), GUILayout.MaxWidth (146f));
				selectedVar.description = EditorGUILayout.TextArea (selectedVar.description);
				EditorGUILayout.EndHorizontal ();
			}
			else
			{
				if (!string.IsNullOrEmpty (selectedVar.description))
				{
					EditorGUILayout.LabelField ("Internal description: " + selectedVar.description);
				}
			}
		}


		public static void ShowPopUpLabelsGUI (GVar variable, bool canEdit)
		{
			ShowPopUpLabelsGUI (variable, canEdit, null);
		}


		public static void ShowPopUpLabelsGUI (GVar variable, bool canEdit, Object objectToRecord)
		{
			if (KickStarter.variablesManager == null)
			{
				EditorGUILayout.HelpBox ("A Variables Manager must be assigned in the AC Game Editor window.", MessageType.Warning);
				return;
			}

			if (canEdit)
			{
				List<string> popUpPresetLabels = new List<string>();
				popUpPresetLabels.Add ("No preset");
				int j = 0;
				for (int i=0; i<KickStarter.variablesManager.popUpLabelData.Count; i++)
				{
					popUpPresetLabels.Add (KickStarter.variablesManager.popUpLabelData[i].EditorLabel);
					if (variable.popUpID == KickStarter.variablesManager.popUpLabelData[i].ID)
					{
						j = i+1;
					}
				}

				popUpPresetLabels.Add ("Create new...");
				j = EditorGUILayout.Popup ("Label preset:", j, popUpPresetLabels.ToArray ());
				if (j == 0)
				{
					variable.popUpID = 0;
				}
				else if (j == popUpPresetLabels.Count-1)
				{
					// Create new
					if (popUpPresetLabels.Count > 50)
					{
						ACDebug.LogWarning ("The maximum number of popup presets has been reached!");
						return;
					}

					List<int> idArray = new List<int>();
					foreach (PopUpLabelData data in KickStarter.variablesManager.popUpLabelData)
					{
						idArray.Add (data.ID);
					}
					idArray.Sort ();
					PopUpLabelData newData = new PopUpLabelData (idArray.ToArray (), variable.popUps, variable.popUpsLineID);
					KickStarter.variablesManager.popUpLabelData.Add (newData);
					variable.popUpID = newData.ID;
					variable.popUps = new string[0];
					variable.popUpsLineID = -1;

					EditorUtility.SetDirty (KickStarter.variablesManager);
				}
				else
				{
					variable.popUpID = KickStarter.variablesManager.popUpLabelData[j-1].ID;
				}
			}
			else if (variable.popUpID > 0)
			{
				PopUpLabelData data = KickStarter.variablesManager.GetPopUpLabelData (variable.popUpID);
				if (data != null)
				{
					EditorGUILayout.LabelField ("Selected preset: " + data.EditorLabel);
				}
			}

			PopUpLabelData presetData = KickStarter.variablesManager.GetPopUpLabelData (variable.popUpID);
			if (presetData != null)
			{
				presetData.ShowGUI (canEdit, KickStarter.variablesManager);
			}
			else
			{
				variable.popUps = PopupsGUI (variable.popUps, canEdit, objectToRecord);
			}
		}


		public static string[] PopupsGUI (string[] popUps, bool canEdit = true, Object objectToRecord = null)
		{
			List<string> popUpList = new List<string>();
			if (popUps != null && popUps.Length > 0)
			{
				foreach (string p in popUps)
				{
					popUpList.Add (p);
				}
			}

			if (canEdit)
			{
				for (int i=0; i<popUpList.Count; i++)
				{
					EditorGUILayout.BeginHorizontal ();
					popUpList[i] = EditorGUILayout.TextField ("Value " + i.ToString () +":", popUpList[i]);

					if (GUILayout.Button ("-", GUILayout.MaxWidth (20f)))
					{
						if (objectToRecord != null) Undo.RecordObject (objectToRecord, "Delete PopUp value");
						ACDebug.LogWarning ("Take caution when deleting a PopUp Variable's values, as this may affect the behaviour of Actions that reference it.  References made to this Variable can be found via the 'Find references' option in the cog menu beside it's name.", objectToRecord);
						popUpList.RemoveAt (i);
						EditorGUIUtility.editingTextField = false;
						i=-1;
					}

					EditorGUILayout.EndHorizontal ();
				}

				if (GUILayout.Button ("Add new value"))
				{
					if (objectToRecord != null) Undo.RecordObject (objectToRecord, "Add PopUp value");
					popUpList.Add (string.Empty);
				}
			}
			else
			{
				for (int i=0; i<popUpList.Count; i++)
				{
					EditorGUILayout.LabelField ("Value: " + i.ToString () + ": " + popUpList[i]);
				}
			}

			return popUpList.ToArray ();
		}


		private void SetTab (int tab)
		{
			if (tab == 0)
			{
				if (showLocalTab)
				{
					selectedLocalVar = null;
					EditorGUIUtility.editingTextField = false;
				}
				showGlobalTab = true;
				showLocalTab = false;
			}
			else if (tab == 1)
			{
				if (showGlobalTab)
				{
					selectedGlobalVar = null;
					EditorGUIUtility.editingTextField = false;
				}
				showLocalTab = true;
				showGlobalTab = false;
			}
		}


		private List<VarPreset> ShowPresets (List<VarPreset> _varPresets, List<GVar> _vars, VariableLocation location)
		{
			if (_vars == null || _vars.Count == 0)
			{
				return _varPresets;
			}

			if (!Application.isPlaying || _varPresets.Count > 0)
			{
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				showPresets = CustomGUILayout.ToggleHeader (showPresets, "Preset configurations");
			}

			if (showPresets && (!Application.isPlaying || _varPresets.Count > 0))
			{
				List<string> labelList = new List<string>();
				
				int i = 0;
				int presetNumber = -1;
				
				if (_varPresets.Count > 0)
				{
					foreach (VarPreset _varPreset in _varPresets)
					{
						if (_varPreset.label != "")
						{
							labelList.Add (i.ToString () + ": " + _varPreset.label);
						}
						else
						{
							labelList.Add (i.ToString () + ": (Untitled)");
						}
						
						if (_varPreset.ID == chosenPresetID)
						{
							presetNumber = i;
						}
						i++;
					}
					
					if (presetNumber == -1)
					{
						chosenPresetID = 0;
					}
					else if (presetNumber >= _varPresets.Count)
					{
						presetNumber = Mathf.Max (0, _varPresets.Count - 1);
					}
					else
					{
						presetNumber = EditorGUILayout.Popup ("Created presets:", presetNumber, labelList.ToArray());
						chosenPresetID = _varPresets[presetNumber].ID;
					}
				}
				else
				{
					chosenPresetID = presetNumber = -1;
				}

				if (presetNumber >= 0)
				{
					string apiPrefix = ((location == VariableLocation.Local) ? "AC.KickStarter.localVariables.GetPreset (" + chosenPresetID + ")" : "AC.KickStarter.runtimeVariables.GetPreset (" + chosenPresetID + ")");

					if (!Application.isPlaying)
					{
						_varPresets [presetNumber].label = CustomGUILayout.TextField ("Preset name:", _varPresets [presetNumber].label, apiPrefix + ".label");
					}

					EditorGUILayout.BeginHorizontal ();
					if (!Application.isPlaying)
					{
						GUI.enabled = false;
					}
					if (GUILayout.Button ("Bulk-assign"))
					{
						if (presetNumber >= 0 && _varPresets.Count > presetNumber)
						{
							if (location == VariableLocation.Global)
							{
								if (KickStarter.runtimeVariables)
								{
									KickStarter.runtimeVariables.AssignFromPreset (_varPresets [presetNumber]);
									ACDebug.Log ("Global variables updated to " + _varPresets [presetNumber].label);
								}
							}
							else if (location == VariableLocation.Local)
							{
								if (KickStarter.localVariables)
								{
									KickStarter.localVariables.AssignFromPreset (_varPresets [presetNumber]);
									ACDebug.Log ("Local variables updated to " + _varPresets [presetNumber].label);
								}
							}
						}
					}

					GUI.enabled = !Application.isPlaying;
					if (GUILayout.Button ("Delete"))
					{
						_varPresets.RemoveAt (presetNumber);
						presetNumber = 0;
						chosenPresetID = 0;
					}

					GUI.enabled = true;
					EditorGUILayout.EndHorizontal ();
				}

				if (!Application.isPlaying)
				{
					if (GUILayout.Button ("Create new preset"))
					{
						VarPreset newVarPreset = new VarPreset (_vars, GetIDArray (_varPresets));
						_varPresets.Add (newVarPreset);
						chosenPresetID = newVarPreset.ID;
					}
				}
			}
			if (!Application.isPlaying || _varPresets.Count > 0)
			{
				EditorGUILayout.EndVertical ();
			}

			EditorGUILayout.Space ();

			return _varPresets;
		}


		public int ShowPlaceholderPresetData (int dataID)
		{
			if (popUpLabelData.Count == 0)
			{
				return -1;
			}

			int index = 0;

			List<string> labels = new List<string>();
			labels.Add ("(No preset)");
			for (int i=0; i<popUpLabelData.Count; i++)
			{
				labels.Add (popUpLabelData[i].EditorLabel);

				if (popUpLabelData[i].ID == dataID)
				{
					index = i+1;
				}
			}
			index = EditorGUILayout.Popup ("Placeholder preset:", index, labels.ToArray ());

			if (index >= 1 && (index-1) < popUpLabelData.Count)
			{
				return popUpLabelData[index-1].ID;
			}
			return -1;
		}

		#endif


		/**
		 * <summary>Gets a global variable</summary>
		 * <param name = "_id">The ID number of the global variable to find</param>
		 * <returns>The global variable</returns>
		 */
		public GVar GetVariable (int _id)
		{
			foreach (GVar _var in vars)
			{
				if (_var.id == _id)
				{
					return _var;
				}
			}
			return null;
		}


		public PopUpLabelData GetPopUpLabelData (int ID)
		{
			if (ID > 0)
			{
				foreach (PopUpLabelData data in popUpLabelData)
				{
					if (data.ID == ID)
					{
						return data;
					}
				}
			}
			return null;
		}

	}

}