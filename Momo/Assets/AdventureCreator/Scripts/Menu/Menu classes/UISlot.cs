/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"UISlot.cs"
 * 
 *	This is a class for Unity UI elements that contain both
 *	Image and Text components that must be linked to AC's Menu system.
 * 
 */

using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A data container that links a Unity UI Button to AC's own Menu system.
	 */
	[System.Serializable]
	public class UISlot
	{

		#region Variables

		/** The Unity UI Button this is linked to */
		public UnityEngine.UI.Button uiButton;
		/** The ConstantID number of the linked Unity UI Button */
		public int uiButtonID;
		/** The sprite to set in the Button's Image */
		public UnityEngine.Sprite sprite;
		
		#if TextMeshProIsPresent
		private TMPro.TextMeshProUGUI uiText;
		#else
		private Text uiText;
		#endif

		private Image uiImage;
		private RawImage uiRawImage;

		private Color originalColour;
		private UnityEngine.Sprite emptySprite;
		private Texture cacheTexture;

		#endregion


		#region Constructors

		/**
		 * The default Constructor.
		 */
		public UISlot ()
		{
			uiButton = null;
			uiButtonID = 0;
			uiText = null;
			uiImage = null;
			uiRawImage = null;
			sprite = null;
		}


		/** A Constructor that gets its values by copying another */
		public UISlot (UISlot uiSlot)
		{
			uiButton = uiSlot.uiButton;
			uiButtonID = uiSlot.uiButtonID;
			sprite = uiSlot.sprite;
			uiImage = null;
			uiRawImage = null;
		}

		#endregion

		#if UNITY_EDITOR

		public void LinkedUiGUI (int i, MenuSource source)
		{
			uiButton = (UnityEngine.UI.Button) EditorGUILayout.ObjectField ("Linked Button (" + (i+1).ToString () + "):", uiButton, typeof (UnityEngine.UI.Button), true);

			if (Application.isPlaying && source == MenuSource.UnityUiPrefab)
			{}
			else
			{
				uiButtonID = Menu.FieldToID <UnityEngine.UI.Button> (uiButton, uiButtonID);
				uiButton = Menu.IDToField <UnityEngine.UI.Button> (uiButton, uiButtonID, source);
			}
		}

		#endif


		#region PublicFunctions

		/**
		 * <summary>Gets the boundary of the UI Button.</summary>
		 * <returns>The boundary Rect of the UI Button</returns>
		 */
		public RectTransform GetRectTransform ()
		{
			if (uiButton != null && uiButton.GetComponent <RectTransform>())
			{
				return uiButton.GetComponent <RectTransform>();
			}
			return null;
		}


		/**
		 * <summary>Links the UI GameObjects to the class, based on the supplied uiButtonID.</summary>
		 * <param name = "canvas">The Canvas that contains the UI GameObjects</param>
		 * <param name = "linkUIGraphic">What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic)</param>
		 * <param name = "emptySlotTexture">If set, the texture to use when a slot is considered empty</param>
		 */
		public void LinkUIElements (Canvas canvas, LinkUIGraphic linkUIGraphic, Texture2D emptySlotTexture = null)
		{
			if (canvas != null)
			{
				uiButton = Serializer.GetGameObjectComponent <UnityEngine.UI.Button> (uiButtonID, canvas.gameObject);
			}
			else
			{
				uiButton = null;
			}

			if (uiButton)
			{
				#if TextMeshProIsPresent
				uiText = uiButton.GetComponentInChildren <TMPro.TextMeshProUGUI>();
				#else
				uiText = uiButton.GetComponentInChildren <Text>();
				#endif
				uiRawImage = uiButton.GetComponentInChildren <RawImage>();

				if (linkUIGraphic == LinkUIGraphic.ImageComponent)
				{
					uiImage = uiButton.GetComponentInChildren <Image>();
				}
				else if (linkUIGraphic == LinkUIGraphic.ButtonTargetGraphic)
				{
					if (uiButton.targetGraphic != null)
					{
						if (uiButton.targetGraphic is Image)
						{
							uiImage = uiButton.targetGraphic as Image;
						}
						else
						{
							ACDebug.LogWarning ("Cannot assign UI Image for " + uiButton.name + "'s target graphic as " + uiButton.targetGraphic + " is not an Image component.", canvas);
						}
					}
					else
					{
						ACDebug.LogWarning ("Cannot assign UI Image for " + uiButton.name + "'s target graphic because it has none.", canvas);
					}
				}

				originalColour = uiButton.colors.normalColor;
			}

			if (emptySlotTexture != null)
			{
				emptySprite = Sprite.Create (emptySlotTexture, new Rect (0f, 0f, emptySlotTexture.width, emptySlotTexture.height), new Vector2 (0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
			}
		}


		/**
		 * <summary>Sets the text of the UI Button.</summary>
		 * <param name = "_text">The text to assign the Button</param>
		 */
		public void SetText (string _text)
		{
			if (uiText)
			{
				uiText.text = _text;
			}
		}


		/**
		 * <summary>Sets the image of the UI Button using a Texture.</summary>
		 * <param name = "_texture">The texture to assign the Button</param>
		 */
		public void SetImage (Texture _texture)
		{
			if (uiRawImage != null)
			{
				uiRawImage.texture = _texture;
			}
			else if (uiImage != null)
			{
				if (_texture == null)
				{
					sprite = EmptySprite;
				}
				else if (sprite == null || sprite == emptySprite || cacheTexture != _texture)
				{
					if (_texture is Texture2D)
					{
						Texture2D texture2D = (Texture2D) _texture;
						sprite = Sprite.Create (texture2D, new Rect (0f, 0f, texture2D.width, texture2D.height), new Vector2 (0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
					}
					else
					{
						ACDebug.LogWarning ("Cannot show texture " + _texture.name + " in UI Image " + uiImage.name + " because it is not a 2D Texture. Use a UI RawImage component instead.", uiImage);
					}
				}

				if (_texture != null)
				{
					cacheTexture = _texture;
				}
				uiImage.sprite = sprite;
			}
		}


		/**
		 * <summary>Sets the image of the UI Button using a Sprite.</summary>
		 * <param name = "_sprite">The sprite to assign the Button</param>
		 */
		public void SetImageAsSprite (Sprite _sprite)
		{
			if (uiImage != null)
			{
				if (_sprite == null)
				{
					sprite = EmptySprite;
				}
				else if (sprite == null || sprite == EmptySprite || sprite != _sprite)
				{
					sprite = _sprite;
				}

				uiImage.sprite = sprite;
			}
		}


		/**
		 * <summary>Enables the visibility of the linked UI Button.</summary>
		 * <param name = "uiHideStyle">The method by which the UI element is hidden (DisableObject, ClearContent) </param>
		 */
		public void ShowUIElement (UIHideStyle uiHideStyle)
		{
			if (Application.isPlaying && uiButton != null && uiButton.gameObject != null)
			{
				if (uiHideStyle == UIHideStyle.DisableObject && !uiButton.gameObject.activeSelf)
				{
					uiButton.gameObject.SetActive (true);
				}
			}
		}


		/**
		 * <summary>Disables the visibility of the linked UI Button.</summary>
		 * <param name = "uiHideStyle">The method by which the UI element is hidden (DisableObject, ClearContent) </param>
		 */
		public void HideUIElement (UIHideStyle uiHideStyle)
		{
			if (Application.isPlaying && uiButton != null && uiButton.gameObject != null && uiButton.gameObject.activeSelf)
			{
				if (uiHideStyle == UIHideStyle.DisableObject)
				{
					uiButton.gameObject.SetActive (false);
				}
				else if (uiHideStyle == UIHideStyle.ClearContent)
				{
					SetImage (null);
					SetText (string.Empty);
				}
			}
		}


		/**
		 * <summary>Adds a UISlotClick component to the Button, which acts as a click-handler.</summary>
		 * <param name = "_menu">The Menu that the Button is linked to</param>
		 * <param name = "_element">The MenuElement within _menu that the Button is linked to</param>
		 * <param name = "_slot">The index number of the slot within _element that the Button is linked to</param>
		 */
		public void AddClickHandler (AC.Menu _menu, MenuElement _element, int _slot)
		{
			UISlotClick uiSlotClick = uiButton.gameObject.AddComponent <UISlotClick>();
			uiSlotClick.Setup (_menu, _element, _slot);
		}


		/**
		 * <summary>Changes the 'normal' colour of the linked UI Button.</summary>
		 * <param name = "newColour">The new 'normal' colour to set</param>
		 */
		public void SetColour (Color newColour)
		{
			if (uiButton != null)
			{
				ColorBlock colorBlock = uiButton.colors;
				colorBlock.normalColor = newColour;
				uiButton.colors = colorBlock;
			}
		}


		/**
		 * <summary>Reverts the 'normal' colour of the linked UI Button, if it was changed using SetColour.</summary>
		 */
		public void RestoreColour ()
		{
			if (uiButton != null)
			{
				ColorBlock colorBlock = uiButton.colors;
				colorBlock.normalColor = originalColour;
				uiButton.colors = colorBlock;
			}
		}

		#endregion


		#region GetSet

		/** Checks if the associated UI components can set a Hotspot label when selected */
		public bool CanOverrideHotspotLabel
		{
			get
			{
				if (uiButton != null)
				{
					return uiButton.interactable;
				}
				return true;
			}
		}


		private Sprite EmptySprite
		{
			get
			{
				if (emptySprite == null)
				{
					emptySprite = Resources.Load<Sprite> (Resource.emptySlot);
				}
				return emptySprite;
			}
		}

		#endregion

	}

}