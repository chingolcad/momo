/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionPlayerSwitch.cs"
 * 
 *	This action causes a different Player prefab
 *	to be controlled.  Note that only one Player prefab
 *  can exist in a scene at any one time - for two player
 *  "characters" to be present, one must be a swapped-out
 * 	NPC instead.
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
	public class ActionPlayerSwitch : Action
	{
		
		public int playerID;
		public int playerIDParameterID = -1;

		public NewPlayerPosition newPlayerPosition = NewPlayerPosition.ReplaceNPC;
		public OldPlayer oldPlayer = OldPlayer.RemoveFromScene;
		
		public bool restorePreviousData = false;
		public bool keepInventory = false;
		
		public ChooseSceneBy chooseNewSceneBy = ChooseSceneBy.Number;
		public int newPlayerScene;
		public string newPlayerSceneName;
		
		public int oldPlayerNPC_ID;
		public NPC oldPlayerNPC;
		protected NPC runtimeOldPlayerNPC;
		
		public int newPlayerNPC_ID;
		public NPC newPlayerNPC;
		protected NPC runtimeNewPlayerNPC;
		
		public int newPlayerMarker_ID;
		public Marker newPlayerMarker;
		protected Marker runtimeNewPlayerMarker;

		public bool alwaysSnapCamera = true;

		#if UNITY_EDITOR
		private SettingsManager settingsManager;
		#endif


		public ActionPlayerSwitch ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Player;
			title = "Switch";
			description = "Swaps out the Player prefab mid-game. If the new prefab has been used before, you can restore that prefab's position data – otherwise you can set the position or scene of the new player. This Action only applies to games for which 'Player switching' has been allowed in the Settings Manager.";
		}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeNewPlayerNPC = AssignFile <NPC> (newPlayerNPC_ID, newPlayerNPC);
			runtimeNewPlayerMarker = AssignFile <Marker> (newPlayerMarker_ID, newPlayerMarker);

			if (oldPlayer == OldPlayer.ReplaceWithAssociatedNPC)
			{
				if (KickStarter.player != null)
				{
					if (KickStarter.player.associatedNPCPrefab != null)
					{
						ConstantID associatedNPCPrefabID = KickStarter.player.associatedNPCPrefab.GetComponent <ConstantID>();
						if (associatedNPCPrefabID != null)
						{
							oldPlayerNPC_ID = associatedNPCPrefabID.constantID;
						}
					}
				}
			}

			runtimeOldPlayerNPC = AssignFile <NPC> (oldPlayerNPC_ID, oldPlayerNPC);
			playerID = AssignInteger (parameters, playerIDParameterID, playerID);
		}
		
		
		public override float Run ()
		{
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				PlayerPrefab newPlayerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);

				if (newPlayerPrefab != null)
				{
					if (KickStarter.player != null && KickStarter.player.ID == playerID)
					{
						Log ("Cannot switch player - already controlling the desired prefab.");
						return 0f;
					}
					
					if (newPlayerPrefab.playerOb != null)
					{
						KickStarter.saveSystem.SaveCurrentPlayerData ();
						
						Vector3 oldPlayerPosition = Vector3.zero;
						Quaternion oldPlayerRotation = new Quaternion ();
						Vector3 oldPlayerScale = Vector3.one;

						PlayerData oldPlayerData = new PlayerData ();
						NPCData oldNPCData = new NPCData ();
						bool recordedOldPlayerData = false;
						bool recordedOldNPCData = false;

						if (KickStarter.player != null)
						{
							oldPlayerPosition = KickStarter.player.transform.position;
							oldPlayerRotation = KickStarter.player.TransformRotation;
							oldPlayerScale = KickStarter.player.transform.localScale;

							oldPlayerData = KickStarter.player.SavePlayerData (oldPlayerData);
							recordedOldPlayerData = true;
						}

						if (newPlayerPosition != NewPlayerPosition.ReplaceCurrentPlayer)
						{
							if (oldPlayer == OldPlayer.ReplaceWithAssociatedNPC &&
								(runtimeOldPlayerNPC == null || !runtimeOldPlayerNPC.gameObject.activeInHierarchy) &&
								KickStarter.player.associatedNPCPrefab != null)
							{
								GameObject newObject = (GameObject) Instantiate (KickStarter.player.associatedNPCPrefab.gameObject);
								newObject.name = KickStarter.player.associatedNPCPrefab.gameObject.name;
								runtimeOldPlayerNPC = newObject.GetComponent <NPC>();
							}

							if ((oldPlayer == OldPlayer.ReplaceWithNPC || oldPlayer == OldPlayer.ReplaceWithAssociatedNPC) &&
								runtimeOldPlayerNPC != null && runtimeOldPlayerNPC.gameObject.activeInHierarchy)
							{
								runtimeOldPlayerNPC.Teleport (oldPlayerPosition);
								runtimeOldPlayerNPC.SetRotation (oldPlayerRotation);
								runtimeOldPlayerNPC.transform.localScale = oldPlayerScale;

								if (recordedOldPlayerData)
								{
									ApplyRenderData (runtimeOldPlayerNPC, oldPlayerData);
								}

								// Force the rotation / sprite child to update
								runtimeOldPlayerNPC._Update ();
							}
						}

						if (runtimeNewPlayerNPC == null || newPlayerPosition == NewPlayerPosition.ReplaceAssociatedNPC)
						{
							// Try to find from associated NPC prefab

							if (newPlayerPrefab.playerOb.associatedNPCPrefab != null)
							{
								ConstantID prefabID = newPlayerPrefab.playerOb.associatedNPCPrefab.GetComponent <ConstantID>();
								if (prefabID != null && prefabID.constantID != 0)
								{
									newPlayerNPC_ID = prefabID.constantID;
									runtimeNewPlayerNPC = AssignFile <NPC> (prefabID.constantID, null);
								}
							}
						}

						Quaternion newRotation = Quaternion.identity;
						if (newPlayerPosition == NewPlayerPosition.ReplaceCurrentPlayer)
						{
							newRotation = oldPlayerRotation;
						}
						else if (newPlayerPosition == NewPlayerPosition.ReplaceNPC && runtimeNewPlayerNPC != null)
						{
							newRotation = runtimeNewPlayerNPC.TransformRotation;
						}
						else if (newPlayerPosition == NewPlayerPosition.AppearAtMarker && runtimeNewPlayerMarker != null)
						{
							newRotation = runtimeNewPlayerMarker.transform.rotation;
						}

						if (runtimeNewPlayerNPC != null)
						{
							oldNPCData = runtimeNewPlayerNPC.SaveData (oldNPCData);
						}

						bool replacesOldPlayer = newPlayerPosition == NewPlayerPosition.ReplaceCurrentPlayer &&
												 (!restorePreviousData || !KickStarter.saveSystem.DoesPlayerDataExist (playerID, true));
						KickStarter.ResetPlayer (newPlayerPrefab.playerOb, playerID, true, newRotation, keepInventory, false, replacesOldPlayer, alwaysSnapCamera);
						Player newPlayer = KickStarter.player;
						PlayerMenus.ResetInventoryBoxes ();

						if (replacesOldPlayer && recordedOldPlayerData)
						{
							ApplyRenderData (newPlayer, oldPlayerData);
						}

						if (restorePreviousData && KickStarter.saveSystem.DoesPlayerDataExist (playerID, true))
						{
							int sceneToLoad = KickStarter.saveSystem.GetPlayerScene (playerID);
							if (sceneToLoad >= 0 && sceneToLoad != UnityVersionHandler.GetCurrentSceneNumber ())
							{
								KickStarter.saveSystem.loadingGame = LoadingGame.JustSwitchingPlayer;
								KickStarter.sceneChanger.ChangeScene (new SceneInfo (string.Empty, sceneToLoad), true, false);
							}
							else
							{
								// Same scene
								if (runtimeNewPlayerNPC != null)
								{
									newPlayer.RepositionToTransform (runtimeNewPlayerNPC.transform);
									runtimeNewPlayerNPC.HideFromView (newPlayer);
								}	
							}
						}
						else
						{
							// No data to restore

							if (newPlayerPosition == NewPlayerPosition.ReplaceCurrentPlayer)
							{
								newPlayer.Teleport (oldPlayerPosition);
								newPlayer.SetRotation (oldPlayerRotation);
								newPlayer.transform.localScale = oldPlayerScale;
							}
							else if (newPlayerPosition == NewPlayerPosition.ReplaceNPC || newPlayerPosition == NewPlayerPosition.ReplaceAssociatedNPC)
							{
								if (runtimeNewPlayerNPC != null)
								{
									newPlayer.RepositionToTransform (runtimeNewPlayerNPC.transform);
									runtimeNewPlayerNPC.HideFromView (newPlayer);

									if (recordedOldNPCData)
									{
										ApplyRenderData (newPlayer, oldNPCData);
									}
								}
							}
							else if (newPlayerPosition == NewPlayerPosition.AppearAtMarker)
							{
								if (runtimeNewPlayerMarker)
								{
									newPlayer.RepositionToTransform (runtimeNewPlayerMarker.transform);
								}
							}
							else if (newPlayerPosition == NewPlayerPosition.AppearInOtherScene)
							{
								if (chooseNewSceneBy == ChooseSceneBy.Name && newPlayerSceneName == UnityVersionHandler.GetCurrentSceneName () ||
									(chooseNewSceneBy == ChooseSceneBy.Number && newPlayerScene == UnityVersionHandler.GetCurrentSceneNumber ()))
								{
									// Already in correct scene
									if (runtimeNewPlayerNPC && runtimeNewPlayerNPC.gameObject.activeInHierarchy)
									{
										newPlayer.RepositionToTransform (runtimeNewPlayerNPC.transform);
										runtimeNewPlayerNPC.HideFromView (newPlayer);
									}
								}
								else
								{
									KickStarter.sceneChanger.ChangeScene (new SceneInfo (chooseNewSceneBy, newPlayerSceneName, newPlayerScene), true, false, true);
								}
							}
						}
						
						if (KickStarter.mainCamera.attachedCamera && alwaysSnapCamera)
						{
							KickStarter.mainCamera.attachedCamera.MoveCameraInstant ();
						}
						
						AssetLoader.UnloadAssets ();
					}
					else
					{
						LogWarning ("Cannot switch player - no player prefabs is defined.");
					}
				}
			}
			
			return 0f;
		}


		protected void ApplyRenderData (Char character, PlayerData playerData)
		{
			character.lockDirection = playerData.playerLockDirection;
			character.lockScale = playerData.playerLockScale;
			if (character.spriteChild && character.spriteChild.GetComponent <FollowSortingMap>())
			{
				character.spriteChild.GetComponent <FollowSortingMap>().lockSorting = playerData.playerLockSorting;
			}
			else if (character.GetComponent <FollowSortingMap>())
			{
				character.GetComponent <FollowSortingMap>().lockSorting = playerData.playerLockSorting;
			}
			else
			{
				character.ReleaseSorting ();
			}
			
			if (playerData.playerLockDirection)
			{
				character.spriteDirection = playerData.playerSpriteDirection;
			}
			if (playerData.playerLockScale)
			{
				character.spriteScale = playerData.playerSpriteScale;
			}
			if (playerData.playerLockSorting)
			{
				if (character.spriteChild && character.spriteChild.GetComponent <Renderer>())
				{
					character.spriteChild.GetComponent <Renderer>().sortingOrder = playerData.playerSortingOrder;
					character.spriteChild.GetComponent <Renderer>().sortingLayerName = playerData.playerSortingLayer;
				}
				else if (character.GetComponent <Renderer>())
				{
					character.GetComponent <Renderer>().sortingOrder = playerData.playerSortingOrder;
					character.GetComponent <Renderer>().sortingLayerName = playerData.playerSortingLayer;
				}
			}

			if (character.GetComponentsInChildren <FollowSortingMap>() != null)
			{
				FollowSortingMap[] followSortingMaps = character.GetComponentsInChildren <FollowSortingMap>();
				SortingMap customSortingMap = Serializer.returnComponent <SortingMap> (playerData.customSortingMapID);
				
				foreach (FollowSortingMap followSortingMap in followSortingMaps)
				{
					followSortingMap.followSortingMap = playerData.followSortingMap;
					if (!playerData.followSortingMap && customSortingMap != null)
					{
						followSortingMap.SetSortingMap (customSortingMap);
					}
					else
					{
						followSortingMap.SetSortingMap (KickStarter.sceneSettings.sortingMap);
					}
				}
			}
		}


		protected void ApplyRenderData (Char character, NPCData npcData)
		{
			character.lockDirection = npcData.lockDirection;
			character.lockScale = npcData.lockScale;
			if (character.spriteChild && character.spriteChild.GetComponent <FollowSortingMap>())
			{
				character.spriteChild.GetComponent <FollowSortingMap>().lockSorting = npcData.lockSorting;
			}
			else if (character.GetComponent <FollowSortingMap>())
			{
				character.GetComponent <FollowSortingMap>().lockSorting = npcData.lockSorting;
			}
			else
			{
				character.ReleaseSorting ();
			}
			
			if (npcData.lockDirection)
			{
				character.spriteDirection = npcData.spriteDirection;
			}
			if (npcData.lockScale)
			{
				character.spriteScale = npcData.spriteScale;
			}
			if (npcData.lockSorting)
			{
				if (character.spriteChild && character.spriteChild.GetComponent <Renderer>())
				{
					character.spriteChild.GetComponent <Renderer>().sortingOrder = npcData.sortingOrder;
					character.spriteChild.GetComponent <Renderer>().sortingLayerName = npcData.sortingLayer;
				}
				else if (character.GetComponent <Renderer>())
				{
					character.GetComponent <Renderer>().sortingOrder = npcData.sortingOrder;
					character.GetComponent <Renderer>().sortingLayerName = npcData.sortingLayer;
				}
			}

			if (character.GetComponentsInChildren <FollowSortingMap>() != null)
			{
				FollowSortingMap[] followSortingMaps = character.GetComponentsInChildren <FollowSortingMap>();
				SortingMap customSortingMap = Serializer.returnComponent <SortingMap> (npcData.customSortingMapID);

				foreach (FollowSortingMap followSortingMap in followSortingMaps)
				{
					followSortingMap.followSortingMap = npcData.followSortingMap;
					if (!npcData.followSortingMap && customSortingMap != null)
					{
						followSortingMap.SetSortingMap (customSortingMap);
					}
					else
					{
						followSortingMap.SetSortingMap (KickStarter.sceneSettings.sortingMap);
					}
				}
			}
		}

		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (settingsManager == null)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
			
			if (settingsManager == null)
			{
				return;
			}
			
			if (settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				EditorGUILayout.HelpBox ("This Action requires Player Switching to be allowed, as set in the Settings Manager.", MessageType.Info);
				return;
			}
			
			if (settingsManager.players.Count > 0)
			{
				playerIDParameterID = Action.ChooseParameterGUI ("New Player ID:", parameters, playerIDParameterID, ParameterType.Integer);
				if (playerIDParameterID == -1)
				{
					// Create a string List of the field's names (for the PopUp box)
					List<string> labelList = new List<string>();
					
					int i = 0;
					int playerNumber = -1;

					foreach (PlayerPrefab playerPrefab in settingsManager.players)
					{
						if (playerPrefab.playerOb != null)
						{
							labelList.Add (playerPrefab.playerOb.name);
						}
						else
						{
							labelList.Add ("(Undefined prefab)");
						}
						
						// If a player has been removed, make sure selected player is still valid
						if (playerPrefab.ID == playerID)
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
						playerID = 0;
					}
				
					playerNumber = EditorGUILayout.Popup ("New Player:", playerNumber, labelList.ToArray());
					playerID = settingsManager.players[playerNumber].ID;
				}

				if (AdvGame.GetReferences ().settingsManager == null || !AdvGame.GetReferences ().settingsManager.shareInventory)
				{
					keepInventory = EditorGUILayout.Toggle ("Transfer inventory?", keepInventory);
				}
				restorePreviousData = EditorGUILayout.Toggle ("Restore position?", restorePreviousData);
				if (restorePreviousData)
				{
					EditorGUILayout.BeginVertical (CustomStyles.thinBox);
					EditorGUILayout.LabelField ("If first time in game:", EditorStyles.boldLabel);
				}
				
				newPlayerPosition = (NewPlayerPosition) EditorGUILayout.EnumPopup ("New Player position:", newPlayerPosition);
				
				if (newPlayerPosition == NewPlayerPosition.ReplaceNPC)
				{
					newPlayerNPC = (NPC) EditorGUILayout.ObjectField ("NPC to be replaced:", newPlayerNPC, typeof (NPC), true);
					
					newPlayerNPC_ID = FieldToID <NPC> (newPlayerNPC, newPlayerNPC_ID);
					newPlayerNPC = IDToField <NPC> (newPlayerNPC, newPlayerNPC_ID, false);
				}
				else if (newPlayerPosition == NewPlayerPosition.AppearAtMarker)
				{
					newPlayerMarker = (Marker) EditorGUILayout.ObjectField ("Marker to appear at:", newPlayerMarker, typeof (Marker), true);
					
					newPlayerMarker_ID = FieldToID <Marker> (newPlayerMarker, newPlayerMarker_ID);
					newPlayerMarker = IDToField <Marker> (newPlayerMarker, newPlayerMarker_ID, false);
				}
				else if (newPlayerPosition == NewPlayerPosition.AppearInOtherScene)
				{
					chooseNewSceneBy = (ChooseSceneBy) EditorGUILayout.EnumPopup ("Choose scene by:", chooseNewSceneBy);
					if (chooseNewSceneBy == ChooseSceneBy.Name)
					{
						newPlayerSceneName = EditorGUILayout.TextField ("Scene to appear in:", newPlayerSceneName);
					}
					else
					{
						newPlayerScene = EditorGUILayout.IntField ("Scene to appear in:", newPlayerScene);
					}

					newPlayerNPC = (NPC) EditorGUILayout.ObjectField ("NPC to be replaced:", newPlayerNPC, typeof (NPC), true);
					
					newPlayerNPC_ID = FieldToID <NPC> (newPlayerNPC, newPlayerNPC_ID);
					newPlayerNPC = IDToField <NPC> (newPlayerNPC, newPlayerNPC_ID, false);

					EditorGUILayout.HelpBox ("If the Player has an Associated NPC defined, it will be used if none is defined here.", MessageType.Info);
				}
				else if (newPlayerPosition == NewPlayerPosition.ReplaceAssociatedNPC)
				{
					EditorGUILayout.HelpBox ("A Player's 'Associated NPC' is defined in the Player Inspector.", MessageType.Info);
				}

				if (restorePreviousData)
				{
					EditorGUILayout.EndVertical ();
				}

				if (newPlayerPosition == NewPlayerPosition.ReplaceNPC ||
					newPlayerPosition == NewPlayerPosition.AppearAtMarker ||
					newPlayerPosition == NewPlayerPosition.AppearInOtherScene ||
					newPlayerPosition == NewPlayerPosition.ReplaceAssociatedNPC)
				{
					EditorGUILayout.Space ();
					oldPlayer = (OldPlayer) EditorGUILayout.EnumPopup ("Old Player:", oldPlayer);
					
					if (oldPlayer == OldPlayer.ReplaceWithNPC)
					{
						oldPlayerNPC = (NPC) EditorGUILayout.ObjectField ("NPC to replace old Player:", oldPlayerNPC, typeof (NPC), true);
						
						oldPlayerNPC_ID = FieldToID <NPC> (oldPlayerNPC, oldPlayerNPC_ID);
						oldPlayerNPC = IDToField <NPC> (oldPlayerNPC, oldPlayerNPC_ID, false);

						EditorGUILayout.HelpBox ("This NPC must be already be present in the scene - either within the scene file itself, or spawned at runtime with the 'Object: Add or remove' Action.", MessageType.Info);
					}
					else if (oldPlayer == OldPlayer.ReplaceWithAssociatedNPC)
					{
						EditorGUILayout.HelpBox ("A Player's 'Associated NPC' is defined in the Player Inspector.", MessageType.Info);
					}
				}
			}
			else
			{
				EditorGUILayout.LabelField ("No players exist!");
				playerID = -1;
			}

			alwaysSnapCamera = EditorGUILayout.Toggle ("Snap camera if shared?", alwaysSnapCamera);
			
			EditorGUILayout.Space ();
			
			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberNPC> (oldPlayerNPC);
				AddSaveScript <RememberNPC> (newPlayerNPC);
			}

			AssignConstantID <NPC> (oldPlayerNPC, oldPlayerNPC_ID, -1);
			AssignConstantID <NPC> (newPlayerNPC, newPlayerNPC_ID, -1);
			AssignConstantID <Marker> (newPlayerMarker, newPlayerMarker_ID, -1);
		}

		
		public override string SetLabel ()
		{
			if (playerIDParameterID >= 0) return string.Empty;

			if (settingsManager == null)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
			
			if (settingsManager != null && settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				PlayerPrefab newPlayerPrefab = settingsManager.GetPlayerPrefab (playerID);
				if (newPlayerPrefab != null)
				{
					if (newPlayerPrefab.playerOb != null)
					{
						return newPlayerPrefab.playerOb.name;
					}
					else
					{
						return "Undefined prefab";
					}
				}
			}
			
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (newPlayerPosition == NewPlayerPosition.AppearAtMarker)
			{
				if (newPlayerMarker != null && newPlayerMarker.gameObject == gameObject) return true;
				if (newPlayerMarker_ID == id) return true;
			}
			if (newPlayerPosition == NewPlayerPosition.AppearInOtherScene || newPlayerPosition == NewPlayerPosition.ReplaceNPC)
			{
				if (newPlayerNPC != null && newPlayerNPC.gameObject == gameObject) return true;
				if (newPlayerNPC_ID == id) return true;
			}
			if (newPlayerPosition == NewPlayerPosition.ReplaceNPC ||
					newPlayerPosition == NewPlayerPosition.AppearAtMarker ||
					newPlayerPosition == NewPlayerPosition.AppearInOtherScene ||
					newPlayerPosition == NewPlayerPosition.ReplaceAssociatedNPC)
			{
				if (oldPlayer == OldPlayer.ReplaceWithNPC)
				{
					if (oldPlayerNPC != null && oldPlayerNPC.gameObject == gameObject) return true;
					if (oldPlayerNPC_ID == id) return true;
				}
			}
			return false;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Player: Switch' Action, set to make the old Player become an NPC</summary>
		 * <param name = "newPlayerID">The ID number of the Player to switch to</param>
		 * <param name = "transferInventory">If True, the previous Player's inventory will be transferred to the new one</param>
		 * <param name = "newSceneInfo">A data container for information about the new Player's scene. If null, the new Player will replace his associated NPC in the current scene</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPlayerSwitch CreateNew (int newPlayerID, bool transferInventory = false, SceneInfo newSceneInfo = null)
		{
			ActionPlayerSwitch newAction = (ActionPlayerSwitch) CreateInstance <ActionPlayerSwitch>();
			newAction.playerID = newPlayerID;
			newAction.restorePreviousData = true;
			newAction.keepInventory = transferInventory;
			newAction.newPlayerPosition = (newSceneInfo != null) ? NewPlayerPosition.AppearInOtherScene : NewPlayerPosition.ReplaceAssociatedNPC;
			newAction.oldPlayer = OldPlayer.ReplaceWithAssociatedNPC;
			if (newSceneInfo != null)
			{
				newAction.newPlayerScene = newSceneInfo.number;
				newAction.newPlayerSceneName = newSceneInfo.name;
			}
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Player: Switch' Action, set to swap the new Player with the old</summary>
		 * <param name = "newPlayerID">The ID number of the Player to switch to</param>
		 * <param name = "transferInventory">If True, the previous Player's inventory will be transferred to the new one</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPlayerSwitch CreateNew_SwapCurentPlayer (int newPlayerID, bool transferInventory = false)
		{
			ActionPlayerSwitch newAction = (ActionPlayerSwitch) CreateInstance <ActionPlayerSwitch>();
			newAction.playerID = newPlayerID;
			newAction.restorePreviousData = false;
			newAction.keepInventory = transferInventory;
			newAction.newPlayerPosition = NewPlayerPosition.ReplaceCurrentPlayer;
			newAction.oldPlayer = OldPlayer.RemoveFromScene;
			return newAction;
		}
		
	}
	
}