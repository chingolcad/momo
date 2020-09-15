using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Callbacks;

namespace AC
{

	[CustomEditor(typeof(ActionListAsset))]
	[System.Serializable]
	public class ActionListAssetEditor : Editor
	{

		private AC.Action actionToAffect;
		private ActionsManager actionsManager;


		private void OnEnable ()
		{
			if (AdvGame.GetReferences ())
			{
				if (AdvGame.GetReferences ().actionsManager)
				{
					actionsManager = AdvGame.GetReferences ().actionsManager;
					AdventureCreator.RefreshActions ();
				}
				else
				{
					ACDebug.LogError ("An Actions Manager is required - please use the Game Editor window to create one.");
				}
			}
			else
			{
				ACDebug.LogError ("A References file is required - please use the Game Editor window to create one.");
			}
		}
		

		public override void OnInspectorGUI ()
		{
			ActionListAsset _target = (ActionListAsset) target;

			ShowPropertiesGUI (_target);
			EditorGUILayout.Space ();

			if (actionsManager == null)
			{
				EditorGUILayout.HelpBox ("An Actions Manager asset file must be assigned in the Game Editor Window", MessageType.Warning);
				OnEnable ();
				UnityVersionHandler.CustomSetDirty (_target);
				return;
			}

			if (actionsManager.displayActionsInInspector)
			{
				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Expand all", EditorStyles.miniButtonLeft))
				{
					Undo.RecordObject (_target, "Expand actions");
					foreach (AC.Action action in _target.actions)
					{
						action.isDisplayed = true;
					}
				}
				if (GUILayout.Button ("Collapse all", EditorStyles.miniButtonMid))
				{
					Undo.RecordObject (_target, "Collapse actions");
					foreach (AC.Action action in _target.actions)
					{
						action.isDisplayed = false;
					}
				}
				if (GUILayout.Button ("Action List Editor", EditorStyles.miniButtonMid))
				{
					ActionListEditorWindow.Init (_target);
				}
				GUI.enabled = Application.isPlaying;
				if (GUILayout.Button ("Run now", EditorStyles.miniButtonRight))
				{
					if (KickStarter.actionListAssetManager != null)
					{
						if (!_target.canRunMultipleInstances)
						{
							int numRemoved = KickStarter.actionListAssetManager.EndAssetList (_target);
							if (numRemoved > 0)
							{
								ACDebug.Log ("Removed 1 instance of ActionList asset '" + _target.name + "' because it is set to only run one at a time.", _target);
							}
						}

						AdvGame.RunActionListAsset (_target);
					}
					else
					{
						ACDebug.LogWarning ("An AC PersistentEngine object must be present in the scene for ActionList assets to run.", _target);
					}
				}
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal ();
			}
			else
			{
				if (Application.isPlaying)
				{
					EditorGUILayout.BeginHorizontal ();
					if (GUILayout.Button ("Edit Actions", GUILayout.Height (40f)))
					{
						ActionListEditorWindow.Init (_target);
					}
					if (GUILayout.Button ("Run now", GUILayout.Height (40f)))
					{
						AdvGame.RunActionListAsset (_target);
					}
					EditorGUILayout.EndHorizontal ();
				}
				else
				{
					if (GUILayout.Button ("Edit Actions", GUILayout.Height (40f)))
					{
						ActionListEditorWindow.Init (_target);
					}
				}
				UnityVersionHandler.CustomSetDirty (_target);
				return;
			}

			EditorGUILayout.Space ();

			for (int i=0; i<_target.actions.Count; i++)
			{
				int typeIndex = KickStarter.actionsManager.GetActionTypeIndex (_target.actions[i]);

				if (_target.actions[i] == null)
				{
					RebuildAction (_target.actions[i], typeIndex, _target, i);
				}
				
				_target.actions[i].isAssetFile = true;
				
				EditorGUILayout.BeginVertical ("Button");

				string actionLabel = " (" + i + ") " + actionsManager.GetActionTypeLabel (_target.actions[i], true);
				actionLabel = actionLabel.Replace("\r\n", "");
				actionLabel = actionLabel.Replace("\n", "");
				actionLabel = actionLabel.Replace("\r", "");
				if (actionLabel.Length > 40)
				{
					actionLabel = actionLabel.Substring (0, 40) + "..)";
				}

				EditorGUILayout.BeginHorizontal ();
				_target.actions[i].isDisplayed = EditorGUILayout.Foldout (_target.actions[i].isDisplayed, actionLabel);
				if (!_target.actions[i].isEnabled)
				{
					EditorGUILayout.LabelField ("DISABLED", EditorStyles.boldLabel, GUILayout.Width (100f));
				}

				if (GUILayout.Button ("", CustomStyles.IconCog))
				{
					ActionSideMenu (_target.actions[i]);
				}
				EditorGUILayout.EndHorizontal ();
				
				if (_target.actions[i].isDisplayed)
				{
					if (!actionsManager.DoesActionExist (_target.actions[i].GetType ().ToString ()))
					{
						EditorGUILayout.HelpBox ("This Action type is not listed in the Actions Manager", MessageType.Warning);
					}
					else
					{
						int newTypeIndex = ActionListEditor.ShowTypePopup (_target.actions[i], typeIndex);
						if (newTypeIndex >= 0)
						{
							// Rebuild constructor if Subclass and type string do not match
							ActionEnd _end = new ActionEnd ();
							_end.resultAction = _target.actions[i].endAction;
							_end.skipAction = _target.actions[i].skipAction;
							_end.linkedAsset = _target.actions[i].linkedAsset;
							_end.linkedCutscene = _target.actions[i].linkedCutscene;

							Undo.RecordObject (_target, "Change Action type");

							RebuildAction (_target.actions[i], newTypeIndex, _target, i, _end);
						}

						EditorGUILayout.Space ();
						GUI.enabled = _target.actions[i].isEnabled;

						if (Application.isPlaying)
						{
							_target.actions[i].AssignValues (_target.GetParameters ());
						}

						_target.actions[i].ShowGUI (_target.GetParameters ());
					}
					GUI.enabled = true;
				}
				
				if (_target.actions[i].endAction == AC.ResultAction.Skip || _target.actions[i] is ActionCheck || _target.actions[i] is ActionCheckMultiple || _target.actions[i] is ActionParallel)
				{
					_target.actions[i].SkipActionGUI (_target.actions, _target.actions[i].isDisplayed);
				}
				
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space ();
			}
			
			if (GUILayout.Button("Add new Action"))
			{
				Undo.RecordObject (_target, "Create action");
				AddAction (ActionsManager.GetDefaultAction (), _target.actions.Count, _target);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}


		public static Action RebuildAction (AC.Action action, int typeIndex, ActionListAsset _target, int insertIndex = -1, ActionEnd _end = null)
		{
			ActionsManager actionsManager = AdvGame.GetReferences ().actionsManager;
			
			if (actionsManager)
			{
				bool _showComment = action.showComment;
				bool _showOutputSockets = action.showOutputSockets;
				string _comment = action.comment;
				
				ActionListAssetEditor.DeleteAction (action, _target);

				string className = actionsManager.AllActions [typeIndex].fileName;

				AC.Action newAction = (AC.Action) CreateInstance (className);
				newAction.name = className;

				if (_end != null)
				{
					newAction.endAction = _end.resultAction;
					newAction.skipAction = _end.skipAction;
					newAction.linkedAsset = _end.linkedAsset;
					newAction.linkedCutscene = _end.linkedCutscene;
				}

				newAction.showComment = _showComment;
				newAction.comment = _comment;
				newAction.showOutputSockets = _showOutputSockets;

				if (insertIndex >= 0)
				{
					_target.actions.Insert (insertIndex, newAction);
				}

				SyncAssetObjects (_target);

				return newAction;
			}
			
			return action;
		}


		public static void SyncAssetObjects (ActionListAsset actionListAsset)
		{
			bool modified = false;

			// Search for assets to delete
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath (AssetDatabase.GetAssetPath (actionListAsset));
			foreach (Object asset in assets)
			{
				Action actionAsset = asset as Action;
				if (actionAsset != null)
				{
					bool foundMatch = false;

					foreach (Action action in actionListAsset.actions)
					{
						if (actionAsset == action)
						{
							foundMatch = true;
							break;
						}
					}

					if (!foundMatch)
					{
						//Debug.LogWarning ("Found no match of asset " + actionAsset + " - deleting now");
						Undo.DestroyObjectImmediate (actionAsset);
						modified = true;
					}
				}
			}

			// Search for assets to add
			foreach (Action action in actionListAsset.actions)
			{
				if (action != null)
				{
					bool foundMatch = false;

					foreach (Object asset in assets)
					{
						Action actionAsset = asset as Action;
						if (actionAsset == action)
						{
							foundMatch = true;
							break;
						}
					}

					if (!foundMatch)
					{
						action.hideFlags = HideFlags.HideInHierarchy;
						AssetDatabase.AddObjectToAsset (action, actionListAsset);
						AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath (action));
						//Debug.LogWarning ("Found no match of " + action + " '" + action.name + "' in database - adding now to " + AssetDatabase.GetAssetPath (action));
						modified = true;
					}
				}
			}

			if (modified)
			{
				AssetDatabase.SaveAssets ();
				AssetDatabase.Refresh ();
			}
		}

		
		public static void DeleteAction (AC.Action action, ActionListAsset _target)
		{
			if (action != null) 
			{
				_target.actions.Remove (action);
				SyncAssetObjects (_target);
			}
		}
		
		
		public static Action AddAction (string className, int i, ActionListAsset _target)
		{
			if (string.IsNullOrEmpty (className))
			{
				return null;
			}

			List<int> idArray = new List<int>();
			foreach (AC.Action _action in _target.actions)
			{
				idArray.Add (_action.id);
			}
			idArray.Sort ();
			
			AC.Action newAction = (AC.Action) CreateInstance (className);
			newAction.name = className;

			// Update id based on array
			foreach (int _id in idArray.ToArray())
			{
				if (newAction.id == _id)
					newAction.id ++;
			}
			
			newAction.name = newAction.title;

			return AddAction (newAction, i, _target);
		}


		public static Action AddAction (AC.Action newAction, int i, ActionListAsset _target)
		{
			if (i < 0)
			{
				_target.actions.Add (newAction);
			}
			else
			{
				_target.actions.Insert (i, newAction);
			}

			SyncAssetObjects (_target);

			return newAction;
		}
		
		
		private void ActionSideMenu (AC.Action action)
		{
			ActionListAsset _target = (ActionListAsset) target;
			
			int i = _target.actions.IndexOf (action);
			actionToAffect = action;
			GenericMenu menu = new GenericMenu ();
			
			if (action.isEnabled)
			{
				menu.AddItem (new GUIContent ("Disable"), false, Callback, "Disable");
			}
			else
			{
				menu.AddItem (new GUIContent ("Enable"), false, Callback, "Enable");
			}
			menu.AddSeparator ("");
			if (_target.actions.Count > 1)
			{
				menu.AddItem (new GUIContent ("Cut"), false, Callback, "Cut");
			}
			menu.AddItem (new GUIContent ("Copy"), false, Callback, "Copy");
			if (AdvGame.copiedActions.Count > 0)
			{
				menu.AddItem (new GUIContent ("Paste after"), false, Callback, "Paste after");
			}
			menu.AddSeparator ("");
			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			if (i > 0 || i < _target.actions.Count-1)
			{
				menu.AddSeparator ("");
			}
			if (i > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (i < _target.actions.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom");
			}
			
			menu.ShowAsContext ();
		}


		private void Callback (object obj)
		{
			ActionListAsset t = (ActionListAsset) target;
			ModifyAction (t, actionToAffect, obj.ToString ());
			EditorUtility.SetDirty (t);
		}
		
		
		public static void ModifyAction (ActionListAsset _target, AC.Action _action, string callback)
		{
			ActionsManager actionsManager = AdvGame.GetReferences ().actionsManager;
			if (actionsManager == null)
			{
				return;
			}

			int i = -1;
			if (_action != null && _target.actions.IndexOf (_action) > -1)
			{
				i = _target.actions.IndexOf (_action);
			}

			bool doUndo = (callback != "Copy");

			if (doUndo)
			{
				Undo.SetCurrentGroupName (callback);
				Undo.RecordObjects (new UnityEngine.Object [] { _target }, callback);
				Undo.RecordObjects (_target.actions.ToArray (), callback);
			}

			switch (callback)
			{
			case "Enable":
				_target.actions [i].isEnabled = true;
				break;
				
			case "Disable":
				_target.actions [i].isEnabled = false;
				break;
				
			case "Cut":
				List<AC.Action> cutList = new List<AC.Action>();
				AC.Action cutAction = Object.Instantiate (_action) as AC.Action;
				cutAction.name = cutAction.name.Replace ("(Clone)", "");
				cutList.Add (cutAction);
				AdvGame.copiedActions = cutList;
				DeleteAction (_action, _target);
				break;
				
			case "Copy":
				List<AC.Action> copyList = new List<AC.Action>();
				AC.Action copyAction = Object.Instantiate (_action) as AC.Action;
				copyAction.ClearIDs ();
				copyAction.name = copyAction.name.Replace ("(Clone)", string.Empty);
				copyList.Add (copyAction);
				AdvGame.copiedActions = copyList;
				break;
				
			case "Paste after":
				List<AC.Action> pasteList = AdvGame.copiedActions;
				int j=i+1;
				foreach (AC.Action action in pasteList)
				{
					if (action == null)
					{
						ACDebug.LogWarning ("Error when pasting Action - cannot find original. Did you change scene before pasting? If you need to transfer Actions between scenes, copy them to an ActionList asset first.");
						continue;
					}

					AC.Action pastedAction = Object.Instantiate (action) as AC.Action;
					pastedAction.name = pastedAction.name.Replace ("(Clone)", string.Empty);
					AddAction (pastedAction, j, _target);
					j++;
				}
				break;
				
			case "Insert after":
				Action newAction = AddAction (ActionsManager.GetDefaultAction (), i+1, _target);
				newAction.endAction = _action.endAction;
				newAction.skipAction = -1;
				newAction.skipActionActual = _action.skipActionActual;
				break;
				
			case "Delete":
				DeleteAction (_action, _target);
				break;
				
			case "Move to top":
				_target.actions.Remove (_action);
				_target.actions.Insert (0, _action);
				break;
				
			case "Move up":
				_target.actions.Remove (_action);
				_target.actions.Insert (i-1, _action);
				break;
				
			case "Move to bottom":
				_target.actions.Remove (_action);
				_target.actions.Insert (_target.actions.Count, _action);
				break;
				
			case "Move down":
				_target.actions.Remove (_action);
				_target.actions.Insert (i+1, _action);
				break;
			}

			if (doUndo)
			{
				Undo.RecordObjects (new UnityEngine.Object [] { _target }, callback);
				Undo.RecordObjects (_target.actions.ToArray (), callback);
				Undo.CollapseUndoOperations (Undo.GetCurrentGroup ());
				EditorUtility.SetDirty (_target);
			}
		}

			
		public static ActionListAsset ResizeList (ActionListAsset _target, int listSize)
		{
			ActionsManager actionsManager = AdvGame.GetReferences ().actionsManager;
			
			string defaultAction = "";
			
			if (actionsManager)
			{
				defaultAction = ActionsManager.GetDefaultAction ();
			}
			
			if (_target.actions.Count < listSize)
			{
				// Increase size of list
				while (_target.actions.Count < listSize)
				{
					List<int> idArray = new List<int>();
					
					foreach (AC.Action _action in _target.actions)
					{
						idArray.Add (_action.id);
					}
					
					idArray.Sort ();

					Action newAction = (Action) CreateInstance (defaultAction);
					newAction.name = defaultAction;
					AddAction (newAction, -1, _target);
					
					// Update id based on array
					foreach (int _id in idArray.ToArray())
					{
						if (_target.actions [_target.actions.Count -1].id == _id)
							_target.actions [_target.actions.Count -1].id ++;
					}
				}
			}
			else if (_target.actions.Count > listSize)
			{
				// Decrease size of list
				while (_target.actions.Count > listSize)
				{
					Action removeAction = _target.actions [_target.actions.Count - 1];
					DeleteAction (removeAction, _target);
				}
			}

			return (_target);
		}


		public static void ResetList (ActionListAsset _targetAsset)
		{
			if (_targetAsset.actions.Count == 0 || (_targetAsset.actions.Count == 1 && _targetAsset.actions[0] == null))
			{
				if (_targetAsset.actions.Count == 1)
				{
					DeleteAction (_targetAsset.actions[0], _targetAsset);
				}

				AddAction (ActionsManager.GetDefaultAction (), -1, _targetAsset);
			}
		}


		public static void ShowPropertiesGUI (ActionListAsset _target)
		{
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Asset properties", EditorStyles.boldLabel);
			_target.actionListType = (ActionListType) EditorGUILayout.EnumPopup ("When running:", _target.actionListType);
			if (_target.actionListType == ActionListType.PauseGameplay)
			{
				_target.isSkippable = EditorGUILayout.Toggle ("Is skippable?", _target.isSkippable);
				_target.unfreezePauseMenus = EditorGUILayout.Toggle ("Unfreeze 'pause' Menus?", _target.unfreezePauseMenus);
			}
			_target.canRunMultipleInstances = EditorGUILayout.Toggle ("Can run multiple instances?", _target.canRunMultipleInstances);
			if (!_target.IsSkippable ())
			{
				_target.canSurviveSceneChanges = EditorGUILayout.Toggle ("Can survive scene changes?", _target.canSurviveSceneChanges);
			}
			_target.useParameters = EditorGUILayout.Toggle ("Use parameters?", _target.useParameters);
			if (_target.useParameters)
			{
				_target.revertToDefaultParametersAfterRunning = EditorGUILayout.ToggleLeft ("Revert to default parameter values after running?", _target.revertToDefaultParametersAfterRunning);
			}

			EditorGUILayout.EndVertical ();
			
			if (_target.useParameters)
			{
				EditorGUILayout.Space ();
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.LabelField ("Parameters", EditorStyles.boldLabel);
				ActionListEditor.ShowParametersGUI (null, _target, _target.GetParameters ());
				EditorGUILayout.EndVertical ();
			}

			_target.tagID = ActionListEditor.ShowTagUI (_target.actions.ToArray (), _target.tagID);
		}


		[OnOpenAssetAttribute(10)]
		public static bool OnOpenAsset (int instanceID, int line)
		{
			if (Selection.activeObject is ActionListAsset && instanceID == Selection.activeInstanceID)
			{
				ActionListAsset actionListAsset = (ActionListAsset) Selection.activeObject as ActionListAsset;
				ActionListEditorWindow.Init (actionListAsset);
				return true;
			}
			return false;
		}

	}

}