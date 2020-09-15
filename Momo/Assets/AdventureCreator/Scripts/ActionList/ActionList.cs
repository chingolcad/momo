/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionList.cs"
 * 
 *	This script stores, and handles the sequentual triggering of, actions.
 *	It is derived by Cutscene, Hotspot, Trigger, and DialogOption.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * An ActionList stores and handles the sequential triggering of Action objects.
	 * Strung together, Actions can be used to create cutscenes, effects and gameplay logic.
	 * This base class is never used itself - only subclasses are intended to be placed on GameObjects.
	 */
	[System.Serializable]
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_action_list.html")]
	public class ActionList : MonoBehaviour, iActionListAssetReferencer
	{

		/** The Actions */
		[HideInInspector] public List<AC.Action> actions = new List<AC.Action> ();
		/** If True, the Actions will be skipped when the user presses the 'EndCutscene' Input button */
		[HideInInspector] public bool isSkippable = true;
		/** The delay, in seconds, before the Actions are run when the ActionList is triggered */
		[HideInInspector] public float triggerTime = 0f;
		/** If True, the game will auto-save when the Actions have finished running */
		[HideInInspector] public bool autosaveAfter = false;
		/** The effect that running the Actions has on the rest of the game (PauseGameplay, RunInBackground) */
		[HideInInspector] public ActionListType actionListType = ActionListType.PauseGameplay;
		/** The Conversation to run when the Actions have finished running */
		[HideInInspector] public Conversation conversation = null;
		/** The ActionListAsset file that stores the Actions, if source = ActionListSource.AssetFile */
		[HideInInspector] public ActionListAsset assetFile;
		/** Where the Actions are stored when not being run (InScene, AssetFile) */
		[HideInInspector] public ActionListSource source;
		/** If True, the game will un-freeze itself while the Actions run if the game was previously paused due to an enabled Menu */
		[HideInInspector] public bool unfreezePauseMenus = true;
		/** If True, ActionParameters can be used to override values within the Action objects */
		[HideInInspector] public bool useParameters = false;
		/** A List of ActionParameter objects that can be used to override values within the Actions, if useParameters = True */
		[HideInInspector] public List<ActionParameter> parameters = new List<ActionParameter> ();
		/** The ID of the associated SpeechTag */
		[HideInInspector] public int tagID;
		/** If True, and source = ActionListSource.AssetFile, the asset file's parameter values will be shared amongst all linked ActionLists */
		[HideInInspector] public bool syncParamValues = true;

		protected bool isSkipping = false;
		protected LayerMask LayerHotspot;
		protected LayerMask LayerOff;

		protected List<int> resumeIndices = new List<int> ();
		private bool pauseWhenActionFinishes = false;
		private const string parameterSeparator = "{PARAM_SEP}";

		protected bool isChangingScene = false;
		private int skipIteractions = 0; // Used to combat StackOverflow exceptions


		private void Awake ()
		{
			if (KickStarter.settingsManager != null)
			{
				LayerHotspot = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
				LayerOff = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);
			}

			DownloadParameters ();

			/*if (useParameters)
			{
				// Reset all parameters
				foreach (ActionParameter _parameter in parameters)
				{
					_parameter.Reset ();
				}
			}*/
		}


		private void DownloadParameters ()
		{
			// If asset-based, download actions
			if (source == ActionListSource.AssetFile)
			{
				actions.Clear ();
				if (assetFile != null && assetFile.actions.Count > 0)
				{
					foreach (AC.Action action in assetFile.actions)
					{
						actions.Add (action);
						actions[actions.Count - 1].isAssetFile = false;
					}

					if (!syncParamValues && useParameters && assetFile.useParameters && parameters.Count == assetFile.DefaultParameters.Count)
					{
						// Using local parameters
						return;
					}

					if (!assetFile.useParameters)
					{
						useParameters = false;
					}
					else
					{
						if (syncParamValues)
						{
							parameters = assetFile.GetParameters ();
							useParameters = true;
						}
						else
						{
							parameters.Clear ();
							foreach (ActionParameter parameter in assetFile.DefaultParameters)
							{
								if (parameter != null)
								{
									ActionParameter newParameter = new ActionParameter (parameter, !useParameters);
									parameters.Add (newParameter);
								}
							}
							useParameters = true;
						}
					}
				}
			}
		}


		/**
		 * Clears the List of Actions and creates one instance of the default, as set within ActionsManager.
		 */
		public void Initialise ()
		{
			actions.Clear ();
			if (actions == null || actions.Count < 1)
			{
				actions.Add (GetDefaultAction ());
			}
		}


		/**
		 * Runs the Actions normally, from the beginning.
		 */
		public virtual void Interact ()
		{
			Interact (0, true);
		}


		/**
		 * <summary>Runs the Actions from a set point.</summary>
		 * <param name = "index">The index number of actions to start from</param>
		 */
		public void RunFromIndex (int index)
		{
			Interact (index, true);
		}


		/**
		 * <summary>Runs the Actions from a set point.</summary>
		 * <param name = "i">The index number of actions to start from</param>
		 * <param name = "addToSkipQueue">If True, then the ActionList will be skipped when the user presses the 'EndCutscene' Input button</param>
		 */
		public void Interact (int i, bool addToSkipQueue)
		{
			if (!gameObject.activeSelf)
			{
				ACDebug.LogWarning ("Cannot run ActionList '" + name + "' because its GameObject is disabled!", this);
				return;
			}

			if (actions.Count > 0 && actions.Count > i)
			{
				if (triggerTime > 0f && i == 0)
				{
					StartCoroutine ("PauseUntilStart", addToSkipQueue);
				}
				else
				{
					ResetList ();
					ResetSkips ();
					BeginActionList (i, addToSkipQueue);
				}
			}
			else
			{
				Kill ();
			}
		}


		/**
		 * Runs the Actions instantly, from the beginning.
		 */
		public void Skip ()
		{
			Skip (0);
		}


		/**
		 * <summary>Runs the Actions instantly, from a set point.</summary>
		 * <param name = "i">The index number of actions to start from</param>
		 */
		public void Skip (int i)
		{
			skipIteractions = 0;

			if (actionListType == ActionListType.RunInBackground)
			{
				Interact (i, false);
				return;
			}

			if (i < 0 || actions.Count <= i)
			{
				return;
			}

			if (actionListType == ActionListType.RunInBackground || !isSkippable)
			{
				// Can't skip, so just run normally
				Interact ();
				return;
			}

			// Already running
			if (!isSkipping)
			{
				ResetList ();
				if (KickStarter.actionListManager.CanResetSkipVars (this))
				{
					// We need to reset skip vars if the ActionList is not currently in the skip queue
					ResetSkips ();
				}

				isSkipping = true;

				BeginActionList (i, false);
			}
		}


		private IEnumerator PauseUntilStart (bool addToSkipQueue)
		{
			if (triggerTime > 0f)
			{
				yield return new WaitForSeconds (triggerTime);
			}

			ResetList ();
			ResetSkips ();
			BeginActionList (0, addToSkipQueue);
		}


		private void ResetSkips ()
		{
			// "lastResult" is used to backup Check results when skipping
			foreach (Action action in actions)
			{
				if (action != null)
				{
					action.lastResult.skipAction = -10;
				}
			}
		}


		protected virtual void BeginActionList (int i, bool addToSkipQueue)
		{
			pauseWhenActionFinishes = false;

			if (KickStarter.actionListManager)
			{
				KickStarter.actionListManager.AddToList (this, addToSkipQueue, i);
				KickStarter.eventManager.Call_OnBeginActionList (this, null, i, isSkipping);

				if (KickStarter.actionListManager.IsListRegistered (this))
				{
					ProcessAction (i);
				}
			}
			else
			{
				ACDebug.LogWarning ("Cannot run " + this.name + " because no ActionListManager was found.", gameObject);
			}
		}


		private IEnumerator DelayProcessAction (int i)
		{
			yield return new WaitForSeconds (0.05f);
			ProcessAction (i);
		}


		protected void ProcessAction (int i)
		{
			if (i >= 0 && i < actions.Count && actions[i] != null && actions[i] is Action)
			{
				// Action exists
				if (!actions[i].isEnabled)
				{
					// Disabled, try next
					ProcessAction (i + 1);
				}
				else
				{
					// Run it
#if UNITY_EDITOR
					actions[i].BreakPoint (i, this);
#endif
					StartCoroutine ("RunAction", actions[i]);
				}
			}
			else
			{
				CheckEndCutscene ();
			}
		}


		private IEnumerator RunAction (Action action)
		{
			action.AssignParentList (this);
			if (useParameters)
			{
				action.AssignValues (parameters);
			}
			else
			{
				action.AssignValues (null);
			}

			if (isSkipping)
			{
				skipIteractions++;
				action.Skip ();

				if (KickStarter.settingsManager.printActionCommentsInConsole)
				{
					action.PrintComment (this);
				}
			}
			else
			{
				if (action is ActionRunActionList)
				{
					ActionRunActionList actionRunActionList = (ActionRunActionList)action;
					actionRunActionList.isSkippable = IsSkippable ();
				}

				if (isChangingScene)
				{
					ACDebug.Log ("Cannot run Action while changing scene, will resume once loading is complete.", this, action);
					while (isChangingScene)
					{
						yield return null;
					}
				}

				action.isRunning = false;
				float waitTime = action.Run ();

				if (KickStarter.settingsManager.printActionCommentsInConsole)
				{
					action.PrintComment (this);
				}

				if (action is ActionParallel)
				{ }
				else if (!Mathf.Approximately (waitTime, 0f))
				{
					while (action.isRunning)
					{
						bool runInRealtime = (this is RuntimeActionList && actionListType == ActionListType.PauseGameplay && !unfreezePauseMenus && KickStarter.playerMenus.ArePauseMenusOn (null));
						if (isChangingScene)
						{
							ACDebug.Log ("Cannot continue Action while changing scene, will resume once loading is complete.", this, action);
							while (isChangingScene)
							{
								yield return null;
							}
						}

						if (waitTime < 0)
						{
							if (!runInRealtime && Time.timeScale <= 0f)
							{
								while (Time.timeScale <= 0f)
								{
									yield return new WaitForEndOfFrame ();
								}
							}
							else
							{
								yield return new WaitForEndOfFrame ();
							}
						}
						else if (runInRealtime)
						{
							float endTime = Time.realtimeSinceStartup + waitTime;
							while (Time.realtimeSinceStartup < endTime)
							{
								yield return null;
							}
						}
						else
						{
							yield return new WaitForSeconds (waitTime);
						}

						if (!action.isRunning)
						{
							// In rare cases (once an actionlist is reset) isRunning may be false but this while loop will still run
							ResetList ();
							break;
						}

						waitTime = action.Run ();
					}
				}
			}

			if (action is ActionParallel)
			{
				EndActionParallel ((ActionParallel)action);
			}
			else
			{
				EndAction (action);
			}
		}


		private void EndAction (Action action)
		{
			action.isRunning = false;

			ActionEnd actionEnd = action.End (actions);
			if (isSkipping && action.lastResult.skipAction != -10 && (action is ActionCheck || action is ActionCheckMultiple))
			{
				// When skipping an ActionCheck that has already run, revert to previous result
				actionEnd = new ActionEnd (action.lastResult);
			}
			else
			{
				action.SetLastResult (new ActionEnd (actionEnd));
				ReturnLastResultToSource (actionEnd, actions.IndexOf (action));
			}

			if (action is ActionCheck || action is ActionCheckMultiple)
			{
				if (actionEnd.resultAction == ResultAction.Skip && actionEnd.skipAction == actions.IndexOf (action))
				{
					// Looping on itself will cause a StackOverflowException, so delay slightly
					ProcessActionEnd (actionEnd, actions.IndexOf (action), true);
					return;
				}
			}

			ProcessActionEnd (actionEnd, actions.IndexOf (action));
		}


		private void ProcessActionEnd (ActionEnd actionEnd, int i, bool doStackOverflowDelay = false)
		{
			if (isSkipping && skipIteractions > (actions.Count * 3))
			{
				// StackOverFlow
				ACDebug.LogWarning ("Looping ActionList '" + gameObject.name + "' detected while skipping - ending prematurely to avoid a StackOverflow exception.", gameObject);
				CheckEndCutscene ();
				return;
			}

			if (pauseWhenActionFinishes)
			{
				resumeIndices.Add (i);
				if (!AreActionsRunning ())
				{
					FinishPause ();
				}
				return;
			}

			if (actionEnd.resultAction == ResultAction.RunCutscene)
			{
				if (actionEnd.linkedAsset != null)
				{
					if (isSkipping)
					{
						AdvGame.SkipActionListAsset (actionEnd.linkedAsset);
					}
					else
					{
						AdvGame.RunActionListAsset (actionEnd.linkedAsset, 0, !IsSkippable ());
					}
					CheckEndCutscene ();
				}
				else if (actionEnd.linkedCutscene != null)
				{
					if (actionEnd.linkedCutscene != this)
					{
						if (isSkipping)
						{
							actionEnd.linkedCutscene.Skip ();
						}
						else
						{
							actionEnd.linkedCutscene.Interact (0, !IsSkippable ());
						}
						CheckEndCutscene ();
					}
					else
					{
						if (triggerTime > 0f)
						{
							Kill ();
							StartCoroutine ("PauseUntilStart", !IsSkippable ());
						}
						else
						{
							ProcessAction (0);
						}
					}
				}
				else
				{
					CheckEndCutscene ();
				}
			}
			else if (actionEnd.resultAction == ResultAction.Stop)
			{
				CheckEndCutscene ();
			}
			else if (actionEnd.resultAction == ResultAction.Skip)
			{
				if (doStackOverflowDelay)
				{
					StartCoroutine (DelayProcessAction (actionEnd.skipAction));
				}
				else
				{
					ProcessAction (actionEnd.skipAction);
				}
			}
			else if (actionEnd.resultAction == ResultAction.Continue)
			{
				ProcessAction (i + 1);
			}

			pauseWhenActionFinishes = false;
		}


		private void EndActionParallel (ActionParallel actionParallel)
		{
			actionParallel.isRunning = false;
			ActionEnd[] actionEnds = actionParallel.Ends (this.actions, isSkipping);

			foreach (ActionEnd actionEnd in actionEnds)
			{
				ProcessActionEnd (actionEnd, actions.IndexOf (actionParallel));
			}
		}


		private IEnumerator EndCutscene ()
		{
			yield return new WaitForEndOfFrame ();

			if (AreActionsRunning ())
			{
				yield break;
			}

			Kill ();
		}


		protected void CheckEndCutscene ()
		{
			if (!AreActionsRunning ())
			{
				StartCoroutine ("EndCutscene");
			}
		}


		/**
		 * <summary>Checks if any Actions are currently being run.</summary>
		 * <returns>True if any Actions are currently being run</returns>
		 */
		public bool AreActionsRunning ()
		{
			for (int i = 0; i < actions.Count; i++)
			{
				if (actions[i] != null && actions[i].isRunning)
				{
					return true;
				}
			}
			return false;
		}


		private void TurnOn ()
		{
			gameObject.layer = LayerHotspot;
		}


		private void TurnOff ()
		{
			gameObject.layer = LayerOff;
		}


		/**
		 * Stops the Actions from running.
		 */
		public void ResetList ()
		{
			isSkipping = false;
			StopAllCoroutines ();

			foreach (Action action in actions)
			{
				if (action != null)
				{
					action.Reset (this);
				}
			}
		}


		/**
		 * Stops the Actions from running and sets the gameState in StateHandler to the correct value.
		 */
		public virtual void Kill ()
		{
			StopAllCoroutines ();

			KickStarter.eventManager.Call_OnEndActionList (this, null, isSkipping);

			KickStarter.actionListManager.EndList (this);
		}


		/**
		 * <summary>Gets the default Action set within ActionsManager.</summary>
		 * <returns>The default Action set within ActionsManager</returns>
		 */
		public static AC.Action GetDefaultAction ()
		{
			if (AdvGame.GetReferences ().actionsManager)
			{
				string defaultAction = ActionsManager.GetDefaultAction ();
				AC.Action newAction = (AC.Action)ScriptableObject.CreateInstance (defaultAction);
				newAction.name = defaultAction;
				return newAction;
			}
			else
			{
				ACDebug.LogError ("Cannot create Action - no Actions Manager found.");
				return null;
			}
		}


		protected void ReturnLastResultToSource (ActionEnd _lastResult, int i)
		{ }


		/**
		 * <summary>Checks if the ActionListAsset is skippable. This is safer than just reading 'isSkippable', because it also accounts for actionListType - since ActionLists that run in the background cannot be skipped</summary>
		 * <returns>True if the ActionListAsset is skippable</returns>
		 */
		public bool IsSkippable ()
		{
			if (isSkippable && actionListType == ActionListType.PauseGameplay)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets the List of Actions that this ActionList runs, regardless of source.</summary>
		 * <returns>The List of Actions that this ActionList runs, regardless of source.</returns>
		 */
		public List<Action> GetActions ()
		{
			if (source == ActionListSource.AssetFile)
			{
				if (assetFile)
				{
					return assetFile.actions;
				}
			}
			else
			{
				return actions;
			}
			return null;
		}


		/**
		 * <summary>Gets a parameter of a given name.</summary>
		 * <param name = "label">The name of the parameter to get</param>
		 * <returns>The parameter with the given name</returns>
		 */
		public ActionParameter GetParameter (string label)
		{
			if (useParameters)
			{
				if (source == ActionListSource.InScene)
				{
					return GetParameter (label, parameters);
				}
				else if (source == ActionListSource.AssetFile && assetFile != null && assetFile.useParameters)
				{
					if (syncParamValues)
					{
						return GetParameter (label, assetFile.GetParameters ());
					}
					else
					{
						return GetParameter (label, parameters);
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Gets a parameter of a given ID number.</summary>
		 * <param name = "_ID">The ID of the parameter to get</param>
		 * <returns>The parameter with the given ID number</returns>
		 */
		public ActionParameter GetParameter (int _ID)
		{
			if (useParameters)
			{
				if (source == ActionListSource.InScene)
				{
					return GetParameter (_ID, parameters);
				}
				else if (source == ActionListSource.AssetFile && assetFile != null && assetFile.useParameters)
				{
					if (syncParamValues)
					{
						return GetParameter (_ID, assetFile.GetParameters ());
					}
					else
					{
						return GetParameter (_ID, parameters);
					}
				}
			}
			return null;
		}


		private ActionParameter GetParameter (int _ID, List<ActionParameter> _parameters)
		{
			if (_parameters != null)
			{
				foreach (ActionParameter parameter in _parameters)
				{
					if (parameter.ID == _ID)
					{
						return parameter;
					}
				}
			}
			return null;
		}


		private ActionParameter GetParameter (string _label, List<ActionParameter> _parameters)
		{
			if (_parameters != null)
			{
				foreach (ActionParameter parameter in _parameters)
				{
					if (parameter.label == _label)
					{
						return parameter;
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Pauses the ActionList once it has finished running it's current Action.</summary>
		 */
		public void Pause ()
		{
			resumeIndices.Clear ();
			pauseWhenActionFinishes = true;

			KickStarter.eventManager.Call_OnPauseActionList (this);
		}


		protected virtual void FinishPause ()
		{
			KickStarter.actionListManager.AssignResumeIndices (this, resumeIndices.ToArray ());
			CheckEndCutscene ();
		}


		/**
		 * <summary>Resumes the ActionList.</summary>
		 * <param name = "_startIndex">The Action index that the ActionList was originally started from.</param>
		 * <param name = "_resumeIndices">An array of Action indices to resume from</param>
		 * <param name = "_parameterData">The ActionParameter values when paused, as a serializable string</param>
		 * <param name = "rerunPreviousAction">If True, then any Actions that were running when the ActionList was paused will be re-run</param>
		 */
		public void Resume (int _startIndex, int[] _resumeIndices, string _parameterData, bool rerunPreviousAction = false)
		{
			resumeIndices.Clear ();
			foreach (int resumeIndex in _resumeIndices)
			{
				resumeIndices.Add (resumeIndex);
			}

			if (resumeIndices.Count > 0)
			{
				ResetList ();
				ResetSkips ();

				SetParameterData (_parameterData);

				pauseWhenActionFinishes = false;

				if (KickStarter.actionListManager == null)
				{
					ACDebug.LogWarning ("Cannot run " + this.name + " because no ActionListManager was found.", gameObject);
					return;
				}

				AddResumeToManager (_startIndex);

				KickStarter.eventManager.Call_OnResumeActionList (this);

				foreach (int resumeIndex in resumeIndices)
				{
					if (resumeIndex >= 0 && resumeIndex < actions.Count)
					{
						if (rerunPreviousAction)
						{
							ProcessAction (resumeIndex);
						}
						else
						{
							Action action = actions[resumeIndex];

							if (useParameters)
							{
								action.AssignValues (parameters);
							}
							else
							{
								action.AssignValues (null);
							}

							if (action is ActionParallel)
							{
								EndActionParallel ((ActionParallel)action);
							}
							else
							{
								EndAction (action);
							}
						}
					}
				}
			}
			else
			{
				Kill ();
				Interact ();
			}
		}


		protected virtual void AddResumeToManager (int startIndex)
		{
			KickStarter.actionListManager.AddToList (this, true, startIndex);
		}


		/**
		 * <summary>Gets the current ActionParameter values as a serializable string.</summary>
		 * <returns>The current ActionParameter values as a serializable string</returns>
		 */
		public string GetParameterData ()
		{
			if (useParameters)
			{
				string dataString = string.Empty;
				for (int i = 0; i < parameters.Count; i++)
				{
					dataString += parameters[i].GetSaveData ();

					if (i < (parameters.Count - 1))
					{
						dataString += parameterSeparator;
					}
				}
				return dataString;
			}
			return "";
		}


		/**
		 * <summary>Assigns parameter values based on a string generated by the GetParameterData function</summary>
		 * <param name = "dataString">The data string to load parameter data from</param>
		 */
		public void SetParameterData (string dataString)
		{
			if (useParameters && !string.IsNullOrEmpty (dataString))
			{
				string[] stringSeparators = new string[] { parameterSeparator };
				string[] dataArray = dataString.Split (stringSeparators, System.StringSplitOptions.None);

				for (int i = 0; i < parameters.Count; i++)
				{
					if (i < dataArray.Length)
					{
						parameters[i].LoadData (dataArray[i]);
					}
				}
			}
		}


		/** The number of parameters associated with the ActionList */
		public int NumParameters
		{
			get
			{
				if (useParameters && parameters != null) return parameters.Count;
				return 0;
			}
		}


#if UNITY_EDITOR

		private void OnValidate ()
		{
			CopyScriptable ();
		}


		public int GetInventoryReferences (InvItem item, string sceneFile)
		{
			int totalNumReferences = 0;

			if ((source == ActionListSource.InScene && NumParameters > 0) ||
				(source == ActionListSource.AssetFile && assetFile != null && assetFile.NumParameters > 0 && !syncParamValues && useParameters))
			{
				int thisNumReferences = GetParameterReferences (parameters, item.id, ParameterType.InventoryItem);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
					ACDebug.Log ("Found " + thisNumReferences + " references to inventory item '" + item.label + "' in parameter values of ActionList '" + name + "' in scene '" + sceneFile + "'", this);
				}
			}

			foreach (Action action in actions)
			{
				int thisNumReferences = action.GetInventoryReferences (parameters, item.id);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
					ACDebug.Log ("Found " + thisNumReferences + " references to inventory item '" + item.label + "' in Action #" + actions.IndexOf (action) + " of ActionList '" + name + "' in scene '" + sceneFile + "'", this);
				}
			}

			return totalNumReferences;
		}


		public int GetVariableReferences (VariableLocation _location, GVar _variable, Variables _variables = null, string sceneFile = "")
		{
			int totalNumReferences = 0;

			if (_variable == null)
			{
				if ((source == ActionListSource.InScene && NumParameters > 0) ||
					(source == ActionListSource.AssetFile && assetFile != null && assetFile.NumParameters > 0 && !syncParamValues && useParameters))
				{
					ParameterType parameterType = (_location == VariableLocation.Global) ? ParameterType.GlobalVariable : ParameterType.LocalVariable;
					int thisNumReferences = GetParameterReferences (parameters, _variable.id, parameterType);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						if (string.IsNullOrEmpty (sceneFile))
						{
							ACDebug.Log ("Found " + thisNumReferences + " references to variable '" + _variable.label + "' in parameter values of ActionList '" + name + "'", this);
						}
						else
						{
							ACDebug.Log ("Found " + thisNumReferences + " references to variable '" + _variable.label + "' in parameter values of ActionList '" + name + "' in scene '" + sceneFile + "'", this);
						}
					}
				}
			}

			foreach (Action action in actions)
			{
				int thisNumReferences = action.GetVariableReferences (parameters, _location, _variable.id, _variables);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
					if (string.IsNullOrEmpty (sceneFile))
					{
						ACDebug.Log ("Found " + thisNumReferences + " references to " + _location + " variable '" + _variable.label + "' in Action #" + actions.IndexOf (action) + " of ActionList '" + name + "'", this);
					}
					else
					{
						ACDebug.Log ("Found " + thisNumReferences + " references to " + _location + " variable '" + _variable.label + "' in Action #" + actions.IndexOf (action) + " of ActionList '" + name + "' in scene '" + sceneFile + "'", this);
					}
				}
			}

			return totalNumReferences;
		}


		public int GetDocumentReferences (Document document, string sceneFile)
		{
			int totalNumReferences = 0;

			if ((source == ActionListSource.InScene && NumParameters > 0) ||
				(source == ActionListSource.AssetFile && assetFile != null && assetFile.NumParameters > 0 && !syncParamValues && useParameters))
			{
				int thisNumReferences = GetParameterReferences (parameters, document.ID, ParameterType.Document);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
					ACDebug.Log ("Found " + thisNumReferences + " references to Document '" + document.title + "' in parameter values of ActionList '" + name + "' in scene '" + sceneFile + "'", this);
				}
			}

			foreach (Action action in actions)
			{
				int thisNumReferences = action.GetDocumentReferences (parameters, document.ID);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
					ACDebug.Log ("Found " + thisNumReferences + " references to document '" + document.title + "' in Action #" + actions.IndexOf (action) + " of ActionList '" + name + "' in scene '" + sceneFile + "'", this);
				}
			}

			return totalNumReferences;
		}


		private int GetParameterReferences (List<ActionParameter> parameters, int _ID, ParameterType _paramType)
		{
			int thisCount = 0;

			foreach (ActionParameter parameter in parameters)
			{
				if (parameter != null && parameter.parameterType == _paramType && _ID == parameter.intValue)
				{
					thisCount++;
				}
			}

			return thisCount;
		}


		private void CopyScriptable ()
		{
			if (Application.isPlaying)
			{
				return;
			}

			if (actions == null || actions.Count == 0 || source == ActionListSource.AssetFile)
			{
				return;
			}

			bool modified = false;
			for (int i = 0; i < actions.Count; i++)
			{
				if (actions[i] != null && actions[i].parentActionListInEditor != this)
				{
					actions[i] = Instantiate (actions[i]) as Action;
					actions[i].name = actions[i].name.Replace ("(Clone)", string.Empty);
					actions[i].parentActionListInEditor = this;
					modified = true;
				}
			}
			if (modified) UnityVersionHandler.CustomSetDirty (this);
		}


		public bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (source == ActionListSource.AssetFile && assetFile == actionListAsset)
			{
				return true;
			}
			return false;
		}

		#endif

	}
	
}