/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"DragTrack.cs"
 * 
 *	The base class for "tracks", which are used to
 *	constrain Moveable_Drag objects along set paths
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * The base class for "tracks", which are used to contrain Moveable_Drag objects along a pre-determined path
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_drag_track.html")]
	public class DragTrack : MonoBehaviour
	{

		#region Variables

		/** The Physics Material to give the track's end colliders */
		public PhysicMaterial colliderMaterial;
		/** The size of the track's end colliders, as seen in the Scene window */
		public float discSize = 0.2f;
		/** The colour of Scene window Handles */
		public Color handleColour = Color.white;
		/** How input movement is calculated (DragVector, CursorPosition) */
		public DragMovementCalculation dragMovementCalculation = DragMovementCalculation.DragVector;
		/** If True, then snapping is enabled and any object attached to the track can snap to pre-set points along it when let go by the player */
		public bool doSnapping = false;
		/** A list of all the points along the track that attached objects can snap to, if doSnapping = True */
		public List<TrackSnapData> allTrackSnapData = new List<TrackSnapData>();
		/** The speed to move by when attached objects snap */
		public float snapSpeed = 100f;
		/** If True, then snapping will only occur when the player releases the object - and not when moving on its own accord */
		public bool onlySnapOnPlayerRelease;
		/** If True, and the track doesn't loop, then the dragged object will be prevented from jumping from one end to the other without first moving somewhere in between */
		public bool preventEndToEndJumping = false;

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Initialises two end colliders for an object that prevent it from moving beyond the track.</summary>
		 * <param name = "draggable">The Moveable_Drag object to create colliders for</param>
		 */
		public virtual void AssignColliders (Moveable_Drag draggable)
		{
			if (UsesEndColliders && draggable.minCollider != null && draggable.maxCollider != null)
			{
				draggable.maxCollider.transform.rotation = Quaternion.AngleAxis (90f, draggable.maxCollider.transform.right) * draggable.maxCollider.transform.rotation;
				draggable.minCollider.transform.rotation = Quaternion.AngleAxis (90f, draggable.minCollider.transform.right) * draggable.minCollider.transform.rotation;

				if (colliderMaterial)
				{
					draggable.maxCollider.material = colliderMaterial;
					draggable.minCollider.material = colliderMaterial;
				}

				draggable.maxCollider.transform.parent = this.transform;
				draggable.minCollider.transform.parent = this.transform;

				draggable.maxCollider.name = draggable.name + "_UpperLimit";
				draggable.minCollider.name = draggable.name + "_LowerLimit";
			}

			LimitCollisions (draggable);
		}


		/**
		 * <summary>Gets the proportion along the track that an object is positioned.</summary>
		 * <param name = "draggable">The Moveable_Drag object to check the position of</param>
		 * <returns>The proportion along the track that the Moveable_Drag object is positioned (0 to 1)</returns>
		 */
		public virtual float GetDecimalAlong (Moveable_Drag draggable)
		{
			return 0f;
		}


		/**
		 * <summary>Positions an object on a specific point along the track.</summary>
		 * <param name = "proportionAlong">The proportion along which to place the Moveable_Drag object (0 to 1)</param>
		 * <param name = "draggable">The Moveable_Drag object to reposition</param>
		 */
		public virtual void SetPositionAlong (float proportionAlong, Moveable_Drag draggable)
		{
			draggable.trackValue = proportionAlong;
		}


		/**
		 * <summary>Connects an object to the track when the game begins.</summary>
		 * <param name = "draggable">The Moveable_Drag object to connect to the track</param>
		 */
		public virtual void Connect (Moveable_Drag draggable)
		{}


		/**
		 * <summary>Applies a force to an object connected to the track.</summary>
		 * <param name = "force">The drag force vector input by the player</param>
		 * <param name = "draggable">The Moveable_Drag object to apply the force to</param>
		 */
		public virtual void ApplyDragForce (Vector3 force, Moveable_Drag draggable)
		{}


		/**
		 * <summary>Gets the proportion along the track closest to a given position in screen-space</summary>
		 * <param name = "point">The position in screen-space</param>
		 * <returns>The proportion along the track closest to a given position in screen-space</returns>
		 */
		public virtual float GetScreenPointProportionAlong (Vector2 point)
		{
			return 0f;
		}


		/**
		 * <summary>Applies a force that, when applied every frame, pushes an object connected to the track towards a specific point along it.</summary>
		 * <param name = "_position">The proportion along which to place the Moveable_Drag object (0 to 1)</param>
		 * <param name = "_speed">The speed to move by</param>
		 * <param name = "draggable">The draggable object to move</param>
		 */
		public virtual void ApplyAutoForce (float _position, float _speed, Moveable_Drag draggable)
		{}


		/**
		 * <summary>Updates the position of an object connected to the track. This is called every frame.</summary>
		 * <param name = "draggable">The Moveable_Drag object to update the position of</param>
		 */
		public virtual void UpdateDraggable (Moveable_Drag draggable)
		{
			draggable.trackValue = GetDecimalAlong (draggable);

			if (!onlySnapOnPlayerRelease)
			{
				DoSnapCheck (draggable);
			}
		}


		/**
		 * <summary>Called whenever an object attached to the track is let go by the player</summary>
		 * <param name = "draggable">The draggable object</param>
		 */
		public void OnLetGo (Moveable_Drag draggable)
		{
			DoSnapCheck (draggable);
		}


		/**
		 * <summary>Corrects the position of an object so that it is placed along the track.</summary>
		 * <param name = "draggable">The Moveable_Drag object to snap onto the track</param>
		 * <param name = "onStart">Is True if the game has just begun (i.e. this function is being run for the first time)</param>
		 */
		public virtual void SnapToTrack (Moveable_Drag draggable, bool onStart)
		{}


		/**
		 * <summary>Checks if the icon that can display when an object is moved along the track remains in the same place as the object moves.</summary>
		 * <returns>True if the icon remains in the same place (always False unless overridden by subclasses)</returns>
		 */
		public virtual bool IconIsStationary ()
		{
			return false;
		}


		/**
		 * <summary>Gets the position of gizmos at a certain position along the track</summary>
		 * <param name = "proportionAlong">The proportio along the track to get the gizmo position of</param>
		 * <returns>The position of the gizmo</returns>
		 */
		public virtual Vector3 GetGizmoPosition (float proportionAlong)
		{
			return transform.position;
		}


		/**
		 * <summary>Calculates a force to get a draggable object to a given point along the track</summary>
		 * <param name = "draggable">The draggable object</param>
		 * <param name = "targetProportionAlong">How far along the track to calculate a force for</param>
		 * <returns>The force vector, in world space</returns>
		 */
		public virtual Vector3 GetForceToPosition (Moveable_Drag draggable, float targetProportionAlong)
		{
			return Vector3.zero;
		}



		/*
		 * <summary>Gets the current intensity of a draggable object's movement sound</summary>
		 * <param name = "draggable">The draggable object</param>
		 * <returns>The current intensity of a draggable object's movement sound</summary>
		 */
		public virtual float GetMoveSoundIntensity (Moveable_Drag draggable)
		{
			return draggable.Rigidbody.velocity.magnitude;
		}


		public TrackSnapData GetSnapData (int ID)
		{
			foreach (TrackSnapData trackSnapData in allTrackSnapData)
			{
				if (trackSnapData.ID == ID)
				{
					return trackSnapData;
				}
			}
			return null;
		}


		/**
		 * <summary>Checks if a region along the track is within a given snap region</summary>
		 * <param name = "trackValue">The distance along the track, as a decimal of its total length</param>
		 * <param name = "snapID">The ID number of the snap region</param>
		 * <returns>True if a region along the track is within the snap's region</region>
		 */
		public bool IsWithinSnapRegion (float trackValue, int snapID)
		{
			foreach (TrackSnapData trackSnapData in allTrackSnapData)
			{
				if (trackSnapData.ID == snapID)
				{
					return trackSnapData.IsWithinRegion (trackValue);
				}
			}
			return false;
		}

		#endregion


		#region ProtectedFunctions

		protected void DoSnapCheck (Moveable_Drag draggable)
		{
			if (doSnapping && !draggable.IsAutoMoving () && !draggable.IsHeld)
			{
				SnapToNearest (draggable);
			}
		}


		protected void SnapToNearest (Moveable_Drag draggable)
		{
			int bestIndex = -1;
			float minDistanceFrom = Mathf.Infinity;

			for (int i=0; i<allTrackSnapData.Count; i++)
			{
				float thisDistanceFrom = allTrackSnapData[i].GetDistanceFrom (draggable.trackValue);
				if (thisDistanceFrom < minDistanceFrom)
				{
					bestIndex = i;
					minDistanceFrom = thisDistanceFrom;
				}
			}

			if (bestIndex >= 0)
			{
				allTrackSnapData[bestIndex].MoveTo (draggable, snapSpeed);
			}
		}


		protected void LimitCollisions (Moveable_Drag draggable)
		{
			Collider[] allColliders = FindObjectsOfType (typeof(Collider)) as Collider[];
			Collider[] dragColliders = draggable.GetComponentsInChildren <Collider>();

			// Disable all collisions on max/min colliders
			if (draggable.minCollider != null && draggable.maxCollider != null)
			{
				foreach (Collider _collider in allColliders)
				{
					if (_collider.enabled)
					{
						if (_collider != draggable.minCollider && draggable.minCollider.enabled)
						{
							Physics.IgnoreCollision (_collider, draggable.minCollider, true);
						}
						if (_collider != draggable.maxCollider && draggable.maxCollider.enabled)
						{
							Physics.IgnoreCollision (_collider, draggable.maxCollider, true);
						}
					}
				}
			}

			// Set collisions on draggable's colliders
			foreach (Collider _collider in allColliders)
			{
				if (_collider.GetComponent <AC_Trigger>() != null) continue;

				foreach (Collider dragCollider in dragColliders)
				{
					if (_collider == dragCollider)
					{
						continue;
					}

					bool result = true;

					if ((draggable.minCollider != null && draggable.minCollider == _collider) || (draggable.maxCollider != null && draggable.maxCollider == _collider))
					{
						result = false;
					}
					else if (KickStarter.player != null && _collider.gameObject == KickStarter.player.gameObject)
					{
						result = draggable.ignorePlayerCollider;
					}
					else if (_collider.GetComponent <Rigidbody>() && _collider.gameObject != draggable.gameObject)
					{
						if (_collider.GetComponent <Moveable>())
						{
							result = draggable.ignoreMoveableRigidbodies;
						}
						else
						{
							result = false;
						}
					}

					if (_collider.enabled && dragCollider.enabled)
					{
						Physics.IgnoreCollision (_collider, dragCollider, result);
					}
				}
			}

			// Enable collisions between max/min collisions and draggable's colliders
			if (draggable.minCollider != null && draggable.maxCollider != null)
			{
				foreach (Collider _collider in dragColliders)
				{
					if (_collider.enabled && draggable.minCollider.enabled)
					{
						Physics.IgnoreCollision (_collider, draggable.minCollider, false);
					}
					if (_collider.enabled && draggable.maxCollider.enabled)
					{
						Physics.IgnoreCollision (_collider, draggable.maxCollider, false);
					}
				}
			}
		}


		protected Vector3 RotatePointAroundPivot (Vector3 point, Vector3 pivot, Quaternion rotation)
		{
			return rotation * (point - pivot) + pivot;
		}

		#endregion


		#region GetSet		

		/** Checks if the track is on a loop */
		public virtual bool Loops
		{
			get
			{
				return false;
			}
		}


		/**
		 * If True, end-colliders are generated to prevent draggable objects from leaving the track's boundaries
		 */
		public virtual bool UsesEndColliders
		{
			get
			{
				return false;
			}
		}

		#endregion

	}

}
