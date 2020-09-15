using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace AC
{

	[System.Serializable]
	public class ActionObjectiveCheck : ActionCheckMultiple
	{

		public int objectiveID;
		public int playerID;
		public bool setPlayer;

		
		public ActionObjectiveCheck ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Objective;
			title = "Check state";
			description = "Queries the current state of an objective.";
		}


		public override ActionEnd End (List<Action> actions)
		{
			if (numSockets <= 1)
			{
				return GenerateStopActionEnd ();
			}

			Objective objective = KickStarter.inventoryManager.GetObjective (objectiveID);
			if (objective != null)
			{
				int _playerID = (setPlayer && KickStarter.inventoryManager.ObjectiveIsPerPlayer (objectiveID)) ? playerID : -1;

				ObjectiveState currentObjectiveState = KickStarter.runtimeObjectives.GetObjectiveState (objectiveID, _playerID);
				if (currentObjectiveState != null)
				{
					int stateIndex = objective.states.IndexOf (currentObjectiveState);
					return ProcessResult (stateIndex + 1, actions);
				}
				else
				{
					return ProcessResult (0, actions);
				}
			}
			return GenerateStopActionEnd ();
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

			Objective objective = KickStarter.inventoryManager.GetObjective (objectiveID);
			if (objective != null)
			{
				numSockets = objective.NumStates + 1;

				if (KickStarter.inventoryManager.ObjectiveIsPerPlayer (objectiveID))
				{
					setPlayer = EditorGUILayout.Toggle ("Check specific Player?", setPlayer);
					if (setPlayer)
					{
						playerID = ChoosePlayerGUI (playerID);
					}
				}
			}
			else
			{
				numSockets = 1;
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

			if (numSockets < 1)
			{
				numSockets = 1;
			}
		
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

					if (i == 0)
					{
						ending.resultAction = (ResultAction) EditorGUILayout.EnumPopup ("If inactive:", (ResultAction) ending.resultAction);
					}
					else
					{
						Objective objective = KickStarter.inventoryManager.GetObjective (objectiveID);
						if (objective != null)
						{
							string[] popUpLabels = objective.GenerateEditorStateLabels ();
							ending.resultAction = (ResultAction) EditorGUILayout.EnumPopup ("If = '" + popUpLabels[i-1] + "':", (ResultAction) ending.resultAction);
						}
						else
						{
							ending.resultAction = (ResultAction) EditorGUILayout.EnumPopup ("If ID = '" + objectiveID.ToString () + "':", (ResultAction) ending.resultAction);
						}
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