using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace AC
{

	[System.Serializable]
	public class ActionObjectiveCheckType : ActionCheckMultiple
	{

		public int objectiveID;
		public int playerID;
		public bool setPlayer;

		
		public ActionObjectiveCheckType ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Objective;
			title = "Check state type";
			description = "Queries the current state type of an objective.";
		}


		public override ActionEnd End (List<Action> actions)
		{
			Objective objective = KickStarter.inventoryManager.GetObjective (objectiveID);
			if (objective != null)
			{
				int _playerID = (setPlayer && KickStarter.inventoryManager.ObjectiveIsPerPlayer (objectiveID)) ? playerID : -1;

				ObjectiveState currentObjectiveState = KickStarter.runtimeObjectives.GetObjectiveState (objectiveID, _playerID);
				if (currentObjectiveState != null)
				{
					return ProcessResult ((int) currentObjectiveState.stateType, actions);
				}
			}
			return ProcessResult (0, actions);
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI ()
		{
			if (KickStarter.inventoryManager == null)
			{
				numSockets = 0;
				EditorGUILayout.HelpBox ("An Inventory Manager must be defined to use this Action", MessageType.Warning);
				return;
			}

			objectiveID = InventoryManager.ObjectiveSelectorList (objectiveID);

			if (KickStarter.inventoryManager.ObjectiveIsPerPlayer (objectiveID))
			{
				setPlayer = EditorGUILayout.Toggle ("Check specific Player?", setPlayer);
				if (setPlayer)
				{
					playerID = ChoosePlayerGUI (playerID);
				}
			}
		}
		

		public override string SetLabel ()
		{
			Objective objective = KickStarter.inventoryManager.GetObjective (objectiveID);
			if (objective != null)
			{
				return objective.Title;
			}			
			return string.Empty;
		}


		public override void SkipActionGUI (List<Action> actions, bool showGUI)
		{
			if (KickStarter.inventoryManager == null) return;

			numSockets = 4;

			if (numSockets < endings.Count)
			{
				endings.RemoveRange (numSockets, endings.Count - numSockets);
			}
			else if (numSockets > endings.Count)
			{
				if (numSockets > endings.Capacity)
				{
					endings.Capacity = numSockets;
				}
				for (int i=endings.Count; i<numSockets; i++)
				{
					ActionEnd newEnd = new ActionEnd ();
					if (i > 0)
					{
						newEnd.resultAction = ResultAction.Stop;
					}
					endings.Add (newEnd);
				}
			}
			
			foreach (ActionEnd ending in endings)
			{
				if (showGUI)
				{
					EditorGUILayout.Space ();
					int i = endings.IndexOf (ending);

					switch (i)
					{
						case 0:
							ending.resultAction = (ResultAction) EditorGUILayout.EnumPopup ("If Inactive:", (ResultAction) ending.resultAction);
							break;

						case 1:
							ending.resultAction = (ResultAction) EditorGUILayout.EnumPopup ("If Active:", (ResultAction) ending.resultAction);
							break;

						case 2:
							ending.resultAction = (ResultAction) EditorGUILayout.EnumPopup ("If Complete:", (ResultAction) ending.resultAction);
							break;

						case 3:
							ending.resultAction = (ResultAction) EditorGUILayout.EnumPopup ("If Failed:", (ResultAction) ending.resultAction);
							break;
					}
				}
				
				if (ending.resultAction == ResultAction.RunCutscene && showGUI)
				{
					if (isAssetFile)
					{
						ending.linkedAsset = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList to run:", ending.linkedAsset, typeof (ActionListAsset), false);
					}
					else
					{
						ending.linkedCutscene = (Cutscene) EditorGUILayout.ObjectField ("Cutscene to run:", ending.linkedCutscene, typeof (Cutscene), true);
					}
				}
				else if (ending.resultAction == ResultAction.Skip)
				{
					SkipActionGUI (ending, actions, showGUI);
				}
			}
		}

		#endif
		
	}

}