/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionRunActionList.cs"
 * 
 *	This Action runs other ActionLists
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
	public class ActionRunActionList : Action
	{
		
		public enum ListSource { InScene, AssetFile };
		public ListSource listSource = ListSource.InScene;

		public ActionList actionList;
		public int constantID = 0;
		public int parameterID = -1;

		public ActionListAsset invActionList;
		public int assetParameterID = -1;

		public bool runFromStart = true;
		public int jumpToAction;
		public int jumpToActionParameterID = -1;
		public AC.Action jumpToActionActual;
		public bool runInParallel = false; // No longer visible, but needed for legacy upgrades

		public bool isSkippable = false; // Important: Set by ActionList, to determine whether or not the ActionList it runs should be added to the skip queue

		public List<ActionParameter> localParameters = new List<ActionParameter>();
		public List<int> parameterIDs = new List<int>();

		public bool setParameters = false;

		protected RuntimeActionList runtimeActionList;


		public ActionRunActionList ()
		{
			this.isDisplayed = true;
			category = ActionCategory.ActionList;
			title = "Run";
			description = "Runs any ActionList (either scene-based like Cutscenes, Triggers and Interactions, or ActionList assets). If the new ActionList takes parameters, this Action can be used to set them.";
		}


		protected void Upgrade ()
		{
			if (!runInParallel)
			{
				numSockets = 1;
				runInParallel = true;
				endAction = ResultAction.Stop;
			}
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (listSource == ListSource.InScene)
			{
				actionList = AssignFile <ActionList> (parameters, parameterID, constantID, actionList);
				jumpToAction = AssignInteger (parameters, jumpToActionParameterID, jumpToAction);
			}
			else if (listSource == ListSource.AssetFile)
			{
				invActionList = (ActionListAsset) AssignObject <ActionListAsset> (parameters, assetParameterID, invActionList);
			}

			if (localParameters != null && localParameters.Count > 0)
			{
				for (int i=0; i<localParameters.Count; i++)
				{
					if (parameterIDs != null && parameterIDs.Count > i && parameterIDs[i] >= 0)
					{
						int ID = parameterIDs[i];
						foreach (ActionParameter parameter in parameters)
						{
							if (parameter.ID == ID)
							{
								localParameters[i].CopyValues (parameter);
								break;
							}
						}
					}
				}
			}
		}


		public override float Run ()
		{
			if (!isRunning)
			{
				Upgrade ();

				isRunning = true;
				runtimeActionList = null;

				if (listSource == ListSource.InScene && actionList != null && !actionList.actions.Contains (this))
				{
					KickStarter.actionListManager.EndList (actionList);

					if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
					{
						if (actionList.syncParamValues)
						{
							SendParameters (actionList.assetFile.GetParameters (), true);
						}
						else
						{
							SendParameters (actionList.parameters, false);
						}
					}
					else if (actionList.source == ActionListSource.InScene && actionList.useParameters)
					{
						SendParameters (actionList.parameters, false);
					}

					if (runFromStart)
					{
						actionList.Interact (0, !isSkippable);
					}
					else
					{
						actionList.Interact (GetSkipIndex (actionList.actions), !isSkippable);
					}
				}
				else if (listSource == ListSource.AssetFile && invActionList != null && !invActionList.actions.Contains (this))
				{
					if (!invActionList.canRunMultipleInstances)
					{
						KickStarter.actionListAssetManager.EndAssetList (invActionList);
					}

					if (invActionList.useParameters)
					{
						SendParameters (invActionList.GetParameters (), true);
					}

					if (runFromStart)
					{
						runtimeActionList = AdvGame.RunActionListAsset (invActionList, 0, !isSkippable);
					}
					else
					{
						runtimeActionList = AdvGame.RunActionListAsset (invActionList, GetSkipIndex (invActionList.actions), !isSkippable);
					}
				}

				if (!runInParallel || (runInParallel && willWait))
				{
					return defaultPauseTime;
				}
			}
			else
			{
				if (listSource == ListSource.InScene && actionList != null)
				{
					if (KickStarter.actionListManager.IsListRunning (actionList))
					{
						return defaultPauseTime;
					}
					else
					{
						isRunning = false;
					}
				}
				else if (listSource == ListSource.AssetFile && invActionList != null)
				{
					if (invActionList.canRunMultipleInstances)
					{
						if (runtimeActionList != null && KickStarter.actionListManager.IsListRunning (runtimeActionList))
						{
							return defaultPauseTime;
						}
						isRunning = false;
					}
					else
					{
						if (KickStarter.actionListAssetManager.IsListRunning (invActionList))
						{
							return defaultPauseTime;
						}
						isRunning = false;
					}
				}
			}

			return 0f;
		}


		public override void Skip ()
		{
			if (listSource == ListSource.InScene && actionList != null)
			{
				if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
				{
					if (actionList.syncParamValues)
					{
						SendParameters (actionList.assetFile.GetParameters (), true);
					}
					else
					{
						SendParameters (actionList.parameters, false);
					}
				}
				else if (actionList.source == ActionListSource.InScene && actionList.useParameters)
				{
					SendParameters (actionList.parameters, false);
				}

				if (runFromStart)
				{
					actionList.Skip ();
				}
				else
				{
					actionList.Skip (GetSkipIndex (actionList.actions));
				}
			}
			else if (listSource == ListSource.AssetFile && invActionList != null)
			{
				if (invActionList.useParameters)
				{
					SendParameters (invActionList.GetParameters (), true);
				}

				if (runtimeActionList != null && !invActionList.IsSkippable () && invActionList.canRunMultipleInstances)
				{
					KickStarter.actionListAssetManager.EndAssetList (runtimeActionList);
				}

				if (runFromStart)
				{
					AdvGame.SkipActionListAsset (invActionList);
				}
				else
				{
					AdvGame.SkipActionListAsset (invActionList, GetSkipIndex (invActionList.actions));
				}
			}
		}


		protected int GetSkipIndex (List<Action> _actions)
		{
			int skip = jumpToAction;
			if (jumpToActionActual && _actions.IndexOf (jumpToActionActual) > 0)
			{
				skip = _actions.IndexOf (jumpToActionActual);
			}
			return skip;
		}


		protected void SendParameters (List<ActionParameter> externalParameters, bool sendingToAsset)
		{
			if (!setParameters)
			{
				return;
			}

			SyncLists (externalParameters, localParameters);
			SetParametersBase.BulkAssignParameterValues (externalParameters, localParameters, sendingToAsset, isAssetFile);
		}


		#if UNITY_EDITOR
				
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			listSource = (ListSource) EditorGUILayout.EnumPopup ("Source:", listSource);
			if (listSource == ListSource.InScene)
			{
				parameterID = Action.ChooseParameterGUI ("ActionList:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					localParameters.Clear ();
					constantID = 0;
					actionList = null;

					if (setParameters)
					{
						EditorGUILayout.HelpBox ("If the ActionList has parameters, they will be set here - unset the parameter to edit them.", MessageType.Info);
					}
				}
				else
				{
					actionList = (ActionList) EditorGUILayout.ObjectField ("ActionList:", actionList, typeof (ActionList), true);
					
					constantID = FieldToID <ActionList> (actionList, constantID);
					actionList = IDToField <ActionList> (actionList, constantID, true);

					if (actionList != null)
					{
						if (actionList.actions.Contains (this))
						{
							EditorGUILayout.HelpBox ("This Action cannot be used to run the ActionList it is in - use the Skip option below instead.", MessageType.Warning);
						}
						else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.NumParameters > 0)
						{
							SetParametersGUI (actionList.assetFile.DefaultParameters, parameters);
						}
						else if (actionList.source == ActionListSource.InScene && actionList.NumParameters > 0)
						{
							SetParametersGUI (actionList.parameters, parameters);
						}
					}
				}


				runFromStart = EditorGUILayout.Toggle ("Run from start?", runFromStart);

				if (!runFromStart)
				{
					jumpToActionParameterID = Action.ChooseParameterGUI ("Action # to skip to:", parameters, jumpToActionParameterID, ParameterType.Integer);
					if (jumpToActionParameterID == -1 && actionList != null && actionList.actions.Count > 1)
					{
						JumpToActionGUI (actionList.actions);
					}
				}
			}
			else if (listSource == ListSource.AssetFile)
			{
				assetParameterID = Action.ChooseParameterGUI ("ActionList asset:", parameters, assetParameterID, ParameterType.UnityObject);
				if (assetParameterID < 0)
				{
					invActionList = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList asset:", invActionList, typeof (ActionListAsset), true);
				}

				if (assetParameterID >= 0)
				{
					EditorGUILayout.LabelField ("Placeholder asset: " + ((invActionList != null) ? invActionList.name : "(None set)"), EditorStyles.whiteLabel);
				}

				if (invActionList != null)
				{
					if (assetParameterID < 0 && invActionList.actions.Contains (this))
					{
						EditorGUILayout.HelpBox ("This Action cannot be used to run the ActionList it is in - use the Skip option below instead.", MessageType.Warning);
					}
					else if (invActionList.NumParameters > 0)
					{
						SetParametersGUI (invActionList.DefaultParameters, parameters);
					}
				}
				else if (assetParameterID >= 0 && setParameters)
				{
					EditorGUILayout.HelpBox ("If the ActionList asset has parameters, they will be set here - unset the parameter to edit them.", MessageType.Info);
				}

				runFromStart = EditorGUILayout.Toggle ("Run from start?", runFromStart);
				
				if (!runFromStart && invActionList != null && invActionList.actions.Count > 1)
				{
					JumpToActionGUI (invActionList.actions);
				}
			}

			if (!runInParallel)
			{
				Upgrade ();
			}

			willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			AfterRunningOption ();
		}


		private void JumpToActionGUI (List<Action> actions)
		{
			int tempSkipAction = jumpToAction;
			List<string> labelList = new List<string>();
			
			if (jumpToActionActual)
			{
				bool found = false;
				
				for (int i = 0; i < actions.Count; i++)
				{
					//labelList.Add (i.ToString () + ": " + actions [i].title);
					labelList.Add ("(" + i.ToString () + ") " + ((KickStarter.actionsManager != null) ? KickStarter.actionsManager.GetActionTypeLabel (actions[i]) : string.Empty));

					if (jumpToActionActual == actions [i])
					{
						jumpToAction = i;
						found = true;
					}
				}

				if (!found)
				{
					jumpToAction = tempSkipAction;
				}
			}
			
			if (jumpToAction < 0)
			{
				jumpToAction = 0;
			}
			
			if (jumpToAction >= actions.Count)
			{
				jumpToAction = actions.Count - 1;
			}
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField ("  Action to skip to:", GUILayout.Width (155f));
			tempSkipAction = EditorGUILayout.Popup (jumpToAction, labelList.ToArray());
			jumpToAction = tempSkipAction;
			EditorGUILayout.EndHorizontal();
			jumpToActionActual = actions [jumpToAction];
		}


		public static int ShowVarSelectorGUI (string label, List<GVar> vars, int ID)
		{
			int variableNumber = -1;
			
			List<string> labelList = new List<string>();
			labelList.Add (" (None)");
			foreach (GVar _var in vars)
			{
				labelList.Add (_var.label);
			}
			
			variableNumber = GetVarNumber (vars, ID) + 1;
			variableNumber = EditorGUILayout.Popup (label, variableNumber, labelList.ToArray()) - 1;

			if (variableNumber >= 0)
			{
				return vars[variableNumber].id;
			}

			return -1;
		}


		public static int ShowInvItemSelectorGUI (string label, List<InvItem> items, int ID)
		{
			int invNumber = -1;
			
			List<string> labelList = new List<string>();
			labelList.Add (" (None)");
			foreach (InvItem _item in items)
			{
				labelList.Add (_item.label);
			}
			
			invNumber = GetInvNumber (items, ID) + 1;
			invNumber = EditorGUILayout.Popup (label, invNumber, labelList.ToArray()) - 1;

			if (invNumber >= 0)
			{
				return items[invNumber].id;
			}
			return -1;
		}


		public static int ShowDocumentSelectorGUI (string label, List<Document> documents, int ID)
		{
			int docNumber = -1;
			
			List<string> labelList = new List<string>();
			labelList.Add (" (None)");
			foreach (Document document in documents)
			{
				labelList.Add (document.Title);
			}
			
			docNumber = GetDocNumber (documents, ID) + 1;
			docNumber = EditorGUILayout.Popup (label, docNumber, labelList.ToArray()) - 1;

			if (docNumber >= 0)
			{
				return documents[docNumber].ID;
			}
			return -1;
		}


		private static int GetVarNumber (List<GVar> vars, int ID)
		{
			int i = 0;
			foreach (GVar _var in vars)
			{
				if (_var.id == ID)
				{
					return i;
				}
				i++;
			}
			return -1;
		}


		private static int GetInvNumber (List<InvItem> items, int ID)
		{
			int i = 0;
			foreach (InvItem _item in items)
			{
				if (_item.id == ID)
				{
					return i;
				}
				i++;
			}
			return -1;
		}


		private static int GetDocNumber (List<Document> documents, int ID)
		{
			int i = 0;
			foreach (Document document in documents)
			{
				if (document.ID == ID)
				{
					return i;
				}
				i++;
			}
			return -1;
		}


		private void SetParametersGUI (List<ActionParameter> externalParameters, List<ActionParameter> ownParameters = null)
		{
			setParameters = EditorGUILayout.Toggle ("Set parameters?", setParameters);
			if (!setParameters)
			{
				return;
			}

			SetParametersBase.GUIData guiData = SetParametersBase.SetParametersGUI (externalParameters, isAssetFile, new SetParametersBase.GUIData (localParameters, parameterIDs), ownParameters);
			localParameters = guiData.fromParameters;
			parameterIDs = guiData.parameterIDs;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID <ActionList> (actionList, constantID, parameterID);
		}


		public override string SetLabel ()
		{
			if (listSource == ListSource.InScene && actionList != null)
			{
				return actionList.name;
			}
			else if (listSource == ListSource.AssetFile && invActionList != null)
			{
				return invActionList.name;
			}
			return string.Empty;
		}


		public override int GetVariableReferences (List<ActionParameter> parameters, VariableLocation location, int varID, Variables _variables)
		{
			int thisCount = 0;

			if (listSource == ListSource.InScene && actionList != null)
			{
				if (actionList.source == ActionListSource.InScene && actionList.useParameters)
				{
					SyncLists (actionList.parameters, localParameters);
				}
				else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
				{
					SyncLists (actionList.assetFile.DefaultParameters, localParameters);
				}
			}
			else if (listSource == ListSource.AssetFile && invActionList != null && invActionList.useParameters)
			{
				SyncLists (invActionList.DefaultParameters, localParameters);
			}

			foreach (ActionParameter localParameter in localParameters)
			{
				if (localParameter != null && localParameter.parameterType == ParameterType.LocalVariable && location == VariableLocation.Local && varID == localParameter.intValue)
				{
					thisCount ++;
				}
				else if (localParameter != null && localParameter.parameterType == ParameterType.GlobalVariable && location == VariableLocation.Global && varID == localParameter.intValue)
				{
					thisCount ++;
				}
				else if (localParameter != null && localParameter.parameterType == ParameterType.ComponentVariable && location == VariableLocation.Component && varID == localParameter.intValue && _variables == localParameter.variables)
				{
					thisCount ++;
				}
			}

			thisCount += base.GetVariableReferences (parameters, location, varID, _variables);
			return thisCount;
		}


		public override int GetInventoryReferences (List<ActionParameter> parameters, int _invID)
		{
			return GetParameterReferences (parameters, _invID, ParameterType.InventoryItem);
		}


		public override int GetDocumentReferences (List<ActionParameter> parameters, int _docID)
		{
			return GetParameterReferences (parameters, _docID, ParameterType.Document);
		}


		private int GetParameterReferences (List<ActionParameter> parameters, int _ID, ParameterType _paramType)
		{
			int thisCount = 0;

			if (listSource == ListSource.InScene && actionList != null)
			{
				if (actionList.source == ActionListSource.InScene && actionList.useParameters)
				{
					SyncLists (actionList.parameters, localParameters);
				}
				else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
				{
					SyncLists (actionList.assetFile.DefaultParameters, localParameters);
				}
			}
			else if (listSource == ListSource.AssetFile && invActionList != null && invActionList.useParameters)
			{
				SyncLists (invActionList.DefaultParameters, localParameters);
			}

			foreach (ActionParameter localParameter in localParameters)
			{
				if (localParameter != null && localParameter.parameterType == _paramType && _ID == localParameter.intValue)
				{
					thisCount ++;
				}
			}

			return thisCount;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (listSource == ListSource.InScene && parameterID < 0)
			{
				if (actionList != null && actionList.gameObject == gameObject) return true;
				if (constantID == id) return true;
			}
			return false;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (listSource == ListSource.AssetFile && invActionList == actionListAsset)
				return true;
			return false;
		}

		#endif


		[SerializeField] private bool hasUpgradedAgain = false;
		protected void SyncLists (List<ActionParameter> externalParameters, List<ActionParameter> oldLocalParameters)
		{
			if (!hasUpgradedAgain)
			{
				// If parameters were deleted before upgrading, there may be a mismatch - so first ensure that order internal IDs match external

				if (oldLocalParameters != null && externalParameters != null && oldLocalParameters.Count != externalParameters.Count && oldLocalParameters.Count > 0)
				{
					LogWarning ("Parameter mismatch detected - please check the 'ActionList: Run' Action for its parameter values.");
				}

				for (int i=0; i<externalParameters.Count; i++)
				{
					if (i < oldLocalParameters.Count)
					{
						oldLocalParameters[i].ID = externalParameters[i].ID;
					}
				}

				hasUpgradedAgain = true;
			}

			// Now that all parameter IDs match to begin with, we can rebuild the internal record based on the external parameters
			SetParametersBase.GUIData newGUIData = SetParametersBase.SyncLists (externalParameters, new SetParametersBase.GUIData (oldLocalParameters, parameterIDs));
			localParameters = newGUIData.fromParameters;
			parameterIDs = newGUIData.parameterIDs;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Run' Action</summary>
		 * <param name = "actionList">The ActionList to run</param>
		 * <param name = "startingActionIndex">The index number of the Action to start from</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionRunActionList CreateNew (ActionList actionList, int startingActionIndex = 0)
		{
			ActionRunActionList newAction = (ActionRunActionList) CreateInstance <ActionRunActionList>();
			newAction.listSource = ListSource.InScene;
			newAction.actionList = actionList;
			newAction.runFromStart = (startingActionIndex <= 0);
			newAction.jumpToAction = startingActionIndex;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Run' Action</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "startingActionIndex">The index number of the Action to start from</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionRunActionList CreateNew (ActionListAsset actionListAsset, int startingActionIndex = 0)
		{
			ActionRunActionList newAction = (ActionRunActionList) CreateInstance <ActionRunActionList>();
			newAction.listSource = ListSource.AssetFile;
			newAction.invActionList = actionListAsset;
			newAction.runFromStart = (startingActionIndex <= 0);
			newAction.jumpToAction = startingActionIndex;
			return newAction;
		}

	}

}