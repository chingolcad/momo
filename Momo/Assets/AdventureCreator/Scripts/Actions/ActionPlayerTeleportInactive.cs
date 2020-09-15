/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionPlayerTeleportInactive.cs"
 * 
 *	Moves the recorded position of an inactive Player to the current scene.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace AC
{

	[System.Serializable]
	public class ActionPlayerTeleportInactive : Action
	{
		
		public int playerID;
		public int playerIDParameterID = -1;

		public Transform newTransform;
		public int newTransformConstantID = 0;
		public int newTransformParameterID = -1;
		protected Transform runtimeNewTransform;

		public _Camera associatedCamera;
		public int associatedCameraConstantID = 0;
		public int associatedCameraParameterID = -1;
		protected _Camera runtimeAssociatedCamera;


		public ActionPlayerTeleportInactive ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Player;
			title = "Teleport inactive";
			description = "Moves the recorded position of an inactive Player to the current scene.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			playerID = AssignInteger (parameters, playerIDParameterID, playerID);
			runtimeNewTransform = AssignFile (parameters, newTransformParameterID, newTransformConstantID, newTransform);
			runtimeAssociatedCamera = AssignFile <_Camera> (parameters, associatedCameraParameterID, associatedCameraConstantID, associatedCamera);
		}
		
		
		public override float Run ()
		{
			KickStarter.saveSystem.MoveInactivePlayerToCurrentScene (playerID, runtimeNewTransform, runtimeAssociatedCamera);

			return 0f;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (KickStarter.settingsManager != null)
			{
				if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
				{
					EditorGUILayout.HelpBox ("This Action requires Player Switching to be allowed, as set in the Settings Manager.", MessageType.Info);
					return;
				}
				
				if (KickStarter.settingsManager.players.Count == 0)
				{
					EditorGUILayout.HelpBox ("No players are defined in the Settings Manager.", MessageType.Warning);
					return;
				}

				playerIDParameterID = Action.ChooseParameterGUI ("New Player ID:", parameters, playerIDParameterID, ParameterType.Integer);
				if (playerIDParameterID == -1)
				{
					// Create a string List of the field's names (for the PopUp box)
					List<string> labelList = new List<string>();
					
					int i = 0;
					int playerNumber = -1;

					foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
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
					playerID = KickStarter.settingsManager.players[playerNumber].ID;
				}

				newTransformParameterID = Action.ChooseParameterGUI ("New Transform:", parameters, newTransformParameterID, ParameterType.GameObject);
				if (newTransformParameterID >= 0)
				{
					newTransformConstantID = 0;
					newTransform = null;
				}
				else
				{
					newTransform = (Transform) EditorGUILayout.ObjectField ("New Transform:", newTransform, typeof (Transform), true);
					
					newTransformConstantID = FieldToID (newTransform, newTransformConstantID);
					newTransform = IDToField (newTransform, newTransformConstantID, true);
				}

				associatedCameraParameterID = Action.ChooseParameterGUI ("Camera (optional):", parameters, associatedCameraParameterID, ParameterType.GameObject);
				if (associatedCameraParameterID >= 0)
				{
					associatedCameraConstantID = 0;
					associatedCamera = null;
				}
				else
				{
					associatedCamera = (_Camera) EditorGUILayout.ObjectField ("Camera (optional):", associatedCamera, typeof (_Camera), true);
					
					associatedCameraConstantID = FieldToID (associatedCamera, associatedCameraConstantID);
					associatedCamera = IDToField (associatedCamera, associatedCameraConstantID, true);
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("No Settings Manager assigned!", MessageType.Warning);
			}
			
			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID <_Camera> (associatedCamera, associatedCameraConstantID, associatedCameraParameterID);
			AssignConstantID (newTransform, newTransformConstantID, newTransformParameterID);
		}
		

		public override string SetLabel ()
		{
			if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				PlayerPrefab newPlayerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
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
			if (newTransformParameterID < 0)
			{
				if (newTransform != null && newTransform.gameObject == gameObject) return true;
				if (newTransformConstantID == id) return true;
			}
			if (associatedCameraParameterID < 0)
			{
				if (associatedCamera != null && associatedCamera.gameObject == gameObject) return true;
				if (associatedCameraConstantID == id) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Player: Teleport inactive' Action</summary>
		 * <param name = "playerID">The ID number of the Player to teleport</param>
		 * <param name = "newTransform">The new Transform for the Player to take</param>
		 * <param name = "newCamera">If set, the camera that will be active when the Player is next switched to</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPlayerTeleportInactive CreateNew (int playerID, Transform newTransform, _Camera newCamera = null)
		{
			ActionPlayerTeleportInactive newAction = (ActionPlayerTeleportInactive) CreateInstance <ActionPlayerTeleportInactive>();
			newAction.playerID = playerID;
			newAction.newTransform = newTransform;
			newAction.associatedCamera = newCamera;
			return newAction;
		}

	}

}