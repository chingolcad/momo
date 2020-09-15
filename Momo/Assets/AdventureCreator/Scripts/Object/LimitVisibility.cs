/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"LimitVisibility.cs"
 * 
 *	Attach this script to a GameObject to limit its visibility
 *	to a specific GameCamera in your scene.
 * 
 */

#if !UNITY_SWITCH
#define ALLOW_VIDEO
#endif

using UnityEngine;
using System.Collections.Generic;

#if ALLOW_VIDEO
using UnityEngine.Video;
#endif

namespace AC
{

	/**
	 * This component limits the visibility of a GameObject so that it can only be viewed through a specific _Camera.
	 */
	[AddComponentMenu("Adventure Creator/Camera/Limit visibility to camera")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_limit_visibility.html")]
	public class LimitVisibility : MonoBehaviour
	{

		#region Variables

		/** The _Camera to limit the GameObject's visibility to (deprecated) */
		[HideInInspector] public _Camera limitToCamera;
		/** The _Cameras to limit the GameObject's visibility to */
		public List<_Camera> limitToCameras = new List<_Camera>();
		/** If True, then child GameObjects will be affected in the same way */
		public bool affectChildren = false;
		/** If True, then the GameObject will only be visible when the Cameras defined in limitToCameras are not active */
		public bool negateEffect = false;
		/** If True, then the object will not be visible even if the correct _Camera is active */
		[HideInInspector] public bool isLockedOff = false;

		protected bool isVisible = false;
		protected _Camera activeCamera = null;
		protected _Camera transitionCamera = null;

		protected Renderer _renderer;
		protected SpriteRenderer spriteRenderer;
		protected Renderer[] childRenderers;
		protected SpriteRenderer[] childSprites;
		#if ALLOW_VIDEO
		protected VideoPlayer videoPlayer;
		#endif

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			_renderer = GetComponent <Renderer>();
			if (_renderer == null)
			{
				spriteRenderer = GetComponent <SpriteRenderer>();
			}

			if (affectChildren)
			{
				childRenderers = GetComponentsInChildren <Renderer>();
				childSprites = GetComponentsInChildren <SpriteRenderer>();
			}

			#if ALLOW_VIDEO
			videoPlayer = GetComponent <VideoPlayer>();
			#endif
		}
		

		protected void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}


		protected void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);

			Upgrade ();

			if (limitToCameras.Count == 0 || KickStarter.mainCamera == null)
			{
				return;
			}

			activeCamera = KickStarter.mainCamera.attachedCamera;

			if (activeCamera != null && !isLockedOff)
			{
				if (limitToCameras.Contains (activeCamera))
				{
					SetVisibility (true);
				}
				else
				{
					SetVisibility (false);
				}
			}
			else
			{
				SetVisibility (false);
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Upgrades the component to make use of the limitToCameras List, rather than the singular limitToCamera variable.</summary>
		 */
		public void Upgrade ()
		{
			if (limitToCameras == null)
			{
				limitToCameras = new List<_Camera>();
			}

			if (limitToCamera != null)
			{
				if (!limitToCameras.Contains (limitToCamera))
				{
					limitToCameras.Add (limitToCamera);
				}
				limitToCamera = null;

				#if UNITY_EDITOR
				if (Application.isPlaying)
				{
					ACDebug.Log ("LimitVisibility component on '" + gameObject.name + "' has been temporarily upgraded - please view its Inspector when the game ends and save the scene.", gameObject);
				}
				else
				{
					UnityVersionHandler.CustomSetDirty (this, true);
					ACDebug.Log ("Upgraded LimitVisibility on '" + gameObject.name + "', please save the scene.", gameObject);
				}
				#endif
			}
		}


		/**
		 * Updates the visibility based on the attached camera. This is public so that it can be called by StateHandler.
		 */
		public void _Update ()
		{
			if (limitToCameras.Count == 0 || KickStarter.mainCamera == null)
			{
				return;
			}

			activeCamera = KickStarter.mainCamera.attachedCamera;
			transitionCamera = KickStarter.mainCamera.GetTransitionFromCamera ();

			if (isLockedOff)
			{
				if (isVisible)
				{
					SetVisibility (false);
				}
				return;
			}

			if (activeCamera != null && limitToCameras.Contains (activeCamera))
			{
				SetVisibility (!negateEffect);
			}
			else if (transitionCamera != null && limitToCameras.Contains (transitionCamera))
			{
				SetVisibility (!negateEffect);
			}
			else
			{
				SetVisibility (negateEffect);
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void SetVisibility (bool state)
		{
			if (_renderer != null)
			{
				_renderer.enabled = state;
			}
			else if (spriteRenderer != null)
			{
				spriteRenderer.enabled = state;
			}

			if (affectChildren)
			{
				foreach (Renderer child in childRenderers)
				{
					child.enabled = state;
				}

				foreach (SpriteRenderer child in childSprites)
				{
					child.enabled = state;
				}
			}

			#if ALLOW_VIDEO
			if (videoPlayer != null)
			{
				videoPlayer.targetCameraAlpha = (state) ? 1f : 0f;
			}
			#endif

			isVisible = state;
		}

		#endregion

	}

}