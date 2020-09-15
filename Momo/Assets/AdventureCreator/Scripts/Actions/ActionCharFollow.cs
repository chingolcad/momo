/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionCharFollow.cs"
 * 
 *	This action causes NPCs to follow other characters.
 *	If they are moved in any other way, their following
 *	state will reset
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
	public class ActionCharFollow : Action
	{

		public int npcToMoveParameterID = -1;
		public int charToFollowParameterID = -1;

		public int npcToMoveID = 0;
		public int charToFollowID = 0;

		public NPC npcToMove;
		protected NPC runtimeNpcToMove;
		public Char charToFollow;
		protected Char runtimeCharToFollow;
		public bool followPlayer;
		public bool faceWhenIdle;
		public float updateFrequency = 2f;
		public float followDistance = 1f;
		public float followDistanceMax = 15f;
		public enum FollowType { StartFollowing, StopFollowing };
		public FollowType followType;
		public bool randomDirection = false;


		public ActionCharFollow ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Character;
			title = "NPC follow";
			description = "Makes an NPC follow another Character, whether it be a fellow NPC or the Player. If they exceed a maximum distance from their target, they will run towards them. Note that making an NPC move via another Action will make them stop following anyone.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeNpcToMove = AssignFile <NPC> (parameters, npcToMoveParameterID, npcToMoveID, npcToMove);
			runtimeCharToFollow = AssignFile <Char> (parameters, charToFollowParameterID, charToFollowID, charToFollow);
		}
		
		
		public override float Run ()
		{
			if (runtimeNpcToMove)
			{
				if (followType == FollowType.StopFollowing)
				{
					runtimeNpcToMove.StopFollowing ();
					return 0f;
				}

				if (followPlayer || (runtimeCharToFollow != null && runtimeCharToFollow != (Char) runtimeNpcToMove))
				{
					runtimeNpcToMove.FollowAssign (runtimeCharToFollow, followPlayer, updateFrequency, followDistance, followDistanceMax, faceWhenIdle, randomDirection);
				}
			}

			return 0f;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			npcToMoveParameterID = Action.ChooseParameterGUI ("NPC to affect:", parameters, npcToMoveParameterID, ParameterType.GameObject);
			if (npcToMoveParameterID >= 0)
			{
				npcToMoveID = 0;
				npcToMove = null;
			}
			else
			{
				npcToMove = (NPC) EditorGUILayout.ObjectField ("NPC to affect:", npcToMove, typeof(NPC), true);
				
				npcToMoveID = FieldToID <NPC> (npcToMove, npcToMoveID);
				npcToMove = IDToField <NPC> (npcToMove, npcToMoveID, false);
			}

			followType = (FollowType) EditorGUILayout.EnumPopup ("Follow type:", followType);
			if (followType == FollowType.StartFollowing)
			{
				followPlayer = EditorGUILayout.Toggle ("Follow Player?", followPlayer);
				
				if (!followPlayer)
				{
					charToFollowParameterID = Action.ChooseParameterGUI ("Character to follow:", parameters, charToFollowParameterID, ParameterType.GameObject);
					if (charToFollowParameterID >= 0)
					{
						charToFollowID = 0;
						charToFollow = null;
					}
					else
					{
						charToFollow = (Char) EditorGUILayout.ObjectField ("Character to follow:", charToFollow, typeof(Char), true);
						
						if (charToFollow && charToFollow == (Char) npcToMove)
						{
							ACDebug.LogWarning ("An NPC cannot follow themselves!", charToFollow);
							charToFollow = null;
						}
						else
						{
							charToFollowID = FieldToID <Char> (charToFollow, charToFollowID);
							charToFollow = IDToField <Char> (charToFollow, charToFollowID, false);
						}
					}

				}

				randomDirection = EditorGUILayout.Toggle ("Randomise position?", randomDirection);
				updateFrequency = EditorGUILayout.FloatField ("Update frequency (s):", updateFrequency);
				if (updateFrequency <= 0f)
				{
					EditorGUILayout.HelpBox ("Update frequency must be greater than zero.", MessageType.Warning);
				}
				followDistance = EditorGUILayout.FloatField ("Minimum distance:", followDistance);
				if (followDistance <= 0f)
				{
					EditorGUILayout.HelpBox ("Minimum distance must be greater than zero.", MessageType.Warning);
				}
				followDistanceMax = EditorGUILayout.FloatField ("Maximum distance:", followDistanceMax);
				if (followDistanceMax <= 0f || followDistanceMax < followDistance)
				{
					EditorGUILayout.HelpBox ("Maximum distance must be greater than minimum distance.", MessageType.Warning);
				}

				if (followPlayer)
				{
					faceWhenIdle = EditorGUILayout.Toggle ("Faces Player when idle?", faceWhenIdle);
				}
				else
				{
					faceWhenIdle = EditorGUILayout.Toggle ("Faces character when idle?", faceWhenIdle);
				}
			}
			
			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				if (!followPlayer && charToFollow != null && charToFollow.GetComponent <NPC>())
				{
					AddSaveScript <RememberNPC> (charToFollow);
				}
				AddSaveScript <RememberNPC> (npcToMove);
			}

			if (!followPlayer)
			{
				AssignConstantID <Char> (charToFollow, charToFollowID, charToFollowParameterID);
			}
			AssignConstantID <NPC> (npcToMove, npcToMoveID, npcToMoveParameterID);
		}

		
		public override string SetLabel ()
		{
			if (npcToMove != null)
			{
				if (followType == FollowType.StopFollowing)
				{
					return "Stop " + npcToMove;
				}
				else
				{
					if (followPlayer)
					{
						return npcToMove.name + " to Player";
					}
					else if (charToFollow != null)
					{
						return (npcToMove.name + " to " + charToFollow.name);
					}
				}
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (npcToMoveParameterID < 0)
			{
				if (npcToMove != null && npcToMove.gameObject == _gameObject) return true;
				if (npcToMoveID == id) return true;
			}
			if (!followPlayer && charToFollowParameterID < 0)
			{
				if (charToFollow != null && charToFollow.gameObject == _gameObject) return true;
				if (charToFollowID == id) return true;
			}
			if (followPlayer && _gameObject.GetComponent <Player>() != null) return true;
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: NPC follow' Action, set to command an NPC to follow a character</summary>
		 * <param name = "npcToMove>The NPC to affect</param>
		 * <param name = "characterToFollow">The character the NPC should follow</param>
		 * <param name = "minimumDistance">The minimum distance the NPC should be from the NPC</param>
		 * <param name = "maximumDistance">The maximum distance the NPC should be from the NPC</param>
		 * <param name = "updateFrequency">How often the NPC should move towards the character they're following</param>
		 * <param name = "randomisePosition">If True, then the NPC will move to some random point around the character they're following</param>
		 * <param name = "faceCharacterWhenIdle">If True, then the NPC will face the character they're following whenever idle</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharFollow CreateNew_Start (NPC npcToMove, AC.Char characterToFollow, float minimumDistance, float maximumDistance, float updateFrequency = 2f, bool randomisePosition = false, bool faceCharacterWhenIdle = false)
		{
			ActionCharFollow newAction = (ActionCharFollow) CreateInstance <ActionCharFollow>();
			newAction.followType = FollowType.StartFollowing;
			newAction.npcToMove = npcToMove;
			newAction.charToFollow = characterToFollow;
			newAction.followDistance = minimumDistance;
			newAction.followDistanceMax = maximumDistance;
			newAction.updateFrequency = updateFrequency;
			newAction.randomDirection = randomisePosition;
			newAction.faceWhenIdle = faceCharacterWhenIdle;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Face direction' Action, set to command an NPC to stop following anyone</summary>
		 * <param name = "npcToMove">The NPC to stop following anyone</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharFollow CreateNew_Stop (NPC npcToMove)
		{
			ActionCharFollow newAction = (ActionCharFollow) CreateInstance <ActionCharFollow>();
			newAction.followType = FollowType.StopFollowing;
			newAction.npcToMove = npcToMove;
			return newAction;
		}
		
	}

}