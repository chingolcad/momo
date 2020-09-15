/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionStopActionList.cs"
 * 
 *	This Action stops other ActionLists
 * 
 */

using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionStopActionList : Action
	{
		
		public enum ListSource { InScene, AssetFile };
		public ListSource listSource = ListSource.InScene;
		
		public ActionList actionList;
		protected ActionList runtimeActionList;

		public ActionListAsset invActionList;
		public int constantID = 0;
		public int parameterID = -1;


		public ActionStopActionList ()
		{
			this.isDisplayed = true;
			category = ActionCategory.ActionList;
			title = "Kill";
			description = "Instantly stops a scene or asset-based ActionList from running.";
		}

		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (listSource == ListSource.InScene)
			{
				runtimeActionList = AssignFile <ActionList> (parameters, parameterID, constantID, actionList);
			}
		}
		
		
		public override float Run ()
		{
			if (listSource == ListSource.InScene && runtimeActionList != null)
			{
				KickStarter.actionListManager.EndList (runtimeActionList);
			}
			else if (listSource == ListSource.AssetFile && invActionList != null)
			{
				KickStarter.actionListAssetManager.EndAssetList (invActionList, this);
			}
			
			return 0f;
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
					constantID = 0;
					actionList = null;
				}
				else
				{
					actionList = (ActionList) EditorGUILayout.ObjectField ("ActionList:", actionList, typeof (ActionList), true);
					
					constantID = FieldToID <ActionList> (actionList, constantID);
					actionList = IDToField <ActionList> (actionList, constantID, true);
				}
			}
			else if (listSource == ListSource.AssetFile)
			{
				invActionList = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList asset:", invActionList, typeof (ActionListAsset), true);
			}

			AfterRunningOption ();
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

		
		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parameterID < 0 && listSource == ListSource.InScene)
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


		/**
		 * <summary>Creates a new instance of the 'ActionList: Kill' Action</summary>
		 * <param name = "actionList">The ActionList to kill</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionStopActionList CreateNew (ActionList actionList)
		{
			ActionStopActionList newAction = (ActionStopActionList) CreateInstance <ActionStopActionList>();
			newAction.listSource = ListSource.InScene;
			newAction.actionList = actionList;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Kill' Action</summary>
		 * <param name = "actionListAsset">The ActionList asset to kill</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionStopActionList CreateNew (ActionListAsset actionListAsset)
		{
			ActionStopActionList newAction = (ActionStopActionList) CreateInstance <ActionStopActionList>();
			newAction.listSource = ListSource.AssetFile;
			newAction.invActionList = actionListAsset;
			return newAction;
		}
		
	}
	
}