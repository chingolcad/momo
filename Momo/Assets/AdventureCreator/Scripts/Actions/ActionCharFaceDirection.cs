/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionCharDirection.cs"
 * 
 *	This action is used to make characters turn to face fixed directions relative to the camera.
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
	public class ActionCharFaceDirection : Action
	{
		
		public int charToMoveParameterID = -1;

		public int charToMoveID = 0;

		public bool isInstant;
		public CharDirection direction;

		public Char charToMove;
		protected Char runtimeCharToMove;

		public bool isPlayer;
		[SerializeField] protected RelativeTo relativeTo = RelativeTo.Camera;
		public enum RelativeTo { Camera, Character };


		public ActionCharFaceDirection ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Character;
			title = "Face direction";
			description = "Makes a Character turn, either instantly or over time, to face a direction relative to the camera – i.e. up, down, left or right.";
		}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeCharToMove = AssignFile <Char> (parameters, charToMoveParameterID, charToMoveID, charToMove);

			if (isPlayer)
			{
				runtimeCharToMove = KickStarter.player;
			}
		}


		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;
				
				if (runtimeCharToMove != null)
				{
					if (!isInstant && runtimeCharToMove.IsMovingAlongPath ())
					{
						runtimeCharToMove.EndPath ();
					}

					Vector3 lookVector = AdvGame.GetCharLookVector (direction, (relativeTo == RelativeTo.Character) ? runtimeCharToMove : null);
					runtimeCharToMove.SetLookDirection (lookVector, isInstant);

					if (!isInstant)
					{
						if (willWait)
						{
							return (defaultPauseTime);
						}
					}
				}
				
				return 0f;
			}
			else
			{
				if (runtimeCharToMove.IsTurning ())
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
			if (runtimeCharToMove != null)
			{
				Vector3 lookVector = AdvGame.GetCharLookVector (direction, (relativeTo == RelativeTo.Character) ? runtimeCharToMove : null);
				runtimeCharToMove.SetLookDirection (lookVector, true);
			}
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

			direction = (CharDirection) EditorGUILayout.EnumPopup ("Direction to face:", direction);
			relativeTo = (RelativeTo) EditorGUILayout.EnumPopup ("Direction is relative to:", relativeTo);
			isInstant = EditorGUILayout.Toggle ("Is instant?", isInstant);
			if (!isInstant)
			{
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			}
			
			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (!isPlayer)
			{
				if (saveScriptsToo && charToMove != null && charToMove.GetComponent <NPC>())
				{
					AddSaveScript <RememberNPC> (charToMove);
				}

				AssignConstantID <Char> (charToMove, charToMoveID, charToMoveParameterID);
			}
		}

		
		public override string SetLabel ()
		{
			if (charToMove != null)
			{
				return charToMove.name + " - " + direction;
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
			return false;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: Face direction' Action</summary>
		 * <param name = "characterToTurn">The character to affect</param>
		 * <param name = "directionToFace">The direction to face</param>
		 * <param name = "relativeTo">What the supplied direction is relative to</param>
		 * <param name = "isInstant">If True, the character will stop turning their head instantly</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharFaceDirection CreateNew (AC.Char characterToTurn, CharDirection directionToFace, RelativeTo relativeTo = RelativeTo.Camera, bool isInstant = false, bool waitUntilFinish = false)
		{
			ActionCharFaceDirection newAction = (ActionCharFaceDirection) CreateInstance <ActionCharFaceDirection>();
			newAction.charToMove = characterToTurn;
			newAction.direction = directionToFace;
			newAction.relativeTo = relativeTo;
			newAction.isInstant = isInstant;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}
		
	}

}