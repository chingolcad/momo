/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionInteraction.cs"
 * 
 *	This Action can enable and disable
 *	a Hotspot's individual Interactions.
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
	public class ActionInteraction : Action
	{

		public int parameterID = -1;
		public int constantID = 0;
		public Hotspot hotspot;
		protected Hotspot runtimeHotspot;

		public InteractionType interactionType;
		public ChangeType changeType = ChangeType.Enable;
		public int number = 0;

		
		public ActionInteraction ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Hotspot;
			title = "Change interaction";
			description = "Enables and disables individual Interactions on a Hotspot.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeHotspot = AssignFile <Hotspot> (parameters, parameterID, constantID, hotspot);
		}

		
		public override float Run ()
		{
			if (runtimeHotspot == null)
			{
				return 0f;
			}

			if (interactionType == InteractionType.Use)
			{
				if (runtimeHotspot.useButtons.Count > number)
				{
					ChangeButton (runtimeHotspot.useButtons [number]);
				}
				else
				{
					LogWarning ("Cannot change Hotspot " + runtimeHotspot.gameObject.name + "'s Use button " + number.ToString () + " because it doesn't exist!");
				}
			}
			else if (interactionType == InteractionType.Examine)
			{
				ChangeButton (runtimeHotspot.lookButton);
			}
			else if (interactionType == InteractionType.Inventory)
			{
				if (runtimeHotspot.invButtons.Count > number)
				{
					ChangeButton (runtimeHotspot.invButtons [number]);
				}
				else
				{
					LogWarning ("Cannot change Hotspot " + runtimeHotspot.gameObject.name + "'s Inventory button " + number.ToString () + " because it doesn't exist!");
				}
			}
			runtimeHotspot.ResetMainIcon ();

			return 0f;
		}


		protected void ChangeButton (AC.Button button)
		{
			if (button == null)
			{
				return;
			}

			if (changeType == ChangeType.Enable)
			{
				button.isDisabled = false;
			}
			else if (changeType == ChangeType.Disable)
			{
				button.isDisabled = true;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().settingsManager)
			{
				parameterID = Action.ChooseParameterGUI ("Hotspot to change:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					hotspot = null;
				}
				else
				{
					hotspot = (Hotspot) EditorGUILayout.ObjectField ("Hotspot to change:", hotspot, typeof (Hotspot), true);
					
					constantID = FieldToID <Hotspot> (hotspot, constantID);
					hotspot = IDToField <Hotspot> (hotspot, constantID, false);
				}

				interactionType = (InteractionType) EditorGUILayout.EnumPopup ("Interaction to change:", interactionType);

				if ((!isAssetFile && hotspot != null) || isAssetFile)
				{
					if (interactionType == InteractionType.Use)
					{
						if (isAssetFile)
						{
							number = EditorGUILayout.IntField ("Use interaction:", number);
						}
						else if (AdvGame.GetReferences ().cursorManager)
						{
							// Multiple use interactions
							List<string> labelList = new List<string>();
							
							foreach (AC.Button button in hotspot.useButtons)
							{
								labelList.Add (hotspot.useButtons.IndexOf (button) + ": " + AdvGame.GetReferences ().cursorManager.GetLabelFromID (button.iconID, 0));
							}
							
							number = EditorGUILayout.Popup ("Use interaction:", number, labelList.ToArray ());
						}
						else
						{
							EditorGUILayout.HelpBox ("A Cursor Manager is required.", MessageType.Warning);
						}
					}
					else if (interactionType == InteractionType.Inventory)
					{
						if (isAssetFile)
						{
							number = EditorGUILayout.IntField ("Inventory interaction:", number);
						}
						else if (AdvGame.GetReferences ().inventoryManager)
						{
							List<string> labelList = new List<string>();

							foreach (AC.Button button in hotspot.invButtons)
							{
								labelList.Add (hotspot.invButtons.IndexOf (button) + ": " + AdvGame.GetReferences ().inventoryManager.GetLabel (button.invID));
							}

							number = EditorGUILayout.Popup ("Inventory interaction:", number, labelList.ToArray ());
						}
						else
						{
							EditorGUILayout.HelpBox ("An Inventory Manager is required.", MessageType.Warning);
						}
					}
				}

				changeType = (ChangeType) EditorGUILayout.EnumPopup ("Change to make:", changeType);
			}
			else
			{
				EditorGUILayout.HelpBox ("A Settings Manager is required for this Action.", MessageType.Warning);
			}

			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberHotspot> (hotspot);
			}

			AssignConstantID <Hotspot> (hotspot, constantID, parameterID);
		}
		
		
		public override string SetLabel ()
		{
			if (hotspot != null)
			{
				return hotspot.name + " - " + changeType + " " + interactionType;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (hotspot != null && hotspot.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			return false;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Hotspot: Change interaction' Action</summary>
		 * <param name = "hotspot">The Hotspot to affect</param>
		 * <param name = "changeType">What kind of change to make</param>
		 * <param name = "interactionType">The type of Hotspot interaction to affect</param>
		 * <param name = "interactionIndex">The index number of the interactions to affect</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInteraction CreateNew (Hotspot hotspot, ChangeType changeType, InteractionType interactionType, int interactionIndex = 0)
		{
			ActionInteraction newAction = (ActionInteraction) CreateInstance <ActionInteraction>();
			newAction.hotspot = hotspot;
			newAction.interactionType = interactionType;
			newAction.changeType = changeType;
			newAction.number = interactionIndex;

			return newAction;
		}
		
	}

}