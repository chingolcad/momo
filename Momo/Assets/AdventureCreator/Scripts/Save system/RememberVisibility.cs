/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"RememberVisibility.cs"
 * 
 *	This script is attached to scene objects
 *	whose renderer.enabled state we wish to save.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Attach this to GameObjects whose Renderer's enabled state you wish to save.
	 * Fading in and out, due to the SpriteFader component, is also saved.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Visibility")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_visibility.html")]
	public class RememberVisibility : Remember
	{

		/** Whether the Renderer is enabled or not when the game begins */
		public AC_OnOff startState = AC_OnOff.On;
		/** True if child Renderers should be affected as well */
		public bool affectChildren = false;
		/** If True, the sprite's colour/alpha will be saved */
		public bool saveColour = false;

		private LimitVisibility limitVisibility;
		private bool loadedData = false;

		
		private void Awake ()
		{
			if (loadedData) return;

			if (GameIsPlaying ())
			{
				bool state = (startState == AC_OnOff.On) ? true : false;

				if (GetComponent <LimitVisibility>())
				{
					limitVisibility = GetComponent <LimitVisibility>();
					limitVisibility.isLockedOff = !state;
				}
				else if (GetComponent <Renderer>())
				{
					GetComponent <Renderer>().enabled = state;
				}

				if (affectChildren)
				{
					foreach (Renderer _renderer in GetComponentsInChildren <Renderer>())
					{
						_renderer.enabled = state;
					}
				}
			}
		}


		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			VisibilityData visibilityData = new VisibilityData ();
			visibilityData.objectID = constantID;
			visibilityData.savePrevented = savePrevented;

			if (GetComponent <SpriteFader>())
			{
				SpriteFader spriteFader = GetComponent <SpriteFader>();
				visibilityData.isFading = spriteFader.isFading;
				if (spriteFader.isFading)
				{
					if (spriteFader.fadeType == FadeType.fadeIn)
					{
						visibilityData.isFadingIn = true;
					}
					else
					{
						visibilityData.isFadingIn = false;
					}

					visibilityData.fadeTime = spriteFader.fadeTime;
					visibilityData.fadeStartTime = spriteFader.fadeStartTime;
				}
				visibilityData.fadeAlpha = GetComponent <SpriteRenderer>().color.a;
			}
			else if (GetComponent <SpriteRenderer>() && saveColour)
			{
				Color _color = GetComponent <SpriteRenderer>().color;
				visibilityData.colourR = _color.r;
				visibilityData.colourG = _color.g;
				visibilityData.colourB = _color.b;
				visibilityData.colourA = _color.a;
			}

			if (GetComponent <FollowTintMap>())
			{
				visibilityData = GetComponent <FollowTintMap>().SaveData (visibilityData);
			}

			if (limitVisibility)
			{
				visibilityData.isOn = !limitVisibility.isLockedOff;
			}
			else if (GetComponent <Renderer>())
			{
				visibilityData.isOn = GetComponent <Renderer>().enabled;
			}
			else if (affectChildren)
			{
				foreach (Renderer _renderer in GetComponentsInChildren <Renderer>())
				{
					visibilityData.isOn = _renderer.enabled;
					break;
				}
			}

			return Serializer.SaveScriptData <VisibilityData> (visibilityData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			VisibilityData data = Serializer.LoadScriptData <VisibilityData> (stringData);
			if (data == null)
			{
				loadedData = false;
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			if (GetComponent <SpriteFader>())
			{
				SpriteFader spriteFader = GetComponent <SpriteFader>();
				if (data.isFading)
				{
					if (data.isFadingIn)
					{
						spriteFader.Fade (FadeType.fadeIn, data.fadeTime, data.fadeAlpha);
					}
					else
					{
						spriteFader.Fade (FadeType.fadeOut, data.fadeTime, data.fadeAlpha);
					}
				}
				else
				{
					spriteFader.EndFade ();
					spriteFader.SetAlpha (data.fadeAlpha);
				}
			}
			else if (GetComponent <SpriteRenderer>() && saveColour)
			{
				Color _color = new Color (data.colourR, data.colourG, data.colourB, data.colourA);
				GetComponent <SpriteRenderer>().color = _color;
			}

			if (GetComponent <FollowTintMap>())
			{
				GetComponent <FollowTintMap>().LoadData (data);
			}

			if (limitVisibility)
			{
				limitVisibility.isLockedOff = !data.isOn;
			}
			else if (GetComponent <Renderer>())
			{
				GetComponent <Renderer>().enabled = data.isOn;
			}

			if (affectChildren)
			{
				foreach (Renderer _renderer in GetComponentsInChildren <Renderer>())
				{
					_renderer.enabled = data.isOn;
				}
			}

			loadedData = true;
		}
		
	}


	/**
	 * A data container used by the RememberVisibility script.
	 */
	[System.Serializable]
	public class VisibilityData : RememberData
	{

		/** True if the Renderer is enabled */
		public bool isOn;
		/** True if the Renderer is fading */
		public bool isFading;
		/** True if the Renderer is fading in */
		public bool isFadingIn;
		/** The fade duration, if the Renderer is fading */
		public float fadeTime;
		/** The fade start time, if the Renderer is fading */
		public float fadeStartTime;
		/** The current alpha, if the Renderer is fading */
		public float fadeAlpha;

		/** If True, then the attached FollowTintMap makes use of the default TintMap defined in SceneSettings */
		public bool useDefaultTintMap;
		/** The ConstantID number of the attached FollowTintMap's tintMap object */
		public int tintMapID;
		/** The intensity value of the attached FollowTintMap component */
		public float tintIntensity;

		/** The Red channel of the sprite's colour */
		public float colourR;
		/** The Green channel of the sprite's colour */
		public float colourG;
		/** The Blue channel of the sprite's colour */
		public float colourB;
		/** The Alpha channel of the sprite's colour */
		public float colourA;

		/**
		 * The default Constructor.
		 */
		public VisibilityData () { }

	}

}