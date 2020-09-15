/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"PlayerMenus.cs"
 * 
 *	This script handles the displaying of each of the menus defined in MenuManager.
 *	It avoids referencing specific menus and menu elements as much as possible,
 *	so that the menu can be completely altered using just the MenuSystem script.
 * 
 */

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script handles the initialisation, position and display of all Menus defined in MenuManager.
	 * Menus are transferred from MenuManager to a local List within this script when the game begins.
	 * It must be placed on the PersistentEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_menus.html")]
	public class PlayerMenus : MonoBehaviour
	{

		protected bool isMouseOverMenu = false;
		protected bool isMouseOverInteractionMenu = false;
		protected bool canKeyboardControl = false;
		protected bool interactionMenuIsOn = false;
		protected bool interactionMenuPauses = false;

		protected bool lockSave = false;
		protected int selected_option;

		protected bool foundMouseOverMenu = false;
		protected bool foundMouseOverInteractionMenu = false;
		protected bool foundMouseOverInventory = false;
		protected bool foundCanKeyboardControl = false;
		protected bool isMouseOverInventory = false;

		protected float pauseAlpha = 0f;
		protected List<Menu> menus = new List<Menu>();
		protected List<Menu> dupSpeechMenus = new List<Menu>();
		protected List<Menu> customMenus = new List<Menu>();
		protected Texture2D pauseTexture;
		protected string menuIdentifier = string.Empty;
		protected string lastMenuIdentifier = string.Empty;
		protected string elementIdentifier = string.Empty;
		protected string lastElementIdentifier = string.Empty;
		protected MenuInput selectedInputBox;
		protected string selectedInputBoxMenuName;
		protected MenuInventoryBox activeInventoryBox;
		protected MenuCrafting activeCrafting;
		protected Menu activeInventoryBoxMenu;
		protected InvItem oldHoverItem;
		protected int doResizeMenus = 0;

		protected Menu mouseOverMenu;
		protected MenuElement mouseOverElement;
		protected int mouseOverElementSlot;
		
		protected Menu crossFadeTo;
		protected Menu crossFadeFrom;
		protected UnityEngine.EventSystems.EventSystem eventSystem;

		protected int elementOverCursorID = -1;

		protected GUIStyle normalStyle = new GUIStyle ();
		protected GUIStyle highlightedStyle = new GUIStyle();
		protected Rect lastSafeRect;
		protected float lastAspectRatio;
		
		#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
		protected TouchScreenKeyboard keyboard;
		#endif

		protected string hotspotLabelOverride;

		
		public void OnStart ()
		{
			RebuildMenus ();
		}


		/**
		 * <summary>Rebuilds the game's Menus, either from the existing MenuManager asset, or from a new one.</summary>
		 * <param name = "menuManager">The Menu Manager to use for Menu generation. If left empty, the default Menu Manager will be used.</param>
		 */
		public void RebuildMenus (MenuManager menuManager = null)
		{
			if (menuManager != null)
			{
				KickStarter.menuManager = menuManager;
			}

			foreach (Menu menu in menus)
			{
				if (menu.menuSource == MenuSource.UnityUiPrefab &&
					menu.RuntimeCanvas != null &&
					menu.RuntimeCanvas.gameObject != null &&
					!menu.GetsDuplicated ())
				{
					Destroy (menu.RuntimeCanvas.gameObject);
				}
			}

			menus = new List<Menu>();
			
			if (KickStarter.menuManager)
			{
				KickStarter.menuManager.Upgrade ();

				pauseTexture = KickStarter.menuManager.pauseTexture;
				foreach (AC.Menu _menu in KickStarter.menuManager.menus)
				{
					Menu newMenu = ScriptableObject.CreateInstance <Menu>();
					newMenu.Copy (_menu, false);

					if (_menu.GetsDuplicated ())
					{
						// Don't make canvas object yet!
					}
					else if (newMenu.IsUnityUI ())
					{
						newMenu.LoadUnityUI ();
					}

					newMenu.Recalculate ();

					menus.Add (newMenu);
					newMenu.Initalise ();
				}
			}
			
			CreateEventSystem ();
			
			foreach (AC.Menu menu in menus)
			{
				menu.Recalculate ();
			}
			
			#if UNITY_WEBPLAYER && !UNITY_EDITOR
			// WebPlayer takes another second to get the correct screen dimensions
			foreach (AC.Menu menu in menus)
			{
				menu.Recalculate ();
			}
			#endif

			KickStarter.eventManager.Call_OnGenerateMenus ();

			StartCoroutine (CycleMouseOverUIs ());
		}


		protected IEnumerator CycleMouseOverUIs ()
		{
			// MouseOver UI menus need to be enabled in the first frame so that their RectTransforms can be recognised by Unity

			foreach (Menu menu in menus)
			{
				if (menu.menuSource != MenuSource.AdventureCreator && menu.appearType == AppearType.MouseOver)
				{
					menu.EnableUI ();
				}
			}

			yield return new WaitForEndOfFrame ();

			foreach (Menu menu in menus)
			{
				if (menu.menuSource != MenuSource.AdventureCreator && menu.appearType == AppearType.MouseOver)
				{
					menu.DisableUI ();
				}
			}
		}


		protected void CreateEventSystem ()
		{
			UnityEngine.EventSystems.EventSystem localEventSystem = GameObject.FindObjectOfType <UnityEngine.EventSystems.EventSystem>();

			if (localEventSystem == null)
			{
				UnityEngine.EventSystems.EventSystem _eventSystem = null;

				if (KickStarter.menuManager)
				{
					if (KickStarter.menuManager.eventSystem != null)
					{
						_eventSystem = (UnityEngine.EventSystems.EventSystem) Instantiate (KickStarter.menuManager.eventSystem);
						_eventSystem.gameObject.name = KickStarter.menuManager.eventSystem.name;
					}
					else if (AreAnyMenusUI ())
					{
						//_eventSystem = UnityVersionHandler.CreateEventSystem ();

						GameObject eventSystemObject = new GameObject ();
						eventSystemObject.name = "EventSystem";
						_eventSystem = eventSystemObject.AddComponent <UnityEngine.EventSystems.EventSystem>();

						if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
						{
							eventSystemObject.AddComponent <StandaloneInputModule>();
						}
						else
						{
							eventSystemObject.AddComponent <OptionalMouseInputModule>();
						}
					}
				}

				if (_eventSystem != null)
				{
					eventSystem = _eventSystem;
				}
			}
			else if (eventSystem == null)
			{
				eventSystem = localEventSystem;

				ACDebug.LogWarning ("A local EventSystem object was found in the scene.  This will override the one created by AC, and may cause problems.  A custom EventSystem prefab can be assigned in the Menu Manager.", localEventSystem);
			}
		}


		protected bool AreAnyMenusUI ()
		{
			Menu[] allMenus = GetMenus (true).ToArray ();
			foreach (AC.Menu menu in allMenus)
			{
				if (menu.menuSource == MenuSource.UnityUiInScene || menu.menuSource == MenuSource.UnityUiPrefab)
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * Initialises the menu system after a scene change. This is called manually by SaveSystem so that the order is correct.
		 */
		public void AfterLoad ()
		{
			if (KickStarter.settingsManager != null && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			CreateEventSystem ();

			foreach (Menu menu in menus)
			{
				menu.AfterSceneChange ();
			}

			foreach (Menu customMenu in customMenus)
			{
				customMenu.AfterSceneChange ();
			}

			StartCoroutine (CycleMouseOverUIs ());
		}


		public void AfterSceneAdd ()
		{
			foreach (AC.Menu _menu in menus)
			{
				if (_menu.menuSource == MenuSource.UnityUiInScene)
				{
					_menu.LoadUnityUI ();
					_menu.Initalise ();
				}
			}
		}


		/**
		 * Clears the parents of any Unity UI-based Menu Canvases.
		 * This makes them able to survive a scene change.
		 */
		public void ClearParents ()
		{
			foreach (AC.Menu _menu in menus)
			{
				if (_menu.IsUnityUI () && _menu.RuntimeCanvas != null)
				{
					_menu.ClearParent ();
				}
			}
		}


		protected void UpdatePauseBackground (bool fadeIn)
		{
			float fadeSpeed = 0.5f;
			if (fadeIn)
			{
				if (pauseAlpha < 1f)
				{
					pauseAlpha += (0.2f * fadeSpeed);
				}				
				else
				{
					pauseAlpha = 1f;
				}
			}
			
			else
			{
				if (pauseAlpha > 0f)
				{
					pauseAlpha -= (0.2f * fadeSpeed);
				}
				else
				{
					pauseAlpha = 0f;
				}
			}
		}


		/**
		 * Draws any OnGUI-based Menus set to appear while the game is loading.
		 */
		public void DrawLoadingMenus ()
		{
			for (int i=0; i<menus.Count; i++)
			{
				int languageNumber = Options.GetLanguage ();
				if (menus[i].appearType == AppearType.WhileLoading)
				{
					DrawMenu (menus[i], languageNumber);
				}
			}
		}
		

		/**
		 * Draws all OnGUI-based Menus.
		 */
		public void DrawMenus ()
		{
			if (doResizeMenus > 0)
			{
				return;
			}

			elementOverCursorID = -1;

			if (KickStarter.playerInteraction && KickStarter.playerInput && KickStarter.menuSystem && KickStarter.stateHandler && KickStarter.settingsManager)
			{
				GUI.depth = KickStarter.menuManager.globalDepth;

				if (pauseTexture != null && pauseAlpha > 0f)
				{
					Color tempColor = GUI.color;
					tempColor.a = pauseAlpha;
					GUI.color = tempColor;
					GUI.DrawTexture (AdvGame.GUIRect (0.5f, 0.5f, 1f, 1f), pauseTexture, ScaleMode.ScaleToFit, true, 0f);
				}

				if (selectedInputBox)
				{
					Event currentEvent = Event.current;
					if (currentEvent.isKey && currentEvent.type == EventType.KeyDown)
					{
						selectedInputBox.CheckForInput (currentEvent.keyCode.ToString (), currentEvent.character.ToString (), currentEvent.shift, selectedInputBoxMenuName);
					}
				}
				
				int languageNumber = Options.GetLanguage ();

				for (int j=0; j<menus.Count; j++)
				{
					DrawMenu (menus[j], languageNumber);
				}

				for (int j=0; j<dupSpeechMenus.Count; j++)
				{
					DrawMenu (dupSpeechMenus[j], languageNumber);
				}

				for (int j=0; j<customMenus.Count; j++)
				{
					DrawMenu (customMenus[j], languageNumber);
				}
			}
		}


		/**
		 * <summary>Gets the Menu that a given MenuElement is a part of</summary>
		 * <param name = "_element">The MenuElement to get the Menu for</param>
		 * <returns>The Menu that the MenuElement is a part of</returns>
		 */
		public Menu GetMenuWithElement (MenuElement _element)
		{
			foreach (Menu menu in menus)
			{
				foreach (MenuElement element in menu.elements)
				{
					if (element == _element)
					{
						return menu;
					}
				}
			}

			foreach (Menu dupSpeechMenu in dupSpeechMenus)
			{
				foreach (MenuElement element in dupSpeechMenu.elements)
				{
					if (element == _element)
					{
						return dupSpeechMenu;
					}
				}
			}

			foreach (Menu customMenu in customMenus)
			{
				foreach (MenuElement element in customMenu.elements)
				{
					if (element == _element)
					{
						return customMenu;
					}
				}
			}

			return null;
		}


		/**
		 * <summary>Draws an Adventure Creator-sourced Menu. This should be called from OnGUI()</summary>
		 * <param name = "menu">The Menu to draw</param>
		 * <param name = "languageNumber">The index number of the language to use (0 = default)</param>
		 */
		public void DrawMenu (AC.Menu menu, int languageNumber = 0)
		{
			Color tempColor = GUI.color;
			bool isACMenu = !menu.IsUnityUI ();
			
			if (menu.IsEnabled ())
			{
				if (!menu.HasTransition () && menu.IsFading ())
				{
					// Stop until no longer "fading" so that it appears in right place
					return;
				}

				if (menu.hideDuringSaveScreenshots && KickStarter.saveSystem.IsTakingSaveScreenshot)
				{
					return;
				}
				
				if (isACMenu)
				{
					if (menu.transitionType == MenuTransition.Fade || menu.transitionType == MenuTransition.FadeAndPan)
					{
						tempColor.a = 1f - menu.GetFadeProgress ();
						GUI.color = tempColor;
					}
					else
					{
						tempColor.a = 1f;
						GUI.color = tempColor;
					}
					
					menu.StartDisplay ();
				}

				for (int j=0; j<menu.NumElements; j++)
				{
					if (menu.elements[j].IsVisible)
					{
						if (isACMenu)
						{
							SetStyles (menu.elements[j]);
						}

						for (int i=0; i<menu.elements[j].GetNumSlots (); i++)
						{
							if (menu.IsEnabled () && KickStarter.stateHandler.gameState != GameState.Cutscene && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && menu.appearType == AppearType.OnInteraction)
							{
								if (menu.elements[j] is MenuInteraction)
								{
									MenuInteraction menuInteraction = (MenuInteraction) menu.elements[j];
									if (menuInteraction.iconID == KickStarter.playerInteraction.GetActiveUseButtonIconID ())
									{
										if (isACMenu)
										{
											menu.elements[j].Display (highlightedStyle, i, menu.GetZoom (), true);
										}
									}
									else
									{
										if (isACMenu)
										{
											menu.elements[j].Display (normalStyle, i, menu.GetZoom (), false);
										}
									}
								}
								else if (menu.elements[j] is MenuInventoryBox)
								{
									MenuInventoryBox menuInventoryBox = (MenuInventoryBox) menu.elements[j];
									if (menuInventoryBox.inventoryBoxType == AC_InventoryBoxType.HotspotBased && menuInventoryBox.items[i].id == KickStarter.playerInteraction.GetActiveInvButtonID ())
									{
										if (isACMenu)
										{
											menu.elements[j].Display (highlightedStyle, i, menu.GetZoom (), true);
										}
									}
									else if (isACMenu)
									{
										menu.elements[j].Display (normalStyle, i, menu.GetZoom (), false);
									}
								}
								else if (isACMenu)
								{
									menu.elements[j].Display (normalStyle, i, menu.GetZoom (), false);
								}
							}

							else if (menu.IsClickable () && KickStarter.playerInput.IsCursorReadable () && SlotIsInteractive (menu, j, i))
							{
								if (isACMenu)
								{
									float zoom = 1;
									if (menu.transitionType == MenuTransition.Zoom)
									{
										zoom = menu.GetZoom ();
									}
									
									if ((!interactionMenuIsOn || menu.appearType == AppearType.OnInteraction)
										&& (KickStarter.playerInput.GetDragState () == DragState.None || (KickStarter.playerInput.GetDragState () == DragState.Inventory && CanElementBeDroppedOnto (menu.elements[j]))))
									{
										menu.elements[j].Display (highlightedStyle, i, zoom, true);
										
										if (menu.elements[j].changeCursor)
										{
											elementOverCursorID = menu.elements[j].cursorID;
										}
									}
									else
									{
										menu.elements[j].Display (normalStyle, i, zoom, false);
									}
								}
								else
								{
									// Unity UI
									if ((!interactionMenuIsOn || menu.appearType == AppearType.OnInteraction)
										&& (KickStarter.playerInput.GetDragState () == DragState.None || (KickStarter.playerInput.GetDragState () == DragState.Inventory && CanElementBeDroppedOnto (menu.elements[j]))))
									{
										if (menu.elements[j].changeCursor)
										{
											elementOverCursorID = menu.elements[j].cursorID;
										}
									}
								}
							}
							else if (isACMenu && menu.elements[j] is MenuInteraction)
							{
								MenuInteraction menuInteraction = (MenuInteraction) menu.elements[j];
								if (menuInteraction.IsDefaultIcon)
								{
									menu.elements[j].Display (highlightedStyle, i, menu.GetZoom (), false);
								}
								else
								{
									menu.elements[j].Display (normalStyle, i, menu.GetZoom (), false);
								}
							}
							else if (isACMenu)
							{
								menu.elements[j].Display (normalStyle, i, menu.GetZoom (), false);
							}
						}
					}
				}
				
				if (isACMenu)
				{
					menu.EndDisplay ();
				}
			}
			
			if (isACMenu)
			{
				tempColor.a = 1f;
				GUI.color = tempColor;
			}
		}
		

		/**
		 * <summary>Updates a Menu's position.</summary>
		 * <param name = "menu">The Menu to reposition</param>
		 * <param name = "invertedMouse">The y-inverted mouse position</param>
		 * <param name = "force">If True, the position will be updated regardless of whether or not the Menu is on.</param>
		 */
		public void UpdateMenuPosition (AC.Menu menu, Vector2 invertedMouse, bool force = false)
		{
			if (!menu.IsEnabled () && !force)
			{
				return;
			}

			if (!menu.oneMenuPerSpeech && menu.appearType == AppearType.WhenSpeechPlays)
			{
				Speech speech = KickStarter.dialog.GetLatestSpeech ();
				if (speech != null && !speech.MenuCanShow (menu))
				{
					// Don't update position for speech menus that are not for the current speech
					return;
				}
			}

			if (menu.IsUnityUI ())
			{
				if (Application.isPlaying)
				{
					Vector2 screenPosition = Vector2.zero;

					switch (menu.uiPositionType)
					{
						case UIPositionType.Manual:
							return;

						case UIPositionType.FollowCursor:
							if (menu.RuntimeCanvas != null && menu.RuntimeCanvas.renderMode == RenderMode.WorldSpace)
							{
								screenPosition = new Vector2 (invertedMouse.x, ACScreen.height + 1f - invertedMouse.y);
								Vector3 worldPosition = menu.RuntimeCanvas.worldCamera.ScreenToWorldPoint (new Vector3 (screenPosition.x, screenPosition.y, 10f));
								menu.SetCentre3D (worldPosition);
							}
							else
							{
								screenPosition = new Vector2 (invertedMouse.x, ACScreen.height + 1f - invertedMouse.y);
								menu.SetCentre (screenPosition);
							}
							break;

						case UIPositionType.OnHotspot:
							if (isMouseOverMenu || canKeyboardControl)
							{
								if (menu.TargetInvItem == null &&
								    menu.TargetHotspot != null)
								{
									// Bypass
									return;
								}

								if (activeCrafting != null)
								{
									if (menu.TargetInvItem != null)
									{
										int slot = activeCrafting.GetItemSlot (menu.TargetInvItem.id);
										screenPosition = activeInventoryBoxMenu.GetSlotCentre (activeCrafting, slot);
										menu.SetCentre (new Vector2 (screenPosition.x, ACScreen.height - screenPosition.y));
									}
									else if (KickStarter.runtimeInventory.hoverItem != null)
									{
										int slot = activeCrafting.GetItemSlot (KickStarter.runtimeInventory.hoverItem.id);
										screenPosition = activeInventoryBoxMenu.GetSlotCentre (activeCrafting, slot);
										menu.SetCentre (new Vector2 (screenPosition.x, ACScreen.height - screenPosition.y));
									}
								}
								else if (activeInventoryBox != null)
								{
									if (menu.TargetInvItem != null)
									{
										int slot = activeInventoryBox.GetItemSlot (menu.TargetInvItem.id);
										screenPosition = activeInventoryBoxMenu.GetSlotCentre (activeInventoryBox, slot);
										menu.SetCentre (new Vector2 (screenPosition.x, ACScreen.height - screenPosition.y));
									}
									else if (KickStarter.runtimeInventory.hoverItem != null)
									{
										int slot = activeInventoryBox.GetItemSlot (KickStarter.runtimeInventory.hoverItem.id);
										screenPosition = activeInventoryBoxMenu.GetSlotCentre (activeInventoryBox, slot);
										menu.SetCentre (new Vector2 (screenPosition.x, ACScreen.height - screenPosition.y));
									}
								}
							}
							else
							{
								if (menu.TargetInvItem != null)
								{
									// Bypass
									return;
								}

								if (!MoveUIMenuToHotspot (menu, menu.TargetHotspot))
								{
									if (!MoveUIMenuToHotspot (menu, KickStarter.playerInteraction.GetActiveHotspot ()))
									{
										if (AreInteractionMenusOn ())
										{
											MoveUIMenuToHotspot (menu, KickStarter.playerInteraction.GetLastOrActiveHotspot ());
										}
									}
								}
							}
							break;

						case UIPositionType.AboveSpeakingCharacter:
							Char speaker = null;
							bool canMove = true;
							if (dupSpeechMenus.Contains (menu))
							{
								if (menu.speech != null)
								{
									speaker = menu.speech.GetSpeakingCharacter ();

									if (!menu.moveWithCharacter)
									{
										canMove = !menu.HasMoved;
									}
								}
							}
							else
							{
								speaker = KickStarter.dialog.GetSpeakingCharacter ();
							}

							if (speaker != null && canMove)
							{
								if (menu.RuntimeCanvas != null && menu.RuntimeCanvas.renderMode == RenderMode.WorldSpace)
								{
									menu.SetCentre3D (speaker.GetSpeechWorldPosition ());
								}
								else
								{
									screenPosition = speaker.GetSpeechScreenPosition (menu.fitWithinScreen);
									screenPosition = KickStarter.mainCamera.ConvertRelativeScreenSpaceToUI (screenPosition);
									menu.SetCentre (screenPosition);
								}
							}
							break;

						case UIPositionType.AbovePlayer:
							if (KickStarter.player)
							{
								if (menu.RuntimeCanvas.renderMode == RenderMode.WorldSpace)
								{
									menu.SetCentre3D (KickStarter.player.GetSpeechWorldPosition ());
								}
								else
								{
									screenPosition = KickStarter.player.GetSpeechScreenPosition (menu.fitWithinScreen);
									screenPosition = KickStarter.mainCamera.ConvertRelativeScreenSpaceToUI (screenPosition);
									menu.SetCentre (screenPosition);
								}
							}
							break;

						default:
							break;
					}
				}

				return;
			}

			if (menu.sizeType == AC_SizeType.Automatic && menu.autoSizeEveryFrame)
			{
				menu.Recalculate ();
			}

			if (invertedMouse == Vector2.zero)
			{
				invertedMouse = KickStarter.playerInput.GetInvertedMouse ();
			}

			switch (menu.positionType)
			{
				case AC_PositionType.FollowCursor:
					Vector2 newMenuPosition = KickStarter.mainCamera.ConvertToMenuSpace (invertedMouse);
					menu.SetCentre (new Vector2 (newMenuPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
												newMenuPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
					break;

				case AC_PositionType.OnHotspot:
					if (isMouseOverInventory)
					{
						if (menu.TargetInvItem == null &&
						    menu.TargetHotspot != null)
						{
							// Bypass
							return;
						}

						if (activeCrafting != null)
						{
							if (menu.TargetInvItem != null)
							{
								int slot = activeCrafting.GetItemSlot (menu.TargetInvItem.id);
								Vector2 activeInventoryItemCentre = activeInventoryBoxMenu.GetSlotCentre (activeCrafting, slot);

								Vector2 screenPosition = new Vector2 (activeInventoryItemCentre.x / ACScreen.width, activeInventoryItemCentre.y / ACScreen.height);
								menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
								                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
							}
							else if (KickStarter.runtimeInventory.hoverItem != null)
							{
								int slot = activeCrafting.GetItemSlot (KickStarter.runtimeInventory.hoverItem.id);
								Vector2 activeInventoryItemCentre = activeInventoryBoxMenu.GetSlotCentre (activeCrafting, slot);

								Vector2 screenPosition = new Vector2 (activeInventoryItemCentre.x / ACScreen.width, activeInventoryItemCentre.y / ACScreen.height);
								menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
								                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
							}
						}
						else if (activeInventoryBox != null)
						{
							if (menu.TargetInvItem != null)
							{
								int slot = activeInventoryBox.GetItemSlot (menu.TargetInvItem.id);
								Vector2 activeInventoryItemCentre = activeInventoryBoxMenu.GetSlotCentre (activeInventoryBox, slot);

								Vector2 screenPosition = new Vector2 (activeInventoryItemCentre.x / ACScreen.width, activeInventoryItemCentre.y / ACScreen.height);
								menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
								                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
							}
							else if (KickStarter.runtimeInventory.hoverItem != null)
							{
								int slot = activeInventoryBox.GetItemSlot (KickStarter.runtimeInventory.hoverItem.id);
								Vector2 activeInventoryItemCentre = activeInventoryBoxMenu.GetSlotCentre (activeInventoryBox, slot);

								Vector2 screenPosition = new Vector2 (activeInventoryItemCentre.x / ACScreen.width, activeInventoryItemCentre.y / ACScreen.height);
								menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
								                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
							}
						}
					}
					else
					{
						if (menu.TargetInvItem != null)
						{
							// Bypass
							return;
						}

						if (!MoveMenuToHotspot (menu, menu.TargetHotspot))
						{
							if (!MoveMenuToHotspot (menu, KickStarter.playerInteraction.GetActiveHotspot ()))
							{
								if (AreInteractionMenusOn ())
								{
									MoveMenuToHotspot (menu, KickStarter.playerInteraction.GetLastOrActiveHotspot ());
								}
							}
						}
					}
					break;

				case AC_PositionType.AboveSpeakingCharacter:
					Char speaker = null;
					bool canMove = true;
					if (dupSpeechMenus.Contains (menu))
					{
						if (menu.speech != null)
						{
							speaker = menu.speech.GetSpeakingCharacter ();

							if (!menu.moveWithCharacter)
							{
								canMove = !menu.HasMoved;
							}
						}
					}
					else
					{
						speaker = KickStarter.dialog.GetSpeakingCharacter ();
					}

					if (speaker != null && canMove)
					{
						Vector2 screenPosition = speaker.GetSpeechScreenPosition (menu.fitWithinScreen);
						menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
						                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f),
						                true);
					}
					break;

				case AC_PositionType.AbovePlayer:
					if (KickStarter.player)
					{
						Vector2 screenPosition = KickStarter.player.GetSpeechScreenPosition (menu.fitWithinScreen);
						menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
						                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f),
						                true);
					}
					break;

				default:
					break;
			}
		}


		protected bool MoveMenuToHotspot (Menu menu, Hotspot hotspot)
		{
			if (hotspot != null)
			{
				Vector2 screenPos = hotspot.GetIconScreenPosition ();
				Vector2 screenPosition = new Vector2 (screenPos.x / ACScreen.width, 1f - (screenPos.y / ACScreen.height));

				menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
											 screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
				 return true;
			}
			return false;
		}


		protected bool MoveUIMenuToHotspot (Menu menu, Hotspot hotspot)
		{
			if (hotspot != null)
			{
				if (menu.RuntimeCanvas == null)
				{
					ACDebug.LogWarning ("Cannot move UI menu " + menu.title + " as no Canvas is assigned!");
				}
				else if (menu.RuntimeCanvas.renderMode == RenderMode.WorldSpace)
				{
					menu.SetCentre3D (hotspot.GetIconPosition ());
				}
				else
				{
					Vector2 screenPos = hotspot.GetIconScreenPosition ();
					Vector2 screenPosition = new Vector2 (screenPos.x / ACScreen.width, 1f - (screenPos.y / ACScreen.height));

					screenPosition = new Vector2 (screenPosition.x * ACScreen.width, (1f - screenPosition.y) * ACScreen.height);
					menu.SetCentre (screenPosition);
				}
				return true;
			}
			return false;
		}


		/**
		 * <summary>Updates a Menu's display and position</summary>
		 * <param name = "menu">The Menu to update</param>
		 * <param name = "languageNumber">The index number of the language to use (0 = default)</param>
		 * <param name = "justPosition">If True, then only the Menu's position will be updated - not its content</param>
		 * <param name = "updateElements">If True, then the Menu's elements will be updated as well</param>
		 */
		public void UpdateMenu (AC.Menu menu, int languageNumber = 0, bool justPosition = false, bool updateElements = true)
		{
			Vector2 invertedMouse = KickStarter.playerInput.GetInvertedMouse ();
			UpdateMenuPosition (menu, invertedMouse);
			if (justPosition)
			{
				return;
			}

			menu.HandleTransition ();

			/*if (menu.IsEnabled ())
			{
				if (!KickStarter.playerMenus.IsCyclingInteractionMenu ())
				{
					Debug.Log ("Input control " + menu.title);
					KickStarter.playerInput.InputControlMenu (menu);
				}
			}

			if (menu.IsOn () && menu.CanCurrentlyKeyboardControl ())
			{
				foundCanKeyboardControl = true;
			}*/

			switch (menu.appearType)
			{
				case AppearType.Manual:
					if (menu.IsVisible () && !menu.isLocked && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
					{
						foundMouseOverMenu = true;
					}
					break;

				case AppearType.OnViewDocument:
					if (KickStarter.runtimeDocuments.ActiveDocument != null && !menu.isLocked && (!KickStarter.stateHandler.IsPaused () || menu.IsBlocking ()))
					{
						if (menu.IsVisible () && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
						{
							foundMouseOverMenu = true;
						}
						menu.TurnOn (true);
					}
					else
					{
						menu.TurnOff (true);
					}
					break;

				case AppearType.DuringGameplay:
					if (KickStarter.stateHandler.IsInGameplay () && !menu.isLocked)
					{
						if (menu.IsOff ())
						{
							menu.TurnOn (true);
						}

						if (menu.IsOn () && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
						{
							foundMouseOverMenu = true;
						}
					}
					else if (KickStarter.stateHandler.gameState == GameState.Paused || KickStarter.stateHandler.gameState == GameState.DialogOptions)
					{
						menu.TurnOff (true);
					}
					else if (menu.IsOn () && KickStarter.actionListManager.IsGameplayBlocked ())
					{
						menu.TurnOff (true);
					}
					break;

				case AppearType.DuringGameplayAndConversations:
					if (!menu.isLocked && (KickStarter.stateHandler.gameState == GameState.Normal || KickStarter.stateHandler.gameState == GameState.DialogOptions))
					{
						if (menu.IsOff ())
						{
							menu.TurnOn (true);
						}

						if (menu.IsOn () && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
						{
							foundMouseOverMenu = true;
						}
					}
					else if (KickStarter.stateHandler.gameState == GameState.Paused)
					{
						menu.TurnOff (true);
					}
					else if (menu.IsOn () && KickStarter.actionListManager.IsGameplayBlocked ())
					{
						menu.TurnOff (true);
					}
					break;

				case AppearType.ExceptWhenPaused:
					if (KickStarter.stateHandler.gameState != GameState.Paused && !menu.isLocked)
					{
						if (menu.IsOff ())
						{
							menu.TurnOn (true);
						}

						if (menu.IsOn () && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
						{
							foundMouseOverMenu = true;
						}
					}
					else if (KickStarter.stateHandler.gameState == GameState.Paused)
					{
						menu.TurnOff (true);
					}
					break;

				case AppearType.DuringCutscene:
					if (KickStarter.stateHandler.gameState == GameState.Cutscene && !menu.isLocked)
					{
						if (menu.IsOff ())
						{
							menu.TurnOn (true);
						}
						
						if (menu.IsOn () && menu.IsPointInside (invertedMouse))
						{
							foundMouseOverMenu = true;
						}
					}
					else if (KickStarter.stateHandler.gameState == GameState.Paused)
					{
						menu.TurnOff (true);
					}
					else if (menu.IsOn () && !KickStarter.actionListManager.IsGameplayBlocked ())
					{
						menu.TurnOff (true);
					}
					break;

				case AppearType.MouseOver:
					if (menu.pauseWhenEnabled)
					{
						if ((KickStarter.stateHandler.gameState == GameState.Paused || KickStarter.stateHandler.IsInGameplay ())
							&& (!menu.isLocked && menu.IsPointInside (invertedMouse) && KickStarter.playerInput.GetDragState () != DragState.Moveable))
						{
							if (menu.IsOff ())
							{
								menu.TurnOn (true);
							}
							
							if (!menu.ignoreMouseClicks)
							{
								foundMouseOverMenu = true;
							}
						}
						else
						{
							menu.TurnOff (true);
						}
					}
					else
					{
						if (KickStarter.stateHandler.IsInGameplay () && !menu.isLocked && menu.IsPointInside (invertedMouse) && KickStarter.playerInput.GetDragState () != DragState.Moveable)
						{
							if (menu.IsOff ())
							{
								menu.TurnOn (true);
							}
							
							if (!menu.ignoreMouseClicks)
							{
								foundMouseOverMenu = true;
							}
						}
						else if (KickStarter.stateHandler.gameState == GameState.Paused)
						{
							menu.ForceOff ();
						}
						else
						{
							menu.TurnOff (true);
						}
					}
					break;

				case AppearType.OnContainer:
					if (KickStarter.playerInput.activeContainer != null && !menu.isLocked && (KickStarter.stateHandler.IsInGameplay () || (KickStarter.stateHandler.gameState == AC.GameState.Paused && menu.IsBlocking ())))
					{
						if (menu.IsVisible () && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
						{
							foundMouseOverMenu = true;
						}
						menu.TurnOn (true);
					}
					else
					{
						menu.TurnOff (true);
					}
					break;

				case AppearType.DuringConversation:
					if (menu.IsEnabled () && !menu.isLocked && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
					{
						foundMouseOverMenu = true;
					}

					if (KickStarter.playerInput.IsInConversation () && KickStarter.stateHandler.gameState == GameState.DialogOptions)
					{
						menu.TurnOn (true);
					}
					else if (KickStarter.stateHandler.gameState == GameState.Paused)
					{
						menu.ForceOff ();
					}
					else
					{
						menu.TurnOff (true);
					}
					break;

				case AppearType.OnInputKey:
					if (menu.IsEnabled () && !menu.isLocked && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
					{
						foundMouseOverMenu = true;
					}
					
					try
					{
						if (KickStarter.playerInput.InputGetButtonDown (menu.toggleKey, true))
						{
							if (!menu.IsEnabled ())
							{
								if (KickStarter.stateHandler.gameState == GameState.Paused)
								{
									CrossFade (menu);
								}
								else
								{
									menu.TurnOn (true);
								}
							}
							else
							{
								menu.TurnOff (true);
							}
						}
					}
					catch
					{
						if (KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen)
						{
							ACDebug.LogWarning ("No '" + menu.toggleKey + "' button exists - please define one in the Input Manager.");
						}
					}
					break;

				case AppearType.OnHotspot:
					if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive && !menu.isLocked && KickStarter.runtimeInventory.SelectedItem == null)
					{
						Hotspot hotspot = KickStarter.playerInteraction.GetActiveHotspot ();
						if (hotspot != null)
						{
							menu.HideInteractions ();

							if (hotspot.HasContextUse ())
							{
								menu.MatchUseInteraction (hotspot.GetFirstUseButton ());
							}

							if (hotspot.HasContextLook ())
							{
								menu.MatchLookInteraction ();
							}

							menu.Recalculate ();
						}
					}

					if (menu.GetsDuplicated ())
					{
						if (KickStarter.stateHandler.gameState == GameState.Cutscene)
						{
							menu.TurnOff ();
						}
						else
						{
							if (menu.TargetInvItem != null)
							{
								InvItem hoverItem = KickStarter.runtimeInventory.hoverItem;
								if (hoverItem != null && menu.TargetInvItem == hoverItem)
								{
									menu.TurnOn ();
								}
								else
								{
									menu.TurnOff ();
								}
							}
							else if (menu.TargetHotspot != null)
							{
								Hotspot hotspot = KickStarter.playerInteraction.GetActiveHotspot ();
								if (hotspot != null && menu.TargetHotspot == hotspot)
								{
									menu.TurnOn ();
								}
								else
								{
									menu.TurnOff ();
								}
							}
							else
							{
								menu.TurnOff ();
							}
						}
					}
					else
					{
						if (!string.IsNullOrEmpty (GetHotspotLabel ()) && !menu.isLocked && KickStarter.stateHandler.gameState != GameState.Cutscene)
						{
							menu.TurnOn (true);
							if (menu.IsUnityUI ())
							{
								// Update position before next frame (Unity UI bug)
								UpdateMenuPosition (menu, invertedMouse);
							}
						}
						else
						{
							menu.TurnOff ();
						}
					}
					break;

				case AppearType.OnInteraction:
					if (KickStarter.player != null && KickStarter.settingsManager.hotspotDetection != HotspotDetection.MouseOver && KickStarter.player.hotspotDetector != null && KickStarter.settingsManager.closeInteractionMenusIfPlayerLeavesVicinity)
					{
						if (menu.TargetHotspot != null && !KickStarter.player.hotspotDetector.IsHotspotInTrigger (menu.TargetHotspot))
						{
							menu.TurnOff ();
							return;
						}
					}

					if (KickStarter.settingsManager.CanClickOffInteractionMenu ())
					{
						if (menu.IsEnabled () && (KickStarter.stateHandler.IsInGameplay () || menu.pauseWhenEnabled || (KickStarter.stateHandler.IsPaused () && menu.TargetInvItem != null && menu.GetGameStateWhenTurnedOn () == GameState.Paused)))
						{
							interactionMenuPauses = menu.pauseWhenEnabled;

							if (menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
							{
								foundMouseOverInteractionMenu = true;
							}
							else if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick)
							{
								if (KickStarter.settingsManager.ShouldCloseInteractionMenu ())
								{
									KickStarter.playerInput.ResetMouseClick ();
									menu.TurnOff (true);
								}
							}
						}
						else if (KickStarter.stateHandler.gameState == GameState.Paused)
						{
							menu.ForceOff ();
						}
						else if (KickStarter.playerInteraction.GetActiveHotspot () == null)
						{
							menu.TurnOff (true);
						}
					}
					else
					{
						if (menu.IsEnabled () && (KickStarter.stateHandler.IsInGameplay () || menu.pauseWhenEnabled || (KickStarter.stateHandler.IsPaused () && menu.TargetInvItem != null && menu.GetGameStateWhenTurnedOn () == GameState.Paused)))
						{
							if (menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
							{
								foundMouseOverInteractionMenu = true;
							}
							else if (!menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks && KickStarter.playerInteraction.GetActiveHotspot () == null && KickStarter.runtimeInventory.hoverItem == null &&
							    (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction || KickStarter.settingsManager.cancelInteractions == CancelInteractions.CursorLeavesMenuOrHotspot))
							{
								menu.TurnOff (true);
							}
							else if (!menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.settingsManager.cancelInteractions == CancelInteractions.CursorLeavesMenu && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.ClickingMenu && !menu.IsFadingIn ())
							{
								menu.TurnOff (true);
							}
							else if (KickStarter.playerInteraction.GetActiveHotspot () == null && KickStarter.runtimeInventory.hoverItem == null &&
							    KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.settingsManager.selectInteractions == AC.SelectInteractions.CyclingMenuAndClickingHotspot)
							{
								menu.TurnOff (true);
							}
							else if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && KickStarter.playerInteraction.GetActiveHotspot () != null)
							{}
							else if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && KickStarter.runtimeInventory.hoverItem != null)
							{}
							else if (KickStarter.playerInteraction.GetActiveHotspot () == null || KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
							{}
							else if (KickStarter.runtimeInventory.SelectedItem == null && KickStarter.playerInteraction.GetActiveHotspot () != null && KickStarter.runtimeInventory.hoverItem != null)
							{
								menu.TurnOff (true);
							}
							else if (KickStarter.runtimeInventory.SelectedItem != null && KickStarter.runtimeInventory.SelectedItem != KickStarter.runtimeInventory.hoverItem)
							{
								menu.TurnOff (true);
							}
						}
						else if (KickStarter.stateHandler.gameState == GameState.Paused)
						{
							if (menu.TargetInvItem != null && menu.GetGameStateWhenTurnedOn () == GameState.Paused)
							{
								// Don't turn off the Menu if it was open for a paused Inventory
							}
							else
							{
								menu.ForceOff ();
							}
						}
						else if (KickStarter.playerInteraction.GetActiveHotspot () == null)
						{
							menu.TurnOff (true);
						}
					}
					break;

				case AppearType.WhenSpeechPlays:
					if (KickStarter.stateHandler.gameState == GameState.Paused)
					{
						if (!menu.showWhenPaused)
						{
							menu.TurnOff ();
						}
					}
					else
					{
						Speech speech = menu.speech;
						if (!menu.oneMenuPerSpeech)
						{
							speech = KickStarter.dialog.GetLatestSpeech ();
						}
						if (speech != null && speech.MenuCanShow (menu))
						{
							if (menu.forceSubtitles ||
								Options.optionsData == null ||
								(Options.optionsData != null && Options.optionsData.showSubtitles) ||
								(KickStarter.speechManager.forceSubtitles && !KickStarter.dialog.FoundAudio ())) 
							{
								menu.TurnOn (true);
							}
							else
							{
								menu.TurnOff (true);	
							}
						}
						else
						{
							menu.TurnOff (true);
						}
					}
					break;

				case AppearType.WhileLoading:
					if (KickStarter.sceneChanger.IsLoading ())
					{
						menu.TurnOn (true);
					}
					else
					{
						menu.TurnOff (true);
					}
					break;

				case AppearType.WhileInventorySelected:
					if (KickStarter.runtimeInventory.SelectedItem != null)
					{
						menu.TurnOn (true);
					}
					else
					{
						menu.TurnOff (true);
					}
					break;
			}

			if (updateElements)
			{
				UpdateElements (menu, languageNumber, justPosition);
			}
		}


		protected void UpdateElements (AC.Menu menu, int languageNumber, bool justDisplay = false)
		{
			if (!menu.HasTransition () && menu.IsFading ())
			{
				// Stop until no longer "fading" so that it appears in right place
				return;
			}

			if (!menu.updateWhenFadeOut && menu.IsFadingOut ())
			{
				return;
			}

			if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard &&
				menu.IsPointInside (KickStarter.playerInput.GetInvertedMouse ()) &&
				!menu.ignoreMouseClicks)
			{
				menuIdentifier = menu.IDString;
				mouseOverMenu = menu;
				mouseOverElement = null;
				mouseOverElementSlot = 0;
			}

			for (int j=0; j<menu.NumElements; j++)
			{
				if ((menu.elements[j].GetNumSlots () == 0 || !menu.elements[j].IsVisible) && menu.menuSource != MenuSource.AdventureCreator)
				{
					menu.elements[j].HideAllUISlots ();
				}

				for (int i=0; i<menu.elements[j].GetNumSlots (); i++)
				{
					bool slotIsActive = (KickStarter.stateHandler.gameState == GameState.Cutscene)
								 		? false
								 		: SlotIsInteractive (menu, j, i);
					menu.elements[j].PreDisplay (i, languageNumber, slotIsActive);

					if (justDisplay)
					{
						continue;
					}

					if (slotIsActive)
					{
						string _hotspotLabelOverride = KickStarter.eventManager.Call_OnRequestMenuElementHotspotLabel (menu, menu.elements[j], i, languageNumber);
						if (string.IsNullOrEmpty (_hotspotLabelOverride))
						{
							_hotspotLabelOverride = menu.elements[j].GetHotspotLabelOverride (i, languageNumber);
						}
						if (!string.IsNullOrEmpty (_hotspotLabelOverride))
						{
							hotspotLabelOverride = _hotspotLabelOverride;
						}
					}

					if (menu.IsVisible () && menu.elements[j].IsVisible && menu.elements[j].isClickable)
					{
						if (i == 0 && !string.IsNullOrEmpty (menu.elements[j].alternativeInputButton))
						{
							if (KickStarter.playerInput.InputGetButtonDown (menu.elements[j].alternativeInputButton))
							{
								CheckClick (menu, menu.elements[j], i, MouseState.SingleClick);
							}
						}
					}

					if (menu.elements[j] is MenuInput)
					{
						MenuInput input = menu.elements[j] as MenuInput;
						if (selectedInputBox == null && (SlotIsInteractive (menu, j, 0) || !input.requireSelection)) // OLD
						{
							if (!menu.IsUnityUI ())
							{
								SelectInputBox (input);
							}
							
							selectedInputBoxMenuName = menu.title;
						}
						else if (selectedInputBox == input && !SlotIsInteractive (menu, j, 0) && input.requireSelection)
						{
							if (!menu.IsUnityUI ())
							{
								DeselectInputBox ();
							}
						}
					}

					if (menu.elements[j].IsVisible && SlotIsInteractive (menu, j, i))
					{
						if ((!interactionMenuIsOn || menu.appearType == AppearType.OnInteraction)
							&& (KickStarter.playerInput.GetDragState () == DragState.None || (KickStarter.playerInput.GetDragState () == DragState.Inventory && CanElementBeDroppedOnto (menu.elements[j]))))
						{
							if (lastElementIdentifier != (menu.IDString + menu.elements[j].IDString + i.ToString ()))
							{
								KickStarter.sceneSettings.PlayDefaultSound (menu.elements[j].GetHoverSound (i), false);
							}
						}

						if (!menu.ignoreMouseClicks)
						{
							elementIdentifier = menu.IDString + menu.elements[j].IDString + i.ToString ();

							mouseOverMenu = menu;
							mouseOverElement = menu.elements[j];
							mouseOverElementSlot = i;
						}

						if (KickStarter.stateHandler.gameState != GameState.Cutscene)
						{
							if (menu.elements[j] is MenuInventoryBox)
							{
								if (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused ||
									(KickStarter.stateHandler.gameState == GameState.DialogOptions && (KickStarter.settingsManager.allowInventoryInteractionsDuringConversations || KickStarter.settingsManager.allowGameplayDuringConversations)))
								{
									if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Single && KickStarter.runtimeInventory.SelectedItem == null)
									{
										KickStarter.playerCursor.ResetSelectedCursor ();
									}

									MenuInventoryBox inventoryBox = (MenuInventoryBox) menu.elements[j];
									if (inventoryBox.inventoryBoxType == AC_InventoryBoxType.HotspotBased)
									{
										if (!menu.ignoreMouseClicks)
										{
											KickStarter.runtimeInventory.UpdateSelectItemModeForMenu (inventoryBox, i);
										}
									}
									else
									{
										foundMouseOverInventory = true;
										if (!isMouseOverInteractionMenu)
										{
											if (interactionMenuIsOn &&
												KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.ClickingMenu &&
												KickStarter.settingsManager.CanClickOffInteractionMenu ())
											{
												return;
											}

											InvItem newHoverItem = inventoryBox.GetItem (i);
											KickStarter.runtimeInventory.SetHoverItem (newHoverItem, inventoryBox);

											if (oldHoverItem != newHoverItem)
											{
												KickStarter.runtimeInventory.MatchInteractions ();
												KickStarter.playerInteraction.RestoreInventoryInteraction ();
												activeInventoryBox = inventoryBox;
												activeCrafting = null;
												activeInventoryBoxMenu = menu;

												if (interactionMenuIsOn)
												{
													CloseInteractionMenus ();
												}
											}
										}
									}
								}
							}
							else if (menu.elements[j] is MenuCrafting)
							{
								if (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused)
								{
									MenuCrafting crafting = (MenuCrafting) menu.elements[j];
									KickStarter.runtimeInventory.SetHoverItem (crafting.GetItem (i), crafting);

									if (KickStarter.runtimeInventory.hoverItem != null)
									{
										activeCrafting = crafting;
										activeInventoryBox = null;
										activeInventoryBoxMenu = menu;
									}

									foundMouseOverInventory = true;
								}
							}
						}
					}
				}
			}
		}


		/**
		 * <summary>Checks if the Unity UI EventSystem currently has a given GameObject selected.</summary>
		 * <param name = "_gameObject">The GameObject to check</param>
		 * <returns>True if the Unity UI EventSystem currently has a given GameObject selected</returns>
		 */
		public bool IsEventSystemSelectingObject (GameObject _gameObject)
		{
			if (eventSystem != null && _gameObject != null && eventSystem.currentSelectedGameObject == _gameObject)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the Unity UI EventSystem currently has any GameObject selected.</summary>
		 * <returns>True if the Unity UI EventSystem currently has any GameObject selected</returns>
		 */
		public bool IsEventSystemSelectingObject ()
		{
			if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
			{
				return true;
			}
			return false;
		}


		/**
		 * The EventSystem used by AC Menus.
		 */
		public UnityEngine.EventSystems.EventSystem EventSystem
		{
			get
			{
				return eventSystem;
			}
		}


		/**
		 * The alpha value of the full-screen texture drawn when the game is paused.  This is set even if no pause texture is assigned.
		 */
		public float PauseTextureAlpha
		{
			get
			{
				return pauseAlpha;
			}
		}


		/**
		 * <summary>De-selects the Unity UI EventSystem's selected gameobject if it is associated with a given Menu.</summary>
		 * <param name = "_menu">The Menu to deselect</param>
		 * <returns>True if the Unity UI EventSystem's selected gameobject was in the given Menu</returns>
		 */
		public bool DeselectEventSystemMenu (Menu _menu)
		{
			if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
			{
				if (_menu.menuSource != MenuSource.AdventureCreator && _menu.RuntimeCanvas != null && _menu.RuntimeCanvas.gameObject != null)
				{
					if (eventSystem.currentSelectedGameObject.transform.IsChildOf (_menu.RuntimeCanvas.transform))
					{
						eventSystem.SetSelectedGameObject (null);
						return true;
					}
				}
			}
			return false;
		}


		protected bool SlotIsInteractive (AC.Menu menu, int elementIndex, int slotIndex)
		{
			if (!menu.IsVisible () || !menu.elements[elementIndex].isClickable || !menu.elements[elementIndex].IsVisible)
			{
				return false;
			}

			if (KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController ||
				(KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard && menu.CanCurrentlyKeyboardControl () && menu.menuSource == MenuSource.AdventureCreator))
			{
				if (menu.menuSource != MenuSource.AdventureCreator)
				{
					return menu.IsElementSelectedByEventSystem (elementIndex, slotIndex);
				}

				if (KickStarter.stateHandler.IsInGameplay ())
				{
					if (!KickStarter.playerInput.canKeyboardControlMenusDuringGameplay && menu.IsPointerOverSlot (menu.elements[elementIndex], slotIndex, KickStarter.playerInput.GetInvertedMouse ()))
					{
						return true;
					}
					else if (KickStarter.playerInput.canKeyboardControlMenusDuringGameplay && menu.CanPause () && !menu.pauseWhenEnabled && menu.selected_element == menu.elements[elementIndex] && menu.selected_slot == slotIndex)
					{
						return true;
					}
				}
				else if (KickStarter.stateHandler.gameState == GameState.Cutscene)
				{
					if (menu.CanClickInCutscenes () && menu.selected_element == menu.elements[elementIndex] && menu.selected_slot == slotIndex)
					{
						return true;
					}
				}
				else if (KickStarter.stateHandler.gameState == GameState.DialogOptions)
				{
					if (KickStarter.menuManager.keyboardControlWhenDialogOptions)
					{
						if (menu.selected_element == menu.elements[elementIndex] && menu.selected_slot == slotIndex)
						{
							return true;
						}
					}
					else
					{
						if (menu.IsPointerOverSlot (menu.elements[elementIndex], slotIndex, KickStarter.playerInput.GetInvertedMouse ()))
						{
							return true;
						}
					}
				}
				else if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					if (KickStarter.menuManager.keyboardControlWhenPaused)
					{
						if (menu.selected_element == menu.elements[elementIndex] && menu.selected_slot == slotIndex)
						{
							return true;
						}
					}
					else
					{
						if (menu.IsPointerOverSlot (menu.elements[elementIndex], slotIndex, KickStarter.playerInput.GetInvertedMouse ()))
						{
							return true;
						}
					}
				}
			}
			else if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard)
			{
				return menu.IsPointerOverSlot (menu.elements[elementIndex], slotIndex, KickStarter.playerInput.GetInvertedMouse ());
			}
			else if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				return menu.IsPointerOverSlot (menu.elements[elementIndex], slotIndex, KickStarter.playerInput.GetInvertedMouse ());
			}

			return false;
		}

		
		protected void CheckClicks (AC.Menu menu)
		{
			if (!menu.HasTransition () && menu.IsFading ())
			{
				// Stop until no longer "fading" so that it appears in right place
				return;
			}

			if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard &&
				menu.IsPointInside (KickStarter.playerInput.GetInvertedMouse ()) &&
				!menu.ignoreMouseClicks)
			{
				menuIdentifier = menu.IDString;
				mouseOverMenu = menu;
				mouseOverElement = null;
				mouseOverElementSlot = 0;
			}

			for (int j=0; j<menu.NumElements; j++)
			{
				if (menu.elements[j].IsVisible)
				{
					for (int i=0; i<menu.elements[j].GetNumSlots (); i++)
					{
						if (SlotIsInteractive (menu, j, i))
						{
							if (!menu.IsUnityUI () && KickStarter.playerInput.GetMouseState () != MouseState.Normal && (KickStarter.playerInput.GetDragState () == DragState.None || KickStarter.playerInput.GetDragState () == DragState.Menu))
							{
								if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || KickStarter.playerInput.GetMouseState () == MouseState.LetGo || KickStarter.playerInput.GetMouseState () == MouseState.RightClick)
								{
									if (menu.elements[j] is MenuInput) {}
									else DeselectInputBox ();
									
									CheckClick (menu, menu.elements[j], i, KickStarter.playerInput.GetMouseState ());
								}
								else if (KickStarter.playerInput.GetMouseState () == MouseState.HeldDown)
								{
									CheckContinuousClick (menu, menu.elements[j], i, KickStarter.playerInput.GetMouseState ());
								}
							}
							else if (menu.IsUnityUI () && KickStarter.runtimeInventory.SelectedItem == null &&  KickStarter.settingsManager.InventoryDragDrop && KickStarter.playerInput.GetMouseState () == MouseState.HeldDown && KickStarter.playerInput.GetDragState () == DragState.None)
							{
								if (menu.elements[j] is MenuInventoryBox || menu.elements[j] is MenuCrafting)
								{
									// Begin UI drag drop
									CheckClick (menu, menu.elements[j], i, MouseState.SingleClick);
								}
							}
							else if (KickStarter.playerInteraction.IsDroppingInventory () && CanElementBeDroppedOnto (menu.elements[j]))
							{
								if (menu.IsUnityUI () && KickStarter.settingsManager.InventoryDragDrop && (menu.elements[j] is MenuInventoryBox || menu.elements[j] is MenuCrafting))
								{
									// End UI drag drop
									menu.elements[j].ProcessClick (menu, i, MouseState.SingleClick);
								}
								else if (!menu.IsUnityUI ())
								{
									DeselectInputBox ();
									CheckClick (menu, menu.elements[j], i, MouseState.SingleClick);
								}
							}
							else if (menu.IsUnityUI () && KickStarter.playerInput.GetMouseState () == MouseState.HeldDown)
							{
								CheckContinuousClick (menu, menu.elements[j], i, KickStarter.playerInput.GetMouseState ());
							}

						}
					}
				}
			}
		}


		/**
		 * Refreshes any active MenuDialogList elements, after changing the state of dialogue options.
		 */
		public void RefreshDialogueOptions ()
		{
			Menu[] allMenus = GetMenus (true).ToArray ();
			foreach (Menu menu in allMenus)
			{
				menu.RefreshDialogueOptions ();
			}
		}


		/**
		 * Updates the state of all Menus set to appear while the game is loading.
		 */
		public void UpdateLoadingMenus ()
		{
			int languageNumber = Options.GetLanguage ();

			for (int i=0; i<menus.Count; i++)
			{
				if (menus[i].appearType == AppearType.WhileLoading)
				{
					UpdateMenu (menus[i], languageNumber, false, menus[i].IsEnabled ());
				}
			}
		}


		/**
		 * Checks for inputs made to all Menus.
		 * This is called every frame by StateHandler.
		 */
		public void CheckForInput ()
		{
			//if (Time.time > 0f)
			{
				if (customMenus != null)
				{
					for (int i=customMenus.Count-1; i>=0; i--)
					{
						CheckForInput (customMenus[i]);
					}
				}

				for (int i=dupSpeechMenus.Count-1; i>=0; i--)
				{
					CheckForInput (dupSpeechMenus[i]);
				}

				for (int i=menus.Count-1; i>=0; i--)
				{
					CheckForInput (menus[i]);
				}
			}
		}


		private void CheckForInput (Menu menu)
		{
			if (menu.IsEnabled () && !menu.ignoreMouseClicks)
			{
				CheckClicks (menu);
			}
		}


		private void CheckForDirectNav ()
		{
			for (int i=customMenus.Count-1; i >= 0; i--)
			{
				CheckForDirectNav (customMenus[i]);
				if (foundCanKeyboardControl) return;
			}

			for (int i=dupSpeechMenus.Count-1; i >= 0; i--)
			{
				CheckForDirectNav (dupSpeechMenus[i]);
				if (foundCanKeyboardControl) return;
			}

			for (int i=menus.Count-1; i >= 0; i--)
			{
				CheckForDirectNav (menus[i]);
				if (foundCanKeyboardControl) return;
			}
		}


		private void CheckForDirectNav (Menu menu)
		{
			if (foundCanKeyboardControl) return;

			if (menu.IsEnabled ())
			{
				if (!KickStarter.playerMenus.IsCyclingInteractionMenu ())
				{
					KickStarter.playerInput.InputControlMenu (menu);
				}
			}

			if (menu.IsOn () && menu.CanCurrentlyKeyboardControl ())
			{
				foundCanKeyboardControl = true;
			}
		}


		/**
		 * Updates the state of all Menus.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateAllMenus ()
		{
			#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
			if (keyboard != null && selectedInputBox != null)
			{
				selectedInputBox.label = keyboard.text;
			}
			#endif

			interactionMenuIsOn = AreInteractionMenusOn ();
			
			if (doResizeMenus > 0)
			{
				doResizeMenus ++;
				
				if (doResizeMenus == 4)
				{
					doResizeMenus = 0;
					List<Menu> menus = GetMenus (true);
					foreach (Menu menu in menus)
					{
						menu.Recalculate ();
						KickStarter.mainCamera.SetCameraRect ();
						menu.Recalculate ();
					}
				}
			}

			//if (Time.time > 0f)
			{
				int languageNumber = Options.GetLanguage ();

				if (!interactionMenuIsOn || !isMouseOverInteractionMenu)
				{
					oldHoverItem = KickStarter.runtimeInventory.hoverItem;
					KickStarter.runtimeInventory.hoverItem = null;
				}
				
				if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					if (Time.timeScale > 0f && KickStarter.stateHandler.IsACEnabled ())
					{
						KickStarter.sceneSettings.PauseGame ();
					}
				}

				elementIdentifier = string.Empty;
				foundMouseOverMenu = false;
				foundMouseOverInteractionMenu = false;
				foundMouseOverInventory = false;
				foundCanKeyboardControl = false;

				hotspotLabelOverride = string.Empty;

				for (int i=0; i<menus.Count; i++)
				{
					UpdateMenu (menus[i], languageNumber, false, menus[i].IsEnabled ());
					if (!menus[i].IsEnabled () && menus[i].IsOff () && menuIdentifier == menus[i].IDString)
					{
						menuIdentifier = string.Empty;
					}
				}

				for (int i=0; i<dupSpeechMenus.Count; i++)
				{
					UpdateMenu (dupSpeechMenus[i], languageNumber);

					if (dupSpeechMenus[i].IsOff () && KickStarter.stateHandler.gameState != GameState.Paused)
					{
						Menu oldMenu = dupSpeechMenus[i];
						dupSpeechMenus.RemoveAt (i);
						if (oldMenu.menuSource != MenuSource.AdventureCreator && oldMenu.RuntimeCanvas != null && oldMenu.RuntimeCanvas.gameObject != null)
						{
							DestroyImmediate (oldMenu.RuntimeCanvas.gameObject);
						}
						DestroyImmediate (oldMenu);
						i=0;
					}
				}

				for (int i=0; i<customMenus.Count; i++)
				{
					UpdateMenu (customMenus[i], languageNumber, false, customMenus[i].IsEnabled ());
					if (customMenus.Count > i && customMenus[i] != null && !customMenus[i].IsEnabled () && customMenus[i].IsOff () && menuIdentifier == customMenus[i].IDString)
					{
						menuIdentifier = string.Empty;
					}
				}

				CheckForDirectNav ();

				UpdatePauseBackground (ArePauseMenusOn ());

				isMouseOverMenu = foundMouseOverMenu;
				isMouseOverInteractionMenu = foundMouseOverInteractionMenu;
				isMouseOverInventory = foundMouseOverInventory;
				canKeyboardControl = foundCanKeyboardControl;

				if (mouseOverMenu != null && (lastElementIdentifier != elementIdentifier || lastMenuIdentifier != menuIdentifier))
				{
					KickStarter.eventManager.Call_OnMouseOverMenuElement (mouseOverMenu, mouseOverElement, mouseOverElementSlot);
				}
				lastElementIdentifier = elementIdentifier;
				lastMenuIdentifier = menuIdentifier;

				UpdateAllMenusAgain ();
			}
		}


		protected void UpdateAllMenusAgain ()
		{
			// We actually need to go through menu calculations twice before displaying, to update any inter-dependencies between menus
			int languageNumber = Options.GetLanguage ();

			for (int i=0; i<menus.Count; i++)
			{
				UpdateMenu (menus[i], languageNumber, true, menus[i].IsEnabled ());
			}
		}
		

		/**
		 * <summary>Begins fading in the second Menu in a crossfade if the first Menu matches the supplied parameter.</summary>
		 * <param name = "_menu">The Menu to check for. If this menu is crossfading out, then it will be turned off, and the second Menu will fade in</param>
		 */
		public void CheckCrossfade (AC.Menu _menu)
		{
			if (crossFadeFrom == _menu && crossFadeTo != null)
			{
				crossFadeFrom.ForceOff ();
				crossFadeTo.TurnOn (true);
				crossFadeTo = null;
			}
		}
		

		/**
		 * <summary>Selects a MenuInput element, allowing the player to enter text into it.</summary>
		 * <param name = "input">The input box to select</param>
		 */
		public virtual void SelectInputBox (MenuInput input)
		{
			selectedInputBox = input;

			// Mobile keyboard
			#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
			TouchScreenKeyboardType keyboardType = (input.inputType == AC_InputType.NumbericOnly)
													? TouchScreenKeyboardType.NumberPad
													: TouchScreenKeyboardType.ASCIICapable;
			
			#if UNITY_2018_3_OR_NEWER
			keyboard.characterLimit = input.characterLimit;
			#endif

			keyboard = TouchScreenKeyboard.Open (input.label, keyboardType, false, false, false, false, string.Empty);

			#endif
		}


		/**
		 * <summary>Called automatically whenever a Menu is turned off</summary>
		 * <param name = "menu">The Menu that was turned off</param>
		 */
		public void OnTurnOffMenu (Menu menu)
		{
			if (selectedInputBox != null && menu != null)
			{
				foreach (MenuElement menuElement in menu.elements)
				{
					if (menuElement != null && menuElement == selectedInputBox)
					{
						DeselectInputBox ();
					}
				}
			}
		}


		/**
		 * <summary>Deselects the active InputBox element if it is the one in the parameter</summary>
		 * <param name = "menuElement">The InputBox element that should be disabled</param>
		 */
		public void DeselectInputBox (MenuElement menuElement)
		{
			if (selectedInputBox != null && menuElement == selectedInputBox)
			{
				DeselectInputBox ();
			}
		}
		
		
		protected virtual void DeselectInputBox ()
		{
			if (selectedInputBox)
			{
				selectedInputBox.Deselect ();
				selectedInputBox = null;
				
				// Mobile keyboard
				#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
				if (keyboard != null)
				{
					keyboard.active = false;
					keyboard = null;
				}
				#endif
			}
		}
		
		
		protected void CheckClick (AC.Menu _menu, MenuElement _element, int _slot, MouseState _mouseState)
		{
			if (_menu == null || _element == null)
			{
				return;
			}

			KickStarter.playerInput.ResetMouseClick ();
			if (_mouseState == MouseState.LetGo)
			{
				if (_menu.appearType == AppearType.OnInteraction)
				{
					if (KickStarter.settingsManager.ReleaseClickInteractions () && !KickStarter.settingsManager.CanDragCursor () && KickStarter.runtimeInventory.SelectedItem == null)
					{
						_mouseState = MouseState.SingleClick;
					}
					else
					{
						_mouseState = MouseState.Normal;
					}
				}
				else if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && !KickStarter.settingsManager.CanDragCursor () && KickStarter.runtimeInventory.SelectedItem == null && !(_element is MenuInventoryBox) && !(_element is MenuCrafting))
				{
					_mouseState = MouseState.SingleClick;
				}
				else
				{
					_mouseState = MouseState.Normal;
					return;
				}
			}

			if (_mouseState != MouseState.Normal)
			{
				_element.ProcessClick (_menu, _slot, _mouseState);
				PlayerMenus.ResetInventoryBoxes ();
			}
		}
		
		
		protected void CheckContinuousClick (AC.Menu _menu, MenuElement _element, int _slot, MouseState _mouseState)
		{
			if (!_menu.IsClickable ())
			{
				return;
			}

			_element.ProcessContinuousClick (_menu, _mouseState);
		}


		/**
		 * <summary>Unassigns a Speech line from any temporarily-duplicated Menus. This will signal such Menus that they can be removed.</summary>
		 * <param name = "speech">The Speech line to unassign</param>
		 */
		public void RemoveSpeechFromMenu (Speech speech)
		{
			foreach (Menu menu in dupSpeechMenus)
			{
				if (menu.speech == speech)
				{
					menu.speech = null;
				}
			}
		}


		/**
		 * <summary>Duplicates any Menu set to display a single speech line.</summary>
		 * <param name = "speech">The Speech line to assign to any duplicated Menu</param>
		 */
		public void AssignSpeechToMenu (Speech speech)
		{
			foreach (Menu menu in menus)
			{
				if (menu.appearType == AppearType.WhenSpeechPlays && menu.oneMenuPerSpeech && speech.MenuCanShow (menu))
				{
					if (menu.forceSubtitles || Options.optionsData == null || (Options.optionsData != null && Options.optionsData.showSubtitles) || (KickStarter.speechManager.forceSubtitles && !KickStarter.dialog.FoundAudio ())) 
					{
						Menu dupMenu = ScriptableObject.CreateInstance <Menu>();
						dupSpeechMenus.Add (dupMenu);
						dupMenu.DuplicateInGame (menu);
						dupMenu.SetSpeech (speech);

						if (dupMenu.IsUnityUI ())
						{
							dupMenu.LoadUnityUI ();
						}
						dupMenu.Recalculate ();
						dupMenu.Initalise ();
						dupMenu.TurnOn (true);
					}
				}
			}
		}


		/**
		 * <summary>Gets all Menus associated with a given Speech line</summary>
		 * <param name = "speech">The Speech line in question</param>
		 * <returns>An array of all Menus associated with the Speech line</returns>
		 */
		public Menu[] GetMenusAssignedToSpeech (Speech speech)
		{
			if (speech == null) return new Menu[0];

			List<Menu> assignedMenus = new List<Menu>();

			foreach (Menu menu in menus)
			{
				if (menu.speech == speech)
				{
					assignedMenus.Add (menu);
				}
			}

			foreach (Menu dupSpeechMenu in dupSpeechMenus)
			{
				if (dupSpeechMenu.speech == speech)
				{
					assignedMenus.Add (dupSpeechMenu);
				}
			}

			return assignedMenus.ToArray ();
		}


		/**
		 * <summary>Crossfades to a Menu. Any other Menus will be turned off.</summary>
		 * <param name = "_menuTo">The Menu to crossfade to</param>
		 */
		public void CrossFade (AC.Menu _menuTo)
		{
			if (_menuTo.isLocked)
			{
				ACDebug.Log ("Cannot crossfade to menu " + _menuTo.title + " as it is locked.");
			}
			else if (!_menuTo.IsEnabled ())
			{
				// Turn off all other menus
				crossFadeFrom = null;
				
				foreach (AC.Menu menu in menus)
				{
					if (menu.IsVisible ())
					{
						if (menu.appearType == AppearType.OnHotspot || menu.fadeSpeed <= 0 || !menu.HasTransition ())
						{
							menu.ForceOff ();
						}
						else
						{
							if (menu.appearType == AppearType.DuringConversation && KickStarter.playerInput.IsInConversation ())
							{
								ACDebug.LogWarning ("Cannot turn off Menu '" + menu.title + "' as a Conversation is currently active.");
								continue;
							}

							menu.TurnOff (true);
							crossFadeFrom = menu;
						}
					}
					else
					{
						menu.ForceOff ();
					}
				}
				
				if (crossFadeFrom != null)
				{
					crossFadeTo = _menuTo;
				}
				else
				{
					_menuTo.TurnOn (true);
				}
			}
		}


		/**
		 * <summary>Closes all "Interaction" Menus.</summary>
		 */
		public void CloseInteractionMenus ()
		{
			SetInteractionMenus (false, null, null);
		}
		

		protected void SetInteractionMenus (bool turnOn, Hotspot _hotspotFor, InvItem _itemFor)
		{
			// Bugfix: menus sometimes being turned on and off in one frame
			if (turnOn)
			{
				KickStarter.playerInput.ResetMouseClick ();
			}

			foreach (AC.Menu _menu in menus)
			{
				if (_menu.appearType == AppearType.OnInteraction)
				{
					if (turnOn)
					{
						InteractionMenuData interactionMenuData = new InteractionMenuData (_menu, _hotspotFor, _itemFor);
						interactionMenuPauses = _menu.pauseWhenEnabled;

						StopCoroutine ("SwapInteractionMenu");
						StartCoroutine ("SwapInteractionMenu", interactionMenuData);
					}
					else
					{
						_menu.TurnOff (true);
					}
				}
			}

			if (turnOn)
			{
				KickStarter.eventManager.Call_OnEnableInteractionMenus (_hotspotFor, _itemFor);
			}
		}


		protected struct InteractionMenuData
		{
			public Menu menuFor;
			public Hotspot hotspotFor;
			public InvItem itemFor;

			public InteractionMenuData (Menu _menuFor, Hotspot _hotspotFor, InvItem _itemFor)
			{
				menuFor = _menuFor;
				hotspotFor = _hotspotFor;
				itemFor = _itemFor;
			}
		}


		/**
		 * <summary>Shows any Menus with appearType = AppearType.OnInteraction, and connected to a given Hotspot.</summary>
		 * <param name = "_hotspotFor">The Hotspot to connect the Menus to.</param>
		 */
		public void EnableInteractionMenus (Hotspot hotspotFor)
		{
			SetInteractionMenus (true, hotspotFor, null);
		}


		/**
		 * <summary>Shows any Menus with appearType = AppearType.OnInteraction, and connected to a given Inventory item to.</summary>
		 * <param name = "_itemFor">The Inventory item to connect the Menus to.</param>
		 */
		public void EnableInteractionMenus (InvItem itemFor)
		{
			SetInteractionMenus (true, null, itemFor);
		}


		protected IEnumerator SwapInteractionMenu (InteractionMenuData interactionMenuData)
		{
			if (interactionMenuData.itemFor == null)
			{
				interactionMenuData.itemFor = KickStarter.runtimeInventory.hoverItem;
			}
			if (interactionMenuData.hotspotFor == null)
			{
				interactionMenuData.hotspotFor = KickStarter.playerInteraction.GetActiveHotspot ();
			}

			if (interactionMenuData.itemFor != null && interactionMenuData.menuFor.TargetInvItem != interactionMenuData.itemFor)
			{
				interactionMenuData.menuFor.TurnOff (true);
			}
			else if (interactionMenuData.hotspotFor  != null && interactionMenuData.menuFor.TargetHotspot != interactionMenuData.hotspotFor)
			{
				interactionMenuData.menuFor.TurnOff (true);
			}

			while (interactionMenuData.menuFor.IsFading ())
			{
				yield return new WaitForFixedUpdate ();
			}

			KickStarter.playerInteraction.ResetInteractionIndex ();

			if (interactionMenuData.itemFor != null)
			{
				interactionMenuData.menuFor.MatchInteractions (interactionMenuData.itemFor, KickStarter.settingsManager.cycleInventoryCursors);
			}
			else if (interactionMenuData.hotspotFor != null)
			{
				interactionMenuData.menuFor.MatchInteractions (interactionMenuData.hotspotFor, KickStarter.settingsManager.cycleInventoryCursors);
			}

			interactionMenuData.menuFor.TurnOn (true);
		}


		/**
		 * Turns off any Menus with appearType = AppearType.OnHotspot.
		 */
		public void DisableHotspotMenus ()
		{
			Menu[] allMenus = GetMenus (true).ToArray ();
			foreach (AC.Menu _menu in allMenus)
			{
				if (_menu.appearType == AppearType.OnHotspot)
				{
					_menu.ForceOff ();
				}
			}
		}
		

		/**
		 * <summary>Gets the complete Hotspot label to be displayed in a MenuLabel element with labelType = AC_LabelType.Hotspot.</summary>
		 * <returns>The complete Hotspot label to be displayed in a MenuLabel element with labelType = AC_LabelType.Hotspot</returns>
		 */
		public string GetHotspotLabel ()
		{
			if (!string.IsNullOrEmpty (hotspotLabelOverride))
			{
				return hotspotLabelOverride;
			}
			return KickStarter.playerInteraction.InteractionLabel;
		}
		
		
		protected void SetStyles (MenuElement element)
		{
			normalStyle.normal.textColor = element.fontColor;
			normalStyle.font = element.font;
			normalStyle.fontSize = element.GetFontSize ();
			normalStyle.alignment = TextAnchor.MiddleCenter;

			highlightedStyle.font = element.font;
			highlightedStyle.fontSize = element.GetFontSize ();
			highlightedStyle.normal.textColor = element.fontHighlightColor;
			highlightedStyle.normal.background = element.highlightTexture;
			highlightedStyle.alignment = TextAnchor.MiddleCenter;
		}
		
		
		protected bool CanElementBeDroppedOnto (MenuElement element)
		{
			if (element is MenuInventoryBox)
			{
				MenuInventoryBox inventoryBox = (MenuInventoryBox) element;
				if (inventoryBox.inventoryBoxType == AC_InventoryBoxType.Default || inventoryBox.inventoryBoxType == AC_InventoryBoxType.Container || inventoryBox.inventoryBoxType == AC_InventoryBoxType.CustomScript)
				{
					return true;
				}
			}
			else if (element is MenuCrafting)
			{
				MenuCrafting crafting = (MenuCrafting) element;
				if (crafting.craftingType == CraftingElementType.Ingredients)
				{
					return true;
				}
			}
			
			return false;
		}
		
		
		protected void OnDestroy ()
		{
			menus = null;
		}


		/**
		 * <summary>Gets a List of all defined Menus.</summary>
		 * <param name = "includeDuplicatesAndCustom">If True, then duplicate and custom Menus will also be included in the returned List</param>
		 * <returns>A List of all defined Menus</returns>
		 */
		public static List<Menu> GetMenus (bool includeDuplicatesAndCustom = false)
		{
			if (KickStarter.playerMenus)
			{
				if (KickStarter.playerMenus.menus.Count == 0 && KickStarter.menuManager != null && KickStarter.menuManager.menus.Count > 0)
				{
					ACDebug.LogError ("A custom script is calling 'PlayerMenus.GetMenus ()' before the Menus have been initialised - consider adjusting your script's Script Execution Order.");
					return null;
				}

				if (!includeDuplicatesAndCustom)
				{
					return KickStarter.playerMenus.menus;
				}

				List<Menu> allMenus = new List<Menu>();
				foreach (Menu menu in KickStarter.playerMenus.menus)
				{
					allMenus.Add (menu);
				}
				foreach (Menu menu in KickStarter.playerMenus.dupSpeechMenus)
				{
					allMenus.Add (menu);
				}
				foreach (Menu menu in KickStarter.playerMenus.customMenus)
				{
					allMenus.Add (menu);
				}
				return allMenus;
			}
			return null;
		}
		

		/**
		 * <summary>Gets a Menu with a specific name.</summary>
		 * <param name = "menuName">The name (title) of the Menu to find</param>
		 * <returns>The Menu with the specific name</returns>
		 */
		public static Menu GetMenuWithName (string menuName)
		{
			menuName = AdvGame.ConvertTokens (menuName);

			if (KickStarter.playerMenus && KickStarter.playerMenus.menus != null)
			{
				if (KickStarter.playerMenus.menus.Count == 0 && KickStarter.menuManager != null && KickStarter.menuManager.menus.Count > 0)
				{
					ACDebug.LogError ("A custom script is calling 'PlayerMenus.GetMenuWithName ()' before the Menus have been initialised - consider adjusting your script's Script Execution Order.");
					return null;
				}

				for (int i=0; i<KickStarter.playerMenus.menus.Count; i++)
				{
					if (KickStarter.playerMenus.menus[i].title == menuName)
					{
						return KickStarter.playerMenus.menus[i];
					}
				}
			}

			ACDebug.LogWarning ("Couldn't find menu with the name '"+ menuName + "'");
			return null;
		}
		

		/**
		 * <summary>Gets a MenuElement with a specific name.</summary>
		 * <param name = "menuName">The name (title) of the Menu to find</param>
		 * <param name = "menuElementName">The name (title) of the MenuElement with the Menu to find</param>
		 * <returns>The MenuElement with the specific name</returns>
		 */
		public static MenuElement GetElementWithName (string menuName, string menuElementName)
		{
			if (KickStarter.playerMenus && KickStarter.playerMenus.menus != null)
			{
				menuName = AdvGame.ConvertTokens (menuName);
				menuElementName = AdvGame.ConvertTokens (menuElementName);

				if (KickStarter.playerMenus.menus.Count == 0 && KickStarter.menuManager != null && KickStarter.menuManager.menus.Count > 0)
				{
					ACDebug.LogError ("A custom script is calling 'PlayerMenus.GetElementWithName ()' before the Menus have been initialised - consider adjusting your script's Script Execution Order.");
					return null;
				}

				foreach (AC.Menu menu in KickStarter.playerMenus.menus)
				{
					if (menu.title == menuName)
					{
						foreach (MenuElement menuElement in menu.elements)
						{
							if (menuElement.title == menuElementName)
							{
								return menuElement;
							}
						}
					}
				}
			}

			ACDebug.LogWarning ("Couldn't find menu element '"+ menuElementName + "' in a menu named '" + menuName + "'");
			return null;
		}
		

		/**
		 * <summary>Checks if saving cannot be performed at this time.</summary>
		 * <param name = "_actionToIgnore">Any gameplay-blocking ActionList that contains this Action will be excluded from the check</param>
		 * <param name = "showDebug">If True, then a warning message will be displayed in the Console if saving is not possible, with further information</param>
		 * <returns>True if saving cannot be performed at this time</returns>
		 */
		public static bool IsSavingLocked (Action _actionToIgnore = null, bool showDebug = false)
		{
			if (KickStarter.stateHandler.gameState == GameState.DialogOptions)
			{
				if (KickStarter.settingsManager.allowGameplayDuringConversations)
				{
					if (KickStarter.actionListManager.IsOverrideConversationRunning ())
					{
						if (showDebug)
						{
							ACDebug.LogWarning ("Cannot save at this time - a Conversation is active.");
						}
						return true;
					}
				}
				else
				{
					if (showDebug)
					{
						ACDebug.LogWarning ("Cannot save at this time - a Conversation is active.");
					}
					return true;
				}
			}

			if (KickStarter.stateHandler.gameState == GameState.Paused && KickStarter.playerInput.IsInConversation ())
			{
				if (showDebug)
				{
					ACDebug.LogWarning ("Cannot save at this time - a Conversation is active.");
				}
				return true;
			}

			if (KickStarter.actionListManager.IsGameplayBlocked (_actionToIgnore, showDebug))
			{
				return true;
			}

			if (KickStarter.playerMenus.lockSave)
			{
				ACDebug.LogWarning ("Cannot save at this time - saving has been manually locked.");
				return true;
			}

			return false;
		}
		

		/**
		 * Calls RecalculateSize() on all MenuInventoryBox elements.
		 */
		public static void ResetInventoryBoxes ()
		{
			if (KickStarter.playerMenus)
			{
				for (int i=0; i<KickStarter.playerMenus.menus.Count; i++)
		        {
		            Menu menu = KickStarter.playerMenus.menus[i];
		            bool update = false;
		            for (int j=0; j<menu.elements.Count; j++)
		            {
		                if (menu.elements[j] is MenuInventoryBox)
		                {
		                    update = true;
		                }
		            }
		            if (update)
		            {
						menu.Recalculate ();
		            }
		        }

		        for (int i=0; i<KickStarter.playerMenus.customMenus.Count; i++)
		        {
		            Menu menu = KickStarter.playerMenus.customMenus[i];
		            bool update = false;
		            for (int j=0; j<menu.elements.Count; j++)
		            {
		                if (menu.elements[j] is MenuInventoryBox)
		                {
		                    update = true;
		                }
		            }
		            if (update)
		            {
						menu.Recalculate ();
		            }
		        }
			}
		}
		

		/**
		 * Takes the ingredients supplied to a MenuCrafting element and sets the appropriate outcome of another MenuCrafting element with craftingType = CraftingElementType.Output.
		 */
		public static void CreateRecipe ()
		{
			Menu[] allMenus = GetMenus (true).ToArray ();
			if (allMenus != null)
			{
				foreach (AC.Menu menu in allMenus)
				{
					foreach (MenuElement menuElement in menu.elements)
					{
						if (menuElement is MenuCrafting)
						{
							MenuCrafting crafting = (MenuCrafting) menuElement;
							crafting.SetOutput (menu.menuSource);
						}
					}
				}
			}
		}
		

		/**
		 * <summary>Instantly turns off all Menus.</summary>
		 * <param name = "onlyPausing">If True, then only Menus with pauseWhenEnabled = True will be turned off</param>
		 */
		public static void ForceOffAllMenus (bool onlyPausing = false)
		{
			Menu[] allMenus = GetMenus (true).ToArray ();
			if (allMenus != null)
			{
				foreach (AC.Menu menu in allMenus)
				{
					if (menu.IsEnabled ())
					{
						if (!onlyPausing || (onlyPausing && menu.IsBlocking ()))
						{
							menu.ForceOff (true);
						}
					}
				}
			}
		}


		/**
		 * <summary>Simulates the clicking of a MenuElement.</summary>
		 * <param name = "menuName">The name (title) of the Menu that contains the MenuElement</param>
		 * <param name = "menuElementName">The name (title) of the MenuElement</param>
		 * <param name = "slot">The index number of the slot, if the MenuElement has multiple slots</param>
		 */
		public static void SimulateClick (string menuName, string menuElementName, int slot = 1)
		{
			if (KickStarter.playerMenus)
			{
				AC.Menu menu = PlayerMenus.GetMenuWithName (menuName);
				MenuElement element = PlayerMenus.GetElementWithName (menuName, menuElementName);
				KickStarter.playerMenus.CheckClick (menu, element, slot, MouseState.SingleClick);
			}
		}
		

		/**
		 * <summary>Simulates the clicking of a MenuElement.</summary>
		 * <param name = "menuName">The name (title) of the Menu that contains the MenuElement</param>
		 * <param name = "_element">The MenuElement</param>
		 * <param name = "slot">The index number of the slot, if the MenuElement has multiple slots</param>
		 */
		public static void SimulateClick (string menuName, MenuElement _element, int _slot = 1)
		{
			if (KickStarter.playerMenus)
			{
				AC.Menu menu = PlayerMenus.GetMenuWithName (menuName);
				KickStarter.playerMenus.CheckClick (menu, _element, _slot, MouseState.SingleClick);
			}
		}
		

		/**
		 * <summary>Checks if any Menus that pause the game are currently turned on.</summary>
		 * <param name ="excludingMenu">If assigned, this Menu will be excluded from the check</param>
		 * <returns>True if any Menus that pause the game are currently turned on</returns>
		 */
		public bool ArePauseMenusOn (Menu excludingMenu = null)
		{
			List<Menu> allMenus = GetMenus (true);
			for (int i=0; i< allMenus.Count; i++)
			{
				if (allMenus[i].IsEnabled () && allMenus[i].IsBlocking () && (excludingMenu == null || allMenus[i] != excludingMenu))
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * <summary>Instantly turns off all Menus that have appearType = AppearType.WhenSpeechPlays.</summary>
		 * <param name = "speechMenuLimit">The type of speech to kill (All, BlockingOnly, BackgroundOnly)</param>
		 */
		public void ForceOffSubtitles (SpeechMenuLimit speechMenuLimit = SpeechMenuLimit.All)
		{
			foreach (AC.Menu menu in menus)
			{
				ForceOffSubtitles (menu, speechMenuLimit);
			}

			foreach (AC.Menu menu in dupSpeechMenus)
			{
				ForceOffSubtitles (menu, speechMenuLimit);
			}

			foreach (AC.Menu menu in customMenus)
			{
				ForceOffSubtitles (menu, speechMenuLimit);
			}
		}


		protected void ForceOffSubtitles (Menu menu, SpeechMenuLimit speechMenuLimit)
		{
			if (menu.IsEnabled () && menu.appearType == AppearType.WhenSpeechPlays)
			{
				if (speechMenuLimit == SpeechMenuLimit.All ||
				    menu.speech == null ||
				   (speechMenuLimit == SpeechMenuLimit.BlockingOnly && menu.speech != null && !menu.speech.isBackground) ||
				   (speechMenuLimit == SpeechMenuLimit.BlockingOnly && menu.speech != null && menu.speech.isBackground))
				{
					menu.ForceOff (true);
				}
			}
		}


		/**
		 * <summary>Gets the Menu associated with a given Unity UI Canvas</summar>
		 * <param name = "canvas">The Canvas object linked to the Menu to be found</param>
		 * <returns>The Menu associated with a given Unity UI Canvas</returns>
		 */
		public Menu GetMenuWithCanvas (Canvas canvas)
		{
			if (canvas == null) return null;

			foreach (Menu dupSpeechMenu in dupSpeechMenus)
			{
				if (dupSpeechMenu.RuntimeCanvas == canvas)
				{
					return dupSpeechMenu;
				}
			}

			foreach (Menu menu in menus)
			{
				if (menu.RuntimeCanvas == canvas)
				{
					return menu;
				}
			}

			foreach (Menu customMenu in customMenus)
			{
				if (customMenu.RuntimeCanvas == canvas)
				{
					return customMenu;
				}
			}

			return null;
		}


		/**
		 * <summary>Registers a script-created Menu instance, so that it's click-handling, updating and rendering are handled automatically.</summary>
		 * <param name = "menu">The custom Menu to register</param>
		 * <param name = "deleteWhenTurnOff">When True, the associated UI canvas will be deleted (if present), and the menu will be unregistered, automatically when the Menu is turned off</param>
		 */
		public void RegisterCustomMenu (Menu menu, bool deleteWhenTurnOff = false)
		{
			if (customMenus == null)
			{
				customMenus = new List<Menu>();
			}

			if (customMenus.Contains (menu))
			{
				ACDebug.LogWarning ("Already registed custom menu '" + menu.title + "'");
			}
			else
			{
				customMenus.Add (menu);
				menu.deleteUIWhenTurnOff = deleteWhenTurnOff;
				menu.ID = -1 * ((customMenus.IndexOf (menu) * 100) + menu.id);
				ACDebug.Log ("Registered custom menu '" + menu.title + "'");
			}
		}


		/**
		 * <summary>Unregisters a script-created Menu instance, so that it is no longer updated automatically.</summary>
		 * <param name = "menu">The custom Menu to unregister</param>
		 * <param name = "showError">If True, then the Console will display a Warning message if the Menu is not registered</param>
		 */
		public void UnregisterCustomMenu (Menu menu, bool showError = true)
		{
			if (customMenus != null && customMenus.Contains (menu))
			{
				customMenus.Remove (menu);
				ACDebug.Log ("Unregistered custom menu '" + menu.title + "'");
			}
			else
			{
				ACDebug.LogWarning ("Custom menu '" + menu.title + "' is not registered.");
			}
		}


		/**
		 * <summary>Gets all Menu instances currently registered as custom Menus that are automatically updated by PlayerMenus</summary>
		 * <returns>An array of all Menu instances currently registered as custom Menus that are automatically updated by PlayerMenus</summary>
		 */
		public Menu[] GetRegisteredCustomMenus ()
		{
			return customMenus.ToArray ();
		}


		/**
		 * <summary>Destroys and unregisters all custom Menus registered with PlayerMenus</summary>
		 */
		public void DestroyCustomMenus ()
		{
			for (int i=0; i<customMenus.Count; i++)
			{
				if (customMenus[i].RuntimeCanvas != null && customMenus[i].RuntimeCanvas.gameObject != null)
				{
					Destroy (customMenus[i].RuntimeCanvas.gameObject);
				}
				customMenus.RemoveAt (i);
				i = -1;
			}
		}
		

		/**
		 * Recalculates the position, size and display of all Menus.
		 * This is an intensive process, and should not be called every fame.
		 */
		public void RecalculateAll ()
		{
			doResizeMenus = 1;
			
			// Border camera
			if (KickStarter.mainCamera)
			{
				KickStarter.mainCamera.RecalculateRects ();
			}
		}


		/**
		 * <summary>Selects the first element GameObject in a Unity UI-based Menu.</summary>
		 * <param name = "menuToIgnore">If set, this menu will be ignored when searching</param>
		 */
		public void FindFirstSelectedElement (Menu menuToIgnore = null)
		{
			if (eventSystem == null || menus.Count == 0)
			{
				return;
			}

			GameObject objectToSelect = null;
			for (int i=menus.Count-1; i>=0; i--)
			{
				Menu menu = menus[i];

				if (menuToIgnore != null && menu == menuToIgnore)
				{
					continue;
				}

				if (menu.IsEnabled ())
				{
					objectToSelect = menu.GetObjectToSelect ();
					if (objectToSelect != null)
					{
						break;
					}
				}
			}

			if (objectToSelect != null)
			{
				SelectUIElement (objectToSelect);
			}
		}


		/**
		 * <summary>Selects a Unity UI GameObject</summary>
		 * <param name = "objectToSelect">The UI GameObject to select</param>
		 */
		public void SelectUIElement (GameObject objectToSelect)
		{
			StartCoroutine (SelectUIElementCoroutine (objectToSelect));
		}


		protected IEnumerator SelectUIElementCoroutine (GameObject objectToSelect)
		{
			eventSystem.SetSelectedGameObject (null);
			yield return null;
			eventSystem.SetSelectedGameObject (objectToSelect);
		}


		/**
		 * <summary>Gets the ID number of the CursorIcon, defined in CursorManager, to switch to based on what MenuElement the cursor is currently over</summary>
		 * <returns>The ID number of the CursorIcon, defined in CursorManager, to switch to based on what MenuElement the cursor is currently over</returns>
		 */
		public int GetElementOverCursorID ()
		{
			return elementOverCursorID;
		}


		/**
		 * <summary>Sets the state of the manual save lock.</summary>
		 * <param name = "state">If True, then saving will be manually disabled</param>
		 */
		public void SetManualSaveLock (bool state)
		{
			lockSave = state;
		}


		/**
		 * <summary>Checks if the cursor is hovering over a Menu.</summary>
		 * <returns>True if the cursor is hovering over a Menu</returns>
		 */
		public bool IsMouseOverMenu ()
		{
			return isMouseOverMenu;
		}


		/**
		 * <summary>Checks if the cursor is hovering over an Inventory</summary>
		 */
		public bool IsMouseOverInventory ()
		{
			return isMouseOverInventory;
		}


		/**
		 * <summary>Checks if the cursor is hovering over a Menu with appearType = AppearType.OnInteraction.</summary>
		 * <returns>True if the cursor is hovering over a Menu with appearType = AppearType.OnInteraction.</returns>
		 */
		public bool IsMouseOverInteractionMenu ()
		{
			return isMouseOverInteractionMenu;
		}


		/**
		 * <summary>Checks if any Menu with appearType = AppearType.OnInteraction is on.</summary>
		 * <returns>True if any Menu with appearType = AppearType.OnInteraction is on.</returns>
		 */
		public bool IsInteractionMenuOn ()
		{
			return interactionMenuIsOn;
		}


		/**
		 * <summary>Checks if the player is currently manipulating an Interaction Menu by cycling the Interaction elements inside it.</summary>
		 * <returns>True if the player is currently manipulating an Interaction Menu by cycling the Interaction elements inside it.</returns>
		 */
		public bool IsCyclingInteractionMenu ()
		{
			if (interactionMenuIsOn && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the last-opened Menu with appearType = AppearType.OnInteraction is both open and set to pause the game.</summary>
		 * <returns>True if the last-opened Menu with appearType = AppearType.OnInteraction is both open and set to pause the game.</returns>
		 */
		public bool IsPausingInteractionMenuOn ()
		{
			if (interactionMenuIsOn)
			{
				return interactionMenuPauses;
			}
			return false;
		}


		/**
		 * Makes all Menus linked to Unity UI interactive.
		 */
		public void MakeUIInteractive ()
		{
			Menu[] allMenus = GetMenus (true).ToArray ();
			foreach (Menu menu in allMenus)
			{
				menu.MakeUIInteractive ();
			}
		}
		
		
		/**
		 * Makes all Menus linked to Unity UI non-interactive.
		 */
		public void MakeUINonInteractive ()
		{
			Menu[] allMenus = GetMenus (true).ToArray ();
			foreach (Menu menu in allMenus)
			{
				menu.MakeUINonInteractive ();
			}
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			mainData.menuLockData = CreateMenuLockData ();
			mainData.menuVisibilityData = CreateMenuVisibilityData ();
			mainData.menuElementVisibilityData = CreateMenuElementVisibilityData ();
			mainData.menuJournalData = CreateMenuJournalData ();

			return mainData;
		}
		
		
		/**
		 * <summary>Updates its own variables from a MainData class.</summary>
		 * <param name = "mainData">The MainData class to load from</param>
		 */
		public void LoadMainData (MainData mainData)
		{
			foreach (Menu menu in menus)
			{
				foreach (MenuElement element in menu.elements)
				{
					if (element is MenuInventoryBox)
					{
						MenuInventoryBox invBox = (MenuInventoryBox) element;
						invBox.ResetOffset ();
					}
				}
			}
			
			AssignMenuLocks (mainData.menuLockData);
			AssignMenuVisibility (mainData.menuVisibilityData);
			AssignMenuElementVisibility ( mainData.menuElementVisibilityData);
			AssignMenuJournals (mainData.menuJournalData);
		}


		protected string CreateMenuLockData ()
		{
			System.Text.StringBuilder menuString = new System.Text.StringBuilder ();
			
			foreach (AC.Menu _menu in menus)
			{
				menuString.Append (_menu.IDString);
				menuString.Append (SaveSystem.colon);
				menuString.Append (_menu.isLocked.ToString ());
				menuString.Append (SaveSystem.pipe);
			}
			
			if (menus.Count > 0)
			{
				menuString.Remove (menuString.Length-1, 1);
			}
			
			return menuString.ToString ();
		}
		
		
		protected string CreateMenuVisibilityData ()
		{
			System.Text.StringBuilder menuString = new System.Text.StringBuilder ();
			bool changeMade = false;
			foreach (AC.Menu _menu in menus)
			{
				if (_menu.IsManualControlled ())
				{
					changeMade = true;
					menuString.Append (_menu.IDString);
					menuString.Append (SaveSystem.colon);
					menuString.Append (_menu.IsEnabled ().ToString ());
					menuString.Append (SaveSystem.pipe);
				}
			}
			
			if (changeMade)
			{
				menuString.Remove (menuString.Length-1, 1);
			}
			return menuString.ToString ();
		}
		
		
		protected string CreateMenuElementVisibilityData ()
		{
			System.Text.StringBuilder visibilityString = new System.Text.StringBuilder ();
			
			foreach (AC.Menu _menu in menus)
			{
				if (_menu.NumElements > 0)
				{
					visibilityString.Append (_menu.IDString);
					visibilityString.Append (SaveSystem.colon);
					
					foreach (MenuElement _element in _menu.elements)
					{
						visibilityString.Append (_element.IDString);
						visibilityString.Append ("=");
						visibilityString.Append (_element.IsVisible.ToString ());
						visibilityString.Append ("+");
					}
					
					visibilityString.Remove (visibilityString.Length-1, 1);
					visibilityString.Append (SaveSystem.pipe);
				}
			}
			
			if (menus.Count > 0)
			{
				visibilityString.Remove (visibilityString.Length-1, 1);
			}
			
			return visibilityString.ToString ();
		}
		
		
		protected string CreateMenuJournalData ()
		{
			System.Text.StringBuilder journalString = new System.Text.StringBuilder ();
			
			foreach (AC.Menu _menu in menus)
			{
				foreach (MenuElement _element in _menu.elements)
				{
					if (_element is MenuJournal)
					{
						MenuJournal journal = (MenuJournal) _element;
						journalString.Append (_menu.IDString);
						journalString.Append (SaveSystem.colon);
						journalString.Append (journal.ID);
						journalString.Append (SaveSystem.colon);
						
						foreach (JournalPage page in journal.pages)
						{
							journalString.Append (page.lineID);
							//journalString.Append ("*");
							//journalString.Append (page.text);
							journalString.Append ("~");
						}
						
						if (journal.pages.Count > 0)
						{
							journalString.Remove (journalString.Length-1, 1);
						}

						journalString.Append (SaveSystem.colon);
						journalString.Append (journal.showPage);
						
						journalString.Append (SaveSystem.pipe);
					}
				}
			}
			
			if (!string.IsNullOrEmpty (journalString.ToString ()))
			{
				journalString.Remove (journalString.Length-1, 1);
			}
			
			return journalString.ToString ();
		}


		protected void AssignMenuLocks (string menuLockData)
		{
			if (!string.IsNullOrEmpty (menuLockData))
			{
				string[] lockArray = menuLockData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in lockArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _id = 0;
					int.TryParse (chunkData[0], out _id);
					
					bool _lock = false;
					bool.TryParse (chunkData[1], out _lock);
					
					foreach (AC.Menu _menu in menus)
					{
						if (_menu.id == _id)
						{
							_menu.isLocked = _lock;
							break;
						}
					}
				}
			}
		}
		
		
		protected void AssignMenuVisibility (string menuVisibilityData)
		{
			if (!string.IsNullOrEmpty (menuVisibilityData))
			{
				string[] visArray = menuVisibilityData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in visArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _id = 0;
					int.TryParse (chunkData[0], out _id);
					
					bool _lock = false;
					bool.TryParse (chunkData[1], out _lock);
					
					foreach (AC.Menu _menu in menus)
					{
						if (_menu.id == _id)
						{
							if (_menu.IsManualControlled ())
							{
								if (_menu.ShouldTurnOffWhenLoading ())
								{
									if (_menu.IsOn () && _menu.actionListOnTurnOff)
									{
										ACDebug.LogWarning ("The '" +_menu.title + "' menu's 'ActionList On Turn Off' (" + _menu.actionListOnTurnOff.name + ") was not run because the menu was turned off as a result of loading.  The SavesList element's 'ActionList after loading' can be used to run the same Actions instead.");
									}
									_menu.ForceOff (true);
								}
								else
								{
									if (_lock)
									{
										_menu.TurnOn (false);
									}
									else
									{
										_menu.TurnOff (false);
									}
								}
							}
							break;
						}
					}
				}
			}
		}
		
		
		protected void AssignMenuElementVisibility (string menuElementVisibilityData)
		{
			if (!string.IsNullOrEmpty (menuElementVisibilityData))
			{
				string[] visArray = menuElementVisibilityData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in visArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _menuID = 0;
					int.TryParse (chunkData[0], out _menuID);
					
					foreach (AC.Menu _menu in menus)
					{
						if (_menu.id == _menuID)
						{
							// Found a match
							string[] perMenuData = chunkData[1].Split ("+"[0]);
							
							foreach (string perElementData in perMenuData)
							{
								string [] chunkData2 = perElementData.Split ("="[0]);
								
								int _elementID = 0;
								int.TryParse (chunkData2[0], out _elementID);
								
								bool _elementVisibility = false;
								bool.TryParse (chunkData2[1], out _elementVisibility);
								
								foreach (MenuElement _element in _menu.elements)
								{
									if (_element.ID == _elementID && _element.IsVisible != _elementVisibility)
									{
										_element.IsVisible = _elementVisibility;
										break;
									}
								}
							}
							
							_menu.ResetVisibleElements ();
							_menu.Recalculate ();
							break;
						}
					}
				}
			}
		}


		protected bool AreInteractionMenusOn ()
		{
			for (int i=0; i<menus.Count; i++)
			{
				if (menus[i].appearType == AppearType.OnInteraction && menus[i].IsEnabled () && !menus[i].IsFadingOut ())
				{
					return true;
				}
			}
			return false;
		}

		
		protected void AssignMenuJournals (string menuJournalData)
		{
			if (!string.IsNullOrEmpty (menuJournalData))
			{
				string[] journalArray = menuJournalData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in journalArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int menuID = 0;
					int.TryParse (chunkData[0], out menuID);
					
					int elementID = 0;
					int.TryParse (chunkData[1], out elementID);
					
					foreach (AC.Menu _menu in menus)
					{
						if (_menu.id == menuID)
						{
							foreach (MenuElement _element in _menu.elements)
							{
								if (_element.ID == elementID && _element is MenuJournal)
								{
									MenuJournal journal = (MenuJournal) _element;
									bool clearedJournal = false;
									string[] pageArray = chunkData[2].Split ("~"[0]);
									
									foreach (string chunkData2 in pageArray)
									{
										int lineID = -1;
										string[] chunkData3 = chunkData2.Split ("*"[0]);
										int.TryParse (chunkData3[0], out lineID);

										if (chunkData3.Length > 1)
										{
											// Backwards-compatibility for old save files

											if (!clearedJournal)
											{
												journal.pages = new List<JournalPage>();
												journal.showPage = 1;
												clearedJournal = true;
											}
											journal.pages.Add (new JournalPage (lineID, chunkData3[1]));
										}
										else if (lineID >= 0)
										{
											if (!clearedJournal)
											{
												journal.pages = new List<JournalPage>();
												journal.showPage = 1;
												clearedJournal = true;
											}

											SpeechLine speechLine = KickStarter.speechManager.GetLine (lineID);
											if (speechLine != null && speechLine.textType == AC_TextType.JournalEntry)
											{
												journal.pages.Add (new JournalPage (lineID, speechLine.text));
											}
										}
									}

									if (clearedJournal)
									{
										if (chunkData.Length > 3)
										{
											int showPage = 1;
											int.TryParse (chunkData[3], out showPage);

											if (showPage > journal.pages.Count)
											{
												showPage = journal.pages.Count;
											}
											else if (showPage < 1)
											{
												showPage = 1;
											}
											journal.showPage = showPage;
										}
									}

									break;
								}
							}
						}
					}
				}
			}
		}


		protected void OnApplicationQuit ()
		{
			if (KickStarter.playerMenus != null)
			{
				foreach (Menu menu in KickStarter.playerMenus.menus)
				{
					if (menu != null)
					{
						foreach (MenuElement menuElement in menu.elements)
						{
							if (menuElement != null && menuElement is MenuGraphic)
							{
								MenuGraphic menuGraphic = (MenuGraphic) menuElement;
								if (menuGraphic.graphic != null)
								{
									menuGraphic.graphic.ClearCache ();
								}
							}
						}
					}
				}
			}
		}


		/**
		 * Backs up the state of the menu and cursor systems, and disables them, before taking a screenshot.
		 */
		public virtual void PreScreenshotBackup ()
		{
			foreach (Menu menu in menus)
			{
				menu.PreScreenshotBackup ();
			}

			foreach (Menu dupSpeechMenu in dupSpeechMenus)
			{
				dupSpeechMenu.PreScreenshotBackup ();
			}

			foreach (Menu customMenu in customMenus)
			{
				customMenu.PreScreenshotBackup ();
			}
		}


		/**
		 * Restores the menu and cursor systems to their former states, after taking a screenshot.
		 */
		public virtual void PostScreenshotBackup ()
		{
			foreach (Menu menu in menus)
			{
				menu.PostScreenshotBackup ();
			}

			foreach (Menu dupSpeechMenu in dupSpeechMenus)
			{
				dupSpeechMenu.PostScreenshotBackup ();
			}

			foreach (Menu customMenu in customMenus)
			{
				customMenu.PostScreenshotBackup ();
			}
		}

	}

}