/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"StateHandler.cs"
 * 
 *	This script stores the gameState variable, which is used by
 *	other scripts to determine if the game is running normal gameplay,
 *	in a cutscene, paused, or displaying conversation options.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script stores the all-important gameState variable, which determines if the game is running normal gameplay, is in a cutscene, or is paused.
	 * It also runs the various "Update", "LateUpdate", "FixedUpdate" and "OnGUI" functions that are within Adventure Creator's main scripts - by running them all from here, performance is drastically improved.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_state_handler.html")]
	public class StateHandler : MonoBehaviour
	{

		#region Variables

		protected GameState _gameState = GameState.Normal;

		protected Music music;
		protected Ambience ambience;
		protected bool inScriptedCutscene;
		protected GameState previousUpdateState = GameState.Normal;
		protected GameState lastNonPausedState = GameState.Normal;
		protected bool isACDisabled = false;

		protected bool cursorIsOff = false;
		protected bool inputIsOff = false;
		protected bool interactionIsOff = false;
		protected bool draggablesIsOff = false;
		protected bool menuIsOff = false;
		protected bool movementIsOff = false;
		protected bool cameraIsOff = false;
		protected bool triggerIsOff = false;
		protected bool playerIsOff = false;

		protected bool runAtLeastOnce = false;
		protected bool hasGameEngine = false;

		protected List<ArrowPrompt> arrowPrompts = new List<ArrowPrompt>();
		protected List<DragBase> dragBases = new List<DragBase>();
		protected List<Parallax2D> parallax2Ds = new List<Parallax2D>();
		protected List<Hotspot> hotspots = new List<Hotspot>();
		protected List<Highlight> highlights = new List<Highlight>();
		protected List<AC_Trigger> triggers = new List<AC_Trigger>();
		protected List<_Camera> cameras = new List<_Camera>();
		protected List<Sound> sounds = new List<Sound>();
		protected List<LimitVisibility> limitVisibilitys = new List<LimitVisibility>();
		protected List<Char> characters = new List<Char>();
		protected List<FollowSortingMap> followSortingMaps = new List<FollowSortingMap>();
		protected List<NavMeshBase> navMeshBases = new List<NavMeshBase>();
		protected List<SortingMap> sortingMaps = new List<SortingMap>();
		protected List<BackgroundCamera> backgroundCameras = new List<BackgroundCamera>();
		protected List<BackgroundImage> backgroundImages = new List<BackgroundImage>();
		protected List<ConstantID> constantIDs = new List<ConstantID>();

		protected int _i = 0;

		#endregion


		#region UnityStandards

		public void OnAwake ()
		{
			Time.timeScale = 1f;
			DontDestroyOnLoad (this);
			inScriptedCutscene = false;

			InitPersistentEngine ();
		}


		protected void Start ()
		{
			if (KickStarter.settingsManager == null)
			{
				hasGameEngine = false;
			}
		}


		protected void Update ()
		{
			#if UNITY_EDITOR
			ACScreen.UpdateCache ();
			#endif

			if (isACDisabled || !hasGameEngine)
			{
				return;
			}

			if (KickStarter.settingsManager.IsInLoadingScene () || KickStarter.sceneChanger.IsLoading ())
			{
				if (!menuIsOff)
				{
					KickStarter.playerMenus.UpdateLoadingMenus ();
				}
				return;
			}

			if (gameState != GameState.Paused)
			{
				lastNonPausedState = gameState;
			}
			if (!inputIsOff)
			{
				if (gameState == GameState.DialogOptions)
				{
					KickStarter.playerInput.DetectConversationInputs ();
				}
				KickStarter.playerInput.UpdateInput ();

				if (IsInGameplay ())
				{
					KickStarter.playerInput.UpdateDirectInput ();
				}

				if (gameState != GameState.Paused)
				{
					KickStarter.playerQTE.UpdateQTE ();
				}
			}

			KickStarter.dialog._Update ();

			KickStarter.playerInteraction.UpdateInteractionLabel ();

			if (!cursorIsOff)
			{
				KickStarter.playerCursor.UpdateCursor ();

				bool canHideHotspots = KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.hideUnhandledHotspots;
				bool canDrawHotspotIcons = (KickStarter.settingsManager.hotspotIconDisplay != HotspotIconDisplay.Never);
				bool canUpdateProximity = (KickStarter.settingsManager.hotspotDetection == HotspotDetection.PlayerVicinity && KickStarter.settingsManager.placeDistantHotspotsOnSeparateLayer && KickStarter.player != null);

				for (_i=0; _i<hotspots.Count; _i++)
				{
					bool showing = (canHideHotspots) ? hotspots[_i].UpdateUnhandledVisibility () : true;
					if (showing)
					{
						if (canDrawHotspotIcons)
						{
							if (KickStarter.settingsManager.hotspotIconDisplay != HotspotIconDisplay.Never)
							{
								hotspots[_i].UpdateIcon ();
								if (KickStarter.settingsManager.hotspotDrawing == ScreenWorld.WorldSpace)
								{
									hotspots[_i].DrawHotspotIcon (true);
								}
							}
						}

						if (canUpdateProximity)
						{
							hotspots[_i].UpdateProximity (KickStarter.player.hotspotDetector);
						}
					}
				}
			}

			if (!menuIsOff)
			{
				KickStarter.playerMenus.CheckForInput ();
			}

			if (!menuIsOff)
			{
				if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.playerInput.GetMouseState () != MouseState.Normal)
				{
					KickStarter.playerMenus.UpdateAllMenus ();
				}
			}

			if (!interactionIsOff)
			{
				KickStarter.playerInteraction.UpdateInteraction ();

				for (_i=0; _i<highlights.Count; _i++)
				{
					highlights[_i]._Update ();
				}

				if (KickStarter.settingsManager.hotspotDetection == HotspotDetection.MouseOver && KickStarter.settingsManager.scaleHighlightWithMouseProximity)
				{
					bool setProximity = IsInGameplay ();
					for (_i=0; _i<hotspots.Count; _i++)
					{
						hotspots[_i].SetProximity (setProximity);
					}
				}
			}

			if (!triggerIsOff)
			{
				for (_i=0; _i<triggers.Count; _i++)
				{
					triggers[_i]._Update ();
				}
			}

			if (!menuIsOff)
			{
				KickStarter.playerMenus.UpdateAllMenus ();
			}

			KickStarter.actionListManager.UpdateActionListManager ();

			for (_i=0; _i<dragBases.Count; _i++)
			{
				dragBases[_i].UpdateMovement ();
			}

			if (!movementIsOff)
			{
				if (IsInGameplay () && KickStarter.settingsManager && KickStarter.settingsManager.movementMethod != MovementMethod.None)
				{
					KickStarter.playerMovement.UpdatePlayerMovement ();
				}

				KickStarter.playerMovement.UpdateFPCamera ();
			}

			if (!interactionIsOff)
			{
				KickStarter.playerInteraction.UpdateInventory ();
			}

			for (_i=0; _i<limitVisibilitys.Count; _i++)
			{
				limitVisibilitys[_i]._Update ();
			}

			for (_i=0; _i<sounds.Count; _i++)
			{
				sounds[_i]._Update ();
			}

			for (_i=0; _i<characters.Count; _i++)
			{
				if (characters[_i] != null && (!playerIsOff || !(characters[_i].IsPlayer)))
				{
					characters[_i]._Update ();
				}
			}

			if (!cameraIsOff)
			{
				for (_i=0; _i<cameras.Count; _i++)
				{
					cameras[_i]._Update ();
				}
			}

			if (HasGameStateChanged ())
			{
				KickStarter.eventManager.Call_OnChangeGameState (previousUpdateState);

				if (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson)
				{
					if (IsInGameplay () || (gameState == GameState.DialogOptions && KickStarter.settingsManager.useFPCamDuringConversations))
					{
						KickStarter.mainCamera.SetFirstPerson ();
					}
				}

				if (Time.time > 0f && gameState != GameState.Paused)
				{
					AudioListener.pause = false;
				}

				if (gameState == GameState.Cutscene && previousUpdateState != GameState.Cutscene)
				{
					KickStarter.playerMenus.MakeUINonInteractive ();
				}
				else if (gameState != GameState.Cutscene && previousUpdateState == GameState.Cutscene)
				{
					KickStarter.playerMenus.MakeUIInteractive ();
				}
			}

			previousUpdateState = gameState;
		}


		protected void LateUpdate ()
		{
			if (isACDisabled || !hasGameEngine)
			{
				return;
			}

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			for (_i=0; _i<characters.Count; _i++)
			{
				if (!playerIsOff || !(characters[_i].IsPlayer))
				{
					characters[_i]._LateUpdate ();
				}
			}

			if (!cameraIsOff)
			{
				KickStarter.mainCamera._LateUpdate ();
			}

			for (_i=0; _i<parallax2Ds.Count; _i++)
			{
				parallax2Ds[_i].UpdateOffset ();
			}

			for (_i=0; _i<sortingMaps.Count; _i++)
			{
				sortingMaps[_i].UpdateSimilarFollowers ();
			}

			KickStarter.dialog._LateUpdate ();
		}


		protected void FixedUpdate ()
		{
			if (isACDisabled || !hasGameEngine)
			{
				return;
			}

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			for (_i=0; _i<characters.Count; _i++)
			{
				if (!playerIsOff || !(characters[_i].IsPlayer))
				{
					characters[_i]._FixedUpdate ();
				}
			}

			for (_i=0; _i<dragBases.Count; _i++)
			{
				dragBases[_i]._FixedUpdate ();
			}

			KickStarter.playerInput._FixedUpdate ();
		}


		#if ACIgnoreOnGUI
		#else

		protected void OnGUI ()
		{
			if (!isACDisabled)
			{
				_OnGUI ();
			}
		}

		#endif


		/**
		 * Runs all of AC's OnGUI code.
		 * This is called automatically from within StateHandler, unless 'ACIgnoreOnGUI' is listed in Unity's Scripting Define Symbols box in the Player settings.
		 */
		public void _OnGUI ()
		{
			if (!hasGameEngine)
			{
				return;
			}

			StatusBox.DrawDebugWindow ();

			if (KickStarter.settingsManager.IsInLoadingScene () || KickStarter.sceneChanger.IsLoading ())
			{
				if (!cameraIsOff && !KickStarter.settingsManager.IsInLoadingScene ())
				{
					KickStarter.mainCamera.DrawCameraFade ();
				}
				if (!menuIsOff)
				{
					KickStarter.playerMenus.DrawLoadingMenus ();
				}
				if (!cameraIsOff)
				{
					KickStarter.mainCamera.DrawBorders ();
				}
				return;
			}

			if (!cursorIsOff && !KickStarter.saveSystem.IsTakingSaveScreenshot)
			{
				if (KickStarter.settingsManager.hotspotIconDisplay != HotspotIconDisplay.Never &&
				   KickStarter.settingsManager.hotspotDrawing == ScreenWorld.ScreenSpace)
				{
					for (_i=0; _i<hotspots.Count; _i++)
					{
						hotspots[_i].DrawHotspotIcon ();
					}
				}

				if (IsInGameplay ())
				{
					for (_i=0; _i<dragBases.Count; _i++)
					{
						dragBases[_i].DrawGrabIcon ();
					}
				}
			}

			if (!inputIsOff)
			{
				if (gameState == GameState.DialogOptions)
				{
					KickStarter.playerInput.DetectConversationNumerics ();
				}
				KickStarter.playerInput.DrawDragLine ();

				for (_i=0; _i<arrowPrompts.Count; _i++)
				{
					arrowPrompts[_i].DrawArrows ();
				}
			}

			if (!menuIsOff)
			{
				KickStarter.playerMenus.DrawMenus ();
			}

			if (!cursorIsOff)
			{
				if (KickStarter.cursorManager.cursorRendering == CursorRendering.Software)
				{
					KickStarter.playerCursor.DrawCursor ();
				}
			}

			if (!cameraIsOff)
			{
				KickStarter.mainCamera.DrawCameraFade ();
				KickStarter.mainCamera.DrawBorders ();
			}
		}

		#endregion


		#region PublicFunctions

		/** The current state of the game (Normal, Cutscene, Paused, DialogOptions) */
		public GameState gameState
		{
			get
			{
				return _gameState;
			}
			set
			{
				if (KickStarter.mainCamera)
				{
					KickStarter.mainCamera.CancelPauseGame ();
				}
				_gameState = value;
			}
		}



		/**
		 * Alerts the StateHandler that a Game Engine prefab is present in the scene.
		 * This is called from KickStarter when the game begins - the StateHandler will not run until this is done.
		 */
		public void RegisterWithGameEngine ()
		{
			if (!hasGameEngine)
			{
				hasGameEngine = true;
			}
		}


		/**
		 * Alerts the StateHandler that a Game Engine prefab is no longer present in the scene.
		 * This is called from KickStarter's OnDestroy function.
		 */
		public void UnregisterWithGameEngine ()
		{
			hasGameEngine = false;
		}


		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			inScriptedCutscene = false;
		}


		/**
		 * <summary>Runs the ActionListAsset defined in SettingsManager's actionListOnStart when the game begins.</summary>
		 * <returns>True if an ActionListAsset was run</returns>
		 */
		public bool PlayGlobalOnStart ()
		{
			if (runAtLeastOnce)
			{
				return false;
			}

			runAtLeastOnce = true;

			ActiveInput.Upgrade ();
			if (KickStarter.settingsManager.activeInputs != null)
			{
				foreach (ActiveInput activeInput in KickStarter.settingsManager.activeInputs)
				{
					activeInput.SetDefaultState ();
				}
			}

			if (gameState != GameState.Paused)
			{
				// Fix for audio pausing on start
				AudioListener.pause = false;
			}

			if (KickStarter.settingsManager.actionListOnStart)
			{
				AdvGame.RunActionListAsset (KickStarter.settingsManager.actionListOnStart);
				return true;
			}

			return false;
		}


		/**
		 * Allows the ActionListAsset defined in SettingsManager's actionListOnStart to be run again.
		 */
		public void CanGlobalOnStart ()
		{
			runAtLeastOnce = false;
		}


		/**
		 * Calls Physics.IgnoreCollision on all appropriate Collider combinations (Unity 5 only).
		 */
		public void IgnoreNavMeshCollisions ()
		{
			Collider[] allColliders = FindObjectsOfType (typeof(Collider)) as Collider[];
			for (_i=0; _i<navMeshBases.Count; _i++)
			{
				navMeshBases[_i].IgnoreNavMeshCollisions (allColliders);
			}
		}


		/**
		 * Sets the maximum volume of all Sound objects in the scene.
		 */
		public void UpdateAllMaxVolumes ()
		{
			foreach (Sound sound in sounds)
			{
				sound.SetMaxVolume ();
			}
		}


		/**
		 * <summary>Gets the last value of gameState that wasn't GameState.Paused.</summary>
		 * <returns>The last value of gameState that wasn't GameState.Paused</summary>
		 */
		public GameState GetLastNonPausedState ()
		{
			return lastNonPausedState;
		}
		

		/**
		 * Restores the gameState to its former state after un-pausing the game.
		 */
		public void RestoreLastNonPausedState ()
		{
			if (Time.timeScale <= 0f)
			{
				KickStarter.sceneSettings.UnpauseGame (KickStarter.playerInput.timeScale);
			}

			if (KickStarter.playerInteraction.InPreInteractionCutscene)
			{
				gameState = GameState.Cutscene;
				return;
			}

			if (KickStarter.actionListManager.IsGameplayBlocked () || inScriptedCutscene)
			{
				gameState = GameState.Cutscene;
			}
			else if (KickStarter.playerInput.IsInConversation (true))
			{
				gameState = GameState.DialogOptions;
			}
			else
			{
				gameState = GameState.Normal;
			}
		}


		/**
		 * <summary>Goes through all Hotspots in the scene, and limits their enabed state based on a specific _Camera, if appropriate.</summary>
		 * <param name = "_camera">The _Camera to attempt to limit all Hotspots to</param>
		 */
		public void LimitHotspotsToCamera (_Camera _camera)
		{
			if (_camera != null)
			{
				for (_i=0; _i<hotspots.Count; _i++)
				{
					hotspots[_i].LimitToCamera (_camera);
				}
			}
		}


		/**
		 * Begins a hard-coded cutscene.
		 * Gameplay will resume once EndCutscene() is called.
		 */
		public void StartCutscene ()
		{
			inScriptedCutscene = true;
			gameState = GameState.Cutscene;
		}


		/**
		 * Ends a hard-coded cutscene, started by calling StartCutscene().
		 */
		public void EndCutscene ()
		{
			inScriptedCutscene = false;
			if (KickStarter.playerMenus.ArePauseMenusOn (null))
			{
				KickStarter.mainCamera.PauseGame ();
			}
			else
			{
				RestoreLastNonPausedState ();
			}
		}


		/**
		 * <summary>Checks if the game is currently in a user-scripted cutscene.</summary>
		 * <returns>True if the game is currently in a user-scripted cutscene</returns>
		 */
		public bool IsInScriptedCutscene ()
		{
			return inScriptedCutscene;
		}


		/**
		 * <summary>Checks if the game is currently in a cutscene, scripted or otherwise.</summary>
		 * <returns>True if the game is currently in a cutscene</returns>
		 */
		public bool IsInCutscene ()
		{
			return (!isACDisabled && gameState == GameState.Cutscene);
		}


		/**
		 * <summary>Checks if the game is currently paused.</summary>
		 * <returns>True if the game is currently paused</returns>
		 */
		public bool IsPaused ()
		{
			return (!isACDisabled && gameState == GameState.Paused);
		}


		/**
		 * <summary>Checks if the game is currently in regular gameplay.</summary>
		 * <returns>True if the game is currently in regular gameplay</returns>
		 */
		public bool IsInGameplay ()
		{
			if (isACDisabled)
			{
				return false;
			}
			if (gameState == GameState.Normal)
			{
				return true;
			}
			if (gameState == GameState.DialogOptions && KickStarter.settingsManager.allowGameplayDuringConversations)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Enables or disables Adventure Creator completely.</summary>
		 * <param name = "state">If True, then Adventure Creator will be enabled. If False, then Adventure Creator will be disabled.</param>
		 */
		public void SetACState (bool state)
		{
			isACDisabled = !state;
		}


		/**
		 * <summary>Checks if AC is currently enabled.<summary>
		 * <returns>Trye if AC is currently enabled.<returns>
		 */
		public bool IsACEnabled ()
		{
			return !isACDisabled;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerCursor system.</summary>
		 * <param name = "state">If True, the PlayerCursor system will be enabled</param>
		 */
		public void SetCursorSystem (bool state)
		{
			cursorIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerInput system.</summary>
		 * <param name = "state">If True, the PlayerInput system will be enabled</param>
		 */
		public void SetInputSystem (bool state)
		{
			inputIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the Interaction system.</summary>
		 * <param name = "state">If True, the Interaction system will be enabled</param>
		 */
		public void SetInteractionSystem (bool state)
		{
			interactionIsOff = !state;

			if (!state)
			{
				KickStarter.playerInteraction.DeselectHotspot (true);
			}
		}


		/**
		 * <summary>Sets the enabled state of the Draggable system.</summary>
		 * <param name = "state">If True, the Draggable system will be enabled</param>
		 */
		public void SetDraggableSystem (bool state)
		{
			draggablesIsOff = !state;

			if (!state)
			{
				KickStarter.playerInput.LetGo ();
			}
		}


		/**
		 * <summary>Checks if the interaction system is enabled.</summary>
		 * <returns>True if the interaction system is enabled</returns>
		 */
		public bool CanInteract ()
		{
			return !interactionIsOff;
		}


		/**
		 * <summary>Checks if the draggables system is enabled.</summary>
		 * <returns>True if the draggables system is enabled</returns>
		 */
		public bool CanInteractWithDraggables ()
		{
			return !draggablesIsOff;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerMenus system.</summary>
		 * <param name = "state">If True, the PlayerMenus system will be enabled</param>
		 */
		public void SetMenuSystem (bool state)
		{
			menuIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerMovement system.</summary>
		 * <param name = "state">If True, the PlayerMovement system will be enabled</param>
		 */
		public void SetMovementSystem (bool state)
		{
			movementIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the MainCamera system.</summary>
		 * <param name = "state">If True, the MainCamera system will be enabled</param>
		 */
		public void SetCameraSystem (bool state)
		{
			cameraIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the trigger system.</summary>
		 * <param name = "state">If True, the trigger system will be enabled</param>
		 */
		public void SetTriggerSystem (bool state)
		{
			triggerIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the Player system.</summary>
		 * <param name = "state">If True, the Player system will be enabled</param>
		 */
		public void SetPlayerSystem (bool state)
		{
			playerIsOff = !state;
		}


		/**
		 * <summary>Checks if the trigger system is disabled.</summary>
		 * <returns>True if the trigger system is disabled</returns>
		 */
		public bool AreTriggersDisabled ()
		{
			return triggerIsOff;
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			mainData.cursorIsOff = cursorIsOff;
			mainData.inputIsOff = inputIsOff;
			mainData.interactionIsOff = interactionIsOff;
			mainData.menuIsOff = menuIsOff;
			mainData.movementIsOff = movementIsOff;
			mainData.cameraIsOff = cameraIsOff;
			mainData.triggerIsOff = triggerIsOff;
			mainData.playerIsOff = playerIsOff;

			if (music != null)
			{
				mainData = music.SaveMainData (mainData);
			}

			if (ambience != null)
			{
				mainData = ambience.SaveMainData (mainData);
			}

			mainData = KickStarter.runtimeObjectives.SaveGlobalObjectives (mainData);

			return mainData;
		}


		/**
		 * <summary>Updates its own variables from a MainData class.</summary>
		 * <param name = "mainData">The MainData class to load from</param>
		 */
		public void LoadMainData (MainData mainData)
		{
			cursorIsOff = mainData.cursorIsOff;
			inputIsOff = mainData.inputIsOff;
			interactionIsOff = mainData.interactionIsOff;
			menuIsOff = mainData.menuIsOff;
			movementIsOff = mainData.movementIsOff;
			cameraIsOff = mainData.cameraIsOff;
			triggerIsOff = mainData.triggerIsOff;
			playerIsOff = mainData.playerIsOff;

			if (music == null)
			{
				CreateMusicEngine ();
			}
			music.LoadMainData (mainData);

			if (ambience == null)
			{
				CreateAmbienceEngine ();
			}
			ambience.LoadMainData (mainData);
			KickStarter.runtimeObjectives.AssignGlobalObjectives (mainData);
		}


		/**
		 * <summary>Gets the Music component used to handle AudioClips played using the 'Sound: Play music' Action.</summary>
		 * <returns>The Music component used to handle AudioClips played using the 'Sound: Play music' Action.</returns>
		 */
		public Music GetMusicEngine ()
		{
			if (music == null)
			{
				CreateMusicEngine ();
			}
			return music;
		}


		/**
		 * <summary>Gets the Ambience component used to handle AudioClips played using the 'Sound: Play ambience' Action.</summary>
		 * <returns>The Ambience component used to handle AudioClips played using the 'Sound: Play ambience' Action.</returns>
		 */
		public Ambience GetAmbienceEngine ()
		{
			if (ambience == null)
			{
				CreateAmbienceEngine ();
			}
			return ambience;
		}

		#endregion


		#region ProtectedFunctions

		protected void InitPersistentEngine ()
		{
			KickStarter.runtimeLanguages.OnAwake ();
			KickStarter.options.OnStart ();

			KickStarter.localVariables.OnStart ();

			KickStarter.sceneChanger.OnAwake ();
			KickStarter.levelStorage.OnAwake ();
			
			KickStarter.runtimeVariables.OnStart ();
			KickStarter.runtimeInventory.OnStart ();
			KickStarter.runtimeDocuments.OnStart ();

			KickStarter.playerMenus.OnStart ();
		}


		protected bool HasGameStateChanged ()
		{
			if (previousUpdateState != gameState)
			{
				return true;
			}
			return false;
		}


		protected void CreateMusicEngine ()
		{
			if (music == null)
			{
				music = CreateSoundtrackEngine <Music> (Resource.musicEngine);
			}
		}


		protected void CreateAmbienceEngine ()
		{
			if (ambience == null)
			{
				ambience = CreateSoundtrackEngine <Ambience> (Resource.ambienceEngine);
			}
		}


		protected T CreateSoundtrackEngine <T> (string resourceName) where T : Soundtrack
		{
			GameObject soundtrackOb = (GameObject) Instantiate (Resources.Load (resourceName));
			if (soundtrackOb != null)
			{
				soundtrackOb.name = AdvGame.GetName (resourceName);
				return soundtrackOb.GetComponent <T>();
			}
			else
			{
				ACDebug.LogError ("Cannot find " + resourceName + " prefab in /AdventureCreator/Resources - did you import AC completely?");
				return null;
			}
		}

		#endregion


		#region GetSet

		/* Checks if the game was unpaused in the previous frame. */
		public bool UnpausedLastFrame
		{
			get
			{
				if (gameState != GameState.Paused && previousUpdateState == GameState.Paused)
				{
					return true;
				}
				return false;
			}
		}


		/** A List of all Char components found in the scene */
		public List<Char> Characters
		{
			get
			{
				return characters;
			}
		}


		/** A List of all Hotspot components found in the scene */
		public List<Hotspot> Hotspots
		{
			get
			{
				return hotspots;
			}
		}


		/** A List of all FollowSortingMap components found in the scene */
		public List<FollowSortingMap> FollowSortingMaps
		{
			get
			{
				return followSortingMaps;
			}
		}


		/** A List of all SortingMap components found in the scene */
		public List<SortingMap> SortingMaps
		{
			get
			{
				return sortingMaps;
			}
		}


		/** A List of all BackgroundCamera components found in the scene */
		public List<BackgroundCamera> BackgroundCameras
		{
			get
			{
				return backgroundCameras;
			}
		}


		/** A List of all BackgroundImage components found in the scene */
		public List<BackgroundImage> BackgroundImages
		{
			get
			{
				return backgroundImages;
			}
		}


		/** A List of all ConstantID components found in the scene */
		public List<ConstantID> ConstantIDs
		{
			get
			{
				return constantIDs;
			}
		}


		/** True if the Movement system has been disabled */
		public bool MovementIsOff
		{
			get
			{
				return movementIsOff;
			}
		}

		#endregion


		#region ObjectRecordKeeping

		/**
		 * <summary>Registers an ArrowPrompt, so that it can be updated</summary>
		 * <param name = "_object">The ArrowPrompt to register</param>
		 */
		public void Register (ArrowPrompt _object)
		{
			if (!arrowPrompts.Contains (_object))
			{
				arrowPrompts.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters an ArrowPrompt, so that it is no longer updated</summary>
		 * <param name = "_object">The ArrowPrompt to unregister</param>
		 */
		public void Unregister (ArrowPrompt _object)
		{
			if (arrowPrompts.Contains (_object))
			{
				arrowPrompts.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a DragBase, so that it can be updated</summary>
		 * <param name = "_object">The DragBase to register</param>
		 */
		public void Register (DragBase _object)
		{
			if (!dragBases.Contains (_object))
			{
				dragBases.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a DragBase, so that it is no longer updated</summary>
		 * <param name = "_object">The DragBase to unregister</param>
		 */
		public void Unregister (DragBase _object)
		{
			if (dragBases.Contains (_object))
			{
				dragBases.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a Parallax2D, so that it can be updated</summary>
		 * <param name = "_object">The Parallax2D to register</param>
		 */
		public void Register (Parallax2D _object)
		{
			if (!parallax2Ds.Contains (_object))
			{
				parallax2Ds.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a Parallax2D, so that it is no longer updated</summary>
		 * <param name = "_object">The Parallax2D to unregister</param>
		 */
		public void Unregister (Parallax2D _object)
		{
			if (parallax2Ds.Contains (_object))
			{
				parallax2Ds.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a Hotspot, so that it can be updated</summary>
		 * <param name = "_object">The Hotspot to register</param>
		 */
		public void Register (Hotspot _object)
		{
			if (!hotspots.Contains (_object))
			{
				hotspots.Add (_object);

				if (KickStarter.eventManager != null)
				{
					KickStarter.eventManager.Call_OnRegisterHotspot (_object, true);
				}
			}
		}


		/**
		 * <summary>Unregisters a Hotspot, so that it is no longer updated</summary>
		 * <param name = "_object">The Hotspot to unregister</param>
		 */
		public void Unregister (Hotspot _object)
		{
			if (hotspots.Contains (_object))
			{
				hotspots.Remove (_object);

				if (KickStarter.eventManager != null)
				{
					KickStarter.eventManager.Call_OnRegisterHotspot (_object, false);
				}
			}
		}


		/**
		 * <summary>Registers a Highlight, so that it can be updated</summary>
		 * <param name = "_object">The Highlight to register</param>
		 */
		public void Register (Highlight _object)
		{
			if (!highlights.Contains (_object))
			{
				highlights.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a Highlight, so that it is no longer updated</summary>
		 * <param name = "_object">The Highlight to unregister</param>
		 */
		public void Unregister (Highlight _object)
		{
			if (highlights.Contains (_object))
			{
				highlights.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a AC_Trigger, so that it can be updated</summary>
		 * <param name = "_object">The AC_Trigger to register</param>
		 */
		public void Register (AC_Trigger _object)
		{
			if (!triggers.Contains (_object))
			{
				triggers.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a AC_Trigger, so that it is no longer updated</summary>
		 * <param name = "_object">The AC_Trigger to unregister</param>
		 */
		public void Unregister (AC_Trigger _object)
		{
			if (triggers.Contains (_object))
			{
				triggers.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a _Camera, so that it can be updated</summary>
		 * <param name = "_object">The _Camera to register</param>
		 */
		public void Register (_Camera _object)
		{
			if (!cameras.Contains (_object))
			{
				cameras.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a _Camera, so that it is no longer updated</summary>
		 * <param name = "_object">The _Camera to unregister</param>
		 */
		public void Unregister (_Camera _object)
		{
			if (cameras.Contains (_object))
			{
				cameras.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a Sound, so that it can be updated</summary>
		 * <param name = "_object">The Sound to register</param>
		 */
		public void Register (Sound _object)
		{
			if (!sounds.Contains (_object))
			{
				sounds.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a Sound, so that it is no longer updated</summary>
		 * <param name = "_object">The Sound to unregister</param>
		 */
		public void Unregister (Sound _object)
		{
			if (sounds.Contains (_object))
			{
				sounds.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a LimitVisibility, so that it can be updated</summary>
		 * <param name = "_object">The LimitVisibility to register</param>
		 */
		public void Register (LimitVisibility _object)
		{
			if (!limitVisibilitys.Contains (_object))
			{
				limitVisibilitys.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a LimitVisibility, so that it is no longer updated</summary>
		 * <param name = "_object">The LimitVisibility to unregister</param>
		 */
		public void Unregister (LimitVisibility _object)
		{
			if (limitVisibilitys.Contains (_object))
			{
				limitVisibilitys.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a Char, so that it can be updated</summary>
		 * <param name = "_object">The Char to register</param>
		 */
		public void Register (Char _object)
		{
			if (!characters.Contains (_object))
			{
				characters.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a Char, so that it is no longer updated</summary>
		 * <param name = "_object">The Char to unregister</param>
		 */
		public void Unregister (Char _object)
		{
			if (characters.Contains (_object))
			{
				characters.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a FollowSortingMap, so that it can be updated</summary>
		 * <param name = "_object">The FollowSortingMap to register</param>
		 */
		public void Register (FollowSortingMap _object)
		{
			if (!followSortingMaps.Contains (_object))
			{
				followSortingMaps.Add (_object);
				_object.UpdateSortingMap ();
			}
		}


		/**
		 * <summary>Unregisters a FollowSortingMap, so that it is no longer updated</summary>
		 * <param name = "_object">The FollowSortingMap to unregister</param>
		 */
		public void Unregister (FollowSortingMap _object)
		{
			if (followSortingMaps.Contains (_object))
			{
				followSortingMaps.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a NavMeshBase, so that it can be updated</summary>
		 * <param name = "_object">The NavMeshBase to register</param>
		 */
		public void Register (NavMeshBase _object)
		{
			if (!navMeshBases.Contains (_object))
			{
				navMeshBases.Add (_object);
				_object.IgnoreNavMeshCollisions ();
			}
		}


		/**
		 * <summary>Unregisters a NavMeshBase, so that it is no longer updated</summary>
		 * <param name = "_object">The NavMeshBase to unregister</param>
		 */
		public void Unregister (NavMeshBase _object)
		{
			if (navMeshBases.Contains (_object))
			{
				navMeshBases.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a SortingMap, so that it can be updated</summary>
		 * <param name = "_object">The SortingMap to register</param>
		 */
		public void Register (SortingMap _object)
		{
			if (!sortingMaps.Contains (_object))
			{
				sortingMaps.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a SortingMap, so that it is no longer updated</summary>
		 * <param name = "_object">The SortingMap to unregister</param>
		 */
		public void Unregister (SortingMap _object)
		{
			if (sortingMaps.Contains (_object))
			{
				sortingMaps.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a BackgroundCamera, so that it can be updated</summary>
		 * <param name = "_object">The BackgroundCamera to register</param>
		 */
		public void Register (BackgroundCamera _object)
		{
			if (!backgroundCameras.Contains (_object))
			{
				backgroundCameras.Add (_object);
				_object.UpdateRect ();
			}
		}


		/**
		 * <summary>Unregisters a BackgroundCamera, so that it is no longer updated</summary>
		 * <param name = "_object">The BackgroundCamera to unregister</param>
		 */
		public void Unregister (BackgroundCamera _object)
		{
			if (backgroundCameras.Contains (_object))
			{
				backgroundCameras.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a BackgroundImage, so that it can be updated</summary>
		 * <param name = "_object">The BackgroundImage to register</param>
		 */
		public void Register (BackgroundImage _object)
		{
			if (!backgroundImages.Contains (_object))
			{
				backgroundImages.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a BackgroundImage, so that it is no longer updated</summary>
		 * <param name = "_object">The BackgroundImage to unregister</param>
		 */
		public void Unregister (BackgroundImage _object)
		{
			if (backgroundImages.Contains (_object))
			{
				backgroundImages.Remove (_object);
			}
		}


		/**
		 * <summary>Registers a ConstantID, so that it can be updated</summary>
		 * <param name = "_object">The ConstantID to register</param>
		 */
		public void Register (ConstantID _object)
		{
			if (!constantIDs.Contains (_object))
			{
				constantIDs.Add (_object);
			}
		}


		/**
		 * <summary>Unregisters a ConstantID, so that it is no longer updated</summary>
		 * <param name = "_object">The ConstantID to unregister</param>
		 */
		public void Unregister (ConstantID _object)
		{
			if (constantIDs.Contains (_object))
			{
				constantIDs.Remove (_object);
			}
		}

		#endregion

	}

}