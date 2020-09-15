using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A subclass of _Camera that allows for the cursor position to tweak the camera's rotation */
	public abstract class CursorInfluenceCamera : _Camera
	{

		#region Variables

		/** If True, then the camera will rotate towards the cursor's position on-screen */
		public bool followCursor = false;
		/** The influence that the cursor's position has on rotation, if followCursor = True */
		public Vector2 cursorInfluence = new Vector2 (0.3f, 0.1f);
		/** If True, and followCursor = True, then camera rotation according to the cursor's X position will be limited */
		public bool constrainCursorInfluenceX = false;
		/** The lower and upper limits, if constrainCursorInfluenceX = True */
		public Vector2 limitCursorInfluenceX;
		/** If True, and followCursor = True, then camera rotation according to the cursor's Y position will be limited */
		public bool constrainCursorInfluenceY = false;
		/** The lower and upper limits, if constrainCursorInfluenceY = True */
		public Vector2 limitCursorInfluenceY;
		/** The speed at which the camera follows the cursor */
		public float followCursorSpeed = 3f;

		protected Vector2 actualCursorOffset;

		#endregion


		#region PublicFunctions

		public override Vector2 CreateRotationOffset ()
		{
			if (followCursor)
			{
				if (KickStarter.stateHandler.IsInGameplay ())
				{
					Vector2 mousePosition = KickStarter.playerInput.GetMousePosition ();
					Vector2 mouseOffset = new Vector2 (mousePosition.x / ( ACScreen.width / 2) - 1, mousePosition.y / ( ACScreen.height / 2) - 1);
					float distFromCentre = mouseOffset.sqrMagnitude;

					if (distFromCentre < 1.96f)
					{
						if (constrainCursorInfluenceX)
						{
							mouseOffset.x = Mathf.Clamp (mouseOffset.x, limitCursorInfluenceX[0], limitCursorInfluenceX[1]);
						}
						if (constrainCursorInfluenceY)
						{
							mouseOffset.y = Mathf.Clamp (mouseOffset.y, limitCursorInfluenceY[0], limitCursorInfluenceY[1]);
						}
					}

					Vector2 targetCursorOffset = new Vector2 (mouseOffset.x * cursorInfluence.x, mouseOffset.y * cursorInfluence.y);
					actualCursorOffset = Vector2.Lerp (actualCursorOffset, targetCursorOffset, Time.deltaTime * followCursorSpeed);
				}

				return actualCursorOffset;
			}
			return Vector2.zero;
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowCursorInfluenceGUI ()
		{
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Cursor influence", EditorStyles.boldLabel);
			followCursor = CustomGUILayout.Toggle ("Follow cursor?", followCursor, "", "If True, then the camera will rotate towards the cursor's position on-screen");
			if (followCursor)
			{
				cursorInfluence = CustomGUILayout.Vector2Field ("Panning factor:", cursorInfluence, "", "The influence that the cursor's position has on rotation");
				followCursorSpeed = CustomGUILayout.Slider ("Follow speed:", followCursorSpeed, 0f, 10f, "", "The speed at which the camera follows the cursor.");

				constrainCursorInfluenceX = CustomGUILayout.ToggleLeft ("Constrain panning in X direction?", constrainCursorInfluenceX, "", "If True, then camera rotation according to the cursor's X position will be limited");
				if (constrainCursorInfluenceX)
				{
					limitCursorInfluenceX[0] = CustomGUILayout.Slider ("Minimum X constraint:", limitCursorInfluenceX[0], -1.4f, 0f, "", "The cursor influence's lower limit in the X-direction");
					limitCursorInfluenceX[1] = CustomGUILayout.Slider ("Maximum X constraint:", limitCursorInfluenceX[1], 0f, 1.4f, "", "The cursor influence's upper limit in the X-direction");
				}
				constrainCursorInfluenceY = CustomGUILayout.ToggleLeft ("Constrain panning in Y direction?", constrainCursorInfluenceY, "", "If True, then camera rotation according to the cursor's Y position will be limited");
				if (constrainCursorInfluenceY)
				{
					limitCursorInfluenceY[0] = CustomGUILayout.Slider ("Minimum Y constraint:", limitCursorInfluenceY[0], -1.4f, 0f, "", "The cursor influence's lower limit in the Y-direction");
					limitCursorInfluenceY[1] = CustomGUILayout.Slider ("Maximum Y constraint:", limitCursorInfluenceY[1], 0f, 1.4f, "", "The cursor influence's upper limit in the Y-direction");
				}

				if (Application.isPlaying && KickStarter.mainCamera != null && KickStarter.mainCamera.attachedCamera == this)
				{
					EditorGUILayout.HelpBox ("Changes made to this panel will not be felt until the MainCamera switches to this camera again.", MessageType.Info);
				}
			}
			EditorGUILayout.EndVertical ();
		}

		#endif

	}

}