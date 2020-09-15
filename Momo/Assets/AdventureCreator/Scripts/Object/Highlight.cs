/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Highlight.cs"
 * 
 *	This script is attached to any gameObject that glows
 *	when a cursor is placed over its associated interaction
 *	object.  These are not always the same object.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace AC
{

	/**
	 * Allows GameObjects associated with Hotspots to glow when the Hotspots are made active.
	 * Attach it to a mesh renderer, and assign it as the Hotspot's highlight variable.
	 */
	[AddComponentMenu("Adventure Creator/Hotspots/Highlight")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_highlight.html")]
	public class Highlight : MonoBehaviour
	{

		#region Variables

		/** If True, then the Highlight effect will be enabled automatically when the Hotspot is selected */
		public bool highlightWhenSelected = true;
		/** If True, then Materials associated with the GameObject's Renderer will be affected. Otherwise, their intended values will be calculated, but not applied, allowing for custom effects to be achieved. */
		public bool brightenMaterials = true;
		/** If True, then child Renderer GameObjects will be brightened as well */
		public bool affectChildren = true;
		/** The maximum highlight intensity (1 = no effect) */
		public float maxHighlight = 2f;
		/** The fade time for the highlight transition effect */
		public float fadeTime = 0.3f;
		/** The length of time that a flash will hold for */
		public float flashHoldTime = 0f;

		/** If True, then custom events can be called when highlighting the object */
		public bool callEvents;
		/** The UnityEvent to run when the highlight effect is enabled */
		public UnityEvent onHighlightOn;
		/** The UnityEvent to run when the highlight effect is disabled */
		public UnityEvent onHighlightOff;

		protected float minHighlight = 1f;
		protected float highlight = 1f;
		protected int direction = 1;
		protected float fadeStartTime;
		protected HighlightState highlightState = HighlightState.None;
		protected List<Color> originalColors = new List<Color>();
		protected Renderer _renderer;
		protected Renderer[] childRenderers;

		#endregion


		#region UnityStandards

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


		protected void Awake ()
		{
			// Go through own materials
			_renderer = GetComponent <Renderer>();
			if (_renderer != null)
			{
				foreach (Material material in _renderer.materials)
				{
					if (material.HasProperty ("_Color"))
					{
						originalColors.Add (material.color);
					}
				}
			}

			// Go through any child materials
			childRenderers = GetComponentsInChildren <Renderer>();
			foreach (Renderer childRenderer in childRenderers)
			{
				foreach (Material material in childRenderer.materials)
				{
					if (material.HasProperty ("_Color"))
					{
						originalColors.Add (material.color);
					}
				}
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Gets the intended intensity of the highlighting effect at the current point in time.</summary>
		 * <returns>The intended intensity of the highlight, ranging from 0 to 1.</returns>
		 */
		public float GetHighlightIntensity ()
		{
			return (highlight - 1f) / maxHighlight;
		}


		/**
		 * <summary>Gets the highlight effect's intensity, as an alpha value for associated icon textures.</summary>
		 * <returns>The alpha value of the highlight effect</returns>
		 */
		public float GetHighlightAlpha ()
		{
			return (highlight - 1f);
		}


		/**
		 * <summary>Sets the minimum intensity of the highlighting effect - i.e. the intensity when the effect is considered "off".</summary>
		 * <param name = "_minHighlight">The minimum intensity of the highlighting effect</param>
		 */
		public void SetMinHighlight (float _minHighlight)
		{
			minHighlight = _minHighlight + 1f;

			if (minHighlight < 1f)
			{
				minHighlight = 1f;
			}
			else if (minHighlight > maxHighlight)
			{
				minHighlight = maxHighlight;
			}
		}


		/**
		 * Turns the highlight effect on. The effect will occur over time.
		 */
		public void HighlightOn ()
		{
			if (highlightState == HighlightState.On ||
			   (highlightState == HighlightState.Normal && direction == 1))
			{
				return;
			}

			highlightState = HighlightState.Normal;
			direction = 1;
			fadeStartTime = Time.time;
			
			if (highlight > minHighlight)
			{
				fadeStartTime -= (highlight - minHighlight) / (maxHighlight - minHighlight) * fadeTime;
			}
			else
			{
				highlight = minHighlight;
			}

			if (callEvents && onHighlightOn != null)
			{
				onHighlightOn.Invoke();
			}
		}


		/**
		 * Instantly turns the highlight effect on, to its maximum intensity.
		 */
		public void HighlightOnInstant ()
		{
			highlightState = HighlightState.On;
			highlight = maxHighlight;
			
			UpdateMaterials ();

			if (callEvents && onHighlightOn != null)
			{
				onHighlightOn.Invoke();
			}
		}
		

		/**
		 * Turns the highlight effect off. The effect will occur over time.
		 */
		public void HighlightOff ()
		{
			highlightState = HighlightState.Normal;
			direction = -1;
			fadeStartTime = Time.time;
			
			if (highlight < maxHighlight)
			{
				fadeStartTime -= (maxHighlight - highlight) / (maxHighlight - minHighlight) * fadeTime;
			}
			else
			{
				highlight = maxHighlight;
			}

			if (callEvents && onHighlightOff != null)
			{
				onHighlightOff.Invoke();
			}
		}


		/**
		 * Instantly turns the highlight effect off.
		 */
		public void HighlightOffInstant ()
		{
			minHighlight = 1f;
			highlightState = HighlightState.None;
			highlight = minHighlight;
			
			UpdateMaterials ();

			if (callEvents && onHighlightOff != null)
			{
				onHighlightOff.Invoke();
			}
		}
		

		/**
		 * Flashes the highlight effect on, and then off, once.
		 */
		public void Flash ()
		{
			if (highlightState != HighlightState.Flash && (highlightState == HighlightState.None || direction == -1))
			{
				highlightState = HighlightState.Flash;
				highlight = minHighlight;
				direction = 1;
				fadeStartTime = Time.time;
			}
		}


		/**
		 * <summary>Gets the duration of the flash (i.e. turn on, then off) effect.</summary>
		 * <returns>The duration, in effect, that the flash effect will last</returns>
		 */
		public float GetFlashTime ()
		{
			return fadeTime * 2f;
		}


		public void CancelFlash ()
		{
			if (direction >= 0 && highlightState == HighlightState.Flash)
			{
				direction = 0;
				fadeStartTime = 0f;
			}
		}


		/**
		 * <summary>Gets the flash effect's intensity, as an alpha value for associated icon textures.</summary>
		 * <param name = "original">The original alpha value of the texture this is being called for</param>
		 * <returns>The flash effect's intensity, as an alpha value</returns>
		 */
		public float GetFlashAlpha (float original)
		{
			if (highlightState == HighlightState.Flash)
			{
				return (highlight - 1f);
			}
			return Mathf.Lerp (original, 0f, Time.deltaTime * 5f);
		}


		/**
		 * <summary>Gets the time that it will take to turn the highlight effect fully on or fully off.</summary>
		 * <returns>The time, in seconds, that it takes to turn the highlight effect fully on or fully off</returns>
		 */
		public float GetFadeTime ()
		{
			return fadeTime;
		}


		/**
		 * Pulses the highlight effect on, and then off, in a continuous cycle.
		 */
		public void Pulse ()
		{
			highlightState = HighlightState.Pulse;
			highlight = minHighlight;
			direction = 1;
			fadeStartTime = Time.time;
		}


		/**
		 * Re-calculates the intensity value. This is public so that it can be called every frame by the StateHandler component.
		 */
		public void _Update ()
		{
			if (highlightState != HighlightState.None)
			{	
				if (direction > 0)
				{
					// Add highlight
					highlight = Mathf.Lerp (minHighlight, maxHighlight, AdvGame.Interpolate (fadeStartTime, fadeTime, MoveMethod.Linear, null));
					
					if (highlight >= maxHighlight)
					{
						highlight = maxHighlight;

						switch (highlightState)
						{
							case HighlightState.Flash:
								direction = 0;
								fadeStartTime = flashHoldTime;
								break;

							case HighlightState.Pulse:
								direction = -1;
								fadeStartTime = Time.time;
								break;

							default:
								highlightState = HighlightState.On;
								break;
						}
					}
				}
				else if (direction < 0)
				{
					// Remove highlight
					highlight = Mathf.Lerp (maxHighlight, minHighlight, AdvGame.Interpolate (fadeStartTime, fadeTime, AC.MoveMethod.Linear, null));
					
					if (highlight <= 1f)
					{
						highlight = 1f;
						
						if (highlightState == HighlightState.Pulse)
						{
							direction = 1;
							fadeStartTime = Time.time;
						}
						else
						{
							highlightState = HighlightState.None;
						}
					}
				}
				else
				{
					// Flash pause
					fadeStartTime -= Time.deltaTime;
					if (fadeStartTime <= 0f)
					{
						direction = -1;
						highlight = maxHighlight;
						fadeStartTime = Time.time;
					}
				}
				
				UpdateMaterials ();
			}
			else
			{
				if (!Mathf.Approximately (highlight, minHighlight))
				{
					highlight = minHighlight;
					UpdateMaterials ();
				}
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void UpdateMaterials ()
		{
			if (!brightenMaterials)
			{
				return;
			}

			int i = 0;
			float alpha;

			// Go through own materials
			if (_renderer)
			{
				foreach (Material material in _renderer.materials)
				{
					if (material.HasProperty ("_Color"))
					{
						alpha = material.color.a;
						Color newColor = originalColors[i] * highlight;
						newColor.a = alpha;
						material.color = newColor;
						i++;
					}
				}
			}

			if (affectChildren)
			{
				// Go through materials
				foreach (Renderer childRenderer in childRenderers)
				{
					foreach (Material material in childRenderer.materials)
					{
						if (originalColors.Count <= i)
						{
							break;
						}
						
						if (material.HasProperty ("_Color"))
						{
							alpha = material.color.a;
							Color newColor = originalColors[i] * highlight;
							newColor.a = alpha;
							material.color = newColor;
							i++;
						}
					}
				}
			}
		}

		#endregion

	}

}