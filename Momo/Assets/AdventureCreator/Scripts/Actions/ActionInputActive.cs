/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionInputCheck.cs"
 * 
 *	This action checks if a specific key
 *	is being pressed
 * 
 */

using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionInputActive : Action
	{

		public int activeInputID;
		public bool newState;

		
		public ActionInputActive ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Input;
			title = "Toggle active";
			description = "Enables or disables an Active Input";
		}


		public override float Run ()
		{
			if (KickStarter.settingsManager != null && KickStarter.settingsManager.activeInputs != null)
			{
				foreach (ActiveInput activeInput in KickStarter.settingsManager.activeInputs)
				{
					if (activeInput.ID == activeInputID)
					{
						activeInput.IsEnabled = newState;
						return 0f;
					}
				}

				LogWarning ("Couldn't find the Active Input with ID=" + activeInputID);
				return 0f;
			}

			LogWarning ("No Active Inputs found! Is the Settings Manager assigned properly?");
			return 0f;
		}
		

		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			int tempNumber = -1;

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.activeInputs != null && KickStarter.settingsManager.activeInputs.Count > 0)
			{
				ActiveInput.Upgrade ();

				string[] labelList = new string[KickStarter.settingsManager.activeInputs.Count];
				for (int i=0; i<KickStarter.settingsManager.activeInputs.Count; i++)
				{
					labelList[i] = i.ToString () + ": " + KickStarter.settingsManager.activeInputs[i].Label;

					if (KickStarter.settingsManager.activeInputs[i].ID == activeInputID)
					{
						tempNumber = i;
					}
				}

				if (tempNumber == -1)
				{
					// Wasn't found (was deleted?), so revert to zero
					if (activeInputID != 0)
						ACDebug.LogWarning ("Previously chosen active input no longer exists!");
					tempNumber = 0;
					activeInputID = 0;
				}

				tempNumber = EditorGUILayout.Popup ("Active input:", tempNumber, labelList);
				activeInputID = KickStarter.settingsManager.activeInputs [tempNumber].ID;
				newState = EditorGUILayout.Toggle ("New state:", newState);
			}
			else
			{
				EditorGUILayout.HelpBox ("No active inputs exist! They can be defined in Adventure Creator -> Editors -> Active Inputs.", MessageType.Info);
				activeInputID = 0;
				tempNumber = 0;
			}

			AfterRunningOption ();
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Input: Toggle active' Action</summary>
		 * <param name = "activeInputID">The ID number of the active input to affect</param>
		 * <param name = "changeType">The type of change to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInputActive CreateNew (int activeInputID, ChangeType changeType)
		{
			ActionInputActive newAction = (ActionInputActive) CreateInstance <ActionInputActive>();
			newAction.activeInputID = activeInputID;
			newAction.newState = (changeType == ChangeType.Enable);
			return newAction;
		}
		
	}
	
}