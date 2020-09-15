/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"SaveData.cs"
 * 
 *	This script contains all the non-scene-specific data we wish to save.
 * 
 */

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A data container for all global data that gets stored in save games.
	 */
	[System.Serializable]
	public class SaveData
	{

		/** An instance of the MainData class */
		public MainData mainData;
		/** Instances of PlayerData for each of the game's Players */
		public List<PlayerData> playerData = new List<PlayerData>();

		/**
		 * The default Constructor.
		 */
		public SaveData () { }


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			EditorGUILayout.LabelField ("Main data:", CustomStyles.subHeader);
			mainData.ShowGUI ();
			if (playerData != null)
			{
				for (int i=0; i<playerData.Count; i++)
				{
					GUILayout.Box (string.Empty, GUILayout.ExpandWidth (true), GUILayout.Height (1));
					EditorGUILayout.LabelField ("Player data #" + i + ":", CustomStyles.subHeader);
					playerData[i].ShowGUI ();
				}
			}
		}

		#endif

	}


	/**
	 * A data container for all non-player global data that gets stored in save games.
	 * A single instance of this class is stored in SaveData by SaveSystem.
	 */
	[System.Serializable]
	public struct MainData
	{

		/** The ID number of the currently-active Player */
		public int currentPlayerID;
		/** The game's current timeScale */
		public float timeScale;

		/** The current values of all Global Variables */
		public string runtimeVariablesData;
		/** All user-generated CustomToken variables */
		public string customTokenData;

		/** The locked state of all Menu variables */
		public string menuLockData;
		/** The visibility state of all Menu instances */
		public string menuVisibilityData;
		/** The visibility state of all MenuElement instances */
		public string menuElementVisibilityData;
		/** The page data of all MenuJournal instances */
		public string menuJournalData;

		/** The Constant ID number of the currently-active ArrowPrompt */
		public int activeArrows;
		/** The Constant ID number of the currently-active Conversation */
		public int activeConversation;

		/** The ID number of the currently-selected InvItem */
		public int selectedInventoryID;
		/** True if the currently-selected InvItem is in "give" mode, as opposed to "use" */
		public bool isGivingItem;

		/** True if the cursor system, PlayerCursor, is disabled */
		public bool cursorIsOff;
		/** True if the input system, PlayerInput, is disabled */
		public bool inputIsOff;
		/** True if the interaction system, PlayerInteraction, is disabled */
		public bool interactionIsOff;
		/** True if the menu system, PlayerMenus, is disabled */
		public bool menuIsOff;
		/** True if the movement system, PlayerMovement, is disabled */
		public bool movementIsOff;
		/** True if the camera system is disabled */
		public bool cameraIsOff;
		/** True if Triggers are disabled */
		public bool triggerIsOff;
		/** True if Players are disabled */
		public bool playerIsOff;
		/** True if keyboard/controller can be used to control menus during gameplay */
		public bool canKeyboardControlMenusDuringGameplay;
		/** The state of the cursor toggle (1 = on, 2 = off) */
		public int toggleCursorState;

		/** The IDs and loop states of all queued music tracks, including the one currently-playing */
		public string musicQueueData;
		/** The IDs and loop states of the last set of queued music tracks */
		public string lastMusicQueueData;
		/** The time position of the current music track */
		public int musicTimeSamples;
		/** The time position of the last-played music track */
		public int lastMusicTimeSamples;
		/** The IDs and time positions of all tracks that have been played before */
		public string oldMusicTimeSamples;

		/** The IDs and loop states of all queued ambience tracks, including the one currently-playing */
		public string ambienceQueueData;
		/** The IDs and loop states of the last set of queued ambience tracks */
		public string lastAmbienceQueueData;
		/** The time position of the current ambience track */
		public int ambienceTimeSamples;
		/** The time position of the last-played ambience track */
		public int lastAmbienceTimeSamples;
		/** The IDs and time positions of all ambience tracks that have been played before */
		public string oldAmbienceTimeSamples;

		/** The currently-set AC_MovementMethod enum, converted to an integer */
		public int movementMethod;
		/** Data regarding paused and skipping ActionList assets */
		public string activeAssetLists;
		/** Data regarding active inputs */
		public string activeInputsData;

		/** Data regarding which speech lines, that can only be spoken once, have already been spoken */
		public string spokenLinesData;

		/** A record of the current global objectives */
		public string globalObjectivesData;


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			CustomGUILayout.MultiLineLabelGUI ("Current Player ID:", currentPlayerID.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("TimeScale:", timeScale.ToString ());

			if (KickStarter.variablesManager != null)
			{
				EditorGUILayout.LabelField ("Global Variables:");

				List<GVar> linkedVariables = SaveSystem.UnloadVariablesData (runtimeVariablesData, KickStarter.variablesManager.vars, true);
				foreach (GVar linkedVariable in linkedVariables)
				{
					if (linkedVariable.link != VarLink.OptionsData)
					{
						EditorGUILayout.LabelField ("   " + linkedVariable.label + ":", linkedVariable.GetValue ());
					}
				}
			}
			else
			{
				CustomGUILayout.MultiLineLabelGUI ("Global Variables:", runtimeVariablesData);
			}

			EditorGUILayout.LabelField ("Menus:");
			CustomGUILayout.MultiLineLabelGUI ("   Menu locks:", menuLockData);
			CustomGUILayout.MultiLineLabelGUI ("   Menu visibility:", menuVisibilityData);
			CustomGUILayout.MultiLineLabelGUI ("   Menu element visibility:", menuElementVisibilityData);
			CustomGUILayout.MultiLineLabelGUI ("   Menu journal pages:", menuJournalData);
			CustomGUILayout.MultiLineLabelGUI ("   Direct-control gameplay?", canKeyboardControlMenusDuringGameplay.ToString ());

			EditorGUILayout.LabelField ("Inventory:");
			CustomGUILayout.MultiLineLabelGUI ("   Selected InvItem ID:", selectedInventoryID.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Is giving item?", isGivingItem.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Global objectives:", globalObjectivesData);

			EditorGUILayout.LabelField ("Systems:");
			CustomGUILayout.MultiLineLabelGUI ("   Cursors disabled?", cursorIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Input disabled?", inputIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Interaction disabled?", interactionIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Menus disabled?", menuIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Movement disabled?", movementIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Cameras disabled?", cameraIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Triggers disabled", triggerIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Players disabled?", playerIsOff.ToString ());

			CustomGUILayout.MultiLineLabelGUI ("Toggle cursor state:", toggleCursorState.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("Movement method:", ((MovementMethod) movementMethod).ToString ());

			EditorGUILayout.LabelField ("Music:");
			CustomGUILayout.MultiLineLabelGUI ("   Music queue data:", musicQueueData);
			CustomGUILayout.MultiLineLabelGUI ("   Last music data:", lastMusicQueueData);
			CustomGUILayout.MultiLineLabelGUI ("   Music time samples:", musicTimeSamples.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Last music samples:", lastMusicTimeSamples.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Old music samples:", oldMusicTimeSamples);

			EditorGUILayout.LabelField ("Ambience:");
			CustomGUILayout.MultiLineLabelGUI ("   Ambience queue data:", ambienceQueueData);
			CustomGUILayout.MultiLineLabelGUI ("   Last ambience data:", lastAmbienceQueueData);
			CustomGUILayout.MultiLineLabelGUI ("   Ambience time samples:", ambienceTimeSamples.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Last ambience samples:", lastAmbienceTimeSamples.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Old ambience samples:", oldAmbienceTimeSamples);

			EditorGUILayout.LabelField ("Speech:");
			CustomGUILayout.MultiLineLabelGUI ("Custom tokens:", customTokenData);
			CustomGUILayout.MultiLineLabelGUI ("Spoken lines:", spokenLinesData);

			EditorGUILayout.LabelField ("Active logic:");
			CustomGUILayout.MultiLineLabelGUI ("   Active Conversation:", activeConversation.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Active ArrowPrompt:", activeArrows.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Active ActionList assets:", activeAssetLists);
			CustomGUILayout.MultiLineLabelGUI ("   Active inputs:", activeInputsData);
		}

		#endif

	}


	/**
	 * A data container for saving the state of a Player.
	 * Each Player in a game has its own instance of this class stored in SaveData by SaveSystem.
	 */
	[System.Serializable]
	public class PlayerData
	{

		/** The ID number of the Player that this data references */
		public int playerID;
		/** The current scene number */
		public int currentScene;
		/** The last-visited scene number */
		public int previousScene;
		/** The current scene name */
		public string currentSceneName;
		/** The last-visited scene name */
		public string previousSceneName;
		/** The details any sub-scenes that are also open */
		public string openSubScenes;

		/** The Player's X position */
		public float playerLocX;
		/** The Player's Y position */
		public float playerLocY;
		/** The Player's Z position */
		public float playerLocZ;
		/** The Player'sY rotation */
		public float playerRotY;

		/** The walk speed */
		public float playerWalkSpeed;
		/** The run speed */
		public float playerRunSpeed;

		/** The idle animation */
		public string playerIdleAnim;
		/** The walk animation */
		public string playerWalkAnim;
		/** The talk animation */
		public string playerTalkAnim;
		/** The run animation */
		public string playerRunAnim;

		/** A unique identifier for the walk sound AudioClip */
		public string playerWalkSound;
		/** A unique identifier for the run sound AudioClip */
		public string playerRunSound;
		/** A unique identified for the portrait graphic */
		public string playerPortraitGraphic;
		/** The Player's display name */
		public string playerSpeechLabel;
		/** The ID number that references the Player's name, as generated by the Speech Manager */
		public int playerDisplayLineID;

		/** The target node number of the current Path */
		public int playerTargetNode;
		/** The previous node number of the current Path */
		public int playerPrevNode;
		/** The positions of each node in a pathfinding-generated Path */
		public string playerPathData;
		/** True if the Player is currently running */
		public bool playerIsRunning;
		/** True if the Player is locked along a Path */
		public bool playerLockedPath;
		/** The Constant ID number of the Player's current Path */
		public int playerActivePath;
		/** True if the Player's current Path affects the Y position */
		public bool playerPathAffectY;

		/** The target node number of the Player's last-used Path */
		public int lastPlayerTargetNode;
		/** The previous node number of the Player's last-used Path */
		public int lastPlayerPrevNode;
		/** The Constant ID number of the Player's last-used Path */
		public int lastPlayerActivePath;

		/** True if the Player cannot move up */
		public bool playerUpLock;
		/** True if the Player cannot move down */
		public bool playerDownLock;
		/** True if the Player cannot move left */
		public bool playerLeftlock;
		/** True if the Player cannot move right */
		public bool playerRightLock;
		/** True if the Player cannot run */
		public int playerRunLock;
		/** True if free-aiming is prevented */
		public bool playerFreeAimLock;
		/** True if the Player's Rigidbody is unaffected by gravity */
		public bool playerIgnoreGravity;

		/** True if a sprite-based Player is locked to face a particular direction */
		public bool playerLockDirection;
		/** The direction that a sprite-based Player is currently facing */
		public string playerSpriteDirection;
		/** True if a sprite-based Player has its scale locked */
		public bool playerLockScale;
		/** The scale of a sprite-based Player */
		public float playerSpriteScale;
		/** True if a sprite-based Player has its sorting locked */
		public bool playerLockSorting;
		/** The sorting order of a sprite-based Player */
		public int playerSortingOrder;
		/** The order in layer of a sprite-based Player */
		public string playerSortingLayer;

		/** What Inventory Items (see: InvItem) the player is currently carrying */
		public string inventoryData;

		/** If True, the Player is playing a custom animation */
		public bool inCustomCharState;
		/** True if the Player's head is facing a Hotspot */
		public bool playerLockHotspotHeadTurning;
		/** True if the Player's head is facing a particular object */
		public bool isHeadTurning;
		/** The ConstantID number of the head target Transform */
		public int headTargetID;
		/** The Player's head target's X position (offset) */
		public float headTargetX;
		/** The Player's head target's Y position (offset) */
		public float headTargetY;
		/** The Player's head target's Z position (offset) */
		public float headTargetZ;

		/** The Constant ID number of the active _Camera */
		public int gameCamera;
		/** The Constant ID number of the last active _Camera during gameplay */
		public int lastNavCamera;
		/** The Constant ID number of the last active-but-one _Camera during gameplay */
		public int lastNavCamera2;

		/** The MainCamera's X position */
		public float mainCameraLocX;
		/** The MainCamera's Y position */
		public float mainCameraLocY;
		/** The MainCamera's Z position */
		public float mainCameraLocZ;
		/** The MainCamera's X rotation */
		public float mainCameraRotX;
		/** The MainCamera's Y rotation */
		public float mainCameraRotY;
		/** The MainCamera's Z rotation */
		public float mainCameraRotZ;

		/** True if split-screen is currently active */
		public bool isSplitScreen;
		/** True if the gameplay is performed in the top (or left) side during split-screen */
		public bool isTopLeftSplit;
		/** True if split-screen is arranged vertically  */
		public bool splitIsVertical;
		/** The Constant ID number of the split-screen camera that gameplay is not performed in */
		public int splitCameraID;
		/** During split-screen, the proportion of the screen that the gameplay camera takes up */
		public float splitAmountMain;
		/** During split-screen, the proportion of the screen that the non-gameplay camera take up */
		public float splitAmountOther;
		/** The intensity of the current camera shake */
		public float shakeIntensity;
		/** The duration, in seconds, of the current camera shake */
		public float shakeDuration;
		/** The int-converted value of CamersShakeEffect */
		public int shakeEffect;
		/** During box-overlay, the size and position of the overlay effect */
		public float overlayRectX, overlayRectY, overlayRectWidth, overlayRectHeight;

		/** True if the NPC has a FollowSortingMap component that follows the scene's default SortingMap */
		public bool followSortingMap;
		/** The ConstantID number of the SortingMap that the NPC's FollowSortingMap follows, if not the scene's default */
		public int customSortingMapID;

		/** The active Document being read */
		public int activeDocumentID = -1;
		/** A record of the Documents collected */
		public string collectedDocumentData;
		/** A record of the last-opened page for each viewed Document */
		public string lastOpenDocumentPagesData;
		/** A record of the player's current objectives */
		public string playerObjectivesData;

		/** Save data for any Remember components attached to the Player */
		public List<ScriptData> playerScriptData;


		public void UpdatePosition (SceneInfo newSceneInfo, Vector3 newPosition, Quaternion newRotation, _Camera associatedCamera)
		{
			currentScene = newSceneInfo.number;
			currentSceneName = newSceneInfo.name;
			playerLocX = newPosition.x;
			playerLocY = newPosition.y;
			playerLocZ = newPosition.z;
			playerRotY = newRotation.eulerAngles.y;

			if (associatedCamera != null)
			{
				gameCamera = 0;
				ConstantID cameraID = associatedCamera.GetComponent<ConstantID> ();
				if (cameraID != null)
				{
					gameCamera = cameraID.constantID;
				}
				else
				{
					Debug.LogWarning ("Cannot save Player's active camera because " + associatedCamera + " has no ConstantID component", associatedCamera);
				}
			}
		}


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			CustomGUILayout.MultiLineLabelGUI ("Player ID:", playerID.ToString ());

			EditorGUILayout.LabelField ("Scene info:");
			CustomGUILayout.MultiLineLabelGUI ("   Current scene:", new SceneInfo (currentSceneName, currentScene).GetLabel ());
			CustomGUILayout.MultiLineLabelGUI ("   Previous scene:", new SceneInfo (currentSceneName, currentScene).GetLabel ());
			CustomGUILayout.MultiLineLabelGUI ("   Sub-scenes:", openSubScenes);

			EditorGUILayout.LabelField ("Movement:");
			CustomGUILayout.MultiLineLabelGUI ("   Position:", "(" + playerLocX.ToString () + ", " + playerLocY.ToString () + ", " + playerLocZ.ToString () + ")");
			CustomGUILayout.MultiLineLabelGUI ("   Rotation:", playerRotY.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Walk speed:", playerWalkSpeed.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Run speed:", playerRunSpeed.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Is running?", playerIsRunning.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Up locked?", playerUpLock.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Down locked?", playerDownLock.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Left locked?", playerLeftlock.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Right locked?", playerRightLock.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Run locked?", playerRunLock.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Free-aim locked?", playerFreeAimLock.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Ignore gravity?", playerIgnoreGravity.ToString ());

			EditorGUILayout.LabelField ("Animation:");
			CustomGUILayout.MultiLineLabelGUI ("Idle animation:", playerIdleAnim);
			CustomGUILayout.MultiLineLabelGUI ("Walk animation:", playerWalkAnim);
			CustomGUILayout.MultiLineLabelGUI ("Talk animation:", playerTalkAnim);
			CustomGUILayout.MultiLineLabelGUI ("Run animation:", playerRunAnim);
			CustomGUILayout.MultiLineLabelGUI ("In custom state?", inCustomCharState.ToString ());

			EditorGUILayout.LabelField ("Sound:");
			CustomGUILayout.MultiLineLabelGUI ("  Walk sound:", playerWalkSound);
			CustomGUILayout.MultiLineLabelGUI ("  Run sound:", playerRunSound);

			EditorGUILayout.LabelField ("Speech:");
			CustomGUILayout.MultiLineLabelGUI ("  Portrait graphic:", playerPortraitGraphic);
			CustomGUILayout.MultiLineLabelGUI ("  Speech label:", playerSpeechLabel);
			CustomGUILayout.MultiLineLabelGUI ("  Speech label ID:", playerDisplayLineID.ToString ());

			EditorGUILayout.LabelField ("Pathfinding:");
			CustomGUILayout.MultiLineLabelGUI ("  Target node:", playerTargetNode.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("  Previous node:", playerPrevNode.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("  Path data:", playerPathData);
			CustomGUILayout.MultiLineLabelGUI ("  Locked to path?", playerLockedPath.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("  Active path:", playerActivePath.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("  Path affects Y?", playerPathAffectY.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("  Last target node:", lastPlayerTargetNode.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("  Last previous node:", lastPlayerPrevNode.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("  Last active path:", lastPlayerActivePath.ToString ());

			EditorGUILayout.LabelField ("Sprites:");
			CustomGUILayout.MultiLineLabelGUI ("   Lock direction?", playerLockDirection.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Sprite direction:", playerSpriteDirection);
			CustomGUILayout.MultiLineLabelGUI ("   Scale locked?", playerLockScale.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Sprite scale:", playerSpriteScale.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Lock sorting?", playerLockSorting.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("    Sorting order:", playerSortingOrder.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("  Sorting layer:", playerSortingLayer);
			CustomGUILayout.MultiLineLabelGUI ("Follow default Sorting Map?", followSortingMap.ToString ());
			if (!followSortingMap)
			{
				CustomGUILayout.MultiLineLabelGUI ("Sorting map?", customSortingMapID.ToString ());
			}

			CustomGUILayout.MultiLineLabelGUI ("Inventory:", inventoryData);
			CustomGUILayout.MultiLineLabelGUI ("   Active Document:", activeDocumentID.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Collected Documents:", collectedDocumentData.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Last-open Document pages", lastOpenDocumentPagesData.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Objectives:", playerObjectivesData.ToString ());

			EditorGUILayout.LabelField ("Head-turning:");
			CustomGUILayout.MultiLineLabelGUI ("   Head facing Hotspot?", playerLockHotspotHeadTurning.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Head turning?", isHeadTurning.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Head target:", headTargetID.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Head target position:", "(" + headTargetX + ", " + headTargetY + ", " + headTargetZ + ")");

			EditorGUILayout.LabelField ("Camera:");
			CustomGUILayout.MultiLineLabelGUI ("   Camera:", gameCamera.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Last nav cam:", lastNavCamera.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Last nav cam 2:", lastNavCamera2.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Camera position:", "(" + mainCameraLocX + ", " + mainCameraLocY + ", " + mainCameraLocZ + ")");
			CustomGUILayout.MultiLineLabelGUI ("   Camera rotation:", "(" + mainCameraRotX + ", " + mainCameraRotY + ", " + mainCameraRotZ + ")");
			CustomGUILayout.MultiLineLabelGUI ("   Split-screen?", isSplitScreen.ToString ());
			if (isSplitScreen)
			{
				CustomGUILayout.MultiLineLabelGUI ("   Top-left split?", isTopLeftSplit.ToString ());
				CustomGUILayout.MultiLineLabelGUI ("   Vertical split?", splitIsVertical.ToString ());
				CustomGUILayout.MultiLineLabelGUI ("   Split camera:", splitCameraID.ToString ());
				CustomGUILayout.MultiLineLabelGUI ("   Split amount main:", splitAmountMain.ToString ());
				CustomGUILayout.MultiLineLabelGUI ("   Split amount other:", splitAmountOther.ToString ());
				CustomGUILayout.MultiLineLabelGUI ("   Overlay rect:", "(" + overlayRectX + ", " + overlayRectY + ", " + overlayRectWidth + ", " + overlayRectHeight + ")");
			}
			CustomGUILayout.MultiLineLabelGUI ("   Shake intensity:", shakeIntensity.ToString ());
			if (shakeIntensity > 0f)
			{
				CustomGUILayout.MultiLineLabelGUI ("   Shake duration", shakeDuration.ToString ());
				CustomGUILayout.MultiLineLabelGUI ("   Shake effect:", ((CameraShakeEffect) shakeEffect).ToString ());
			}

			if (playerScriptData != null && playerScriptData.Count > 0)
			{
				EditorGUILayout.LabelField ("Remember data:");
				foreach (ScriptData scriptData in playerScriptData)
				{
					RememberData rememberData = SaveSystem.FileFormatHandler.DeserializeObject<RememberData> (scriptData.data);
					if (rememberData != null)
					{
						CustomGUILayout.MultiLineLabelGUI ("   " + rememberData.GetType ().ToString () + ":", EditorJsonUtility.ToJson (rememberData, true));
					}
				}
			}
		}

		#endif

	}

}