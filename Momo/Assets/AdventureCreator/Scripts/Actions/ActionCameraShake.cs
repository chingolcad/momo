/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionCameraShake.cs"
 * 
 *	This action causes the MainCamera to shake,
 *	and also affects the BackgroundImage if one is active.
 * 
 */

using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionCameraShake : Action
	{
		
		public int shakeIntensity;
		public int shakeIntensityParameterID = -1;
		public float duration = 1f;
		public int durationParameterID = -1;

		public CameraShakeEffect cameraShakeEffect = CameraShakeEffect.TranslateAndRotate;
		
		
		public ActionCameraShake ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Camera;
			title = "Shake";
			description = "Causes the camera to shake, giving an earthquake screen effect. The method of shaking, i.e. moving or rotating, depends on the type of camera the Main Camera is linked to.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			shakeIntensity = AssignInteger (parameters, shakeIntensityParameterID, shakeIntensity);
			duration = AssignFloat (parameters, durationParameterID, duration);
			if (duration < 0f)
			{
				duration = 0f;
			}
		}
		
		
		public override float Run ()
		{
			MainCamera mainCam = KickStarter.mainCamera;
			if (mainCam)
			{
				if (!isRunning)
				{
					isRunning = true;
					
					DoShake (mainCam, (float) shakeIntensity, duration);
						
					if (willWait)
					{
						return (duration);
					}
				}
				else
				{
					isRunning = false;
					return 0f;
				}
			}
			
			return 0f;
		}


		public override void Skip ()
		{
			MainCamera mainCam = KickStarter.mainCamera;
			if (mainCam)
			{
				DoShake (mainCam, 0f, 0f);
			}
		}


		protected void DoShake (MainCamera mainCam, float _intensity, float _duration)
		{
			if (mainCam.attachedCamera is GameCamera)
			{
				mainCam.Shake (_intensity / 67f, _duration, cameraShakeEffect);
			}
			else if (mainCam.attachedCamera is GameCamera25D)
			{
				mainCam.Shake (_intensity / 67f, _duration, cameraShakeEffect);
				
				GameCamera25D gameCamera = (GameCamera25D) mainCam.attachedCamera;
				if (gameCamera.backgroundImage)
				{
					gameCamera.backgroundImage.Shake (_intensity / 0.67f, _duration);
				}
			}
			else if (mainCam.attachedCamera is GameCamera2D)
			{
				mainCam.Shake (_intensity / 33f, _duration, cameraShakeEffect);
			}
			else
			{
				mainCam.Shake (_intensity / 67f, _duration, cameraShakeEffect);
			}
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			shakeIntensityParameterID = Action.ChooseParameterGUI ("Intensity:", parameters, shakeIntensityParameterID, ParameterType.Integer);
			if (shakeIntensityParameterID < 0)
			{
				shakeIntensity = EditorGUILayout.IntField ("Intensity:", shakeIntensity);
			}

			durationParameterID = Action.ChooseParameterGUI ("Duration (s):", parameters, durationParameterID, ParameterType.Float);
			if (durationParameterID < 0)
			{
				duration = EditorGUILayout.FloatField ("Duration (s):", duration);
			}

			cameraShakeEffect = (CameraShakeEffect) EditorGUILayout.EnumPopup ("Shake effect:", cameraShakeEffect);

			willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			
			AfterRunningOption ();
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Camera: Shake' Action with key variables already set.</summary>
		 * <param name = "intensity">The intensity of the shaking effect</param>
		 * <param name = "duration">The time, in seconds, that the effect should last</param>
		 * <param name = "waitUntilFinish">If True, the Action will wait until the shaking effect has completed</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCameraShake CreateNew (int intensity = 1, float duration = 1f, bool waitUntilFinish = true)
		{
			ActionCameraShake newAction = (ActionCameraShake) CreateInstance <ActionCameraShake>();
			newAction.shakeIntensity = intensity;
			newAction.duration = duration;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}
		
	}

}