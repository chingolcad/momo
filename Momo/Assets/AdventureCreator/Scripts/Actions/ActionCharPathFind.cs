/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionCharPathFind.cs"
 * 
 *	This action moves characters by generating a path to a specified point.
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
	public class ActionCharPathFind : Action
	{

		public int charToMoveParameterID = -1;
		public int markerParameterID = -1;

		public int charToMoveID = 0;
		public int markerID = 0;
		
		public Marker marker;
		public bool isPlayer;
		public Char charToMove;
		public PathSpeed speed;
		public bool pathFind = true;
		public bool doFloat = false;

		public bool doTimeLimit;
		public int maxTimeParameterID = -1;
		public float maxTime = 10f;
		[SerializeField] protected OnReachTimeLimit onReachTimeLimit = OnReachTimeLimit.TeleportToDestination;
		protected enum OnReachTimeLimit { TeleportToDestination, StopMoving };
		protected float currentTimer;
		protected Char runtimeChar;
		protected Marker runtimeMarker;

		public bool faceAfter = false;
		protected bool isFacingAfter;


		public ActionCharPathFind ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Character;
			title = "Move to point";
			description = "Moves a character to a given Marker object. By default, the character will attempt to pathfind their way to the marker, but can optionally just move in a straight line.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				runtimeChar = KickStarter.player;
			}
			else
			{
				runtimeChar = AssignFile <Char> (parameters, charToMoveParameterID, charToMoveID, charToMove);
			}

			Hotspot markerHotspot = AssignFile <Hotspot> (parameters, markerParameterID, markerID, null, false);
			if (markerHotspot != null && markerHotspot.walkToMarker != null)
			{
				runtimeMarker = markerHotspot.walkToMarker;
			}
			else
			{
				runtimeMarker = AssignFile <Marker> (parameters, markerParameterID, markerID, marker);
			}

			maxTime = AssignFloat (parameters, maxTimeParameterID, maxTime);
			isFacingAfter = false;
		}
		
		
		public override float Run ()
		{
			if (!isRunning)
			{
				if (runtimeChar != null && runtimeMarker != null)
				{
					isRunning = true;

					Paths path = runtimeChar.GetComponent <Paths>();
					if (path == null)
					{
						LogWarning ("Cannot move a character with no Paths component", runtimeChar);
					}
					else
					{
						if (runtimeChar is NPC)
						{
							NPC npcToMove = (NPC) runtimeChar;
							npcToMove.StopFollowing ();
						}

						path.pathType = AC_PathType.ForwardOnly;
						path.pathSpeed = speed;
						path.affectY = true;

						Vector3[] pointArray;
						Vector3 targetPosition = runtimeMarker.transform.position;

						if (SceneSettings.ActInScreenSpace ())
						{
							targetPosition = AdvGame.GetScreenNavMesh (targetPosition);
						}

						float distance = Vector3.Distance (targetPosition, runtimeChar.transform.position);
						if (distance <= KickStarter.settingsManager.GetDestinationThreshold ())
						{
							isRunning = false;
							return 0f;
						}

						if (pathFind && KickStarter.navigationManager)
						{
							pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (runtimeChar.transform.position, targetPosition, runtimeChar);
						}
						else
						{
							List<Vector3> pointList = new List<Vector3>();
							pointList.Add (targetPosition);
							pointArray = pointList.ToArray ();
						}

						if (speed == PathSpeed.Walk)
						{
							runtimeChar.MoveAlongPoints (pointArray, false, pathFind);
						}
						else
						{
							runtimeChar.MoveAlongPoints (pointArray, true, pathFind);
						}

						if (runtimeChar.GetPath ())
						{
							if (!pathFind && doFloat)
							{
								runtimeChar.GetPath ().affectY = true;
							}
							else
							{
								runtimeChar.GetPath ().affectY = false;
							}
						}

						if (willWait)
						{
							currentTimer = maxTime;
							return defaultPauseTime;
						}
					}
				}

				return 0f;
			}
			else
			{
				if (runtimeChar.GetPath () == null)
				{
					if (faceAfter)
					{
						if (!isFacingAfter)
						{
							isFacingAfter = true;
							runtimeChar.SetLookDirection (runtimeMarker.transform.forward, false);
							return defaultPauseTime;
						}
						else
						{
							if (runtimeChar.IsTurning ())
							{
								return defaultPauseTime;
							}
						}
					}

					isRunning = false;
					return 0f;
				}
				else
				{
					if (doTimeLimit)
					{
						currentTimer -= Time.deltaTime;
						if (currentTimer <= 0)
						{
							switch (onReachTimeLimit)
							{
								case OnReachTimeLimit.StopMoving:
									runtimeChar.EndPath ();
									break;

								case OnReachTimeLimit.TeleportToDestination:
									Skip ();
									break;
							}

							isRunning = false;
							return 0f;
						}
					}

					return (defaultPauseTime);
				}
			}
		}


		public override void Skip ()
		{
			if (runtimeChar != null && runtimeMarker != null)
			{
				runtimeChar.EndPath ();

				if (runtimeChar is NPC)
				{
					NPC npcToMove = (NPC) runtimeChar;
					npcToMove.StopFollowing ();
				}
				
				Vector3[] pointArray;
				Vector3 targetPosition = runtimeMarker.transform.position;
				
				if (SceneSettings.ActInScreenSpace ())
				{
					targetPosition = AdvGame.GetScreenNavMesh (targetPosition);
				}
				
				if (pathFind && KickStarter.navigationManager)
				{
					pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (runtimeChar.transform.position, targetPosition);
					KickStarter.navigationManager.navigationEngine.ResetHoles (KickStarter.sceneSettings.navMesh);
				}
				else
				{
					List<Vector3> pointList = new List<Vector3>();
					pointList.Add (targetPosition);
					pointArray = pointList.ToArray ();
				}
				
				int i = pointArray.Length-1;

				if (i>0)
				{
					runtimeChar.SetLookDirection (pointArray[i] - pointArray[i-1], true);
				}
				else
				{
					runtimeChar.SetLookDirection (pointArray[i] - runtimeChar.transform.position, true);
				}

				runtimeChar.Teleport (pointArray [i]);

				if (faceAfter)
				{
					runtimeChar.SetLookDirection (runtimeMarker.transform.forward, true);
				}
			}
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
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

			markerParameterID = Action.ChooseParameterGUI ("Marker to reach:", parameters, markerParameterID, ParameterType.GameObject);
			if (markerParameterID >= 0)
			{
				markerID = 0;
				marker = null;

				EditorGUILayout.HelpBox ("If a Hotspot is passed to this parameter, that Hotspot's 'Walk-to Marker' will be referred to.", MessageType.Info);
			}
			else
			{
				marker = (Marker) EditorGUILayout.ObjectField ("Marker to reach:", marker, typeof (Marker), true);
				
				markerID = FieldToID <Marker> (marker, markerID);
				marker = IDToField <Marker> (marker, markerID, false);
			}

			speed = (PathSpeed) EditorGUILayout.EnumPopup ("Move speed:" , speed);
			pathFind = EditorGUILayout.Toggle ("Pathfind?", pathFind);
			if (!pathFind)
			{
				doFloat = EditorGUILayout.Toggle ("Ignore gravity?", doFloat);
			}
			willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);

			if (willWait)
			{
				EditorGUILayout.Space ();
				faceAfter = EditorGUILayout.Toggle ("Copy Marker angle after?", faceAfter);
				doTimeLimit = EditorGUILayout.Toggle ("Enforce time limit?", doTimeLimit);
				if (doTimeLimit)
				{
					maxTimeParameterID = Action.ChooseParameterGUI ("Time limit (s):", parameters, maxTimeParameterID, ParameterType.Float);
					if (maxTimeParameterID < 0)
					{
						maxTime = EditorGUILayout.FloatField ("Time limit (s):", maxTime);
					}
					onReachTimeLimit = (OnReachTimeLimit) EditorGUILayout.EnumPopup ("On reach time limit:", onReachTimeLimit);
				}
			}

			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				if (!isPlayer && charToMove != null && charToMove.GetComponent <NPC>())
				{
					AddSaveScript <RememberNPC> (charToMove);
				}
			}

			if (!isPlayer)
			{
				AssignConstantID <Char> (charToMove, charToMoveID, charToMoveParameterID);
			}
			AssignConstantID <Marker> (marker, markerID, markerParameterID);
		}

		
		public override string SetLabel ()
		{
			if (marker != null)
			{
				if (charToMove != null)
				{
					return charToMove.name + " to " + marker.name;
				}
				else if (isPlayer)
				{
					return "Player to " + marker.name;
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
			if (markerParameterID < 0)
			{
				if (marker != null && marker.gameObject == _gameObject) return true;
				if (markerID == id) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: Move to point' Action with key variables already set.</summary>
		 * <param name = "charToMove">The character to move</param>
		 * <param name = "marker">The Marker to move the character to</param>
		 * <param name = "pathSpeed">How fast the character moves (Walk, Run)</param>
		 * <param name = "usePathfinding">If True, the character will rely on pathfinding to reach the Marker</param>
		 * <param name = "waitUntilFinish">If True, the Action will wait until the character has reached the Marker</param>
		 * <param name = "turnToFaceAfter">If True, and waitUntilFinish = true, then the character will face the same direction that the Marker is facing after reaching it</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharPathFind CreateNew (Char charToMove, Marker marker, PathSpeed pathSpeed = PathSpeed.Walk, bool usePathfinding = true, bool waitUntilFinish = true, bool turnToFaceAfter = false)
		{
			ActionCharPathFind newAction = (ActionCharPathFind) CreateInstance <ActionCharPathFind>();
			newAction.charToMove = charToMove;
			newAction.marker = marker;
			newAction.speed = pathSpeed;
			newAction.pathFind = usePathfinding;
			newAction.willWait = waitUntilFinish;
			newAction.faceAfter = turnToFaceAfter;
			return newAction;
		}
		
	}

}