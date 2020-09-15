/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"CursorManager.cs"
 * 
 *	This script handles the "Cursor" tab of the main wizard.
 *	It is used to define cursor icons and the method in which
 *	interactions are triggered by the player.
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
	 * Handles the "Cursor" tab of the Game Editor window.
	 * All possible cursors that the mouse can have (excluding inventory items) are defined here, as are the various ways in which these cursors are displayed.
	 */
	[System.Serializable]
	public class CursorManager : ScriptableObject
	{

		/** The rendering method of all cursors (Software, Hardware) */
		public CursorRendering cursorRendering = CursorRendering.Software;
		/** The rule that defines when the main cursor is shown (Always, Never, OnlyWhenPaused) */
		public CursorDisplay cursorDisplay = CursorDisplay.Always;
		/** If True, then the system's default hardware cursor will replaced with a custom one */
		public bool allowMainCursor = false;
		/** If True, and cursorRendering = CursorRendering.Software, the system cursor will be locked when the AC cursor is (this is always true when using Hardware cursor rendering) */
		public bool lockSystemCursor = true;
		/** If True, then the cursor will always be kept within the boundary of the game window */
		public bool keepCursorWithinScreen = true;
		#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		/** If True, then the system cursor will be confined to the game window */
		public bool confineSystemCursor = false;
		#endif

		/** If True, then a separate cursor will display when in "walk mode" */
		public bool allowWalkCursor = false;
		/** If True, then a prefix can be added to the Hotspot label when in "walk mode" */
		public bool addWalkPrefix = false;
		/** The prefix to add to the Hotspot label when in "walk mode", if addWalkPrefix = True */
		public HotspotPrefix walkPrefix = new HotspotPrefix ("Walk to");

		/** If True, then the Cursor's interaction verb will prefix the Hotspot label when hovering over Hotspots */
		public bool addHotspotPrefix = false;
		/** If True, then the cursor will be controlled by the current Interaction when hovering over a Hotspot */
		public bool allowInteractionCursor = false;
		/** If True, then the cursor will be controlled by the current Interaction when hovering over an inventory item (see InvItem) */
		public bool allowInteractionCursorForInventory = false;
		/** If True, then cursor modes can by clicked by right-clicking, if interactionMethod = AC_InteractionMethod.ChooseInteractionThenHotspot in SettingsManager */
		public bool cycleCursors = false;
		/** If True, and interactionMethod = AC_InteractionMethod.ChooseInteractionThenHotspot in SettingsManager, then animated cursors will only animate if the cursor is over a Hotspot */
		public bool onlyAnimateOverHotspots = false;
		/** If True, then left-clicking a Hotspot will examine it if no "use" Interaction exists (if interactionMethod = AC_InteractionMethod.ContextSensitive in SettingsManager) */
		public bool leftClickExamine = false;
		/** If True, and allowWalkCursor = True, then the walk cursor will only show when the cursor is hovering over a NavMesh */
		public bool onlyWalkWhenOverNavMesh = false;
		/** If True, then Hotspot labels will not show when an inventory item is selected unless the cursor is over another inventory item or a Hotspot */
		public bool onlyShowInventoryLabelOverHotspots = false;
		/** The size of selected inventory item graphics when used as a cursor */
		public float inventoryCursorSize = 0.06f;
		/** If True, and interactionMethod = AC_InteractionMethod.ChooseInteractionThenHotspot in SettingsManager, then the player can switch the active "interaction" icon by invoking a specific input */
		public bool allowIconInput = true;

		/** The cursor while the game is running a gameplay-blocking cutscene */
		public CursorIconBase waitIcon = new CursorIcon ();
		/** The game's default cursor */
		public CursorIconBase pointerIcon = new CursorIcon ();
		/** The cursor when in "walk mode", if allowWalkCursor = True */
		public CursorIconBase walkIcon = new CursorIcon ();
		/** The cursor when hovering over a Hotspot */
		public CursorIconBase mouseOverIcon = new CursorIcon ();

		/** What happens to the cursor when an inventory item is selected (ChangeCursor, ChangeHotspotLabel, ChangeCursorAndHotspotLabel) */
		public InventoryHandling inventoryHandling = InventoryHandling.ChangeCursor;
		/** The "Use" in the syntax "Use item on Hotspot" */
		public HotspotPrefix hotspotPrefix1 = new HotspotPrefix ("Use");
		/** The "on" in the syntax "Use item on Hotspot" */
		public HotspotPrefix hotspotPrefix2 = new HotspotPrefix ("on");
		/** The "Give" in the syntax "Give item to NPC" */
		public HotspotPrefix hotspotPrefix3 = new HotspotPrefix ("Give");
		/** The "to" in the syntax "Give item to NPC" */
		public HotspotPrefix hotspotPrefix4 = new HotspotPrefix ("to");

		/** A List of all CursorIcon instances that represent the various Interaction types */
		public List<CursorIcon> cursorIcons = new List<CursorIcon>();
		/** A List of ActionListAsset files that get run when an unhandled Interaction is triggered */
		public List<ActionListAsset> unhandledCursorInteractions = new List<ActionListAsset>();
		/** If True, the Hotspot clicked on to initiate unhandledCursorInteractions will be sent as a parameter to the ActionListAsset */
		public bool passUnhandledHotspotAsParameter;

		/** What happens when hovering over a Hotspot that has both a Use and Examine Interaction (DisplayUseIcon, DisplayBothSideBySide, RightClickCyclesModes) */
		public LookUseCursorAction lookUseCursorAction = LookUseCursorAction.DisplayBothSideBySide;
		/** The ID number of the CursorIcon (in cursorIcons) that represents the "Examine" Interaction */
		public int lookCursor_ID = 0;

		/** If True, the cursor will be hidden when manipulating draggable objects */
		public bool hideCursorWhenDraggingMoveables = true;

		#if UNITY_EDITOR
		public bool forceCursorInEditor = true;

		private bool showSettings = true;
		private bool showMainCursor = true;
		private bool showWalkCursor = true;
		private bool showHotspotCursor = true;
		private bool showInventoryCursor = true;
		private bool showInteractionIcons = true;
		private bool showCutsceneCursor = true;
		#endif

		private SettingsManager settingsManager;
		
		
		#if UNITY_EDITOR

		/**
		 * Shows the GUI.
		 */
		public void ShowGUI ()
		{
			settingsManager = AdvGame.GetReferences().settingsManager;

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSettings = CustomGUILayout.ToggleHeader (showSettings, "Global cursor settings");
			if (showSettings)
			{
				cursorRendering = (CursorRendering) CustomGUILayout.EnumPopup ("Cursor rendering:", cursorRendering, "AC.KickStarter.cursorManager.cursorRendering", "The rendering method of all cursors");
				if (cursorRendering == CursorRendering.Software)
				{
					lockSystemCursor = CustomGUILayout.ToggleLeft ("Lock system cursor when locking AC cursor?", lockSystemCursor, "AC.KickStarter.cursorManager.lockSystemCursor", "If True, the system cursor will be locked when the AC cursor is");
				}
				forceCursorInEditor = CustomGUILayout.ToggleLeft ("Always show system cursor in Editor?", forceCursorInEditor, "AC.KickStarter.cursorManager.forceCursorInEditor");

				if (cursorRendering == CursorRendering.Software)
				{
					keepCursorWithinScreen = CustomGUILayout.ToggleLeft ("Always keep cursor within screen boundary?", keepCursorWithinScreen, "AC.KickStarter.cursorManager.keepCursorWithinScreen", "If True, then the cursor will always be kept within the boundary of the game window");
				}
				else
				{
					keepCursorWithinScreen = CustomGUILayout.ToggleLeft ("Always keep perceived cursor within screen boundary?", keepCursorWithinScreen, "AC.KickStarter.cursorManager.keepCursorWithinScreen", "If True, then the cursor will always be kept within the boundary of the game window");
				}

				#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
				confineSystemCursor = CustomGUILayout.ToggleLeft ("Confine system cursor to game window?", confineSystemCursor, "AC.KickStarter.cursorManager.confineSystemCursor", "If True, then the system cursor will be confined to the game window");
				#endif

				hideCursorWhenDraggingMoveables = CustomGUILayout.ToggleLeft ("Hide cursor when manipulating Draggables?", hideCursorWhenDraggingMoveables, "AC.KickStarter.cursorManager.hideCursorWhenDraggingMoveables", "If True, the cursor will be hidden when manipulating Draggable objects");
			}
			EditorGUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showMainCursor = CustomGUILayout.ToggleHeader (showMainCursor, "Main cursor settings");
			if (showMainCursor)
			{
				cursorDisplay = (CursorDisplay) CustomGUILayout.EnumPopup ("Display cursor:", cursorDisplay, "AC.KickStarter.cursorManager.cursorDisplay", "The rule that defines when the main cursor is shown");
				if (cursorDisplay != CursorDisplay.Never)
				{
					allowMainCursor = CustomGUILayout.Toggle ("Replace mouse cursor?", allowMainCursor, "AC.KickStarter.cursorManager.allowMainCursor", "If True, then the system's default hardware cursor will replaced with a custom one");
					if (allowMainCursor || (settingsManager && settingsManager.inputMethod == InputMethod.KeyboardOrController))
					{
						IconBaseGUI ("", pointerIcon, "AC.KickStarter.cursorManager.pointerIcon", "The game's default cursor", false);
					}
				}
			}
			EditorGUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showWalkCursor = CustomGUILayout.ToggleHeader (showWalkCursor, "Walk cursor");
			if (showWalkCursor)
			{
				if (allowMainCursor)
				{
					allowWalkCursor = CustomGUILayout.Toggle ("Provide walk cursor?", allowWalkCursor, "AC.KickStarter.cursorManager.allowWalkCursor", "If True, then a separate cursor will display when in 'walk mode'");
					if (allowWalkCursor)
					{
						if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && allowIconInput)
						{
							EditorGUILayout.LabelField ("Input button:", "Icon_Walk");
						}
						IconBaseGUI ("", walkIcon, "AC.KickStarter.cursorManager.walkIcon", "The cursor when in 'walk mode'");
						onlyWalkWhenOverNavMesh = CustomGUILayout.ToggleLeft ("Only show 'Walk' Cursor when over NavMesh?", onlyWalkWhenOverNavMesh, "AC.KickStarter.cursorManager.onlyWalkWhenOverNavMesh", "If True, then the walk cursor will only show when the cursor is hovering over a NavMesh");
					}
				}
				addWalkPrefix = CustomGUILayout.Toggle ("Prefix cursor labels?", addWalkPrefix, "AC.KickStarter.cursorManager.addWalkPrefix", "If True, then a prefix can be added to the Hotspot label when in 'walk mode'");
				if (addWalkPrefix)
				{
					walkPrefix.label = CustomGUILayout.TextField ("Walk prefix:", walkPrefix.label, "AC.KickStarter.cursorManager.walkPrefix");
				}
			}
			EditorGUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showHotspotCursor = CustomGUILayout.ToggleHeader (showHotspotCursor, "Hotspot cursor");
			if (showHotspotCursor)
			{
				addHotspotPrefix = CustomGUILayout.Toggle ("Prefix cursor labels?", addHotspotPrefix, "AC.KickStarter.cursorManager.addHotspotPrefix", "If True, then the Cursor's interaction verb will prefix the Hotspot label when hovering over Hotspots");
				IconBaseGUI ("", mouseOverIcon, "AC.KickStarter.cursorManager.mouseOverIcon");
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showInventoryCursor = CustomGUILayout.ToggleHeader (showInventoryCursor, "Inventory cursor");
			if (showInventoryCursor)
			{
				inventoryHandling = (InventoryHandling) CustomGUILayout.EnumPopup ("When inventory selected:", inventoryHandling, "AC.KickStarter.cursorManager.inventoryHandling", "What happens to the cursor when an inventory item is selected");
				if (inventoryHandling != InventoryHandling.ChangeCursor)
				{
					onlyShowInventoryLabelOverHotspots = CustomGUILayout.ToggleLeft ("Only show label when over Hotspots and Inventory?", onlyShowInventoryLabelOverHotspots, "AC.KickStarter.cursorManager.onlyShowInventoryLabelOverHotspots", "If True, then Hotspot labels will not show when an inventory item is selected unless the cursor is over another inventory item or a Hotspot");
				}
				if (inventoryHandling != InventoryHandling.ChangeHotspotLabel)
				{
					inventoryCursorSize = CustomGUILayout.FloatField ("Inventory cursor size:", inventoryCursorSize, "AC.KickStarter.cursorManager.inventoryCursorSize", "The size of selected inventory item graphics when used as a cursor");
				}
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Use syntax:", GUILayout.Width (100f));
				hotspotPrefix1.label = CustomGUILayout.TextField (hotspotPrefix1.label, GUILayout.MaxWidth (80f), "AC.KickStarter.cursorManager.hotspotPrefix1");
				EditorGUILayout.LabelField ("(item)", GUILayout.MaxWidth (40f));
				hotspotPrefix2.label = CustomGUILayout.TextField (hotspotPrefix2.label, GUILayout.MaxWidth (80f), "AC.KickStarter.cursorManager.hotspotPrefix2");
				EditorGUILayout.LabelField ("(hotspot)", GUILayout.MaxWidth (55f));
				EditorGUILayout.EndHorizontal ();
				if (AdvGame.GetReferences ().settingsManager && AdvGame.GetReferences ().settingsManager.CanGiveItems ())
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Give syntax:", GUILayout.Width (100f));
					hotspotPrefix3.label = CustomGUILayout.TextField (hotspotPrefix3.label, GUILayout.MaxWidth (80f), "AC.KickStarter.cursorManager.hotspotPrefix3");
					EditorGUILayout.LabelField ("(item)", GUILayout.MaxWidth (40f));
					hotspotPrefix4.label = CustomGUILayout.TextField (hotspotPrefix4.label, GUILayout.MaxWidth (80f), "AC.KickStarter.cursorManager.hotspotPrefix4");
					EditorGUILayout.LabelField ("(hotspot)", GUILayout.MaxWidth (55f));
					EditorGUILayout.EndHorizontal ();
				}
			}
			EditorGUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showInteractionIcons = CustomGUILayout.ToggleHeader (showInteractionIcons, "Interaction icons");
			if (showInteractionIcons)
			{
				if (settingsManager == null || settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					allowInteractionCursor = CustomGUILayout.ToggleLeft ("Change cursor based on Interaction?", allowInteractionCursor, "AC.KickStarter.cursorManager.allowInteractionCursor", "If True, then the cursor will be controlled by the current Interaction when hovering over a Hotspot");
					if (allowInteractionCursor && (settingsManager == null || settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive))
					{
						allowInteractionCursorForInventory = CustomGUILayout.ToggleLeft ("Change when over Inventory items too?", allowInteractionCursorForInventory, "AC.KickStarter.cursorManager.allowInteractionCursorForInventory", "If True, then the cursor will be controlled by the current Interaction when hovering over an inventory item (see InvItem)");
					}
					if (settingsManager && settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
					{
						cycleCursors = CustomGUILayout.ToggleLeft ("Cycle Interactions with right-click?", cycleCursors, "AC.KickStarter.cursorManager.cycleCursors", "If True, then cursor modes can by clicked by right-clicking");
						allowIconInput = CustomGUILayout.ToggleLeft ("Set Interaction with specific inputs?", allowIconInput, "AC.KickStarter.cursorManager.allowIconInput", "then the player can switch the active icon by invoking a specific input");
						onlyAnimateOverHotspots = CustomGUILayout.ToggleLeft ("Only animate icons when over Hotspots?", onlyAnimateOverHotspots, "AC.KickStarter.cursorManager.onlyAnimateOverHotspots", "If True, then animated cursors will only animate if the cursor is over a Hotspot");
					}
				}
				
				IconsGUI ();
			
				EditorGUILayout.Space ();
			
				if (settingsManager == null || settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					LookIconGUI ();
				}
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCutsceneCursor = CustomGUILayout.ToggleHeader (showCutsceneCursor, "Cutscene cursor");
			if (showCutsceneCursor)
			{
				IconBaseGUI ("", waitIcon, "AC.KickStarter.cursorManager.waitIcon", "The cursor while the game is running a gameplay-blocking cutscene");
			}
			EditorGUILayout.EndVertical ();

			if (GUI.changed)
			{
				EditorUtility.SetDirty (this);
			}
		}


		private int iconSideMenu;
		private void SideMenu (int i)
		{
			GenericMenu menu = new GenericMenu ();
			iconSideMenu = i;

			menu.AddItem (new GUIContent ("Insert after"), false, MenuCallback, "Insert after");
			menu.AddItem (new GUIContent ("Delete"), false, MenuCallback, "Delete");

			menu.ShowAsContext ();
		}


		private void MenuCallback (object obj)
		{
			if (iconSideMenu >= 0)
			{
				int i = iconSideMenu;

				switch (obj.ToString ())
				{
				case "Insert after":
					Undo.RecordObject (this, "Add icon");
					cursorIcons.Insert (i+1, new CursorIcon (GetIDArray ()));
					unhandledCursorInteractions.Insert (i+1, null);
					break;

				case "Delete":
					Undo.RecordObject (this, "Delete icon");
					cursorIcons.RemoveAt (i);
					unhandledCursorInteractions.RemoveAt (i);
					break;
				}
			}
			
			iconSideMenu = -1;
		}

		
		private void IconsGUI ()
		{
			// Make sure unhandledCursorInteractions is the same length as cursorIcons
			while (unhandledCursorInteractions.Count < cursorIcons.Count)
			{
				unhandledCursorInteractions.Add (null);
			}
			while (unhandledCursorInteractions.Count > cursorIcons.Count)
			{
				unhandledCursorInteractions.RemoveAt (unhandledCursorInteractions.Count + 1);
			}

			// List icons
			foreach (CursorIcon _cursorIcon in cursorIcons)
			{
				int i = cursorIcons.IndexOf (_cursorIcon);
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Icon ID:", GUILayout.MaxWidth (145));
				EditorGUILayout.LabelField (_cursorIcon.id.ToString (), GUILayout.MaxWidth (120));

				GUILayout.FlexibleSpace ();

				if (GUILayout.Button ("", CustomStyles.IconCog))
				{
					SideMenu (i);
				}

				EditorGUILayout.EndHorizontal ();

				_cursorIcon.label = CustomGUILayout.TextField ("Label:", _cursorIcon.label, "AC.KickStarter.cursorManager.GetCursorIconFromID (" + i + ").label", "The display name of the icon");
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && allowIconInput)
				{
					EditorGUILayout.LabelField ("Input button:", _cursorIcon.GetButtonName ());
				}
				_cursorIcon.ShowGUI (true, true, "Texture:", cursorRendering, "AC.KickStarter.cursorManager.GetCursorIconFromID (" + i + ")", "The icon's texture");

				if (AllowUnhandledIcons ())
				{
					string autoName = _cursorIcon.label + "_Unhandled_Interaction";
					unhandledCursorInteractions[i] = ActionListAssetMenu.AssetGUI ("Unhandled interaction:", unhandledCursorInteractions[i], autoName, "AC.KickStarter.cursorManager.unhandledCursorInteractions[" + i + "]", "An ActionList asset that gets run when an unhandled Interaction is triggered");
				}

				if (settingsManager != null && settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					_cursorIcon.dontCycle = CustomGUILayout.Toggle ("Leave out of Cursor cycle?", _cursorIcon.dontCycle, "AC.KickStarter.cursorManager.GetCursorIconFromID (" + i + ").dontCycle", "If True, then the cursor will be left out of the cycle when right-clicking");
				}
			}

			if (GUILayout.Button("Create new icon"))
			{
				Undo.RecordObject (this, "Add icon");
				cursorIcons.Add (new CursorIcon (GetIDArray ()));
			}

			passUnhandledHotspotAsParameter = CustomGUILayout.ToggleLeft ("Pass Hotspot as GameObject parameter?", passUnhandledHotspotAsParameter, "AC.KickStarter.cursorManager.passUnhandledHotspotAsParameter", "If True, the Hotspot clicked on to initiate unhandled Interactions will be sent as a parameter to the ActionList asset");
			if (passUnhandledHotspotAsParameter)
			{
				EditorGUILayout.HelpBox ("The Hotspot will be set as the Unhandled interaction's first parameter, which must be set to type 'GameObject'.", MessageType.Info);
			}
		}


		private void LookIconGUI ()
		{
			if (cursorIcons.Count > 0)
			{
				int lookCursor_int = GetIntFromID (lookCursor_ID);
				lookCursor_int = CustomGUILayout.Popup ("Examine icon:", lookCursor_int, GetLabelsArray (), "AC.KickStarter.cursorManager.lookCursor_ID", "The Cursor that represents the 'Examine' Interaction");
				lookCursor_ID = cursorIcons[lookCursor_int].id;

				EditorGUILayout.LabelField (new GUIContent ("When Use and Examine interactions are both available:", "What happens when hovering over a Hotspot that has both a Use and Examine Interaction"));
				lookUseCursorAction = (LookUseCursorAction) CustomGUILayout.EnumPopup (" ", lookUseCursorAction, "AC.KickStarter.cursorManager.lookUseCursorAction");
				if (cursorRendering == CursorRendering.Hardware && lookUseCursorAction == LookUseCursorAction.DisplayBothSideBySide)
				{
					EditorGUILayout.HelpBox ("This mode is not available with Hardward cursor rendering.", MessageType.Warning);
				}

				if (lookUseCursorAction != LookUseCursorAction.RightClickCyclesModes)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (new GUIContent ("Left-click to examine when no use interaction exists?", "If True, then left-clicking a Hotspot will examine it if no 'Use' Interaction exists"), GUILayout.Width (300f));
					leftClickExamine = CustomGUILayout.Toggle (leftClickExamine, "AC.KickStarter.cursorManager.leftClickExamine");
					EditorGUILayout.EndHorizontal ();
				}
			}
		}


		private void IconBaseGUI (string fieldLabel, CursorIconBase icon, string apiPrefix, string tooltip = "", bool includeAlwaysAnimate = true)
		{
			if (fieldLabel != "" && fieldLabel.Length > 0)
				EditorGUILayout.LabelField (fieldLabel,  CustomStyles.subHeader);

			icon.ShowGUI (true, includeAlwaysAnimate, "Texture:", cursorRendering, apiPrefix, tooltip);
			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
		}

		#endif


		/**
		 * <summary>Checks if the current settings allow for unhandled variants of each cursor icon to be available.</summary>
		 * <returns>Tr if the current settings allow for unhandled variants of each cursor icon to be available.</returns>
		 */
		public bool AllowUnhandledIcons ()
		{
			if (KickStarter.settingsManager != null)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					return true;
				}
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.settingsManager.selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot && !KickStarter.settingsManager.autoHideInteractionIcons)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Gets an array of the CursorIcon labels defined in cursorIcons.</summary>
		 * <param name = "includeNone">If True, then the array will begin with a (none) option.</param>
		 * <returns>An array of the CursorIcon labels defined in cursorIcons</returns>
		 */
		public string[] GetLabelsArray (bool includeNone = false)
		{
			List<string> iconLabels = new List<string>();
			if (includeNone)
			{
				iconLabels.Add ("(None)");
			}
			foreach (CursorIcon cursorIcon in cursorIcons)
			{
				iconLabels.Add (cursorIcon.id.ToString () + ": " + cursorIcon.label);
			}
			return iconLabels.ToArray();
		}
		

		/**
		 * <summary>Gets a label of the CursorIcon defined in cursorIcons.</summary>
		 * <param name = "_ID">The ID number of the CursorIcon to find</param>
		 * <param name = "languageNumber">The index number of the language to get the label in</param>
		 * <returns>The label of the CursorIcon</returns>
		 */
		public string GetLabelFromID (int _ID, int languageNumber)
		{
			foreach (CursorIcon cursorIcon in cursorIcons)
			{
				if (cursorIcon.id == _ID)
				{
					if (Application.isPlaying)
					{
						return KickStarter.runtimeLanguages.GetTranslation (cursorIcon.label, cursorIcon.lineID, languageNumber, AC_TextType.CursorIcon);
					}
					return cursorIcon.label;
				}
			}
			return string.Empty;
		}
		

		/**
		 * <summary>Gets a CursorIcon defined in cursorIcons.</summary>
		 * <param name = "_ID">The ID number of the CursorIcon to find</param>
		 * <returns>The CursorIcon</returns>
		 */
		public CursorIcon GetCursorIconFromID (int _ID)
		{
			foreach (CursorIcon cursorIcon in cursorIcons)
			{
				if (cursorIcon.id == _ID)
				{
					return (cursorIcon);
				}
			}
			return null;
		}
		

		/**
		 * <summary>Gets the index number (in cursorIcons) of a CursorIcon.</summary>
		 * <param name = "_ID">The ID number of the CursorIcon to find</param>
		 * <returns>The index number (in cursorIcons) of the CursorIcon</returns>
		 */
		public int GetIntFromID (int _ID)
		{
			int i = 0;
			int requestedInt = -1;
			
			foreach (CursorIcon cursorIcon in cursorIcons)
			{
				if (cursorIcon.id == _ID)
				{
					requestedInt = i;
				}
				
				i++;
			}
			
			if (requestedInt == -1)
			{
				// Wasn't found (icon was deleted?), so revert to zero
				requestedInt = 0;
			}
		
			return requestedInt;
		}


		/**
		 * <summary>Gets the ActionListAsset that is used as a CursorIcon's unhandled event.</summary>
		 * <param name = "_ID">The ID number of the CursorIcon to find</param>
		 * <returns>The ActionListAsset that is used as the CursorIcon's unhandled event</returns>
		 */
		public ActionListAsset GetUnhandledInteraction (int _ID)
		{
			if (AllowUnhandledIcons ())
			{
				foreach (CursorIcon cursorIcon in cursorIcons)
				{
					if (cursorIcon.id == _ID)
					{
						int i = cursorIcons.IndexOf (cursorIcon);
						if (unhandledCursorInteractions.Count > i)
						{
							return unhandledCursorInteractions [i];
						}
						return null;
					}
				}
			}
			return null;
		}
		
		
		private int[] GetIDArray ()
		{
			// Returns a list of id's in the list
			
			List<int> idArray = new List<int>();
			
			foreach (CursorIcon cursorIcon in cursorIcons)
			{
				idArray.Add (cursorIcon.id);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}

	}

}