/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionMoveableCheck.cs"
 * 
 *	This Action queries if a moveable object is currently being "held" by the player.
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
	public class ActionMoveableCheck : ActionCheck
	{
		
		public DragBase dragObject;
		public int constantID = 0;
		public int parameterID = -1;
		protected DragBase runtimeDragObject;


		public ActionMoveableCheck ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Moveable;
			title = "Check held by player";
			description = "Queries whether or not a Draggable of PickUp object is currently being manipulated.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeDragObject = AssignFile <DragBase> (parameters, parameterID, constantID, dragObject);
		}
		
		
		public override bool CheckCondition ()
		{
			if (runtimeDragObject != null)
			{
				return (KickStarter.playerInput.IsDragObjectHeld (runtimeDragObject));
			}
			return false;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Object to check:", parameters, parameterID, ParameterType.GameObject);
			if (parameterID >= 0)
			{
				constantID = 0;
				dragObject = null;
			}
			else
			{
				dragObject = (DragBase) EditorGUILayout.ObjectField ("Object to check:", dragObject, typeof (DragBase), true);

				constantID = FieldToID <DragBase> (dragObject, constantID);
				dragObject = IDToField <DragBase> (dragObject, constantID, false);
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID <DragBase> (dragObject, constantID, parameterID);
		}


		public override string SetLabel ()
		{
			if (dragObject != null)
			{
				return dragObject.gameObject.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (dragObject != null && dragObject.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Moveable: Check held by player' Action</summary>
		 * <param name = "dragObject">The name of the moveable object to check is being held by the player</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMoveableCheck CreateNew (DragBase dragObject)
		{
			ActionMoveableCheck newAction = (ActionMoveableCheck) CreateInstance <ActionMoveableCheck>();
			newAction.dragObject = dragObject;
			return newAction;
		}
		
	}

}