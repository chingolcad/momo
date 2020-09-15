/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"SpriteFader.cs"
 * 
 *	Attach this to any sprite you wish to fade.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * Provides functions that can fade a sprite in and out.
	 */
	[AddComponentMenu("Adventure Creator/Misc/Sprite fader")]
	[RequireComponent (typeof (SpriteRenderer))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_sprite_fader.html")]
	public class SpriteFader : MonoBehaviour
	{

		#region Variables

		/** If True, then child Sprite will also be affected */
		public bool affectChildren = false;

		/** True if the Sprite attached to the GameObject this script is attached to is currently fading */
		[HideInInspector] public bool isFading = true;
		/** The time at which the sprite began fading */
		[HideInInspector] public float fadeStartTime;
		/** The duration of the sprite-fading effect */
		[HideInInspector] public float fadeTime;
		/** The direction of the sprite-fading effect (fadeIn, fadeOut) */
		[HideInInspector] public FadeType fadeType;

		protected SpriteRenderer spriteRenderer;
		protected SpriteRenderer[] childSprites;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			spriteRenderer = GetComponent <SpriteRenderer>();

			if (affectChildren)
			{
				childSprites = GetComponentsInChildren <SpriteRenderer>();
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Forces the alpha value of a sprite to a specific value.</summary>
		 * <param name = "_alpha">The alpha value to assign the sprite attached to this GameObject</param>
		 */
		public void SetAlpha (float _alpha)
		{
			if (affectChildren && childSprites != null)
			{
				foreach (SpriteRenderer childSprite in childSprites)
				{
					SetSpriteAlpha (childSprite, _alpha);
				}
			}
			else
			{
				SetSpriteAlpha (spriteRenderer, _alpha);
			}
		}


		/**
		 * <summary>Gets the alpha value of the SpriteRenderer, which in turn is determind by its color</summary>
		 * <returns>Gets the alpha value of the SpriteRenderer, where 0 = fully transparent, and 1 = fully opaque</returns>
		 */
		public float GetAlpha ()
		{
			return spriteRenderer.color.a;
		}


		/**
		 * <summary>Fades the Sprite attached to this GameObject in or out.</summary>
		 * <param name = "_fadeType">The direction of the fade effect (fadeIn, fadeOut)</param>
		 * <param name = "_fadeTime">The duration, in seconds, of the fade effect</param>
		 * <param name = "startAlpha">The alpha value that the Sprite should have when the effect begins. If <0, the Sprite's original alpha will be used.</param>
		 */
		public void Fade (FadeType _fadeType, float _fadeTime, float startAlpha = -1)
		{
			StopCoroutine ("DoFade");

			float currentAlpha = GetAlpha ();

			if (startAlpha >= 0)
			{
				currentAlpha = startAlpha;
				SetAlpha (startAlpha);
			}
			else
			{
				if (spriteRenderer.enabled == false)
				{
					SetEnabledState (true);

					if (_fadeType == FadeType.fadeIn)
					{
						currentAlpha = 0f;
						SetAlpha (0f);
					}
				}
			}

			if (_fadeType == FadeType.fadeOut)
			{
				fadeStartTime = Time.time - (currentAlpha * _fadeTime);
			}
			else
			{
				fadeStartTime = Time.time - ((1f - currentAlpha) * _fadeTime);
			}
		
			fadeTime = _fadeTime;
			fadeType = _fadeType;

			if (fadeTime > 0f)
			{
				StartCoroutine ("DoFade");
			}
			else
			{
				EndFade ();
			}
		}


		/**
		 * Ends the sprite-fading effect, and sets the Sprite's alpha to its target value.
		 */
		public void EndFade ()
		{
			StopCoroutine ("DoFade");

			isFading = false;

			if (fadeType == FadeType.fadeIn)
			{
				SetAlpha (1f);
			}
			else
			{
				SetAlpha (0f);
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void SetSpriteAlpha (SpriteRenderer _spriteRenderer, float alpha)
		{
			Color color = _spriteRenderer.color;
			color.a = alpha;
			_spriteRenderer.color = color;
		}


		protected void SetEnabledState (bool value)
		{
			spriteRenderer.enabled = value;
			if (affectChildren && childSprites != null)
			{
				foreach (SpriteRenderer childSprite in childSprites)
				{
					childSprite.enabled = value;
				}
			}
		}


		protected IEnumerator DoFade ()
		{
			SetEnabledState (true);

			isFading = true;

			float alpha = GetAlpha ();

			if (fadeType == FadeType.fadeIn)
			{
				while (alpha < 1f)
				{
					alpha = -1f + AdvGame.Interpolate (fadeStartTime, fadeTime, MoveMethod.Linear, null);
					SetAlpha (alpha);
					yield return new WaitForFixedUpdate ();
				}
				SetAlpha (1f);
			}
			else
			{
				while (alpha > 0f)
				{
					alpha = 2f - AdvGame.Interpolate (fadeStartTime, fadeTime, MoveMethod.Linear, null);
					SetAlpha (alpha);
					yield return new WaitForFixedUpdate ();
				}
				SetAlpha (0f);
			}
			isFading = false;
		}

		#endregion

	}

}