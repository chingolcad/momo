/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionWaitMultiple.cs"
 * 
 *	This Action will only trigger its 'After running' command once all Actions that can run it have been run.
 * 
 */

using System.Collections.Generic;

namespace AC
{

	[System.Serializable]
	public class ActionWaitMultiple : Action
	{

		protected int triggersToWait;

		
		public ActionWaitMultiple ()
		{
			this.isDisplayed = true;
			category = ActionCategory.ActionList;
			title = "Wait for preceding";
			description = "This Action will only trigger its 'After running' command once all Actions that can run it have been run.";
		}
		
		
		public override float Run ()
		{
			triggersToWait --;
			return 0f;
		}


		public override ActionEnd End (List<Action> actions)
		{
			if (triggersToWait > 0)
			{
				return GenerateStopActionEnd ();
			}

			triggersToWait = 100;
			return base.End (actions);
		}


		#if UNITY_EDITOR

		public override void ShowGUI ()
		{
			AfterRunningOption ();
		}

		#endif


		public override void Reset (ActionList actionList)
		{
			triggersToWait = 0;

			int ownIndex = actionList.actions.IndexOf (this);

			if (ownIndex == 0)
			{
				ACDebug.LogWarning ("The Action '" + category.ToString () + ": " + title + "' should not be first in an ActionList, as it will prevent others from running!");
				return;
			}

			for (int i=0; i<actionList.actions.Count; i++)
			{
				Action otherAction = actionList.actions[i];

				if (otherAction != this)
				{
					if (otherAction is ActionCheck)
					{
						ActionCheck actionCheck = (ActionCheck) otherAction;
						if ((actionCheck.resultActionFail == ResultAction.Skip && actionCheck.skipActionFail == ownIndex) ||
							(actionCheck.resultActionFail == ResultAction.Continue && ownIndex == i+1))
						{
							triggersToWait ++;
						}
						else if ((actionCheck.resultActionTrue == ResultAction.Skip && actionCheck.skipActionTrue == ownIndex) ||
							(actionCheck.resultActionTrue == ResultAction.Continue && ownIndex == i+1))
						{
							triggersToWait ++;
						}
					}
					else if (otherAction is ActionCheckMultiple)
					{
						ActionCheckMultiple actionCheckMultiple = (ActionCheckMultiple) otherAction;
						foreach (ActionEnd ending in actionCheckMultiple.endings)
						{
							if ((ending.resultAction == ResultAction.Skip && ending.skipAction == ownIndex) ||
								(ending.resultAction == ResultAction.Continue && ownIndex == i+1))
							{
								triggersToWait ++;
								break;
							}
						}
					}
					else if (otherAction is ActionParallel)
					{
						ActionParallel actionParallel = (ActionParallel) otherAction;
						foreach (ActionEnd ending in actionParallel.endings)
						{
							if ((ending.resultAction == ResultAction.Skip && ending.skipAction == ownIndex) ||
								(ending.resultAction == ResultAction.Continue && ownIndex == i+1))
							{
								triggersToWait ++;
								break;
							}
						}
					}
					else
					{
						if ((otherAction.endAction == ResultAction.Skip && otherAction.skipAction == ownIndex) ||
							(otherAction.endAction == ResultAction.Continue && ownIndex == i+1))
						{
							triggersToWait ++;
						}
					}
				}
			}

			base.Reset (actionList);
		}
				
	}

}