/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MenuCycle.cs"
 * 
 *	This MenuElement is like a label, only its text cycles through an array when clicked on.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that displays different text each time it is clicked on.
	 * The index number of the text array it displays can be linked to a Global Variable (GVar), a custom script, or the game's current language.
	 */
	public class MenuCycle : MenuElement, ITranslatable
	{

		/** The Unity UI Button this is linked to (Unity UI Menus only) */
		public UnityEngine.UI.Button uiButton;

		/** The Unity UI Dropdown this is linked to (Unity UI Menus only) */
		public Dropdown uiDropdown;

		/** The ActionListAsset to run when the element is clicked on */
		public ActionListAsset actionListOnClick = null;
		/** The text that's displayed on-screen, which prefixes the varying text */
		public string label = "Element";
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The text alignment */
		public TextAnchor anchor;
		/** The index number of the currently-shown text in optionsArray */
		public int selected;
		/** An array of texts that the element can show one at a time */
		public List<string> optionsArray = new List<string>();
		/** What the text links to (CustomScript, GlobalVariable, Language) */
		public AC_CycleType cycleType;
		/** What kind of language to affect, if cycleType = AC_CycleType.Language and SpeechManager.separateVoiceAndTextLanguages = True */
		public SplitLanguageType splitLanguageType = SplitLanguageType.TextAndVoice;
		/** The ID number of the linked GlobalVariable, if cycleType = AC_CycleType.GlobalVariable */
		public int varID;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
		/** What kind of UI element the Cycle is linked to, if the Menu's Source is UnityUiPrefab or UnityUiInScene (Button, Dropdown) */
		public CycleUIBasis cycleUIBasis = CycleUIBasis.Button;

		/** An array of textures to replace the background according to the current choice (optional) */
		public Texture2D[] optionTextures = new Texture2D[0];
		private RawImage rawImage;

		private GVar linkedVariable;
		private string cycleText;

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
			label = "Cycle";
			selected = 0;
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			SetSize (new Vector2 (15f, 5f));
			anchor = TextAnchor.MiddleLeft;
			cycleType = AC_CycleType.CustomScript;
			splitLanguageType = SplitLanguageType.TextAndVoice;
			varID = 0;
			optionsArray = new List<string>();
			cycleText = string.Empty;
			actionListOnClick = null;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
			cycleUIBasis = CycleUIBasis.Button;
			optionTextures = new Texture2D[0];
			rawImage = null;
			linkedVariable = null;
			uiDropdown = null;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuCycle newElement = CreateInstance <MenuCycle>();
			newElement.Declare ();
			newElement.CopyCycle (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyCycle (MenuCycle _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiButton = null;
			}
			else
			{
				uiButton = _element.uiButton;
			}
			uiText = null;

			label = _element.label;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			anchor = _element.anchor;
			selected = _element.selected;
			optionsArray = _element.optionsArray;
			cycleType = _element.cycleType;
			splitLanguageType = _element.splitLanguageType;
			varID = _element.varID;
			cycleText = string.Empty;
			actionListOnClick = _element.actionListOnClick;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;
			cycleUIBasis = _element.cycleUIBasis;
			optionTextures = _element.optionTextures;
			linkedVariable = null;
			uiDropdown = _element.uiDropdown;

			base.Copy (_element);
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas)
		{
			rawImage = null;

			if (_menu.menuSource != MenuSource.AdventureCreator)
			{
				if (cycleUIBasis == CycleUIBasis.Button)
				{
					uiButton = LinkUIElement <UnityEngine.UI.Button> (canvas);
					if (uiButton)
					{
						rawImage = uiButton.GetComponentInChildren <RawImage>();

						#if TextMeshProIsPresent
						uiText = uiButton.GetComponentInChildren <TMPro.TextMeshProUGUI>();
						#else
						uiText = uiButton.GetComponentInChildren <Text>();
						#endif

						uiButton.onClick.AddListener (() => {
							ProcessClickUI (_menu, 0, KickStarter.playerInput.GetMouseState ());
						});
					}
				}
				else if (cycleUIBasis == CycleUIBasis.Dropdown)
				{
					uiDropdown = LinkUIElement <Dropdown> (canvas);
					if (uiDropdown != null)
					{
						uiDropdown.value = selected;
						uiDropdown.onValueChanged.AddListener (delegate {
	         				uiDropdownValueChangedHandler (uiDropdown);
	     				});
	     			}
				}
			}
		}


		private void uiDropdownValueChangedHandler (Dropdown _dropdown)
		{
			ProcessClickUI (parentMenu, 0, KickStarter.playerInput.GetMouseState ());
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
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuCycle)";

			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");

			if (source != AC.MenuSource.AdventureCreator)
			{
				cycleUIBasis = (CycleUIBasis) CustomGUILayout.EnumPopup ("UI basis:", cycleUIBasis, apiPrefix + ".cycleUIBasis", "What kind of UI element the Cycle is linked to");

				if (cycleUIBasis == CycleUIBasis.Button)
				{
					uiButton = LinkedUiGUI <UnityEngine.UI.Button> (uiButton, "Linked Button:", source, "The Unity UI Button this is linked to");
				}
				else if (cycleUIBasis == CycleUIBasis.Dropdown)
				{
					uiDropdown = LinkedUiGUI <Dropdown> (uiDropdown, "Linked Dropdown:", source);
				}
				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
			}

			cycleType = (AC_CycleType) CustomGUILayout.EnumPopup ("Cycle type:", cycleType, apiPrefix + ".cycleType", "What the value links to");

			if (cycleType == AC_CycleType.Language && KickStarter.speechManager != null && KickStarter.speechManager.separateVoiceAndTextLanguages)
			{
				splitLanguageType = (SplitLanguageType) CustomGUILayout.EnumPopup ("Language type:", splitLanguageType, apiPrefix + ".splitLanguageType", "What kind of language this affects");
			}

			if (source == MenuSource.AdventureCreator || cycleUIBasis == CycleUIBasis.Button)
			{
				label = CustomGUILayout.TextField ("Label text:", label, apiPrefix + ".label", "The text that's displayed on-screen, which prefixes the varying text");
			}

			if (cycleType == AC_CycleType.CustomScript || cycleType == AC_CycleType.Variable)
			{
				bool showOptionsGUI = true;

				if (cycleType == AC_CycleType.Variable)
				{
					VariableType[] allowedVarTypes = new VariableType[2];
					allowedVarTypes[0] = VariableType.Integer;
					allowedVarTypes[1] = VariableType.PopUp;

					varID = AdvGame.GlobalVariableGUI ("Global variable:", varID, allowedVarTypes, "The Global PopUp or Integer variable that's value will be synced with the cycle");

					if (AdvGame.GetReferences ().variablesManager != null && AdvGame.GetReferences ().variablesManager.GetVariable (varID) != null && AdvGame.GetReferences ().variablesManager.GetVariable (varID).type == VariableType.PopUp)
					{
						showOptionsGUI = false;
					}
				}

				if (showOptionsGUI)
				{
					int numOptions = optionsArray.Count;
					numOptions = EditorGUILayout.IntField ("Number of choices:", optionsArray.Count);
					if (numOptions < 0)
					{
						numOptions = 0;
					}
					
					if (numOptions < optionsArray.Count)
					{
						optionsArray.RemoveRange (numOptions, optionsArray.Count - numOptions);
					}
					else if (numOptions > optionsArray.Count)
					{
						if (numOptions > optionsArray.Capacity)
						{
							optionsArray.Capacity = numOptions;
						}
						for (int i=optionsArray.Count; i<numOptions; i++)
						{
							optionsArray.Add (string.Empty);
						}
					}
					
					for (int i=0; i<optionsArray.Count; i++)
					{
						optionsArray [i] = CustomGUILayout.TextField ("Choice #" + i.ToString () + ":", optionsArray [i], apiPrefix + ".optionsArray[" + i.ToString () + "]");
					}
				}
				
				if (cycleType == AC_CycleType.CustomScript)
				{
					if (optionsArray.Count > 0)
					{
						selected = CustomGUILayout.IntField ("Default option #:", selected, apiPrefix + ".selected");
					}
					ShowClipHelp ();
				}

				actionListOnClick = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("ActionList on click:", actionListOnClick, false, apiPrefix + ".actionListOnClick", "The ActionList asset to run when the element is clicked on");
			}
			alternativeInputButton = CustomGUILayout.TextField ("Alternative input button:", alternativeInputButton, apiPrefix + ".alternativeInputButton", "The name of the input button that triggers the element when pressed");

			bool showOptionTextures = (optionTextures.Length > 0);
			if (menu.menuSource != MenuSource.AdventureCreator && cycleUIBasis == CycleUIBasis.Dropdown)
			{
				showOptionTextures = false;
			}
			showOptionTextures = EditorGUILayout.Toggle ("Per-option textures?", showOptionTextures);
			if (showOptionTextures)
			{
				int numOptions = (cycleType == AC_CycleType.Language) ? KickStarter.speechManager.languages.Count : optionsArray.Count;
				if (cycleType == AC_CycleType.Language)
				{
					numOptions = 0;
					if (KickStarter.speechManager != null && KickStarter.speechManager.languages != null)
					{
						numOptions = KickStarter.speechManager.languages.Count;
					}
				}
				if (optionTextures.Length != numOptions)
				{
					optionTextures = new Texture2D[numOptions];
				}
			}
			else
			{
				optionTextures = new Texture2D[0];
			}
			EditorGUILayout.EndVertical ();

			if (showOptionTextures)
			{
				EditorGUILayout.BeginVertical ("Button");
				for (int i=0; i<optionTextures.Length; i++)
				{
					optionTextures[i] = (Texture2D) EditorGUILayout.ObjectField ("Option #" + i + " texture:", optionTextures[i], typeof (Texture2D), false);
				}
				if (menu.menuSource != MenuSource.AdventureCreator)
				{
					EditorGUILayout.HelpBox ("Per-option textures require a RawImage component on the linked Button.", MessageType.Info);
				}
				EditorGUILayout.EndVertical ();
			}

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


		public override bool CheckConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID)
		{
			if (cycleType == AC_CycleType.Variable && varID == oldGlobalID)
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

			if (cycleType == AC_CycleType.Variable && varID == _varID)
			{
				numFound ++;
			}

			if (cycleType == AC_CycleType.CustomScript || cycleType == AC_CycleType.Variable)
			{
				foreach (string optionLabel in optionsArray)
				{
					if (optionLabel.Contains (tokenText))
					{
						numFound ++;
					}
				}
			}

			return numFound + base.GetVariableReferences (_varID);
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (cycleUIBasis == CycleUIBasis.Button && uiButton != null && uiButton.gameObject == gameObject) return true;
			if (cycleUIBasis == CycleUIBasis.Dropdown && uiDropdown != null && uiDropdown.gameObject == gameObject) return true;
			if (linkedUiID == id) return true;
			return false;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if ((cycleType == AC_CycleType.CustomScript || cycleType == AC_CycleType.Variable) && actionListOnClick == actionListAsset)
				return true;
			return false;
		}

		#endif


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			CalculateValue ();

			cycleText = TranslateLabel (label, languageNumber) + " : ";

			if (Application.isPlaying && uiDropdown != null)
			{
				cycleText = string.Empty;
			}

			cycleText += GetOptionLabel (selected);

			if (Application.isPlaying && optionTextures.Length > 0 && selected < optionTextures.Length && optionTextures[selected] != null)
			{
				backgroundTexture = optionTextures[selected];
				if (rawImage != null)
				{
					rawImage.texture = backgroundTexture;
				}
			}

			if (uiButton)
			{
				if (uiText)
				{
					uiText.text = cycleText;
				}
				UpdateUISelectable (uiButton, uiSelectableHideStyle);
			}
			else
			{
				if (uiDropdown != null && Application.isPlaying)
				{
					uiDropdown.value = selected;
					UpdateUISelectable (uiDropdown, uiSelectableHideStyle);
				}
			}
		}


		private string GetOptionLabel (int index)
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying && cycleType == AC_CycleType.Language)
			{
				optionsArray = AdvGame.GetReferences ().speechManager.languages;
			}
			#endif

			if (index >= 0 && index < GetNumOptions ())
			{
				if (cycleType == AC_CycleType.Variable && linkedVariable != null && linkedVariable.type == VariableType.PopUp)
				{
					return linkedVariable.GetPopUpForIndex (index, Options.GetLanguage ());
				}
				return optionsArray [index];
			}

			if (Application.isPlaying)
			{
				ACDebug.Log ("Could not gather options for MenuCycle " + label);
				return string.Empty;
			}
			return "Default option";
		}


		private int GetNumOptions ()
		{
			if (!Application.isPlaying && cycleType == AC_CycleType.Variable && (linkedVariable == null || linkedVariable.id != varID))
			{
				if (AdvGame.GetReferences ().variablesManager != null)
				{
					linkedVariable = AdvGame.GetReferences ().variablesManager.GetVariable (varID);
				}
			}

			if (cycleType == AC_CycleType.Variable && linkedVariable != null && linkedVariable.type == VariableType.PopUp)
			{
				return linkedVariable.GetNumPopUpValues ();
			}
			return optionsArray.Count;
		}


		private void CycleOption ()
		{
			selected ++;
			if (selected >= GetNumOptions ())
			{
				selected = 0;
			}
		}
		

		/**
		 * <summary>Draws the element using OnGUI.</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">The index number of the slot to display</param>
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

			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), cycleText, _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (relativeRect, zoom), cycleText, _style);
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
			string optionLabel = GetOptionLabel (selected);
			if (!string.IsNullOrEmpty (optionLabel))
			{
				return TranslateLabel (label, languageNumber) + " : " + optionLabel;
			}
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

			if (uiDropdown != null)
			{
				selected = uiDropdown.value;
			}
			else
			{
				CycleOption ();
			}

			if (cycleType == AC_CycleType.Language)
			{
				if (selected == 0 && KickStarter.speechManager.ignoreOriginalText && KickStarter.runtimeLanguages.Languages.Count > 1)
				{
					// Ignore original text by skipping to first language
					selected = 1;
				}

				if (KickStarter.speechManager != null && KickStarter.speechManager.separateVoiceAndTextLanguages)
				{
					switch (splitLanguageType)
					{
						case SplitLanguageType.TextAndVoice:
							Options.SetLanguage (selected);
							Options.SetVoiceLanguage (selected);
							break;

						case SplitLanguageType.TextOnly:
							Options.SetLanguage (selected);
							break;

						case SplitLanguageType.VoiceOnly:
							Options.SetVoiceLanguage (selected);
							break;
					}
				}
				else
				{
					Options.SetLanguage (selected);
				}
			}
			else if (cycleType == AC_CycleType.Variable)
			{
				if (linkedVariable != null)
				{
					linkedVariable.IntegerValue = selected;
					linkedVariable.Upload ();
				}
			}

			if (cycleType == AC_CycleType.CustomScript)
			{
				MenuSystem.OnElementClick (_menu, this, _slot, (int) _mouseState);
			}
	
			if (actionListOnClick)
			{
				AdvGame.RunActionListAsset (actionListOnClick);
			}
			
			base.ProcessClick (_menu, _slot, _mouseState);
		}


		public override void RecalculateSize (MenuSource source)
		{
			if (Application.isPlaying && uiDropdown != null)
			{
				if (uiDropdown.captionText != null)
				{
					string _label = GetOptionLabel (selected);
					if (!string.IsNullOrEmpty (_label))
					{
						uiDropdown.captionText.text = _label;
					}
				}

				for (int i=0; i<GetNumOptions (); i++)
				{
					if (uiDropdown.options.Count > i && uiDropdown.options[i] != null)
					{
						uiDropdown.options[i].text = GetOptionLabel (i);
					}
				}
			}
			base.RecalculateSize (source);
		}


		public override void OnMenuTurnOn (Menu menu)
		{
			if (cycleType == AC_CycleType.Variable)
			{
				linkedVariable = GlobalVariables.GetVariable (varID);
				if (linkedVariable != null)
				{
					if (linkedVariable.type != VariableType.Integer && linkedVariable.type != VariableType.PopUp)
					{
						ACDebug.LogWarning ("Cannot link the variable '" + linkedVariable.label + "' to Cycle element '" + title + "' because it is not an Integer or PopUp.");
						linkedVariable = null;
					}
				}
				else
				{
					ACDebug.LogWarning ("Cannot find the variable with ID=" + varID + " to link to the Cycle element '" + title + "'");
				}
			}

			base.OnMenuTurnOn (menu);
		}


		private void CalculateValue ()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (cycleType == AC_CycleType.Language)
			{
				if (Application.isPlaying)
				{
					optionsArray = KickStarter.runtimeLanguages.Languages;
				}
				else
				{
					optionsArray = AdvGame.GetReferences ().speechManager.languages;
				}

				if (Options.optionsData != null)
				{
					selected = Options.optionsData.language;

					if (KickStarter.speechManager != null && KickStarter.speechManager.separateVoiceAndTextLanguages && splitLanguageType == SplitLanguageType.VoiceOnly)
					{
						selected = Options.optionsData.voiceLanguage;
					}
				}
			}
			else if (cycleType == AC_CycleType.Variable)
			{
				if (linkedVariable != null)
				{
					if (GetNumOptions () > 0)
					{
						selected = Mathf.Clamp (linkedVariable.IntegerValue, 0, GetNumOptions () - 1);
					}
					else
					{
						selected = 0;
					}
				}
			}
		}


		protected override void AutoSize ()
		{
			AutoSize (new GUIContent (TranslateLabel (label, Options.GetLanguage ()) + " : Default option"));
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