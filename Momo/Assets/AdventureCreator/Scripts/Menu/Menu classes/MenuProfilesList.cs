/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MenuProfilesList.cs"
 * 
 *	This MenuElement handles the display of any save profiles recorded.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;	
#endif

namespace AC
{

	/**
	 * This MenuElement lists any save profiles found on by SaveSystem.
	 */
	public class MenuProfilesList : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The text alignment */
		public TextAnchor anchor;
		/** The maximum number of profiles that can be displayed at once */
		public int maxSlots = 5;
		/** An ActionListAsset that can be run when a profile is clicked on */
		public ActionListAsset actionListOnClick;
		/** If True, then the current active profile will also be listed */
		public bool showActive = true;
		/** The method by which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;
		/** If True, then the profile will be switched to once its slot is clicked on */
		public bool autoHandle = true;
		/** What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic) */
		public LinkUIGraphic linkUIGraphic = LinkUIGraphic.ImageComponent;

		/** If True, then only one profile slot will be shown */
		public bool fixedOption;
		/** The index number of the profile to show, if fixedOption = true */
		public int optionToShow = 0;
		/** If >=0, The ID number of the integer ActionParameter in actionListOnClick to set to the index number of the slot clicked */
		public int parameterID = -1;

		private string[] labels = null;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiSlots = null;
			
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			maxSlots = 5;
			showActive = true;

			SetSize (new Vector2 (20f, 5f));
			anchor = TextAnchor.MiddleCenter;

			actionListOnClick = null;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			uiHideStyle = UIHideStyle.DisableObject;

			fixedOption = false;
			optionToShow = 0;
			autoHandle = true;
			parameterID = -1;
			linkUIGraphic = LinkUIGraphic.ImageComponent;

			base.Declare ();
		}
		

		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuProfilesList newElement = CreateInstance <MenuProfilesList>();
			newElement.Declare ();
			newElement.CopyProfilesList (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyProfilesList (MenuProfilesList _element, bool ignoreUnityUI)
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
			anchor = _element.anchor;
			maxSlots = _element.maxSlots;
			actionListOnClick = _element.actionListOnClick;
			showActive = _element.showActive;
			uiHideStyle = _element.uiHideStyle;
			autoHandle = _element.autoHandle;
			parameterID = _element.parameterID;
			fixedOption = _element.fixedOption;
			optionToShow = _element.optionToShow;
			linkUIGraphic = _element.linkUIGraphic;

			base.Copy (_element);
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
				return uiSlots[0].uiButton.gameObject;
			}
			return null;
		}


		/**
		 * Hides all linked Unity UI GameObjects associated with the element.
		 */
		public override void HideAllUISlots ()
		{
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
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
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuProfilesList)";

			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");

			fixedOption = CustomGUILayout.Toggle ("Fixed Profile ID only?", fixedOption, apiPrefix + ".fixedOption", "If True, then only one profile slot will be shown");
			if (fixedOption)
			{
				numSlots = 1;
				slotSpacing = 0f;
				optionToShow = CustomGUILayout.IntField ("ID to display:", optionToShow, apiPrefix + ".optionToShow", "The index number of the profile to show");
			}
			else
			{
				showActive = CustomGUILayout.Toggle ("Include active?", showActive, apiPrefix + ".showActive", "If True, then the current active profile will also be listed");
				maxSlots = CustomGUILayout.IntField ("Maximum number of slots:", maxSlots, apiPrefix + ".maxSlots", "The maximum number of profiles that can be displayed at once");

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

			autoHandle = CustomGUILayout.Toggle ("Switch profile when click?", autoHandle, apiPrefix + ".autoHandle", "If True, then the profile will be switched to once its slot is clicked on");

			if (autoHandle)
			{
				ActionListGUI ("ActionList after selecting:", menu.title, "After_Selecting", apiPrefix, "The ActionList asset to run once a profile has been switched to");
			}
			else
			{
				ActionListGUI ("ActionList when click:", menu.title, "When_Click", apiPrefix, "The ActionList asset to run once a profile has been clicked on");
			}

			if (source != MenuSource.AdventureCreator)
			{
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
				uiHideStyle = (UIHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiHideStyle, apiPrefix + ".uiHideStyle", "The method by which this element (or slots within it) are hidden from view when made invisible");
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				uiSlots = ResizeUISlots (uiSlots, maxSlots);
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}

				linkUIGraphic = (LinkUIGraphic) CustomGUILayout.EnumPopup ("Link graphics to:", linkUIGraphic, "", "What Image component the element's graphics should be linked to");
			}
			
			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
			textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".anchor");
			if (textEffects != TextEffects.None)
			{
				outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The outline thickness");
			}
		}


		private void ActionListGUI (string label, string menuTitle, string suffix, string apiPrefix, string tooltip)
		{
			actionListOnClick = ActionListAssetMenu.AssetGUI (label, actionListOnClick, menuTitle + "_" + title + "_" + suffix, apiPrefix + ".actionListOnClick", tooltip);
			
			if (actionListOnClick != null && actionListOnClick.NumParameters > 0)
			{
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.BeginHorizontal ();
				parameterID = Action.ChooseParameterGUI (string.Empty, actionListOnClick.DefaultParameters, parameterID, ParameterType.Integer);
				if (parameterID >= 0)
				{
					if (fixedOption)
					{
						EditorGUILayout.LabelField ("(= Profile ID #)");
					}
					else
					{
						EditorGUILayout.LabelField ("(= Slot index)");
					}
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.EndVertical ();
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


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (actionListOnClick == actionListAsset)
				return true;
			return false;
		}

		#endif


		/**
		 * <summary>Shifts which slots are on display, if the number of slots the element has exceeds the number of slots it can show at once.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <param name = "amount">The amount to shift slots by</param>
		 */
		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (fixedOption) return;

			if (isVisible && numSlots >= maxSlots)
			{
				Shift (shiftType, maxSlots, KickStarter.options.GetNumProfiles (), amount);
			}
		}


		/**
		 * <summary>Checks if the element's slots can be shifted in a particular direction.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <returns>True if the element's slots can be shifted in the particular direction</returns>
		 */
		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (numSlots == 0 || fixedOption)
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
				if (offset >= GetMaxOffset ())
				{
					return false;
				}
			}
			return true;
		}


		private int GetMaxOffset ()
		{
			if (fixedOption)
			{
				return 0;
			}

			if (!showActive)
			{
				return Mathf.Max (0, KickStarter.options.GetNumProfiles () - 1 - maxSlots);
			}
			return Mathf.Max (0, KickStarter.options.GetNumProfiles () - maxSlots);
		}
		

		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "slot">The index number of the slot</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			if (Application.isPlaying)
			{
				if (fixedOption)
				{
					return KickStarter.options.GetProfileIDName (optionToShow);
				}
				else
				{
					return KickStarter.options.GetProfileName (slot + offset, showActive);
				}
			}

			if (fixedOption)
			{
				return ("Profile ID " + optionToShow.ToString ());
			}
			return ("Profile " + slot.ToString ());
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiSlots != null && slotIndex >= 0 && uiSlots.Length > slotIndex && uiSlots[slotIndex] != null && uiSlots[slotIndex].uiButton != null)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiSlots[slotIndex].uiButton.gameObject);
			}
			return false;
		}
		

		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			string fullText = GetLabel (_slot, languageNumber);

			if (!Application.isPlaying)
			{
				if (labels == null || labels.Length != numSlots)
				{
					labels = new string [numSlots];
				}
			}

			labels [_slot] = fullText;

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);
					uiSlots[_slot].SetText (labels [_slot]);
				}
			}
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

			_style.wordWrap = true;
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}

			#if UNITY_EDITOR
			if (!Application.isPlaying && labels == null) PreDisplay (_slot, 0, isActive);
			#endif

			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style);
			}
		}
		

		/**
		 * <summary>Performs what should happen when the element is clicked on.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_slot">The index number of ths slot that was clicked</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return;
			}
			
			if (autoHandle)
			{
				bool isSuccess = false;

				if (fixedOption)
				{
					isSuccess = Options.SwitchProfileID (optionToShow);
				}
				else
				{
					isSuccess = KickStarter.options.SwitchProfile (_slot + offset, showActive);
				}

				if (isSuccess)
				{
					RunActionList (_slot);
				}
			}
			else
			{
				RunActionList (_slot);
			}

			base.ProcessClick (_menu, _slot, _mouseState);
		}


		private void RunActionList (int _slot)
		{
			if (fixedOption)
			{
				AdvGame.RunActionListAsset (actionListOnClick, parameterID, optionToShow);
			}
			else
			{
				AdvGame.RunActionListAsset (actionListOnClick, parameterID, _slot + offset);
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
				if (fixedOption)
				{
					numSlots = 1;
				}
				else
				{
					numSlots = KickStarter.options.GetNumProfiles ();

					if (!showActive)
					{
						numSlots --;
					}

					if (numSlots > maxSlots)
					{
						numSlots = maxSlots;
					}

					offset = Mathf.Min (offset, GetMaxOffset ());
				}
			}
			
			labels = new string [numSlots];
			
			if (!isVisible)
			{
				LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
			}
			
			base.RecalculateSize (source);
		}
		
		
		protected override void AutoSize ()
		{
			AutoSize (new GUIContent (GetLabel (0, 0)));
		}
		
	}
	
}