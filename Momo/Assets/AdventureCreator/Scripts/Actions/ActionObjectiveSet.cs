using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace AC
{

	[System.Serializable]
	public class ActionObjectiveSet : Action
	{

		public int objectiveID;
		public int newStateID;
		public bool selectAfter;
		public int playerID;
		public bool setPlayer;
		

		public ActionObjectiveSet ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Objective;
			title = "Set state";
			description = "Updates an objective's current state.";
		}
		
		
		public override float Run ()
		{
			if (KickStarter.inventoryManager.ObjectiveIsPerPlayer (objectiveID) && setPlayer)
			{
				KickStarter.runtimeObjectives.SetObjectiveState (objectiveID, newStateID, playerID);
			}
			else
			{
				KickStarter.runtimeObjectives.SetObjectiveState (objectiveID, newStateID, selectAfter);
			}

			Menu[] menus = PlayerMenus.GetMenus (true).ToArray ();
			foreach (Menu menu in menus)
			{
				menu.Recalculate ();
			}

			return 0f;
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
				newStateID = objective.StateSelectorList (newStateID, "Set to state:");

				if (KickStarter.inventoryManager.ObjectiveIsPerPlayer (objectiveID))
				{
					setPlayer = EditorGUILayout.Toggle ("Affect specific Player?", setPlayer);
					if (setPlayer)
					{
						playerID = ChoosePlayerGUI (playerID);
					}
					else
					{
						selectAfter = EditorGUILayout.Toggle ("Select after?", selectAfter);
					}
				}
				else
				{
					selectAfter = EditorGUILayout.Toggle ("Select after?", selectAfter);
				}
			}

			AfterRunningOption ();
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

		#endif
		
	}

}