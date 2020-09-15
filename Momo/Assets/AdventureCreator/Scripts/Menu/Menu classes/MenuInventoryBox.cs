/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MenuInventoryBox.cs"
 * 
 *	This MenuElement lists all inventory items held by the player.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that lists inventory items (see: InvItem).
	 * It can be used to display all inventory items held by the player, those that are stored within a Container, or as part of an Interaction Menu.
	 */
	public class MenuInventoryBox : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;
		/** What pointer state registers as a 'click' for Unity UI Menus (PointerClick, PointerDown, PointerEnter) */
		public UIPointerState uiPointerState = UIPointerState.PointerClick;

		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The text alignement, if displayType = ConversationDisplayType.TextOnly */
		public TextAnchor anchor = TextAnchor.MiddleCenter;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** How the items to display are chosen (Default, HotspotBased, CustomScript, DisplaySelected, DisplayLastSelected, Container, CollectedDocuments, Objectives) */
		public AC_InventoryBoxType inventoryBoxType;
		/** An ActionList to run when a slot is clicked (if inventoryBoxType = CollectedDocuments or Objectives) */
		public ActionListAsset actionListOnClick = null;
		/** The maximum number of inventory items that can be shown at once */
		public int maxSlots;
		/** If True, only inventory items (InvItem) with a specific category will be displayed */
		public bool limitToCategory;
		/** If True, then only inventory items that are listed in a Hotspot's / InvItem's interactions will be listed if inventoryBoxType = AC_InventoryBoxType.HotspotBased */
		public bool limitToDefinedInteractions = true;
		/** The category ID to limit the display of inventory items by, if limitToCategory = True (Deprecated) */
		public int categoryID;
		/** The category IDs to limit the display of inventory items by, if limitToCategory = True */
		public List<int> categoryIDs = new List<int>();
		/** What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic) */
		public LinkUIGraphic linkUIGraphic = LinkUIGraphic.ImageComponent;
		/** If True, then no interactions will work, but items can still be selected and re-arranged as normal */
		public bool preventInteractions = false;
		/** If True, and the element is scrolled by an offset larger than the number of new items to show, then the offset amount will be reduced to only show those new items. */
		public bool limitMaxScroll = true;

		/** DisplayType != ConversationDisplayType.IconOnly, Hotspot labels will only update when hovering over items if this is True */
		public bool updateHotspotLabelWhenHover = false;
		/** If True, then the hover sound will play even when over an empty slot */
		public bool hoverSoundOverEmptySlots = true;

		/** The List of inventory items that are on display */
		public List<InvItem> items = new List<InvItem>();
		/** If inventoryBoxType = AC_InventoryBoxType.Container, what happens to items when they are removed from the container */
		public ContainerSelectMode containerSelectMode = ContainerSelectMode.MoveToInventoryAndSelect;
		/** How items are displayed (IconOnly, TextOnly, IconAndText) */
		public ConversationDisplayType displayType = ConversationDisplayType.IconOnly;
		/** The method by which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;
		/** If True, and inventoryBoxType = AC_InventoryBoxType.CollectedDocuments, then clicking an element slot will open the chosen Document */
		public bool autoOpenDocument = true;
		/** The texture to display when a slot is empty */
		public Texture2D emptySlotTexture = null;
		/** How the item count is displayed */
		public InventoryItemCountDisplay inventoryItemCountDisplay = InventoryItemCountDisplay.OnlyIfMultiple;

		/** What Objectives to display, if inventoryBoxType = AC_InventoryBoxType.Objectives */
		public ObjectiveDisplayType objectiveDisplayType = ObjectiveDisplayType.All;

		private Container overrideContainer;
		private string[] labels = null;
		private int numDocuments = 0;
		private Texture[] textures;

		public enum ContainerSelectMode { MoveToInventory, MoveToInventoryAndSelect, SelectItemOnly };

		#if UNITY_EDITOR
		private Texture2D testIcon;
		#endif


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiSlots = null;
			uiPointerState = UIPointerState.PointerClick;

			isVisible = true;
			isClickable = true;
			inventoryBoxType = AC_InventoryBoxType.Default;
			actionListOnClick = null;
			anchor = TextAnchor.MiddleCenter;
			numSlots = 0;
			SetSize (new Vector2 (6f, 10f));
			maxSlots = 10;
			limitToCategory = false;
			limitToDefinedInteractions = true;
			containerSelectMode = ContainerSelectMode.MoveToInventoryAndSelect;
			categoryID = -1;
			displayType = ConversationDisplayType.IconOnly;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			uiHideStyle = UIHideStyle.DisableObject;
			emptySlotTexture = null;
			objectiveDisplayType = ObjectiveDisplayType.All;
			items = new List<InvItem>();
			categoryIDs = new List<int>();
			linkUIGraphic = LinkUIGraphic.ImageComponent;
			autoOpenDocument = true;
			updateHotspotLabelWhenHover = false;
			hoverSoundOverEmptySlots = true;
			preventInteractions = false;
			limitMaxScroll = true;
			inventoryItemCountDisplay = InventoryItemCountDisplay.OnlyIfMultiple;
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuInventoryBox newElement = CreateInstance <MenuInventoryBox>();
			newElement.Declare ();
			newElement.CopyInventoryBox (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyInventoryBox (MenuInventoryBox _element, bool ignoreUnityUI)
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

			uiPointerState = _element.uiPointerState;

			isClickable = _element.isClickable;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			anchor = _element.anchor;
			inventoryBoxType = _element.inventoryBoxType;
			actionListOnClick = _element.actionListOnClick;
			numSlots = _element.numSlots;
			maxSlots = _element.maxSlots;
			limitToCategory = _element.limitToCategory;
			limitToDefinedInteractions = _element.limitToDefinedInteractions;
			categoryID = _element.categoryID;
			containerSelectMode = _element.containerSelectMode;
			displayType = _element.displayType;
			uiHideStyle = _element.uiHideStyle;
			emptySlotTexture = _element.emptySlotTexture;
			objectiveDisplayType = _element.objectiveDisplayType;
			categoryIDs = _element.categoryIDs;
			linkUIGraphic = _element.linkUIGraphic;
			autoOpenDocument = _element.autoOpenDocument;
			updateHotspotLabelWhenHover = _element.updateHotspotLabelWhenHover;
			hoverSoundOverEmptySlots = _element.hoverSoundOverEmptySlots;
			preventInteractions = _element.preventInteractions;
			limitMaxScroll = _element.limitMaxScroll;
			inventoryItemCountDisplay = _element.inventoryItemCountDisplay;

			UpdateLimitCategory ();

			items = GetItemList ();

			base.Copy (_element);

			if (Application.isPlaying)
			{
				if (!(inventoryBoxType == AC_InventoryBoxType.HotspotBased && maxSlots == 1))
				{
					alternativeInputButton = "";
				}
			}

			Upgrade ();
		}


		private void Upgrade ()
		{
			if (limitToCategory && categoryID >= 0)
			{
				categoryIDs.Add (categoryID);
				categoryID = -1;

				if (Application.isPlaying)
				{
					ACDebug.Log ("The inventory box element '" + title + "' has been upgraded - please view it in the Menu Manager and Save.");
				}
			}
		}


		private void UpdateLimitCategory ()
		{
			if (Application.isPlaying && AdvGame.GetReferences ().inventoryManager != null && AdvGame.GetReferences ().inventoryManager.bins != null)
			{
				foreach (InvBin invBin in KickStarter.inventoryManager.bins)
				{
					if (categoryIDs.Contains (invBin.id))
					{
						// Fine!
					}
					else
					{
						categoryIDs.Remove (invBin.id);
					}
				}
			}
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
				uiSlot.LinkUIElements (canvas, linkUIGraphic, (inventoryBoxType != AC_InventoryBoxType.CollectedDocuments) ? emptySlotTexture : null);
				if (uiSlot != null && uiSlot.uiButton != null)
				{
					int j=i;

					if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.CustomScript)
					{
						if (KickStarter.settingsManager != null &&
							KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ContextSensitive &&
							KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple)
						{}
						else
						{
							uiPointerState = UIPointerState.PointerClick;
						}
					}

					CreateUIEvent (uiSlot.uiButton, _menu, uiPointerState, j, false);

					uiSlot.AddClickHandler (_menu, this, j);
				}
				i++;
			}
		}


		/**
		 * <summary>Gets the UI Button associated with an inventory item, provided that the Menus' Source is UnityUiPrefab or UnityUiInScene.</summary>
		 * <param name = "itemID">The ID number of the inventory item (InvItem) to search for</param>
		 * <returns>The UI Button associated with an inventory item, or null if a suitable Button cannot be found.</returns>
		 */
		public UnityEngine.UI.Button GetUIButtonWithItem (int itemID)
		{
			for (int i=0; i<items.Count; i++)
			{
				if (items[i] != null && items[i].id == itemID)
				{
					if (uiSlots != null && uiSlots.Length > i && uiSlots[i] != null)
					{
						return uiSlots[i].uiButton;
					}
					return null;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the linked Unity UI GameObject associated with this element.</summary>
		 * <returns>The Unity UI GameObject associated with the element</returns>
		 */
		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (uiSlots != null && uiSlots.Length > slotIndex && uiSlots[slotIndex].uiButton != null)
			{
				return uiSlots[slotIndex].uiButton.gameObject;
			}
			return null;
		}
		

		/**
		 * <summary>Gets the boundary of the slot</summary>
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
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuInventoryBox)";

			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");

			inventoryBoxType = (AC_InventoryBoxType) CustomGUILayout.EnumPopup ("Inventory box type:", inventoryBoxType, apiPrefix + ".inventoryBoxType", "How the items to display are chosen");
			if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.CustomScript)
			{
				maxSlots = CustomGUILayout.IntSlider ("Max number of slots:", maxSlots, 1, 30, apiPrefix + ".maxSlots", "The maximum number of inventory items that can be shown at once");
				isClickable = true;
			}
			else if (inventoryBoxType == AC_InventoryBoxType.DisplaySelected)
			{
				isClickable = false;
				maxSlots = 1;
			}
			else if (inventoryBoxType == AC_InventoryBoxType.DisplayLastSelected)
			{
				isClickable = true;
				maxSlots = 1;
			}
			else if (inventoryBoxType == AC_InventoryBoxType.Container)
			{
				isClickable = true;
				maxSlots = CustomGUILayout.IntSlider ("Max number of slots:", maxSlots, 1, 30, apiPrefix + ".maxSlots", "The maximum number of inventory items that can be shown at once");
				containerSelectMode = (ContainerSelectMode) CustomGUILayout.EnumPopup ("Behaviour after taking?", containerSelectMode, apiPrefix + ".containerSelectMode", "What happens to items when they are removed from the Container");
			}
			else
			{
				isClickable = true;
				if (source == MenuSource.AdventureCreator)
				{
					numSlots = CustomGUILayout.IntField ("Test slots:", numSlots, apiPrefix + ".numSlots");
				}
				maxSlots = CustomGUILayout.IntSlider ("Max number of slots:", maxSlots, 1, 30, apiPrefix + ".maxSlots", "The maximum number of inventory items that can be shown at once");
			}

			if (inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected &&
				inventoryBoxType != AC_InventoryBoxType.DisplaySelected)
			{
				limitMaxScroll = CustomGUILayout.Toggle ("Limit maximum scroll?", limitMaxScroll, apiPrefix + ".limitMaxScroll", "If True, and the element is scrolled by an offset larger than the number of new items to show, then the offset amount will be reduced to only show those new items.");
			}

			if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.Container)
			{
				preventInteractions = CustomGUILayout.Toggle ("Prevent interactions?", preventInteractions, apiPrefix + ".preventInteractions", "If True, then no interactions will work, but items can still be selected and re-arranged as normal");
			}

			if (inventoryBoxType == AC_InventoryBoxType.HotspotBased)
			{
				if (!ForceLimitByReference ())
				{
					limitToDefinedInteractions = CustomGUILayout.ToggleLeft ("Only show items referenced in Interactions?", limitToDefinedInteractions, apiPrefix + ".limitToDefinedInteractions", "If True, then only inventory items that are listed in a Hotspot's / InvItem's interactions will be listed");
				}

				if (maxSlots == 1)
				{
					alternativeInputButton = CustomGUILayout.TextField ("Alternative input button:", alternativeInputButton, apiPrefix + ".alternativeInputButton", "The name of the input button that triggers the element when pressed");
				}
			}
			else if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments)
			{
				autoOpenDocument = CustomGUILayout.ToggleLeft ("Auto-open Document when clicked?", autoOpenDocument, apiPrefix + ".autoOpenDocument", "If True, then clicking a slot will open the chosen Document");
				actionListOnClick = ActionListAssetMenu.AssetGUI ("ActionList when click:", actionListOnClick, title + "_Click", apiPrefix + ".actionListOnClick", "The ActionList asset to run whenever a slot is clicked");
			}
			else if (inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				objectiveDisplayType = (ObjectiveDisplayType) CustomGUILayout.EnumPopup ("Objectives to display:", objectiveDisplayType, apiPrefix + ".objectiveDisplayType", "What Objectives to display");
				autoOpenDocument = CustomGUILayout.ToggleLeft ("Auto-select Objective when clicked?", autoOpenDocument, apiPrefix + ".autoOpenDocument", "If True, then clicking a slot will select the chosen Objective");
				actionListOnClick = ActionListAssetMenu.AssetGUI ("ActionList when click:", actionListOnClick, title + "_Click", apiPrefix + ".actionListOnClick", "The ActionList asset to run whenever a slot is clicked");
			}

			displayType = (ConversationDisplayType) CustomGUILayout.EnumPopup ("Display type:", displayType, apiPrefix + ".displayType", "How items are displayed");
			if (displayType == ConversationDisplayType.IconAndText && source == MenuSource.AdventureCreator)
			{
				EditorGUILayout.HelpBox ("'Icon And Text' mode is only available for Unity UI-based Menus.", MessageType.Warning);
			}

			if (inventoryBoxType != AC_InventoryBoxType.CollectedDocuments && 
				inventoryBoxType != AC_InventoryBoxType.Objectives)
			{
				inventoryItemCountDisplay = (InventoryItemCountDisplay) CustomGUILayout.EnumPopup ("Display item amounts:", inventoryItemCountDisplay, apiPrefix + ".inventoryItemCountDisplay", "How item counts are drawn");
			}

			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (displayType != ConversationDisplayType.IconOnly)
				{
					updateHotspotLabelWhenHover = CustomGUILayout.ToggleLeft ("Update Hotspot label when hovering?", updateHotspotLabelWhenHover, apiPrefix + ".updateHotspotLabelWhenHover", "If True, Hotspot labels will only update when hovering over items");
				}
			}

			if (inventoryBoxType != AC_InventoryBoxType.DisplaySelected && inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected && source == MenuSource.AdventureCreator)
			{
				slotSpacing = CustomGUILayout.Slider ("Slot spacing:", slotSpacing, 0f, 30f, apiPrefix + ".slotSpacing");
				orientation = (ElementOrientation) CustomGUILayout.EnumPopup ("Slot orientation:", orientation, apiPrefix + ".orientation");
				if (orientation == ElementOrientation.Grid)
				{
					gridWidth = CustomGUILayout.IntSlider ("Grid size:", gridWidth, 1, 10, apiPrefix + ".gridWidth");
				}
			}
			
			if (inventoryBoxType == AC_InventoryBoxType.CustomScript)
			{
				ShowClipHelp ();
			}

			uiHideStyle = (UIHideStyle) CustomGUILayout.EnumPopup ("When slot is empty:", uiHideStyle, apiPrefix + ".uiHideStyle", "The method by which this element (or slots within it) are hidden from view when made invisible");

			if (inventoryBoxType != AC_InventoryBoxType.CollectedDocuments && inventoryBoxType != AC_InventoryBoxType.Objectives && uiHideStyle == UIHideStyle.ClearContent && displayType != ConversationDisplayType.TextOnly)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("Empty slot texture:", "The texture to display when a slot is empty"), GUILayout.Width (145f));
				emptySlotTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (emptySlotTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".emptySlotTexture");
				EditorGUILayout.EndHorizontal ();
			}

			hoverSoundOverEmptySlots = CustomGUILayout.Toggle ("Hover sound when empty?", hoverSoundOverEmptySlots, apiPrefix + ".hoverSoundOverEmptySlots", "If True, then the hover sound will play even when over an empty slot");

			if (source != MenuSource.AdventureCreator)
			{
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				uiSlots = ResizeUISlots (uiSlots, maxSlots);
				
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}

				linkUIGraphic = (LinkUIGraphic) CustomGUILayout.EnumPopup ("Link graphics to:", linkUIGraphic, "", "What Image component the element's graphics should be linked to");

				// Don't show if Single and Default or Custom Script
				if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.CustomScript)
				{
					if (KickStarter.settingsManager != null &&
						KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ContextSensitive &&
						KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple)
					{
						uiPointerState = (UIPointerState) CustomGUILayout.EnumPopup ("Responds to:", uiPointerState, apiPrefix + ".uiPointerState", "What pointer state registers as a 'click' for Unity UI Menus");
					}
				}
				else
				{
					uiPointerState = (UIPointerState) CustomGUILayout.EnumPopup ("Responds to:", uiPointerState, apiPrefix + ".uiPointerState", "What pointer state registers as a 'click' for Unity UI Menus");
				}
			}

			ChangeCursorGUI (menu);
			EditorGUILayout.EndVertical ();

			if (CanBeLimitedByCategory ())
			{
				ShowCategoriesUI (apiPrefix);
			}

			base.ShowGUI (menu);
		}


		protected override void ShowTextureGUI (string apiPrefix)
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments && displayType == ConversationDisplayType.IconOnly)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Test icon:", GUILayout.Width (145f));
				testIcon = (Texture2D) EditorGUILayout.ObjectField (testIcon, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
			}
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			if (displayType == ConversationDisplayType.TextOnly)
			{
				anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
			}
			textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");
			if (textEffects != TextEffects.None)
			{
				outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The outline thickness");
			}
		}


		private void ShowCategoriesUI (string apiPrefix)
		{
			EditorGUILayout.BeginVertical ("Button");
		
			limitToCategory = CustomGUILayout.Toggle ("Limit by category?", limitToCategory, apiPrefix + ".limitToCategory", "If True, only items with a specific category will be displayed");
			if (limitToCategory)
			{
				Upgrade ();

				if (AdvGame.GetReferences ().inventoryManager)
				{
					List<InvBin> bins = AdvGame.GetReferences ().inventoryManager.bins;

					if (bins == null || bins.Count == 0)
					{
						categoryIDs.Clear ();
						EditorGUILayout.HelpBox ("No categories defined!", MessageType.Warning);
					}
					else
					{
						for (int i=0; i<bins.Count; i++)
						{
							bool include = (categoryIDs.Contains (bins[i].id)) ? true : false;
							include = EditorGUILayout.ToggleLeft (" " + i.ToString () + ": " + bins[i].label, include);

							if (include)
							{
								if (!categoryIDs.Contains (bins[i].id))
								{
									categoryIDs.Add (bins[i].id);
								}
							}
							else
							{
								if (categoryIDs.Contains (bins[i].id))
								{
									categoryIDs.Remove (bins[i].id);
								}
							}
						}

						if (categoryIDs.Count == 0)
						{
							EditorGUILayout.HelpBox ("At least one category must be checked for this to take effect.", MessageType.Info);
						}
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("No Inventory Manager defined!", MessageType.Warning);
					categoryIDs.Clear ();
				}
			}
			EditorGUILayout.EndVertical ();
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
			if ((inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives) && actionListOnClick == actionListAsset)
				return true;
			return false;
		}

		#endif


		/**
		 * Hides all linked Unity UI GameObjects associated with the element.
		 */
		public override void HideAllUISlots ()
		{
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle, emptySlotTexture);
		}


		public override void SetUIInteractableState (bool state)
		{
			SetUISlotsInteractableState (uiSlots, state);
		}


		public override string GetHotspotLabelOverride (int _slot, int _language)
		{
			if (uiSlots != null && _slot < uiSlots.Length && !uiSlots[_slot].CanOverrideHotspotLabel) return string.Empty;

			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (displayType == ConversationDisplayType.IconOnly || updateHotspotLabelWhenHover)
				{
					return labels [_slot];
				}
				return string.Empty;
			}

			InvItem slotItem = GetItem (_slot);
			if (slotItem == null)
			{
				return string.Empty;
			}

			string slotItemLabel = slotItem.GetLabel (_language);

			if (inventoryBoxType == AC_InventoryBoxType.HotspotBased)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
					KickStarter.settingsManager.SelectInteractionMethod () != SelectInteractions.ClickingMenu)
				{
					// Don't override, refer to the clicked InventoryBox
					return string.Empty;
				}

				if (KickStarter.cursorManager.addHotspotPrefix)
				{
					string prefix = KickStarter.runtimeInventory.GetHotspotPrefixLabel (slotItem, slotItemLabel, _language);
					if (parentMenu.TargetInvItem != null)
					{
						// Combine two items, i.e. "Use worm on apple"
						return AdvGame.CombineLanguageString (prefix, parentMenu.TargetInvItem.GetLabel (_language), _language);
					}

					if (parentMenu.TargetHotspot != null)
					{
						// Use item on hotspot, i.e. "Use worm on bench"
						return AdvGame.CombineLanguageString (prefix, parentMenu.TargetHotspot.GetName (_language), _language);
					}
				}
				else
				{
					if (parentMenu.TargetInvItem != null)
					{
						// Parent menu's item label only
						return parentMenu.TargetInvItem.GetLabel (_language);
					}
				}

				return string.Empty;
			}

			InvItem selectedItem = KickStarter.runtimeInventory.SelectedItem;
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
			{
				if (selectedItem != null && KickStarter.cursorManager.inventoryHandling != InventoryHandling.ChangeCursor)
				{
					if (selectedItem.id == slotItem.id)
					{
						return slotItemLabel;
					}

					// Combine two items, i.e. "Use worm on apple"
					string prefix = KickStarter.runtimeInventory.GetHotspotPrefixLabel (selectedItem, selectedItem.GetLabel (_language), _language);
					return AdvGame.CombineLanguageString (prefix, slotItemLabel, _language);
				}

				// Just the item, i.e. "Worm"
				return slotItemLabel;
			}
			else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot ||
					KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
			{
				if (selectedItem != null)
				{
					if (KickStarter.cursorManager.inventoryHandling != InventoryHandling.ChangeCursor)
					{
						if (selectedItem.id == slotItem.id)
						{
							return slotItemLabel;
						}

						// Combine two items, i.e. "Use worm on apple"
						string prefix = KickStarter.runtimeInventory.GetHotspotPrefixLabel (selectedItem, selectedItem.GetLabel (_language), _language);
						return AdvGame.CombineLanguageString (prefix, slotItemLabel, _language);
					}
				}
				else
				{
					// None selected

					if (KickStarter.cursorManager.addHotspotPrefix)
					{
						if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
							KickStarter.settingsManager.SelectInteractionMethod () != SelectInteractions.ClickingMenu &&
							KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple)
						{
							string prefix = KickStarter.playerInteraction.GetLabelPrefix (null, slotItem, _language);
							return AdvGame.CombineLanguageString (prefix, slotItemLabel, _language);
						}

						if (KickStarter.playerCursor.GetSelectedCursor () >= 0)
						{
							// Use an item, i.e. "Look at worm"
							string prefix = KickStarter.cursorManager.GetLabelFromID (KickStarter.playerCursor.GetSelectedCursorID (), _language);
							return AdvGame.CombineLanguageString (prefix, slotItemLabel, _language);
						}
					}
				}

				// Just the item, i.e. "Worm"
				return slotItemLabel;
			}

			return string.Empty;
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (Application.isPlaying)
				{
					if (uiSlots != null && uiSlots.Length > _slot)
					{
						LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);

						if (displayType == ConversationDisplayType.IconOnly || displayType == ConversationDisplayType.IconAndText)
						{
							uiSlots[_slot].SetImage (textures [_slot]);
						}
						if (displayType == ConversationDisplayType.TextOnly || displayType == ConversationDisplayType.IconAndText)
						{
							uiSlots[_slot].SetText (labels [_slot]);
						}
					}
				}
				else
				{
					string fullText = string.Empty;
					if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments)
					{
						fullText = "Document #" + _slot.ToString ();
					}
					else
					{
						fullText = "Objective #" + _slot.ToString ();
					}

					if (labels == null || labels.Length != numSlots)
					{
						labels = new string[numSlots];
					}
					labels [_slot] = fullText;
				}
				return;
			}

			if (items.Count > 0 && items.Count > (_slot+offset) && items [_slot+offset] != null)
			{
				string fullText = string.Empty;

				if (displayType == ConversationDisplayType.TextOnly || displayType == ConversationDisplayType.IconAndText)
				{
					fullText = items [_slot+offset].label;
					if (KickStarter.runtimeInventory != null)
					{
						fullText = KickStarter.runtimeInventory.GetLabel (items [_slot+offset], languageNumber);
					}
					string countText = GetCount (_slot);
					if (!string.IsNullOrEmpty (countText))
					{
						fullText += " (" + countText + ")";
					}
				}
				else
				{
					string countText = GetCount (_slot);
					if (!string.IsNullOrEmpty (countText))
					{
						fullText = countText;
					}
				}

				if (labels == null || labels.Length != numSlots)
				{
					labels = new string [numSlots];
				}
				labels [_slot] = fullText;
			}

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle, emptySlotTexture);

					uiSlots[_slot].SetText (labels [_slot]);
					if (displayType == ConversationDisplayType.IconOnly || displayType == ConversationDisplayType.IconAndText)
					{
						Texture tex = null;
						if (items.Count > (_slot+offset) && items [_slot+offset] != null)
						{
							if (inventoryBoxType != AC_InventoryBoxType.DisplaySelected && inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected)
							{
								if (KickStarter.settingsManager.selectInventoryDisplay == SelectInventoryDisplay.HideFromMenu && ItemIsSelected (_slot+offset))
								{
									if (!items[_slot+offset].CanSelectSingle ())
									{
										// Display as normal if we only have one selected from many
										uiSlots[_slot].SetImage (null);
										labels [_slot] = string.Empty;
										uiSlots[_slot].SetText (labels [_slot]);
										return;
									}
								}
								tex = GetTexture (_slot+offset, isActive);
							}

							if (tex == null)
							{
								tex = items [_slot+offset].tex;
							}
						}
						uiSlots[_slot].SetImage (tex);
					}

					if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot &&
						inventoryBoxType == AC_InventoryBoxType.HotspotBased &&
						items[_slot + offset].id == KickStarter.playerInteraction.GetActiveInvButtonID ())
					{
						// Select through script, not by mouse-over
						if (uiSlots[_slot].uiButton != null)
						{
							uiSlots[_slot].uiButton.Select ();
						}
					}
				}
			}

			return;
		}


		private bool ItemIsSelected (int index)
		{
			if (items[index] != null && (!KickStarter.settingsManager.InventoryDragDrop || KickStarter.playerInput.GetDragState () == DragState.Inventory))
			{
				if (items[index] == KickStarter.runtimeInventory.SelectedItem)
				{
					return true;
				}
				if (inventoryBoxType == AC_InventoryBoxType.Container && containerSelectMode == ContainerSelectMode.SelectItemOnly && KickStarter.runtimeInventory.selectedContainerItem != null)
				{
					Container container = (overrideContainer != null) ? overrideContainer : KickStarter.playerInput.activeContainer;
					if (container != null && container.items[index] == KickStarter.runtimeInventory.selectedContainerItem)
					{
						return true;
					}
				}
			}
			return false;
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

			if (displayType == ConversationDisplayType.TextOnly)
			{
				_style.alignment = anchor;
			}

			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
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
					if (Application.isPlaying && textures[_slot] != null)
					{
						GUI.DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), textures[_slot], ScaleMode.StretchToFill, true, 0f);
					}
					#if UNITY_EDITOR
					else if (testIcon != null)
					{
						GUI.DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), testIcon, ScaleMode.StretchToFill, true, 0f);
					}
					#endif
					
					GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), string.Empty, _style);
				}
				return;
			}

			if (items.Count > 0 && items.Count > (_slot+offset) && items [_slot+offset] != null)
			{
				if (Application.isPlaying && KickStarter.settingsManager.selectInventoryDisplay == SelectInventoryDisplay.HideFromMenu && ItemIsSelected (_slot+offset))
				{
					if (!items[_slot+offset].CanSelectSingle ())
					{
						// Display as normal if we only have one selected from many
						return;
					}
				}

				Rect slotRect = GetSlotRectRelative (_slot);

				if (displayType == ConversationDisplayType.IconOnly)
				{
					GUI.Label (slotRect, string.Empty, _style);
					DrawTexture (ZoomRect (slotRect, zoom), _slot+offset, isActive);
					_style.normal.background = null;
					
					if (textEffects != TextEffects.None)
					{
						AdvGame.DrawTextEffect (ZoomRect (slotRect, zoom), GetCount (_slot), _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
					}
					else
					{
						GUI.Label (ZoomRect (slotRect, zoom), GetCount (_slot), _style);
					}
				}
				else if (displayType == ConversationDisplayType.TextOnly)
				{
					if (textEffects != TextEffects.None)
					{
						AdvGame.DrawTextEffect (ZoomRect (slotRect, zoom), labels[_slot], _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
					}
					else
					{
						GUI.Label (ZoomRect (slotRect, zoom), labels[_slot], _style);
					}
				}
				return;
			}

			if (displayType == ConversationDisplayType.IconOnly && emptySlotTexture != null)
			{
				Rect slotRect = GetSlotRectRelative (_slot);

				_style.normal.background = null;
				GUI.Label (slotRect, string.Empty, _style);
				GUI.DrawTexture (ZoomRect (slotRect, zoom), emptySlotTexture, ScaleMode.StretchToFill, true, 0f);
			}
		}


		private bool AllowInteractions ()
		{
			if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.Container)
			{
				return !preventInteractions;
			}
			return true;
		}
		

		/**
		 * <summary>Performs what should happen when the element is clicked on, if inventoryBoxType = AC_InventoryBoxType.Default.</summary>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 * <param name = "_slot">The index number of the slot that was clicked on</param>
		 * <param name = "interactionMethod">The game's interaction method (ContextSensitive, ChooseHotspotThenInteraction, ChooseInteractionThenHotspot)</param>
		 */
		public void HandleDefaultClick (MouseState _mouseState, int _slot, AC_InteractionMethod interactionMethod)
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				return;
			}

			if (KickStarter.stateHandler.gameState == GameState.DialogOptions && !KickStarter.settingsManager.allowInventoryInteractionsDuringConversations && !KickStarter.settingsManager.allowGameplayDuringConversations)
			{
				return;
			}
			if (KickStarter.runtimeInventory != null)
			{
				KickStarter.playerMenus.CloseInteractionMenus ();

				KickStarter.runtimeInventory.HighlightItemOffInstant ();
				KickStarter.runtimeInventory.SetFont (font, GetFontSize (), fontColor, textEffects);

				int trueIndex = _slot + offset;

				if (inventoryBoxType == AC_InventoryBoxType.Default)
				{
					if (items.Count <= trueIndex || items[trueIndex] == null)
					{
						ContainerItem selectedContainerItem = (KickStarter.runtimeInventory.selectedContainerItem != null) ? new ContainerItem (KickStarter.runtimeInventory.selectedContainerItem) : null;
						Container selectedContainerItemContainer = KickStarter.runtimeInventory.selectedContainerItemContainer;

						if (selectedContainerItem != null && !KickStarter.runtimeInventory.CanTransferContainerItemsToInventory (selectedContainerItem))
						{
							return;
						}

						// Blank space
						if (KickStarter.runtimeInventory.SelectedItem != null && KickStarter.settingsManager.canReorderItems)
						{
							if (limitToCategory && categoryIDs != null && categoryIDs.Count > 0)
							{
								// Need to change index because we want to affect the actual inventory, not the percieved one shown in the restricted menu
								List<InvItem> trueItemList = GetItemList (false);
								LimitedItemList limitedItemList = LimitByCategory (trueItemList, trueIndex);
								trueIndex += limitedItemList.Offset;
							}

							KickStarter.runtimeInventory.MoveItemToIndex (KickStarter.runtimeInventory.SelectedItem, trueIndex);
						}
						else if (selectedContainerItem != null)
						{
							trueIndex = KickStarter.runtimeInventory.localItems.Count;
							KickStarter.runtimeInventory.MoveItemToIndex (KickStarter.runtimeInventory.SelectedItem, trueIndex);
						}

						if (selectedContainerItem != null && selectedContainerItemContainer != null)
						{
							KickStarter.eventManager.Call_OnUseContainer (false, selectedContainerItemContainer, selectedContainerItem);
						}
						KickStarter.runtimeInventory.SetNull ();
						return;
					}
				}

				if (KickStarter.runtimeInventory.selectedContainerItem != null)
				{
					return;
				}

				if (interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.runtimeInventory.SelectedItem != null)
					{
						if (_mouseState == MouseState.SingleClick)
						{
							if (items.Count <= trueIndex)
							{
								return;
							}
							if (!AllowInteractions ())
							{
								if (items [trueIndex] == KickStarter.runtimeInventory.SelectedItem)
								{
									KickStarter.runtimeInventory.SetNull ();
								}
								return;
							}
							KickStarter.runtimeInventory.Combine (KickStarter.runtimeInventory.SelectedItem, items [trueIndex]);
						}
						else if (_mouseState == MouseState.RightClick)
						{
							KickStarter.runtimeInventory.SetNull ();
						}
					}
					else
					{
						if (items.Count <= trueIndex)
						{
							return;
						}

						if (!AllowInteractions ())
						{
							KickStarter.runtimeInventory.SelectItem (items [trueIndex], SelectItemMode.Use);
						}
						else
						{
							KickStarter.runtimeInventory.ShowInteractions (items [trueIndex]);
						}
					}
				}
				else if (interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					if (items.Count <= trueIndex) return;

					if (_mouseState == MouseState.SingleClick)
					{
						int cursorID = KickStarter.playerCursor.GetSelectedCursorID ();
						int cursor = KickStarter.playerCursor.GetSelectedCursor ();

						if (cursor == -2 && KickStarter.runtimeInventory.SelectedItem != null)
						{
							if (items [trueIndex] == KickStarter.runtimeInventory.SelectedItem)
							{
								KickStarter.runtimeInventory.SelectItem (items [trueIndex], SelectItemMode.Use);
							}
							else if (AllowInteractions ())
							{
								KickStarter.runtimeInventory.Combine (KickStarter.runtimeInventory.SelectedItem, items [trueIndex]);
							}
						}
						else if ((cursor == -1 && !KickStarter.settingsManager.selectInvWithUnhandled) || !AllowInteractions ())
						{
							KickStarter.runtimeInventory.SelectItem (items [trueIndex], SelectItemMode.Use);
						}
						else if (cursorID > -1)
						{
							KickStarter.runtimeInventory.RunInteraction (items [trueIndex], cursorID);
						}
					}
				}
				else if (interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					if (_mouseState == MouseState.SingleClick)
					{
						if (items.Count <= trueIndex)
						{
							return;
						}

						if (KickStarter.runtimeInventory.SelectedItem == null)
						{
							if (!AllowInteractions ())
							{
								KickStarter.runtimeInventory.SelectItem (items [trueIndex], SelectItemMode.Use);
							}
							else if (KickStarter.cursorManager.lookUseCursorAction == LookUseCursorAction.RightClickCyclesModes && KickStarter.playerCursor.ContextCycleExamine)
							{
								KickStarter.runtimeInventory.Look (items [trueIndex]);
							}
							else
							{
								KickStarter.runtimeInventory.Use (items [trueIndex]);
							}
						}
						else if (AllowInteractions ())
						{
							KickStarter.runtimeInventory.Combine (KickStarter.runtimeInventory.SelectedItem, items [trueIndex]);
						}
					}
					else if (_mouseState == MouseState.RightClick)
					{
						if (KickStarter.runtimeInventory.SelectedItem == null)
						{
							if (items.Count > trueIndex && KickStarter.cursorManager.lookUseCursorAction != LookUseCursorAction.RightClickCyclesModes && AllowInteractions ())
							{
								KickStarter.runtimeInventory.Look (items [trueIndex]);
							}
						}
						else
						{
							KickStarter.runtimeInventory.SetNull ();
						}
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
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments)
			{
				if (Application.isPlaying)
				{
					int[] documentIDs = KickStarter.runtimeDocuments.GetCollectedDocumentIDs ((limitToCategory) ? categoryIDs.ToArray () : null);

					numDocuments = documentIDs.Length;
					numSlots = numDocuments;
					if (numSlots > maxSlots)
					{
						numSlots = maxSlots;
					}

					LimitOffset (numDocuments);

					labels = new string[numSlots];
					textures = new Texture[numSlots];

					int languageNumber = Options.GetLanguage ();
					for (int i=0; i<numSlots; i++)
					{
						int documentID = documentIDs [i + offset];
						Document document = KickStarter.inventoryManager.GetDocument (documentID);

						labels[i] = KickStarter.runtimeLanguages.GetTranslation (document.title,
																					document.titleLineID,
																					languageNumber,
																					document.GetTranslationType (0));

						textures[i] = document.texture;
					}

					if (uiHideStyle == UIHideStyle.DisableObject)
					{
						if (numSlots > numDocuments)
						{
							offset = 0;
							numSlots = numDocuments;
						}
					}
				}
			}
			else if (inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (Application.isPlaying)
				{
					ObjectiveInstance[] objectiveInstances = KickStarter.runtimeObjectives.GetObjectives (objectiveDisplayType);

					numDocuments = objectiveInstances.Length;
					numSlots = numDocuments;
					if (numSlots > maxSlots)
					{
						numSlots = maxSlots;
					}

					LimitOffset (numDocuments);

					labels = new string[numSlots];
					textures = new Texture[numSlots];

					int languageNumber = Options.GetLanguage ();
					for (int i=0; i<numSlots; i++)
					{
						labels[i] = objectiveInstances [i+offset].Objective.GetTitle (languageNumber);
						textures[i] = objectiveInstances [i+offset].Objective.texture;
					}

					if (uiHideStyle == UIHideStyle.DisableObject)
					{
						if (numSlots > numDocuments)
						{
							offset = 0;
							numSlots = numDocuments;
						}
					}
				}
			}
			else
			{
				items = GetItemList ();

				if (inventoryBoxType == AC_InventoryBoxType.HotspotBased)
				{
					if (Application.isPlaying)
					{
						numSlots = Mathf.Clamp (items.Count, 0, maxSlots);
					}
					else
					{
						numSlots = Mathf.Clamp (numSlots, 0, maxSlots);
					}
				}
				else
				{
					numSlots = maxSlots;
				}

				if (uiHideStyle == UIHideStyle.DisableObject)
				{
					if (numSlots > items.Count)
					{
						offset = 0;
						numSlots = items.Count;
					}
				}

				LimitOffset (items.Count);

				labels = new string [numSlots];

				if (Application.isPlaying && uiSlots != null)
				{
					ClearSpriteCache (uiSlots);
				}
			}

			if (!isVisible)
			{
				LimitUISlotVisibility (uiSlots, 0, uiHideStyle, emptySlotTexture);
			}
			base.RecalculateSize (source);
		}
		
		
		private List<InvItem> GetItemList (bool doLimit = true)
		{
			List<InvItem> newItemList = new List<InvItem>();

			if (Application.isPlaying)
			{
				if (inventoryBoxType == AC_InventoryBoxType.HotspotBased)
				{
					if (limitToDefinedInteractions || ForceLimitByReference ())
					{
						newItemList = KickStarter.runtimeInventory.MatchInteractions ();
					}
					else
					{
						newItemList = KickStarter.runtimeInventory.localItems;
					}
				}
				else if (inventoryBoxType == AC_InventoryBoxType.DisplaySelected)
				{
					if (KickStarter.runtimeInventory.SelectedItem != null)
					{
						newItemList.Add (KickStarter.runtimeInventory.SelectedItem);
					}
				}
				else if (inventoryBoxType == AC_InventoryBoxType.DisplayLastSelected)
				{
					if (KickStarter.runtimeInventory.LastSelectedItem != null && KickStarter.runtimeInventory.IsItemCarried (KickStarter.runtimeInventory.LastSelectedItem))
					{
						newItemList.Add (KickStarter.runtimeInventory.LastSelectedItem);
					}
				}
				else if (inventoryBoxType == AC_InventoryBoxType.Container)
				{
					if (overrideContainer != null)
					{
						newItemList = GetItemsFromContainer (overrideContainer);
					}
					else if (KickStarter.playerInput.activeContainer != null)
					{
						newItemList = GetItemsFromContainer (KickStarter.playerInput.activeContainer);
					}
				}
				else
				{
					newItemList = new List<InvItem>();
					foreach (InvItem _item in KickStarter.runtimeInventory.localItems)
					{
						newItemList.Add (_item);
					}
				}

				newItemList = AddExtraNulls (newItemList);
			}
			else
			{
				newItemList = new List<InvItem>();
				if (AdvGame.GetReferences ().inventoryManager)
				{
					foreach (InvItem _item in AdvGame.GetReferences ().inventoryManager.items)
					{
						newItemList.Add (_item);
						if (_item != null)
						{
							_item.recipeSlot = -1;
						}
					}
				}
			}

			if (Application.isPlaying && 
				(inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.CustomScript))
			{
				while (AreAnyItemsInRecipe (newItemList))
				{
					foreach (InvItem _item in newItemList)
					{
						if (_item != null && _item.recipeSlot > -1)
						{
							if (AdvGame.GetReferences ().settingsManager.canReorderItems)
								newItemList [newItemList.IndexOf (_item)] = null;
							else
								newItemList.Remove (_item);
							break;
						}
					}
				}
			}

			if (doLimit && CanBeLimitedByCategory ())
			{
				newItemList = LimitByCategory (newItemList, 0).LimitedItems;
			}

			return newItemList;
		}


		private List<InvItem> AddExtraNulls (List<InvItem> _items)
		{
			if (inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected &&
				inventoryBoxType != AC_InventoryBoxType.DisplaySelected &&
				!limitMaxScroll &&
				_items.Count > 0 &&
				_items.Count % maxSlots != 0)
			{
				while (_items.Count % maxSlots != 0)
				{
					_items.Add (null);
				}
			}
			return _items;
		}


		private List<InvItem> GetItemsFromContainer (Container container)
		{
			List<InvItem> newItemList = new List<InvItem>();
			newItemList.Clear ();
			foreach (ContainerItem containerItem in container.items)
			{
				if (!containerItem.IsEmpty)
				{
					InvItem referencedItem = new InvItem (KickStarter.inventoryManager.GetItem (containerItem.linkedID));
					referencedItem.count = containerItem.count;
					newItemList.Add (referencedItem);
				}
				else if (KickStarter.settingsManager.canReorderItems)
				{
					newItemList.Add (null);
				}
			}
			if (KickStarter.settingsManager.canReorderItems && newItemList.Count < maxSlots-1)
			{
				while (newItemList.Count < maxSlots-1)
				{
					newItemList.Add (null);
				}
			}
			return newItemList;
		}


		private bool CanBeLimitedByCategory ()
		{
			if (inventoryBoxType == AC_InventoryBoxType.Default ||
				inventoryBoxType == AC_InventoryBoxType.CustomScript ||
				inventoryBoxType == AC_InventoryBoxType.DisplaySelected ||
				inventoryBoxType == AC_InventoryBoxType.DisplayLastSelected ||
				inventoryBoxType == AC_InventoryBoxType.CollectedDocuments)
			{
				return true;
			}

			if (inventoryBoxType == AC_InventoryBoxType.HotspotBased && !limitToDefinedInteractions && !ForceLimitByReference ())
			{
				return true;
			}

			return false;
		}


		/**
		 * <summary>Checks if the element's slots can be shifted in a particular direction.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <returns>True if the element's slots can be shifted in the particular direction</returns>
		 */
		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
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
					if ((maxSlots + offset) >= numDocuments)
					{
						return false;
					}
				}
				return true;
			}

			if (items.Count == 0)
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
				if ((maxSlots + offset) >= items.Count)
				{
					return false;
				}
			}
			return true;
		}


		private bool AreAnyItemsInRecipe (List<InvItem> _itemList)
		{
			foreach (InvItem item in _itemList)
			{
				if (item != null && item.recipeSlot >= 0)
				{
					return true;
				}
			}
			return false;
		}


		private LimitedItemList LimitByCategory (List<InvItem> itemsToLimit, int reverseItemIndex)
		{
			int offset = 0;

			List<InvItem> nonLinkedItemsToLimit = new List<InvItem>();
			foreach (InvItem itemToLimit in itemsToLimit)
			{
				nonLinkedItemsToLimit.Add (itemToLimit);
			}

			if (limitToCategory && categoryIDs.Count > 0)
			{
				for (int i=0; i<nonLinkedItemsToLimit.Count; i++)
				{
					if (nonLinkedItemsToLimit[i] != null && !categoryIDs.Contains (nonLinkedItemsToLimit[i].binID))
					{
						if (i <= reverseItemIndex)
						{
							offset ++;
						}

						nonLinkedItemsToLimit.RemoveAt (i);
						i = -1;
					}
				}

				// Bugfix: Remove extra nulls at end in case some where added as a result of re-ordering another menu
				if (nonLinkedItemsToLimit != null && Application.isPlaying)
				{
					nonLinkedItemsToLimit = KickStarter.runtimeInventory.RemoveEmptySlots (nonLinkedItemsToLimit);
				}

				nonLinkedItemsToLimit = AddExtraNulls (nonLinkedItemsToLimit);
			}

			return new LimitedItemList (nonLinkedItemsToLimit, offset);
		}
		

		/**
		 * <summary>Shifts which slots are on display, if the number of slots the element has exceeds the number of slots it can show at once.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <param name = "amount">The amount to shift slots by</param>
		 */
		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (numSlots >= maxSlots)
			{
				if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
				{
					Shift (shiftType, maxSlots, numDocuments, amount);
				}
				else
				{
					Shift (shiftType, maxSlots, items.Count, amount);
				}
			}
		}


		private Texture GetTexture (int itemIndex, bool isActive)
		{
			if (ItemIsSelected (itemIndex))
			{
				switch (KickStarter.settingsManager.selectInventoryDisplay)
				{
					case SelectInventoryDisplay.ShowSelectedGraphic:
						return items[itemIndex].selectedTex;

					case SelectInventoryDisplay.ShowHoverGraphic:
						return items[itemIndex].activeTex;

					default:
						break;
				}
			}
			else if (isActive && KickStarter.settingsManager.activeWhenHover)
			{
				return items[itemIndex].activeTex;
			}
			return items[itemIndex].tex;
		}
		
		
		private void DrawTexture (Rect rect, int itemIndex, bool isActive)
		{
			InvItem _item = items[itemIndex];
			if (_item == null) return;

			Texture tex = null;
			if (Application.isPlaying && KickStarter.runtimeInventory != null && inventoryBoxType != AC_InventoryBoxType.DisplaySelected)
			{
				if (_item == KickStarter.runtimeInventory.highlightItem && _item.activeTex != null)
				{
					KickStarter.runtimeInventory.DrawHighlighted (rect);
					return;
				}

				if (inventoryBoxType != AC_InventoryBoxType.DisplaySelected && inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected)
				{
					tex = GetTexture (itemIndex, isActive);
				}

				if (tex == null)
				{
					tex = _item.tex;
				}
			}
			else if (_item.tex != null)
			{
				tex = _item.tex;
			}

			if (tex != null)
			{
				GUI.DrawTexture (rect, tex, ScaleMode.StretchToFill, true, 0f);
			}
		}


		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "slot">The index number of the slot</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int i, int languageNumber)
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (labels.Length > i)
				{
					return labels[i];
				}
				return "";
			}
			else
			{
				if (items.Count <= (i+offset) || items [i+offset] == null)
				{
					return string.Empty;
				}
				return items [i+offset].GetLabel (languageNumber);
			}
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiSlots != null && slotIndex >= 0 && uiSlots.Length > slotIndex && uiSlots[slotIndex] != null && uiSlots[slotIndex].uiButton != null)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiSlots[slotIndex].uiButton.gameObject);
			}
			return false;
		}


		public override AudioClip GetHoverSound (int slot)
		{
			if (!hoverSoundOverEmptySlots && GetItem (slot) == null) return null;

			return base.GetHoverSound (slot);
		}


		/**
		 * <summary>Gets the inventory item shown in a specific slot</summary>
		 * <param name = "i">The index number of the slot</param>
		 * <returns>The inventory item shown in the slot</returns>
		 */
		public InvItem GetItem (int i)
		{
			if (items.Count <= (i+offset) || items [i+offset] == null)
			{
				return null;
			}

			return items [i+offset];
		}


		private string GetCount (int i)
		{
			if (inventoryItemCountDisplay == InventoryItemCountDisplay.Never) return string.Empty;

			if (Application.isPlaying)
			{
				if (items.Count <= (i+offset) || items [i+offset] == null)
				{
					return string.Empty;
				}

				if (items [i+offset].count < 2 && inventoryItemCountDisplay == InventoryItemCountDisplay.OnlyIfMultiple)
				{
					return string.Empty;
				}

				if (ItemIsSelected (i+offset) && items [i+offset].CanSelectSingle ())
				{
					return (items [i+offset].count-1).ToString ();
				}

				return items [i + offset].count.ToString ();
			}

			if (items[i+offset].canCarryMultiple && !items[i+offset].useSeparateSlots)
			{
				if (items[i+offset].count > 1 || inventoryItemCountDisplay == InventoryItemCountDisplay.Always)
				{
					return items[i+offset].count.ToString ();
				}
			}
			return string.Empty;
		}


		/**
		 * Re-sets the "shift" offset, so that the first InvItem shown is the first InvItem in items.
		 */
		public void ResetOffset ()
		{
			offset = 0;
		}
		
		
		protected override void AutoSize ()
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (!Application.isPlaying)
				{
					#if UNITY_EDITOR
					if (displayType == ConversationDisplayType.IconOnly)
					{
						AutoSize (new GUIContent (testIcon));
					}
					else
					{
						if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments)
						{
							AutoSize (new GUIContent ("Document 0"));
						}
						else
						{
							AutoSize (new GUIContent ("Objective 0"));
						}
					}
					return;
					#endif
				}

				if (numDocuments > 0)
				{
					if (displayType == ConversationDisplayType.IconOnly)
					{
						AutoSize (new GUIContent (textures[0]));
					}
					else
					{
						AutoSize (new GUIContent (labels[0]));
					}
					return;
				}
			}
			else if (items.Count > 0)
			{
				foreach (InvItem _item in items)
				{
					if (_item != null)
					{
						if (displayType == ConversationDisplayType.IconOnly)
						{
							AutoSize (new GUIContent (_item.tex));
						}
						else if (displayType == ConversationDisplayType.TextOnly)
						{
							AutoSize (new GUIContent (_item.label));
						}
						return;
					}
				}
			}
			AutoSize (GUIContent.none);
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on, if inventoryBoxType = AC_InventoryBoxType.Container.</summary>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 * <param name = "_slot">The index number of the slot that was clicked on</param>
		 */
		public void ClickContainer (MouseState _mouseState, int _slot)
		{
			Container container = (overrideContainer != null) ? overrideContainer : KickStarter.playerInput.activeContainer;

			if (container == null || KickStarter.runtimeInventory == null) return;

			KickStarter.runtimeInventory.SetFont (font, GetFontSize (), fontColor, textEffects);

			if (_mouseState == MouseState.SingleClick)
			{
				if (KickStarter.runtimeInventory.SelectedItem == null)
				{
					// Take an item from the Container

					if (container.items.Count > (_slot+offset) && !container.items [_slot+offset].IsEmpty)
					{
						ContainerItem containerItem = container.items [_slot + offset];
						ContainerItem preservedContainerItem = new ContainerItem (containerItem);

						// Prevent if player is already carrying one, and multiple can't be carried
						InvItem invItem = KickStarter.inventoryManager.GetItem (containerItem.linkedID);
						if (KickStarter.runtimeInventory.IsCarryingItem (invItem.id) && !invItem.canCarryMultiple)
						{
							KickStarter.eventManager.Call_OnUseContainerFail (container, containerItem);
							return;
						}

						if (containerSelectMode == ContainerSelectMode.MoveToInventory ||
							containerSelectMode == ContainerSelectMode.MoveToInventoryAndSelect)
						{
							if (KickStarter.runtimeInventory.CanTransferContainerItemsToInventory (containerItem))
							{
								bool selectItem = (containerSelectMode == ContainerSelectMode.MoveToInventoryAndSelect);

								if (KickStarter.inventoryManager.GetItem (containerItem.linkedID).CanSelectSingle (containerItem.count))
								{
									// Only take one
									KickStarter.runtimeInventory.Add (containerItem.linkedID, 1, selectItem, -1);
									container.items [_slot+offset].count -= 1;
								}
								else
								{
									KickStarter.runtimeInventory.Add (containerItem.linkedID, containerItem.count, selectItem, -1);
									container.Remove (containerItem);
								}
								KickStarter.eventManager.Call_OnUseContainer (false, container, preservedContainerItem);
							}
							else
							{
								return;
							}
						}
						else if (containerSelectMode == ContainerSelectMode.SelectItemOnly)
						{
							KickStarter.runtimeInventory.SelectItem (container, containerItem);
						}
					}
				}
				else
				{
					// Placing an item inside the container

					if (container.maxSlots > 0 && container.FilledSlots >= container.maxSlots)
					{
						// Can't hold any more
						KickStarter.runtimeInventory.SetNull ();
						return;
					}

					if (KickStarter.runtimeInventory.selectedContainerItem != null)
					{
						// Transfer from container
						int index = KickStarter.runtimeInventory.selectedContainerItemContainer.items.IndexOf (KickStarter.runtimeInventory.selectedContainerItem);
						if (KickStarter.settingsManager.canReorderItems)
						{
							KickStarter.runtimeInventory.selectedContainerItemContainer.items[index].IsEmpty = true;
						}
						else
						{
							KickStarter.runtimeInventory.selectedContainerItemContainer.Remove (KickStarter.runtimeInventory.selectedContainerItem);
						}

						container.InsertAt (KickStarter.runtimeInventory.SelectedItem, _slot+offset, KickStarter.runtimeInventory.selectedContainerItem.count);

						if (KickStarter.runtimeInventory.selectedContainerItemContainer != container)
						{
							KickStarter.eventManager.Call_OnUseContainer (true, container, KickStarter.runtimeInventory.selectedContainerItem);
						}
						KickStarter.runtimeInventory.SetNull ();
					}
					else
					{
						// Transfer from inventory

						int numToChange = (KickStarter.runtimeInventory.SelectedItem.CanSelectSingle ()) ? 1 : 0;
						ContainerItem containerItem = container.InsertAt (KickStarter.runtimeInventory.SelectedItem, _slot+offset, numToChange);
						if (containerItem != null && !containerItem.IsEmpty)
						{
							KickStarter.runtimeInventory.Remove (KickStarter.runtimeInventory.SelectedItem, numToChange);
							KickStarter.eventManager.Call_OnUseContainer (true, container, containerItem);
						}
					}
				}
			}

			else if (_mouseState == MouseState.RightClick)
			{
				if (KickStarter.runtimeInventory.SelectedItem != null)
				{
					KickStarter.runtimeInventory.SetNull ();
				}
			}
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_slot">The index number of ths slot that was clicked</param>
		 * <param name = "_mouseState The state of the mouse button</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return;
			}

			if (_mouseState == MouseState.SingleClick)
			{
				KickStarter.runtimeInventory.lastClickedItem = GetItem (_slot);
			}

			switch (inventoryBoxType)
			{
				case AC_InventoryBoxType.CollectedDocuments:
					if (autoOpenDocument)
					{
						Document document = GetDocument (_slot);
						KickStarter.runtimeDocuments.OpenDocument (document);
					}
					if (actionListOnClick != null)
					{
						actionListOnClick.Interact ();
					}
					break;

				case AC_InventoryBoxType.CustomScript:
					MenuSystem.OnElementClick (_menu, this, _slot, (int)_mouseState);
					break;

				case AC_InventoryBoxType.Objectives:
					if (autoOpenDocument)
					{
						ObjectiveInstance objectiveInstance = KickStarter.runtimeObjectives.GetObjectives (objectiveDisplayType)[_slot + offset];
						KickStarter.runtimeObjectives.SelectedObjective = objectiveInstance;
					}
					if (actionListOnClick != null)
					{
						actionListOnClick.Interact ();
					}
					break;

				default:
					KickStarter.runtimeInventory.ProcessInventoryBoxClick (_menu, this, _slot, _mouseState);
					break;
			}

			base.ProcessClick (_menu, _slot, _mouseState);
		}


		/**
		 * <summary>Gets the Document associated with a given slot</summary>
		 * <param name = "slotIndex">The element's slot index number</param>
		 * <returns>The Document assoicated with the slot</returns>
		 */
		public Document GetDocument (int slotIndex)
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments)
			{
				int documentID = KickStarter.runtimeDocuments.GetCollectedDocumentIDs ((limitToCategory) ? categoryIDs.ToArray () : null) [slotIndex + offset];
				return KickStarter.inventoryManager.GetDocument (documentID);
			}
			return null;
		}


		/**
		 * <summary>Gets the Document associated with a given slot</summary>
		 * <param name = "slotIndex">The element's slot index number</param>
		 * <returns>The Document assoicated with the slot</returns>
		 */
		public ObjectiveInstance GetObjective (int slotIndex)
		{
			if (inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				ObjectiveInstance[] allObjectives = KickStarter.runtimeObjectives.GetObjectives (objectiveDisplayType);
				return allObjectives [slotIndex+offset];
			}
			return null;
		}


		/**
		 * <summary>Gets the slot index number that a given InvItem (inventory item) appears in.</summary>
		 * <param name = "itemID">The ID number of the InvItem to search for</param>
		 * <returns>The slot index number that the inventory item appears in</returns>
		 */
		public int GetItemSlot (int itemID)
		{
			foreach (InvItem invItem in items)
			{
				if (invItem != null && invItem.id == itemID)
				{
					return items.IndexOf (invItem) - offset;
				}
			}
			return 0;
		}


		private bool ForceLimitByReference ()
		{
			if (KickStarter.settingsManager != null && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
				KickStarter.settingsManager.cycleInventoryCursors &&
				(KickStarter.settingsManager.selectInteractions == SelectInteractions.CyclingCursorAndClickingHotspot || KickStarter.settingsManager.selectInteractions == SelectInteractions.CyclingMenuAndClickingHotspot))
			{
				return true;
			}
			return false;
		}


		/**
		 * If set, and inventoryBoxType = AC_InventoryBoxType.Container, then this Container will be used instead of the global 'active' one.  Note that its Menu's 'Appear type' should not be set to 'On Container'.
		 */
		public Container OverrideContainer
		{
			set
			{
				overrideContainer = value;
			}
		}


		private struct LimitedItemList
		{

			List<InvItem> limitedItems;
			int offset;

	
			public LimitedItemList (List<InvItem> _limitedItems, int _offset)
			{
				limitedItems = _limitedItems;
				offset = _offset;
			}


			public List<InvItem> LimitedItems
			{
				get
				{
					return limitedItems;
				}
			}


			public int Offset
			{
				get
				{
					return offset;
				}
			}

		}

	}

}