/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionCharHold.cs"
 * 
 *	This action parents a GameObject to a character's hand.
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
	public class ActionCharHold : Action
	{

		public int objectToHoldParameterID = -1;

		public int _charID = 0;
		public int objectToHoldID = 0;

		public GameObject objectToHold;

		public bool isPlayer;
		public Char _char;
		protected Char runtimeChar;

		public bool rotate90; // Deprecated
		public Vector3 localEulerAngles;
		public int localEulerAnglesParameterID = -1;

		protected GameObject loadedObject = null;
		
		public Hand hand;


		public ActionCharHold ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Character;
			title = "Hold object";
			description = "Parents a GameObject to a Character's hand Transform, as chosen in the Character's inspector. The local transforms of the GameObject will be cleared. Note that this action only works with 3D characters.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeChar = AssignFile <Char> (_charID, _char);
			objectToHold = AssignFile (parameters, objectToHoldParameterID, objectToHoldID, objectToHold);

			if (objectToHold != null && !objectToHold.activeInHierarchy)
			{
				loadedObject = (GameObject) Instantiate (objectToHold);
			}

			if (isPlayer)
			{
				runtimeChar = KickStarter.player;
			}

			Upgrade ();

			localEulerAngles = AssignVector3 (parameters, localEulerAnglesParameterID, localEulerAngles);
		}


		protected void Upgrade ()
		{
			if (rotate90)
			{
				localEulerAngles = new Vector3 (0f, 0f, 90f);
				rotate90 = false;
			}
		}


		protected GameObject GetObjectToHold ()
		{
			if (loadedObject)
			{
				return loadedObject;
			}
			return objectToHold;
		}

		
		public override float Run ()
		{
			if (runtimeChar)
			{
				if (runtimeChar.GetAnimEngine () != null && runtimeChar.GetAnimEngine ().ActionCharHoldPossible ())
				{
					if (runtimeChar.HoldObject (GetObjectToHold (), hand))
					{
						GetObjectToHold ().transform.localEulerAngles = localEulerAngles;
					}
				}
			}
			else
			{
				LogWarning ("Could not create animation engine!");
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);
			if (isPlayer)
			{
				if (Application.isPlaying)
				{
					_char = KickStarter.player;
				}
				else if (AdvGame.GetReferences ().settingsManager)
				{
					_char = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
				}
				else
				{
					EditorGUILayout.HelpBox ("A Settings Manager and player must be defined", MessageType.Warning);
				}
			}
			else
			{
				_char = (Char) EditorGUILayout.ObjectField ("Character:", _char, typeof (Char), true);
					
				_charID = FieldToID <Char> (_char, _charID);
				_char = IDToField <Char> (_char, _charID, true);
			}
			
			if (_char)
			{
				if (_char.GetAnimEngine () && _char.GetAnimEngine ().ActionCharHoldPossible ())
				{
					objectToHoldParameterID = Action.ChooseParameterGUI ("Object to hold:", parameters, objectToHoldParameterID, ParameterType.GameObject);
					if (objectToHoldParameterID >= 0)
					{
						objectToHoldID = 0;
						objectToHold = null;
					}
					else
					{
						objectToHold = (GameObject) EditorGUILayout.ObjectField ("Object to hold:", objectToHold, typeof (GameObject), true);
						
						objectToHoldID = FieldToID (objectToHold, objectToHoldID);
						objectToHold = IDToField (objectToHold, objectToHoldID, false);
					}
					
					hand = (Hand) EditorGUILayout.EnumPopup ("Hand:", hand);

					Upgrade ();

					localEulerAnglesParameterID = Action.ChooseParameterGUI ("Object local angles:", parameters, localEulerAnglesParameterID, ParameterType.Vector3);
					if (localEulerAnglesParameterID < 0)
					{
						localEulerAngles = EditorGUILayout.Vector3Field ("Object local angles:", localEulerAngles);
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("This Action is not compatible with this Character's Animation Engine.", MessageType.Info);
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("This Action requires a Character before more options will show.", MessageType.Info);
			}
			
			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				if (!isPlayer && _char != null && _char.GetComponent <NPC>())
				{
					AddSaveScript <RememberNPC> (_char);
				}

				AddSaveScript <RememberTransform> (objectToHold);
				if (objectToHold != null && objectToHold.GetComponent <RememberTransform>())
				{
					objectToHold.GetComponent <RememberTransform>().saveParent = true;
					if (objectToHold.transform.parent)
					{
						AddSaveScript <ConstantID> (objectToHold.transform.parent.gameObject);
					}
				}
			}

			if (!isPlayer)
			{
				AssignConstantID <Char> (_char, _charID, 0);
			}
			AssignConstantID (objectToHold, objectToHoldID, objectToHoldParameterID);
		}

		
		public override string SetLabel ()
		{
			if (_char != null && objectToHold != null)
			{
				return _char.name + " hold " + objectToHold.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer)
			{
				if (_char != null && _char.gameObject == _gameObject) return true;
				if (_charID == id) return true;
			}
			if (isPlayer && _gameObject.GetComponent <Player>() != null) return true;
			if (objectToHoldParameterID < 0)
			{
				if (objectToHold != null && objectToHold == _gameObject) return true;
				if (objectToHoldID == id) return true;
			}
			return false;
		}
		
		#endif



		/**
		 * <summary>Creates a new instance of the 'Character: Hold object' Action with key variables already set.</summary>
		 * <param name = "characterToUpdate">The character who will hold the object</param>
		 * <param name = "objectToHold">The object that the character is to hold</param>
		 * <param name = "handToUse">Which hand to place the object in (Left, Right)</param>
		 * <param name = "localEulerAngles">The euler angles to apply locally to the object being held</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharHold CreateNew (Char characterToUpdate, GameObject objectToHold, Hand handToUse, Vector3 localEulerAngles = default(Vector3))
		{
			ActionCharHold newAction = (ActionCharHold) CreateInstance <ActionCharHold>();
			newAction._char = characterToUpdate;
			newAction.objectToHold = objectToHold;
			newAction.hand = handToUse;
			newAction.localEulerAngles = localEulerAngles;
			return newAction;
		}
		
		
	}

}