/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionPlayMaker.cs"
 * 
 *	This action interacts with the popular
 *	PlayMaker FSM-manager.
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
	public class ActionPlayMaker : Action
	{

		public bool isPlayer;

		public int constantID = 0;
		public int parameterID = -1;
		public GameObject linkedObject;
		protected GameObject runtimeLinkedObject;

		public string fsmName;
		public int fsmNameParameterID = -1;
		public string eventName;
		public int eventNameParameterID = -1;


		public ActionPlayMaker ()
		{
			this.isDisplayed = true;
			category = ActionCategory.ThirdParty;
			title = "PlayMaker";
			description = "Calls a specified Event within a PlayMaker FSM. Note that PlayMaker is a separate Unity Asset, and the 'PlayMakerIsPresent' preprocessor must be defined for this to work.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				if (KickStarter.player != null)
				{
					runtimeLinkedObject = KickStarter.player.gameObject;
				}
			}
			else
			{
				runtimeLinkedObject = AssignFile (parameters, parameterID, constantID, linkedObject);
			}

			fsmName = AssignString (parameters, fsmNameParameterID, fsmName);
			eventName = AssignString (parameters, eventNameParameterID, eventName);
		}


		public override float Run ()
		{
			if (isPlayer && KickStarter.player == null)
			{
				LogWarning ("Cannot use Player's FSM since no Player was found!");
			}

			if (runtimeLinkedObject != null && !string.IsNullOrEmpty (eventName))
			{
				if (fsmName != "")
				{
					PlayMakerIntegration.CallEvent (runtimeLinkedObject, eventName, fsmName);
				}
				else
				{
					PlayMakerIntegration.CallEvent (runtimeLinkedObject, eventName);
				}
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (PlayMakerIntegration.IsDefinePresent ())
			{
				isPlayer = EditorGUILayout.Toggle ("Use Player's FSM?", isPlayer);
				if (!isPlayer)
				{
					parameterID = Action.ChooseParameterGUI ("Playmaker FSM:", parameters, parameterID, ParameterType.GameObject);
					if (parameterID >= 0)
					{
						constantID = 0;
						linkedObject = null;
					}
					else
					{
						linkedObject = (GameObject) EditorGUILayout.ObjectField ("Playmaker FSM:", linkedObject, typeof (GameObject), true);
						
						constantID = FieldToID (linkedObject, constantID);
						linkedObject = IDToField (linkedObject, constantID, false);
					}
				}

				fsmNameParameterID = Action.ChooseParameterGUI ("FSM to call (optional):", parameters, fsmNameParameterID, ParameterType.String);
				if (fsmNameParameterID < 0)
				{
					fsmName = EditorGUILayout.TextField ("FSM to call (optional):", fsmName);
				}
				eventNameParameterID = Action.ChooseParameterGUI ("Event to call:", parameters, eventNameParameterID, ParameterType.String);
				if (eventNameParameterID < 0)
				{
					eventName = EditorGUILayout.TextField ("Event to call:", eventName);
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("The 'PlayMakerIsPresent' Scripting Define Symbol must be listed in the\nPlayer Settings. Please set it from Edit -> Project Settings -> Player", MessageType.Warning);
			}

			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID (linkedObject, constantID, parameterID);
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (linkedObject != null && linkedObject == gameObject) return true;
				if (constantID == id) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Third Party: PlayMaker' Action</summary>
		 * <param name = "playMakerFSM">The GameObject with the Playmaker FSM to trigger</param>
		 * <param name = "eventToCall">The name of the event to trigger</param>
		 * <param name = "fsmName">The name of the FSM to call</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPlayMaker CreateNew (GameObject playmakerFSM, string eventToCall, string fsmName = "")
		{
			ActionPlayMaker newAction = (ActionPlayMaker) CreateInstance <ActionPlayMaker>();
			newAction.linkedObject = playmakerFSM;
			newAction.eventName = eventToCall;
			newAction.fsmName = fsmName;
			return newAction;
		}

	}

}