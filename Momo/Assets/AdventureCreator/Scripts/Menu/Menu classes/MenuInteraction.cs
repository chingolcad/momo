/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MenuInteraction.cs"
 * 
 *	This MenuElement displays a cursor icon inside a menu.
 * 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that displays available interactions inside a Menu.
	 * It is used to allow the player to run Hotspot interactions from within a UI.
	 */
	public class MenuInteraction : MenuElement
	{

		/** The Unity UI Button this is linked to (Unity UI Menus only) */
		public UnityEngine.UI.Button uiButton;
		/** What pointer state registers as a 'click' for Unity UI Menus (PointerClick, PointerDown, PointerEnter) */
		public UIPointerState uiPointerState = UIPointerState.PointerClick;

		/** How interactions are displayed (IconOnly, TextOnly, IconAndText) */
		public AC_DisplayType displayType = AC_DisplayType.IconOnly;
		/** The text alignment */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The ID number of the interaction's associated CursorIcon */
		public int iconID;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
		/** If True, the element's texture can be set independently of the associated interaction icon set within the Cursor Manager (OnGUI only) */
		public bool overrideTexture;
		/** The element's texture (OnGUI only) */
		public Texture activeTexture;

		#if TextMeshProIsPresent
		private TMPro.TextMeshProUGUI uiText;
		#else
		private Text uiText;
		#endif

		private Image uiImage;
		private CursorIcon icon;
		private string label = "";
		private bool isDefaultIcon = false;

		private CursorManager cursorManager;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiButton = null;
			uiPointerState = UIPointerState.PointerClick;
			uiImage = null;
			uiText = null;
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (5f, 5f));
			iconID = -1;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			overrideTexture = false;
			activeTexture = null;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
			
			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuInteraction newElement = CreateInstance <MenuInteraction>();
			newElement.Declare ();
			newElement.CopyInteraction (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyInteraction (MenuInteraction _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiButton = null;
			}
			else
			{
				uiButton = _element.uiButton;
			}

			uiPointerState = _element.uiPointerState;
			uiText = null;
			uiImage = null;

			displayType = _element.displayType;
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			iconID = _element.iconID;
			overrideTexture = _element.overrideTexture;
			activeTexture = _element.activeTexture;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;

			base.Copy (_element);
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu">The element's parent Menu<param>
		 */
		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas)
		{
			uiButton = LinkUIElement <UnityEngine.UI.Button> (canvas);
			if (uiButton)
			{
				#if TextMeshProIsPresent
				uiText = uiButton.GetComponentInChildren <TMPro.TextMeshProUGUI>();
				#else
				uiText = uiButton.GetComponentInChildren <Text>();
				#endif

				uiImage = uiButton.GetComponentInChildren <Image>();
				CreateUIEvent (uiButton, _menu, uiPointerState);
			}
		}


		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (uiButton)
			{
				return uiButton.gameObject;
			}
			return null;
		}
		

		/**
		 * <summary>Gets the boundary of the element</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <returns>The boundary Rect of the element</returns>
		 */
		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiButton)
			{
				return uiButton.GetComponent <RectTransform>();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			if (uiButton)
			{
				uiButton.interactable = state;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuInteraction)";

			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");

			if (source == MenuSource.AdventureCreator)
			{
				GetCursorGUI ();
				displayType = (AC_DisplayType) CustomGUILayout.EnumPopup ("Display type:", displayType, apiPrefix + ".displayType", "How interactions are displayed");

				if (displayType != AC_DisplayType.TextOnly)
				{
					overrideTexture = CustomGUILayout.Toggle ("Override icon texture?", overrideTexture, apiPrefix + ".overrideTexture", "If True, the element's texture can be set independently of the associated interaction icon set within the Cursor Manager");
				}
			}
			else
			{
				uiButton = LinkedUiGUI <UnityEngine.UI.Button> (uiButton, "Linked Button:", source, "The Unity UI Button this is linked to");
				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
				uiPointerState = (UIPointerState) CustomGUILayout.EnumPopup ("Responds to:", uiPointerState, apiPrefix + ".uiPointerState", "What pointer state registers as a 'click' for Unity UI Menus");
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
				displayType = (AC_DisplayType) CustomGUILayout.EnumPopup ("Display type:", displayType, apiPrefix + ".displayType", "How interactions are displayed");
				GetCursorGUI ();
			}
			alternativeInputButton = CustomGUILayout.TextField ("Alternative input button:", alternativeInputButton, apiPrefix + ".alternativeInputButton", "The name of the input button that triggers the element when pressed");
			EditorGUILayout.EndVertical ();

			base.ShowGUI (menu);
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			if (displayType != AC_DisplayType.IconOnly)
			{
				anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
				textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");
				if (textEffects != TextEffects.None)
				{
					outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The outline thickness");
				}
			}
		}


		protected override void ShowTextureGUI (string apiPrefix)
		{
			if (displayType != AC_DisplayType.TextOnly && overrideTexture)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Active texture:", GUILayout.Width (145f));
				activeTexture = (Texture) EditorGUILayout.ObjectField (activeTexture, typeof (Texture), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		
		private void GetCursorGUI ()
		{
			if (AdvGame.GetReferences ().cursorManager && AdvGame.GetReferences().cursorManager.cursorIcons.Count > 0)
			{
				int iconInt = AdvGame.GetReferences ().cursorManager.GetIntFromID (iconID);
				iconInt = EditorGUILayout.Popup ("Cursor:", iconInt, AdvGame.GetReferences ().cursorManager.GetLabelsArray ());
				iconID = AdvGame.GetReferences ().cursorManager.cursorIcons [iconInt].id;
			}
			else
			{
				iconID = -1;
			}
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (uiButton != null && uiButton.gameObject == gameObject) return true;
			if (linkedUiID == id) return true;
			return false;
		}
		
		#endif


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			isDefaultIcon = false;
			if (Application.isPlaying && KickStarter.stateHandler.IsInGameplay () && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
			{
				if (KickStarter.settingsManager.allowDefaultinteractions &&
					KickStarter.runtimeInventory.SelectedItem == null &&
					KickStarter.playerInteraction.GetActiveHotspot () != null &&
					KickStarter.playerInteraction.GetActiveHotspot ().GetFirstUseIcon () == iconID)
				{
					isActive = true;
					isDefaultIcon = true;
				}
				else if (KickStarter.settingsManager.allowDefaultInventoryInteractions &&
						 KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple &&
						 KickStarter.settingsManager.CanSelectItems (false) &&
						 KickStarter.playerInteraction.GetActiveHotspot () == null &&
						 KickStarter.runtimeInventory.SelectedItem == null &&
						 KickStarter.runtimeInventory.hoverItem != null &&
						 KickStarter.runtimeInventory.hoverItem.GetFirstStandardIcon () == iconID
						 )
				{
					isActive = true;
					isDefaultIcon = true;
				}
			}

			if (uiButton != null)
			{
				UpdateUISelectable (uiButton, uiSelectableHideStyle);
					
				if (displayType != AC_DisplayType.IconOnly && uiText != null)
				{
					uiText.text = label;
				}
				if (displayType == AC_DisplayType.IconOnly && uiImage != null && icon != null && icon.isAnimated)
				{
					uiImage.sprite = icon.GetAnimatedSprite (isActive);
				}

				if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot &&
					iconID == KickStarter.playerInteraction.GetActiveUseButtonIconID ())
				{
					// Select through script, not by mouse-over
					uiButton.Select ();
				}
			}
		}
		

		/**
		 * <summary>Draws the element using OnGUI</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			_style.wordWrap = true;
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}

			if (displayType != AC_DisplayType.IconOnly)
			{
				if (textEffects != TextEffects.None)
				{
					AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), label, _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
				}
				else
				{
					GUI.Label (ZoomRect (relativeRect, zoom), label, _style);
				}
			}
			else
			{
				GUI.Label (ZoomRect (relativeRect, zoom), "", _style);
			}

			if (overrideTexture)
			{
				if (iconID >= 0 && KickStarter.playerCursor.GetSelectedCursorID () == iconID && activeTexture != null)
				{
					GUI.DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), activeTexture, ScaleMode.StretchToFill, true, 0f);
				}
			}
			else
			{
				if (displayType != AC_DisplayType.TextOnly && icon != null)
				{
					icon.DrawAsInteraction (ZoomRect (relativeRect, zoom), isActive);
				}
			}
		}


		/**
		 * <summary>Recalculates display for a particular inventory item.</summary>
		 * <param name = "parentMenu">The Menu that the element is a part of</param>
		 * <param name = "item">The InvItem to recalculate the Menus's display for</param>
		 */
		public void MatchInteractions (InvItem item)
		{
			bool match = false;
			foreach (InvInteraction interaction in item.interactions)
			{
				if (interaction.icon.id == iconID)
				{
					match = true;
					break;
				}
			}

			IsVisible = match;
		}
		

		/**
		 * <summary>Recalculates display for a particular set of Hotspot Buttons.</summary>
		 * <param name = "parentMenu">The Menu that the element is a part of</param>
		 * <param name = "buttons">A List of Button classes to recalculate the Menus's display for</param>
		 */
		public void MatchInteractions (List<AC.Button> buttons)
		{
			bool match = false;
			
			foreach (AC.Button button in buttons)
			{
				if (button.iconID == iconID && !button.isDisabled)
				{
					match = true;
					break;
				}
			}

			IsVisible = match;
		}


		/**
		 * <summary>Recalculates display for an "Use" Hotspot Button.</summary>
		  * <param name = "button">A Button class to recalculate the Menus's display for</param>
		 */
		public void MatchUseInteraction (AC.Button button)
		{
			if (button.iconID == iconID && !button.isDisabled)
			{
				IsVisible = true;
			}
		}


		/**
		 * <summary>Recalculates display for a given cursor icon ID.</summary>
		 * <param name = "parentMenu">The Menu that the element is a part of</param>
		 * <param name = "_iconID">The ID number of the CursorIcon in CursorManager</param>
		 */
		public void MatchInteraction (int _iconID)
		{
			if (_iconID == iconID)
			{
				IsVisible = true;
			}
		}


		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			return label;
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiButton != null)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiButton.gameObject);
			}
			return false;
		}


		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void RecalculateSize (MenuSource source)
		{
			if (AdvGame.GetReferences ().cursorManager)
			{
				CursorIcon _icon = AdvGame.GetReferences ().cursorManager.GetCursorIconFromID (iconID);
				if (_icon != null)
				{
					icon = _icon;
					if (Application.isPlaying)
					{
						label = KickStarter.runtimeLanguages.GetTranslation (_icon.label, _icon.lineID, Options.GetLanguage (), _icon.GetTranslationType (0));
					}
					else
					{
						label = _icon.label;
					}
					icon.Reset ();
				}
			}

			base.RecalculateSize (source);
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return;
			}

			if (_mouseState == MouseState.RightClick)
			{
				return;
			}

			KickStarter.playerInteraction.ClickInteractionIcon (_menu, iconID);
			base.ProcessClick (_menu, _slot, _mouseState);
		}
		
		
		protected override void AutoSize ()
		{
			if (displayType == AC_DisplayType.IconOnly && icon != null && icon.texture != null)
			{
				GUIContent content = new GUIContent (icon.texture);
				AutoSize (content);
			}
			else
			{
				GUIContent content = new GUIContent (TranslateLabel  (label, Options.GetLanguage ()));
				AutoSize (content);
			}
		}


		/**
		 * If using Choose Interaction Then Hotspot mode, and default interactions are enabled, then this is True if the active Hotspot's first-enabled Use interaction uses this icon
		 */
		public bool IsDefaultIcon
		{
			get
			{
				return isDefaultIcon;
			}
		}


		public override string GetHotspotLabelOverride (int _slot, int _language)
		{
			if (uiButton != null && !uiButton.interactable) return string.Empty;

			#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				return string.Empty;
			}
			#endif

			if (KickStarter.cursorManager.addHotspotPrefix)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.settingsManager.SelectInteractionMethod () != SelectInteractions.ClickingMenu)
					{
						// Don't override, refer to the clicked InventoryBox or Hotspot
						return string.Empty;
					}

					if (parentMenu.TargetInvItem != null)
					{
						return AdvGame.CombineLanguageString (
										KickStarter.cursorManager.GetLabelFromID (iconID, _language),
										parentMenu.TargetInvItem.GetLabel (_language),
										_language
										);
					}

					if (KickStarter.settingsManager.SelectInteractionMethod () != SelectInteractions.ClickingMenu)
					{
						return string.Empty;
					}

					if (parentMenu.TargetHotspot != null)
					{
						return AdvGame.CombineLanguageString (
										KickStarter.cursorManager.GetLabelFromID (iconID, _language),
										parentMenu.TargetHotspot.GetName (_language),
										_language
										);
					}
				}
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					if (KickStarter.settingsManager.ShowHoverInteractionInHotspotLabel ())
					{
						if (KickStarter.playerCursor.GetSelectedCursor () == -1)
						{
							return KickStarter.cursorManager.GetLabelFromID (iconID, _language);
						}
					}
				}
			}

			return string.Empty;
		}

	}

}