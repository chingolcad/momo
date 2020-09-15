/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"TrackSnapData.cs"
 * 
 *	Stores information related to snapping draggable objects along tracks.
 * 
 */
 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	/**
	 * Stores information related to snapping draggable objects along tracks.
	 */
	[System.Serializable]
	public class TrackSnapData
	{

		#region Variables

		[SerializeField] protected float positionAlong;
		[SerializeField] protected float width;
		[SerializeField] protected int id;
		#if UNITY_EDITOR
		[SerializeField] protected Color gizmoColor;
		#endif

		#endregion


		#region Constructors

		/**
		 * The default constructor
		 */
		public TrackSnapData (float _positionAlong, int[] idArray)
		{
			positionAlong = _positionAlong;
			width = 0.1f;
			#if UNITY_EDITOR
			gizmoColor = Color.blue;
			#endif

			id = 0;
			// Update id based on array
			if (idArray != null && idArray.Length > 0)
			{
				foreach (int _id in idArray)
				{
					if (id == _id)
						id ++;
				}
			}
		}

		#endregion


		#region PublicFunctions

		#if UNITY_EDITOR

		public TrackSnapData ShowGUI (bool useAngles)
		{
			positionAlong = CustomGUILayout.Slider ("Snap to " + ((useAngles) ? "angle" : "position:"), positionAlong, 0f, 1f, "", "How far along the track (as a decimal) to snap to.");
			width = CustomGUILayout.Slider ("Catchment size:", width, 0f, 1f, "", "How far apart from the snapping point (as a decimal of the track's length) the object can be for this to be enforced.");
			gizmoColor = CustomGUILayout.ColorField ("Editor colour:", gizmoColor, "", "What colour to draw handles in the Scene with.");
			return this;
		}

		#endif


		/**
		 * <summary>Gets the distance, in Unity units, between a region along the track and the centre of a snapping point</summary>
		 * <param name = "trackValue">The distance along the track, as a decimal of its total length</param>
		 * <returns>The distance between the draggable object and the centre of the snapping point</returns>
		 */
		public float GetDistanceFrom (float trackValue)
		{
			float distance = positionAlong - trackValue;

			if (Mathf.Abs (distance) > width)
			{
				return Mathf.Infinity;
			}			
			return distance;
		}


		/**
		 * <summary>Moves a draggable object towards the snap point</summary>
		 * <param name = "draggable">The object to move</param>
		 * <param name = "speed">How fast to move the object by</param>
		 */
		public void MoveTo (Moveable_Drag draggable, float speed)
		{
			draggable.AutoMoveAlongTrack (positionAlong, speed, true, 1 << 0, ID);
		}


		/**
		 * <summary>Checks if a region along the track is within the snap's region</summary>
		 * <param name = "trackValue">The distance along the track, as a decimal of its total length</param>
		 * <returns>True if a region along the track is within the snap's region</region>
		 */
		public bool IsWithinRegion (float trackValue)
		{
			if (GetDistanceFrom (trackValue) <= width)
			{
				return true;
			}
			return false;
		}

		#endregion


		#region GetSet

		/** How far along the track the snap point is */
		public float PositionAlong
		{
			get
			{
				return positionAlong;
			}
		}


		/** How wide, as a proportion of the track length, the snap point is valid for */
		public float Width
		{
			get
			{
				return width;
			}
		}


		/** A unique identifier */
		public int ID
		{
			get
			{
				return id;
			}
		}


		#if UNITY_EDITOR

		public string EditorLabel
		{
			get
			{
				return (id.ToString () + ": " + positionAlong.ToString ());
			}
		}


		public Color GizmoColor
		{
			get
			{
				return gizmoColor;
			}
		}

		#endif

		#endregion

	}

}