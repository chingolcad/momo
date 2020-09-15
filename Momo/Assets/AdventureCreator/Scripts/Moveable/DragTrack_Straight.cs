/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"DragTrack_Linear.cs"
 * 
 *	This track constrains Moveable_Drag objects to a straight line
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A track that constrains a Moveable_Drag object along a straight line.
	 * The dragged object can also be made to rotate as it moves: either so it rolls, or rotates around the line's axis (like a screw).
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_drag_track___straight.html")]
	public class DragTrack_Straight : DragTrack
	{

		#region Variables

		/** The way in which the Moveable_Drag object rotates as it moves (None, Roll, Screw) */
		public DragRotationType rotationType = DragRotationType.None;
		/** The track's length */
		public float maxDistance = 2f;
		/** If True, and the Moveable_Drag object rotates like a screw, then the input drag vector must also rotate, so that it is always tangential to the dragged object */
		public bool dragMustScrew = false;
		/** The "thread" if the Moveable_Drag object rotates like a screw - effectively how fast the object rotates as it moves */
		public float screwThread = 1f;
		/** If True, then colliders will be auto-generated at the ends of the track, to add friction/bounce effects when the dragged objects reaches the limits */
		public bool generateColliders = true;

		#endregion


		#region PublicFunctions

		public override void AssignColliders (Moveable_Drag draggable)
		{
			if (!UsesEndColliders)
			{
				base.AssignColliders (draggable);
				return;
			}
			
			if (draggable.maxCollider == null)
			{
				draggable.maxCollider = (Collider) Instantiate (Resources.Load (Resource.dragCollider, typeof (Collider)));
			}
			
			if (draggable.minCollider == null)
			{
				draggable.minCollider = (Collider) Instantiate (Resources.Load (Resource.dragCollider, typeof (Collider)));
			}

			draggable.maxCollider.transform.position = transform.position + (transform.up * maxDistance) + (transform.up * draggable.ColliderWidth);
			draggable.minCollider.transform.position = transform.position - (transform.up * draggable.ColliderWidth);
			
			draggable.minCollider.transform.up = transform.up;
			draggable.maxCollider.transform.up = -transform.up;

			base.AssignColliders (draggable);
		}


		public override void Connect (Moveable_Drag draggable)
		{
			AssignColliders (draggable);
		}


		public override float GetDecimalAlong (Moveable_Drag draggable)
		{
			return (draggable.transform.position - transform.position).magnitude / maxDistance;
		}


		public override void SetPositionAlong (float proportionAlong, Moveable_Drag draggable)
		{
			draggable.transform.position = transform.position + (transform.up * proportionAlong * maxDistance);
			if (rotationType != DragRotationType.None)
			{
				SetRotation (draggable, proportionAlong);
			}

			base.SetPositionAlong (proportionAlong, draggable);
		}


		public override void SnapToTrack (Moveable_Drag draggable, bool onStart)
		{
			Vector3 vec = draggable.transform.position - transform.position;
			float proportionAlong = Vector3.Dot (vec, transform.up) / maxDistance;
			proportionAlong = Mathf.Clamp01 (proportionAlong);
			
			if (onStart)
			{

				if (rotationType != DragRotationType.None)
				{
					SetRotation (draggable, proportionAlong);
				}

				draggable.Rigidbody.velocity = draggable.Rigidbody.angularVelocity = Vector3.zero;
			}

			draggable.transform.position = transform.position + transform.up * proportionAlong * maxDistance;

			// Limit velocity to just along track
			Vector3 localVelocity = transform.InverseTransformDirection(draggable.Rigidbody.velocity);
			localVelocity.x = 0;
			localVelocity.z = 0;
			draggable.Rigidbody.velocity = transform.TransformDirection (localVelocity);
		}


		public override void ApplyAutoForce (float _position, float _speed, Moveable_Drag draggable)
		{
			Vector3 deltaForce = GetForceToPosition (draggable, _position);
			deltaForce *= _speed / draggable.Rigidbody.mass;

			// Limit magnitude
			if (deltaForce.magnitude > draggable.maxSpeed)
			{
				deltaForce *= draggable.maxSpeed / deltaForce.magnitude;
			}
			
			deltaForce -= draggable.Rigidbody.velocity;
			draggable.Rigidbody.AddForce (deltaForce, ForceMode.VelocityChange);
		}


		public override void ApplyDragForce (Vector3 force, Moveable_Drag draggable)
		{
			float dotProduct = 0f;

			if (rotationType == DragRotationType.Screw)
			{
				if (dragMustScrew)
				{
					draggable.UpdateScrewVector ();
				}

				dotProduct = Vector3.Dot (force, draggable._dragVector);
			}
			else
			{
				dotProduct = Vector3.Dot (force, transform.up);
			}

			// Calculate the amount of force along the tangent
			Vector3 tangentForce = transform.up * dotProduct;

			if (rotationType == DragRotationType.Screw)
			{
				if (dragMustScrew)
				{
					// Take radius into account
					tangentForce = (transform.up * dotProduct).normalized * force.magnitude;
					tangentForce /= Mathf.Sqrt ((draggable.GetGrabPosition () - draggable.transform.position).magnitude) / 0.4f;
				}
				tangentForce /= Mathf.Sqrt (screwThread);
			}

			draggable.Rigidbody.AddForce (tangentForce, ForceMode.Force);
		}


		public override float GetScreenPointProportionAlong (Vector2 point)
		{
			Vector3 endPosition = transform.position + (transform.up * maxDistance);

			Vector2 screen_startPosition = KickStarter.CameraMain.WorldToScreenPoint (transform.position);
			Vector2 screen_endPosition = KickStarter.CameraMain.WorldToScreenPoint (endPosition);

			Vector2 startToEnd = screen_startPosition - screen_endPosition;
			Vector2 pointToEnd = point - screen_endPosition;

			float angleFromEnd = Vector2.Angle (startToEnd, pointToEnd);

			return 1f - (Mathf.Cos (angleFromEnd * Mathf.Deg2Rad) * pointToEnd.magnitude / startToEnd.magnitude);
		}


		public override bool IconIsStationary ()
		{
			if (dragMovementCalculation == DragMovementCalculation.CursorPosition)
			{
				return true;
			}

			if (rotationType == DragRotationType.Roll || (rotationType == DragRotationType.Screw && !dragMustScrew))
			{
				return true;
			}
			return false;
		}


		public override void UpdateDraggable (Moveable_Drag draggable)
		{
			SnapToTrack (draggable, false);
			draggable.trackValue = GetDecimalAlong (draggable);
			
			if (rotationType != DragRotationType.None)
			{
				SetRotation (draggable, draggable.trackValue);
			}

			if (!onlySnapOnPlayerRelease)
			{
				DoSnapCheck (draggable);
			}
		}


		public override Vector3 GetGizmoPosition (float proportionAlong)
		{
			return transform.position + (transform.up * proportionAlong * maxDistance);
		}


		public override Vector3 GetForceToPosition (Moveable_Drag draggable, float targetProportionAlong)
		{
			float proportionalDifference = Mathf.Clamp01 (targetProportionAlong) - draggable.trackValue;
			return transform.up * proportionalDifference * 1000f;
		}

		#endregion


		#region ProtectedFunctions

		protected void SetRotation (Moveable_Drag draggable, float proportionAlong)
		{
			float angle = proportionAlong * maxDistance / draggable.ColliderWidth / 2f * Mathf.Rad2Deg;

			if (rotationType == DragRotationType.Roll)
			{
				draggable.Rigidbody.rotation = Quaternion.AngleAxis (angle, transform.forward) * transform.rotation;
			}
			else if (rotationType == DragRotationType.Screw)
			{
				draggable.Rigidbody.rotation = Quaternion.AngleAxis (angle * screwThread, transform.up) * transform.rotation;
			}
		}

		#endregion


		#region GetSet

		public override bool UsesEndColliders
		{
			get
			{
				return generateColliders;
			}
		}

		#endregion

	}
	
}