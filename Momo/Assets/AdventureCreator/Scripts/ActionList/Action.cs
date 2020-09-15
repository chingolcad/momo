/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Action.cs"
 * 
 *	This is the base class from which all Actions derive.
 *	We need blank functions Run, ShowGUI and SetLabel,
 *	which will be over-ridden by the subclasses.
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
	 * The base class from which all Actions derive.
	 * An Action is a ScriptableObject that performs a specific command, like pausing the game or moving a character.
	 * They are chained together in ActionLists to form cutscenes, gameplay logic etc.
	 */
	[System.Serializable]
	abstract public class Action : ScriptableObject
	{

		/** A unique identifier */
		public int id;
		/** The category (ActionList, Camera, Character, Container, Dialogue, Engine, Hotspot, Input, Inventory, Menu, Moveable, Object, Player, Save, Sound, ThirdParty, Variable, Custom) */
		public ActionCategory category = ActionCategory.Custom;
		/** The Action's title */
		public string title;
		/** A brief description about what the Action does */
		public string description;
		/** If True, the Action is expanded in the Editor */
		public bool isDisplayed;
		/** If True, then the comment text will be shown in the Action's UI in the ActionList Editor window */
		public bool showComment;
		/** A user-defined comment about the Action's purpose */
		public string comment;

		/** How many output sockets the Action has */
		public int numSockets = 1;
		/** If True, the ActionList will wait until the Action has finished running before continuing */
		public bool willWait;

		/** If True, then the Action is running */
		[System.NonSerialized] public bool isRunning;
		/** What happened the last time the Action was run (used when skipping Actions) */
		[System.NonSerialized] public ActionEnd lastResult = new ActionEnd (-10);

		/** What happens when the Action ends (Continue, Stop, Skip, RunCutscene) */
		public ResultAction endAction = ResultAction.Continue;
		/** The index number of the Action to skip to, if resultAction = ResultAction.Skip */
		public int skipAction = -1;
		/** The Action to skip to, if resultAction = ResultAction.Skip */
		public AC.Action skipActionActual;
		/** The Cutscene to run, if resultAction = ResultAction.RunCutscene and the Action is in a scene-based ActionList */
		public Cutscene linkedCutscene;
		/** The ActionListAsset to run, if resultAction = ResultAction.RunCutscene and the Action is in an ActionListAsset file */
		public ActionListAsset linkedAsset;

		/** If True, the Action is enabled and can be run */
		public bool isEnabled = true;
		/** If True, the Action is stored within an ActionListAsset file */
		public bool isAssetFile = false;

		/** If True, the Action has been marked for modification in ActionListEditorWindow */
		[System.NonSerialized] public bool isMarked = false;
		/** If True, the Editor will pause when this Action is run in-game */
		public bool isBreakPoint = false;

		#if UNITY_EDITOR
		public Rect nodeRect = new Rect (0,0,300,60);
		public Color overrideColor = Color.white;
		public bool showOutputSockets = true;
		/** The Action's parent ActionList, if in the scene.  This is not 100% reliable and should not be used in custom scripts */
		public ActionList parentActionListInEditor = null;
		#endif


		/**
		 * The default Constructor.
		 */
		public Action ()
		{
			this.isDisplayed = true;
		}
		

		/**
		 * <summary>Runs the Action.</summary>
		 * <returns>The time, in seconds, to wait before ActionList calls this function again.  If 0, then the Action will not be re-run.  If >0, and isRunning = True, then the Action will be re-run isRunning = False</returns>
		 */
		public virtual float Run ()
		{
			return 0f;
		}


		/**
		 * Runs the Action instantaneously.
		 */
		public virtual void Skip ()
		{
			Run ();
		}


		public float defaultPauseTime
		{
			get
			{
				return -1f;
			}
		}
		

		/**
		 * <summary>Generates an ActionEnd class that describes what should happen after the Action has run.</summary>
		 * <param name = "actions">The List of Actions that the Action is a part of</param>
		 * <returns>An ActionEnd class that describes what should happen after the Action has run</returns>
		 */
		public virtual ActionEnd End (List<Action> actions)
		{
			return GenerateActionEnd (endAction, linkedAsset, linkedCutscene, skipAction, skipActionActual, actions);
		}


		/**
		 * <summary>Prints the Action's comment, if applicable, to the Console.</summary>
		 * <param name = "actionList">The associated ActionList of which this Action is a part of</param>
		 */
		public void PrintComment (ActionList actionList)
		{
			if (showComment && !string.IsNullOrEmpty (comment))
			{
				string log = AdvGame.ConvertTokens (comment, 0, null, actionList.parameters);
				log += "\n" + "(From Action '(" + actionList.actions.IndexOf (this) + ") " + KickStarter.actionsManager.GetActionTypeLabel (this) + "' in ActionList '" + actionList.gameObject.name + "')";
				ACDebug.Log (log, actionList);
			}
		}
		
		
		#if UNITY_EDITOR

		/**
		 * <summary>Shows the Action's GUI when its parent ActionList / ActionListAsset uses parameters.</summary>
		 * <param name = "parameters">A List of parameters available in the parent ActionList / ActionListAsset</param>
		 */
		public virtual void ShowGUI (List<ActionParameter> parameters)
		{
			ShowGUI ();
		}


		/**
		 * Shows the Action's GUI.
		 */
		public virtual void ShowGUI ()
		{ }


		protected void AfterRunningOption ()
		{
			EditorGUILayout.Space ();
			endAction = (ResultAction) EditorGUILayout.EnumPopup ("After running:", (ResultAction) endAction);
			
			if (endAction == ResultAction.RunCutscene)
			{
				if (isAssetFile)
				{
					linkedAsset = ActionListAssetMenu.AssetGUI ("ActionList to run:", linkedAsset);
				}
				else
				{
					linkedCutscene = ActionListAssetMenu.CutsceneGUI ("Cutscene to run:", linkedCutscene);
				}
			}
		}
		

		public virtual void SkipActionGUI (List<Action> actions, bool showGUI)
		{
			if (skipAction == -1)
			{
				// Set default
				int i = actions.IndexOf (this);
				if (actions.Count > i+1)
				{
					skipAction = i+1;
				}
				else
				{
					skipAction = i;
				}
			}

			int tempSkipAction = skipAction;
			List<string> labelList = new List<string>();

			if (skipActionActual)
			{
				bool found = false;

				for (int i = 0; i < actions.Count; i++)
				{
					//labelList.Add ("(" + i.ToString () + ") " + actions[i].category.ToString () + ": " + actions [i].title);
					labelList.Add ("(" + i.ToString () + ") " + ((KickStarter.actionsManager != null) ? KickStarter.actionsManager.GetActionTypeLabel (actions[i]) : string.Empty));

					if (skipActionActual == actions [i])
					{
						skipAction = i;
						found = true;
					}
				}
				
				if (!found)
				{
					skipAction = tempSkipAction;
				}
			}

			if (skipAction >= actions.Count)
			{
				skipAction = actions.Count - 1;
			}

			if (showGUI)
			{
				if (actions.Count > 1)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField ("  Action to skip to:", GUILayout.Width (155f));
					tempSkipAction = EditorGUILayout.Popup (skipAction, labelList.ToArray());
					EditorGUILayout.EndHorizontal();
					skipAction = tempSkipAction;
				}
				else
				{
					EditorGUILayout.HelpBox ("Cannot skip action - no further Actions available", MessageType.Warning);
					return;
				}
			}

			skipActionActual = actions [skipAction];
		}


		/**
		 * <summary>Called when an ActionList has been converted from a scene-based object to an asset file.
		 * Within it, AssignConstantID should be called for each of the Action's Constant ID numbers, which will assign a new number if one does not already exist, based on the referenced scene object.</summary>
		 * <param name = "saveScriptsToo">If True, then the Action shall attempt to add the appropriate 'Remember' script to reference GameObjects as well.</param>
		 * <param name = "fromAssetFile">If True, then the Action is placed in an ActionListAsset file</param>
		 */
		public virtual void AssignConstantIDs (bool saveScriptsToo = false, bool fromAssetFile = false)
		{ }


		/**
		 * <summary>Gets a string that is shown after the Action's title in the Editor to help the user understand what it does.</summary>
		 * <returns>A string that is shown after the Action's title in the Editor to help the user understand what it does.</returns>
		 */
		public virtual string SetLabel ()
		{
			return (string.Empty);
		}


		public virtual void DrawOutWires (List<Action> actions, int i, int offset, Vector2 scrollPosition)
		{
			if (endAction == ResultAction.Continue)
			{
				if (actions.Count > i+1)
				{
					AdvGame.DrawNodeCurve (new Rect (nodeRect.position - scrollPosition, nodeRect.size),
										   new Rect (actions[i+1].nodeRect.position - scrollPosition, actions[i + 1].nodeRect.size),
										   new Color (0.3f, 0.3f, 1f, 1f), 10, false, isDisplayed);
				}
			}
			else if (endAction == ResultAction.Skip && showOutputSockets)
			{
				if (actions.Contains (skipActionActual))
				{
					AdvGame.DrawNodeCurve (new Rect (nodeRect.position - scrollPosition, nodeRect.size),
										   new Rect (skipActionActual.nodeRect.position - scrollPosition, skipActionActual.nodeRect.size),
										   new Color (0.3f, 0.3f, 1f, 1f), 10, false, isDisplayed);
				}
			}
		}


		public static int ChooseParameterGUI (string label, List<ActionParameter> _parameters, int _parameterID, ParameterType _expectedType, int excludeParameterID = -1, string tooltip = "")
		{
			if (_parameters == null || _parameters.Count == 0)
			{
				return -1;
			}
			
			// Don't show list if no parameters of the correct type are present
			bool found = false;
			foreach (ActionParameter _parameter in _parameters)
			{
				if (excludeParameterID < 0 || excludeParameterID != _parameter.ID)
				{
					if (_parameter.parameterType == _expectedType ||
						(_expectedType == ParameterType.GameObject && _parameter.parameterType == ParameterType.ComponentVariable))
					{
						found = true;
					}
				}
			}
			if (!found)
			{
				return -1;
			}
			
			int chosenNumber = 0;
			List<PopupSelectData> popupSelectDataList = new List<PopupSelectData>();
			for (int i=0; i<_parameters.Count; i++)
			{
				if (excludeParameterID < 0 || excludeParameterID != _parameters[i].ID)
				{
					if (_parameters[i].parameterType == _expectedType ||
						(_expectedType == ParameterType.GameObject && _parameters[i].parameterType == ParameterType.ComponentVariable))
					{
						PopupSelectData popupSelectData = new PopupSelectData (_parameters[i].ID, _parameters[i].ID + ": " + _parameters[i].label, i);
						popupSelectDataList.Add (popupSelectData);

						if (popupSelectData.ID == _parameterID)
						{
							chosenNumber = popupSelectDataList.Count;
						}
					}
				}
			}

			List<string> labelList = new List<string>();
			labelList.Add ("(No parameter)");
			foreach (PopupSelectData popupSelectData in popupSelectDataList)
			{
				labelList.Add (popupSelectData.label);
			}

			if (!string.IsNullOrEmpty (label))
			{
				chosenNumber = CustomGUILayout.Popup ("-> " + label, chosenNumber, labelList.ToArray (), "", tooltip) - 1;
			}
			else
			{
				chosenNumber = EditorGUILayout.Popup (chosenNumber, labelList.ToArray ()) - 1;
			}

			if (chosenNumber < 0)
			{
				return -1;
			}
			int rootIndex = popupSelectDataList[chosenNumber].rootIndex;
			return _parameters [rootIndex].ID;
		}


		public static int ChooseParameterGUI (List<ActionParameter> _parameters, int _parameterID)
		{
			if (_parameters == null || _parameters.Count == 0)
			{
				return -1;
			}

			int chosenNumber = 0;
			List<string> labelList = new List<string>();
			foreach (ActionParameter _parameter in _parameters)
			{
				labelList.Add (_parameter.ID + ": " + _parameter.label);
				if (_parameter.ID == _parameterID)
				{
					chosenNumber = _parameters.IndexOf (_parameter);
				}
			}
			
			chosenNumber = EditorGUILayout.Popup ("Parameter:", chosenNumber, labelList.ToArray ());
			if (chosenNumber < 0)
			{
				return -1;
			}
			return _parameters [chosenNumber].ID;
		}


		public int FieldToID <T> (T field, int _constantID) where T : Behaviour
		{
			if (field != null)
			{
				if (isAssetFile || (!isAssetFile && !field.gameObject.activeInHierarchy))
				{
					if (field.GetComponent <ConstantID>())
					{
						if (!field.gameObject.activeInHierarchy && field.GetComponent <ConstantID>().constantID == 0)
						{
							UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
						}
						_constantID = field.GetComponent <ConstantID>().constantID;
					}
					else if (field.GetComponent <Player>() == null)
					{
						UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
					}
					return _constantID;
				}
				if (!Application.isPlaying)
				{
					return 0;
				}
			}
			return _constantID;
		}


		public int FieldToID (Collider field, int _constantID)
		{
			if (field != null)
			{
				if (isAssetFile || (!isAssetFile && !field.gameObject.activeInHierarchy))
				{
					if (field.GetComponent <ConstantID>())
					{
						if (!field.gameObject.activeInHierarchy && field.GetComponent <ConstantID>().constantID == 0)
						{
							UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
						}
						_constantID = field.GetComponent <ConstantID>().constantID;
					}
					else if (field.GetComponent <Player>() == null)
					{
						UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
					}
					return _constantID;
				}
				if (!Application.isPlaying)
				{
					return 0;
				}
			}
			return _constantID;
		}


		public void AddSaveScript <T> (Behaviour field) where T : ConstantID
		{
			if (field != null)
			{
				if (field.gameObject.GetComponent <T>() == null)
				{
					T newComponent = UnityVersionHandler.AddConstantIDToGameObject <T> (field.gameObject);

					if (!(newComponent is ConstantID))
					{
						ACDebug.Log ("Added '" + newComponent.GetType ().ToString () + "' component to " + field.gameObject.name, field.gameObject);
					}

					EditorUtility.SetDirty (this);
				}
			}
		}


		public void AddSaveScript <T> (GameObject _gameObject) where T : ConstantID
		{
			if (_gameObject != null)
			{
				if (_gameObject.GetComponent <T>() == null)
				{
					T newComponent = UnityVersionHandler.AddConstantIDToGameObject <T> (_gameObject);

					ACDebug.Log ("Added '" + newComponent.GetType ().ToString () + "' component to " + _gameObject.name, _gameObject);

					EditorUtility.SetDirty (this);
				}
			}
		}


		public void AssignConstantID <T> (T field, int _constantID, int _parameterID) where T : Behaviour
		{
			if (_parameterID >= 0)
			{
				_constantID = 0;
			}
			else
			{
				_constantID = FieldToID <T> (field, _constantID);
			}
		}


		public void AssignConstantID (Collider field, int _constantID, int _parameterID)
		{
			if (_parameterID >= 0)
			{
				_constantID = 0;
			}
			else
			{
				_constantID = FieldToID (field, _constantID);
			}
		}


		public void AssignConstantID (Transform field, int _constantID, int _parameterID)
		{
			if (_parameterID >= 0)
			{
				_constantID = 0;
			}
			else
			{
				_constantID = FieldToID (field, _constantID);
			}
		}


		public void AssignConstantID (GameObject field, int _constantID, int _parameterID)
		{
			if (_parameterID >= 0)
			{
				_constantID = 0;
			}
			else
			{
				_constantID = FieldToID (field, _constantID);
			}
		}
		
		
		public T IDToField <T> (T field, int _constantID, bool moreInfo) where T : Behaviour
		{
			if (isAssetFile || (!isAssetFile && (field == null || !field.gameObject.activeInHierarchy)))
			{
				T newField = field;
				if (_constantID != 0)
				{
					newField = Serializer.returnComponent <T> (_constantID);
					if (field != null && field.GetComponent <ConstantID>() != null && field.GetComponent <ConstantID>().constantID == _constantID)
					{}
					else if (newField != null && !Application.isPlaying)
					{
						field = newField;
					}

					EditorGUILayout.BeginVertical ("Button");
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
					if (field == null)
					{
						if (GUILayout.Button ("Search scenes", EditorStyles.miniButton))
						{
							AdvGame.FindObjectWithConstantID (_constantID);
						}
					}
					EditorGUILayout.EndHorizontal ();
					
					if (field == null && moreInfo)
					{
						EditorGUILayout.HelpBox ("Further controls cannot display because the referenced object cannot be found.", MessageType.Warning);
					}
					EditorGUILayout.EndVertical ();
				}
			}
			return field;
		}


		public Collider IDToField (Collider field, int _constantID, bool moreInfo)
		{
			if (isAssetFile || (!isAssetFile && (field == null || !field.gameObject.activeInHierarchy)))
			{
				Collider newField = field;
				if (_constantID != 0)
				{
					newField = Serializer.returnComponent <Collider> (_constantID);
					if (field != null && field.GetComponent <ConstantID>() != null && field.GetComponent <ConstantID>().constantID == _constantID)
					{}
					else if (newField != null && !Application.isPlaying)
					{
						field = newField;
					}

					EditorGUILayout.BeginVertical ("Button");
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
					if (field == null)
					{
						if (GUILayout.Button ("Search scenes", EditorStyles.miniButton))
						{
							AdvGame.FindObjectWithConstantID (_constantID);
						}
					}
					EditorGUILayout.EndHorizontal ();
					
					if (field == null && moreInfo)
					{
						EditorGUILayout.HelpBox ("Further controls cannot display because the referenced object cannot be found.", MessageType.Warning);
					}
					EditorGUILayout.EndVertical ();
				}
			}
			return field;
		}
		
		
		public int FieldToID (Transform field, int _constantID)
		{
			if (field != null)
			{
				if (isAssetFile || (!isAssetFile && !field.gameObject.activeInHierarchy))
				{
					if (field.GetComponent <ConstantID>())
					{
						if (!field.gameObject.activeInHierarchy && field.GetComponent <ConstantID>().constantID == 0)
						{
							UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
						}
						_constantID = field.GetComponent <ConstantID>().constantID;
					}
					else if (field.GetComponent <Player>() == null)
					{
						UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
					}
					return _constantID;
				}
				if (!Application.isPlaying)
				{
					return 0;
				}
			}
			return _constantID;
		}
		
		
		public Transform IDToField (Transform field, int _constantID, bool moreInfo)
		{
			if (isAssetFile || (!isAssetFile && (field == null || !field.gameObject.activeInHierarchy)))
			{
				if (_constantID != 0)
				{
					ConstantID newID = Serializer.returnComponent <ConstantID> (_constantID);
					if (field != null && field.GetComponent <ConstantID>() != null && field.GetComponent <ConstantID>().constantID == _constantID)
					{}
					else if (newID != null && !Application.isPlaying)
					{
						field = newID.transform;
					}

					EditorGUILayout.BeginVertical ("Button");
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
					if (field == null)
					{
						if (GUILayout.Button ("Search scenes", EditorStyles.miniButton))
						{
							AdvGame.FindObjectWithConstantID (_constantID);
						}
					}
					EditorGUILayout.EndHorizontal ();
					
					if (field == null && moreInfo)
					{
						EditorGUILayout.HelpBox ("Further controls cannot display because the referenced object cannot be found.", MessageType.Warning);
					}
					EditorGUILayout.EndVertical ();
				}
			}
			return field;
		}


		public int FieldToID (GameObject field, int _constantID)
		{
			return FieldToID (field, _constantID, false);
		}

		
		public int FieldToID (GameObject field, int _constantID, bool alwaysAssign)
		{
			return FieldToID (field, _constantID, alwaysAssign, isAssetFile);
		}


		public static int FieldToID (GameObject field, int _constantID, bool alwaysAssign, bool _isAssetFile)
		{
			if (field != null)
			{
				if (alwaysAssign || _isAssetFile || (!_isAssetFile && !field.gameObject.activeInHierarchy))
				{
					if (field.GetComponent <ConstantID>())
					{
						if (!field.gameObject.activeInHierarchy && field.GetComponent <ConstantID>().constantID == 0)
						{
							UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
						}
						_constantID = field.GetComponent <ConstantID>().constantID;
					}
					else if (field.GetComponent <Player>() == null)
					{
						UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
					}
					return _constantID;
				}
				if (!Application.isPlaying)
				{
					return 0;
				}
			}
			return _constantID;
		}


		public GameObject IDToField (GameObject field, int _constantID, bool moreInfo)
		{
			return IDToField (field, _constantID, moreInfo, false);
		}
		
		
		public GameObject IDToField (GameObject field, int _constantID, bool moreInfo, bool alwaysShow)
		{
			return IDToField (field, _constantID, moreInfo, alwaysShow, isAssetFile);
		}


		public static GameObject IDToField (GameObject field, int _constantID, bool moreInfo, bool alwaysShow, bool _isAssetFile)
		{
			if (alwaysShow || _isAssetFile || (!_isAssetFile && (field == null || !field.activeInHierarchy)))
			{
				if (_constantID != 0)
				{
					ConstantID newID = Serializer.returnComponent <ConstantID> (_constantID);
					if (field != null && field.GetComponent <ConstantID>() != null && field.GetComponent <ConstantID>().constantID == _constantID)
					{}
					else if (newID != null && !Application.isPlaying)
					{
						field = newID.gameObject;
					}

					EditorGUILayout.BeginVertical ("Button");
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
					if (field == null)
					{
						if (GUILayout.Button ("Search scenes", EditorStyles.miniButton))
						{
							AdvGame.FindObjectWithConstantID (_constantID);
						}
					}
					EditorGUILayout.EndHorizontal ();
					
					if (field == null && moreInfo)
					{
						EditorGUILayout.HelpBox ("Further controls cannot display because the referenced object cannot be found.", MessageType.Warning);
					}
					EditorGUILayout.EndVertical ();
				}
			}
			return field;
		}


		/**
		 * <summary>Converts the Action's references from a given local variable to a given global variable</summary>
		 * <param name = "oldLocalID">The ID number of the old local variable</param>
		 * <param name = "newGlobalID">The ID number of the new global variable</param>
		 * <returns>True if the Action was amended</returns>
		 */
		public virtual bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			string newComment = AdvGame.ConvertLocalVariableTokenToGlobal (comment, oldLocalID, newGlobalID);
			bool wasAmended = (comment != newComment);
			comment = newComment;
			return wasAmended;
		}


		/**
		 * <summary>Converts the Action's references from a given global variable to a given local variable</summary>
		 * <param name = "oldGlobalID">The ID number of the old global variable</param>
		 * <param name = "newLocalID">The ID number of the new local variable</param>
		 * <param name = "isCorrectScene">If True, the local variable is in the same scene as this ActionList.  Otherwise, no change will made, but the return value will be the same</param>
		 * <returns>True if the Action is affected</returns>
		 */
		public virtual bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			string newComment = AdvGame.ConvertGlobalVariableTokenToLocal (comment, oldGlobalID, newLocalID);
			if (comment != newComment)
			{
				if (isCorrectScene)
				{
					comment = newComment;
				}
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets the number of references the Action makes to a local or global variable</summary>
		 * <param name = "parameters">The List of ActionParameters associated with the ActionList that contains the Action</param>
		 * <param name = "location">The variable's location (Global, Local)</param>
		 * <param name = "varID">The variable's ID number</param>
		 * <returns>The number of references the Action makes to the variable</returns>
		 */
		public virtual int GetVariableReferences (List<ActionParameter> parameters, VariableLocation location, int varID)
		{
			return GetVariableReferences (parameters, location, varID, null);
		}


		public virtual int GetVariableReferences (List<ActionParameter> parameters, VariableLocation location, int varID, Variables variables)
		{
			string tokenText = AdvGame.GetVariableTokenText (location, varID);

			if (!string.IsNullOrEmpty (tokenText) && comment != null && comment.Contains (tokenText))
			{
				return 1;
			}
			return 0;
		}


		public virtual int GetInventoryReferences (List<ActionParameter> parameters, int invID)
		{
			return 0;
		}


		public virtual int GetDocumentReferences (List<ActionParameter> parameters, int invID)
		{
			return 0;
		}


		/**
		 * <summary>Checks if the Action makes reference to a particular GameObject</summary>
		 * <param name = "gameObject">The GameObject to check for</param>
		 * <param name = "id">The GameObject's associated ConstantID value</param>
		 * <returns>True if the Action references the GameObject</param>
		 */
		public virtual bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (!isAssetFile && endAction == ResultAction.RunCutscene)
			{
				if (linkedCutscene != null && linkedCutscene.gameObject == gameObject) return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the Action makes reference to a particular ActionList asset</summary>
		 * <param name = "actionListAsset">The ActionList to check for</param>
		 * <returns>True if the Menu references the ActionList</param>
		 */
		public virtual bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (isAssetFile && endAction == ResultAction.RunCutscene)
			{
				if (linkedAsset == actionListAsset) return true;
			}
			return false;
		}


		public static int ChoosePlayerGUI (int _playerID)
		{
			SettingsManager settingsManager = KickStarter.settingsManager;
			if (settingsManager == null) return _playerID;

			List<string> labelList = new List<string>();
			int i = 0;
			int playerNumber = -1;

			if (settingsManager.players.Count > 0)
			{
				foreach (PlayerPrefab playerPrefab in settingsManager.players)
				{
					if (playerPrefab.playerOb != null)
					{
						labelList.Add ("(" + playerPrefab.ID.ToString () + ") " + playerPrefab.playerOb.name);
					}
					else
					{
						labelList.Add ("(Undefined prefab)");
					}
					
					// If a player has been removed, make sure selected player is still valid
					if (playerPrefab.ID == _playerID)
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
					_playerID = 0;
				}

				playerNumber = EditorGUILayout.Popup ("Player:", playerNumber, labelList.ToArray());
				_playerID = settingsManager.players[playerNumber].ID;

				return _playerID;
			}

			return _playerID;
		}

		#endif

		#if UNITY_EDITOR
		private ActionList parentList;
		#endif


		/**
		 * <summary>Passes the ActionList that the Action is a part of to the Action class. This is run before the Action is called or displayed in an Editor.</summary>
		 * <param name = "actionList">The ActionList that the Action is contained in</param>
		 */
		public virtual void AssignParentList (ActionList actionList)
		{
			#if UNITY_EDITOR
			parentList = actionList;
			#endif
		}


		protected void Log (string message, Object context = null)
		{
			#if UNITY_EDITOR
			ACDebug.Log (message, parentList, this, context);
			#else
			ACDebug.Log (message, context);
			#endif
		}


		protected void LogWarning (string message, Object context = null)
		{
			#if UNITY_EDITOR
			ACDebug.LogWarning (message, parentList, this, context);
			#else
			ACDebug.LogWarning (message, context);
			#endif
		}


		protected void LogError (string message, Object context = null)
		{
			#if UNITY_EDITOR
			ACDebug.LogError (message, parentList, this, context);
			#else
			ACDebug.LogError (message, context);
			#endif
		}


		/**
		 * <summary>Overwrites any appropriate variables with values set using parameters, or from ConstantID numbers.</summary>
		 * <param name = "parameters">A List of parameters that overwrite variable values</param>
		 */
		public virtual void AssignValues (List<ActionParameter> parameters)
		{
			AssignValues ();
		}


		/**
		 * Overwrites any appropriate variables from ConstantID numbers.
		 */
		public virtual void AssignValues ()
		{ }


		protected ActionParameter GetParameterWithID (List<ActionParameter> parameters, int _id)
		{
			if (parameters != null && _id >= 0)
			{
				foreach (ActionParameter _parameter in parameters)
				{
					if (_parameter.ID == _id)
					{
						return _parameter;
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Resets any runtime values that are necessary to run the Action succesfully</summary>
		 * <param name = "actionList">The ActionList that the Action is a part of<param>
		 */
		public virtual void Reset (ActionList actionList)
		{
			isRunning = false;
		}


		protected string AssignString (List<ActionParameter> parameters, int _parameterID, string field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.String)
			{
				return (parameter.stringValue);
			}
			return field;
		}


		/**
		 * <summary>Replaces a boolean based on an ActionParameter, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Transform</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the float</param>
		 * <param name = "field">The bool to replace</param>
		 * <returns>The replaced BoolValue enum, or field if no replacements were found</returns>
		 */
		public BoolValue AssignBoolean (List<ActionParameter> parameters, int _parameterID, BoolValue field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.Boolean)
			{
				if (parameter.intValue == 1)
				{
					return BoolValue.True;
				}
				return BoolValue.False;
			}
			return field;
		}


		/**
		 * <summary>Replaces an integer based on an ActionParameter, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Transform</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the float</param>
		 * <param name = "field">The integer to replace</param>
		 * <returns>The replaced integer, or field if no replacements were found</returns>
		 */
		public int AssignInteger (List<ActionParameter> parameters, int _parameterID, int field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.Integer)
			{
				return (parameter.intValue);
			}
			return field;
		}


		/**
		 * <summary>Replaces a float based on an ActionParameter, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Transform</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the float</param>
		 * <param name = "field">The float to replace</param>
		 * <returns>The replaced float, or field if no replacements were found</returns>
		 */
		public float AssignFloat (List<ActionParameter> parameters, int _parameterID, float field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.Float)
			{
				return (parameter.floatValue);
			}
			return field;
		}


		protected Vector3 AssignVector3 (List<ActionParameter> parameters, int _parameterID, Vector3 field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.Vector3)
			{
				return (parameter.vector3Value);
			}
			return field;
		}


		protected int AssignVariableID (List<ActionParameter> parameters, int _parameterID, int field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && (parameter.parameterType == ParameterType.GlobalVariable || parameter.parameterType == ParameterType.LocalVariable))
			{
				return (parameter.intValue);
			}
			return field;
		}


		protected GVar AssignVariable (List<ActionParameter> parameters, int _parameterID, GVar field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null)
			{
				return (parameter.GetVariable ());
			}
			return field;
		}


		protected Variables AssignVariablesComponent (List<ActionParameter> parameters, int _parameterID, Variables field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
			{
				return (parameter.variables);
			}
			return field;
		}


		protected int AssignInvItemID (List<ActionParameter> parameters, int _parameterID, int field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.InventoryItem)
			{
				return (parameter.intValue);
			}
			return field;
		}


		protected int AssignDocumentID (List<ActionParameter> parameters, int _parameterID, int field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.Document)
			{
				return (parameter.intValue);
			}
			return field;
		}


		/**
		 * <summary>Replaces a Transform based on an ActionParameter or ConstantID instance, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Transform</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the Transform</param>
		 * <param name = "_constantID">If !=0, The ConstantID number of the Transform to replace field with</param>
		 * <param name = "field">The Transform to replace</param>
		 * <returns>The replaced Transform, or field if no replacements were found</returns>
		 */
		public Transform AssignFile (List<ActionParameter> parameters, int _parameterID, int _constantID, Transform field)
		{
			Transform file = field;
			
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			
			if (parameter != null && parameter.parameterType == ParameterType.GameObject)
			{
				if (parameter.intValue != 0)
				{
					ConstantID idObject = Serializer.returnConstantID (parameter.intValue);
					if (idObject != null)
					{
						file = idObject.gameObject.transform;
					}
				}

				if (file == null)
				{
					if (/*!isAssetFile && */parameter.gameObject != null)
					{
						file = parameter.gameObject.transform;
					}
					else if (parameter.intValue != 0)
					{
						ConstantID idObject = Serializer.returnConstantID (parameter.intValue);
						if (idObject != null)
						{
							file = idObject.gameObject.transform;
						}
					}
				}
			}
			else if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
			{
				if (parameter.variables != null)
				{
					file = parameter.variables.transform;
				}
			}
			else if (_constantID != 0)
			{
				ConstantID idObject = Serializer.returnConstantID (_constantID);
				if (idObject != null)
				{
					file = idObject.gameObject.transform;
				}
			}
			
			return file;
		}


		/**
		 * <summary>Replaces a Collider based on an ActionParameter or ConstantID instance, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Collider</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the Collider</param>
		 * <param name = "_constantID">If !=0, The ConstantID number of the Collider to replace field with</param>
		 * <param name = "field">The Collider to replace</param>
		 * <returns>The replaced Collider, or field if no replacements were found</returns>
		 */
		public Collider AssignFile (List<ActionParameter> parameters, int _parameterID, int _constantID, Collider field)
		{
			Collider file = field;
			
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.GameObject)
			{
				file = null;
				if (parameter.intValue != 0)
				{
					file = Serializer.returnComponent <Collider> (parameter.intValue);
				}
				if (file == null)
				{
					if (parameter.gameObject != null && parameter.gameObject.GetComponent <Collider>())
					{
						file = parameter.gameObject.GetComponent <Collider>();
					}
					else if (parameter.intValue != 0)
					{
						file = Serializer.returnComponent <Collider> (parameter.intValue);
					}
				}
			}
			else if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
			{
				if (parameter.variables != null)
				{
					file = parameter.variables.GetComponent <Collider>();;
				}
			}
			else if (_constantID != 0)
			{
				Collider newField = Serializer.returnComponent <Collider> (_constantID);
				if (newField != null)
				{
					file = newField;
				}

			}
			return file;
		}


		/**
		 * <summary>Replaces a GameObject based on an ActionParameter or ConstantID instance, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the GameObject</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the GameObject</param>
		 * <param name = "_constantID">If !=0, The ConstantID number of the GameObject to replace field with</param>
		 * <param name = "field">The GameObject to replace</param>
		 * <returns>The replaced GameObject, or field if no replacements were found</returns>
		 */
		protected GameObject AssignFile (List<ActionParameter> parameters, int _parameterID, int _constantID, GameObject field)
		{
			GameObject file = field;
			
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.GameObject)
			{
				file = null;
				if (parameter.intValue != 0)
				{
					ConstantID idObject = Serializer.returnConstantID (parameter.intValue);
					if (idObject != null)
					{
						file = idObject.gameObject;
					}
				}

				if (file == null)
				{
					if (parameter.gameObject != null)
					{
						file = parameter.gameObject;
					}
					else if (parameter.intValue != 0)
					{
						ConstantID idObject = Serializer.returnConstantID (parameter.intValue);
						if (idObject != null)
						{
							file = idObject.gameObject;
						}
					}
				}
			}
			else if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
			{
				if (parameter.variables != null)
				{
					file = parameter.variables.gameObject;
				}
			}
			else if (_constantID != 0)
			{
				ConstantID idObject = Serializer.returnConstantID (_constantID);
				if (idObject != null)
				{
					file = idObject.gameObject;
				}
			}
			
			return file;
		}


		/**
		 * <summary>Replaces a GameObject based on an ActionParameter or ConstantID instance, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the GameObject</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the GameObject</param>
		 * <param name = "field">The Object to replace</param>
		 * <returns>The replaced Object, or field if no replacements were found</returns>
		 */
		protected Object AssignObject <T> (List<ActionParameter> parameters, int _parameterID, Object field) where T : Object
		{
			Object file = field;
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);

			if (parameter != null && parameter.parameterType == ParameterType.UnityObject)
			{
				file = null;
				if (parameter.objectValue != null)
				{
					if (parameter.objectValue is T)
					{
						file = parameter.objectValue;
					}
					else
					{
						ACDebug.LogWarning ("Cannot convert " + parameter.objectValue.name + " to type '" + typeof (T) + "'");
					}
				}
			}

			return file;
		}


		/**
		 * <summary>Replaces a generic Behaviour based on an ActionParameter or ConstantID instance, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Behaviour</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the Behaviour</param>
		 * <param name = "_constantID">If !=0, The ConstantID number of the Behaviour to replace field with</param>
		 * <param name = "field">The Behaviour to replace</param>
		 * <param name = "doLog">If True, and no file is found when one is expected, a warning message will be displayed in the Console</param>
		 * <returns>The replaced Behaviour, or field if no replacements were found</returns>
		 */
		public T AssignFile <T> (List<ActionParameter> parameters, int _parameterID, int _constantID, T field, bool doLog = true) where T : Behaviour
		{
			T file = field;
			
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.GameObject)
			{
				file = null;
				if (parameter.intValue != 0)
				{
					file = Serializer.returnComponent <T> (parameter.intValue);

					if (file == null && parameter.gameObject != null && parameter.intValue != -1 && doLog)
					{
						ACDebug.LogWarning ("No " + typeof(T) + " component attached to " + parameter.gameObject + "!", parameter.gameObject);
					}
				}
				if (file == null)
				{
					if (parameter.gameObject != null && parameter.gameObject.GetComponent <T>())
					{
						file = parameter.gameObject.GetComponent <T>();
					}
					else if (parameter.intValue != 0)
					{
						file = Serializer.returnComponent <T> (parameter.intValue);
					}
					else if (parameter.gameObject != null && parameter.gameObject.GetComponent <T>() == null && doLog)
					{
						ACDebug.LogWarning ("No " + typeof(T) + " component attached to " + parameter.gameObject + "!", parameter.gameObject);
					}
				}
			}
			else if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
			{
				if (parameter.variables != null)
				{
					file = parameter.variables.GetComponent <T>();
				}
			}
			else if (_constantID != 0)
			{
				T newField = Serializer.returnComponent <T> (_constantID);
				if (newField != null)
				{
					file = newField;
				}
			}

			return file;
		}


		/**
		 * <summary>Replaces a generic Behaviour based on a ConstantID, if appropriate.</summary>
		 * <param name = "_constantID">If !=0, The ConstantID number of the Behaviour to replace field with</param>
		 * <param name = "field">The Behaviour to replace</param>
		 * <returns>The replaced Behaviour, or field if no replacements were found</returns>
		 */
		public T AssignFile <T> (int _constantID, T field) where T : Behaviour
		{
			if (_constantID != 0)
			{
				T newField = Serializer.returnComponent <T> (_constantID);
				if (newField != null)
				{
					return newField;
				}
			}
			return field;
		}


		/**
		 * <summary>Replaces a GameObject based on a ConstantID, if appropriate.</summary>
		 * <param name = "_constantID">If !=0, The ConstantID number of the GameObject to replace field with</param>
		 * <param name = "field">The GameObject to replace</param>
		 * <returns>The replaced GameObject, or field if no replacements were found</returns>
		 */
		protected GameObject AssignFile (int _constantID, GameObject field)
		{
			if (_constantID != 0)
			{
				ConstantID newField = Serializer.returnConstantID (_constantID);
				if (newField != null)
				{
					return newField.gameObject;
				}
			}
			return field;
		}


		/**
		 * <summary>Replaces a Transform based on a ConstantID, if appropriate.</summary>
		 * <param name = "_constantID">If !=0, The ConstantID number of the Transform to replace field with</param>
		 * <param name = "field">The Transform to replace</param>
		 * <returns>The replaced Transform, or field if no replacements were found</returns>
		 */
		public Transform AssignFile (int _constantID, Transform field)
		{
			if (_constantID != 0)
			{
				ConstantID newField = Serializer.returnConstantID (_constantID);
				if (newField != null)
				{
					return newField.transform;
				}
			}
			return field;
		}


		#if UNITY_EDITOR

		public virtual void FixLinkAfterDeleting (Action actionToDelete, Action targetAction, List<Action> actionList)
		{
			if ((endAction == ResultAction.Skip && skipActionActual == actionToDelete) || (endAction == ResultAction.Continue && actionList.IndexOf (actionToDelete) == (actionList.IndexOf (this) + 1)))
			{
				if (targetAction == null)
				{
					endAction = ResultAction.Stop;
				}
				else
				{
					endAction = ResultAction.Skip;
					skipActionActual = targetAction;
				}
			}
		}


		public virtual void ClearIDs ()
		{}


		public virtual void PrepareToCopy (int originalIndex, List<Action> actionList)
		{
			if (endAction == ResultAction.Continue)
			{
				if (originalIndex == actionList.Count - 1)
				{
					endAction = ResultAction.Stop;
				}
				else if (actionList [originalIndex + 1].isMarked)
				{
					endAction = ResultAction.Skip;
					skipActionActual = actionList [originalIndex + 1];
				}
				else
				{
					endAction = ResultAction.Stop;
				}
			}
			if (endAction == ResultAction.Skip)
			{
				if (skipActionActual.isMarked)
				{
					int place = 0;
					foreach (Action _action in actionList)
					{
						if (_action.isMarked)
						{
							if (_action == skipActionActual)
							{
								skipActionActual = null;
								skipAction = place;
								break;
							}
							place ++;
						}
					}
				}
				else
				{
					endAction = ResultAction.Stop;
				}
			}
		}


		public virtual void PrepareToPaste (int offset)
		{
			skipAction += offset;
		}


		public void BreakPoint (int i, ActionList list)
		{
			if (isBreakPoint)
			{
				ACDebug.Log ("Break-point with (" + i.ToString () + ") '" + category.ToString () + ": " + title + "' in " + list.gameObject.name, list.gameObject);
				EditorApplication.isPaused = true;
			}
		}

		#endif


		protected ActionEnd GenerateActionEnd (ResultAction _resultAction, ActionListAsset _linkedAsset, Cutscene _linkedCutscene, int _skipAction, Action _skipActionActual, List<Action> _actions)
		{
			ActionEnd actionEnd = new ActionEnd ();

			actionEnd.resultAction = _resultAction;
			actionEnd.linkedAsset = _linkedAsset;
			actionEnd.linkedCutscene = _linkedCutscene;
			
			if (_resultAction == ResultAction.RunCutscene)
			{
				if (isAssetFile && _linkedAsset != null)
				{
					actionEnd.linkedAsset = _linkedAsset;
				}
				else if (!isAssetFile && _linkedCutscene != null)
				{
					actionEnd.linkedCutscene = _linkedCutscene;
				}
			}
			else if (_resultAction == ResultAction.Skip)
			{
				int skip = _skipAction;
				if (_skipActionActual && _actions.Contains (_skipActionActual))
				{
					skip = _actions.IndexOf (_skipActionActual);
				}
				else if (skip == -1)
				{
					skip = 0;
				}
				actionEnd.skipAction = skip;
			}
			
			return actionEnd;
		}


		protected ActionEnd GenerateStopActionEnd ()
		{
			ActionEnd actionEnd = new ActionEnd ();
			actionEnd.resultAction = ResultAction.Stop;
			return actionEnd;
		}


		/**
		 * <summary>Sets the value of the lastResult ActionEnd.</summary>
		 * <param name = "_actionEnd">The ActionEnd to copy onto lastResult</param>
		 */
		public virtual void SetLastResult (ActionEnd _actionEnd)
		{
			lastResult = _actionEnd;
		}


		/**
		 * <summary>Update the Action's output socket</summary>
		 * <param name = "actionEnd">A data container for the output socket</param>
		 */
		public void SetOutput (ActionEnd actionEnd)
		{
			endAction = actionEnd.resultAction;
			skipAction = actionEnd.skipAction;
			skipActionActual = actionEnd.skipActionActual;
			linkedCutscene = actionEnd.linkedCutscene;
			linkedAsset = actionEnd.linkedAsset;
		}

	}
	
}