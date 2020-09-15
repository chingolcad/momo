using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor(typeof(DragTrack))]
	public class DragTrackEditor : Editor
	{

		public void SnapDataGUI (DragTrack _target, bool useAngles)
		{
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Snapping", EditorStyles.boldLabel);
				
			_target.doSnapping = CustomGUILayout.Toggle ("Enable snapping?", _target.doSnapping, "", "If True, then snapping is enabled and any object attached to the track can snap to pre-set points along it when let go by the player");
			if (_target.doSnapping)
			{
				_target.snapSpeed = CustomGUILayout.FloatField ("Snap speed:", _target.snapSpeed, "", "The speed to move by when attached objects snap");
				_target.onlySnapOnPlayerRelease = CustomGUILayout.ToggleLeft ("Only snap on player release?", _target.onlySnapOnPlayerRelease, "", "If True, then snapping will only occur when the player releases the object - and not when moving on its own accord");
				
				for (int i=0; i<_target.allTrackSnapData.Count; i++)
				{
					GUILayout.Box ("", GUILayout.ExpandWidth (true), GUILayout.Height (1));

					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Snap " + _target.allTrackSnapData[i].ID.ToString ());
					GUILayout.FlexibleSpace ();
					if (GUILayout.Button ("-"))
					{
						Undo.RecordObject (this, "Delete snap point");
						_target.allTrackSnapData.RemoveAt (i);
						i=-1;
						break;
					}
					EditorGUILayout.EndHorizontal ();

					_target.allTrackSnapData[i] = _target.allTrackSnapData[i].ShowGUI (useAngles);
					EditorGUILayout.Space ();
				}
				if (GUILayout.Button ("Create new snap point"))
				{
					Undo.RecordObject (this, "Create snap point");
					TrackSnapData trackSnapData = new TrackSnapData (0f, GetSnapIDArray (_target.allTrackSnapData));
					_target.allTrackSnapData.Add (trackSnapData);
				}
			}
			EditorGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}



		private int[] GetSnapIDArray (List<TrackSnapData> allTrackSnapData)
		{
			List<int> idArray = new List<int>();
			if (allTrackSnapData != null)
			{
				foreach (TrackSnapData trackSnapData in allTrackSnapData)
				{
					idArray.Add (trackSnapData.ID);
				}
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}

	}

}