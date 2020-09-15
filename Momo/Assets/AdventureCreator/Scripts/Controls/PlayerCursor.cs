/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"PlayerCursor.cs"
 * 
 *	This script displays a cursor graphic on the screen.
 *	PlayerInput decides if this should be at the mouse position,
 *	or a position based on controller input.
 *	The cursor graphic changes based on what hotspot is underneath it.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script displays the cursor on screen.
	 * The available cursors are defined in CursorManager.
	 * It should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_cursor.html")]
	public class PlayerCursor : MonoBehaviour
	{
		
		protected Menu limitCursorToMenu;

		protected int selectedCursor = -10; // -2 = inventory, -1 = pointer, 0+ = cursor array
		protected bool showCursor = false;
		protected bool canShowHardwareCursor = false;
		protected float pulse = 0f;
		protected int pulseDirection = 0; // 0 = none, 1 = in, -1 = out
		
		// Animation variables
		protected CursorIconBase activeIcon = null;
		protected CursorIconBase activeLookIcon = null;
		protected string lastCursorName;

		protected Texture2D currentCursorTexture2D;
		protected Texture currentCursorTexture;
		protected bool contextCycleExamine = false;
		protected int manualCursorID = -1;
		protected bool isDrawingHiddenCursor = false;

		protected bool forceOffCursor;


		protected void Start ()
		{
			if (KickStarter.cursorManager != null && KickStarter.cursorManager.cursorDisplay != CursorDisplay.Never && KickStarter.cursorManager.allowMainCursor && (KickStarter.cursorManager.pointerIcon == null || KickStarter.cursorManager.pointerIcon.texture == null))
			{
				ACDebug.LogWarning ("Main cursor has no texture - please assign one in the Cursor Manager.");
			}
			SelectedCursor = -1;
		}
		

		/**
		 * Updates the cursor. This is called every frame by StateHandler.
		 */
		public void UpdateCursor ()
		{
			if (KickStarter.cursorManager.cursorRendering == CursorRendering.Software)
			{
				bool shouldShowCursor = false;

				if (!canShowHardwareCursor)
				{
					shouldShowCursor = false;
				}
				else if (KickStarter.playerInput.GetDragState () == DragState.Moveable)
				{
					shouldShowCursor = false;
				}
				else if (KickStarter.settingsManager && KickStarter.cursorManager && (!KickStarter.cursorManager.allowMainCursor || KickStarter.cursorManager.pointerIcon.texture == null) && (KickStarter.runtimeInventory.SelectedItem == null || KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeHotspotLabel) && KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard && KickStarter.stateHandler.gameState != GameState.Cutscene)
				{
					shouldShowCursor = true;
				}
				else if (KickStarter.cursorManager == null)
				{
					shouldShowCursor = true;
				}
				else
				{
					shouldShowCursor = false;
				}

				SetCursorVisibility (shouldShowCursor);
			}
			
			if (KickStarter.settingsManager && KickStarter.stateHandler)
			{
				if (forceOffCursor)
				{
					showCursor = false;
				}
				else if (KickStarter.stateHandler.gameState == GameState.Cutscene)
				{
					if (KickStarter.cursorManager.waitIcon.texture != null)
					{
						showCursor = true;
					}
					else
					{
						showCursor = false;
					}
				}
				else if (KickStarter.stateHandler.gameState != GameState.Normal && KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen)
				{
					if (KickStarter.stateHandler.gameState == GameState.Paused && !KickStarter.menuManager.keyboardControlWhenPaused)
					{
						showCursor = true;
					}
					else if (KickStarter.stateHandler.gameState == GameState.DialogOptions && !KickStarter.menuManager.keyboardControlWhenDialogOptions)
					{
						showCursor = true;
					}
					else
					{
						showCursor = false;
					}
				}
				else if (KickStarter.cursorManager)
				{
					if (KickStarter.stateHandler.gameState == GameState.Paused && (KickStarter.cursorManager.cursorDisplay == CursorDisplay.OnlyWhenPaused || KickStarter.cursorManager.cursorDisplay == CursorDisplay.Always))
					{
						showCursor = true;
					}
					else if (KickStarter.playerInput.GetDragState () == DragState.Moveable && KickStarter.cursorManager.hideCursorWhenDraggingMoveables)
					{
						showCursor = false;
					}
					else if (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.DialogOptions)
					{
						showCursor = true;
					}
					else
					{
						showCursor = false;
					}
				}
				else
				{
					showCursor = true;
				}

				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.CustomScript)
				{}
				
				else if (KickStarter.stateHandler.IsInGameplay () && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.cursorManager != null &&
				    ((KickStarter.cursorManager.cycleCursors && KickStarter.playerInput.GetMouseState () == MouseState.RightClick) || KickStarter.playerInput.InputGetButtonDown ("CycleCursors")))
				{
					CycleCursors ();
				}
				
				else if (KickStarter.stateHandler.IsInGameplay () && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot &&
				         (KickStarter.playerInput.GetMouseState () == MouseState.RightClick || KickStarter.playerInput.InputGetButtonDown ("CycleCursors")))
				{
					KickStarter.playerInteraction.SetNextInteraction ();
				}

				else if (KickStarter.stateHandler.IsInGameplay () && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot &&
				         (KickStarter.playerInput.InputGetButtonDown ("CycleCursorsBack")))
				{
					KickStarter.playerInteraction.SetPreviousInteraction ();
				}

				else if (CanCycleContextSensitiveMode () && KickStarter.playerInput.GetMouseState () == MouseState.RightClick)
				{
					Hotspot hotspot = KickStarter.playerInteraction.GetActiveHotspot ();
					if (hotspot != null)
					{
						if (hotspot.HasContextUse () && hotspot.HasContextLook ())
						{
							KickStarter.playerInput.ResetMouseClick ();
							contextCycleExamine = !contextCycleExamine;
						}
					}
					else if (KickStarter.runtimeInventory.hoverItem != null && KickStarter.runtimeInventory.SelectedItem == null)
					{
						if (KickStarter.runtimeInventory.hoverItem.lookActionList != null)
						{
							KickStarter.playerInput.ResetMouseClick ();
							contextCycleExamine = !contextCycleExamine;
						}
					}
				}
			}
			
			if (KickStarter.cursorManager.cursorRendering == CursorRendering.Hardware)
			{
				SetCursorVisibility (showCursor);
				DrawCursor ();
			}
		}


		/**
		 * If True, the cursor will always be hidden.
		 */
		public bool ForceOffCursor
		{
			set
			{
				forceOffCursor = value;
			}
		}


		/**
		 * <summary>Sets the active cursor ID, provided that the interaction method is CustomScript.</summary>
		 * <param name = "_cursorID">The ID number of the cursor defined in the Cursor Manager. If set to -1, the current cursor will be deselected main cursor will be displayed.</param>
		 */
		public void SetSelectedCursorID (int _cursorID)
		{
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.CustomScript)
			{
				manualCursorID = _cursorID;
			}
			else
			{
				ACDebug.LogWarning ("The cursor ID can only be set manually if the 'Interaction method' is set to 'Custom Script'");
			}
		}


		/**
		 * Draws the cursor. This is called from StateHandler's OnGUI() function.
		 */
		public void DrawCursor ()
		{
			if (!showCursor)
			{
				if (!isDrawingHiddenCursor)
				{
					activeIcon = activeLookIcon = null;

					SetHardwareCursor (null, Vector2.zero);
					isDrawingHiddenCursor = true;
				}
				return;
			}
			isDrawingHiddenCursor = false;

			if (KickStarter.playerInput.IsCursorLocked () && KickStarter.settingsManager.hideLockedCursor)
			{
				canShowHardwareCursor = false;
				return;
			}

			GUI.depth = -1;
			canShowHardwareCursor = true;

			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.CustomScript)
			{
				ShowCycleCursor (manualCursorID);
				return;
			}

			if (KickStarter.runtimeInventory.SelectedItem != null)
			{
				// Cursor becomes selected inventory
				SelectedCursor = -2;
				canShowHardwareCursor = false;
			}
			else if (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ChooseInteractionThenHotspot)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive && KickStarter.cursorManager.allowInteractionCursorForInventory && KickStarter.runtimeInventory.hoverItem != null)
				{
					ShowContextIcons (KickStarter.runtimeInventory.hoverItem);
					return;
				}
				else if (KickStarter.playerInteraction.GetActiveHotspot () != null && KickStarter.stateHandler.IsInGameplay () && (KickStarter.playerInteraction.GetActiveHotspot ().HasContextUse () || KickStarter.playerInteraction.GetActiveHotspot ().HasContextLook ()))
				{
					SelectedCursor = 0;
					
					if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
					{
						Button useButton = KickStarter.playerInteraction.GetActiveHotspot ().GetFirstUseButton ();
						if (useButton != null) SelectedCursor = useButton.iconID;

						if (KickStarter.cursorManager.allowInteractionCursor)
						{
							canShowHardwareCursor = false;
							ShowContextIcons ();
						}
						else if (KickStarter.cursorManager.mouseOverIcon.texture != null)
						{
							DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
						}
						else
						{
							DrawMainCursor ();
						}
					}
				}
				else
				{
					SelectedCursor = -1;
				}
			}
			else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
			{
				if (KickStarter.stateHandler.gameState == GameState.DialogOptions || KickStarter.stateHandler.gameState == GameState.Paused)
				{
					SelectedCursor = -1;
				}
				else if (KickStarter.playerInteraction.GetActiveHotspot () != null && !KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction () && !KickStarter.cursorManager.allowInteractionCursor && KickStarter.cursorManager.mouseOverIcon.texture != null)
				{
					DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
					return;
				}
			}

			if (KickStarter.stateHandler.gameState == GameState.Cutscene && KickStarter.cursorManager.waitIcon.texture != null)
			{
				// Wait
				int elementOverCursorID = KickStarter.playerMenus.GetElementOverCursorID ();
				if (elementOverCursorID >= 0)
				{
					DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (elementOverCursorID), false);
					return;
				}

				DrawIcon (KickStarter.cursorManager.waitIcon, false);
			}
			else if (selectedCursor == -2 && KickStarter.runtimeInventory.SelectedItem != null)
			{
				// Inventory
				canShowHardwareCursor = false;
				
				if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot && KickStarter.settingsManager.cycleInventoryCursors)
				{
					if (KickStarter.playerInteraction.GetActiveHotspot () == null && KickStarter.runtimeInventory.hoverItem == null)
					{
						if (KickStarter.playerInteraction.GetInteractionIndex () >= 0)
						{
							// Item was selected due to cycling icons
							KickStarter.playerInteraction.ResetInteractionIndex ();
							KickStarter.runtimeInventory.SetNull ();
							return;
						}
					}
				}

				/*if (KickStarter.settingsManager.InventoryDragDrop && KickStarter.playerInput.GetDragState () != DragState.Inventory)
				{
					DrawMainCursor ();
				}
				else */if (KickStarter.settingsManager.inventoryActiveEffect != InventoryActiveEffect.None && KickStarter.runtimeInventory.SelectedItem.CanBeAnimated () && !string.IsNullOrEmpty (KickStarter.playerMenus.GetHotspotLabel ()) &&
				    	(KickStarter.settingsManager.activeWhenUnhandled || 
				    	KickStarter.playerInteraction.DoesHotspotHaveInventoryInteraction () || 
				    	(KickStarter.runtimeInventory.hoverItem != null && KickStarter.runtimeInventory.hoverItem.DoesHaveInventoryInteraction (KickStarter.runtimeInventory.SelectedItem))))
				{
					if (KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeHotspotLabel)
					{
						DrawMainCursor ();
					}
					else
					{
						DrawActiveInventoryCursor ();
					}
				}
				else
				{
					if (KickStarter.cursorManager.inventoryHandling != InventoryHandling.ChangeHotspotLabel && KickStarter.runtimeInventory.SelectedItem.HasCursorIcon ())
					{
						DrawInventoryCursor ();
					}
					else
					{
						if (KickStarter.cursorManager.inventoryHandling != InventoryHandling.ChangeHotspotLabel && !KickStarter.runtimeInventory.SelectedItem.HasCursorIcon ())
						{
							ACDebug.LogWarning ("Cannot change cursor to display the selected Inventory item because the item '" + KickStarter.runtimeInventory.SelectedItem.label + "' has no associated graphic.");
						}

						if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
						{
							if (KickStarter.playerInteraction.GetActiveHotspot () == null)
							{
								DrawMainCursor ();
							}
							else if (KickStarter.cursorManager.allowInteractionCursor)
							{
								canShowHardwareCursor = false;
								ShowContextIcons ();
							}
							else if (KickStarter.cursorManager.mouseOverIcon.texture != null)
							{
								DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
							}
							else
							{
								DrawMainCursor ();
							}
						}
						else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
						{
							if (KickStarter.stateHandler.gameState == GameState.DialogOptions || KickStarter.stateHandler.gameState == GameState.Paused)
							{}
							else if (KickStarter.playerInteraction.GetActiveHotspot () != null && !KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction () && !KickStarter.cursorManager.allowInteractionCursor && KickStarter.cursorManager.mouseOverIcon.texture != null)
							{
								DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
							}
							else
							{
								DrawMainCursor ();
							}
						}
					}
				}
				
				if (KickStarter.runtimeInventory.SelectedItem != null && KickStarter.runtimeInventory.SelectedItem.canCarryMultiple && !KickStarter.runtimeInventory.SelectedItem.CanSelectSingle ())
				{
					KickStarter.runtimeInventory.DrawInventoryCount (KickStarter.playerInput.GetMousePosition (), KickStarter.cursorManager.inventoryCursorSize, KickStarter.runtimeInventory.SelectedItem.count);
				}
			}
			else if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				ShowCycleCursor (KickStarter.playerInteraction.GetActiveUseButtonIconID ());
			}
			else if (KickStarter.cursorManager.allowMainCursor || KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController)
			{
				// Pointer
				pulseDirection = 0;

				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.runtimeInventory.hoverItem == null && KickStarter.playerInteraction.GetActiveHotspot () != null && (!KickStarter.playerMenus.IsInteractionMenuOn () || KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot))
					{
						if (KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction ())
						{
							ShowContextIcons ();
						}
						else if (KickStarter.cursorManager.mouseOverIcon.texture != null)
						{
							DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
						}
						else
						{
							DrawMainCursor ();
						}
					}
					else
					{
						DrawMainCursor ();
					}
				}
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					if (selectedCursor == -1)
					{
						DrawMainCursor ();
					}
					else if (selectedCursor == -2 && KickStarter.runtimeInventory.SelectedItem == null)
					{
						SelectedCursor = -1;
					}
				}
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					if (KickStarter.playerInteraction.GetActiveHotspot () != null && KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction ())
					{
						//SelectedCursor = -1;

						if (KickStarter.cursorManager.allowInteractionCursor)
						{
							ShowContextIcons ();
						}
						else if (KickStarter.cursorManager.mouseOverIcon.texture != null)
						{
							DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
						}
						else
						{
							DrawMainCursor ();
						}
					}
					else if (selectedCursor >= 0)
					{
						if (KickStarter.cursorManager.allowInteractionCursor)
						{
							//	Custom icon
							pulseDirection = 0;
							canShowHardwareCursor = false;

							bool canAnimate = false;
							if (!KickStarter.cursorManager.onlyAnimateOverHotspots ||
								 KickStarter.playerInteraction.GetActiveHotspot () != null ||
								(KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple && KickStarter.runtimeInventory.hoverItem != null))
							{
								canAnimate = true;
							}

							DrawIcon (KickStarter.cursorManager.cursorIcons [selectedCursor], false, canAnimate);
						}
						else
						{
							DrawMainCursor ();
						}
					}
					else if (selectedCursor == -1)
					{
						DrawMainCursor ();
					}
					else if (selectedCursor == -2 && KickStarter.runtimeInventory.SelectedItem == null)
					{
						SelectedCursor = -1;
					}
				}
			}
		}
		
		
		protected void DrawMainCursor ()
		{
			if (!showCursor)
			{
				return;
			}

			if (KickStarter.cursorManager.cursorDisplay == CursorDisplay.Never || !KickStarter.cursorManager.allowMainCursor)
			{
				return;
			}
			
			if (KickStarter.stateHandler.gameState != GameState.Paused && KickStarter.cursorManager.cursorDisplay == CursorDisplay.OnlyWhenPaused)
			{
				return;
			}
			
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return;
			}

			bool showWalkCursor = false;
			int elementOverCursorID = KickStarter.playerMenus.GetElementOverCursorID ();

			if (elementOverCursorID >= 0)
			{
				DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (elementOverCursorID), false);
				return;
			}

			if (KickStarter.cursorManager.allowWalkCursor && KickStarter.playerInput && !KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerMenus.IsInteractionMenuOn () && KickStarter.stateHandler.IsInGameplay ())
			{
				if (KickStarter.cursorManager.onlyWalkWhenOverNavMesh)
				{
					if (KickStarter.playerMovement.ClickPoint (KickStarter.playerInput.GetMousePosition (), true) != Vector3.zero)
					{
						showWalkCursor = true;
					}
				}
				else
				{
					showWalkCursor = true;
				}
			}
			
			if (showWalkCursor)
			{
				DrawIcon (KickStarter.cursorManager.walkIcon, false);
			}
			else if (KickStarter.cursorManager.pointerIcon.texture)
			{
				DrawIcon (KickStarter.cursorManager.pointerIcon, false);
			}
		}
		
		
		protected void ShowContextIcons ()
		{
			Hotspot hotspot = KickStarter.playerInteraction.GetActiveHotspot ();
			if (hotspot == null)
			{
				return;
			}

			if (hotspot.HasContextUse ())
			{
				if (!hotspot.HasContextLook ())
				{
					DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (hotspot.GetFirstUseButton ().iconID), false);
					return;
				}
				else
				{
					Button _button = hotspot.GetFirstUseButton ();
					
					if (hotspot.HasContextUse () && hotspot.HasContextLook () && CanDisplayIconsSideBySide ())
					{
						CursorIcon icon = KickStarter.cursorManager.GetCursorIconFromID (_button.iconID);
						DrawIcon (new Vector2 (-icon.size * ACScreen.width / 2f, 0f), icon, false);
					}
					else if (CanCycleContextSensitiveMode () && contextCycleExamine && hotspot.HasContextLook ())
					{
						CursorIcon lookIcon = KickStarter.cursorManager.GetCursorIconFromID (KickStarter.cursorManager.lookCursor_ID);
						DrawIcon (Vector2.zero, lookIcon, true);
					}
					else
					{
						DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (_button.iconID), false);
					}
				}
			}
			
			if (hotspot.HasContextLook () &&
			    (!hotspot.HasContextUse () ||
			 (hotspot.HasContextUse () && CanDisplayIconsSideBySide ())))
			{
				if (KickStarter.cursorManager.cursorIcons.Count > 0)
				{
					CursorIcon icon = KickStarter.cursorManager.GetCursorIconFromID (KickStarter.cursorManager.lookCursor_ID);
					
					if (hotspot.HasContextUse () && hotspot.HasContextLook () && CanDisplayIconsSideBySide ())
					{
						DrawIcon (new Vector2 (icon.size * ACScreen.width / 2f, 0f), icon, true);
					}
					else
					{
						DrawIcon (icon, true);
					}
				}
			}	
		}
		
		
		protected void ShowContextIcons (InvItem invItem)
		{
			if (KickStarter.cursorManager.cursorIcons.Count > 0)
			{
				if (invItem.lookActionList != null && CanDisplayIconsSideBySide ())
				{
					if (invItem.useIconID < 0)
					{
						// Hide use
						if (invItem.lookActionList != null)
						{
							CursorIcon icon = KickStarter.cursorManager.GetCursorIconFromID (KickStarter.cursorManager.lookCursor_ID);
							DrawIcon (icon, true);
						}
						return;
					}
					else
					{
						CursorIcon icon = KickStarter.cursorManager.GetCursorIconFromID (invItem.useIconID);
						DrawIcon (new Vector2 (-icon.size * ACScreen.width / 2f, 0f), icon, false);
					}
				}
				else if (CanCycleContextSensitiveMode () && contextCycleExamine && invItem.lookActionList != null)
				{}
				else
				{
					DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (invItem.useIconID), false);
					return;
				}
				
				if (invItem.lookActionList != null)
				{
					CursorIcon lookIcon = KickStarter.cursorManager.GetCursorIconFromID (KickStarter.cursorManager.lookCursor_ID);

					if (invItem.lookActionList != null && CanDisplayIconsSideBySide ())
					{
						DrawIcon (new Vector2 (lookIcon.size * ACScreen.width / 2f, 0f), lookIcon, true);
					}
					else if (CanCycleContextSensitiveMode ())
					{
						if (contextCycleExamine)
						{
							DrawIcon (Vector2.zero, lookIcon, true);
						}
					}
					else
					{
						DrawIcon (lookIcon, true);
					}
				}
			}	
		}
		
		
		protected void ShowCycleCursor (int useCursorID)
		{
			if (KickStarter.runtimeInventory.SelectedItem != null)
			{
				SelectedCursor = -2;

				if (KickStarter.cursorManager.inventoryHandling != InventoryHandling.ChangeHotspotLabel)
				{
					DrawActiveInventoryCursor ();
				}
			}
			else if (useCursorID >= 0)
			{
				SelectedCursor = useCursorID;
				DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (selectedCursor), false);
			}
			else if (useCursorID == -1)
			{
				SelectedCursor = -1;
				DrawMainCursor ();
			}
		}


		protected void DrawInventoryCursor ()
		{
			InvItem invItem = KickStarter.runtimeInventory.SelectedItem;
			if (invItem == null)
			{
				return;
			}

			if (invItem.cursorIcon.texture != null)
			{
				if (KickStarter.settingsManager.inventoryActiveEffect != InventoryActiveEffect.None)
				{
					// Only animate when active
					DrawIcon (invItem.cursorIcon, false, false);
				}
				else
				{
					DrawIcon (invItem.cursorIcon, false, true);
				}
			}
			else
			{
				DrawIcon (AdvGame.GUIBox (KickStarter.playerInput.GetMousePosition (), KickStarter.cursorManager.inventoryCursorSize), KickStarter.runtimeInventory.SelectedItem.tex);
			}
			pulseDirection = 0;
		}
		
		
		protected void DrawActiveInventoryCursor ()
		{	
			InvItem invItem = KickStarter.runtimeInventory.SelectedItem;
			if (invItem == null)
			{
				return;
			}

			if (invItem.cursorIcon.texture != null)
			{
				DrawIcon (invItem.cursorIcon, false, true);
			}
			else if (invItem.activeTex == null)
			{
				DrawInventoryCursor ();
			}
			else if (KickStarter.settingsManager.inventoryActiveEffect == InventoryActiveEffect.Simple)
			{
				DrawIcon (AdvGame.GUIBox (KickStarter.playerInput.GetMousePosition (), KickStarter.cursorManager.inventoryCursorSize), invItem.activeTex);
			}
			else if (KickStarter.settingsManager.inventoryActiveEffect == InventoryActiveEffect.Pulse && invItem.tex)
			{
				if (pulseDirection == 0)
				{
					pulse = 0f;
					pulseDirection = 1;
				}
				else if (pulse > 1f)
				{
					pulse = 1f;
					pulseDirection = -1;
				}
				else if (pulse < 0f)
				{
					pulse = 0f;
					pulseDirection = 1;
				}
				else if (pulseDirection == 1)
				{
					pulse += KickStarter.settingsManager.inventoryPulseSpeed * Time.deltaTime;
				}
				else if (pulseDirection == -1)
				{
					pulse -= KickStarter.settingsManager.inventoryPulseSpeed * Time.deltaTime;
				}
				
				Color backupColor = GUI.color;
				Color tempColor = GUI.color;
				
				tempColor.a = pulse;
				GUI.color = tempColor;
				DrawIcon (AdvGame.GUIBox (KickStarter.playerInput.GetMousePosition (), KickStarter.cursorManager.inventoryCursorSize), invItem.activeTex);
				GUI.color = backupColor;
				DrawIcon (AdvGame.GUIBox (KickStarter.playerInput.GetMousePosition (), KickStarter.cursorManager.inventoryCursorSize), invItem.tex);
			}
		}
		
		
		protected void DrawIcon (Rect _rect, Texture _tex)
		{
			if (_tex != null)
			{
				RecordCursorTexture (_tex);

				if (KickStarter.cursorManager.cursorRendering == CursorRendering.Hardware)
				{
					lastCursorName = string.Empty;
					activeIcon = activeLookIcon = null;

					SetHardwareCursor (currentCursorTexture2D, Vector2.zero);
				}
				else
				{
					GUI.DrawTexture (_rect, currentCursorTexture, ScaleMode.ScaleToFit, true, 0f);
				}
			}
		}


		protected void SetHardwareCursor (Texture2D texture2D, Vector2 clickOffset)
		{
			Cursor.SetCursor (texture2D, clickOffset, CursorMode.Auto);
			KickStarter.eventManager.Call_OnSetHardwareCursor (texture2D, clickOffset);
		}
		
		
		protected void DrawIcon (Vector2 offset, CursorIconBase icon, bool isLook, bool canAnimate = true)
		{
			if (icon != null)
			{
				bool isNew = false;
				if (isLook && activeLookIcon != icon)
				{
					activeLookIcon = icon;
					isNew = true;
					icon.Reset ();
				}
				else if (!isLook && activeIcon != icon)
				{
					activeIcon = icon;
					isNew = true;
					icon.Reset ();
				}
				
				if (KickStarter.cursorManager.cursorRendering == CursorRendering.Hardware)
				{
					if (icon.isAnimated)
					{
						Texture2D animTex = icon.GetAnimatedTexture (canAnimate);

						if (icon.GetName () != lastCursorName)
						{
							lastCursorName = icon.GetName ();
							RecordCursorTexture (animTex);

							SetHardwareCursor (currentCursorTexture2D, icon.clickOffset);
						}
					}
					else if (isNew)
					{
						RecordCursorTexture (icon.texture);
						SetHardwareCursor (currentCursorTexture2D, icon.clickOffset);
					}
				}
				else
				{
					Texture tex = icon.Draw (KickStarter.playerInput.GetMousePosition () + offset, canAnimate);
					RecordCursorTexture (tex);
				}
			}
		}
		
		
		protected void DrawIcon (CursorIconBase icon, bool isLook, bool canAnimate = true)
		{
			if (icon != null)
			{
				DrawIcon (new Vector2 (0f, 0f), icon, isLook, canAnimate);
			}
		}


		protected void RecordCursorTexture (Texture newCursorTexture)
		{
			if (newCursorTexture != null)
			{
				if (currentCursorTexture != newCursorTexture)
				{
					currentCursorTexture = newCursorTexture;

					if (newCursorTexture is Texture2D)
					{
						Texture2D newCursorTexture2D = (Texture2D) newCursorTexture;
						currentCursorTexture2D = newCursorTexture2D;
					}
				}
			}
		}


		/**
		 * <summary>Gets the current cursor texture.</summary>
		 * <returns>The current cursor texture. If the cursor is hidden or showing no texture, the last-assigned texture will be returned instead.</returns>
		 */
		public Texture GetCurrentCursorTexture ()
		{
			return currentCursorTexture;
		}


		protected void CycleCursors ()
		{
			if (KickStarter.playerInteraction.GetActiveHotspot () != null && KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction ())
			{
				return;
			}

			int newSelectedCursor = selectedCursor;

			if (KickStarter.cursorManager.cursorIcons.Count > 0)
			{
				newSelectedCursor ++;

				if (newSelectedCursor >= 0 && newSelectedCursor < KickStarter.cursorManager.cursorIcons.Count && KickStarter.cursorManager.cursorIcons [newSelectedCursor].dontCycle)
				{
					while (KickStarter.cursorManager.cursorIcons [newSelectedCursor].dontCycle)
					{
						newSelectedCursor ++;

						if (newSelectedCursor >= KickStarter.cursorManager.cursorIcons.Count)
						{
							newSelectedCursor = -1;
							break;
						}
					}
				}
				else if (newSelectedCursor >= KickStarter.cursorManager.cursorIcons.Count)
				{
					newSelectedCursor = -1;
				}
			}
			else
			{
				// Pointer
				newSelectedCursor = -1;
			}

			if (newSelectedCursor == -1 && selectedCursor >= 0)
			{
				// Ended icon cycle
				if (KickStarter.settingsManager.cycleInventoryCursors)
				{
					KickStarter.runtimeInventory.ReselectLastItem ();
					KickStarter.playerInput.ResetMouseClick ();
    			}
			}

			SelectedCursor = newSelectedCursor;
		}
		

		/**
		 * <summary>Gets the index number of the currently-selected cursor within CursorManager's cursorIcons List.</summary>
		 * <returns>If = -2, the inventory cursor is showing.
		 * If = -1, the main pointer is showing.
		 * If > 0, the index number of the currently-selected cursor within CursorManager's cursorIcons List</returns>
		 */
		public int GetSelectedCursor ()
		{
			return selectedCursor;
		}
		

		/**
		 * <summary>Gets the ID number of the currently-selected cursor, within CursorManager's cursorIcons List.</summary>
		 * <returns>The ID number of the currently-selected cursor, within CursorManager's cursorIcons List</returns>
		 */
		public int GetSelectedCursorID ()
		{
			if (KickStarter.cursorManager && KickStarter.cursorManager.cursorIcons.Count > 0 && selectedCursor > -1)
			{
				return KickStarter.cursorManager.cursorIcons [selectedCursor].id;
			}
			return -1;
		}
		

		/**
		 * <summary>Resets the currently-selected cursor</summary>
		 */
		public void ResetSelectedCursor ()
		{
			SelectedCursor = -1;
		}
		

		/**
		 * <summary>Sets the cursor to an icon defined in CursorManager.</summary>
		 * <param name = "ID">The ID number of the cursor, within CursorManager's cursorIcons List, to select</param>
		 */
		public void SetCursorFromID (int ID)
		{
			if (KickStarter.cursorManager && KickStarter.cursorManager.cursorIcons.Count > 0)
			{
				foreach (CursorIcon cursor in KickStarter.cursorManager.cursorIcons)
				{
					if (cursor.id == ID)
					{
						SetCursor (cursor);
					}
				}
			}
		}


		/**
		 * <summary>Sets the cursor to an icon defined in CursorManager.</summary>
		 * <param name = "_icon">The cursor, within CursorManager's cursorIcons List, to select</param>
		 */
		public void SetCursor (CursorIcon _icon)
		{
			KickStarter.runtimeInventory.SetNull ();
			SelectedCursor = KickStarter.cursorManager.cursorIcons.IndexOf (_icon);
		}


		public bool ContextCycleExamine
		{
			get
			{
				return contextCycleExamine;
			}
		}


		protected bool CanDisplayIconsSideBySide ()
		{
			if (KickStarter.cursorManager.lookUseCursorAction == LookUseCursorAction.DisplayBothSideBySide &&
			    KickStarter.cursorManager.cursorRendering == CursorRendering.Software &&
			    KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
			{
				return true;
			}
			return false;
		}


		protected bool CanCycleContextSensitiveMode ()
		{
			if (KickStarter.cursorManager.lookUseCursorAction == LookUseCursorAction.RightClickCyclesModes &&
			    KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
			{
				return true;
			}
			return false;
		}


		protected void OnApplicationQuit ()
		{
			if (KickStarter.cursorManager != null)
			{
				KickStarter.cursorManager.waitIcon.ClearCache ();
				KickStarter.cursorManager.pointerIcon.ClearCache ();
				KickStarter.cursorManager.walkIcon.ClearCache ();
				KickStarter.cursorManager.mouseOverIcon.ClearCache ();

				foreach (CursorIcon cursorIcon in KickStarter.cursorManager.cursorIcons)
				{
					cursorIcon.ClearCache ();
				}
			}
		}


		protected int SelectedCursor
		{
			set
			{
				if (selectedCursor != value)
				{
					selectedCursor = value;

					if (KickStarter.eventManager != null)
					{
						KickStarter.eventManager.Call_OnChangeCursorMode (selectedCursor);
					}
				}
			}
		}


		/** If set, the cursor's range of movement will be limited to this Menu's boundary */
		public Menu LimitCursorToMenu
		{
			get
			{
				return limitCursorToMenu;
			}
			set
			{
				limitCursorToMenu = value;
			}
		}


		/**
		 * <summary>Sets the visiblity of the cursor.</summary>
		 * <param name = "state">If True, the cursor will be shown. If False, the cursor will be hidden."</param>
		 */
		public void SetCursorVisibility (bool state)
		{
			#if UNITY_EDITOR
			if (KickStarter.cursorManager != null && KickStarter.cursorManager.forceCursorInEditor)
			{
				state = true;
			}
			#endif

			Cursor.visible = state;
		}

	}
	
}