/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MenuDialogList.cs"
 * 
 *	This MenuElement lists the available options of the active conversation,
 *	and runs them when clicked on.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;	
#endif

namespace AC
{

	/**
	 * A MenuElement that lists the available options in the active Conversation, and runs their interactions when clicked on.
	 */
	public class MenuDialogList : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** How the Conversation's dialogue options are displayed (IconOnly, TextOnly, IconAndText) */
		public ConversationDisplayType displayType = ConversationDisplayType.TextOnly;
		/** A temporary dialogue option icon, used for test purposes when the game is not running */
		public Texture2D testIcon = null;
		/** The text alignment */
		public TextAnchor anchor;
		/** If True, then only one dialogue option will be shown */
		public bool fixedOption;
		/** The index number of the dialogue option to show, if fixedOption = true */
		public int optionToShow;
		/** The maximum number of dialogue options that can be shown at once */
		public int maxSlots = 10;
		/** If True, then options that have already been clicked can be displayed in a different colour */
		public bool markAlreadyChosen = false;
		/** The font colour for options already chosen (If markAlreadyChosen = True, OnGUI only) */
		public Color alreadyChosenFontColour = Color.white;
		/** The font colour when the option is highlighted but has already been chosen (OnGUI only) */
		public Color alreadyChosenFontHighlightedColour = Color.white;
		/** If True, and displayType = ConversationDisplayType.TextOnly, then each option's index number will be prefixed to the label */
		public bool showIndexNumbers = false;
		/** The method by which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;
		/** What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic) */
		public LinkUIGraphic linkUIGraphic = LinkUIGraphic.ImageComponent;
		/** If True, then the offset value will be reset when the parent menu is turned on for the same Conversation that it last displayed */
		public bool resetOffsetWhenRestart = true;

		private Conversation linkedConversation;
		private Conversation overrideConversation;
		private int numOptions = 0;
		private string[] labels = null;
		private CursorIconBase[] icons;
		private bool[] chosens = null;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiSlots = null;

			isVisible = true;
			isClickable = true;
			fixedOption = false;
			displayType = ConversationDisplayType.TextOnly;
			testIcon = null;
			optionToShow = 1;
			numSlots = 0;
			SetSize (new Vector2 (20f, 5f));
			maxSlots = 10;
			anchor = TextAnchor.MiddleLeft;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			markAlreadyChosen = false;
			alreadyChosenFontColour = Color.white;
			alreadyChosenFontHighlightedColour = Color.white;
			showIndexNumbers = false;
			uiHideStyle = UIHideStyle.DisableObject;
			linkUIGraphic = LinkUIGraphic.ImageComponent;
			resetOffsetWhenRestart = true;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuDialogList newElement = CreateInstance <MenuDialogList>();
			newElement.Declare ();
			newElement.CopyDialogList (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyDialogList (MenuDialogList _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiSlots = null;
			}
			else
			{
				uiSlots = new UISlot[_element.uiSlots.Length];
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i] = new UISlot (_element.uiSlots[i]);
				}
			}

			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			displayType = _element.displayType;
			testIcon = _element.testIcon;
			anchor = _element.anchor;
			labels = _element.labels;
			fixedOption = _element.fixedOption;
			optionToShow = _element.optionToShow;
			maxSlots = _element.maxSlots;
			markAlreadyChosen = _element.markAlreadyChosen;
			alreadyChosenFontColour = _element.alreadyChosenFontColour;
			alreadyChosenFontHighlightedColour = _element.alreadyChosenFontHighlightedColour;
			showIndexNumbers = _element.showIndexNumbers;
			uiHideStyle = _element.uiHideStyle;
			linkUIGraphic = _element.linkUIGraphic;
			resetOffsetWhenRestart = _element.resetOffsetWhenRestart;

			base.Copy (_element);
		}


		/**
		 * Hides all linked Unity UI GameObjects associated with the element.
		 */
		public override void HideAllUISlots ()
		{
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObjects.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas)
		{
			int i=0;
			foreach (UISlot uiSlot in uiSlots)
			{
				uiSlot.LinkUIElements (canvas, linkUIGraphic);
				if (uiSlot != null && uiSlot.uiButton != null)
				{
					int j=i;
					uiSlot.uiButton.onClick.AddListener (() => {
						ProcessClickUI (_menu, j, KickStarter.playerInput.GetMouseState ());
					});
				}
				i++;
			}
		}


		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (uiSlots != null && uiSlots.Length > slotIndex && uiSlots[slotIndex].uiButton != null)
			{
				return uiSlots[slotIndex].uiButton.gameObject;
			}
			return null;
		}
		

		/**
		 * <summary>Gets the boundary of a slot</summary>
		 * <param name = "_slot">The index number of the slot to get the boundary of</param>
		 * <returns>The boundary Rect of the slot</returns>
		 */
		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiSlots != null && uiSlots.Length > _slot)
			{
				return uiSlots[_slot].GetRectTransform ();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			SetUISlotsInteractableState (uiSlots, state);
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuDialogList)";

			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");
			fixedOption = CustomGUILayout.Toggle ("Fixed option number?", fixedOption, apiPrefix + ".fixedOption", "If True, then only one dialogue option will be shown");
			if (fixedOption)
			{
				numSlots = 1;
				slotSpacing = 0f;
				optionToShow = CustomGUILayout.IntSlider ("Option to display:", optionToShow, 1, 10, apiPrefix + ".optionToShow", "The index number of the dialogue option to show");
			}
			else
			{
				maxSlots = CustomGUILayout.IntField ("Maximum number of slots:", maxSlots, apiPrefix + ".maxSlots", "The maximum number of dialogue options that can be shown at once");
				resetOffsetWhenRestart = CustomGUILayout.Toggle ("Reset offset when turn on?", resetOffsetWhenRestart, apiPrefix + ".resetOffsetWhenRestart", "If True, then the offset value will be reset when the parent menu is turned on for the same Conversation that it last displayed");

				if (source == MenuSource.AdventureCreator)
				{
					numSlots = CustomGUILayout.IntSlider ("Test slots:", numSlots, 1, maxSlots, apiPrefix + ".numSlots");
					slotSpacing = CustomGUILayout.Slider ("Slot spacing:", slotSpacing, 0f, 30f, apiPrefix + ".slotSpacing");
					orientation = (ElementOrientation) CustomGUILayout.EnumPopup ("Slot orientation:", orientation, apiPrefix + ".orientation");
					if (orientation == ElementOrientation.Grid)
					{
						gridWidth = CustomGUILayout.IntSlider ("Grid size:", gridWidth, 1, 10, apiPrefix + ".gridWidth");
					}
				}
			}

			displayType = (ConversationDisplayType) CustomGUILayout.EnumPopup ("Display type:", displayType, apiPrefix + ".displayType", "How the Conversation's dialogue options are displayed");
			if (displayType == ConversationDisplayType.IconAndText && source == MenuSource.AdventureCreator)
			{
				EditorGUILayout.HelpBox ("'Icon And Text' mode is only available for Unity UI-based Menus.", MessageType.Warning);
			}

			markAlreadyChosen = CustomGUILayout.Toggle ("Mark options already used?", markAlreadyChosen, apiPrefix + ".markAlreadyChosen", "If True, then options that have already been clicked can be displayed in a different colour");
			if (markAlreadyChosen)
			{
				alreadyChosenFontColour = (Color) CustomGUILayout.ColorField ("'Already chosen' colour:", alreadyChosenFontColour, apiPrefix + ".alreadyChosenFontColour", "The font colour for options already chosen");
				alreadyChosenFontHighlightedColour = (Color) CustomGUILayout.ColorField ("'Already chosen' highlighted colour:", alreadyChosenFontHighlightedColour, apiPrefix + ".alreadyChosenFontHighlightedColour", "The font colour when the option is highlighted but has already been chosen");
			}

			if (source != MenuSource.AdventureCreator)
			{
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
				uiHideStyle = (UIHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiHideStyle, apiPrefix + ".uiHideStyle", "The method by which this element (or slots within it) are hidden from view when made invisible");
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				if (fixedOption)
				{
					uiSlots = ResizeUISlots (uiSlots, 1);
				}
				else
				{
					uiSlots = ResizeUISlots (uiSlots, maxSlots);
				}

				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}

				linkUIGraphic = (LinkUIGraphic) CustomGUILayout.EnumPopup ("Link graphics to:", linkUIGraphic, "", "What Image component the element's graphics should be linked to");
			}

			if (displayType == ConversationDisplayType.TextOnly || displayType == ConversationDisplayType.IconAndText)
			{
				showIndexNumbers = CustomGUILayout.Toggle ("Prefix with index numbers?", showIndexNumbers, apiPrefix + ".showIndexNumbers", "If True, then each option's index number will be prefixed to the label");
			}

			ChangeCursorGUI (menu);
			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			if (displayType != ConversationDisplayType.IconOnly)
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
			if (displayType == ConversationDisplayType.IconOnly)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Test icon:", GUILayout.Width (145f));
				testIcon = (Texture2D) EditorGUILayout.ObjectField (testIcon, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
			}
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			foreach (UISlot uiSlot in uiSlots)
			{
				if (uiSlot.uiButton != null && uiSlot.uiButton == gameObject) return true;
				if (uiSlot.uiButtonID == id) return true;
			}
			return false;
		}
		
		#endif

		public override string GetHotspotLabelOverride (int _slot, int _language)
		{
			if (uiSlots != null && _slot < uiSlots.Length && !uiSlots[_slot].CanOverrideHotspotLabel) return string.Empty;

			if (displayType == ConversationDisplayType.IconOnly)
			{
				if (labels.Length > _slot)
				{
					return labels[_slot];
				}
			}
			return string.Empty;
		}
		

		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (fixedOption)
			{
				_slot = 0;
			}

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);

					if (displayType == ConversationDisplayType.IconOnly || displayType == ConversationDisplayType.IconAndText)
					{
						uiSlots[_slot].SetImageAsSprite (icons [_slot].GetAnimatedSprite (isActive));
					}
					if (displayType == ConversationDisplayType.TextOnly || displayType == ConversationDisplayType.IconAndText)
					{
						uiSlots[_slot].SetText (labels [_slot]);
					}
				}
			}
			else
			{
				string fullText = "";
				if (fixedOption)
				{
					fullText = "Dialogue option " + optionToShow.ToString ();
					fullText = AddIndexNumber (fullText, optionToShow);
				}
				else
				{
					fullText = "Dialogue option " + _slot.ToString ();
					fullText = AddIndexNumber (fullText, _slot + 1);
				}

				if (labels == null || labels.Length != numSlots)
				{
					labels = new string[numSlots];
				}
				chosens = new bool[numSlots];
				labels [_slot] = fullText;
			}
		}


		private string AddIndexNumber (string _label, int _i)
		{
			if (showIndexNumbers)
			{
				return (_i.ToString () + ". " + _label);
			}
			return _label;
		}
		

		/**
		 * <summary>Draws the element using OnGUI</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">The index number of the slot to display</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			if (fixedOption)
			{
				_slot = 0;
			}

			if (markAlreadyChosen)
			{
				if (chosens[_slot])
				{
					if (isActive)
					{
						_style.normal.textColor = alreadyChosenFontHighlightedColour;
					}
					else
					{
						_style.normal.textColor = alreadyChosenFontColour;
					}
				}
				else if (isActive)
				{
					_style.normal.textColor = fontHighlightColor;
				}
				else
				{
					_style.normal.textColor = fontColor;
				}
			}

			_style.wordWrap = true;
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}

			if (displayType == ConversationDisplayType.TextOnly)
			{
				if (textEffects != TextEffects.None)
				{
					AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), labels [_slot], _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
				}
				else
				{
					GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), labels [_slot], _style);
				}
			}
			else
			{
				if (Application.isPlaying && icons[_slot] != null)
				{
					icons[_slot].DrawAsInteraction (ZoomRect (GetSlotRectRelative (_slot), zoom), isActive);
				}
				else if (testIcon != null)
				{
					GUI.DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), testIcon, ScaleMode.StretchToFill, true, 0f);
				}
				
				GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), "", _style);
			}
		}
		

		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void RecalculateSize (MenuSource source)
		{
			if (Application.isPlaying)
			{
				if (linkedConversation != null)
				{
					numOptions = linkedConversation.GetCount ();
					
					if (fixedOption)
					{
						if (numOptions < optionToShow)
						{
							numSlots = 0;
						}
						else
						{
							numSlots = 1;
							labels = new string[numSlots];
							labels[0] = linkedConversation.GetOptionName (optionToShow - 1);
							labels[0] = AddIndexNumber (labels[0], optionToShow);
							
							icons = new CursorIconBase[numSlots];
							icons[0] = new CursorIconBase ();
							icons[0].Copy (linkedConversation.GetOptionIcon (optionToShow - 1));

							chosens = new bool[numSlots];
							chosens[0] = linkedConversation.OptionHasBeenChosen (optionToShow - 1);
						}
					}
					else
					{
						numSlots = numOptions;
						if (numSlots > maxSlots)
						{
							numSlots = maxSlots;
						}

						labels = new string[numSlots];
						icons = new CursorIconBase[numSlots];
						chosens = new bool[numSlots];
						for (int i=0; i<numSlots; i++)
						{
							labels[i] = linkedConversation.GetOptionName (i + offset);
							labels[i] = AddIndexNumber (labels[i], i + offset + 1);
							icons[i] = new CursorIconBase ();
							icons[i].Copy (linkedConversation.GetOptionIcon (i + offset));
							chosens[i] = linkedConversation.OptionHasBeenChosen (i + offset);
						}

						if (markAlreadyChosen && source != MenuSource.AdventureCreator)
						{
							for (int i=0; i<chosens.Length; i++)
							{
								bool chosen = chosens[i];

								if (uiSlots.Length > i)
								{
									if (chosen)
									{
										uiSlots[i].SetColour (alreadyChosenFontColour);
									}
									else
									{
										uiSlots[i].RestoreColour ();
									}
								}
							}
						}

						LimitOffset (numOptions);
					}
				}
				else
				{
					numSlots = 0;
				}
			}
			else if (fixedOption)
			{
				numSlots = 1;
				offset = 0;
				labels = new string[numSlots];
				icons = new CursorIconBase[numSlots];
				chosens = new bool[numSlots];
			}

			if (Application.isPlaying && uiSlots != null)
			{
				ClearSpriteCache (uiSlots);
			}

			if (!isVisible)
			{
				LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
			}

			base.RecalculateSize (source);
		}
		

		/**
		 * <summary>Shifts which slots are on display, if the number of slots the element has exceeds the number of slots it can show at once.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <param name = "amount">The amount to shift slots by</param>
		 */
		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (isVisible && numSlots >= maxSlots)
			{
				Shift (shiftType, maxSlots, numOptions, amount);
			}
		}
		

		/**
		 * <summary>Checks if the element's slots can be shifted in a particular direction.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <returns>True if the element's slots can be shifted in the particular direction</returns>
		 */
		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (numSlots == 0)
			{
				return false;
			}

			if (shiftType == AC_ShiftInventory.ShiftPrevious)
			{
				if (offset == 0)
				{
					return false;
				}
			}
			else
			{
				if ((maxSlots + offset) >= numOptions)
				{
					return false;
				}
			}
			return true;
		}


		public override void OnMenuTurnOn (Menu menu)
		{
			base.OnMenuTurnOn (menu);

			Conversation oldConversation = linkedConversation;
			linkedConversation = (overrideConversation != null) ? overrideConversation : KickStarter.playerInput.activeConversation;

			if (oldConversation != linkedConversation || resetOffsetWhenRestart)
			{
				offset = 0;
			}

			if (linkedConversation != null && !fixedOption)
			{
				LimitOffset (linkedConversation.GetCount ());
			}
		}
		

		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "slot">The index number of the slot</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			if (labels.Length > slot)
			{
				return labels[slot];
			}
			
			return "";
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiSlots != null && slotIndex >= 0 && uiSlots.Length > slotIndex && uiSlots[slotIndex] != null && uiSlots[slotIndex].uiButton != null)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiSlots[slotIndex].uiButton.gameObject);
			}
			return false;
		}
		

		/**
		 * <summary>Performs what should happen when the element is clicked on.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_slot">The index number of ths slot that was clicked</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState != GameState.DialogOptions)
			{
				return;
			}

			if (linkedConversation != null && 
				(linkedConversation == overrideConversation || (overrideConversation == null && KickStarter.playerInput.activeConversation)))
			{
				if (fixedOption)
				{
					linkedConversation.RunOption (optionToShow - 1);
				}
				else
				{
					linkedConversation.RunOption (_slot + offset);
				}
			}

			base.ProcessClick (_menu, _slot, _mouseState);
		}


		/**
		 * If set, then this Conversation will be used instead of the global 'active' one.  This must be set either before the Menu is turned on, or within the OnMenuTurnOn custom event.  Note that its Menu's 'Appear type' should not be set to 'During Conversation', and that the Conversation's dialogue options should not be overridden with the 'Dialogue: Start conversation' Action.
		 */
		public Conversation OverrideConversation
		{
			set
			{
				overrideConversation = value;
			}
		}

		
		protected override void AutoSize ()
		{
			if (displayType == ConversationDisplayType.IconOnly)
			{
				AutoSize (new GUIContent (testIcon));
			}
			else
			{
				AutoSize (new GUIContent ("Dialogue option 0"));
			}
		}


		/**
		 * <summary>Gets a ButtonDialog data-container class for a dialogue option within the active Conversation</summary>
		 * <param name = "slotIndex">The element's slot index number associated with the dialogue option</param>
		 * <returns>The ButtonDialog data-container class for a dialogue option within the active Conversation</returns>
		 */
		public ButtonDialog GetDialogueOption (int slotIndex)
		{
			slotIndex += offset;
			if (linkedConversation)
			{
				return linkedConversation.GetOption (slotIndex);
			}
			return null;
		}

	}
	
}
