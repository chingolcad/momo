/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionParallel.cs"
 * 
 *	This action can play multiple subsequent Actions at once.
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
	public class ActionParallel : Action
	{
		
		public List<ActionEnd> endings = new List<ActionEnd>();
		
		
		public ActionParallel ()
		{
			this.isDisplayed = true;
			category = ActionCategory.ActionList;
			title = "Run in parallel";
			description = "Runs any subsequent Actions (whether in the same list or in a new one) simultaneously. This is useful when making complex cutscenes that require timing to be exact.";
			if (numSockets == 1) numSockets = 2;
		}
		
		
		public ActionEnd[] Ends (List<Action> actions, bool doSkip)
		{
			foreach (ActionEnd ending in endings)
			{
				if (ending.resultAction == ResultAction.Skip)
				{
					int skip = ending.skipAction;
					if (skipActionActual && actions.Contains (ending.skipActionActual))
					{
						skip = actions.IndexOf (ending.skipActionActual);
					}
					else if (skip == -1)
					{
						skip = 0;
					}
					
					ending.skipAction = skip;
				}
			}
			
			return endings.ToArray ();
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			numSockets = EditorGUILayout.IntSlider ("# of outputs:", numSockets, 1, 10);
		}
		
		
		public override void SkipActionGUI (List<Action> actions, bool showGUI)
		{
			if (numSockets < 0)
			{
				numSockets = 0;
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
					endings.Add (newEnd);
				}
			}
			
			foreach (ActionEnd ending in endings)
			{
				if (showGUI)
				{
					EditorGUILayout.Space ();
					int i = endings.IndexOf (ending) +1;
					ending.resultAction = (ResultAction) EditorGUILayout.EnumPopup ("Output " + i.ToString () + ":", (ResultAction) ending.resultAction);
				}
				
				if (ending.resultAction == ResultAction.RunCutscene && showGUI)
				{
					if (isAssetFile)
					{
						ending.linkedAsset = ActionListAssetMenu.AssetGUI ("ActionList to run:", ending.linkedAsset);
					}
					else
					{
						ending.linkedCutscene = ActionListAssetMenu.CutsceneGUI ("Cutscene to run:", ending.linkedCutscene);
					}
				}
				else if (ending.resultAction == ResultAction.Skip)
				{
					SkipActionGUI (ending, actions, showGUI);
				}
			}

			bool shownError = false;
			List<int> outputIndices = new List<int>();
			foreach (ActionEnd ending in endings)
			{
				if (ending.resultAction == ResultAction.Skip)
				{
					if (outputIndices.Contains (ending.skipAction))
					{
						if (!shownError)
						{
							EditorGUILayout.HelpBox ("Two or more output sockets connect to the same subsequent Action - this may cause unexpected behaviour and should be changed.", MessageType.Warning);
						}
						shownError = true;
					}
					else
					{
						outputIndices.Add (ending.skipAction);
					}
				}
			}
		}
		
		
		protected void SkipActionGUI (ActionEnd ending, List<Action> actions, bool showGUI)
		{
			if (ending.skipAction == -1)
			{
				// Set default
				int i = actions.IndexOf (this);
				if (actions.Count > i+1)
				{
					ending.skipAction = i+1;
				}
				else
				{
					ending.skipAction = i;
				}
			}
			
			int tempSkipAction = ending.skipAction;
			List<string> labelList = new List<string>();
			
			if (ending.skipActionActual)
			{
				bool found = false;
				
				for (int i = 0; i < actions.Count; i++)
				{
					//labelList.Add (i.ToString () + ": " + actions [i].title);
					labelList.Add ("(" + i.ToString () + ") " + ((KickStarter.actionsManager != null) ? KickStarter.actionsManager.GetActionTypeLabel (actions[i]) : string.Empty));

					if (ending.skipActionActual == actions [i])
					{
						ending.skipAction = i;
						found = true;
					}
				}
				
				if (!found)
				{
					ending.skipAction = tempSkipAction;
				}
			}
			
			if (ending.skipAction >= actions.Count)
			{
				ending.skipAction = actions.Count - 1;
			}
			
			if (showGUI)
			{
				if (actions.Count > 1)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField ("  Action to skip to:", GUILayout.Width (155f));
					tempSkipAction = EditorGUILayout.Popup (ending.skipAction, labelList.ToArray());
					ending.skipAction = tempSkipAction;
					EditorGUILayout.EndHorizontal();
				}
				else
				{
					EditorGUILayout.HelpBox ("Cannot skip action - no further Actions available", MessageType.Warning);
					return;
				}
			}
			
			ending.skipActionActual = actions [ending.skipAction];
		}
		
		
		public override void DrawOutWires (List<Action> actions, int i, int offset, Vector2 scrollPosition)
		{
			int totalHeight = 7;
			for (int j = endings.Count-1; j>=0; j--)
			{
				ActionEnd ending = endings [j];

				float fac = (float) (endings.Count - endings.IndexOf (ending)) / endings.Count;
				Color wireColor = new Color (1f-fac, fac*0.7f, 0.1f);

				if (ending.resultAction == ResultAction.Continue)
				{
					if (actions.Count > i+1)
					{
						AdvGame.DrawNodeCurve (new Rect (nodeRect.position - scrollPosition, nodeRect.size),
											   new Rect (actions[i+1].nodeRect.position - scrollPosition, actions[i+1].nodeRect.size),
											   wireColor, totalHeight, true, isDisplayed);
					}
				}
				else if (ending.resultAction == ResultAction.Skip && showOutputSockets)
				{
					if (actions.Contains (ending.skipActionActual))
					{
						AdvGame.DrawNodeCurve (new Rect (nodeRect.position - scrollPosition, nodeRect.size),
											   new Rect (ending.skipActionActual.nodeRect.position - scrollPosition, ending.skipActionActual.nodeRect.size),
											   wireColor, totalHeight, true, isDisplayed);
					}
				}

				if (ending.resultAction == ResultAction.Skip)
				{
					totalHeight += 44;
				}
				else
				{
					totalHeight += 26;
				}
			}
		}
		
		
		public override void FixLinkAfterDeleting (Action actionToDelete, Action targetAction, List<Action> actionList)
		{
			foreach (ActionEnd end in endings)
			{
				if ((end.resultAction == ResultAction.Skip && end.skipActionActual == actionToDelete) || (end.resultAction == ResultAction.Continue && actionList.IndexOf (actionToDelete) == (actionList.IndexOf (this) + 1)))
				{
					if (targetAction == null)
					{
						end.resultAction = ResultAction.Stop;
					}
					else
					{
						end.resultAction = ResultAction.Skip;
						end.skipActionActual = targetAction;
					}
				}
			}
		}
		
		
		public override void PrepareToCopy (int originalIndex, List<Action> actionList)
		{
			foreach (ActionEnd end in endings)
			{
				if (end.resultAction == ResultAction.Continue)
				{
					if (originalIndex == actionList.Count - 1)
					{
						end.resultAction = ResultAction.Stop;
					}
					else if (actionList [originalIndex + 1].isMarked)
					{
						end.resultAction = ResultAction.Skip;
						end.skipActionActual = actionList [originalIndex + 1];
					}
					else
					{
						end.resultAction = ResultAction.Stop;
					}
				}
				if (end.resultAction == ResultAction.Skip)
				{
					if (end.skipActionActual.isMarked)
					{
						int place = 0;
						foreach (Action _action in actionList)
						{
							if (_action.isMarked)
							{
								if (_action == end.skipActionActual)
								{
									end.skipActionActual = null;
									end.skipAction = place;
									break;
								}
								place ++;
							}
						}
					}
					else
					{
						end.resultAction = ResultAction.Stop;
					}
				}
			}
		}


		public override void PrepareToPaste (int offset)
		{
			foreach (ActionEnd end in endings)
			{
				end.skipAction += offset;
			}
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (!isAssetFile)
			{
				foreach (ActionEnd ending in endings)
				{
					if (ending.resultAction == ResultAction.RunCutscene)
					{
						if (ending.linkedCutscene != null && ending.linkedCutscene.gameObject == gameObject) return true;
					}
				}
			}
			return false;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (isAssetFile)
			{
				foreach (ActionEnd ending in endings)
				{
					if (ending.resultAction == ResultAction.RunCutscene)
					{
						if (ending.linkedAsset == actionListAsset) return true;
					}
				}
			}
			return false;
		}

#endif


		/**
		 * <summary>Creates a new instance of the 'ActionList: Run in parallel' Action</summary>
		 * <param name = "actionEnds">An array of data about what output sockets the Action has</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParallel CreateNew (ActionEnd[] actionEnds)
		{
			ActionParallel newAction = (ActionParallel) CreateInstance <ActionParallel>();
			newAction.endings = new List<ActionEnd>();
			foreach (ActionEnd actionEnd in actionEnds)
			{
				newAction.endings.Add (new ActionEnd (actionEnd));
			}
			return newAction;
		}
		
	}
	
}