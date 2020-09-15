/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionCharFace.cs"
 * 
 *	This action is used to make characters turn to face GameObjects.
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
	public class ActionCharFace : Action
	{

		public int charToMoveParameterID = -1;
		public int faceObjectParameterID = -1;

		public int charToMoveID = 0;
		public int faceObjectID = 0;

		public bool isInstant;
		public Char charToMove;
		protected Char runtimeCharToMove;
		public GameObject faceObject;
		protected GameObject runtimeFaceObject;
		public bool copyRotation;
		public bool facePlayer;
		
		public CharFaceType faceType = CharFaceType.Body;
		public bool isPlayer;
		public bool lookUpDown;
		public bool stopLooking = false;

		public bool lookAtHead = false;


		public ActionCharFace ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Character;
			title = "Face object";
			description = "Makes a Character turn, either instantly or over time. Can turn to face another object, or copy that object's facing direction.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeCharToMove = AssignFile <Char> (parameters, charToMoveParameterID, charToMoveID, charToMove);
			runtimeFaceObject = AssignFile (parameters, faceObjectParameterID, faceObjectID, faceObject);

			if (isPlayer)
			{
				runtimeCharToMove = KickStarter.player;
			}
			else if (facePlayer && KickStarter.player)
			{
				runtimeFaceObject = KickStarter.player.gameObject;
			}
		}

		
		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;
			
				if (runtimeFaceObject == null && (faceType == CharFaceType.Body || (faceType == CharFaceType.Head && !stopLooking)))
				{
					return 0f;
				}

				if (runtimeCharToMove)
				{
					if (faceType == CharFaceType.Body)
					{
						if (!isInstant && runtimeCharToMove.IsMovingAlongPath ())
						{
							runtimeCharToMove.EndPath ();
						}

						if (lookUpDown && isPlayer && KickStarter.settingsManager.IsInFirstPerson ())
						{
							Player player = (Player) runtimeCharToMove;
							player.SetTilt (runtimeFaceObject.transform.position, isInstant);
						}

						runtimeCharToMove.SetLookDirection (GetLookVector (KickStarter.settingsManager), isInstant);
					}
					else if (faceType == CharFaceType.Head)
					{
						if (stopLooking)
						{
							runtimeCharToMove.ClearHeadTurnTarget (isInstant, HeadFacing.Manual);
						}
						else
						{
							Vector3 offset = Vector3.zero;

							Hotspot faceObjectHotspot = runtimeFaceObject.GetComponent <Hotspot>();
							Char faceObjectChar = runtimeFaceObject.GetComponent <Char>();

							if (lookAtHead && faceObjectChar != null)
							{
								Transform neckBone = faceObjectChar.neckBone;
								if (neckBone != null)
								{
									runtimeFaceObject = neckBone.gameObject;
								}
								else
								{
									Log ("Cannot look at " + faceObjectChar.name + "'s head as their 'Neck bone' has not been defined.", faceObjectChar);
								}
							}
							else if (faceObjectHotspot != null)
							{
								if (faceObjectHotspot.centrePoint != null)
								{
									runtimeFaceObject = faceObjectHotspot.centrePoint.gameObject;
								}
								else
								{
									offset = faceObjectHotspot.GetIconPosition (true);
								}
							}

							runtimeCharToMove.SetHeadTurnTarget (runtimeFaceObject.transform, offset, isInstant);
						}
					}

					if (isInstant)
					{
						return 0f;
					}
					else
					{
						if (willWait)
						{
							return (defaultPauseTime);
						}
						else
						{
							return 0f;
						}
					}
				}

				return 0f;
			}
			else
			{
				if (faceType == CharFaceType.Head && runtimeCharToMove.IsMovingHead ())
				{
					return defaultPauseTime;
				}
				else if (faceType == CharFaceType.Body && runtimeCharToMove.IsTurning ())
				{
					return defaultPauseTime;
				}
				else
				{
					isRunning = false;
					return 0f;
				}
			}
		}


		public override void Skip ()
		{
			if (runtimeFaceObject == null && (faceType == CharFaceType.Body || (faceType == CharFaceType.Head && !stopLooking)))
			{
				return;
			}
			
			if (runtimeCharToMove)
			{
				if (faceType == CharFaceType.Body)
				{
					runtimeCharToMove.SetLookDirection (GetLookVector (KickStarter.settingsManager), true);
					
					if (lookUpDown && isPlayer && KickStarter.settingsManager.IsInFirstPerson ())
					{
						Player player = (Player) runtimeCharToMove;
						player.SetTilt (runtimeFaceObject.transform.position, true);
					}
				}

				else if (faceType == CharFaceType.Head)
				{
					if (stopLooking)
					{
						runtimeCharToMove.ClearHeadTurnTarget (true, HeadFacing.Manual);
					}
					else
					{
						Vector3 offset = Vector3.zero;
						if (runtimeFaceObject.GetComponent <Hotspot>())
						{
							offset = runtimeFaceObject.GetComponent <Hotspot>().GetIconPosition (true);
						}
						else if (lookAtHead && runtimeFaceObject.GetComponent <Char>())
						{
							Transform neckBone = runtimeFaceObject.GetComponent <Char>().neckBone;
							if (neckBone != null)
							{
								runtimeFaceObject = neckBone.gameObject;
							}
							else
							{
								ACDebug.Log ("Cannot look at " + runtimeFaceObject.name + "'s head as their 'Neck bone' has not been defined.", runtimeFaceObject);
							}
						}

						runtimeCharToMove.SetHeadTurnTarget (runtimeFaceObject.transform, offset, true);
					}
				}
			}
		}

		
		protected Vector3 GetLookVector (SettingsManager settingsManager)
		{
			if (copyRotation)
			{
				return runtimeFaceObject.transform.forward;
			}
			else if (SceneSettings.ActInScreenSpace ())
			{
				return AdvGame.GetScreenDirection (runtimeCharToMove.transform.position, runtimeFaceObject.transform.position);
			}
			return runtimeFaceObject.transform.position - runtimeCharToMove.transform.position;
		}


		protected GameObject GetActualHeadFaceObject (GameObject _originalObject)
		{
			Hotspot faceObjectHotspot = _originalObject.GetComponent <Hotspot>();
			Char faceObjectChar = _originalObject.GetComponent <Char>();

			if (lookAtHead && faceObjectChar != null)
			{
				Transform neckBone = faceObjectChar.neckBone;
				if (neckBone != null)
				{
					return neckBone.gameObject;
				}
			}
			else if (faceObjectHotspot != null)
			{
				if (faceObjectHotspot.centrePoint != null)
				{
					return faceObjectHotspot.centrePoint.gameObject;
				}
			}
			return _originalObject;
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Affect Player?", isPlayer);
			if (!isPlayer)
			{
				charToMoveParameterID = Action.ChooseParameterGUI ("Character to turn:", parameters, charToMoveParameterID, ParameterType.GameObject);
				if (charToMoveParameterID >= 0)
				{
					charToMoveID = 0;
					charToMove = null;
				}
				else
				{
					charToMove = (Char) EditorGUILayout.ObjectField ("Character to turn:", charToMove, typeof(Char), true);
					
					charToMoveID = FieldToID <Char> (charToMove, charToMoveID);
					charToMove = IDToField <Char> (charToMove, charToMoveID, false);
				}
			}
			else
			{
				facePlayer = false;
			}

			faceType = (CharFaceType) EditorGUILayout.EnumPopup ("Face with:", faceType);

			if (!isPlayer)
			{
				facePlayer = EditorGUILayout.Toggle ("Face player?", facePlayer);
			}
			else
			{
				SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
				if (faceType == CharFaceType.Body && settingsManager && settingsManager.IsInFirstPerson ())
				{
					lookUpDown = EditorGUILayout.Toggle ("Affect head pitch?", lookUpDown);
				}
			}

			if (faceType == CharFaceType.Head)
			{
				stopLooking = EditorGUILayout.Toggle ("Stop looking?", stopLooking);
			}

			if (facePlayer || (faceType == CharFaceType.Head && stopLooking))
			{ }
			else
			{
				faceObjectParameterID = Action.ChooseParameterGUI ("Object to face:", parameters, faceObjectParameterID, ParameterType.GameObject);
				if (faceObjectParameterID >= 0)
				{
					faceObjectID = 0;
					faceObject = null;
				}
				else
				{
					faceObject = (GameObject) EditorGUILayout.ObjectField ("Object to face:", faceObject, typeof(GameObject), true);
					
					faceObjectID = FieldToID (faceObject, faceObjectID);
					faceObject = IDToField (faceObject, faceObjectID, false);
				}
			}

			if (faceType == CharFaceType.Body)
			{
				copyRotation = EditorGUILayout.Toggle ("Use object's rotation?", copyRotation);
			}
			else if (faceType == CharFaceType.Head && !stopLooking)
			{
				if (facePlayer || (faceObject != null && faceObject.GetComponent <Char>()))
				{
					lookAtHead = EditorGUILayout.Toggle ("Look at character's head?", lookAtHead);
				}
				else if (faceObjectParameterID >= 0)
				{
					lookAtHead = EditorGUILayout.Toggle ("If a character, look at head?", lookAtHead);
				}
			}

			isInstant = EditorGUILayout.Toggle ("Is instant?", isInstant);
			if (!isInstant)
			{
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
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
				if (faceType == CharFaceType.Head && faceObject != null)
				{
					AddSaveScript <ConstantID> (GetActualHeadFaceObject (faceObject));
				}
			}

			if (!isPlayer)
			{
				AssignConstantID <Char> (charToMove, charToMoveID, charToMoveParameterID);
			}
			AssignConstantID (faceObject, faceObjectID, faceObjectParameterID);
		}

		
		public override string SetLabel ()
		{
			if (faceObject != null)
			{
				if (charToMove != null)
				{
					return charToMove.name + " to " + faceObject.name;
				}
				else if (isPlayer)
				{
					return "Player to " + faceObject.name;
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
			if (!facePlayer && faceObjectParameterID < 0)
			{
				if (faceObject != null && faceObject.gameObject == _gameObject) return true;
				if (faceObjectID == id) return true;
			}
			if (facePlayer && _gameObject.GetComponent <Player>() != null) return true;
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: Face object' Action, set to turn a character's body towards an object</summary>
		 * <param name = "characterToTurn">The character to turn</param>
		 * <param name = "objectToFace">The GameObject to face</param>
		 * <param name = "useObjectRotation">If True, the character will face the same direction as the GameObject, as opposed to facing the GameObject itself</param>
		 * <param name = "isInstant">If True, the character will turn instantly</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the turn is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharFace CreateNew_BodyFace (AC.Char characterToTurn, GameObject objectToFace, bool useObjectRotation = false, bool isInstant = false, bool waitUntilFinish = false)
		{
			ActionCharFace newAction = (ActionCharFace) CreateInstance <ActionCharFace>();
			newAction.charToMove = characterToTurn;
			newAction.faceType = CharFaceType.Body;
			newAction.faceObject = objectToFace;
			newAction.copyRotation = useObjectRotation;
			newAction.isInstant = isInstant;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Face object' Action, set to turn a character's head towards an object</summary>
		 * <param name = "characterToTurn">The character to turn</param>
		 * <param name = "objectToFace">The GameObject to face</param>
		 * <param name = "isInstant">If True, the character will turn instantly</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the turn is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharFace CreateNew_HeadTurn (AC.Char characterToTurn, GameObject objectToFace, bool faceHeadIfCharacter = true, bool isInstant = false, bool waitUntilFinish = false)
		{
			ActionCharFace newAction = (ActionCharFace) CreateInstance <ActionCharFace>();
			newAction.charToMove = characterToTurn;
			newAction.faceType = CharFaceType.Head;
			newAction.faceObject = objectToFace;
			newAction.lookAtHead = true;
			newAction.isInstant = isInstant;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Face object' Action, set to stop a character's head-turning</summary>
		 * <param name = "characterToTurn">The character to affect</param>
		 * <param name = "isInstant">If True, the character will stop turning their head instantly</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharFace CreateNew_HeadStop (AC.Char characterToTurn, bool isInstant = false)
		{
			ActionCharFace newAction = (ActionCharFace) CreateInstance <ActionCharFace>();
			newAction.charToMove = characterToTurn;
			newAction.faceType = CharFaceType.Head;
			newAction.stopLooking = true;
			newAction.isInstant = isInstant;
			return newAction;
		}
		
	}

}