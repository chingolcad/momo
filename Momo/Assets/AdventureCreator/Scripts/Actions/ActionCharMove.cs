/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionCharMove.cs"
 * 
 *	This action moves characters by assinging them a Paths object.
 *	If a player is moved, the game will automatically pause.
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
	public class ActionCharMove : Action
	{

		public enum MovePathMethod { MoveOnNewPath, StopMoving, ResumeLastSetPath };
		public MovePathMethod movePathMethod = MovePathMethod.MoveOnNewPath;

		public int charToMoveParameterID = -1;
		public int movePathParameterID = -1;

		public int charToMoveID = 0;
		public int movePathID = 0;

		public bool stopInstantly;
		public Paths movePath;
		protected Paths runtimeMovePath;

		public bool isPlayer;
		public Char charToMove;

		public bool doTeleport;
		public bool doStop;
		public bool startRandom = false;

		protected Char runtimeChar;

		
		public ActionCharMove ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Character;
			title = "Move along path";
			description = "Moves the Character along a pre-determined path. Will adhere to the speed setting selected in the relevant Paths object. Can also be used to stop a character from moving, or resume moving along a path if it was previously stopped.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeChar = AssignFile <Char> (parameters, charToMoveParameterID, charToMoveID, charToMove);
			runtimeMovePath = AssignFile <Paths> (parameters, movePathParameterID, movePathID, movePath);

			if (isPlayer)
			{
				runtimeChar = KickStarter.player;
			}
		}


		protected void UpgradeSelf ()
		{
			if (!doStop)
			{
				return;
			}

			doStop = false;
			movePathMethod = MovePathMethod.StopMoving;
			
			if (Application.isPlaying)
			{
				ACDebug.Log ("'Character: Move along path' Action has been temporarily upgraded - - please view its Inspector when the game ends and save the scene.");
			}
			else
			{
				ACDebug.Log ("Upgraded 'Character: Move along path' Action, please save the scene.");
			}
		}


		public override float Run ()
		{
			UpgradeSelf ();

			if (runtimeMovePath && runtimeMovePath.GetComponent <Char>())
			{
				LogWarning ("Can't follow a Path attached to a Character!");
				return 0f;
			}

			if (!isRunning)
			{
				isRunning = true;

				if (runtimeChar)
				{
					if (runtimeChar is NPC)
					{
						NPC npcToMove = (NPC) runtimeChar;
						npcToMove.StopFollowing ();
					}

					if (movePathMethod == MovePathMethod.StopMoving)
					{
						runtimeChar.EndPath ();
						if (runtimeChar.IsPlayer && KickStarter.playerInteraction.GetHotspotMovingTo () != null)
						{
							KickStarter.playerInteraction.StopMovingToHotspot ();
						}

						if (stopInstantly)
						{
							runtimeChar.Halt ();
						}
					}
					else if (movePathMethod == MovePathMethod.MoveOnNewPath)
					{
						if (runtimeMovePath)
						{
							int randomIndex = -1;
							if (runtimeMovePath.pathType == AC_PathType.IsRandom && startRandom)
							{
								if (runtimeMovePath.nodes.Count > 1)
								{
									randomIndex = Random.Range (0, runtimeMovePath.nodes.Count);
								}
							}

							PrepareCharacter (randomIndex);

							if (willWait && runtimeMovePath.pathType != AC_PathType.ForwardOnly && runtimeMovePath.pathType != AC_PathType.ReverseOnly)
							{
								willWait = false;
								LogWarning ("Cannot pause while character moves along a linear path, as this will create an indefinite cutscene.");
							}

							if (randomIndex >= 0)
							{
								runtimeChar.SetPath (runtimeMovePath, randomIndex, 0);
							}
							else
							{
								runtimeChar.SetPath (runtimeMovePath);
							}
						
							if (willWait)
							{
								return defaultPauseTime;
							}
						}
					}
					else if (movePathMethod == MovePathMethod.ResumeLastSetPath)
					{
						runtimeChar.ResumeLastPath ();
					}
				}

				return 0f;
			}
			else
			{
				if (runtimeChar.GetPath () != runtimeMovePath)
				{
					isRunning = false;
					return 0f;
				}
				else
				{
					return (defaultPauseTime);
				}
			}
		}


		public override void Skip ()
		{
			if (runtimeChar)
			{
				runtimeChar.EndPath (runtimeMovePath);

				if (runtimeChar is NPC)
				{
					NPC npcToMove = (NPC) runtimeChar;
					npcToMove.StopFollowing ();
				}
				
				if (doStop)
				{
					runtimeChar.EndPath ();
				}
				else if (runtimeMovePath)
				{
					int randomIndex = -1;

					if (runtimeMovePath.pathType == AC_PathType.ForwardOnly)
					{
						// Place at end
						int i = runtimeMovePath.nodes.Count-1;
						runtimeChar.Teleport (runtimeMovePath.nodes[i]);
						if (i>0)
						{
							runtimeChar.SetLookDirection (runtimeMovePath.nodes[i] - runtimeMovePath.nodes[i-1], true);
						}
						return;
					}
					else if (runtimeMovePath.pathType == AC_PathType.ReverseOnly)
					{
						// Place at start
						runtimeChar.Teleport (runtimeMovePath.transform.position);
						if (runtimeMovePath.nodes.Count > 1)
						{
							runtimeChar.SetLookDirection (runtimeMovePath.nodes[0] - runtimeMovePath.nodes[1], true);
						}
						return;
					}
					else if (runtimeMovePath.pathType == AC_PathType.IsRandom && startRandom)
					{
						if (runtimeMovePath.nodes.Count > 1)
						{
							randomIndex = Random.Range (0, runtimeMovePath.nodes.Count);
						}
					}

					PrepareCharacter (randomIndex);

					if (!isPlayer)
					{
						if (randomIndex >= 0)
						{
							runtimeChar.SetPath (runtimeMovePath, randomIndex, 0);
						}
						else
						{
							runtimeChar.SetPath (runtimeMovePath);
						}
					}
				}
			}
		}


		protected void PrepareCharacter (int randomIndex)
		{
			if (doTeleport)
			{
				if (randomIndex >= 0)
				{
					runtimeChar.Teleport (runtimeMovePath.nodes[randomIndex]);
				}
				else
				{
					int numNodes = runtimeMovePath.nodes.Count;

					if (runtimeMovePath.pathType == AC_PathType.ReverseOnly)
					{
						runtimeChar.Teleport (runtimeMovePath.nodes[numNodes-1]);

						// Set rotation if there is more than two nodes
						if (numNodes > 2)
						{
							runtimeChar.SetLookDirection (runtimeMovePath.nodes[numNodes-2] - runtimeMovePath.nodes[numNodes-1], true);
						}
					}
					else
					{
						runtimeChar.Teleport (runtimeMovePath.transform.position);
						
						// Set rotation if there is more than one node
						if (numNodes > 1)
						{
							runtimeChar.SetLookDirection (runtimeMovePath.nodes[1] - runtimeMovePath.nodes[0], true);
						}
					}
				}
			}
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			UpgradeSelf ();

			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);

			if (!isPlayer)
			{
				charToMoveParameterID = Action.ChooseParameterGUI ("Character to move:", parameters, charToMoveParameterID, ParameterType.GameObject);
				if (charToMoveParameterID >= 0)
				{
					charToMoveID = 0;
					charToMove = null;
				}
				else
				{
					charToMove = (Char) EditorGUILayout.ObjectField ("Character to move:", charToMove, typeof (Char), true);
					
					charToMoveID = FieldToID <Char> (charToMove, charToMoveID);
					charToMove = IDToField <Char> (charToMove, charToMoveID, false);
				}
			}

			movePathMethod = (MovePathMethod) EditorGUILayout.EnumPopup ("Method:", movePathMethod);
			if (movePathMethod == MovePathMethod.MoveOnNewPath)
			{
				movePathParameterID = Action.ChooseParameterGUI ("Path to follow:", parameters, movePathParameterID, ParameterType.GameObject);
				if (movePathParameterID >= 0)
				{
					movePathID = 0;
					movePath = null;
				}
				else
				{
					movePath = (Paths) EditorGUILayout.ObjectField ("Path to follow:", movePath, typeof(Paths), true);
					
					movePathID = FieldToID <Paths> (movePath, movePathID);
					movePath = IDToField <Paths> (movePath, movePathID, false);
				}

				if (movePath != null && movePath.pathType == AC_PathType.IsRandom)
				{
					startRandom = EditorGUILayout.Toggle ("Start at random node?", startRandom);
				}

				doTeleport = EditorGUILayout.Toggle ("Teleport to start?", doTeleport);
				if (movePath != null && movePath.pathType != AC_PathType.ForwardOnly && movePath.pathType != AC_PathType.ReverseOnly)
				{
					willWait = false;
				}
				else
				{
					willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
				}

				if (movePath != null && movePath.GetComponent <Char>())
				{
					EditorGUILayout.HelpBox ("Can't follow a Path attached to a Character!", MessageType.Warning);
				}
			}
			else if (movePathMethod == MovePathMethod.StopMoving)
			{
				stopInstantly = EditorGUILayout.Toggle ("Stop instantly?", stopInstantly);
			}
			else if (movePathMethod == MovePathMethod.ResumeLastSetPath)
			{
				//
			}
			
			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <ConstantID> (movePath);
				if (!isPlayer && charToMove != null && charToMove.GetComponent <NPC>())
				{
					AddSaveScript <RememberNPC> (charToMove);
				}
			}

			if (!isPlayer)
			{
				AssignConstantID <Char> (charToMove, charToMoveID, charToMoveParameterID);
			}
			AssignConstantID <Paths> (movePath, movePathID, movePathParameterID);
		}
				
		
		public override string SetLabel ()
		{
			if (movePath != null)
			{
				if (charToMove != null)
				{
					return charToMove.name + " to " + movePath.name;
				}
				else if (isPlayer)
				{
					return "Player to " + movePath.name;
				}
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && charToMoveParameterID < 0)
			{
				if (charToMove != null && charToMove.gameObject == _gameObject) return true;
				if (charToMoveID == id) return true;
			}
			if (isPlayer && _gameObject.GetComponent <Player>() != null) return true;
			if (movePathMethod == MovePathMethod.MoveOnNewPath && movePathParameterID < 0)
			{
				if (movePath != null && movePath.gameObject == _gameObject) return true;
				if (movePathID == id) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: Move along path' Action, set to command a character to move along a new path</summary>
		 * <param name = "characterToMove">The character to affect</param>
		 * <param name = "pathToFollow">The Path that the character should follow</param>
		 * <param name = "teleportToStart">If True, the character will teleport to the first node on the Path</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharMove CreateNew_NewPath (AC.Char characterToMove, Paths pathToFollow, bool teleportToStart = false)
		{
			ActionCharMove newAction = (ActionCharMove) CreateInstance <ActionCharMove>();
			newAction.movePathMethod = MovePathMethod.MoveOnNewPath;
			newAction.charToMove = characterToMove;
			newAction.movePath = pathToFollow;
			newAction.doTeleport = teleportToStart;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Move along path' Action, set to command a character to resume moving along their last-assigned path</summary>
		 * <param name = "characterToMove">The character to affect</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharMove CreateNew_ResumeLastPath (AC.Char characterToMove)
		{
			ActionCharMove newAction = (ActionCharMove) CreateInstance <ActionCharMove>();
			newAction.movePathMethod = MovePathMethod.ResumeLastSetPath;
			newAction.charToMove = characterToMove;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Move along path' Action, set to command a character to stop moving</summary>
		 * <param name = "characterToStop">The character to affect</param>
		 * <param name = "stopInstantly">If True, the character will stop in one frame, as opposed to more naturally through deceleration</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharMove CreateNew_StopMoving (AC.Char characterToStop, bool stopInstantly = false)
		{
			ActionCharMove newAction = (ActionCharMove) CreateInstance <ActionCharMove>();
			newAction.movePathMethod = MovePathMethod.StopMoving;
			newAction.charToMove = characterToStop;
			newAction.stopInstantly = stopInstantly;
			return newAction;
		}
		
	}

}