/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Moveable_Drag.cs"
 * 
 *	Attach this script to a GameObject to make it
 *	moveable according to a set method, either
 *	by the player or through Actions.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Attaching this component to a GameObject allows it to be dragged, through physics, according to a set method.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_moveable___drag.html")]
	public class Moveable_Drag : DragBase, iActionListAssetReferencer
	{

		#region Variables

		/** The way in which the object can be dragged (LockedToTrack, MoveAlongPlane, RotateOnly) */
		public DragMode dragMode = DragMode.LockToTrack;
		/** The DragTrack the object is locked to (if dragMode = DragMode.LockToTrack */
		public DragTrack track;
		/** If True, and dragMode = DragMode.LockToTrack, then the position and rotation of all child objects will be maintained when the object is attached to the track */
		public bool retainOriginalTransform = false;

		/** If True, and the object is locked to a DragTrack, then the object will be placed at a specific point along the track when the game begins */
		public bool setOnStart = true;
		/** How far along its DragTrack that the object should be placed at when the game begins */
		public float trackValueOnStart = 0f;
		
		/** Where to locate interactions */
		public ActionListSource actionListSource = ActionListSource.InScene;
		/** The Interaction to run whenever the object is moved by the player (and actionListSource = ActionListSource.InScene) */
		public Interaction interactionOnMove = null;
		/** The Interaction to run whenever the object is let go by the player (and actionListSource = ActionListSource.InScene) */
		public Interaction interactionOnDrop = null;
		/** The ActionListAsset to run whenever the object is moved by the player (and actionListSource = ActionListSource.AssetFile) */
		public ActionListAsset actionListAssetOnMove = null;
		/** The ActionListAsset to run whenever the object is let go by the player (and actionListSource = ActionListSource.AssetFile) */
		public ActionListAsset actionListAssetOnDrop = null;
		/** The parameter ID to set as this object in the interactionOnMove / actionListAssetOnMove ActionLists */
		public int moveParameterID = -1;
		/** The parameter ID to set as this object in the interactionOnDrop / actionListAssetOnDrop ActionLists */
		public int dropParameterID = -1;

		/** What movement is aligned to, if dragMode = DragMode.MoveAlongPlane (AlignToCamera, AlignToPlane) */
		public AlignDragMovement alignMovement = AlignDragMovement.AlignToCamera;
		/** The plane to align movement to, if alignMovement = AlignDragMovement.AlignToPlane */
		public Transform plane;
		/** If True, then gravity will be disabled on the object while it is held by the player */
		public bool noGravityWhenHeld = true;
		/** If True, then movement will occur by affecting the Rigidbody, as opposed to directly manipulating the Transform */
		public bool moveWithRigidbody = true;

		protected Vector3 grabPositionRelative;

		protected float colliderRadius = 0.5f;
		protected float grabDistance = 0.5f;
		/** How far along a track the object is, if it is locked to one */
		[HideInInspector] public float trackValue;
		/** A vector used in drag calculations */
		[HideInInspector] public Vector3 _dragVector;

		/** The upper-limit collider when locked to a DragTrack. */
		[HideInInspector] public Collider maxCollider;
		/** The lower-limit collider when locked to a DragTrack */
		[HideInInspector] public Collider minCollider;
		/** The number of revolutions the object has been rotated by, if placed on a DragTrack_Hinge */
		[HideInInspector] public int revolutions = 0;

		protected bool canPlayCollideSound = false;
		protected float screenToWorldOffset;

		protected float lastFrameTotalCursorPositionAlong;
		protected bool endLocked = false;

		protected AutoMoveTrackData activeAutoMove;
		public bool canCallSnapEvents = true;

		private Vector3 thisFrameTorque;
		/** The amount of damping to apply when rotating an object without a Rigidbody */
		public float toruqeDamping = 2f;
		private LerpUtils.Vector3Lerp torqueDampingLerp = new LerpUtils.Vector3Lerp (true);

		#endregion


		#region UnityStandards

		protected override void Awake ()
		{
			base.Awake ();

			if (_rigidbody)
			{
				SetGravity (true);

				if (dragMode == DragMode.RotateOnly)
				{
					if (_rigidbody.constraints == RigidbodyConstraints.FreezeRotation ||
						_rigidbody.constraints == RigidbodyConstraints.FreezeRotationX ||
						_rigidbody.constraints == RigidbodyConstraints.FreezeRotationY ||
						_rigidbody.constraints == RigidbodyConstraints.FreezeRotationZ)
					{
						ACDebug.LogWarning ("Draggable " + gameObject.name + " has a Drag Mode of 'RotateOnly', but its rigidbody rotation is constrained. This may lead to inconsistent behaviour, and using a HingeTrack is advised instead.", gameObject);
					}
				}
			}
			else
			{
				if (dragMode == DragMode.RotateOnly && moveWithRigidbody)
				{
					moveWithRigidbody = false;
				}
				else
				{
					ACDebug.LogWarning ("A Rigidbody is required on the Draggable object " + name, this);
				}
			}

			SphereCollider sphereCollider = GetComponent <SphereCollider>();
			if (sphereCollider != null)
			{
				colliderRadius = sphereCollider.radius * transform.localScale.x;
			}
			else if (dragMode == DragMode.LockToTrack && track != null && track.UsesEndColliders)
			{
				ACDebug.LogWarning ("Cannot calculate collider radius for Draggable object '" + gameObject.name + "' - it should have either a SphereCollider attached, even if it's disabled.", this);
			}

			if (dragMode == DragMode.LockToTrack)
			{
				StartCoroutine (InitToTrack ());
			}
		}


		public override void _FixedUpdate ()
		{
			if (activeAutoMove != null)
			{
				activeAutoMove.Update (track, this);
			}
			else if (dragMode == DragMode.RotateOnly && !moveWithRigidbody)
			{
				transform.Rotate (thisFrameTorque, Space.World);
				//thisFrameTorque = Vector3.Lerp (thisFrameTorque, Vector3.zero, toruqeDamping * Time.deltaTime);
				thisFrameTorque = torqueDampingLerp.Update (thisFrameTorque, Vector3.zero, toruqeDamping);
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Gets how far the object is along its DragTrack.</summary>
		 * <returns>How far the object is along its DragTrack. This is normally 0 to 1, but if the object is locked to a looped DragTrack_Hinge, then the number of revolutions will be added to the result.</returns>
		 */
		public float GetPositionAlong ()
		{
			if (dragMode == DragMode.LockToTrack && track && track is DragTrack_Hinge)
			{
				return trackValue + (float) revolutions;
			}
			return trackValue;
		}


		public override void UpdateMovement ()
		{
			base.UpdateMovement ();
		
			if (dragMode == DragMode.LockToTrack && track)
			{
				track.UpdateDraggable (this);

				if (_rigidbody.angularVelocity != Vector3.zero || _rigidbody.velocity != Vector3.zero)
				{
					RunInteraction (true);
				}

				if (IsAutoMoving () && activeAutoMove.CheckForEnd (this))
				{
					StopAutoMove (true);
				}

				if (collideSound && collideSoundClip && track is DragTrack_Hinge)
				{
					if (trackValue > 0.05f && trackValue < 0.95f)
					{
						canPlayCollideSound = true;
					}
					else if ((Mathf.Approximately (trackValue, 0f) || (!onlyPlayLowerCollisionSound && Mathf.Approximately (trackValue, 1f))) && canPlayCollideSound)
					{
						canPlayCollideSound = false;
						collideSound.Play (collideSoundClip, false);
					}
				}
			}
			else if (isHeld)
			{
				if (dragMode == DragMode.RotateOnly && allowZooming && distanceToCamera > 0f)
				{
					LimitZoom ();
				}
			}

			if (moveSoundClip && moveSound)
			{
				if (dragMode == DragMode.LockToTrack && track != null)
				{
					PlayMoveSound (track.GetMoveSoundIntensity (this), trackValue);
				}
				else
				{
					PlayMoveSound (_rigidbody.velocity.magnitude, trackValue);
				}
			}
		}
		

		/**
		 * Draws an icon at the point of contact on the object, if appropriate.
		 */
		public override void DrawGrabIcon ()
		{
			if (isHeld && showIcon && KickStarter.CameraMain.WorldToScreenPoint (transform.position).z > 0f && icon != null)
			{
				if (dragMode == DragMode.LockToTrack && track != null && track.IconIsStationary ())
				{
					Vector3 screenPosition = KickStarter.CameraMain.WorldToScreenPoint (grabPositionRelative + transform.position);
					icon.Draw (new Vector3 (screenPosition.x, screenPosition.y));
				}
				else
				{
					Vector3 screenPosition = KickStarter.CameraMain.WorldToScreenPoint (grabPoint.position);
					icon.Draw (new Vector3 (screenPosition.x, screenPosition.y));
				}
			}
		}


		/**
		 * <summary>Applies a drag force on the object, based on the movement of the cursor.</summary>
		 * <param name = "force">The force vector to apply</param>
		 * <param name = "mousePosition">The position of the mouse</param>
		 * <param name = "_distanceToCamera">The distance between the object's centre and the camera</param>
		 */
		public override void ApplyDragForce (Vector3 force, Vector3 mousePosition, float _distanceToCamera)
		{
			distanceToCamera = _distanceToCamera;

			// Scale force
			force *= speedFactor * distanceToCamera / 50f;

			// Limit magnitude
			if (force.magnitude > maxSpeed)
			{
				force *= maxSpeed / force.magnitude;
			}

			if (dragMode == DragMode.LockToTrack)
			{
				if (track != null)
				{
					switch (track.dragMovementCalculation)
					{
						case DragMovementCalculation.DragVector:
							track.ApplyDragForce (force, this);
							break;

						case DragMovementCalculation.CursorPosition:
							float mousePositionAlong = track.GetScreenPointProportionAlong (mousePosition);
							float totalPositionAlong = mousePositionAlong + screenToWorldOffset;

							if (track.preventEndToEndJumping)
							{
								bool inDeadZone = (totalPositionAlong >= 1f || totalPositionAlong <= 0f);
								if (endLocked)
								{
									if (!inDeadZone)
									{
										endLocked = false;
									}
								}
								else
								{
									if (inDeadZone)
									{
										endLocked = true;
									}
								}
									
								if (track.Loops || !endLocked)
								{
									lastFrameTotalCursorPositionAlong = totalPositionAlong;
								}
								else
								{
									totalPositionAlong = lastFrameTotalCursorPositionAlong;
								}
							}
								
							track.ApplyAutoForce (totalPositionAlong, speedFactor / 10f, this);
							break;
					}
				}
			}
			else
			{
				Vector3 newRot = Vector3.Cross (force, KickStarter.CameraMain.transform.forward);

				if (dragMode == DragMode.MoveAlongPlane)
				{
					if (alignMovement == AlignDragMovement.AlignToPlane)
					{
						if (plane)
						{
							_rigidbody.AddForceAtPosition (Vector3.Cross (newRot, plane.up), transform.position + (plane.up * grabDistance));
						}
						else
						{
							ACDebug.LogWarning ("No alignment plane assigned to " + this.name, this);
						}
					}
					else
					{
						_rigidbody.AddForceAtPosition (force, transform.position - (KickStarter.CameraMain.transform.forward * grabDistance));
					}
				}
				else if (dragMode == DragMode.RotateOnly)
				{
					newRot /= Mathf.Sqrt ((grabPoint.position - transform.position).magnitude) * 2.4f * rotationFactor;

					if (moveWithRigidbody)
					{
						_rigidbody.AddTorque (newRot);
					}
					else
					{
						//transform.Rotate (newRot, Space.World);
						thisFrameTorque = newRot;
					}

					if (allowZooming)
					{
						UpdateZoom ();
					}
				}
			}
		}


		/**
		 * Detaches the object from the player's control.
		 */
		public override void LetGo (bool ignoreEvents = false)
		{
			canCallSnapEvents = true;

			SetGravity (true);

			if (dragMode == DragMode.RotateOnly && moveWithRigidbody)
			{
				_rigidbody.velocity = Vector3.zero;
			}

			if (!ignoreEvents)
			{
				RunInteraction (false);
			}

			base.LetGo (ignoreEvents);
			
			if (dragMode == DragMode.LockToTrack && track != null)
			{
				track.OnLetGo (this);
			}
		}


		/**
		 * <summary>Attaches the object to the player's control.</summary>
		 * <param name = "grabPosition">The point of contact on the object</param>
		 */
		public override void Grab (Vector3 grabPosition)
		{
			isHeld = true;
			grabPoint.position = grabPosition;
			grabPositionRelative = grabPosition - transform.position;
			grabDistance = grabPositionRelative.magnitude;

			if (dragMode == DragMode.LockToTrack && track != null)
			{
				StopAutoMove (false);

				if (track.dragMovementCalculation == DragMovementCalculation.CursorPosition)
				{
					screenToWorldOffset = trackValue - track.GetScreenPointProportionAlong (KickStarter.playerInput.GetMousePosition ());
					endLocked = false;

					if (track.Loops)
					{
						if (trackValue < 0.5f && screenToWorldOffset < -0.5f)
						{
							// Other side
							screenToWorldOffset += 1f;
						}
						else if (trackValue > 0.5f && screenToWorldOffset > 0.5f)
						{
							// Other side #2
							screenToWorldOffset -= 1f;
						}
					}
				}
				
				if (track is DragTrack_Straight)
				{
					UpdateScrewVector ();
				}
				else if (track is DragTrack_Hinge)
				{
					_dragVector = grabPosition;
				}
			}

			SetGravity (false);

			if (dragMode == DragMode.RotateOnly && moveWithRigidbody)
			{
				_rigidbody.velocity = Vector3.zero;
			}
		}


		/**
		 * If the object rotates like a screw along a DragTrack_Straight, this updates the correct drag vector.
		 */
		public void UpdateScrewVector ()
		{
			float forwardDot = Vector3.Dot (grabPoint.position - transform.position, transform.forward);
			float rightDot = Vector3.Dot (grabPoint.position - transform.position, transform.right);
			
			_dragVector = (transform.forward * -rightDot) + (transform.right * forwardDot);
		}


		/**
		 * <summary>Stops the object from moving without the player's direct input (i.e. through Actions).</summary>
		 * <param name = "snapToTarget">If True, then the object will snap instantly to the intended target position</param>
		 */
		public void StopAutoMove (bool snapToTarget = true)
		{
			if (IsAutoMoving ())
			{
				activeAutoMove.Stop (track, this, snapToTarget);
				activeAutoMove = null;
				_rigidbody.velocity = Vector3.zero;
				_rigidbody.angularVelocity = Vector3.zero;
			}
		}


		/**
		 * <summary>Checks if the object is moving without the player's direct input.</summary>
		 * <returns>True if the object is moving without the player's direct input (gravity doesn't count).</returns>
		 */
		public bool IsAutoMoving (bool beAccurate = true)
		{
			if (activeAutoMove != null)
			{
				if (!beAccurate)
				{
					if (activeAutoMove.CheckForEnd (this, false))
					{
						// Special case: waiting for action to complete, so don't worry about being too accurate
						return false;
					}
				}
				return true;
			}
			return false;
		}


		/**
		 * <summary>Forces the object to move along a DragTrack without the player's direct input.</summary>
		 * <param name = "_targetTrackValue">The intended proportion along the track to send the object to</param>
		 * <param name = "_targetTrackSpeed">The intended speed to move the object by</param>
		 * <param name = "removePlayerControl">If True and the player is currently moving the object, then player control will be removed</param>
		 */
		public void AutoMoveAlongTrack (float _targetTrackValue, float _targetTrackSpeed, bool removePlayerControl)
		{
			AutoMoveAlongTrack (_targetTrackValue, _targetTrackSpeed, removePlayerControl, 1 << 0);
		}


		/**
		 * <summary>Forces the object to move along a DragTrack without the player's direct input.</summary>
		 * <param name = "_targetTrackValue">The intended proportion along the track to send the object to</param>
		 * <param name = "_targetTrackSpeed">The intended speed to move the object by</param>
		 * <param name = "removePlayerControl">If True and the player is currently moving the object, then player control will be removed</param>
		 * <param name = "layerMask">A LayerMask that determines what collisions will cause the automatic movement to cease</param>
		 * <param name = "snapID">The ID number of the associated snap, if snapping</param>
		 */
		public void AutoMoveAlongTrack (float _targetTrackValue, float _targetTrackSpeed, bool removePlayerControl, LayerMask layerMask, int snapID = -1)
		{
			if (dragMode == DragMode.LockToTrack && track != null)
			{
				if (snapID < 0)
				{
					canCallSnapEvents = true;
				}

				if (_targetTrackSpeed <= 0f)
				{
					activeAutoMove = null;
					track.SetPositionAlong (_targetTrackValue, this);
					return;
				}

				if (removePlayerControl)
				{
					isHeld = false;
				}

				activeAutoMove = new AutoMoveTrackData (_targetTrackValue, _targetTrackSpeed / 6000f, layerMask, snapID);
			}
			else
			{
				ACDebug.LogWarning ("Cannot move " + this.name + " along a track, because no track has been assigned to it", this);
			}
		}

		#endregion


		#region ProtectedFunctions

		protected IEnumerator InitToTrack ()
		{
			if (track != null)
			{
				ChildTransformData[] childTransformData = GetChildTransforms ();

				track.Connect (this);

				if (retainOriginalTransform)
				{
					track.SnapToTrack (this, true);
					SetChildTransforms (childTransformData);
					yield return new WaitForEndOfFrame ();
				}

				if (setOnStart)
				{
					track.SetPositionAlong (trackValueOnStart, this);
				}
				else
				{
					track.SnapToTrack (this, true);
				}
				trackValue = track.GetDecimalAlong (this);
			}
		}


		protected ChildTransformData[] GetChildTransforms ()
		{
			List<ChildTransformData> childTransformData = new List<ChildTransformData>();
			for (int i=0; i<transform.childCount; i++)
			{
				Transform childTransform = transform.GetChild (i);
				childTransformData.Add (new ChildTransformData (childTransform.position, childTransform.rotation));
			}
			return childTransformData.ToArray ();
		}


		protected void SetChildTransforms (ChildTransformData[] childTransformData)
		{
			for (int i=0; i<transform.childCount; i++)
			{
				Transform childTransform = transform.GetChild (i);
				childTransformData[i].UpdateTransform (childTransform);
			}
		}


		protected void RunInteraction (bool onMove)
		{
			int parameterID = (onMove) ? moveParameterID : dropParameterID;

			switch (actionListSource)
			{
				case ActionListSource.InScene:
					Interaction interaction = (onMove) ? interactionOnMove : interactionOnDrop;
					if (interaction != null && gameObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer))
					{
						if (!onMove || !KickStarter.actionListManager.IsListRunning (interaction))
						{
							if (parameterID >= 0)
							{
								ActionParameter parameter = interaction.GetParameter (parameterID);
								if (parameter != null && parameter.parameterType == ParameterType.GameObject)
								{
									parameter.gameObject = gameObject;
								}
							}

							interaction.Interact ();
						}
					}
					break;

				case ActionListSource.AssetFile:
					ActionListAsset actionListAsset = (onMove) ? actionListAssetOnMove : actionListAssetOnDrop;
					if (actionListAsset != null && gameObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer))
					{
						if (!onMove || !KickStarter.actionListAssetManager.IsListRunning (actionListAsset))
						{
							if (parameterID >= 0)
							{
								ActionParameter parameter = actionListAsset.GetParameter (parameterID);
								if (parameter != null && parameter.parameterType == ParameterType.GameObject)
								{
									parameter.gameObject = gameObject;
									if (GetComponent <ConstantID>())
									{
										parameter.intValue = GetComponent <ConstantID>().constantID;
									}
									else
									{
										ACDebug.LogWarning ("Cannot set the value of parameter " + parameterID + " ('" + parameter.label + "') as " + gameObject.name + " has no Constant ID component.", gameObject);
									}
								}
							}

							actionListAsset.Interact ();
						}
					}
					break;
			}
		}


		protected void OnCollisionEnter (Collision collision)
		{
			if (IsAutoMoving ())
			{
				activeAutoMove.ProcessCollision (collision, this);
			}

			BaseOnCollisionEnter (collision);
		}


		protected void SetGravity (bool value)
		{
			if (dragMode != DragMode.LockToTrack)
			{
				if (noGravityWhenHeld && _rigidbody != null)
				{
					_rigidbody.useGravity = value;
				}
			}
		}

		#endregion


		#region GetSet

		public float ColliderWidth
		{
			get
			{
				return colliderRadius;
			}
		}

		#endregion


		#region ProtectedClasses

		protected class AutoMoveTrackData
		{

			protected float targetValue;
			protected float speed;
			protected LayerMask blockLayerMask;
			protected int snapID = -1;


			public AutoMoveTrackData (float _targetValue, float _speed, LayerMask _blockLayerMask, int _snapID = -1)
			{
				targetValue = _targetValue;
				speed = _speed;
				blockLayerMask = _blockLayerMask;
				snapID = _snapID;
			}

			
			public void Update (DragTrack track, Moveable_Drag draggable)
			{
				track.ApplyAutoForce (targetValue, speed, draggable);
			}


			public bool CheckForEnd (Moveable_Drag draggable, bool beAccurate = true)
			{
				float currentValue = draggable.trackValue;

				if (draggable.track.Loops)
				{
					if (targetValue - currentValue > 0.5f)
					{
						currentValue += 1f;
					}
					else if (currentValue - targetValue > 0.5f)
					{
						currentValue -= 1f;
					}
				}

				float diff = Mathf.Abs (targetValue - currentValue);

				if (diff < 0.01f)
				{
					if (draggable.canCallSnapEvents && snapID >= 0)
					{
						KickStarter.eventManager.Call_OnDraggableSnap (draggable, draggable.track, draggable.track.GetSnapData (snapID));
						draggable.canCallSnapEvents = false;
					}

					if (!beAccurate)
					{
						return true;
					}
				}
				if (diff < 0.001f)
				{
					return true;
				}
				return false;
			}


			public void Stop (DragTrack track, Moveable_Drag draggable, bool snapToTarget)
			{
				if (snapToTarget)
				{
					track.SetPositionAlong (targetValue, draggable);
				}
			}


			public void ProcessCollision (Collision collision, Moveable_Drag draggable)
			{
				if ((blockLayerMask.value & 1 << collision.gameObject.layer) != 0)
				{
					draggable.StopAutoMove (false);
				}
			}

		}


		protected class ChildTransformData
		{

			protected Vector3 originalPosition;
			protected Quaternion originalRotation;


			public ChildTransformData (Vector3 _originalPosition, Quaternion _originalRotation)
			{
				originalPosition = _originalPosition;
				originalRotation = _originalRotation;
			}


			public void UpdateTransform (Transform transform)
			{
				transform.position = originalPosition;
				transform.rotation = originalRotation;
			}
		}

		#endregion


		#if UNITY_EDITOR

		public bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (actionListSource == ActionListSource.AssetFile)
			{
				if (actionListAssetOnMove == actionListAsset) return true;
				if (actionListAssetOnDrop == actionListAsset) return true;
			}
			return false;
		}

		#endif

	}
	
}