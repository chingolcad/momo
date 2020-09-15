/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"KickStarter.cs"
 * 
 *	This script will make sure that PersistentEngine and the Player gameObjects are always created,
 *	regardless of which scene the game is begun from.  It will also check the key gameObjects for
 *	essential scripts and references.
 * 
 */

using UnityEngine;

namespace AC
{
	
	/**
	 * This component instantiates the PersistentEngine and Player prefabs when the game beings.
	 * It also provides static references to each of Adventure Creator's main components.
	 * It should be attached to the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_kick_starter.html")]
	public class KickStarter : MonoBehaviour
	{
		
		private static Player playerPrefab = null;
		private static MainCamera mainCameraPrefab = null;
		private static Camera cameraMain = null;
		private static GameObject persistentEnginePrefab = null;
		private static GameObject gameEnginePrefab = null;
		
		// Managers
		private static SceneManager sceneManagerPrefab = null;
		private static SettingsManager settingsManagerPrefab = null;
		private static ActionsManager actionsManagerPrefab = null;
		private static VariablesManager variablesManagerPrefab = null;
		private static InventoryManager inventoryManagerPrefab = null;
		private static SpeechManager speechManagerPrefab = null;
		private static CursorManager cursorManagerPrefab = null;
		private static MenuManager menuManagerPrefab = null;
		
		// PersistentEngine components
		private static Options optionsComponent = null;
		private static RuntimeInventory runtimeInventoryComponent = null;
		private static RuntimeVariables runtimeVariablesComponent = null;
		private static PlayerMenus playerMenusComponent = null;
		private static StateHandler stateHandlerComponent = null;
		private static SceneChanger sceneChangerComponent = null;
		private static SaveSystem saveSystemComponent = null;
		private static LevelStorage levelStorageComponent = null;
		private static RuntimeLanguages runtimeLanguagesComponent = null;
		private static RuntimeDocuments runtimeDocumentsComponent = null;
		private static RuntimeObjectives runtimeObjectivesComponent = null;
		private static ActionListAssetManager actionListAssetManagerComponent = null;
		
		// GameEngine components
		private static MenuSystem menuSystemComponent = null;
		private static Dialog dialogComponent = null;
		private static PlayerInput playerInputComponent = null;
		private static PlayerInteraction playerInteractionComponent = null;
		private static PlayerMovement playerMovementComponent = null;
		private static PlayerCursor playerCursorComponent = null;
		private static PlayerQTE playerQTEComponent = null;
		private static SceneSettings sceneSettingsComponent = null;
		private static NavigationManager navigationManagerComponent = null;
		private static ActionListManager actionListManagerComponent = null;
		private static LocalVariables localVariablesComponent = null;
		private static MenuPreview menuPreviewComponent = null;
		private static EventManager eventManagerComponent = null;
		private static KickStarter kickStarterComponent = null;


		protected void Awake ()
		{
			if (GetComponent <MultiSceneChecker>() == null)
			{
				ACDebug.LogError ("A 'MultiSceneChecker' component must be attached to the GameEngine prefab - please re-import AC.", gameObject);
			}
		}


		protected void OnDestroy ()
		{
			if (stateHandler)
			{
				stateHandler.UnregisterWithGameEngine ();
			}
		}


		public static void SetGameEngine (GameObject _gameEngine = null)
		{
			if (_gameEngine != null)
			{
				gameEnginePrefab = _gameEngine;

				menuSystemComponent = null;
				playerCursorComponent = null;
				playerInputComponent = null;
				playerInteractionComponent = null;
				playerMovementComponent = null;
				playerMenusComponent = null;
				playerQTEComponent = null;
				kickStarterComponent = null;
				sceneSettingsComponent = null;
				dialogComponent = null;
				menuPreviewComponent = null;
				navigationManagerComponent = null;
				actionListManagerComponent = null;
				localVariablesComponent = null;
				eventManagerComponent = null;

				return;
			}

			if (gameEnginePrefab == null)
			{
				SceneSettings sceneSettings = UnityVersionHandler.GetKickStarterComponent <SceneSettings>();
				if (sceneSettings != null)
				{
					gameEnginePrefab = sceneSettings.gameObject;
				}
			}
		}


		private static void SetPersistentEngine ()
		{
			if (persistentEnginePrefab == null)
			{
				StateHandler stateHandler = UnityVersionHandler.GetKickStarterComponent <StateHandler>();
				if (stateHandler != null)
				{
					persistentEnginePrefab = stateHandler.gameObject;
				}
				else
				{
					try
					{
						persistentEnginePrefab = (GameObject) Instantiate (Resources.Load (Resource.persistentEngine));
						persistentEnginePrefab.name = AdvGame.GetName (Resource.persistentEngine);
					}
					catch (System.Exception e)
		 			{
						ACDebug.LogWarning ("Could not create PersistentEngine - make sure " + Resource.persistentEngine + ", prefab is present in a Resources folder. Exception: " + e);
		 			}

		 			if (persistentEnginePrefab != null)
		 			{
						stateHandler = persistentEnginePrefab.GetComponent <StateHandler>();
						if (stateHandler != null)
						{
							stateHandler.OnAwake ();
						}
						else
						{
							ACDebug.LogWarning ("Could not find StateHandler component on the PersistentEngine - is one attached?", persistentEnginePrefab);
						}
					}
				}
			}
		}


		/**
		 * Clears the internal Manager references.  Call this when changing the assigned Managers, so that other Inspectors/Editors get updated to reflect this
		 */
		public static void ClearManagerCache ()
		{
			sceneManagerPrefab = null;
			settingsManagerPrefab = null;
			actionsManagerPrefab = null;
			variablesManagerPrefab = null;
			inventoryManagerPrefab = null;
			speechManagerPrefab = null;
			cursorManagerPrefab = null;
			menuManagerPrefab = null;
		}
		
		
		public static SceneManager sceneManager
		{
			get
			{
				if (sceneManagerPrefab != null) return sceneManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().sceneManager)
				{
					sceneManagerPrefab = AdvGame.GetReferences ().sceneManager;
					return sceneManagerPrefab;
				}
				return null;
			}
			set
			{
				sceneManagerPrefab = value;
			}
		}
		
		
		public static SettingsManager settingsManager
		{
			get
			{
				if (settingsManagerPrefab != null) return settingsManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().settingsManager)
				{
					settingsManagerPrefab = AdvGame.GetReferences ().settingsManager;
					return settingsManagerPrefab;
				}
				return null;
			}
			set
			{
				settingsManagerPrefab = value;
			}
		}
		
		
		public static ActionsManager actionsManager
		{
			get
			{
				if (actionsManagerPrefab != null) return actionsManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().actionsManager)
				{
					actionsManagerPrefab = AdvGame.GetReferences ().actionsManager;
					return actionsManagerPrefab;
				}
				return null;
			}
			set
			{
				actionsManagerPrefab = value;
			}
		}
		
		
		public static VariablesManager variablesManager
		{
			get
			{
				if (variablesManagerPrefab != null) return variablesManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
				{
					variablesManagerPrefab = AdvGame.GetReferences ().variablesManager;
					return variablesManagerPrefab;
				}
				return null;
			}
			set
			{
				variablesManagerPrefab = value;
			}
		}
		
		
		public static InventoryManager inventoryManager
		{
			get
			{
				if (inventoryManagerPrefab != null) return inventoryManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().inventoryManager)
				{
					inventoryManagerPrefab = AdvGame.GetReferences ().inventoryManager;
					return inventoryManagerPrefab;
				}
				return null;
			}
			set
			{
				inventoryManagerPrefab = value;
			}
		}
		
		
		public static SpeechManager speechManager
		{
			get
			{
				if (speechManagerPrefab != null) return speechManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().speechManager)
				{
					speechManagerPrefab = AdvGame.GetReferences ().speechManager;
					return speechManagerPrefab;
				}
				return null;
			}
			set
			{
				speechManagerPrefab = value;
			}
		}
		
		
		public static CursorManager cursorManager
		{
			get
			{
				if (cursorManagerPrefab != null) return cursorManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().cursorManager)
				{
					cursorManagerPrefab = AdvGame.GetReferences ().cursorManager;
					return cursorManagerPrefab;
				}
				return null;
			}
			set
			{
				cursorManagerPrefab = value;
			}
		}
		
		
		public static MenuManager menuManager
		{
			get
			{
				if (menuManagerPrefab != null) return menuManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().menuManager)
				{
					menuManagerPrefab = AdvGame.GetReferences ().menuManager;
					return menuManagerPrefab;
				}
				return null;
			}
			set
			{
				menuManagerPrefab = value;
			}
		}
		
		
		public static Options options
		{
			get
			{
				if (optionsComponent != null) return optionsComponent;
				else if (persistentEnginePrefab)
				{
					optionsComponent = persistentEnginePrefab.GetComponent <Options>();
					return optionsComponent;
				}
				return null;
			}
		}
		
		
		public static RuntimeInventory runtimeInventory
		{
			get
			{
				if (runtimeInventoryComponent != null) return runtimeInventoryComponent;
				else if (persistentEnginePrefab)
				{
					runtimeInventoryComponent = persistentEnginePrefab.GetComponent <RuntimeInventory>();
					return runtimeInventoryComponent;
				}
				return null;
			}
		}
		
		
		public static RuntimeVariables runtimeVariables
		{
			get
			{
				if (runtimeVariablesComponent != null) return runtimeVariablesComponent;
				else if (persistentEnginePrefab)
				{
					runtimeVariablesComponent = persistentEnginePrefab.GetComponent <RuntimeVariables>();
					return runtimeVariablesComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerMenus playerMenus
		{
			get
			{
				if (playerMenusComponent != null) return playerMenusComponent;
				else if (persistentEnginePrefab)
				{
					playerMenusComponent = persistentEnginePrefab.GetComponent <PlayerMenus>();
					return playerMenusComponent;
				}
				return null;
			}
		}
		
		
		public static StateHandler stateHandler
		{
			get
			{
				if (stateHandlerComponent != null) return stateHandlerComponent;
				else if (persistentEnginePrefab)
				{
					stateHandlerComponent = persistentEnginePrefab.GetComponent <StateHandler>();
					return stateHandlerComponent;
				}
				return null;
			}
		}
		
		
		public static SceneChanger sceneChanger
		{
			get
			{
				if (sceneChangerComponent != null) return sceneChangerComponent;
				else if (persistentEnginePrefab)
				{
					sceneChangerComponent = persistentEnginePrefab.GetComponent <SceneChanger>();
					return sceneChangerComponent;
				}
				return null;
			}
		}
		
		
		public static SaveSystem saveSystem
		{
			get
			{
				if (saveSystemComponent != null) return saveSystemComponent;
				else if (persistentEnginePrefab)
				{
					saveSystemComponent = persistentEnginePrefab.GetComponent <SaveSystem>();
					return saveSystemComponent;
				}
				return null;
			}
		}
		
		
		public static LevelStorage levelStorage
		{
			get
			{
				if (levelStorageComponent != null) return levelStorageComponent;
				else if (persistentEnginePrefab)
				{
					levelStorageComponent = persistentEnginePrefab.GetComponent <LevelStorage>();
					return levelStorageComponent;
				}
				return null;
			}
		}


		public static RuntimeLanguages runtimeLanguages
		{
			get
			{
				if (runtimeLanguagesComponent != null) return runtimeLanguagesComponent;
				else if (persistentEnginePrefab)
				{
					runtimeLanguagesComponent = persistentEnginePrefab.GetComponent <RuntimeLanguages>();
					return runtimeLanguagesComponent;
				}
				return null;
			}
		}


		public static RuntimeDocuments runtimeDocuments
		{
			get
			{
				if (runtimeDocumentsComponent != null) return runtimeDocumentsComponent;
				else if (persistentEnginePrefab)
				{
					runtimeDocumentsComponent = persistentEnginePrefab.GetComponent <RuntimeDocuments>();
					return runtimeDocumentsComponent;
				}
				return null;
			}
		}


		public static RuntimeObjectives runtimeObjectives
		{
			get
			{
				if (runtimeObjectivesComponent != null) return runtimeObjectivesComponent;
				else if (persistentEnginePrefab)
				{
					runtimeObjectivesComponent = persistentEnginePrefab.GetComponent <RuntimeObjectives>();
					return runtimeObjectivesComponent;
				}
				return null;
			}
		}


		public static ActionListAssetManager actionListAssetManager
		{
			get
			{
				if (actionListAssetManagerComponent != null) return actionListAssetManagerComponent;
				else if (persistentEnginePrefab)
				{
					actionListAssetManagerComponent = persistentEnginePrefab.GetComponent <ActionListAssetManager>();
					return actionListAssetManagerComponent;
				}
				return null;
			}
		}
		
		
		public static MenuSystem menuSystem
		{
			get
			{
				if (menuSystemComponent != null) return menuSystemComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					menuSystemComponent = gameEnginePrefab.GetComponent <MenuSystem>();
					return menuSystemComponent;
				}
				return null;
			}
		}
		
		
		public static Dialog dialog
		{
			get
			{
				if (dialogComponent != null) return dialogComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					dialogComponent = gameEnginePrefab.GetComponent <Dialog>();
					return dialogComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerInput playerInput
		{
			get
			{
				if (playerInputComponent != null) return playerInputComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerInputComponent = gameEnginePrefab.GetComponent <PlayerInput>();
					return playerInputComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerInteraction playerInteraction
		{
			get
			{
				if (playerInteractionComponent != null) return playerInteractionComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerInteractionComponent = gameEnginePrefab.GetComponent <PlayerInteraction>();
					return playerInteractionComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerMovement playerMovement
		{
			get
			{
				if (playerMovementComponent != null) return playerMovementComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerMovementComponent = gameEnginePrefab.GetComponent <PlayerMovement>();
					return playerMovementComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerCursor playerCursor
		{
			get
			{
				if (playerCursorComponent != null) return playerCursorComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerCursorComponent = gameEnginePrefab.GetComponent <PlayerCursor>();
					return playerCursorComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerQTE playerQTE
		{
			get
			{
				if (playerQTEComponent != null) return playerQTEComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerQTEComponent = gameEnginePrefab.GetComponent <PlayerQTE>();
					return playerQTEComponent;
				}
				return null;
			}
		}
		
		
		public static SceneSettings sceneSettings
		{
			get
			{
				if (sceneSettingsComponent != null && Application.isPlaying) return sceneSettingsComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					sceneSettingsComponent = gameEnginePrefab.GetComponent <SceneSettings>();
					return sceneSettingsComponent;
				}
				return null;
			}
		}
		
		
		public static NavigationManager navigationManager
		{
			get
			{
				if (navigationManagerComponent != null) return navigationManagerComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					navigationManagerComponent = gameEnginePrefab.GetComponent <NavigationManager>();
					return navigationManagerComponent;
				}
				return null;
			}
		}
		
		
		public static ActionListManager actionListManager
		{
			get
			{
				if (actionListManagerComponent != null) 
				{
					return actionListManagerComponent;
				}
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					actionListManagerComponent = gameEnginePrefab.GetComponent <ActionListManager>();
					return actionListManagerComponent;
				}
				return null;
			}
		}
		
		
		public static LocalVariables localVariables
		{
			get
			{
				if (localVariablesComponent != null) return localVariablesComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					localVariablesComponent = gameEnginePrefab.GetComponent <LocalVariables>();
					return localVariablesComponent;
				}
				return null;
			}
		}
		
		
		public static MenuPreview menuPreview
		{
			get
			{
				if (menuPreviewComponent != null) return menuPreviewComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					menuPreviewComponent = gameEnginePrefab.GetComponent <MenuPreview>();
					return menuPreviewComponent;
				}
				return null;
			}
		}


		public static EventManager eventManager
		{
			get
			{
				if (eventManagerComponent != null) return eventManagerComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					eventManagerComponent = gameEnginePrefab.GetComponent <EventManager>();
					return eventManagerComponent;
				}
				return null;
			}
		}


		public static KickStarter kickStarter
		{
			get
			{
				if (kickStarterComponent != null) return kickStarterComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					kickStarterComponent = gameEnginePrefab.GetComponent <KickStarter>();
					return kickStarterComponent;
				}
				return null;
			}
		}


		public static Music music
		{
			get
			{
				if (KickStarter.stateHandler != null)
				{
					return KickStarter.stateHandler.GetMusicEngine ();
				}
				return null;
			}
		}
		
		
		public static Player player
		{
			get
			{
				if (playerPrefab != null)
				{
					return playerPrefab;
				}

				Player foundPlayer = GameObject.FindObjectOfType <Player>();
				if (foundPlayer != null)
				{
					playerPrefab = foundPlayer.GetComponent <Player>();
					return playerPrefab;
				}
				return null;
			}
		}
		
		
		public static MainCamera mainCamera
		{
			get
			{
				if (mainCameraPrefab != null)
				{
					return mainCameraPrefab;
				}
				else
				{
					MainCamera _mainCamera = (MainCamera) GameObject.FindObjectOfType (typeof (MainCamera));
					if (_mainCamera)
					{
						mainCameraPrefab = _mainCamera;
					}
					return mainCameraPrefab;
				}
			}
			set
			{
				if (value != null)
				{
					mainCameraPrefab = value;
				}
			}
		}


		/**
		 * A cache of Unity's own Camera.main
		 */
		public static Camera CameraMain
		{
			get
			{
				if (KickStarter.settingsManager.cacheCameraMain)
				{
					if (cameraMain == null)
					{
						cameraMain = Camera.main;
					}
					return cameraMain;
				}
				return Camera.main;
			}
			set
			{
				if (value != null)
				{
					cameraMain = value;
				}
			}
		}
		

		/**
		 * <summary>Removes the current Player from the scene, and re-instantiates the correct Player prefab.</summary>
		 * <param name = "ref_player">The new Player prefab to instantiate</param>
		 * <param name = "ID">The ID number to assign the new Player</param>
		 * <param name = "resetReferences">If True, then any references to the Player prefab in other AC scripts will be updated</param>
		 * <param name = "_rotation">The new Player's rotation</param>
		 * <param name = "keepInventory">If True, then the inventory items of the previous player (if there is one) will be transferred onto the new one, replacing any items held by the new one.</param>
		 * <param name = "deleteInstantly">If True, the old Player object will be deleted using DestroyImmediate, which should not be called if this function is invoked by a Physics collision or trigger.</param>
		 * <param name = "replacesOld">If True, the new player replaces the old one (and uses the old player's position and camera)</param>
		 * <param name = "snapCamera">If True, then the Player's active camera will be snapped to</param>
		 */
		public static void ResetPlayer (Player ref_player, int ID, bool resetReferences, Quaternion _rotation, bool keepInventory = false, bool deleteInstantly = false, bool replacesOld = false, bool snapCamera = true)
		{
			// Delete current player(s)
			Player[] playerObjects = GameObject.FindObjectsOfType <Player>();
			if (playerObjects != null && playerObjects.Length > 0)
			{
				foreach (Player playerOb in playerObjects)
				{
					if (playerOb != null)
					{
						playerOb.ReleaseHeldObjects ();

						if (deleteInstantly)
						{
							DestroyImmediate (playerOb.gameObject);
						}
						else
						{
							Renderer[] playerObRenderers = playerOb.gameObject.GetComponentsInChildren <Renderer>();
							foreach (Renderer renderer in playerObRenderers)
							{
								renderer.enabled = false;
							}

							Collider[] playerObColliders = playerOb.gameObject.GetComponentsInChildren <Collider>();
							foreach (Collider collider in playerObColliders)
							{
								if (collider is CharacterController) continue;
								collider.isTrigger = true;
							}
							KickStarter.sceneChanger.ScheduleForDeletion (playerOb.gameObject);
						}
					}
				}
			}

			// Load new player
			if (ref_player)
			{
				SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;

				Player newPlayer = (Player) Instantiate (ref_player, Vector3.zero, _rotation);

				newPlayer.TransformRotation = _rotation;

				newPlayer.ID = ID;
				newPlayer.name = ref_player.name;
				playerPrefab = newPlayer;

				DontDestroyOnLoad (newPlayer);
				
				if (KickStarter.runtimeInventory)
				{
					KickStarter.runtimeInventory.SetNull ();
					KickStarter.runtimeInventory.RemoveRecipes ();
					KickStarter.runtimeObjectives.ClearUniqueToPlayer ();

					// Clear inventory
					if (settingsManager.playerSwitching == PlayerSwitching.Allow && !settingsManager.shareInventory)
					{
						if (!keepInventory)
						{
							KickStarter.runtimeInventory.localItems.Clear ();
							KickStarter.runtimeDocuments.ClearCollection ();
						}
					}
					
					if (KickStarter.saveSystem != null && KickStarter.saveSystem.DoesPlayerDataExist (ID, false))
					{
						bool loadNewInventory = !settingsManager.shareInventory;
						bool loadNewCamera = !replacesOld;

						if (settingsManager.playerSwitching == PlayerSwitching.DoNotAllow || (!settingsManager.shareInventory && keepInventory))
						{
							loadNewInventory = false;
						}
						saveSystem.AssignPlayerData (ID, loadNewInventory, loadNewCamera, snapCamera);
					}
					
					// Menus
					foreach (AC.Menu menu in PlayerMenus.GetMenus ())
					{
						//menu.Recalculate ();
						foreach (MenuElement element in menu.elements)
						{
							if (element is MenuInventoryBox)
							{
								MenuInventoryBox invBox = (MenuInventoryBox) element;
								invBox.ResetOffset ();
							}
						}
					}
				}

				newPlayer.Initialise ();

				if (KickStarter.eventManager != null) KickStarter.eventManager.Call_OnSetPlayer (newPlayer);
			}

			// Reset player references
			if (resetReferences)
			{
				KickStarter.playerMovement.AssignFPCamera ();
				KickStarter.stateHandler.IgnoreNavMeshCollisions ();
				KickStarter.stateHandler.UpdateAllMaxVolumes ();
				_Camera[] cameras = FindObjectsOfType (typeof (_Camera)) as _Camera[];
				if (cameras != null)
				{
					foreach (_Camera camera in cameras)
					{
						camera.ResetTarget();
					}
				}
			}
		}



		public void OnAwake ()
		{
			ClearVariables ();

			// Test for key imports
			References references = (References) Resources.Load (Resource.references);
			if (references)
			{
				SceneManager sceneManager = AdvGame.GetReferences ().sceneManager;
				SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
				ActionsManager actionsManager = AdvGame.GetReferences ().actionsManager;
				InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
				VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;
				SpeechManager speechManager = AdvGame.GetReferences ().speechManager;
				CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
				MenuManager menuManager = AdvGame.GetReferences ().menuManager;
				
				if (sceneManager == null)
				{
					ACDebug.LogError ("No Scene Manager found - please set one using the main Adventure Creator window");
				}
				
				if (settingsManager == null)
				{
					ACDebug.LogError ("No Settings Manager found - please set one using the main Adventure Creator window");
				}
				else
				{
					if (settingsManager.IsInLoadingScene ())
					{
						ACDebug.Log ("Bypassing regular AC startup because the current scene is the 'Loading' scene.");
						SetPersistentEngine ();
						return;
					}

					// Unity 5.3 has a bug whereby a modified Player prefab is placed in the scene when editing, but not visible
					// This causes the Player to not load properly, so try to detect this remnant and delete it!
					Player existingPlayer = GameObject.FindObjectOfType <Player> ();
					if (existingPlayer != null)
					{
						if (settingsManager.GetDefaultPlayer () != null && existingPlayer.name == (settingsManager.GetDefaultPlayer ().name + "(Clone)"))
						{
							DestroyImmediate (existingPlayer);
							ACDebug.LogWarning ("Player clone found in scene - this may have been hidden by a Unity bug, and has been destroyed.");
						}
					}

					existingPlayer = GameObject.FindObjectOfType <Player> ();
					if (existingPlayer == null)
					{
						KickStarter.ResetPlayer (settingsManager.GetDefaultPlayer (), settingsManager.GetDefaultPlayerID (), false, Quaternion.identity, false, true);
					}
					else
					{
						KickStarter.playerPrefab = existingPlayer;
						SetPersistentEngine ();
			
						if (sceneChanger == null || sceneChanger.GetPlayerOnTransition () == null)
						{
							// New local player after another local player scene
							if (KickStarter.playerPrefab != null)
							{
								KickStarter.playerPrefab.Initialise ();
								SetLocalPlayerID (KickStarter.playerPrefab);
							}
						}
						
						AssignLocalPlayer ();
					}

					if (KickStarter.player == null && KickStarter.settingsManager.movementMethod != MovementMethod.None)
					{
						ACDebug.LogWarning ("No Player found - this can be assigned in the Settings Manager.");
					}
				}

				if (actionsManager == null)
				{
					ACDebug.LogError ("No Actions Manager found - please set one using the main Adventure Creator window");
				}
				
				if (inventoryManager == null)
				{
					ACDebug.LogError ("No Inventory Manager found - please set one using the main Adventure Creator window");
				}
				
				if (variablesManager == null)
				{
					ACDebug.LogError ("No Variables Manager found - please set one using the main Adventure Creator window");
				}
				
				if (speechManager == null)
				{
					ACDebug.LogError ("No Speech Manager found - please set one using the main Adventure Creator window");
				}
				
				if (cursorManager == null)
				{
					ACDebug.LogError ("No Cursor Manager found - please set one using the main Adventure Creator window");
				}
				
				if (menuManager == null)
				{
					ACDebug.LogError ("No Menu Manager found - please set one using the main Adventure Creator window");
				}
			}
			else
			{
				ACDebug.LogError ("No References object found. Please set one using the main Adventure Creator window");
			}
			
			SetPersistentEngine ();

			#if UNITY_EDITOR
				
			if (persistentEnginePrefab == null)
			{
				ACDebug.LogError ("No PersistentEngine prefab found - please place one in the Resources directory, and tag it as PersistentEngine");
			}
			else
			{
				if (persistentEnginePrefab.GetComponent <Options>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no Options component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <RuntimeInventory>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no RuntimeInventory component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <RuntimeVariables>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no RuntimeVariables component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <PlayerMenus>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no PlayerMenus component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <RuntimeObjectives>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no RuntimeObjectives component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <StateHandler>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no StateHandler component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <SceneChanger>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no SceneChanger component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <SaveSystem>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no SaveSystem component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <LevelStorage>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no LevelStorage component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <RuntimeLanguages>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no RuntimeLanguages component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <RuntimeDocuments>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no RuntimeDocuments component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
				if (persistentEnginePrefab.GetComponent <ActionListAssetManager>() == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no ActionListAssetManager component attached. It can be found in /Assets/AdventureCreator/Resources", persistentEnginePrefab);
				}
			}
			
			if (this.GetComponent <MenuSystem>() == null)
			{
				ACDebug.LogError (this.name + " has no MenuSystem component attached.", this);
			}
			if (this.GetComponent <Dialog>() == null)
			{
				ACDebug.LogError (this.name + " has no Dialog component attached.", this);
			}
			if (this.GetComponent <PlayerInput>() == null)
			{
				ACDebug.LogError (this.name + " has no PlayerInput component attached.", this);
			}
			if (this.GetComponent <PlayerInteraction>() == null)
			{
				ACDebug.LogError (this.name + " has no PlayerInteraction component attached.", this);
			}
			if (this.GetComponent <PlayerMovement>() == null)
			{
				ACDebug.LogError (this.name + " has no PlayerMovement component attached.", this);
			}
			if (this.GetComponent <PlayerCursor>() == null)
			{
				ACDebug.LogError (this.name + " has no PlayerCursor component attached.", this);
			}
			if (this.GetComponent <PlayerQTE>() == null)
			{
				ACDebug.LogError (this.name + " has no PlayerQTE component attached.", this);
			}
			if (this.GetComponent <SceneSettings>() == null)
			{
				ACDebug.LogError (this.name + " has no SceneSettings component attached.", this);
			}
			else
			{
				if (this.GetComponent <SceneSettings>().navigationMethod == AC_NavigationMethod.meshCollider && this.GetComponent <SceneSettings>().navMesh == null)
				{
					// No NavMesh, are there Characters in the scene?
					AC.Char[] allChars = GameObject.FindObjectsOfType (typeof(AC.Char)) as AC.Char[];
					if (allChars.Length > 0)
					{
						ACDebug.LogWarning ("No NavMesh set. Characters will not be able to PathFind until one is defined - please choose one using the Scene Manager.");
					}
				}
				
				if (this.GetComponent <SceneSettings>().defaultPlayerStart == null)
				{
					if (AdvGame.GetReferences ().settingsManager == null || AdvGame.GetReferences ().settingsManager.GetDefaultPlayer () != null)
					{
						ACDebug.LogWarning ("No default PlayerStart set.  The game may not be able to begin if one is not defined - please choose one using the Scene Manager.");
					}
				}
			}
			if (this.GetComponent <NavigationManager>() == null)
			{
				ACDebug.LogError (this.name + " has no NavigationManager component attached.", this);
			}
			if (this.GetComponent <ActionListManager>() == null)
			{
				ACDebug.LogError (this.name + " has no ActionListManager component attached.", this);
			}
			if (this.GetComponent <EventManager>() == null)
			{
				ACDebug.LogError (this.name + " has no EventManager component attached.", this);
			}
			#endif

			if (KickStarter.player != null)
			{
				if (KickStarter.saveSystem != null &&
					KickStarter.saveSystem.loadingGame == LoadingGame.JustSwitchingPlayer &&
					KickStarter.settingsManager != null &&
					KickStarter.settingsManager.useLoadingScreen)
				 {
				 	// Special case: As player is moved out of way when in a loading screen, need to re-load position data once in new scene
					saveSystem.AssignPlayerAllData (KickStarter.player);
				 }
				 else
				 {
					saveSystem.AssignPlayerAnimData (KickStarter.player);
				 }
			}
		}


		private static void SetLocalPlayerID (Player player)
		{
			player.ID = (-2 - UnityVersionHandler.GetCurrentSceneNumber ()); // Always unique to the scene

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				ACDebug.LogWarning ("The use of 'in-scene' local Players is not recommended when Player-switching is enabled - consider using the 'Player: Switch' Action to change Player instead.");
			}

			if (KickStarter.saveSystem != null && KickStarter.saveSystem.DoesPlayerDataExist (player.ID))
			{
				KickStarter.saveSystem.AssignPlayerAnimData (player);
			}
		}

		

		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			Player playerOb = GameObject.FindObjectOfType <Player> ();
			if (playerOb != null)
			{
				KickStarter.playerPrefab = playerOb;
			}
		}
		
		
		/**
		 * Turns Adventure Creator off.
		 */
		public static void TurnOnAC ()
		{
			if (KickStarter.stateHandler != null && KickStarter.actionListManager != null)
			{
				KickStarter.stateHandler.SetACState (true);
				ACDebug.Log ("Adventure Creator has been turned on.");
			}
			else
			{
				ACDebug.LogWarning ("Cannot turn AC on because the PersistentEngine and GameEngine are not present!");
			}
		}
		
		
		/**
		 * Turns Adventure Creator on.
		 */
		public static void TurnOffAC ()
		{
			if (KickStarter.actionListManager != null)
			{
				KickStarter.actionListManager.KillAllLists ();
				KickStarter.dialog.KillDialog (true, true);
				
				Moveable[] moveables = FindObjectsOfType (typeof (Moveable)) as Moveable[];
				foreach (Moveable moveable in moveables)
				{
					moveable.StopMoving ();
				}
				
				Char[] chars = FindObjectsOfType (typeof (Char)) as Char[];
				foreach (Char _char in chars)
				{
					_char.EndPath ();
				}
				
				if (KickStarter.stateHandler)
				{
					KickStarter.stateHandler.SetACState (false);
					ACDebug.Log ("Adventure Creator has been turned off.");
				}
			}
			else
			{
				ACDebug.LogWarning ("Cannot turn AC off because it is not on!");
			}
		}


		private static void AssignLocalPlayer ()
		{
			if (sceneChanger != null && sceneChanger.GetPlayerOnTransition () != null)
			{
				// Replace "prefab" player with a local one if one exists
				Player[] playerObs = GameObject.FindObjectsOfType <Player> ();
				foreach (Player playerOb in playerObs)
				{
					if (sceneChanger.GetPlayerOnTransition () != playerOb)
					{
						KickStarter.sceneChanger.DestroyOldPlayer ();
						KickStarter.playerPrefab = playerOb;
						SetLocalPlayerID (KickStarter.playerPrefab);

						break;
					}
				}
			}
		}


		/**
		 * <summary>Unsets the values of all script variables, so that they can be re-assigned to the correct scene if multiple scenes are open.</summary>
		 */
		public void ClearVariables ()
		{
			playerPrefab = null;
			mainCameraPrefab = null;
			persistentEnginePrefab = null;
			gameEnginePrefab = null;

			// Managers
			sceneManagerPrefab = null;
			settingsManagerPrefab = null;
			actionsManagerPrefab = null;
			variablesManagerPrefab = null;
			inventoryManagerPrefab = null;
			speechManagerPrefab = null;
			cursorManagerPrefab = null;
			menuManagerPrefab = null;

			// PersistentEngine components
			optionsComponent = null;
			runtimeInventoryComponent = null;
			runtimeVariablesComponent = null;
			playerMenusComponent = null;
			stateHandlerComponent = null;
			sceneChangerComponent = null;
			saveSystemComponent = null;
			levelStorageComponent = null;
			runtimeLanguagesComponent = null;
			actionListAssetManagerComponent = null;

			// GameEngine components
			menuSystemComponent = null;
			dialogComponent = null;
			playerInputComponent = null;
			playerInteractionComponent = null;
			playerMovementComponent = null;
			playerCursorComponent = null;
			playerQTEComponent = null;
			sceneSettingsComponent = null;
			navigationManagerComponent = null;
			actionListManagerComponent = null;
			localVariablesComponent = null;
			menuPreviewComponent = null;
			eventManagerComponent = null;

			SetGameEngine ();
		}

	}
	
}