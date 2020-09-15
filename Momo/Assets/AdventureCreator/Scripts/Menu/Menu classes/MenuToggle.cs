/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MenuToggle.cs"
 * 
 *	This MenuElement toggles between On and Off when clicked on.
 *	It can be used for changing boolean options.
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
	 * A MenuElement that provides an "on/off" toggle button.
	 * It can be used to change the value of a Boolean global variable, or the display of subtitles in Options.
	 */
	public class MenuToggle : MenuElement, ITranslatable
	{

		/** The Unity UI Toggle this is linked to (Unity UI Menus only) */
		public Toggle uiToggle;
		/** What the value of the toggle represents (Subtitles, Variable, CustomScript) */
		public AC_ToggleType toggleType;
		/** An ActionListAsset that will run when the element is clicked on */
		public ActionListAsset actionListOnClick = null;
		/** The text that's displayed on-screen */
		public string label;
		/** If True, then the toggle will be in its "on" state by default */
		public bool isOn;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The text alignment */
		public TextAnchor anchor;
		/** The ID number of the Boolean global variable to link to, if toggleType = AC_ToggleType.Variable */
		public int varID;
		/** If True, then the state ("On"/"Off") will be added to the display label */
		public bool appendState = true;
		/** The background texture when in the "on" state (OnGUI Menus only) */
		public Texture2D onTexture = null;
		/** The background texture when in the "off" state (OnGUI Menus only) */
		public Texture2D offTexture = null;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

		/** The text suffix when the toggle is 'on' */
		public string onText = "On";
		/** The translation ID of the 'off' text, as set within SpeechManager */
		public int onTextLineID = -1;
		/** The text suffix when the toggle is 'off' */
		public string offText = "Off";
		/** The translation ID of the 'off' text, as set within SpeechManager */
		public int offTextLineID = -1;

		private Text uiText;
		private string fullText;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiToggle = null;
			uiText = null;
			label = "Toggle";
			isOn = false;
			isVisible = true;
			isClickable = true;
			toggleType = AC_ToggleType.CustomScript;
			numSlots = 1;
			varID = 0;
			SetSize (new Vector2 (15f, 5f));
			anchor = TextAnchor.MiddleLeft;
			appendState = true;
			onTexture = null;
			offTexture = null;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			actionListOnClick = null;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
			onText = "On";
			offText = "Off";
			onTextLineID = -1;
			offTextLineID = -1;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuToggle newElement = CreateInstance <MenuToggle>();
			newElement.Declare ();
			newElement.CopyToggle (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyToggle (MenuToggle _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiToggle = null;
			}
			else
			{
				uiToggle = _element.uiToggle;
			}

			uiText = null;
			label = _element.label;
			isOn = _element.isOn;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			anchor = _element.anchor;
			toggleType = _element.toggleType;
			varID = _element.varID;
			appendState = _element.appendState;
			onTexture = _element.onTexture;
			offTexture = _element.offTexture;
			actionListOnClick = _element.actionListOnClick;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;
			onText = _element.onText;
			offText = _element.offText;
			onTextLineID = _element.onTextLineID;
			offTextLineID = _element.offTextLineID;
			isClickable = _element.isClickable;

			base.Copy (_element);
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas)
		{
			uiToggle = LinkUIElement <Toggle> (canvas);
			if (uiToggle)
			{
				uiText = uiToggle.GetComponentInChildren <Text>();

				uiToggle.interactable = isClickable;
				if (isClickable)
				{
					uiToggle.onValueChanged.AddListener ((isOn) => {
					ProcessClickUI (_menu, 0, KickStarter.playerInput.GetMouseState ());
					});
				}
			}
		}


		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (uiToggle)
			{
				return uiToggle.gameObject;
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
			if (uiToggle)
			{
				return uiToggle.GetComponent <RectTransform>();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			if (uiToggle)
			{
				uiToggle.interactable = state;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuToggle)";

			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");

			if (source != MenuSource.AdventureCreator)
			{
				uiToggle = LinkedUiGUI <Toggle> (uiToggle, "Linked Toggle:", source, "The Unity UI Toggle this is linked to");
				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
			}

			label = CustomGUILayout.TextField ("Label text:", label, apiPrefix + ".label", "The text that's displayed on-screen");
			appendState = CustomGUILayout.Toggle ("Append state to label?", appendState, apiPrefix + ".appendState", "If True, then the state (On/Off) will be added to the display label");
			if (appendState)
			{
				onText = CustomGUILayout.TextField ("'On' state text:", onText, apiPrefix + ".onText", "The text suffix when the toggle is 'on'");
				offText = CustomGUILayout.TextField ("'Off' state text:", offText, apiPrefix + ".offText", "The text suffix when the toggle is 'off'");
			}

			if (source == MenuSource.AdventureCreator)
			{
				anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
				textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");
				if (textEffects != TextEffects.None)
				{
					outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The outline thickness");
				}
			
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("'On' texture:", "The background texture when in the 'on' state"), GUILayout.Width (145f));
				onTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (onTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".onTexture");
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("'Off' texture:", "The background texture when in the 'off' state"), GUILayout.Width (145f));
				offTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (offTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".offTexture");
				EditorGUILayout.EndHorizontal ();
			}

			toggleType = (AC_ToggleType) CustomGUILayout.EnumPopup ("Toggle type:", toggleType, apiPrefix + ".toggleType", "What the value of the toggle represents");
			if (toggleType == AC_ToggleType.CustomScript)
			{
				isOn = CustomGUILayout.Toggle ("On by default?", isOn, apiPrefix + ".isOn", "If True, then the toggle will be in its 'on' state by default");
				ShowClipHelp ();
			}
			else if (toggleType == AC_ToggleType.Variable)
			{
				varID = AdvGame.GlobalVariableGUI ("Global boolean var:", varID, VariableType.Boolean, "The global Boolean variable whose value is linked to the Toggle");
			}

			isClickable = CustomGUILayout.Toggle ("User can change value?", isClickable, apiPrefix + ".isClickable", "If True, the slider is interactive and can be modified by the user");
			if (isClickable)
			{
				if (toggleType != AC_ToggleType.Subtitles)
				{
					actionListOnClick = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("ActionList on click:", actionListOnClick, false, apiPrefix + ".actionListOnClick", "An ActionList asset that will run when the element is clicked on");
				}
				alternativeInputButton = CustomGUILayout.TextField ("Alternative input button:", alternativeInputButton, apiPrefix + ".alternativeInputButton", "The name of the input button that triggers the element when pressed");
			}
			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		public override bool CheckConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID)
		{
			if (toggleType == AC_ToggleType.Variable && varID == oldGlobalID)
			{
				return true;
			}
			return false;
		}


		public override int GetVariableReferences (int _varID)
		{
			int numFound = 0;

			string tokenText = "[var:" + _varID.ToString () + "]";
			if (label.Contains (tokenText))
			{
				numFound ++;
			}

			if (toggleType == AC_ToggleType.Variable && varID == _varID)
			{
				numFound ++;
			}

			return numFound + base.GetVariableReferences (_varID);
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (uiToggle != null && uiToggle.gameObject == gameObject) return true;
			if (linkedUiID == id) return true;
			return false;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (isClickable && toggleType != AC_ToggleType.Subtitles && actionListOnClick == actionListAsset)
				return true;
			return false;
		}

		#endif


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			CalculateValue ();

			fullText = TranslateLabel (label, languageNumber);
			if (appendState)
			{
				if (languageNumber == 0)
				{
					if (isOn)
					{
						fullText += " : " + onText;
					}
					else
					{
						fullText += " : " + offText;
					}
				}
				else
				{
					if (isOn)
					{
						fullText += " : " + KickStarter.runtimeLanguages.GetTranslation (onText, onTextLineID, languageNumber, GetTranslationType (0));
					}
					else
					{
						fullText += " : " + KickStarter.runtimeLanguages.GetTranslation (offText, offTextLineID, languageNumber, GetTranslationType (0));
					}
				}
			}

			if (uiToggle)
			{
				if (uiText)
				{
					uiText.text = fullText;
				}
				uiToggle.isOn = isOn;
				UpdateUISelectable (uiToggle, uiSelectableHideStyle);
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
			
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}
			
			Rect rect = ZoomRect (relativeRect, zoom);
			if (isOn && onTexture != null)
			{
				GUI.DrawTexture (rect, onTexture, ScaleMode.StretchToFill, true, 0f);
			}
			else if (!isOn && offTexture != null)
			{
				GUI.DrawTexture (rect, offTexture, ScaleMode.StretchToFill, true, 0f);
			}
			
			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (rect, fullText, _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (rect, fullText, _style);
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
			if (appendState)
			{
				if (isOn)
				{
					return TranslateLabel (label, languageNumber) + " : " + KickStarter.runtimeLanguages.GetTranslation (onText, onTextLineID, languageNumber, GetTranslationType (0));
				}
				
				return TranslateLabel (label, languageNumber) + " : " + KickStarter.runtimeLanguages.GetTranslation (offText, offTextLineID, languageNumber, GetTranslationType (0));
			}
			return TranslateLabel (label, languageNumber);
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiToggle != null)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiToggle.gameObject);
			}
			return false;
		}
		

		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (!_menu.IsClickable ())
			{
				return;
			}

			if (uiToggle != null)
			{
				isOn = uiToggle.isOn;
			}
			else
			{
				if (isOn)
				{
					isOn = false;
				}
				else
				{
					isOn = true;
				}
			}

			if (toggleType == AC_ToggleType.Subtitles)
			{
				Options.SetSubtitles (isOn);
			}
			else if (toggleType == AC_ToggleType.Variable)
			{
				if (varID >= 0)
				{
					GVar var = GlobalVariables.GetVariable (varID);
					if (var.type == VariableType.Boolean)
					{
						if (isOn)
						{
							var.val = 1;
						}
						else
						{
							var.val = 0;
						}
						var.Upload (VariableLocation.Global);
					}
				}
			}
			
			if (toggleType == AC_ToggleType.CustomScript)
			{
				MenuSystem.OnElementClick (_menu, this, _slot, (int) _mouseState);
			}

			if (actionListOnClick)
			{
				AdvGame.RunActionListAsset (actionListOnClick);
			}

			base.ProcessClick (_menu, _slot, _mouseState);
		}


		private void CalculateValue ()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (toggleType == AC_ToggleType.Subtitles)
			{	
				if (Options.optionsData != null)
				{
					isOn = Options.optionsData.showSubtitles;
				}
			}
			else if (toggleType == AC_ToggleType.Variable)
			{
				if (varID >= 0)
				{
					GVar var = GlobalVariables.GetVariable (varID);
					if (var != null && var.type == VariableType.Boolean)
					{
						if (var.val == 1)
						{
							isOn = true;
						}
						else
						{
							isOn = false;
						}
					}
					else
					{
						ACDebug.LogWarning ("Cannot link MenuToggle " + title + " to Variable " + varID + " as it is not a Boolean.");
					}
				}
			}
		}

		
		protected override void AutoSize ()
		{
			int languageNumber = Options.GetLanguage ();
			if (appendState)
			{
				AutoSize (new GUIContent (TranslateLabel (label, languageNumber) + " : Off"));
			}
			else
			{
				AutoSize (new GUIContent (TranslateLabel (label, languageNumber)));
			}
		}


		#region ITranslatable
		
		public string GetTranslatableString (int index)
		{
			if (index == 0)
			{
				return label;
			}
			else if (index == 1)
			{
				return onText;
			}
			else
			{
				return offText;
			}
		}
		

		public int GetTranslationID (int index)
		{
			if (index == 0)
			{
				return lineID;
			}
			else if (index == 1)
			{
				return onTextLineID;
			}
			else
			{
				return offTextLineID;
			}
		}

		
		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.MenuElement;
		}



		#if UNITY_EDITOR

		public int GetNumTranslatables ()
		{
			return 3;
		}


		public bool HasExistingTranslation (int index)
		{
			if (index == 0)
			{
				return (lineID > -1);
			}
			else if (index == 1)
			{
				return (onTextLineID > -1);
			}
			else
			{
				return (offTextLineID > -1);
			}
		}


		public void SetTranslationID (int index, int _lineID)
		{
			if (index == 0)
			{
				lineID = _lineID;
			}
			else if (index == 1)
			{
				onTextLineID = _lineID;
			}
			else
			{
				offTextLineID = _lineID;
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
			else if (index == 1)
			{
				return !string.IsNullOrEmpty (onText);
			}
			else
			{
				return !string.IsNullOrEmpty (offText);
			}
		}
		
		#endif

		#endregion

	}
	
}