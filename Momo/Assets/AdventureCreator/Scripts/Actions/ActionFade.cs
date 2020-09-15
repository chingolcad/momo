/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionFade.cs"
 * 
 *	This action controls the MainCamera's fading.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionFade : Action
	{
		
		public FadeType fadeType;
		public bool isInstant;
		public float fadeSpeed = 0.5f;
		public int fadeSpeedParameterID = -1;
		public bool setTexture;
		public Texture2D tempTexture;
		public int tempTextureParameterID = -1;
		public bool forceCompleteTransition = true;
		
		
		public ActionFade ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Camera;
			title = "Fade";
			description = "Fades the camera in or out. The fade speed can be adjusted, as can the overlay texture – this is black by default.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			tempTexture = (Texture2D) AssignObject <Texture2D> (parameters, tempTextureParameterID, tempTexture);
			fadeSpeed = AssignFloat (parameters, fadeSpeedParameterID, fadeSpeed);
		}
		
		
		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;
				
				MainCamera mainCam = KickStarter.mainCamera;
				RunSelf (mainCam, fadeSpeed);
					
				if (willWait && !isInstant)
				{
					return (fadeSpeed);
				}

				return 0f;
			}

			else
			{
				isRunning = false;
				return 0f;
			}
		}


		public override void Skip ()
		{
			RunSelf (KickStarter.mainCamera, 0f);
		}


		protected void RunSelf (MainCamera mainCam, float _time)
		{
			if (mainCam == null)
			{
				return;
			}

			mainCam.StopCrossfade ();

			if (fadeType == FadeType.fadeIn)
			{
				if (isInstant)
				{
					mainCam.FadeIn (0f);
				}
				else
				{
					mainCam.FadeIn (_time, forceCompleteTransition);
				}
			}
			else
			{
				Texture2D texToUse = tempTexture;
				if (!setTexture)
				{
					texToUse = null;
				}

				float timeToFade = _time;
				if (isInstant)
				{
					timeToFade = 0f;
				}

				mainCam.FadeOut (timeToFade, texToUse, forceCompleteTransition);
			}
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			fadeType = (FadeType) EditorGUILayout.EnumPopup ("Type:", fadeType);

			if (fadeType == FadeType.fadeOut)
			{
				setTexture = EditorGUILayout.Toggle ("Custom fade texture?", setTexture);
				if (setTexture)
				{
					tempTextureParameterID = Action.ChooseParameterGUI ("Fade texture:", parameters, tempTextureParameterID, ParameterType.UnityObject);
					if (tempTextureParameterID < 0)
					{
						tempTexture = (Texture2D) EditorGUILayout.ObjectField ("Fade texture:", tempTexture, typeof (Texture2D), false);
					}
				}
			}

			isInstant = EditorGUILayout.Toggle ("Instant?", isInstant);
			if (!isInstant)
			{
				fadeSpeedParameterID = Action.ChooseParameterGUI ("Time to fade (s):", parameters, fadeSpeedParameterID, ParameterType.Float);
				if (fadeSpeedParameterID < 0)
				{
					fadeSpeed = EditorGUILayout.Slider ("Time to fade (s):", fadeSpeed, 0f, 10f);
				}
				forceCompleteTransition = EditorGUILayout.Toggle ("Force complete transition?", forceCompleteTransition);
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			}

			AfterRunningOption ();
		}
		
		
		public override string SetLabel ()
		{
			if (fadeType == FadeType.fadeIn)
			{
				return "In";
			}
			else
			{
				return "Out";
			}
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Camera: Fade' Action</summary>
		 * <param name = "fadeType">The type of fade to perform</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning from the old to the new animation</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionFade CreateNew (FadeType fadeType, float transitionTime = 1f, bool waitUntilFinish = true)
		{
			ActionFade newAction = (ActionFade) CreateInstance <ActionFade>();
			newAction.fadeType = fadeType;
			newAction.fadeSpeed = transitionTime;
			newAction.isInstant = (transitionTime < 0f);
			newAction.willWait = waitUntilFinish;
			return newAction;
		}

	}

}