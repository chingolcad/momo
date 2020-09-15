/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"GameCamera2D.cs"
 * 
 *	This GameCamera allows scrolling horizontally and vertically without altering perspective.
 *	Based on the work by Eric Haines (Eric5h5) at http://wiki.unity3d.com/index.php?title=OffsetVanishingPoint
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * The standard 2D camera. It can be scrolled horizontally and vertically without altering perspective (causing a "Ken Burns effect" if the camera uses Perspective projection.
	 * Based on the work by Eric Haines (Eric5h5) at http://wiki.unity3d.com/index.php?title=OffsetVanishingPoint
	 */
	[RequireComponent (typeof (Camera))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_game_camera2_d.html")]
	public class GameCamera2D : CursorInfluenceCamera
	{

		#region Variables

		/** If True, then horizontal panning is prevented */
		public bool lockHorizontal = true;
		/** If True, then vertical panning is prevented */
		public bool lockVertical = true;

		/** If True, then horizontal panning will be limited to minimum and maximum values */
		public bool limitHorizontal;
		/** If True, then vertical panning will be limited to minimum and maximum values */
		public bool limitVertical;

		/** The lower and upper horizontal limits, if limitHorizontal = True */
		public Vector2 constrainHorizontal;
		/** The lower and upper vertical limits, if limitVertical = True */
		public Vector2 constrainVertical;

		/** The amount of freedom when tracking a target. Higher values will result in looser tracking */
		public Vector2 freedom = Vector2.zero;
		/** The follow speed when tracking a target */
		public float dampSpeed = 0.9f;

		/** The influence that the target's facing direction has on the tracking position */
		public Vector2 directionInfluence = Vector2.zero;
		/** The intended horizontal and vertical panning offsets */
		public Vector2 afterOffset = Vector2.zero;

		/** If True, the camera will only move in steps, as if snapping to a grid */
		public bool doSnapping = false;
		/** The step size when doSnapping is True */
		public float unitSnap = 0.1f;

		protected Vector2 perspectiveOffset = Vector2.zero;
		protected Vector3 originalPosition = Vector3.zero;
		protected Vector2 desiredOffset = Vector2.zero;
		protected bool haveSetOriginalPosition = false;

		protected LerpUtils.FloatLerp xLerp = new LerpUtils.FloatLerp ();
		protected LerpUtils.FloatLerp yLerp = new LerpUtils.FloatLerp ();

		#endregion


		#region UnityStandards

		protected override void Awake ()
		{
			SetOriginalPosition ();
			base.Awake ();
		}
		
		
		protected override void Start ()
		{
			base.Start ();

			ResetTarget ();
			if (target)
			{
				MoveCameraInstant ();
			}
		}


		public override void _Update ()
		{
			MoveCamera ();
		}

		#endregion


		#region PublicFunctions

		public override bool Is2D ()
		{
			return true;
		}


		public override void MoveCameraInstant ()
		{
			if (targetIsPlayer && KickStarter.player)
			{
				target = KickStarter.player.transform;
			}

			SetOriginalPosition ();

			if (target && (!lockHorizontal || !lockVertical))
			{
				SetDesired ();
			
				if (!lockHorizontal)
				{
					perspectiveOffset.x = xLerp.Update (desiredOffset.x, desiredOffset.x, dampSpeed);
				}
				
				if (!lockVertical)
				{
					perspectiveOffset.y = yLerp.Update (desiredOffset.y, desiredOffset.y, dampSpeed);
				}
			}

			SetProjection ();
		}


		/**
		 * Snaps the camera to its offset values and recalculates the camera's projection matrix.
		 */
		public void SnapToOffset ()
		{
			perspectiveOffset = afterOffset;
			SetProjection ();
		}


		/**
		 * Sets the camera's rotation and projection according to the chosen settings in SettingsManager.
		 */
		public void SetCorrectRotation ()
		{
			if (KickStarter.settingsManager)
			{
				if (SceneSettings.IsTopDown ())
				{
					transform.rotation = Quaternion.Euler (90f, 0, 0);
					return;
				}

				if (SceneSettings.IsUnity2D ())
				{
					Camera.orthographic = true;
				}
			}

			transform.rotation = Quaternion.Euler (0, 0, 0);
		}


		/**
		 * <summary>Checks if the GameObject's rotation matches the intended rotation, according to the chosen settings in SettingsManager.</summary>
		 * <returns>True if the GameObject's rotation matches the intended rotation<returns>
		 */
		public bool IsCorrectRotation ()
		{
			if (SceneSettings.IsTopDown ())
			{
				if (transform.rotation == Quaternion.Euler (90f, 0f, 0f))
				{
					return true;
				}

				return false;
			}

			if (SceneSettings.CameraPerspective != CameraPerspective.TwoD)
			{
				return true;
			}

			if (transform.rotation == Quaternion.Euler (0f, 0f, 0f))
			{
				return true;
			}

			return false;
		}


		public override Vector2 GetPerspectiveOffset ()
		{
			return GetSnapOffset ();
		}


		/**
		 * <summary>Sets the actual horizontal and vertical panning offsets. Be aware that the camera will still be subject to the movement set by the target, so it will move back to its original position afterwards unless you also change the target.</summary>
		 * <param name = "_perspectiveOffset">The new offsets</param>
		 */
		public void SetPerspectiveOffset (Vector2 _perspectiveOffset)
		{
			perspectiveOffset = _perspectiveOffset;
		}

		#endregion


		#region ProtectedFunctions

		protected void SetDesired ()
		{
			Vector2 targetOffset = GetOffsetForPosition (target.position);
			if (targetOffset.x < (perspectiveOffset.x - freedom.x))
			{
				desiredOffset.x = targetOffset.x + freedom.x;
			}
			else if (targetOffset.x > (perspectiveOffset.x + freedom.x))
			{
				desiredOffset.x = targetOffset.x - freedom.x;
			}

			desiredOffset.x += afterOffset.x;
			if (!Mathf.Approximately (directionInfluence.x, 0f))
			{
				desiredOffset.x += Vector3.Dot (TargetForward, transform.right) * directionInfluence.x;
			}

			if (limitHorizontal)
			{
				desiredOffset.x = ConstrainAxis (desiredOffset.x, constrainHorizontal);
			}
			
			if (targetOffset.y < (perspectiveOffset.y - freedom.y))
			{
				desiredOffset.y = targetOffset.y + freedom.y;
			}
			else if (targetOffset.y > (perspectiveOffset.y + freedom.y))
			{
				desiredOffset.y = targetOffset.y - freedom.y;
			}
			
			desiredOffset.y += afterOffset.y;
			if (!Mathf.Approximately (directionInfluence.y, 0f))
			{
				if (SceneSettings.IsTopDown ())
				{
					desiredOffset.y += Vector3.Dot (TargetForward, transform.up) * directionInfluence.y;
				}
				else
				{
					desiredOffset.y += Vector3.Dot (TargetForward, transform.forward) * directionInfluence.y;
				}
			}

			if (limitVertical)
			{
				desiredOffset.y = ConstrainAxis (desiredOffset.y, constrainVertical);
			}
		}	
		

		protected void MoveCamera ()
		{
			if (targetIsPlayer && KickStarter.player)
			{
				target = KickStarter.player.transform;
			}
			
			if (target && (!lockHorizontal || !lockVertical))
			{
				SetDesired ();

				if (!lockHorizontal)
				{
					perspectiveOffset.x = (dampSpeed > 0f)
											? xLerp.Update (perspectiveOffset.x, desiredOffset.x, dampSpeed)
											: desiredOffset.x;
				}
				
				if (!lockVertical)
				{
					perspectiveOffset.y = (dampSpeed > 0f)
											? yLerp.Update (perspectiveOffset.y, desiredOffset.y, dampSpeed)
											: desiredOffset.y;
				}

			}
			else if (!Camera.orthographic)
			{
				SnapToOffset ();
			}
			
			SetProjection ();
		}


		protected void SetOriginalPosition ()
		{
			if (!haveSetOriginalPosition)
			{
				originalPosition = transform.position;
				haveSetOriginalPosition = true;
			}
		}
		

		protected void SetProjection ()
		{
			Vector2 snapOffset = GetSnapOffset ();

			if (Camera.orthographic)
			{
				transform.position = originalPosition + (transform.right * snapOffset.x) + (transform.up * snapOffset.y);
			}
			else
			{
				Camera.projectionMatrix = AdvGame.SetVanishingPoint (Camera, snapOffset);
			}
		}


		protected Vector2 GetOffsetForPosition (Vector3 targetPosition)
		{
			Vector2 targetOffset = new Vector2 ();
			float forwardOffsetScale = 93 - (299 * Camera.nearClipPlane);

			if (SceneSettings.IsTopDown ())
			{
				if (Camera.orthographic)
				{
					targetOffset.x = transform.position.x;
					targetOffset.y = transform.position.z;
				}
				else
				{
					targetOffset.x = - (targetPosition.x - transform.position.x) / (forwardOffsetScale * (targetPosition.y - transform.position.y));
					targetOffset.y = - (targetPosition.z - transform.position.z) / (forwardOffsetScale * (targetPosition.y - transform.position.y));
				}
			}
			else
			{
				if (Camera.orthographic)
				{
					targetOffset = transform.TransformVector (new Vector3 (targetPosition.x, targetPosition.y, -targetPosition.z));
				}
				else
				{
					float rightDot = Vector3.Dot (transform.right, targetPosition - transform.position);
					float forwardDot = Vector3.Dot (transform.forward, targetPosition - transform.position);
					float upDot = Vector3.Dot (transform.up, targetPosition - transform.position);

					targetOffset.x = rightDot / (forwardOffsetScale * forwardDot);
					targetOffset.y = upDot / (forwardOffsetScale * forwardDot);
				}
			}

			return targetOffset;
		}


		protected Vector2 GetSnapOffset ()
		{
			if (doSnapping)
			{
				Vector2 snapOffset = perspectiveOffset;
				snapOffset /= unitSnap;
				snapOffset.x = Mathf.Round (snapOffset.x);
				snapOffset.y = Mathf.Round (snapOffset.y);
				snapOffset *= unitSnap;
				return snapOffset;
			}
			return perspectiveOffset;
		}

		#endregion


		#if UNITY_EDITOR

		[ContextMenu ("Make active")]
		private void LocalMakeActive ()
		{
			MakeActive ();
		}

		#endif


		#region GetSet

		public override TransparencySortMode TransparencySortMode
		{
			get
			{
				return TransparencySortMode.Orthographic;
			}
		}

		#endregion

	}

}