/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MenuButton.cs"
 * 
 *	This MenuElement can be clicked on to perform a specified function.
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
	 * A MenuElement that can be clicked on to perform a specific function.
	 */
	[System.Serializable]
	public class MenuButton : MenuElement, ITranslatable
	{

		/** The Unity UI Button this is linked to (Unity UI Menus only) */
		public UnityEngine.UI.Button uiButton;
		/** What pointer state registers as a 'click' for Unity UI Menus (PointerClick, PointerDown, PointerEnter) */
		public UIPointerState uiPointerState = UIPointerState.PointerClick;

		/** The text that's displayed on-screen */
		public string label = "Element";
		/** The text that appears in the Hotspot label buffer when the mouse hovers over */
		public string hotspotLabel = "";
		/** The translation ID of the text that appears in the Hotspot label buffer when the mouse hovers over, as set in SpeechManager */
		public int hotspotLabelID = -1;	

		/** The text alignment */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The type of reaction that occurs when clicked (TurnOffMenu, Crossfade, OffsetElementSlot, RunActionList, CustomScript, OffsetJournal, SimulateInput) */
		public AC_ButtonClickType buttonClickType;

		/** The ActionListAsset to run when clicked, if buttonClickType = AC_ButtonClickType.RunActionList */
		public ActionListAsset actionList;
		/** The ID of the integer ActionParameter that can be set within actionList, if buttonClickType = AC_ButtonClickType.RunActionList */
		public int parameterID = -1;
		/** The value to set the integer ActionParameter within actionList, if buttonClickType = AC_ButtonClickType.RunActionList */
		public int parameterValue = 0;

		/** If True, and buttonClickType = AC_ButtonClickType.TurnOffMenu, then the Menu will transition as it turns off */
		public bool doFade;
		/** The name of the Menu to crossfade to, if buttonClickType = AC_ButtonClickType.Crossfade */
		public string switchMenuTitle;
		/** The name of the MenuElement with slots to shift, if buttonClickType = AC_ButtonClickType.OffsetElementSlot */
		public string inventoryBoxTitle;
		/** The direction to shift slots, if buttonClickType = AC_ButtonClickType.OffsetElementSlot (Left, Right) */
		public AC_ShiftInventory shiftInventory;
		/** The amount to shift slots by, if buttonClickType = AC_ButtonClickType.OffsetElementSlot */
		public int shiftAmount = 1;
		/** If True, and buttonClickType = AC_ButtyonClickType.OffsetElementSlot, then the button will only be visible if the slots it affects can actually be shifted */
		public bool onlyShowWhenEffective;
		/** If True, and buttonClickType = AC_ButtonClickType.OffsetJournal then shifting past the last journal page will open the first */
		public bool loopJournal = false;
		/** The name of the Input to simulate when clicked, if buttonClickType = AC_ButtonClickType.SimulateInput */
		public string inputAxis;
		/** The type of Input to simulate when clicked, if buttonClickType = AC_ButtonClickType.SimulateInput */
		public SimulateInputType simulateInput = SimulateInputType.Button;
		/** The value of the Input to simulate when clicked, if buttonClickType = AC_ButtonClickType.SimulateInput */
		public float simulateValue = 1f;

		/** The texture to overlay when the button is clicked on */
		public Texture2D clickTexture;
		/** If True, then the button will respond to the mouse button being held down */
		public bool allowContinuousClick = false;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

		private MenuElement elementToShift;
		private float clickAlpha = 0f;
		private string fullText;
		private bool disabledUI = false;

		#if TextMeshProIsPresent
		private TMPro.TextMeshProUGUI uiText;
		#else
		private Text uiText;
		#endif


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiText = null;
			uiButton = null;
			uiPointerState = UIPointerState.PointerClick;
			label = "Button";
			hotspotLabel = "";
			hotspotLabelID = -1;
			isVisible = true;
			isClickable = true;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			buttonClickType = AC_ButtonClickType.RunActionList;
			simulateInput = SimulateInputType.Button;
			simulateValue = 1f;
			numSlots = 1;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (10f, 5f));
			doFade = false;
			switchMenuTitle = "";
			inventoryBoxTitle = "";
			shiftInventory = AC_ShiftInventory.ShiftPrevious;
			loopJournal = false;
			actionList = null;
			inputAxis = "";
			clickTexture = null;
			clickAlpha = 0f;
			shiftAmount = 1;
			onlyShowWhenEffective = false;
			allowContinuousClick = false;
			parameterID = -1;
			parameterValue = 0;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuButton newElement = CreateInstance <MenuButton>();
			newElement.Declare ();
			newElement.CopyButton (this, ignoreUnityUI);
			return newElement;
		}
		

		private void CopyButton (MenuButton _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiButton = null;
				uiText = null;
			}
			else
			{
				uiButton = _element.uiButton;
				uiText = _element.uiText;
			}
			uiPointerState = _element.uiPointerState;

			label = _element.label;
			hotspotLabel = _element.hotspotLabel;
			hotspotLabelID = _element.hotspotLabelID;
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			buttonClickType = _element.buttonClickType;
			simulateInput = _element.simulateInput;
			simulateValue = _element.simulateValue;
			doFade = _element.doFade;
			switchMenuTitle = _element.switchMenuTitle;
			inventoryBoxTitle = _element.inventoryBoxTitle;
			shiftInventory = _element.shiftInventory;
			loopJournal = _element.loopJournal;
			actionList = _element.actionList;
			inputAxis = _element.inputAxis;
			clickTexture = _element.clickTexture;
			clickAlpha = _element.clickAlpha;
			shiftAmount = _element.shiftAmount;
			onlyShowWhenEffective = _element.onlyShowWhenEffective;
			allowContinuousClick = _element.allowContinuousClick;
			parameterID = _element.parameterID;
			parameterValue = _element.parameterValue;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;

			base.Copy (_element);
		}


		public override void Initialise (AC.Menu _menu)
		{
			if (buttonClickType == AC_ButtonClickType.OffsetElementSlot || buttonClickType == AC_ButtonClickType.OffsetJournal)
			{
				elementToShift = _menu.GetElementWithName (inventoryBoxTitle);
			}

			base.Initialise (_menu);
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
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
		 * <summary>Gets the boundary of the element.</summary>
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
				disabledUI = !state;

				// Don't make interactable if dependent on others
				if (state && buttonClickType == AC_ButtonClickType.OffsetElementSlot || buttonClickType == AC_ButtonClickType.OffsetJournal)
				{
					if (onlyShowWhenEffective && uiSelectableHideStyle == UISelectableHideStyle.DisableInteractability && Application.isPlaying && elementToShift != null)
					{
						if (buttonClickType == AC_ButtonClickType.OffsetElementSlot || !loopJournal)
						{
							if (!elementToShift.CanBeShifted (shiftInventory))
							{
								return;
							}
						}
					}
				}
				uiButton.interactable = state;
			}
		}


		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuButton)";

			EditorGUILayout.BeginVertical ("Button");
			MenuSource source = menu.menuSource;

			if (source != MenuSource.AdventureCreator)
			{
				uiButton = LinkedUiGUI <UnityEngine.UI.Button> (uiButton, "Linked Button:", source, "The Unity UI Button this is linked to");
				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
				uiPointerState = (UIPointerState) CustomGUILayout.EnumPopup ("Responds to:", uiPointerState, apiPrefix + ".uiPointerState", "What pointer state registers as a 'click' for Unity UI Menus");
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
			}

			label = CustomGUILayout.TextField ("Button text:", label, apiPrefix + ".label", "The text that's displayed on-screen");
			buttonClickType = (AC_ButtonClickType) CustomGUILayout.EnumPopup ("Click type:", buttonClickType, apiPrefix + ".buttonClickType", "The type of reaction that occurs when clicked");

			if (buttonClickType == AC_ButtonClickType.TurnOffMenu)
			{
				doFade = CustomGUILayout.Toggle ("Do transition?", doFade, apiPrefix + ".doFade", "If True, then the menu will transition as it turns off");
			}
			else if (buttonClickType == AC_ButtonClickType.Crossfade)
			{
				switchMenuTitle = CustomGUILayout.TextField ("Menu to switch to:", switchMenuTitle, apiPrefix + ".switchMenutitle", "The name of the menu to crossfade to");
			}
			else if (buttonClickType == AC_ButtonClickType.OffsetElementSlot)
			{
				inventoryBoxTitle = CustomGUILayout.TextField ("Element to affect:", inventoryBoxTitle, apiPrefix + ".inventoryBoxTitle", "The name of the element (in the same menu) with slots to shift");
				shiftInventory = (AC_ShiftInventory) CustomGUILayout.EnumPopup ("Offset type:", shiftInventory, apiPrefix + ".shiftInventory", "The direction to shift slots");
				shiftAmount = CustomGUILayout.IntField ("Offset amount:", shiftAmount, apiPrefix + ".shiftAmount", "The amount to shift slots by");
				onlyShowWhenEffective = CustomGUILayout.Toggle ("Only show when effective?", onlyShowWhenEffective, apiPrefix + ".onlyShowWhenEffective", "If True, then the button will only be visible if the slots it affects can actually be shifted");
			}
			else if (buttonClickType == AC_ButtonClickType.OffsetJournal)
			{
				inventoryBoxTitle = CustomGUILayout.TextField ("Journal to affect:", inventoryBoxTitle, apiPrefix + ".inventoryBoxTitle", "The name of the Journal element (in the same menu)");
				shiftInventory = (AC_ShiftInventory) CustomGUILayout.EnumPopup ("Offset type:", shiftInventory, apiPrefix + ".shiftInventory", "The direction to shift pages in");
				loopJournal = CustomGUILayout.Toggle ("Cycle pages?", loopJournal, apiPrefix + ".loopJournal", "If True, then shifting past the last Journal page will open the first");
				shiftAmount = CustomGUILayout.IntField ("Offset amount:", shiftAmount, apiPrefix + ".shiftAmount", "The number of pages to shift by");
				onlyShowWhenEffective = CustomGUILayout.Toggle ("Only show when effective?", onlyShowWhenEffective, apiPrefix + ".onlyShowWhenEffective", "If True, then the button will only be visible if the Journal it affects can actually be shifted");
			}
			else if (buttonClickType == AC_ButtonClickType.RunActionList)
			{
				ActionListGUI (menu.title, apiPrefix);
			}
			else if (buttonClickType == AC_ButtonClickType.CustomScript)
			{
				allowContinuousClick = CustomGUILayout.Toggle ("Accept held-down clicks?", allowContinuousClick, apiPrefix + ".allowContinuousClick", "If True, then the button will respond to the mouse button being held down");
				ShowClipHelp ();
			}
			else if (buttonClickType == AC_ButtonClickType.SimulateInput)
			{
				simulateInput = (SimulateInputType) CustomGUILayout.EnumPopup ("Simulate:", simulateInput, apiPrefix + ".simulateInput", "The type of Input to simulate when clicked");
				inputAxis = CustomGUILayout.TextField ("Input axis:", inputAxis, apiPrefix + ".inputAxis", "The name of the Input axis to simulate when clicked");
				if (simulateInput == SimulateInputType.Axis)
				{
					simulateValue = CustomGUILayout.FloatField ("Input value:", simulateValue, apiPrefix + ".simulateValue", "The value of the Input axis to simulate when clicked");
				}
			}

			hotspotLabel = CustomGUILayout.TextField ("Hotspot label override:", hotspotLabel, apiPrefix + ".hotspotLabel", "The text that appears in the Hotspot label buffer when the mouse hovers over");
			alternativeInputButton = CustomGUILayout.TextField ("Alternative input button:", alternativeInputButton, apiPrefix + ".alternativeInputButton", "The name of the input button that triggers the element when pressed");
			ChangeCursorGUI (menu);
			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
			textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");
			if (textEffects != TextEffects.None)
			{
				outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The outline thickness");
			}
		}


		protected override void ShowTextureGUI (string apiPrefix)
		{
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("Click texture:", "The texture to overlay when the button is clicked on"), GUILayout.Width (145f));
			clickTexture = (Texture2D) EditorGUILayout.ObjectField (clickTexture, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
			EditorGUILayout.EndHorizontal ();
		}


		private void ActionListGUI (string menuTitle, string apiPrefix)
		{
			actionList = ActionListAssetMenu.AssetGUI ("ActionList to run:", actionList, menuTitle + "_" + title + "_OnClick", apiPrefix + ".actionList", "The ActionList asset to run when clicked");
			if (actionList != null && actionList.NumParameters > 0)
			{
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.BeginHorizontal ();
				bool hasValid = false;
				parameterID = Action.ChooseParameterGUI (string.Empty, actionList.DefaultParameters, parameterID, ParameterType.Integer);
				if (parameterID >= 0)
				{
					parameterValue = EditorGUILayout.IntField (parameterValue);
					hasValid = true;
				}
				EditorGUILayout.EndHorizontal ();
				if (!hasValid)
				{
					EditorGUILayout.HelpBox ("Only Integer parameters can be passed to a MenuButton's ActionList", MessageType.Info);
				}
				EditorGUILayout.EndVertical ();
			}
		}


		public override bool CheckConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID)
		{
			string newLabel = AdvGame.ConvertGlobalVariableTokenToLocal (label, oldGlobalID, newLocalID);
			return (label != newLabel);
		}


		public override int GetVariableReferences (int varID)
		{
			int numFound = 0;
			string tokenText = "[var:" + varID.ToString () + "]";
			if (label.Contains (tokenText))
			{
				numFound ++;
			}
			return numFound + base.GetVariableReferences (varID);
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (uiButton != null && uiButton.gameObject == gameObject) return true;
			if (linkedUiID == id) return true;
			return false;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (buttonClickType == AC_ButtonClickType.RunActionList && actionList == actionListAsset)
				return true;
			return false;
		}

		#endif


		/**
		 * Shows the assigned clickTexture overlay, which fades out over time.
		 */
		public void ShowClick ()
		{
			if (isClickable)
			{
				clickAlpha = 1f;
			}
		}


		public override string GetHotspotLabelOverride (int _slot, int _language)
		{
			if (uiButton != null && !uiButton.interactable) return string.Empty;

			return GetHotspotLabel (_language);
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			SetEffectiveVisibility (true);

			fullText = TranslateLabel (label, languageNumber);
			fullText = AdvGame.ConvertTokens (fullText, languageNumber);

			if (uiButton != null)
			{
				if (uiSelectableHideStyle == UISelectableHideStyle.DisableInteractability && disabledUI)
				{
					// Ignore in this special case, since we're already disabling the Menu UI
				}
				else
				{
					UpdateUISelectable (uiButton, uiSelectableHideStyle);
				}

				if (uiText != null)
				{
					uiText.text = fullText;
				}
			}
		}
		

		/**
		 * <summary>Draws the element using OnGUI</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
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
			
			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), fullText, _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (relativeRect, zoom), fullText, _style);
			}

			if (clickAlpha > 0f)
			{
				if (clickTexture)
				{
					Color tempColor = GUI.color;
					tempColor.a = clickAlpha;
					GUI.color = tempColor;
					GUI.DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), clickTexture, ScaleMode.StretchToFill, true, 0f);
					tempColor.a = 1f;
					GUI.color = tempColor;
				}
				clickAlpha -= (KickStarter.stateHandler.gameState == GameState.Paused) ? 0.02f : Time.deltaTime;
				if (clickAlpha < 0f)
				{
					clickAlpha = 0f;
				}
			}
		}


		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			return TranslateLabel (label, languageNumber);
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiButton != null)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiButton.gameObject);
			}
			return false;
		}

		
		protected override void AutoSize ()
		{
			if (string.IsNullOrEmpty (label) && backgroundTexture != null)
			{
				GUIContent content = new GUIContent (backgroundTexture);
				AutoSize (content);
			}
			else
			{
				GUIContent content = new GUIContent (TranslateLabel (label, Options.GetLanguage ()));
				AutoSize (content);
			}
		}


		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void RecalculateSize (MenuSource source)
		{
			SetEffectiveVisibility (false);

			clickAlpha = 0f;
			base.RecalculateSize (source);
		}


		private void SetEffectiveVisibility (bool fromPreDisplay)
		{
			if (buttonClickType == AC_ButtonClickType.OffsetElementSlot || buttonClickType == AC_ButtonClickType.OffsetJournal)
			{
				if (onlyShowWhenEffective && Application.isPlaying && elementToShift != null)
				{
					if (buttonClickType == AC_ButtonClickType.OffsetElementSlot || !loopJournal)
					{
						bool newVisibleValue = elementToShift.CanBeShifted (shiftInventory);
						if (newVisibleValue != isVisible)
						{
							IsVisible = newVisibleValue;

							if (fromPreDisplay)
							{
								parentMenu.Recalculate ();
							}
						}
					}
				}
			}
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (!_menu.IsClickable ())
			{
				return;
			}

			ShowClick ();

			if (buttonClickType == AC_ButtonClickType.TurnOffMenu)
			{
				_menu.TurnOff (doFade);
			}
			else if (buttonClickType == AC_ButtonClickType.Crossfade)
			{
				AC.Menu menuToSwitchTo = PlayerMenus.GetMenuWithName (switchMenuTitle);
				
				if (menuToSwitchTo != null)
				{
					KickStarter.playerMenus.CrossFade (menuToSwitchTo);
				}
				else
				{
					ACDebug.LogWarning ("Cannot find any menu of name '" + switchMenuTitle + "'");
				}
			}
			else if (buttonClickType == AC_ButtonClickType.OffsetElementSlot)
			{
				if (elementToShift != null)
				{
					elementToShift.Shift (shiftInventory, shiftAmount);
					elementToShift.RecalculateSize (_menu.menuSource);
					_menu.Recalculate ();
				}
				else
				{
					ACDebug.LogWarning ("Cannot find '" + inventoryBoxTitle + "' inside '" + _menu.title + "'");
				}
			}
			else if (buttonClickType == AC_ButtonClickType.OffsetJournal)
			{
				MenuJournal journalToShift = (MenuJournal) PlayerMenus.GetElementWithName (_menu.title, inventoryBoxTitle);
				
				if (journalToShift != null)
				{
					journalToShift.Shift (shiftInventory, loopJournal, shiftAmount);
					journalToShift.RecalculateSize (_menu.menuSource);
					_menu.Recalculate ();
				}
				else
				{
					ACDebug.LogWarning ("Cannot find '" + inventoryBoxTitle + "' inside '" + _menu.title + "'");
				}
			}
			else if (buttonClickType == AC_ButtonClickType.RunActionList)
			{
				if (actionList)
				{
					if (!actionList.canRunMultipleInstances)
					{
						KickStarter.actionListAssetManager.EndAssetList (actionList);
					}

					AdvGame.RunActionListAsset (actionList, parameterID, parameterValue);
				}
			}
			else if (buttonClickType == AC_ButtonClickType.CustomScript)
			{
				MenuSystem.OnElementClick (_menu, this, _slot, (int) _mouseState);
			}
			else if (buttonClickType == AC_ButtonClickType.SimulateInput)
			{
				KickStarter.playerInput.SimulateInput (simulateInput, inputAxis, simulateValue);
			}
			
			base.ProcessClick (_menu, _slot, _mouseState);
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on continuously.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 */
		public override void ProcessContinuousClick (AC.Menu _menu, MouseState _mouseState)
		{
			if (buttonClickType == AC_ButtonClickType.SimulateInput)
			{
				if (uiButton != null && uiPointerState == UIPointerState.PointerClick)
				{
					// Not applicable here
					return;
				}
				KickStarter.playerInput.SimulateInput (simulateInput, inputAxis, simulateValue);
			}
			else if (buttonClickType == AC_ButtonClickType.CustomScript && allowContinuousClick)
			{
				MenuSystem.OnElementClick (_menu, this, 0, (int) _mouseState);
			}
		}


		/**
		 * <summary>Gets the text to place in the Hotspot label buffer (in PlayerMenus) when the mouse hovers over the element.</summary>
		 * <param name = "languageNumber">The index of the language to display the text in</param>
		 * <returns>The text to place in the Hotspot label buffer when the mouse hovers over the element</returns>
		 */
		public string GetHotspotLabel (int languageNumber)
		{
			if (languageNumber > 0)
			{
				return KickStarter.runtimeLanguages.GetTranslation (hotspotLabel, hotspotLabelID, languageNumber, GetTranslationType (0));
			}
			return hotspotLabel;
		}


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			if (index == 0)
			{
				return label;
			}
			else
			{
				return hotspotLabel;
			}
		}

		
		public int GetTranslationID (int index)
		{
			if (index == 0)
			{
				return lineID;
			}
			else
			{
				return hotspotLabelID;
			}
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.MenuElement;
		}


		#if UNITY_EDITOR

		public int GetNumTranslatables ()
		{
			return 2;
		}


		public bool HasExistingTranslation (int index)
		{
			if (index == 0)
			{
				return (lineID > -1);
			}
			else
			{
				return (hotspotLabelID > -1);
			}
		}


		public void SetTranslationID (int index, int _lineID)
		{
			if (index == 0)
			{
				lineID = _lineID;
			}
			else
			{
				hotspotLabelID = _lineID;
			}
		}


		public string GetOwner (int index)
		{
			return title;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public bool CanTranslate (int index)
		{
			if (index == 0)
			{
				return !string.IsNullOrEmpty (label);
			}
			else
			{
				return !string.IsNullOrEmpty (hotspotLabel);
			}
		}

		#endif

		#endregion

	}

}