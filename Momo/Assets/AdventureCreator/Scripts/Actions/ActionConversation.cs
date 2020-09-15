/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionConversation.cs"
 * 
 *	This action turns on a conversation.
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
	public class ActionConversation : ActionCheckMultiple
	{

		public int parameterID = -1;
		public int constantID = 0;
		public Conversation conversation;
		protected Conversation runtimeConversation;

		public bool overrideOptions = false;


		public ActionConversation ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Dialogue;
			title = "Start conversation";
			description = "Enters Conversation mode, and displays the available dialogue options in a specified conversation.";
			numSockets = 0;
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeConversation = AssignFile <Conversation> (parameters, parameterID, constantID, conversation);
		}

		
		public override float Run ()
		{
			if (runtimeConversation == null)
			{
				return 0f;
			}

			if (isRunning)
			{
				if (runtimeConversation.IsActive (true))
				{
					return defaultPauseTime;
				}

				isRunning = false;
				return 0f;
			}

			isRunning = false;

			if (overrideOptions)
			{
				if (runtimeConversation.lastOption >= 0)
				{
					KickStarter.actionListManager.ignoreNextConversationSkip = true;
					return 0f;
				}
				KickStarter.actionListManager.ignoreNextConversationSkip = false;
			}


			if (overrideOptions)
			{
				runtimeConversation.Interact (this);
			}
			else
			{
				runtimeConversation.Interact ();

				if (willWait && !KickStarter.settingsManager.allowGameplayDuringConversations)
				{
					isRunning = true;
					return defaultPauseTime;
				}
			}
			
			return 0f;
		}


		public override void Skip ()
		{
			if (KickStarter.actionListManager.ignoreNextConversationSkip)
			{
				KickStarter.actionListManager.ignoreNextConversationSkip = false;
				return;
			}

			Run ();
		}

		
		public override ActionEnd End (List<Action> actions)
		{
			if (runtimeConversation)
			{
				int _chosenOptionIndex = runtimeConversation.lastOption;
				runtimeConversation.lastOption = -1;
				if (overrideOptions && _chosenOptionIndex >= 0 && endings.Count > _chosenOptionIndex)
				{
					return endings[_chosenOptionIndex];
				}

				if (!overrideOptions && !KickStarter.settingsManager.allowGameplayDuringConversations && willWait && endings.Count > 0)
				{
					return endings[0];
				}
			}
			return GenerateStopActionEnd ();
		}
		
		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Conversation:", parameters, parameterID, ParameterType.GameObject);
			if (parameterID >= 0)
			{
				constantID = 0;
				conversation = null;
			}
			else
			{
				conversation = (Conversation) EditorGUILayout.ObjectField ("Conversation:", conversation, typeof (Conversation), true);
				
				constantID = FieldToID <Conversation> (conversation, constantID);
				conversation = IDToField <Conversation> (conversation, constantID, false);
			}

			if (conversation)
			{
				conversation.Upgrade ();
				overrideOptions = EditorGUILayout.Toggle ("Override options?", overrideOptions);

				if (overrideOptions)
				{
					numSockets = conversation.options.Count;
				}
				else
				{
					numSockets = 0;
				}
			}
			else
			{
				if (isAssetFile && overrideOptions && constantID != 0)
				{
					EditorGUILayout.HelpBox ("Cannot find linked Conversation - please open its scene file.", MessageType.Warning);
				}
				else
				{
					numSockets = 0;
				}
			}

			if (!overrideOptions && !KickStarter.settingsManager.allowGameplayDuringConversations)
			{
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
				if (willWait)
				{
					numSockets = 1;
				}
			}
		}


		protected override string GetSocketLabel (int i)
		{
			i -= 1;

			if (!overrideOptions && !KickStarter.settingsManager.allowGameplayDuringConversations && willWait)
			{
				return "After running:";
			}

			if (conversation != null && conversation.options.Count > i)
			{
				return ("'" + conversation.options[i].label + "':");
			}
			return "Option " + i.ToString () + ":";
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberConversation> (conversation);
			}
			AssignConstantID <Conversation> (conversation, constantID, parameterID);
		}

		
		public override string SetLabel ()
		{
			if (conversation != null)
			{
				return conversation.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (conversation != null && conversation.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Dialogue: Start conversation' Action</summary>
		 * <param name = "conversationToRun">The Conversation to begin</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionConversation CreateNew (Conversation conversationToRun)
		{
			ActionConversation newAction = (ActionConversation) CreateInstance <ActionConversation>();
			newAction.conversation = conversationToRun;
			return newAction;
		}

	}

}