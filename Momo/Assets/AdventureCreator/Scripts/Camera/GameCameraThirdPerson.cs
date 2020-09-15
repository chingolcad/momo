/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"GameCameraThirdPerson.cs"
 * 
 *	This is attached to a scene-based camera, similar to GameCamera and GameCamera2D.
 *	It should not be a child of the Player, but instead scene-specific.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A third-person game similar to those found in action-adventure games.
	 * The camera will rotate around its target and follow just behind it.
	 * It should not be a child of the Player / target - it will attach itself to its target through script.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_game_camera_third_person.html")]
	public class GameCameraThirdPerson : _Camera
	{

		#region Variables

		/** How spin rotation is affected (Free, Limited, Locked) */
		public RotationLock spinLock = RotationLock.Free;
		/** How pitch rotation is affected (Free, Limited, Locked) */
		public RotationLock pitchLock = RotationLock.Locked;

		/** If True, then spin and pitch can be altered when a Conversation is active */
		public bool canRotateDuringConversations = false;
		/** If True, then spin and pitch can be altered when gameplay is blocked */
		public bool canRotateDuringCutscenes = false;

		/** The horizontal position offset */
		public float horizontalOffset = 0f;
		/** The vertical position offset */
		public float verticalOffset = 2f;

		/** The normal distance to keep from its target */
		public float distance = 2f;
		/** If True, the mousewheel can be used to zoom the camera's distance from the target */
		public bool allowMouseWheelZooming = false;
		/** If True, then the camera will detect Colliders to try to avoid clipping through walls */
		public bool detectCollisions = true;
		/** The size of the SphereCast to use when detecting collisions */
		public float collisionRadius = 0.3f;
		/** The LayerMask used to detect collisions, if detectCollisions = True */
		public LayerMask collisionLayerMask;
		/** The minimum distance to keep from its target */
		public float minDistance = 1f;
		/** The maximum distance to keep from its target */
		public float maxDistance = 3f;

		/** If True, then focalDistance will match the distance to target */
		public bool focalPointIsTarget = false;

		/** If True, then the cursor must be locked for spin rotation to occur (see: IsCursorLocked in PlayerInput) */
		public bool toggleCursor = false;

		/** The speed of spin rotations */
		public float spinSpeed = 5f;
		/** The acceleration of spin rotations */
		public float spinAccleration = 5f;
		/** The deceleration of spin rotations */
		public float spinDeceleration = 5f;
		/** The name of the Input axis that controls spin rotation, if isDragControlled = False */
		public string spinAxis = "";
		/** If True, then the direction of spin rotations will be reversed */
		public bool invertSpin = false;
		/** If True, then the camera's spin rotation will be relative to the target's rotation */
		public bool alwaysBehind = false;
		/** If True, then the spin rotation will be reset when the camera is made active */
		public bool resetSpinWhenSwitch = false;
		/** The offset in spin (yaw) angle if alwaysBehind = true */
		public float spinOffset = 0f;
		/** The maximum spin angle, if spinLock = RotationLock.Limited */
		public float maxSpin = 40f;

		/** The speed of pitch rotations */
		public float pitchSpeed = 3f;
		/** The acceleration of pitch rotations */
		public float pitchAccleration = 20f;
		/** The deceleration of pitch rotations */
		public float pitchDeceleration = 20f;
		/** The maximum pitch angle, if pitchLock = RotationLock.Limited. This also becomes the locked pitch angle, if pitchLock = RotationLock.Locked */
		public float maxPitch = 40f;
		/** The minimum pitch angle, if pitchLock = RotationLock.Limited */
		public float minPitch = -40f;
		/** The name of the Input axis that controls pitch rotation, if isDragControlled = False */
		public string pitchAxis = "";
		/** If True, then the direction of pitch rotations will be reversed */
		public bool invertPitch = false;
		/** If True, then the pitch rotation will be reset when the camera is made active */
		public bool resetPitchWhenSwitch = false;

		/** If True, then the magnitude of the input vector affects the magnitude of the rotation speed */
		public bool inputAffectsSpeed = false;

		protected Vector3 actualCollisionOffset;

		protected float deltaDistance = 0f;
		protected float deltaSpin = 0f;
		protected float deltaPitch = 0f;

		protected float roll = 0f;
		protected float spin = 0f;
		protected float pitch = 0f;

		protected float initialPitch = 0f;
		protected float initialSpin = 0f;

		protected Vector3 centrePosition;
		protected Vector3 targetPosition;
		protected Quaternion targetRotation;

		protected bool autoControlPitch = false;
		protected bool autoControlSpin = false;
		protected float autoControlTime;
		protected float autoControlStartTime;
		protected float autoPitchAngle;
		protected float autoSpinAngle;
		protected MoveMethod autoMoveMethod;
		protected AnimationCurve autoMoveCurve;

		#endregion


		#region UnityStandards

		protected override void Awake ()
		{
			base.Awake ();

			targetRotation = transform.rotation;
			initialPitch = transform.eulerAngles.x;
			initialSpin = transform.eulerAngles.y;
			autoControlPitch = autoControlSpin = false;
		}


		protected override void Start ()
		{
			base.Start ();

			ResetTarget ();

			Vector3 angles = transform.eulerAngles;
			spin = angles.y;
			roll = angles.z; 

			UpdateTargets (true);
			SnapMovement ();
		}


		public override void _Update ()
		{
			UpdateTargets ();
			DetectCollisions ();

			UpdateSelf ();
		}

		#endregion


		#region PublicFunctions

		/**
		 * Resests the spin and pitch rotatsions to their original values.
		 */
		public void ResetRotation ()
		{
			if (pitchLock != RotationLock.Locked && resetPitchWhenSwitch)
			{
				pitch = initialPitch;
			}
			if (spinLock != RotationLock.Locked && resetSpinWhenSwitch)
			{
				spin = initialSpin;
			}
			autoControlPitch = autoControlSpin = false;
		}


		/**
		 * <summary>Rotates the camera automatically to a fixed pitch and/or spin angle. Regular rotation will be disabled while this occurs.</summary>
		 * <param name = "_controlPitch">If True, the pitch angle will be affected</param>
		 * <param name = "_newPitchAngle">The new pitch angle, if _controlPitch = True</param>
		 * <param name = "_controlSpin">If True, the spin angle will be affected</param>
		 * <param name = "_newSpinAngle">The new spin angle, if _controlSpin = True</param>
		 * <param name = "_transitionTime">The duration, in seconds, that the rotation will take</param>
		 * <param name = "moveMethod">The movement method, if _transitionTime > 0 (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "timeCurve">The AnimationCurve that controls the change in rotation over time, if moveMethod = MoveMethod.CustomCurve</param>
		 */
		public void ForceRotation (bool _controlPitch, float _newPitchAngle, bool _controlSpin, float _newSpinAngle, float _transitionTime = 0f, MoveMethod moveMethod = MoveMethod.Linear, AnimationCurve timeCurve = null)
		{
			autoControlPitch = false;
			autoControlPitch = false;

			if (_controlPitch || _controlSpin)
			{
				if (_transitionTime > 0f)
				{
					autoControlPitch = _controlPitch;
					autoControlSpin = _controlSpin;

					autoPitchAngle = _newPitchAngle;
					autoSpinAngle = _newSpinAngle;

					autoMoveMethod = moveMethod;
					autoControlTime = _transitionTime;
					autoControlStartTime = Time.time;
					autoMoveCurve = timeCurve;
				}
				else
				{
					if (_controlPitch)
					{
						pitch = _newPitchAngle;
					}
					if (_controlSpin)
					{
						spin = _newSpinAngle;
					}
				}
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void DetectCollisions ()
		{
			if (detectCollisions && target != null)
			{
				Vector3 direction = target.position + new Vector3 (0, verticalOffset, 0f) - targetPosition;

				RaycastHit hit;
				if (Physics.SphereCast (direction + targetPosition, collisionRadius, -direction.normalized, out hit, direction.magnitude, collisionLayerMask))
				{
					float counterDist = (direction.magnitude - hit.distance);
					float maxDist = direction.magnitude - minDistance;
					counterDist = Mathf.Min (counterDist, maxDist);
					actualCollisionOffset = direction.normalized * counterDist;
				}
				else
				{
					actualCollisionOffset = Vector3.zero;
				}
			}
		}


		protected void UpdateSelf ()
		{
			transform.rotation = targetRotation;
			transform.position = targetPosition + actualCollisionOffset;
		}


		protected void UpdateTargets (bool onStart = false)
		{
			if (!target)
			{
				return;
			}

			if (autoControlPitch || autoControlSpin)
			{
				if (Time.time > autoControlStartTime + autoControlTime)
				{
					autoControlPitch = autoControlSpin = false;
				}
				else
				{
					if (autoControlPitch)
					{
						pitch = Mathf.Lerp (pitch, autoPitchAngle, AdvGame.Interpolate (autoControlStartTime, autoControlTime, autoMoveMethod, autoMoveCurve));
					}
					if (autoControlSpin)
					{
						spin = Mathf.Lerp (spin, autoSpinAngle, AdvGame.Interpolate (autoControlStartTime, autoControlTime, autoMoveMethod, autoMoveCurve));
					}
				}
			}
			else if (onStart || CanAcceptInput ())
			{
				if (allowMouseWheelZooming && minDistance < maxDistance)
				{
					if (Input.GetAxis ("Mouse ScrollWheel") < 0f)
					{
						deltaDistance = Mathf.Lerp (deltaDistance, Mathf.Min (spinSpeed, maxDistance - distance), spinAccleration/5f * Time.deltaTime);
					}
					else if (Input.GetAxis ("Mouse ScrollWheel") > 0f)
					{
						deltaDistance = Mathf.Lerp (deltaDistance, -Mathf.Min (spinSpeed, distance - minDistance), spinAccleration/5f * Time.deltaTime);
					}
					else
					{
						deltaDistance = Mathf.Lerp (deltaDistance, 0f, spinAccleration * Time.deltaTime);
					}
					
					distance += deltaDistance;
					distance = Mathf.Clamp (distance, minDistance, maxDistance);
				}
				
				if (KickStarter.playerInput.IsCursorLocked () || !toggleCursor)
				{
					if (!isDragControlled)
					{
						inputMovement = new Vector2 (KickStarter.playerInput.InputGetAxis (spinAxis), KickStarter.playerInput.InputGetAxis (pitchAxis));
					}
					else
					{
						if (KickStarter.playerInput.GetDragState () == DragState._Camera)
						{
							if (KickStarter.playerInput.IsCursorLocked ())
							{
								inputMovement = KickStarter.playerInput.GetFreeAim ();
							}
							else
							{
								inputMovement = KickStarter.playerInput.GetDragVector ();
							}
						}
						else
						{
							inputMovement = Vector2.zero;
						}
					}
				
					if (spinLock != RotationLock.Locked)
					{
						if (Mathf.Approximately (inputMovement.x, 0f))
						{
							deltaSpin = Mathf.Lerp (deltaSpin, 0f, spinDeceleration * Time.deltaTime);
						}
						else
						{
							float scaleFactor = 1f;
							if (inputAffectsSpeed)
							{
								scaleFactor *= Mathf.Abs (inputMovement.x);
								if (isDragControlled)
								{
									scaleFactor /= 1000f;
								}
							}

							if (inputMovement.x > 0f)
							{
								deltaSpin = Mathf.Lerp (deltaSpin, spinSpeed * scaleFactor, spinAccleration * Time.deltaTime * inputMovement.x);
							}
							else if (inputMovement.x < 0f)
							{
								deltaSpin = Mathf.Lerp (deltaSpin, -spinSpeed * scaleFactor, spinAccleration * Time.deltaTime * -inputMovement.x);
							}
						}

						if (spinLock == RotationLock.Limited)
						{
							if ((invertSpin && deltaSpin > 0f) || (!invertSpin && deltaSpin < 0f))
							{
								if (maxSpin - spin < 5f)
								{
									deltaSpin *= (maxSpin - spin) / 5f;
								}
							}
							else if ((invertSpin && deltaSpin < 0f) || (!invertSpin && deltaSpin > 0f))
							{
								if (maxSpin + spin < 5f)
								{
									deltaSpin *= (maxSpin + spin) / 5f;
								}
							}
						}
						
						if (invertSpin)
						{
							spin += deltaSpin;
						}
						else
						{
							spin -= deltaSpin;
						}
						
						if (spinLock == RotationLock.Limited)
						{
							spin = Mathf.Clamp (spin, -maxSpin, maxSpin);
						}
					}
					else
					{
						if (alwaysBehind)
						{
							spin = Mathf.LerpAngle (spin, target.eulerAngles.y + spinOffset, spinAccleration * Time.deltaTime);
						}
					}
				
					if (pitchLock != RotationLock.Locked)
					{
						if (Mathf.Approximately (inputMovement.y, 0f))
						{
							deltaPitch = Mathf.Lerp (deltaPitch, 0f, pitchDeceleration * Time.deltaTime);
						}
						else
						{
							float scaleFactor = 1f;
							if (inputAffectsSpeed)
							{
								scaleFactor *= Mathf.Abs (inputMovement.y);
								if (isDragControlled)
								{
									scaleFactor /= 1000f;
								}
							}
							
							if (inputMovement.y > 0f)
							{
								deltaPitch = Mathf.Lerp (deltaPitch, pitchSpeed * scaleFactor, pitchAccleration * Time.deltaTime * inputMovement.y);
							}
							else if (inputMovement.y < 0f)
							{
								deltaPitch = Mathf.Lerp (deltaPitch, -pitchSpeed * scaleFactor, pitchAccleration * Time.deltaTime * -inputMovement.y);
							}
						}

						if (pitchLock == RotationLock.Limited)
						{
							if ((invertPitch && deltaPitch > 0f) || (!invertPitch && deltaPitch < 0f))
							{
								if (maxPitch - pitch < 5f)
								{
									deltaPitch *= (maxPitch - pitch) / 5f;
								}
							}
							else if ((invertPitch && deltaPitch < 0f) || (!invertPitch && deltaPitch > 0f))
							{
								if (minPitch - pitch > -5f)
								{
									deltaPitch *= (minPitch - pitch) / -5f;
								}
							}
						}
						
						if (invertPitch)
						{
							pitch += deltaPitch;
						}
						else
						{
							pitch -= deltaPitch;
						}
						
						if (pitchLock == RotationLock.Limited)
						{
							pitch = Mathf.Clamp (pitch, minPitch, maxPitch);
						}
					}
				}
			}
			else
			{
				if (spinLock != RotationLock.Free)
				{
					if (alwaysBehind)
					{
						spin = Mathf.LerpAngle (spin, target.eulerAngles.y + spinOffset, spinAccleration * Time.deltaTime);
					}
				}
			}
			
			if (pitchLock == RotationLock.Locked)
			{
				pitch = maxPitch;
			}

			float finalSpin = spin;
			float finalPitch = pitch;

			if (alwaysBehind && spinLock == RotationLock.Limited)
			{
				finalSpin += target.eulerAngles.y;
			}
			else if (!targetIsPlayer)
			{
				if (spinLock != RotationLock.Locked)
				{
					finalSpin += target.eulerAngles.y;
				}
				if (pitchLock != RotationLock.Locked)
				{
					finalPitch += target.eulerAngles.x;
				}
			}
			
			Quaternion rotation = Quaternion.Euler (finalPitch, finalSpin, roll);
			targetRotation = rotation;
			centrePosition = target.position + (Vector3.up * verticalOffset) + (rotation * Vector3.right * horizontalOffset);
			targetPosition = centrePosition - (rotation * Vector3.forward * distance);

			SetFocalPoint ();
		}


		protected bool CanAcceptInput ()
		{
			if (KickStarter.mainCamera != null && KickStarter.mainCamera.attachedCamera != this)
			{
				return false;
			}

			if (KickStarter.stateHandler == null || 
				(KickStarter.stateHandler.gameState == AC.GameState.Normal) ||
				(KickStarter.stateHandler.gameState == AC.GameState.Cutscene && canRotateDuringCutscenes) ||
				(KickStarter.stateHandler.gameState == GameState.DialogOptions && canRotateDuringConversations))
			{
				return true;
			}

			return false;
		}


		protected void SnapMovement ()
		{
			transform.rotation = targetRotation;
			transform.position = targetPosition;

			SetFocalPoint ();
		}


		protected void SetFocalPoint ()
		{
			if (focalPointIsTarget && target != null)
			{
				focalDistance = Vector3.Dot (transform.forward, target.position - transform.position);
				if (focalDistance < 0f)
				{
					focalDistance = 0f;
				}
			}
		}

		#endregion

	}

}