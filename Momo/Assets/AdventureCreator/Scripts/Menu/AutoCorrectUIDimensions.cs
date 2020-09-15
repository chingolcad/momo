/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"AutoCorrectUIDimensions.cs"
 * 
 *	This script can re-position and re-scale a Unity UI-based Menu if the playable area is not the same as the game screen. This can be the case if an aspect ratio is enforced, or if running on a mobile device with notched features.
 * 
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AC
{

	/** This script can re-position and re-scale a Unity UI-based Menu if the playable area is not the same as the game screen. This can be the case if an aspect ratio is enforced, or if running on a mobile device with notched features. */
	[AddComponentMenu ("Adventure Creator/UI/Auto-correct UI Dimensions")]
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_auto_correct_ui_dimensions.html")]
	public class AutoCorrectUIDimensions : MonoBehaviour
	{

		#region Variables

		protected Canvas canvas;
		protected CanvasScaler canvasScaler;
		protected RectTransform transformToControl;

		public Vector2 minAnchorPoint = new Vector2 (0.5f, 0.5f);
		public Vector2 maxAnchorPoint = new Vector2 (0.5f, 0.5f);

		public bool updatePosition = true;
		public bool updateScale = true;

		protected Vector2 originalReferenceResolution;

		#endregion


		#region UnityStandards

		protected void Start ()
		{
			Initialise ();
		}


		protected void OnEnable ()
		{
			EventManager.OnUpdatePlayableScreenArea += OnUpdatePlayableScreenArea;
			StartCoroutine (UpdateInOneFrame ());
		}


		protected void OnDisable ()
		{
			EventManager.OnUpdatePlayableScreenArea -= OnUpdatePlayableScreenArea;
		}

		#endregion


		#region ProtectedFunctions

		protected void Initialise ()
		{
			canvas = GetComponent<Canvas> ();
			canvasScaler = canvas.GetComponent<CanvasScaler> ();
			if (canvasScaler != null)
			{
				originalReferenceResolution = canvasScaler.referenceResolution;
			}

			if (canvas == null || canvasScaler == null)
			{
				ACDebug.LogWarning ("The AutoCorrectUIDimensions component must be attached to a GameObject with both a Canvas and CanvasScaler component - be sure to attach it to the root Canvas object.", this);
			}

			if (KickStarter.playerMenus != null)
			{
				Menu menu = KickStarter.playerMenus.GetMenuWithCanvas (canvas);
				if (menu != null)
				{
					transformToControl = menu.rectTransform;
				}
			}
		}


		protected IEnumerator UpdateInOneFrame ()
		{
			yield return new WaitForEndOfFrame ();
			OnUpdatePlayableScreenArea ();
		}


		protected void OnUpdatePlayableScreenArea ()
		{
			// Position
			if (updatePosition && transformToControl != null)
			{
				transformToControl.anchorMin = ConvertToPlayableSpace (minAnchorPoint);
				transformToControl.anchorMax = ConvertToPlayableSpace (maxAnchorPoint);
			}

			// Scale
			if (updateScale && canvasScaler != null)
			{
				Vector2 safeSize = KickStarter.mainCamera.GetPlayableScreenArea (true).size;
				canvasScaler.referenceResolution = new Vector2 (originalReferenceResolution.x / safeSize.x, originalReferenceResolution.y / safeSize.y);
			}
		}


		protected Vector2 ConvertToPlayableSpace (Vector2 screenPosition)
		{
			Rect playableScreenArea = KickStarter.mainCamera.GetPlayableScreenArea (true);
			return new Vector2 (screenPosition.x * playableScreenArea.width, screenPosition.y * playableScreenArea.height) + playableScreenArea.position;
		}

		#endregion

	}

}