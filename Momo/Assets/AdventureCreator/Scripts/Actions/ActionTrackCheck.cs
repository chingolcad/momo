/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionMoveableCheck.cs"
 * 
 *	This action checks the position of a Drag object
 *	along a locked track
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
	public class ActionTrackCheck : ActionCheck
	{

		public Moveable_Drag dragObject;
		public int dragConstantID = 0;
		public int dragParameterID = -1;
		protected Moveable_Drag runtimeDragObject;

		public float checkPosition;
		public int checkPositionParameterID = -1;

		public float errorMargin = 0.05f;
		public IntCondition condition;

		public int snapID = 0;
		public int snapParameterID = -1;

		[SerializeField] protected TrackCheckMethod method = TrackCheckMethod.PositionValue;
		protected enum TrackCheckMethod { PositionValue, WithinSnapRegion };


		public ActionTrackCheck ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Moveable;
			title = "Check track position";
			description = "Queries how far a Draggable object is along its track.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeDragObject = AssignFile <Moveable_Drag> (parameters, dragParameterID, dragConstantID, dragObject);
			
			checkPosition = AssignFloat (parameters, checkPositionParameterID, checkPosition);
			checkPosition = Mathf.Max (0f, checkPosition);
			checkPosition = Mathf.Min (1f, checkPosition);

			snapID = AssignInteger (parameters, snapParameterID, snapID);
		}

			
		public override ActionEnd End (List<AC.Action> actions)
		{
			return ProcessResult (CheckCondition (), actions);
		}
		
		
		public override bool CheckCondition ()
		{
			if (runtimeDragObject == null) return false;

			if (method == TrackCheckMethod.PositionValue)
			{
				float actualPositionAlong = runtimeDragObject.GetPositionAlong ();

				switch (condition)
				{
					case IntCondition.EqualTo:
						if (actualPositionAlong > (checkPosition - errorMargin) && actualPositionAlong < (checkPosition + errorMargin))
						{
							return true;
						}
						break;

					case IntCondition.NotEqualTo:
						if (actualPositionAlong < (checkPosition - errorMargin) || actualPositionAlong > (checkPosition + errorMargin))
						{
							return true;
						}
						break;

					case IntCondition.LessThan:
						if (actualPositionAlong < checkPosition)
						{
							return true;
						}
						break;

					case IntCondition.MoreThan:
						if (actualPositionAlong > checkPosition)
						{
							return true;
						}
						break;
				}
			}
			else if (method == TrackCheckMethod.WithinSnapRegion)
			{
				if (runtimeDragObject.track != null)
				{
					return runtimeDragObject.track.IsWithinSnapRegion (runtimeDragObject.trackValue, snapID);
				}
			}

			return false;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			dragParameterID = Action.ChooseParameterGUI ("Drag object:", parameters, dragParameterID, ParameterType.GameObject);
			if (dragParameterID < 0 || method == TrackCheckMethod.WithinSnapRegion)
			{
				string label = (dragParameterID < 0) ? "Placeholder drag object:" : "Drag object";

				dragObject = (Moveable_Drag) EditorGUILayout.ObjectField (label, dragObject, typeof (Moveable_Drag), true);
				
				dragConstantID = FieldToID <Moveable_Drag> (dragObject, dragConstantID);
				dragObject = IDToField <Moveable_Drag> (dragObject, dragConstantID, false);
				
				if (dragObject != null && dragObject.dragMode != DragMode.LockToTrack)
				{
					EditorGUILayout.HelpBox ("The chosen Drag object must be in 'Lock To Track' mode", MessageType.Warning);
				}
			}

			method = (TrackCheckMethod) EditorGUILayout.EnumPopup ("Method:", method);
			if (method == TrackCheckMethod.PositionValue)
			{
				condition = (IntCondition) EditorGUILayout.EnumPopup ("Condition:", condition);

				checkPositionParameterID = Action.ChooseParameterGUI ("Position:", parameters, checkPositionParameterID, ParameterType.Float);
				if (checkPositionParameterID < 0)
				{
					checkPosition = EditorGUILayout.Slider ("Position:", checkPosition, 0f, 1f);
				}

				if (condition == IntCondition.EqualTo || condition == IntCondition.NotEqualTo)
				{
					errorMargin = EditorGUILayout.Slider ("Error margin:", errorMargin, 0f, 1f);
				}
			}
			else if (method == TrackCheckMethod.WithinSnapRegion)
			{
				if (dragObject == null)
				{
					EditorGUILayout.HelpBox ("A drag object must be assigned above.", MessageType.Warning);
				}
				else if (dragObject.track == null || dragObject.dragMode != DragMode.LockToTrack)
				{
					EditorGUILayout.HelpBox ("The chosen Drag object must have an assigned Track.", MessageType.Warning);
				}
				else if (!dragObject.track.doSnapping || dragObject.track.allTrackSnapData == null || dragObject.track.allTrackSnapData.Count == 0)
				{
					EditorGUILayout.HelpBox ("The chosen Drag object's Track has no snap points.", MessageType.Warning);
				}
				else
				{
					snapParameterID = Action.ChooseParameterGUI ("Snap ID:", parameters, snapParameterID, ParameterType.Integer);
					if (snapParameterID < 0)
					{
						List<string> labelList = new List<string>();
						int snapIndex = 0;
						
						for (int i=0; i<dragObject.track.allTrackSnapData.Count; i++)
						{
							labelList.Add (dragObject.track.allTrackSnapData[i].EditorLabel);
							
							if (dragObject.track.allTrackSnapData[i].ID == snapID)
							{
								snapIndex = i;
							}
						}
						
						snapIndex = EditorGUILayout.Popup ("Snap:", snapIndex, labelList.ToArray ());
						snapID = dragObject.track.allTrackSnapData[snapIndex].ID;
					}
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID <Moveable_Drag> (dragObject, dragConstantID, dragParameterID);
		}


		public override string SetLabel ()
		{
			if (dragObject != null)
			{
				return (dragObject.gameObject.name + " " + condition.ToString () + " " + checkPosition);
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (dragParameterID < 0)
			{
				if (dragObject != null && dragObject.gameObject == gameObject) return true;
				if (dragConstantID == id) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Check track position' Action</summary>
		 * <param name = "dragObject">The moveable object to query</param>
		 * <param name = "trackPosition">The track position to check for</param>
		 * <param name = "condition">The condition to make</param>
		 * <param name = "errorMargin">The maximum difference between the queried track position, and the true track position, for the condition to be met</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTrackCheck CreateNew (Moveable_Drag dragObject, float trackPosition, IntCondition condition = IntCondition.MoreThan, float errorMargin = 0.05f)
		{
			ActionTrackCheck newAction = (ActionTrackCheck) CreateInstance <ActionTrackCheck>();
			newAction.method = TrackCheckMethod.PositionValue;
			newAction.dragObject = dragObject;
			newAction.checkPosition = trackPosition;
			newAction.errorMargin = errorMargin;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Check track position' Action</summary>
		 * <param name = "dragObject">The moveable object to query</param>
		 * <param name = "snapRegionID">The ID of the track's snap region</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTrackCheck CreateNew (Moveable_Drag dragObject, int snapRegionID)
		{
			ActionTrackCheck newAction = (ActionTrackCheck) CreateInstance <ActionTrackCheck>();
			newAction.method = TrackCheckMethod.WithinSnapRegion;
			newAction.dragObject = dragObject;
			newAction.snapID = snapRegionID;
			return newAction;
		}

	}

}