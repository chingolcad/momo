/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MenuInput.cs"
 * 
 *	This MenuElement acts like a label, whose text can be changed with keyboard input.
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
	 * A MenuElement that provides an input box that the player can enter text into.
	 */
	public class MenuInput : MenuElement, ITranslatable
	{

		/** The Unity UI InputField this is linked to (Unity UI Menus only) */
		public InputField uiInput;
		/** The text that's displayed on-screen */
		public string label = "Element";
		/** The text alignment */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** What kind of characters can be entered in by the player (AlphaNumeric, NumericOnly, AllowSpecialCharacters) */
		public AC_InputType inputType;
		/** The character limit on text that can be entered */
		public int characterLimit = 10;
		/** The name of the MenuButton element that is synced with the 'Return' key when this element is active */
		public string linkedButton = "";
		/** If True, then spaces are recognised */
		public bool allowSpaces = false;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
		/** If True, then the element will need to be selected before it receives input */
		public bool requireSelection = false;

		private bool isSelected = false;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiInput = null;
			label = "Input";
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (10f, 5f));
			inputType = AC_InputType.AlphaNumeric;
			characterLimit = 10;
			linkedButton = "";
			textEffects = TextEffects.None;
			outlineSize = 2f;
			allowSpaces = false;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
			requireSelection = false;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuInput newElement = CreateInstance <MenuInput>();
			newElement.Declare ();
			newElement.CopyInput (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyInput (MenuInput _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiInput = null;
			}
			else
			{
				uiInput = _element.uiInput;
			}

			label = _element.label;
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			inputType = _element.inputType;
			characterLimit = _element.characterLimit;
			linkedButton = _element.linkedButton;
			allowSpaces = _element.allowSpaces;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;
			requireSelection = _element.requireSelection;

			base.Copy (_element);
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas)
		{
			uiInput = LinkUIElement <InputField> (canvas);
		}
		

		/**
		 * <summary>Gets the boundary of the element</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <returns>The boundary Rect of the element</returns>
		 */
		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiInput != null)
			{
				return uiInput.GetComponent <RectTransform>();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			if (uiInput != null)
			{
				uiInput.interactable = state;
			}
		}


		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (uiInput != null)
			{
				return uiInput.gameObject;
			}
			return null;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuInput)";

			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");
			if (source == MenuSource.AdventureCreator)
			{
				inputType = (AC_InputType) CustomGUILayout.EnumPopup ("Input type:", inputType, apiPrefix + ".inputType", "What kind of characters can be entered in by the player");
				label = EditorGUILayout.TextField ("Default text:", label);
				if (inputType == AC_InputType.AlphaNumeric)
				{
					allowSpaces = CustomGUILayout.Toggle ("Allow spaces?", allowSpaces, apiPrefix + ".allowSpace", "If True, then spaces are recognised");
				}
				characterLimit = CustomGUILayout.IntField ("Character limit:", characterLimit, apiPrefix + ".characterLimit", "The character limit on text that can be entered");

				#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_2018_3_OR_NEWER
				EditorGUILayout.HelpBox ("For the character limit to be obeyed on Android and iOS, Unity 2018.3 or later must be used.", MessageType.Info);
				#endif

				linkedButton = CustomGUILayout.TextField ("'Enter' key's linked Button:", linkedButton, apiPrefix + ".linkedPrefab", "The name of the MenuButton element that is synced with the 'Return' key when this element is active");
				requireSelection = CustomGUILayout.ToggleLeft ("Require selection to accept input?", requireSelection, apiPrefix + ".requireSelection", "If True, then the element will need to be selected before it receives input");
			}
			else
			{
				uiInput = LinkedUiGUI <InputField> (uiInput, "Linked InputField:", source);
				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
			}
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


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (uiInput != null && uiInput.gameObject == gameObject) return true;
			if (linkedUiID == id) return true;
			return false;
		}
		
		#endif
		

		/**
		 * <summary>Gets the contents of the text box.</summary>
		 * <returns>The contents of the text box.</returns>
		 */
		public string GetContents ()
		{
			if (uiInput != null)
			{
				if (uiInput.textComponent != null)
				{
					return uiInput.textComponent.text;
				}
				else
				{
					ACDebug.LogWarning (uiInput.gameObject.name + " has no Text component");
				}
			}

			return label;
		}


		/**
		 * <summary>Set the contents of the text box manually.</summary>
		 * <param name = "_label">The new label for the text box.</param>
		 */
		public void SetLabel (string _label)
		{
			label = _label;

			if (uiInput != null && uiInput.textComponent != null)
			{
				uiInput.text = _label;
			}
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (uiInput != null)
			{
				UpdateUISelectable (uiInput, uiSelectableHideStyle);
			}
		}


		/**
		 * <summary>Draws the element using OnGUI.</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			string fullText = label;
			if (Application.isPlaying && (isSelected || isActive))
			{
				fullText = AdvGame.CombineLanguageString (fullText, "|", Options.GetLanguage (), false);
			}

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
		}


		/**
		 * <summary>Gets the display text of the element.</summary>
		 * <param name = "slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			return TranslateLabel (label, languageNumber);
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiInput != null)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiInput.gameObject);
			}
			return false;
		}


		private void ProcessReturn (string input, string menuName)
		{
			if (input == "KeypadEnter" || input == "Return" || input == "Enter")
			{
				if (linkedButton != "" && menuName != "")
				{
					PlayerMenus.SimulateClick (menuName, PlayerMenus.GetElementWithName (menuName, linkedButton), 1);
				}
			}
		}


		/**
		 * <summary>Processes input entered by the player, and applies it to the text box (OnGUI-based Menus only).</summary>
		 * <param name = "keycode">The keycode of the Event that recorded input</param>
		 * <param name = "character">The character of the Event that recorded input</param>
		 * <param name = "shift">If True, shift was held down</param>
		 * <param name = "menuName">The name of the Menu that stores this element</param>
		 */
		public void CheckForInput (string keycode, string character, bool shift, string menuName)
		{
			if (uiInput != null)
			{
				return;
			}

			string input = keycode;

			if (inputType == AC_InputType.AllowSpecialCharacters)
			{
				if (!(input == "KeypadEnter" || input == "Return" || input == "Enter" || input == "Backspace"))
				{
					input = character;
				}
			}

			bool rightToLeft = KickStarter.runtimeLanguages.LanguageReadsRightToLeft (Options.GetLanguage ());

			isSelected = true;
			if (input == "Backspace")
			{
				if (label.Length > 1)
				{
					if (rightToLeft)
					{
						label = label.Substring (1, label.Length - 1);
					}
					else
					{
						label = label.Substring (0, label.Length - 1);
					}
				}
				else if (label.Length == 1)
				{
					label = "";
				}
			}
			else if (input == "KeypadEnter" || input == "Return" || input == "Enter")
			{
				ProcessReturn (input, menuName);
			}
			else if ((inputType == AC_InputType.AlphaNumeric && (input.Length == 1 || input.Contains ("Alpha"))) ||
			         (inputType == AC_InputType.NumbericOnly && input.Contains ("Alpha")) ||
			         (inputType == AC_InputType.AlphaNumeric && allowSpaces && input == "Space") ||
			         (inputType == AC_InputType.AllowSpecialCharacters && (input.Length == 1 || input == "Space")))
			{
				input = input.Replace ("Alpha", "");
				input = input.Replace ("Space", " ");

				if (inputType != AC_InputType.AllowSpecialCharacters)
				{
					if (shift)
					{
						input = input.ToUpper ();
					}
					else
					{
						input = input.ToLower ();
					}
				}

				if (characterLimit == 1)
				{
					label = input;
				}
				else if (label.Length < characterLimit)
				{
					if (rightToLeft)
					{
						label = input + label;
					}
					else
					{
						label += input;
					}
				}
			}
		}


		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void RecalculateSize (MenuSource source)
		{
			if (source == MenuSource.AdventureCreator)
			{
				Deselect ();
			}

			base.RecalculateSize (source);
		}


		/**
		 * De-selects the text box (OnGUI-based Menus only).
		 */
		public void Deselect ()
		{
			isSelected = false;
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

			KickStarter.playerMenus.SelectInputBox (this);
			base.ProcessClick (_menu, _slot, _mouseState);
		}

		
		protected override void AutoSize ()
		{
			GUIContent content = new GUIContent (TranslateLabel (label, Options.GetLanguage ()));
			AutoSize (content);
		}


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			return label;
		}


		public int GetTranslationID (int index)
		{
			return lineID;
		}


		#if UNITY_EDITOR

		public int GetNumTranslatables ()
		{
			return 1;
		}


		public bool HasExistingTranslation (int index)
		{
			return (lineID > -1);
		}


		public void SetTranslationID (int index, int _lineID)
		{
			lineID = _lineID;
		}


		public string GetOwner (int index)
		{
			return title;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.MenuElement;
		}


		public bool CanTranslate (int index)
		{
			return !string.IsNullOrEmpty (label);
		}

		#endif

		#endregion

	}

}