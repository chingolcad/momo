/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionListManager.cs"
 * 
 *	This script keeps track of which ActionLists are running in a scene.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component keeps track of which ActionLists are running.
	 * When an ActionList runs or ends, it is passed to this script, which sets up the correct GameState in StateHandler.
	 * It should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_action_list_manager.html")]
	public class ActionListManager : MonoBehaviour
	{

		#region Variables

		/** If True, then the next time ActionConversation's Skip() function is called, it will be ignored */
		[HideInInspector] public bool ignoreNextConversationSkip = false;

		protected bool playCutsceneOnVarChange = false;
		protected bool saveAfterCutscene = false;

		protected int playerIDOnStartQueue;
		protected bool noPlayerOnStartQueue;

		/** Data about any ActionList that has been run and we need to store information about */
		[HideInInspector] public List<ActiveList> activeLists = new List<ActiveList>();

		#endregion


		#region UnityStandards

		public void OnAwake ()
		{
			activeLists.Clear ();
		}


		protected void OnDestroy ()
		{
			activeLists.Clear ();
		}

		#endregion


		#region PublicFunctions

		/**
		 * Checks for autosaving and changed variables.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateActionListManager ()
		{
			if (saveAfterCutscene && !IsGameplayBlocked ())
			{
				saveAfterCutscene = false;
				SaveSystem.SaveAutoSave ();
			}
			
			if (playCutsceneOnVarChange && KickStarter.stateHandler && (KickStarter.stateHandler.gameState == GameState.Normal || KickStarter.stateHandler.gameState == GameState.DialogOptions))
			{
				playCutsceneOnVarChange = false;
				
				if (KickStarter.sceneSettings.actionListSource == ActionListSource.InScene && KickStarter.sceneSettings.cutsceneOnVarChange != null)
				{
					KickStarter.sceneSettings.cutsceneOnVarChange.Interact ();
				}
				else if (KickStarter.sceneSettings.actionListSource == ActionListSource.AssetFile && KickStarter.sceneSettings.actionListAssetOnVarChange != null)
				{
					KickStarter.sceneSettings.actionListAssetOnVarChange.Interact ();
				}
			}
		}
		

		/**
		 * Ends all skippable ActionLists.
		 * This is triggered when the user presses the "EndCutscene" Input button.
		 */
		public void EndCutscene ()
		{
			if (!IsInSkippableCutscene ())
			{
				return;
			}

			if (AdvGame.GetReferences ().settingsManager.blackOutWhenSkipping)
			{
				KickStarter.mainCamera.HideScene ();
			}
			
			// Stop all non-looping sound
			Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
			foreach (Sound sound in sounds)
			{
				if (sound.GetComponent <AudioSource>())
				{
					if (sound.soundType != SoundType.Music && !sound.GetComponent <AudioSource>().loop)
					{
						sound.Stop ();
					}
				}
			}

			// Set correct Player prefab before skipping
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				if (KickStarter.player != null && !noPlayerOnStartQueue && playerIDOnStartQueue != KickStarter.player.ID && playerIDOnStartQueue >= 0)
				{
					Player playerToRevertTo = KickStarter.settingsManager.GetPlayer (playerIDOnStartQueue);
					KickStarter.ResetPlayer (playerToRevertTo, playerIDOnStartQueue, true, Quaternion.identity, false, true);
				}
				else if (KickStarter.player != null && noPlayerOnStartQueue)
				{
					KickStarter.ResetPlayer (null, KickStarter.settingsManager.GetEmptyPlayerID (), true, Quaternion.identity, false, true);
				}
				else if (KickStarter.player == null && !noPlayerOnStartQueue && playerIDOnStartQueue >= 0)
				{
					Player playerToRevertTo = KickStarter.settingsManager.GetPlayer (playerIDOnStartQueue);
					KickStarter.ResetPlayer (playerToRevertTo, playerIDOnStartQueue, true, Quaternion.identity, false, true);
				}
			}

			List<ActiveList> listsToSkip = new List<ActiveList>();
			List<ActiveList> listsToReset = new List<ActiveList>();

			foreach (ActiveList activeList in activeLists)
			{
				if (!activeList.inSkipQueue && activeList.actionList.IsSkippable ())
				{
					listsToReset.Add (activeList);
				}
				else
				{
					listsToSkip.Add (activeList);
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (!activeList.inSkipQueue && activeList.actionList.IsSkippable ())
				{
					listsToReset.Add (activeList);
				}
				else
				{
					listsToSkip.Add (activeList);
				}
			}

			foreach (ActiveList listToReset in listsToReset)
			{
				// Kill, but do isolated, to bypass setting GameState etc
				listToReset.Reset (true);
			}

			foreach (ActiveList listToSkip in listsToSkip)
			{
				listToSkip.Skip ();
			}
		}


		/**
		 * <summary>Checks if a particular ActionList is running.</summary>
		 * <param name = "actionList">The ActionList to search for</param>
		 * <returns>True if the ActionList is currently running</returns>
		 */
		public bool IsListRunning (ActionList actionList)
		{
			if (actionList == null) return false;

			RuntimeActionList runtimeActionList = actionList as RuntimeActionList;
			if (runtimeActionList != null)
			{
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
				{
					if (activeList.IsFor (runtimeActionList))
					{
						if (activeList.IsRunning ())
						{
							return true;
						}
					}
				}
				return false;
			}

			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionList))
				{
					if (activeList.IsRunning ())
					{
						return true;
					}
				}
			}
			
			return false;
		}


		public bool IsListRegistered (ActionList actionList)
		{
			if (actionList == null) return false;

			RuntimeActionList runtimeActionList = actionList as RuntimeActionList;
			if (runtimeActionList != null)
			{
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
				{
					if (activeList.IsFor (runtimeActionList))
					{
						return true;
					}
				}
				return false;
			}

			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionList))
				{
					return true;
				}
			}
			
			return false;
		}


		public bool CanResetSkipVars (ActionList actionList)
		{
			RuntimeActionList runtimeActionList = actionList as RuntimeActionList;
			if (runtimeActionList != null)
			{
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
				{
					if (activeList.IsFor (runtimeActionList))
					{
						return activeList.CanResetSkipVars ();
					}
					if (activeList.IsFor (runtimeActionList.assetSource))
					{
						return activeList.CanResetSkipVars ();
					}
				}
				return true;
			}

			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionList))
				{
					return activeList.CanResetSkipVars ();
				}
			}
			
			return true;
		}


		/**
		 * <summary>Checks if any currently-running ActionLists pause gameplay.</summary>
		 * <param name = "_actionToIgnore">Any ActionList that contains this Action will be excluded from the check</param>
		 * <param name = "showSaveDebug">If True, and an ActionList is pausing gameplay, a Console warning will be given to explain why saving is not currently possible</param>
		 * <returns>True if any currently-running ActionLists pause gameplay</returns>
		 */
		public bool IsGameplayBlocked (Action _actionToIgnore = null, bool showSaveDebug = false)
		{
			if (KickStarter.stateHandler.IsInScriptedCutscene ())
			{
				if (showSaveDebug)
				{
					ACDebug.LogWarning ("Cannot save at this time - currently in a scripted cutscene.");
				}
				return true;
			}
			
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.actionList.actionListType == ActionListType.PauseGameplay && activeList.IsRunning ())
				{
					if (_actionToIgnore != null)
					{
						if (activeList.actionList.actions.Contains (_actionToIgnore))
						{
							continue;
						}
					}
					
					if (showSaveDebug)
					{
						ACDebug.LogWarning ("Cannot save at this time - the ActionList '" + activeList.actionList.name + "' is blocking gameplay.", activeList.actionList);
					}
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.actionList != null && activeList.actionList.actionListType == ActionListType.PauseGameplay && activeList.IsRunning ())
				{
					if (_actionToIgnore != null)
					{
						if (activeList.actionList.actions.Contains (_actionToIgnore))
						{
							continue;
						}
					}

					if (showSaveDebug)
					{
						ACDebug.LogWarning ("Cannot save at this time - the ActionListAsset '" + activeList.actionList.name + "' is blocking gameplay.", activeList.actionList);
					}
					return true;
				}
			}

			return false;
		}


		public bool IsOverrideConversationRunning ()
		{
			if (KickStarter.playerInput.activeConversation != null)
			{
				foreach (ActiveList activeList in activeLists)
				{
					if (activeList.IsConversationOverride ())
					{
						return true;
					}
				}

				foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
				{
					if (activeList.IsConversationOverride ())
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Checks if any currently-running ActionListAssets pause gameplay and unfreeze 'Pause' Menus.</summary>
		 * <returns>True if any currently-running ActionListAssets pause gameplay and unfreeze 'Pause' Menus.</returns>
		 */
		public bool IsGameplayBlockedAndUnfrozen ()
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.CanUnfreezePauseMenus () && activeList.IsRunning ())
				{
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.CanUnfreezePauseMenus () && activeList.IsRunning ())
				{
					return true;
				}
			}
			return false;
		}
		
		
		/**
		 * <summary>Checks if any skippable ActionLists are currently running.</summary>
		 * <returns>True if any skippable ActionLists are currently running.</returns>
		 */
		public bool IsInSkippableCutscene ()
		{
			if (!IsGameplayBlocked ())
			{
				return false;
			}

			if (HasSkipQueue ())
			{
				return true;
			}

			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsRunning () && activeList.actionList.IsSkippable ())
				{
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.IsRunning () && activeList.actionListAsset != null && activeList.actionListAsset.IsSkippable ())
				{
					return true;
				}
			}
			
			return false;
		}


		/**
		 * <summary>Adds a new ActionList, assumed to already be running, to the internal record of currently-running ActionLists, and sets the correct GameState in StateHandler.</summary>
		 * <param name = "actionList">The ActionList to run</param>
		 * <param name = "addToSkipQueue">If True, then the ActionList will be added to the list of ActionLists to skip</param>
		 * <param name = "_startIndex">The index number of the Action to start skipping from, if addToSkipQueue = True</param>
		 * <param name = "actionListAsset">The ActionListAsset that is the ActionList's source, if it has one.</param>
		 */
		public void AddToList (ActionList actionList, bool addToSkipQueue, int _startIndex)
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionList))
				{
					activeLists.RemoveAt (i);
				}
			}
			addToSkipQueue = CanAddToSkipQueue (actionList, addToSkipQueue);
			activeLists.Add (new ActiveList (actionList, addToSkipQueue, _startIndex));

			if (KickStarter.playerMenus.ArePauseMenusOn ())
			{
				if (actionList.actionListType == ActionListType.RunInBackground)
				{
					// Don't change gamestate if running in background
					return;
				}
				if (actionList is RuntimeActionList && actionList.actionListType == ActionListType.PauseGameplay && !actionList.unfreezePauseMenus)
				{
					// Don't affect the gamestate if we want to remain frozen
					return;
				}
			}

			SetCorrectGameState ();
		}
		

		/**
		 * <summary>Resets and removes a ActionList from the internal record of currently-running ActionLists, and sets the correct GameState in StateHandler.</summary>
		 * <param name = "actionList">The ActionList to end</param>
		 */
		public void EndList (ActionList actionList)
		{
			if (actionList == null)
			{
				return;
			}
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionList))
				{
					EndList (activeLists[i]);
					return;
				}
			}
		}


		/**
		 * <summary>Ends the ActionList or ActionListAsset associated with a given ActiveList data container</summary>
		 * <param name = "activeList">The ActiveList associated with the ActionList or ActionListAsset to end.</param>
		 */
		public void EndList (ActiveList activeList)
		{
			activeList.Reset (false);
			if (activeList.GetConversationOnEnd ())
			{
				ResetSkipVars ();
				activeList.RunConversation ();
			}
			else
			{
				if (activeList.actionListAsset != null && activeList.actionList.actionListType == ActionListType.PauseGameplay && !activeList.actionList.unfreezePauseMenus && KickStarter.playerMenus.ArePauseMenusOn (null))
				{
					// Don't affect the gamestate if we want to remain frozen
					if (KickStarter.stateHandler.gameState != GameState.Cutscene)
					{
						ResetSkipVars ();
					}
					PurgeLists ();
				}
				else
				{
					SetCorrectGameStateEnd ();
				}
			}

			/*if (activeList != null && activeList.actionList != null && activeList.actionList.autosaveAfter)
			{
				if (!IsGameplayBlocked ())
				{
					SaveSystem.SaveAutoSave ();
				}
				else
				{
					saveAfterCutscene = true;
				}
			}*/
		}


		/**
		 * Inform ActionListManager that a Variable's value has changed.
		 */
		public void VariableChanged ()
		{
			playCutsceneOnVarChange = true;
		}


		/**
		 * Ends all currently-running ActionLists and ActionListAssets.
		 */
		public void KillAllLists ()
		{
			foreach (ActiveList activeList in activeLists)
			{
				activeList.Reset (true);
			}
			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				activeList.Reset (true);
			}
		}
		

		/**
		 * <summary>Ends all currently-running ActionLists present within a given scene.</summary>
		 * <param name = "sceneInfo">A data container for information about the scene in question</param>
		 */
		public void KillAllFromScene (SceneInfo sceneInfo)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.actionList != null && sceneInfo.Matches (UnityVersionHandler.GetSceneInfoFromGameObject (activeList.actionList.gameObject)) && activeList.actionListAsset == null)
				{
					activeList.Reset (true);
				}
			}
		} 


		/**
		 * <summary>Clears all data about the current state of "skippable" Cutscenes, allowing you to prevent previously-run Cutscenes in the same block of gameplay-blocking ActionLists. Use with caution!</summary>
		 */
		public void ResetSkippableData ()
		{
			ResetSkipVars (true);
		}



		/**
		 * Sets the StateHandler's gameState variable to the correct value, based on what ActionLists are currently running.
		 */
		public void SetCorrectGameState ()
		{
			if (KickStarter.stateHandler != null)
			{
				if (IsGameplayBlocked ())
				{
					if (KickStarter.stateHandler.gameState != GameState.Cutscene)
					{
						ResetSkipVars ();
					}
					KickStarter.stateHandler.gameState = GameState.Cutscene;

					if (IsGameplayBlockedAndUnfrozen ())
					{
						KickStarter.sceneSettings.UnpauseGame (KickStarter.playerInput.timeScale);
					}
				}
				else if (KickStarter.playerMenus.ArePauseMenusOn (null))
				{
					KickStarter.stateHandler.gameState = GameState.Paused;
					KickStarter.sceneSettings.PauseGame ();
				}
				else
				{
					if (KickStarter.playerInput.IsInConversation (true))
					{
						KickStarter.stateHandler.gameState = GameState.DialogOptions;
					}
					else
					{
						KickStarter.stateHandler.gameState = GameState.Normal;
					}
				}
			}
			else
			{
				ACDebug.LogWarning ("Could not set correct GameState!");
			}
		}


		/**
		 * <summary>Sets the point to continue from, when a Conversation's options are overridden by an ActionConversation.</summary>
		 * <param name = "actionConversation">The "Dialogue: Start conversation" Action that is overriding the Conversation's options</param>
		 */
		public void SetConversationPoint (ActionConversation actionConversation)
		{
			foreach (ActiveList activeList in activeLists)
			{
				activeList.SetConversationOverride (actionConversation);
			}
			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				activeList.SetConversationOverride (actionConversation);
			}
		}


		/**
		 * <summary>Attempts to override a Conversation object's default options by resuming an ActionList from the last ActionConversation.</summary>
		 * <param name = "optionIndex">The index number of the chosen dialogue option.</param>
		 * <returns>True if the override was succesful.</returns>
		 */
		public bool OverrideConversation (int optionIndex)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.ResumeConversationOverride ())
				{
					return true;
				}
			}
			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.ResumeConversationOverride ())
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * Called when manually ending a Conversation by invoking the 'EndConversation' input
		 */
		public void OnEndConversation ()
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsConversationOverride ())
				{
					activeLists[i].Reset (true);
					activeLists.RemoveAt (i);
					i--;
				}
			}

			for (int i=0; i<KickStarter.actionListAssetManager.activeLists.Count; i++)
			{
				if (KickStarter.actionListAssetManager.activeLists[i].IsConversationOverride ())
				{
					KickStarter.actionListAssetManager.activeLists[i].Reset (true);
					KickStarter.actionListAssetManager.activeLists.RemoveAt (i);
					i--;
				}
			}
		}


		/**
		 * <summary>Checks if a given ActionList should be skipped when the 'EndCutscene' input is triggered.</summary>
		 * <param name = "actionList">The ActionList to check</param>
		 * <param name = "originalValue">If True, the user would like it to be skippable.</param>
		 * <returns>True if the ActionList can be skipped.</returns>
		 */
		public bool CanAddToSkipQueue (ActionList actionList, bool originalValue)
		{
			if (!actionList.IsSkippable ())
			{
				return false;
			}
			else if (!KickStarter.actionListManager.HasSkipQueue ()) // was InSkippableCutscene
			{
				if (KickStarter.player)
				{
					playerIDOnStartQueue = KickStarter.player.ID;
					noPlayerOnStartQueue = false;
				}
				else
				{
					//playerIDOnStartQueue = -1;
					noPlayerOnStartQueue = true;
				}
				return true;
			}
			return originalValue;
		}


		/**
		 * <summary>Records the Action indices that the associated ActionList was running before being paused. This data is sent to the ActionList's associated ActiveList</summary>
		 * <param name = "actionList">The ActionList that is being paused</param>
		 * <param name = "resumeIndices">An array of Action indices to run when the ActionList is resumed</param>
		 */
		public void AssignResumeIndices (ActionList actionList, int[] resumeIndices)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionList))
				{
					activeList.SetResumeIndices (resumeIndices);
				}
			}
		}


		/**
		 * <summary>Resumes a previously-paused ActionList. If the ActionList is already running, nothing will happen.</summary>
		 * <param name = "actionList">The ActionList to pause</param>
		 * <param name = "rerunPausedActions">If True, then any Actions that were midway-through running when the ActionList was paused will be restarted. Otherwise, the Actions that follow them will be reun instead.</param>
		 */
		public void Resume (ActionList actionList, bool rerunPausedActions)
		{
			if (IsListRunning (actionList))
			{
				return;
			}

			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionList))
				{
					activeLists[i].Resume (null, rerunPausedActions);
					return;
				}
			}

			actionList.Interact ();
		}


		/**
		 * <summary>Generates a save-able string out of the ActionList resume data.<summary>
		 * <param name = "If set, only data for a given subscene will be saved. If null, only data for the active scene will be saved</param>
		 * <returns>A save-able string out of the ActionList resume data<returns>
		 */
		public string GetSaveData (SubScene subScene = null)
		{
			PurgeLists ();
			string localResumeData = "";
			for (int i=0; i<activeLists.Count; i++)
			{
				localResumeData += activeLists[i].GetSaveData (subScene);

				if (i < (activeLists.Count - 1))
				{
					localResumeData += SaveSystem.pipe;
				}
			}
			return localResumeData;
		}


		/**
		 * <summary>Recreates ActionList resume data from a saved data string.</summary>
		 * <param name = "If set, the data is for a subscene and so existing data will not be cleared.</param>
		 * <param name = "_localResumeData">The saved data string</param>
		 */
		public void LoadData (string _dataString, SubScene subScene = null)
		{
			if (subScene == null)
			{
				activeLists.Clear ();
			}

			if (!string.IsNullOrEmpty (_dataString))
			{
				string[] dataArray = _dataString.Split (SaveSystem.pipe[0]);
				foreach (string chunk in dataArray)
				{
					ActiveList activeList = new ActiveList ();
					if (activeList.LoadData (chunk, subScene))
					{
						activeLists.Add (activeList);
					}
				}
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void SetCorrectGameStateEnd ()
		{
			if (KickStarter.stateHandler != null)
			{
				if (KickStarter.playerMenus.ArePauseMenusOn (null) && !IsGameplayBlockedAndUnfrozen ())
				{
					// Only pause the game again if no unfreezing ActionLists are running
					KickStarter.mainCamera.PauseGame (true); // was false
				}
				else
				{
					KickStarter.stateHandler.RestoreLastNonPausedState ();
				}

				if (KickStarter.stateHandler.gameState != GameState.Cutscene)
				{
					ResetSkipVars ();
				}
			}
			else
			{
				ACDebug.LogWarning ("Could not set correct GameState!");
			}

			PurgeLists ();
		}


		protected void PurgeLists ()
		{
			bool checkAutoSave = false;

			for (int i=0; i<activeLists.Count; i++)
			{
				if (!activeLists[i].IsNecessary ())
				{
					if (!saveAfterCutscene && !checkAutoSave && activeLists[i].actionList != null && activeLists[i].actionList.autosaveAfter)
					{
						checkAutoSave = true;
					}

					activeLists.RemoveAt (i);
					i--;
				}
			}
			for (int i=0; i<KickStarter.actionListAssetManager.activeLists.Count; i++)
			{
				if (!KickStarter.actionListAssetManager.activeLists[i].IsNecessary ())
				{
					KickStarter.actionListAssetManager.activeLists.RemoveAt (i);
					i--;
				}
			}

			if (checkAutoSave)
			{
				if (!IsGameplayBlocked ())
				{
					SaveSystem.SaveAutoSave ();
				}
				else
				{
					saveAfterCutscene = true;
				}
			}
		}


		protected void ResetSkipVars (bool ignoreBlockCheck = false)
		{
			if (ignoreBlockCheck || !IsGameplayBlocked ())
			{
				ignoreNextConversationSkip = false;
				foreach (ActiveList activeList in activeLists)
				{
					activeList.inSkipQueue = false;
				}
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
				{
					activeList.inSkipQueue = false;
				}

				GlobalVariables.BackupAll ();
				KickStarter.localVariables.BackupAllValues ();
			}
		}


		protected bool HasSkipQueue ()
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsRunning () && activeList.inSkipQueue)
				{
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.IsRunning () && activeList.inSkipQueue)
				{
					return true;
				}
			}
			
			return false;
		}

		#endregion


		#region StaticFunctions

		/**
		 * Ends all currently-running ActionLists and ActionListAssets.
		 */
		public static void KillAll ()
		{
			KickStarter.actionListManager.KillAllLists ();
		}

		#endregion
		
	}
	
}