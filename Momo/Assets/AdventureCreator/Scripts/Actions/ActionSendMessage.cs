/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionSendMessage.cs"
 * 
 *	This action calls "SendMessage" on a GameObject.
 *	Both standard messages, and custom ones with paremeters, can be sent.
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
	public class ActionSendMessage : Action
	{
		
		public int constantID = 0;
		public int parameterID = -1;
		public bool isPlayer;
		public GameObject linkedObject;
		protected GameObject runtimeLinkedObject;

		public bool affectChildren = false;
		
		public MessageToSend messageToSend;
		public enum MessageToSend { TurnOn, TurnOff, Interact, Kill, Custom };

		public int customMessageParameterID = -1;
		public string customMessage;

		public bool sendValue;

		public int customValueParameterID = -1;
		public int customValue;

		public bool ignoreWhenSkipping = false;


		public ActionSendMessage ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Object;
			title = "Send message";
			description = "Sends a given message to a GameObject. Can be either a message commonly-used by Adventure Creator (Interact, TurnOn, etc) or a custom one, with an integer argument.";
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

			customMessage = AssignString (parameters, customMessageParameterID, customMessage);
			customValue = AssignInteger (parameters, customValueParameterID, customValue);
		}
		
		
		public override float Run ()
		{
			if (runtimeLinkedObject != null)
			{
				if (messageToSend == MessageToSend.Custom)
				{
					if (affectChildren)
					{
						if (!sendValue)
						{
							runtimeLinkedObject.BroadcastMessage (customMessage, SendMessageOptions.DontRequireReceiver);
						}
						else
						{
							runtimeLinkedObject.BroadcastMessage (customMessage, customValue, SendMessageOptions.DontRequireReceiver);
						}
					}
					else
					{
						if (!sendValue)
						{
							runtimeLinkedObject.SendMessage (customMessage);
						}
						else
						{
							runtimeLinkedObject.SendMessage (customMessage, customValue);
						}
					}
				}
				else
				{
					if (affectChildren)
					{
						runtimeLinkedObject.BroadcastMessage (messageToSend.ToString (), SendMessageOptions.DontRequireReceiver);
					}
					else
					{
						runtimeLinkedObject.SendMessage (messageToSend.ToString ());
					}
				}
			}
			
			return 0f;
		}
		
		
		public override void Skip ()
		{
			if (!ignoreWhenSkipping)
			{
				Run ();
			}
		}
		
		
		public override ActionEnd End (List<AC.Action> actions)
		{
			// If the linkedObject is an immediately-starting ActionList, don't end the cutscene
			if (runtimeLinkedObject && messageToSend == MessageToSend.Interact)
			{
				Cutscene tempAction = runtimeLinkedObject.GetComponent<Cutscene>();
				if (tempAction != null && tempAction.triggerTime <= 0f)
				{
					ActionEnd actionEnd = new ActionEnd ();
					actionEnd.resultAction = ResultAction.RunCutscene;
					return actionEnd;
				}
			}
			
			return (base.End (actions));
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Send to Player?", isPlayer);
			if (!isPlayer)
			{
				parameterID = Action.ChooseParameterGUI ("Object to affect:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					linkedObject = null;
				}
				else
				{
					linkedObject = (GameObject) EditorGUILayout.ObjectField ("Object to affect:", linkedObject, typeof(GameObject), true);
					
					constantID = FieldToID (linkedObject, constantID);
					linkedObject = IDToField  (linkedObject, constantID, false);
				}
			}

			messageToSend = (MessageToSend) EditorGUILayout.EnumPopup ("Message to send:", messageToSend);
			if (messageToSend == MessageToSend.Custom)
			{
				customMessageParameterID = Action.ChooseParameterGUI ("Method name:", parameters, customMessageParameterID, ParameterType.String);
				if (customMessageParameterID < 0)
				{
					customMessage = EditorGUILayout.TextField ("Method name:", customMessage);
				}
				
				sendValue = EditorGUILayout.Toggle ("Pass integer to method?", sendValue);
				if (sendValue)
				{
					customValueParameterID = Action.ChooseParameterGUI ("Integer to send:", parameters, customValueParameterID, ParameterType.Integer);
					if (customValueParameterID < 0)
					{
						customValue = EditorGUILayout.IntField ("Integer to send:", customValue);
					}
				}
			}
			
			affectChildren = EditorGUILayout.Toggle ("Send to children too?", affectChildren);
			ignoreWhenSkipping = EditorGUILayout.Toggle ("Ignore when skipping?", ignoreWhenSkipping);
			
			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID (linkedObject, constantID, parameterID);
		}
		
		
		public override string SetLabel ()
		{
			if (linkedObject != null)
			{
				string labelAdd = string.Empty;
				if (messageToSend == MessageToSend.TurnOn)
				{
					labelAdd = "'Turn on' ";
				}
				else if (messageToSend == MessageToSend.TurnOff)
				{
					labelAdd = "'Turn off' ";
				}
				else if (messageToSend == MessageToSend.Interact)
				{
					labelAdd = "'Interact' ";
				}
				else if (messageToSend == MessageToSend.Kill)
				{
					labelAdd = "'Kill' ";
				}
				else
				{
					labelAdd = "'" + customMessage + "' ";
				}
				
				labelAdd += "to " + linkedObject.name;
				return labelAdd;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (linkedObject != null && linkedObject == gameObject) return true;
				if (constantID == id) return true;
			}
			if (isPlayer && gameObject.GetComponent <Player>() != null) return true;
			return false;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Send message' Action</summary>
		 * <param name = "receivingObject">The GameObject to send the message to</param>
		 * <param name = "messageName">The message to send</param>
		 * <param name = "affectChildren">If True, the message will be broadcast to all child GameObjects as well</param>
		 * <param name = "ignoreWhenSkipping">If True, the message will not be send if the ActionList is being skipped</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSendMessage CreateNew (GameObject receivingObject, string messageName, bool affectChildren = false, bool ignoreWhenSkipping = false)
		{
			ActionSendMessage newAction = (ActionSendMessage) CreateInstance <ActionSendMessage>();
			newAction.linkedObject = receivingObject;
			newAction.messageToSend = MessageToSend.Custom;
			newAction.customMessage = messageName;
			newAction.sendValue = false;
			newAction.affectChildren = affectChildren;
			newAction.ignoreWhenSkipping = ignoreWhenSkipping;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Send message' Action</summary>
		 * <param name = "receivingObject">The GameObject to send the message to</param>
		 * <param name = "messageName">The message to send</param>
		 * <param name = "parameterValue">An integer value to pass as a parameter</param>
		 * <param name = "affectChildren">If True, the message will be broadcast to all child GameObjects as well</param>
		 * <param name = "ignoreWhenSkipping">If True, the message will not be send if the ActionList is being skipped</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSendMessage CreateNew (GameObject receivingObject, string messageName, int parameterValue, bool affectChildren = false, bool ignoreWhenSkipping = false)
		{
			ActionSendMessage newAction = (ActionSendMessage) CreateInstance <ActionSendMessage>();
			newAction.linkedObject = receivingObject;
			newAction.messageToSend = MessageToSend.Custom;
			newAction.customMessage = messageName;
			newAction.sendValue = true;
			newAction.customValue = parameterValue;
			newAction.affectChildren = affectChildren;
			newAction.ignoreWhenSkipping = ignoreWhenSkipping;
			return newAction;
		}
		
	}
	
}