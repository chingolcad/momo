/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Parallax2D.cs"
 * 
 *	Attach this script to a GameObject when making a 2D game,
 *	to make it scroll as the camera moves.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * When used in 2D games, this script can be attached to scene objects to make them scroll as the camera moves, creating a parallax effect.
	 */
	[AddComponentMenu("Adventure Creator/Misc/Parallax 2D")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_parallax2_d.html")]
	public class Parallax2D : MonoBehaviour
	{

		#region Variables

		/** The intensity of the depth effect. Positive values will make the GameObject appear further away (i.e. in the background), negative values will make it appear closer to the camera (i.e. in the foreground). */
		public float depth;
		/** If True, then the GameObject will scroll in the X-direction */
		public bool xScroll;
		/** If True, then the GameObject will scroll in the Y-direction */
		public bool yScroll;
		/** An offset for the GameObject's initial position along the X-axis */
		public float xOffset;
		/** An offset for the GameObject's initial position along the Y-axis */
		public float yOffset;
		
		/** If True, scrolling in the X-direction will be constrained */
		public bool limitX;
		/** The minimum scrolling position in the X-direction, if limitX = True */
		public float minX;
		/** The maximum scrolling position in the X-direction, if limitX = True */
		public  float maxX;

		/** If True, scrolling in the Y-direction will be constrained */
		public bool limitY;
		/** The minimum scrolling position in the Y-direction, if limitY = True */
		public float minY;
		/** The maximum scrolling position in the Y-direction, if limitY = True */
		public float maxY;

		/** What entity affects the parallax behaviour (Camera, Cursor) */
		public ParallaxReactsTo reactsTo = ParallaxReactsTo.Camera;

		protected float xStart;
		protected float yStart;
		protected float xDesired;
		protected float yDesired;
		protected Vector2 perspectiveOffset;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			Initialise ();
		}


		protected void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}

		#endregion


		#region PublicFunctions

		/**
		 * Updates the GameObject's position according to the camera.  This is called every frame by the StateHandler.
		 */
		public void UpdateOffset ()
		{
			switch (reactsTo)
			{
				case ParallaxReactsTo.Camera:
					if (KickStarter.mainCamera.attachedCamera is GameCamera2D && !KickStarter.mainCamera.attachedCamera.Camera.orthographic)
					{
						perspectiveOffset = KickStarter.mainCamera.GetPerspectiveOffset ();
					}
					else
					{
						perspectiveOffset = new Vector2 (KickStarter.CameraMain.transform.position.x, KickStarter.CameraMain.transform.position.y);
					}
					break;

				case ParallaxReactsTo.Cursor:
					Vector2 screenCentre = ACScreen.safeArea.size / 2f;
					Vector2 mousePosition = KickStarter.playerInput.GetMousePosition ();
					perspectiveOffset = new Vector2 (((1f - mousePosition.x) / screenCentre.x) + 1f, ((1f - mousePosition.y) / screenCentre.y + 1f));
					break;
			}

			if (limitX)
			{
				perspectiveOffset.x = Mathf.Clamp (perspectiveOffset.x, minX, maxX);
			}
			if (limitY)
			{
				perspectiveOffset.y = Mathf.Clamp (perspectiveOffset.y, minY, maxY);
			}

			xDesired = xStart;
			if (xScroll)
			{
				xDesired += perspectiveOffset.x * depth;
				xDesired += xOffset;
			}

			yDesired = yStart;
			if (yScroll)
			{
				yDesired += perspectiveOffset.y * depth;
				yDesired += yOffset;
			}

			if (xScroll && yScroll)
			{
				transform.localPosition = new Vector3 (xDesired, yDesired, transform.localPosition.z);
			}
			else if (xScroll)
			{
				transform.localPosition = new Vector3 (xDesired, transform.localPosition.y, transform.localPosition.z);
			}
			else if (yScroll)
			{
				transform.localPosition = new Vector3 (transform.localPosition.x, yDesired, transform.localPosition.z);
			}
		}

		#endregion


		#region ProtectedFunctions

		protected virtual void Initialise ()
		{
			xStart = transform.localPosition.x;
			yStart = transform.localPosition.y;

			xDesired = xStart;
			yDesired = yStart;
		}

		#endregion

	}

}