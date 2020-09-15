using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(DragTrack_Straight))]
	public class DragTrack_StraightEditor : DragTrackEditor
	{
		
		public override void OnInspectorGUI ()
		{
			DragTrack_Straight _target = (DragTrack_Straight) target;
			
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Track shape:", EditorStyles.boldLabel);
			
			_target.maxDistance = CustomGUILayout.FloatField ("Length:", _target.maxDistance, "", "The track's length");
			_target.handleColour = CustomGUILayout.ColorField ("Handles colour:", _target.handleColour, "", "The colour of Scene window Handles");
			_target.rotationType = (DragRotationType) CustomGUILayout.EnumPopup ("Rotation type:", _target.rotationType, "", "The way in which the Moveable_Drag object rotates as it moves");
			
			if (_target.rotationType == DragRotationType.Screw)
			{
				_target.screwThread = CustomGUILayout.FloatField ("Screw thread:", _target.screwThread, "", "The 'thread' if the Moveable_Drag object rotates like a screw - effectively how fast the object rotates as it moves");
			}

			_target.dragMovementCalculation = (DragMovementCalculation) CustomGUILayout.EnumPopup ("Movement input:", _target.dragMovementCalculation);

			if (_target.rotationType == DragRotationType.Screw && _target.dragMovementCalculation == DragMovementCalculation.DragVector)
			{
				_target.dragMustScrew = CustomGUILayout.Toggle ("Drag must rotate too?", _target.dragMustScrew, "", "If True, then the input drag vector must also rotate, so that it is always tangential to the dragged object");
			}
			_target.discSize = CustomGUILayout.Slider ("Gizmo size:", _target.discSize, 0f, 2f, "", "The size of the track's ends, as seen in the Scene window");
			
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("End-colliders", EditorStyles.boldLabel);
			
			_target.generateColliders = CustomGUILayout.Toggle ("Generate end-colliders?", _target.generateColliders);

			if (_target.generateColliders)
			{
				_target.colliderMaterial = (PhysicMaterial) CustomGUILayout.ObjectField <PhysicMaterial> ("Material:", _target.colliderMaterial, false, "", "Physics Material to give the track's end colliders");
			}
			
			EditorGUILayout.EndVertical ();

			SnapDataGUI (_target, false);
			
			UnityVersionHandler.CustomSetDirty (_target);
		}
		
		
		public void OnSceneGUI ()
		{
			DragTrack_Straight _target = (DragTrack_Straight) target;
			
			Handles.color = _target.handleColour;
			Vector3 maxPosition = _target.GetGizmoPosition (1f);
			maxPosition = Handles.PositionHandle (maxPosition, Quaternion.identity);
			Handles.DrawSolidDisc (maxPosition, -_target.transform.up, _target.discSize);
			_target.maxDistance = Vector3.Dot (maxPosition - _target.transform.position, _target.transform.up);
			
			Handles.color = new Color (_target.handleColour.r / 2f, _target.handleColour.g / 2f, _target.handleColour.b / 2f, _target.handleColour.a);
			Handles.DrawSolidDisc (_target.GetGizmoPosition (0f), _target.transform.up, _target.discSize);
			
			Handles.color = _target.handleColour;
			Handles.DrawLine (_target.GetGizmoPosition (0f), maxPosition);

			UnityVersionHandler.CustomSetDirty (_target);

			if (_target.doSnapping)
			{
				foreach (TrackSnapData trackSnapData in _target.allTrackSnapData)
				{
					DrawSnapHandles (trackSnapData, _target);
				}
			}
		}


		private void DrawSnapHandles (TrackSnapData trackSnapData, DragTrack_Straight _target)
        {
            float minPositionAlong = Mathf.Clamp01 (trackSnapData.PositionAlong - trackSnapData.Width);
            float maxPositionAlong = Mathf.Clamp01 (trackSnapData.PositionAlong + trackSnapData.Width);

            Handles.color = trackSnapData.GizmoColor;
            Handles.DrawSolidDisc (_target.GetGizmoPosition (trackSnapData.PositionAlong), _target.transform.up, _target.discSize / 2f);
			Handles.DrawSolidDisc (_target.GetGizmoPosition (minPositionAlong), _target.transform.up, _target.discSize / 4f);
			Handles.DrawSolidDisc (_target.GetGizmoPosition (maxPositionAlong), _target.transform.up, _target.discSize / 4f);
            Handles.DrawLine (_target.GetGizmoPosition (minPositionAlong), _target.GetGizmoPosition (maxPositionAlong));
        }
		
	}

}