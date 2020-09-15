/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionCheck.cs"
 * 
 *	This is an intermediate class for "checking" Actions,
 *	that have TRUE and FALSE endings.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * An Action subclass that allows for two different outcomes based on a boolean check.
	 */
	[System.Serializable]
	public class ActionCheck : Action
	{

		/** What happens when the Action ends and CheckCondition() = True (Continue, Stop, Skip, RunCutscene) */
		public ResultAction resultActionTrue;
		/** The index number of the Action to skip to, if resultAction = ResultAction.Skip and CheckCondition() = True */
		public int skipActionTrue = -1;
		/** The Action to skip to, if resultAction = ResultAction.Skip and CheckCondition() = True */
		public AC.Action skipActionTrueActual;
		/** The Cutscene to run, if resultAction = ResultAction.RunCutscene, the Action is in a scene-based ActionList, and CheckCondition() = True */
		public Cutscene linkedCutsceneTrue;
		/** The ActionListAsset to run, if resultAction = ResultAction.RunCutscene, the Action is in an ActionListAsset file, and CheckCondition() = True */
		public ActionListAsset linkedAssetTrue;

		/** What happens when the Action ends and CheckCondition() = False (Continue, Stop, Skip, RunCutscene) */
		public ResultAction resultActionFail = ResultAction.Stop;
		/** The index number of the Action to skip to, if resultAction = ResultAction.Skip and CheckCondition() = False */
		public int skipActionFail = -1;
		/** The Action to skip to, if resultAction = ResultAction.Skip and CheckCondition() = False */
		public AC.Action skipActionFailActual;
		/** The Cutscene to run, if resultAction = ResultAction.RunCutscene, the Action is in a scene-based ActionList, and CheckCondition() = False */
		public Cutscene linkedCutsceneFail;
		/** The ActionListAsset to run, if resultAction = ResultAction.RunCutscene, the Action is in an ActionListAsset file, and CheckCondition() = False */
		public ActionListAsset linkedAssetFail;


		/**
		 * The default Constructor.
		 */
		public ActionCheck ()
		{
			numSockets = 2;
		}


		public override ActionEnd End (List<Action> actions)
		{
			return ProcessResult (CheckCondition (), actions);
		}


		protected ActionEnd ProcessResult (bool result, List<Action> actions)
		{
			if (result)
			{
				return GenerateActionEnd (resultActionTrue, linkedAssetTrue, linkedCutsceneTrue, skipActionTrue, skipActionTrueActual, actions);
			}
			return GenerateActionEnd (resultActionFail, linkedAssetFail, linkedCutsceneFail, skipActionFail, skipActionFailActual, actions);
		}


		/**
		 * <summary>Works out which of the two outputs should be run when this Action is complete.</summary>
		 * <returns>If True, then resultActionTrue will be used - otherwise resultActionFalse will be used</returns>
		 */
		public virtual bool CheckCondition ()
		{
			return false;
		}


		#if UNITY_EDITOR
		
		public override void SkipActionGUI (List<Action> actions, bool showGUI)
		{
			if (showGUI)
			{
				EditorGUILayout.Space ();
				resultActionTrue = (ResultAction) EditorGUILayout.EnumPopup("If condition is met:", (ResultAction) resultActionTrue);
			}
			if (resultActionTrue == ResultAction.RunCutscene && showGUI)
			{
				if (isAssetFile)
				{
					linkedAssetTrue = ActionListAssetMenu.AssetGUI ("ActionList to run:", linkedAssetTrue);
				}
				else
				{
					linkedCutsceneTrue = ActionListAssetMenu.CutsceneGUI ("Cutscene to run:", linkedCutsceneTrue);
				}
			}
			else if (resultActionTrue == ResultAction.Skip)
			{
				SkipActionTrueGUI (actions, showGUI);
			}
			
			if (showGUI)
			{
				resultActionFail = (ResultAction) EditorGUILayout.EnumPopup("If condition is not met:", (ResultAction) resultActionFail);
			}
			if (resultActionFail == ResultAction.RunCutscene && showGUI)
			{
				if (isAssetFile)
				{
					linkedAssetFail = ActionListAssetMenu.AssetGUI ("ActionList to run:", linkedAssetFail);
				}
				else
				{
					linkedCutsceneFail = ActionListAssetMenu.CutsceneGUI ("Cutscene to run:", linkedCutsceneFail);
				}
			}
			else if (resultActionFail == ResultAction.Skip)
			{
				SkipActionFailGUI (actions, showGUI);
			}
		}
		
		
		private void SkipActionTrueGUI (List<Action> actions, bool showGUI)
		{
			if (skipActionTrue == -1)
			{
				// Set default
				int i = actions.IndexOf (this);
				if (actions.Count > i+1)
				{
					skipActionTrue = i+1;
				}
				else
				{
					skipActionTrue = i;
				}
			}

			int tempSkipAction = skipActionTrue;
			List<string> labelList = new List<string>();
			
			if (skipActionTrueActual)
			{
				bool found = false;
				
				for (int i = 0; i < actions.Count; i++)
				{
					//labelList.Add (i.ToString () + ": " + actions [i].title);
					labelList.Add ("(" + i.ToString () + ") " + ((KickStarter.actionsManager != null) ? KickStarter.actionsManager.GetActionTypeLabel (actions[i]) : string.Empty));
					
					if (skipActionTrueActual == actions [i])
					{
						skipActionTrue = i;
						found = true;
					}
				}
				
				if (!found)
				{
					skipActionTrue = tempSkipAction;
				}
			}
			
			if (skipActionTrue >= actions.Count)
			{
				skipActionTrue = actions.Count - 1;
			}
			
			if (showGUI)
			{
				if (actions.Count > 1)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField ("  Action to skip to:", GUILayout.Width (155f));
					tempSkipAction = EditorGUILayout.Popup (skipActionTrue, labelList.ToArray());
					skipActionTrue = tempSkipAction;
					EditorGUILayout.EndHorizontal();
				}
				else
				{
					EditorGUILayout.HelpBox ("Cannot skip action - no further Actions available", MessageType.Warning);
					return;
				}
			}
			
			skipActionTrueActual = actions [skipActionTrue];
		}
		
		
		private void SkipActionFailGUI (List<Action> actions, bool showGUI)
		{
			if (skipActionFail == -1)
			{
				// Set default
				int i = actions.IndexOf (this);
				if (actions.Count > i+1)
				{
					skipActionFail = i+1;
				}
				else
				{
					skipActionFail = i;
				}
			}

			int tempSkipAction = skipActionFail;
			List<string> labelList = new List<string>();
			
			if (skipActionFailActual)
			{
				bool found = false;
				
				for (int i = 0; i < actions.Count; i++)
				{
					labelList.Add ("(" + i.ToString () + ") " + ((KickStarter.actionsManager != null) ? KickStarter.actionsManager.GetActionTypeLabel (actions[i]) : string.Empty));

					if (skipActionFailActual == actions [i])
					{
						skipActionFail = i;
						found = true;
					}
				}
				
				if (!found)
				{
					skipActionFail = tempSkipAction;
				}
			}
			
			if (skipActionFail >= actions.Count)
			{
				skipActionFail = actions.Count - 1;
			}
			
			if (showGUI)
			{
				if (actions.Count > 1)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField ("  Action to skip to:", GUILayout.Width (155f));
					tempSkipAction = EditorGUILayout.Popup (skipActionFail, labelList.ToArray());
					skipActionFail = tempSkipAction;
					EditorGUILayout.EndHorizontal();
				}
				else
				{
					EditorGUILayout.HelpBox ("Cannot skip action - no further Actions available", MessageType.Warning);
					return;
				}
			}
			
			skipActionFailActual = actions [skipActionFail];
		}


		public override void DrawOutWires (List<Action> actions, int i, int offset, Vector2 scrollPosition)
		{
			if (resultActionTrue == ResultAction.Continue)
			{
				if (actions.Count > i+1)
				{
					AdvGame.DrawNodeCurve (new Rect (nodeRect.position - scrollPosition, nodeRect.size),
										   new Rect (actions[i+1].nodeRect.position - scrollPosition, actions[i+1].nodeRect.size),
										   new Color (0.1f, 0.7f, 0.1f), 27 + offset, true, isDisplayed);
				}
			}
			else if (resultActionTrue == ResultAction.Skip && showOutputSockets)
			{
				if (actions.Contains (skipActionTrueActual))
				{
					AdvGame.DrawNodeCurve (new Rect (nodeRect.position - scrollPosition, nodeRect.size),
										   new Rect (skipActionTrueActual.nodeRect.position - scrollPosition, skipActionTrueActual.nodeRect.size),
										   new Color (0.1f, 0.7f, 0.1f), 27 + offset, true, isDisplayed);
				}
			}
			
			if (resultActionFail == ResultAction.Continue)
			{
				if (actions.Count > i+1)
				{
					AdvGame.DrawNodeCurve (new Rect (nodeRect.position - scrollPosition, nodeRect.size),
										   new Rect (actions[i+1].nodeRect.position - scrollPosition, actions[i+1].nodeRect.size),
										   Color.red, 10, true, isDisplayed);
				}
			}
			else if (resultActionFail == ResultAction.Skip && showOutputSockets)
			{
				if (actions.Contains (skipActionFailActual))
				{
					AdvGame.DrawNodeCurve (new Rect (nodeRect.position - scrollPosition, nodeRect.size),
										   new Rect (skipActionFailActual.nodeRect.position - scrollPosition, skipActionFailActual.nodeRect.size),
										   Color.red, 10, true, isDisplayed);
				}
			}
		}


		public override void FixLinkAfterDeleting (Action actionToDelete, Action targetAction, List<Action> actionList)
		{
			if ((resultActionFail == ResultAction.Skip && skipActionFailActual == actionToDelete) || (resultActionFail == ResultAction.Continue && actionList.IndexOf (actionToDelete) == (actionList.IndexOf (this) + 1)))
			{
				if (targetAction == null)
				{
					resultActionFail = ResultAction.Stop;
				}
				else
				{
					resultActionFail = ResultAction.Skip;
					skipActionFailActual = targetAction;
				}
			}
			if ((resultActionTrue == ResultAction.Skip && skipActionTrueActual == actionToDelete) || (resultActionTrue == ResultAction.Continue && actionList.IndexOf (actionToDelete) == (actionList.IndexOf (this) + 1)))
			{
				if (targetAction == null)
				{
					resultActionTrue = ResultAction.Stop;
				}
				else
				{
					resultActionTrue = ResultAction.Skip;
					skipActionTrueActual = targetAction;
				}
			}
		}


		public override void PrepareToCopy (int originalIndex, List<Action> actionList)
		{
			if (resultActionFail == ResultAction.Continue)
			{
				if (originalIndex == actionList.Count - 1)
				{
					resultActionFail = ResultAction.Stop;
				}
				else if (actionList [originalIndex + 1].isMarked)
				{
					resultActionFail = ResultAction.Skip;
					skipActionFailActual = actionList [originalIndex + 1];
				}
				else
				{
					resultActionFail = ResultAction.Stop;
				}
			}
			if (resultActionFail == ResultAction.Skip)
			{
				if (skipActionFailActual.isMarked)
				{
					int place = 0;
					foreach (Action _action in actionList)
					{
						if (_action.isMarked)
						{
							if (_action == skipActionFailActual)
							{
								skipActionFailActual = null;
								skipActionFail = place;
								break;
							}
							place ++;
						}
					}
				}
				else
				{
					resultActionFail = ResultAction.Stop;
				}
			}

			if (resultActionTrue == ResultAction.Continue)
			{
				if (originalIndex == actionList.Count - 1)
				{
					resultActionTrue = ResultAction.Stop;
				}
				else if (actionList [originalIndex + 1].isMarked)
				{
					resultActionTrue = ResultAction.Skip;
					skipActionTrueActual = actionList [originalIndex + 1];
				}
				else
				{
					resultActionTrue = ResultAction.Stop;
				}
			}
			if (resultActionTrue == ResultAction.Skip)
			{
				if (skipActionTrueActual.isMarked)
				{
					int place = 0;
					foreach (Action _action in actionList)
					{
						if (_action.isMarked)
						{
							if (_action == skipActionTrueActual)
							{
								skipActionTrueActual = null;
								skipActionTrue = place;
								break;
							}
							place ++;
						}
					}
				}
				else
				{
					resultActionTrue = ResultAction.Stop;
				}
			}
		}


		public override void PrepareToPaste (int offset)
		{
			skipActionFail += offset;
			skipActionTrue += offset;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (!isAssetFile)
			{
				if (resultActionFail == ResultAction.RunCutscene)
				{
					if (linkedCutsceneFail != null && linkedCutsceneFail.gameObject == gameObject) return true;
				}
				if (resultActionTrue == ResultAction.RunCutscene)
				{
					if (linkedCutsceneTrue != null && linkedCutsceneTrue.gameObject == gameObject) return true;
				}
			}
			return false;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (isAssetFile)
			{
				if (resultActionFail == ResultAction.RunCutscene)
				{
					if (linkedAssetFail == actionListAsset) return true;
				}
				if (resultActionTrue == ResultAction.RunCutscene)
				{
					if (linkedAssetTrue == actionListAsset) return true;
				}
			}
			return false;
		}

		#endif


		/**
		 * <summary>Update the Action's output sockets</summary>
		 * <param name = "actionEndOnPass">A data container for the 'Condition is met' output socket</param>
		 * <param name = "actionEndOnFail">A data container for the 'Condition is not met' output socket</param>
		 */
		public void SetOutputs (ActionEnd actionEndOnPass, ActionEnd actionEndOnFail)
		{
			resultActionTrue = actionEndOnPass.resultAction;
			skipActionTrue = actionEndOnPass.skipAction;
			skipActionTrueActual = actionEndOnPass.skipActionActual;
			linkedCutsceneTrue = actionEndOnPass.linkedCutscene;
			linkedAssetTrue = actionEndOnPass.linkedAsset;

			resultActionFail = actionEndOnFail.resultAction;
			skipActionFail = actionEndOnFail.skipAction;
			skipActionFailActual = actionEndOnFail.skipActionActual;
			linkedCutsceneFail = actionEndOnFail.linkedCutscene;
			linkedAssetFail = actionEndOnFail.linkedAsset;
		}

	}

}