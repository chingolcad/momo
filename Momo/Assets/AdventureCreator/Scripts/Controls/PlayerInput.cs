/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"PlayerInput.cs"
 * 
 *	This script records all input and processes it for other scripts.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script recieves and processes all input, for use by other scripts.
	 * It should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_input.html")]
	public class PlayerInput : MonoBehaviour
	{

		protected AnimationCurve timeCurve;
		protected float changeTimeStart;

		protected MouseState mouseState = MouseState.Normal;
		protected DragState dragState = DragState.None;

		protected Vector2 moveKeys = new Vector2 (0f, 0f);
		protected bool playerIsControlledRunning = false;
		/** The game's current Time.timeScale value */
		[HideInInspector] public float timeScale = 1f;
		
		protected bool isUpLocked = false;
		protected bool isDownLocked = false;
		protected bool isLeftLocked = false;
		protected bool isRightLocked = false;
		protected bool freeAimLock = false;
		protected bool isJumpLocked = false;

		/** If True, Menus can be controlled via the keyboard or controller during gameplay (if SettingsManager.inputMethod = InputMethod.KeyboardOrController */
		[HideInInspector] public bool canKeyboardControlMenusDuringGameplay = false;
		/** If True, then the Player prefab cannot run */
		[HideInInspector] public PlayerMoveLock runLock = PlayerMoveLock.Free;
		/** The name of the Input button that skips movies played with ActionMove */
		[HideInInspector] public string skipMovieKey = "";
		/** The minimum duration, in seconds, that can elapse between mouse clicks */
		public float clickDelay = 0.3f;
		/** The maximum duration, in seconds, between two successive mouse clicks to register a "double-click" */
		public float doubleClickDelay = 1f;
		/** The name of the Input Axis that controls dragging effects. If empty, the default inputs (LMB / "InteractionA") will be used */
		public string dragOverrideInput = "";

		protected float clickTime = 0f;
		protected float doubleClickTime = 0;
		protected MenuDrag activeDragElement;
		protected bool hasUnclickedSinceClick = false;
		protected bool lastClickWasDouble = false;
		protected float lastclickTime = 0f;
		
		// Menu input override
		protected string menuButtonInput;
		protected float menuButtonValue;
		protected SimulateInputType menuInput;
		
		// Controller movement
		/** The movement speed of a keyboard or controller-controlled cursor */
		public float cursorMoveSpeed = 4f;
		/** If True, and Direct movement is used to control the Player, then the Player will not change direction. This is to avoid the Player moving in unwanted directions when the camera cuts. */
		[HideInInspector] public bool cameraLockSnap = false;
		protected Vector2 xboxCursor;
		protected Vector2 mousePosition;
		protected bool scrollingLocked = false;
		protected bool canCycleInteractionInput = true;

		// Touch-Screen movement
		protected  Vector2 dragStartPosition = Vector2.zero;
		protected Vector2 dragEndPosition = Vector2.zero;
		protected float dragSpeed = 0f;
		protected Vector2 dragVector;
		protected float touchTime = 0f;
		protected float touchThreshold = 0.2f;
		
		// 1st person movement
		protected Vector2 freeAim;
		protected bool toggleCursorOn = false;
		protected bool cursorIsLocked = false;
		public ForceGameplayCursor forceGameplayCursor = ForceGameplayCursor.None;

		// Draggable
		protected bool canDragMoveable = false;
		protected DragBase dragObject = null;
		protected Vector2 lastMousePosition;
		protected bool resetMouseDelta = false;
		protected Vector3 lastCameraPosition;
		protected Vector3 dragForce;
		protected Vector2 deltaDragMouse;

		/** The active Conversation */
		[HideInInspector] public Conversation activeConversation = null;
		protected Conversation pendingOptionConversation = null;
		/** The active ArrowPrompt */
		[HideInInspector] public ArrowPrompt activeArrows = null;
		/** The active Container */
		[HideInInspector] public Container activeContainer = null;
		protected bool mouseIsOnScreen = true;

		// Delegates
		/** A delegate template for overriding input button detection */
		public delegate bool InputButtonDelegate (string buttonName);
		/** A delegate template for overriding input axis detection */
		public delegate float InputAxisDelegate (string axisName);
		/** A delegate template for overriding mouse position detection */
		public delegate Vector2 InputMouseDelegate (bool cusorIsLocked = false);
		/** A delegate template for overriding mouse button detection */
		public delegate bool InputMouseButtonDelegate (int button);
		/** A delegate template for overriding touch position detection */
		public delegate Vector2 InputTouchDelegate (int index);
		/** A delegate template for overriding touch phase */
		public delegate TouchPhase InputTouchPhaseDelegate (int index);
		/** A delegate template for overriding touch count */
		public delegate int _InputTouchCountDelegate ();

		/** A delegate for the InputGetButtonDown function, used to detect when a button is first pressed */
		public InputButtonDelegate InputGetButtonDownDelegate = null;
		/** A delegate for the InputGetButtonUp function, used to detect when a button is released */
		public InputButtonDelegate InputGetButtonUpDelegate = null;
		/** A delegate for the InputGetButton function, used to detect when a button is held down */
		public InputButtonDelegate InputGetButtonDelegate = null;
		/** A delegate for the InputGetAxis function, used to detect the value of an input axis */
		public InputAxisDelegate InputGetAxisDelegate = null;
		/** A delegate for the InputGetMouseButton function, used to detect mouse clicks */
		public InputMouseButtonDelegate InputGetMouseButtonDelegate;
		/** A delegate for the InputGetMouseDownButton function, used to detect when a mouse button is first clicked */
		public InputMouseButtonDelegate InputGetMouseButtonDownDelegate;
		/** A delegate for the InputMousePosition function, used to detect the mouse position */
		public InputMouseDelegate InputMousePositionDelegate;
		/** A delegate for the InputTouchPosition function, used to detect the touch position */
		public InputTouchDelegate InputTouchPositionDelegate;
		/** A delegate for the InputTouchPosition function, used to detect the touch deltaPosition */
		public InputTouchDelegate InputTouchDeltaPositionDelegate;
		/** A delegate for the InputTouchPhase function, used to detect a touch index's phase */
		public InputTouchPhaseDelegate InputGetTouchPhaseDelegate;
		/** A delegate for the InputGetFreeAim function, used to get the free-aiming vector */
		public InputMouseDelegate InputGetFreeAimDelegate;
		/** A delegate for the _InputTouchCountDelegate function, used to get the number of touches */
		public _InputTouchCountDelegate InputTouchCountDelegate;


		public void OnAwake ()
		{
			if (KickStarter.settingsManager)
			{
				InitialiseCursorLock (KickStarter.settingsManager.movementMethod);
			}
		
			ResetClick ();

			xboxCursor = LockedCursorPosition;

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.CanDragCursor ())
			{
				mousePosition = xboxCursor;
			}
		}


		/**
		 * Updates the input handler.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateInput ()
		{
			if (timeCurve != null && timeCurve.length > 0)
			{
				float timeIndex = Time.time - changeTimeStart;
				if (timeCurve [timeCurve.length -1].time < timeIndex)
				{
					SetTimeScale (timeCurve [timeCurve.length -1].time);
					timeCurve = null;
				}
				else
				{
					SetTimeScale (timeCurve.Evaluate (timeIndex));
				}
			}

			if (clickTime > 0f)
			{
				clickTime -= 4f * GetDeltaTime ();
			}
			if (clickTime < 0f)
			{
				clickTime = 0f;
			}

			if (doubleClickTime > 0f)
			{
				doubleClickTime -= 4f * GetDeltaTime ();
			}
			if (doubleClickTime < 0f)
			{
				doubleClickTime = 0f;
			}

			bool isSkippingMovie = false;
			if (!string.IsNullOrEmpty (skipMovieKey) && InputGetButtonDown (skipMovieKey) && KickStarter.stateHandler.gameState != GameState.Paused)
			{
				skipMovieKey = string.Empty;
				isSkippingMovie = true;
			}
			
			if (KickStarter.stateHandler && KickStarter.settingsManager)
			{
				lastMousePosition = mousePosition;

				if (InputGetButtonDown ("ToggleCursor") && KickStarter.stateHandler.IsInGameplay ())
				{
					ToggleCursor ();
				}

				if (KickStarter.stateHandler.gameState == GameState.Cutscene && InputGetButtonDown ("EndCutscene") && !isSkippingMovie)
				{
					KickStarter.actionListManager.EndCutscene ();
				}

				#if UNITY_EDITOR
				if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard || KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
				#else
				if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard)
				#endif
				{
					// Cursor lock state
					if (KickStarter.stateHandler.gameState == GameState.Paused ||
						(KickStarter.stateHandler.gameState == GameState.DialogOptions && !KickStarter.settingsManager.allowGameplayDuringConversations) ||
						(freeAimLock && KickStarter.settingsManager.IsInFirstPerson ()))
					{
						cursorIsLocked = false;
					}
					else if (dragObject != null && 
							 KickStarter.settingsManager.IsInFirstPerson () && 
							 KickStarter.settingsManager.disableFreeAimWhenDragging)
					{
						cursorIsLocked = false;
					}
					else
					{
						if (forceGameplayCursor == ForceGameplayCursor.KeepLocked)
						{
							cursorIsLocked = true;
						}
						else if (forceGameplayCursor == ForceGameplayCursor.KeepUnlocked)
						{
							cursorIsLocked = false;
						}
						else
						{
							cursorIsLocked = toggleCursorOn;
						}
					}

					UnityVersionHandler.CursorLock = cursorIsLocked;

					// Cursor position
					if (cursorIsLocked)
					{
						mousePosition = InputMousePosition (true);
					}
					else
					{
						mousePosition = InputMousePosition (false);
					}
					freeAim = GetSmoothFreeAim (InputGetFreeAim (cursorIsLocked));

					// Cursor state
					if (mouseState == MouseState.Normal)
					{
						dragState = DragState.None;
					}

					if (InputGetMouseButtonDown (0) || InputGetButtonDown ("InteractionA"))
					{
						if (KickStarter.settingsManager.touchUpWhenPaused && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.stateHandler.gameState == GameState.Paused)
						{
							ResetMouseClick ();
						}
						else if (mouseState == MouseState.Normal)
						{
							if (CanDoubleClick ())
							{
								mouseState = MouseState.DoubleClick;
								ResetClick ();
							}
							else if (CanClick ())
							{
								dragStartPosition = GetInvertedMouse ();
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
						}
					}
					else if (InputGetButtonDown (dragOverrideInput))
					{
						if (KickStarter.stateHandler.IsInGameplay () && mouseState == MouseState.Normal && !CanDoubleClick () && CanClick ())
						{
							dragStartPosition = GetInvertedMouse ();
						}
					}
					else if (InputGetMouseButtonDown (1) || InputGetButtonDown ("InteractionB"))
					{
						mouseState = MouseState.RightClick;
					}
					else if (!string.IsNullOrEmpty (dragOverrideInput) && InputGetButton (dragOverrideInput))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else if (string.IsNullOrEmpty (dragOverrideInput) && (InputGetMouseButton (0) || InputGetButton ("InteractionA")))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else
					{
						if (mouseState == MouseState.HeldDown && dragState == DragState.None && CanClick ())
						{
							if (KickStarter.settingsManager.touchUpWhenPaused && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.stateHandler.gameState == GameState.Paused)
							{
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
							else
							{
								mouseState = MouseState.LetGo;
							}
						}
						else
						{
							ResetMouseClick ();
						}
					}

					SetDoubleClickState ();
					
					if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
					{
						if (InputGetFreeAimDelegate != null)
						{
							freeAim = GetSmoothFreeAim (InputGetFreeAim (dragState == DragState.Player));
						}
						else
						{
							if (dragState == DragState.Player)
							{
								if (KickStarter.settingsManager.IsFirstPersonDragMovement ())
								{
									freeAim = GetSmoothFreeAim (new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, 0f));
								}
								else
								{
									freeAim = GetSmoothFreeAim (new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, -dragVector.y * KickStarter.settingsManager.freeAimTouchSpeed));
								}
							}
							else
							{
								freeAim = GetSmoothFreeAim (Vector2.zero);
							}
						}
					}
				}
				else if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
				{
					int touchCount = InputTouchCount ();

					// Cursor lock state
					if (forceGameplayCursor == ForceGameplayCursor.KeepLocked)
					{
						cursorIsLocked = true;
					}
					else if (forceGameplayCursor == ForceGameplayCursor.KeepUnlocked)
					{
						cursorIsLocked = false;
					}
					else
					{
						cursorIsLocked = toggleCursorOn;
					}

					// Cursor position
					if (cursorIsLocked)
					{
						mousePosition = LockedCursorPosition;
					}
					else if (touchCount > 0)
					{
						if (KickStarter.settingsManager.CanDragCursor ())
						{
							if (touchTime > touchThreshold)
							{
								if (InputTouchPhase (0) == TouchPhase.Moved && touchCount == 1)
								{
									mousePosition += InputTouchDeltaPosition (0);

									if (mousePosition.x < 0f)
									{
										mousePosition.x = 0f;
									}
									else if (mousePosition.x > ACScreen.width)
									{
										mousePosition.x = ACScreen.width;
									}
									if (mousePosition.y < 0f)
									{
										mousePosition.y = 0f;
									}
									else if (mousePosition.y > ACScreen.height)
									{
										mousePosition.y = ACScreen.height;
									}
								}
							}
						}
						else
						{
							mousePosition = InputTouchPosition (0);
						}
					}

					// Cursor state
					if (mouseState == MouseState.Normal)
					{
						dragState = DragState.None;
					}
					
					if (touchTime > 0f && touchTime < touchThreshold)
					{
						dragStartPosition = GetInvertedMouse ();
					}

					if ((touchCount == 1 && KickStarter.stateHandler.gameState == GameState.Cutscene && InputTouchPhase (0) == TouchPhase.Began)
						|| (touchCount == 1 && !KickStarter.settingsManager.CanDragCursor () && InputTouchPhase (0) == TouchPhase.Began)
						|| Mathf.Approximately (touchTime, -1f))
					{
						if (KickStarter.settingsManager.touchUpWhenPaused && KickStarter.stateHandler.gameState == GameState.Paused)
						{
							ResetMouseClick ();
						}
						else if (mouseState == MouseState.Normal)
						{
							dragStartPosition = GetInvertedMouse (); //

							if (CanDoubleClick ())
							{
								mouseState = MouseState.DoubleClick;
								ResetClick ();
							}
							else if (CanClick ())
							{
								dragStartPosition = GetInvertedMouse ();
								
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
						}
					}
					else if (touchCount == 2 && InputTouchPhase (1) == TouchPhase.Began)
					{
						mouseState = MouseState.RightClick;

						if (KickStarter.settingsManager.IsFirstPersonDragComplex ())
						{
							dragStartPosition = GetInvertedMouse ();
						}
					}
					else if (touchCount == 1 && (InputTouchPhase (0) == TouchPhase.Stationary || InputTouchPhase (0) == TouchPhase.Moved))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else if (touchCount == 2 && (InputTouchPhase (0) == TouchPhase.Stationary || InputTouchPhase (0) == TouchPhase.Moved) && KickStarter.settingsManager.IsFirstPersonDragComplex ())
					{
						mouseState = MouseState.HeldDown;
						SetDragStateTouchScreen ();
					}
					else
					{
						if (mouseState == MouseState.HeldDown && dragState == DragState.None && CanClick ())
						{
							if (KickStarter.settingsManager.touchUpWhenPaused && KickStarter.stateHandler.gameState == GameState.Paused)
							{
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
							else
							{
								mouseState = MouseState.LetGo;
							}
						}
						else
						{
							ResetMouseClick ();
						}
					}

					SetDoubleClickState ();
					
					if (KickStarter.settingsManager.CanDragCursor ())
					{
						if (touchCount > 0)
						{
							touchTime += GetDeltaTime ();
						}
						else
						{
							if (touchTime > 0f && touchTime < touchThreshold)
							{
								touchTime = -1f;
							}
							else
							{
								touchTime = 0f;
							}
						}
					}

					if (InputGetFreeAimDelegate != null)
					{
						freeAim = GetSmoothFreeAim (InputGetFreeAim (dragState == DragState.Player));
					}
					else
					{
						if (dragState == DragState.Player)
						{
							if (KickStarter.settingsManager.IsFirstPersonDragMovement ())
							{
								freeAim = GetSmoothFreeAim (new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, 0f));
							}
							else
							{
								freeAim = GetSmoothFreeAim (new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, -dragVector.y * KickStarter.settingsManager.freeAimTouchSpeed));
							}
						}
						else
						{
							freeAim = GetSmoothFreeAim (Vector2.zero);
						}
					}
				}
				else if (KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController)
				{
					// Cursor lock
					if (freeAimLock && KickStarter.settingsManager.IsInFirstPerson ())
					{
						cursorIsLocked = false;
					}
					else if (dragObject != null && 
							 KickStarter.settingsManager.IsInFirstPerson () && 
							 KickStarter.settingsManager.disableFreeAimWhenDragging)
					{
						cursorIsLocked = false;
					}
					else if (KickStarter.stateHandler.IsInGameplay ())
					{
						if (forceGameplayCursor == ForceGameplayCursor.KeepLocked)
						{
							cursorIsLocked = true;
						}
						else if (forceGameplayCursor == ForceGameplayCursor.KeepUnlocked)
						{
							cursorIsLocked = false;
						}

						else
						{
							cursorIsLocked = toggleCursorOn;
						}
					}
					else
					{
						cursorIsLocked = false;
					}

					// Cursor position
					if (cursorIsLocked)
					{
						mousePosition = LockedCursorPosition;
					}
					else
					{
						if (KickStarter.settingsManager.scaleCursorSpeedWithScreen)
						{
							xboxCursor.x += InputGetAxis ("CursorHorizontal") * cursorMoveSpeed * GetDeltaTime () * ACScreen.width * 0.5f;
							xboxCursor.y += InputGetAxis ("CursorVertical") * cursorMoveSpeed * GetDeltaTime () * ACScreen.height * 0.5f;
						}
						else
						{
							xboxCursor.x += InputGetAxis ("CursorHorizontal") * cursorMoveSpeed * GetDeltaTime () * 300f;
							xboxCursor.y += InputGetAxis ("CursorVertical") * cursorMoveSpeed * GetDeltaTime () * 300f;
						}

						xboxCursor.x = Mathf.Clamp (xboxCursor.x, 0f, ACScreen.width);
						xboxCursor.y = Mathf.Clamp (xboxCursor.y, 0f, ACScreen.height);
						
						mousePosition = xboxCursor;
						freeAim = Vector2.zero;
					}

					freeAim = GetSmoothFreeAim (InputGetFreeAim (cursorIsLocked, 50f));
					
					// Cursor state
					if (mouseState == MouseState.Normal)
					{
						dragState = DragState.None;
					}
					
					if (InputGetButtonDown ("InteractionA"))
					{
						if (mouseState == MouseState.Normal)
						{
							if (CanDoubleClick ())
							{
								mouseState = MouseState.DoubleClick;
								ResetClick ();
							}
							else if (CanClick ())
							{
								dragStartPosition = GetInvertedMouse ();
								
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
						}
					}
					else if (InputGetButtonDown (dragOverrideInput))
					{
						if (mouseState == MouseState.Normal && !CanDoubleClick () && CanClick ())
						{
							dragStartPosition = GetInvertedMouse ();
						}
					}
					else if (InputGetButtonDown ("InteractionB"))
					{
						mouseState = MouseState.RightClick;
					}
					else if (!string.IsNullOrEmpty (dragOverrideInput) && InputGetButton (dragOverrideInput))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else if (string.IsNullOrEmpty (dragOverrideInput) && InputGetButton ("InteractionA"))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else
					{
						ResetMouseClick ();
					}

					SetDoubleClickState ();
				}

				if (KickStarter.playerInteraction.GetHotspotMovingTo () != null)
				{
					freeAim = Vector2.zero;
				}

				if (KickStarter.stateHandler.IsInGameplay ())
				{
					DetectCursorInputs ();
				}

				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot &&
					InputGetButtonDown ("DefaultInteraction") &&
					KickStarter.settingsManager.allowDefaultInventoryInteractions &&
					KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple &&
					KickStarter.settingsManager.CanSelectItems (false) &&
					KickStarter.runtimeInventory.SelectedItem == null &&
					KickStarter.runtimeInventory.hoverItem != null && 
					KickStarter.playerInteraction.GetActiveHotspot () == null)
				{
					KickStarter.runtimeInventory.hoverItem.RunDefaultInteraction ();
					ResetMouseClick ();
					ResetClick ();
					return;
				}

				if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && KickStarter.playerMenus.IsInteractionMenuOn ())
				{
					float cycleInteractionsInput = InputGetAxis ("CycleInteractions");

					if (InputGetButtonDown ("CycleInteractionsRight"))
					{
						KickStarter.playerInteraction.SetNextInteraction ();
					}
					else if (InputGetButtonDown ("CycleInteractionsLeft"))
					{
						KickStarter.playerInteraction.SetPreviousInteraction ();
					}

					if (cycleInteractionsInput > 0.1f)
					{
						if (canCycleInteractionInput)
						{
							canCycleInteractionInput = false;
							KickStarter.playerInteraction.SetNextInteraction ();
						}
					}
					else if (cycleInteractionsInput < -0.1f)
					{
						if (canCycleInteractionInput)
						{
							canCycleInteractionInput = false;
							KickStarter.playerInteraction.SetPreviousInteraction ();
						}
					}
					else
					{
						canCycleInteractionInput = true;
					}
				}

				mousePosition = KickStarter.mainCamera.LimitToAspect (mousePosition);
				if (resetMouseDelta)
				{
					lastMousePosition = mousePosition;
					resetMouseDelta = false;
				}

				if (mouseState == MouseState.Normal && !hasUnclickedSinceClick)
				{
					hasUnclickedSinceClick = true;
				}
				
				if (mouseState == MouseState.Normal)
				{
					canDragMoveable = true;
				}
				
				UpdateDrag ();
				
				if (dragState != DragState.None)
				{
					dragVector = GetInvertedMouse () - dragStartPosition;
					dragSpeed = dragVector.magnitude;
				}
				else
				{
					dragVector = Vector2.zero;
					dragSpeed = 0f;
				}

				UpdateActiveInputs ();

				if (mousePosition.x < 0f || mousePosition.x > ACScreen.width || mousePosition.y < 0f || mousePosition.y > ACScreen.height)
				{
					mouseIsOnScreen = false;
				}
				else
				{
					mouseIsOnScreen = true;
				}
			}

			UpdateDragLine ();
		}


		protected void SetDoubleClickState ()
		{
			if (mouseState == MouseState.DoubleClick)
			{
				lastClickWasDouble = true;
			}
			else if (mouseState == MouseState.SingleClick || mouseState == MouseState.RightClick || mouseState == MouseState.LetGo)
			{
				lastClickWasDouble = false;
			}

			if (mouseState == MouseState.DoubleClick || mouseState == MouseState.RightClick || mouseState == MouseState.SingleClick)
			{
				lastclickTime = clickDelay;
			}
			else if (lastclickTime > 0f)
			{
				lastclickTime -= Time.deltaTime;
			}
		}


		/**
		 * <summary>Checks if the player clicked within the last few frames. This is useful when checking for input in Actions, because Actions do not run every frame.</summary>
		 * <param name = "checkForDouble">If True, then the check will be made for a double-click, rather than a single-click.</param>
		 * <returns>True if the player recently clicked.</returns>
		 */
		public bool ClickedRecently (bool checkForDouble = false)
		{
			if (lastclickTime > 0f)
			{
				if (checkForDouble == lastClickWasDouble)
				{
					return true;
				}
			}
			return false;
		}


		protected void UpdateActiveInputs ()
		{
			if (KickStarter.settingsManager.activeInputs != null)
			{
				for (int i=0; i<KickStarter.settingsManager.activeInputs.Count; i++)
				{
					bool responded = KickStarter.settingsManager.activeInputs[i].TestForInput ();
					if (responded) return;
				}
			}
		}


		protected void DetectCursorInputs ()
		{
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.cursorManager.allowIconInput)
			{
				if (KickStarter.cursorManager.allowWalkCursor)
				{
					if (InputGetButtonDown ("Icon_Walk"))
					{
						KickStarter.runtimeInventory.SetNull ();
						KickStarter.playerCursor.ResetSelectedCursor ();
						return;
					}
				}

				foreach (CursorIcon icon in KickStarter.cursorManager.cursorIcons)
				{
					if (InputGetButtonDown (icon.GetButtonName ()))
					{
						KickStarter.runtimeInventory.SetNull ();
						KickStarter.playerCursor.SetCursor (icon);
						return;
					}
				}
			}
		}


		/**
		 * <summary>Gets the cursor's position in screen space.</summary>
		 * <returns>The cursor's position in screen space</returns>
		 */
		public Vector2 GetMousePosition ()
		{
			return mousePosition;
		}


		/**
		 * <summary>Gets the y-inverted cursor position. This is useful because Menu Rects are drawn upwards, while screen space is measured downwards.</summary>
		 * <returns>Gets the y-inverted cursor position. This is useful because Menu Rects are drawn upwards, while screen space is measured downwards.</returns>
		 */
		public Vector2 GetInvertedMouse ()
		{
			return new Vector2 (GetMousePosition ().x, ACScreen.height - GetMousePosition ().y);
		}


		/**
		 * <summary>Sets the position of the simulated cursor, which is used when the game's Input method is set to Keyboard Or Controller</summary>
		 * <param name = "newPosition">The position, in screen-space co-ordinates, to move the simulated cursor to<param>
		 */
		public void SetSimulatedCursorPosition (Vector2 newPosition)
		{
			xboxCursor = newPosition;

			if (!cursorIsLocked)
			{
				mousePosition = xboxCursor;
			}
		}


		/**
		 * <summary>Initialises the cursor lock based on a given movement method.</summary>
		 * <param name = "movementMethod">The new movement method</param>
		 */
		public void InitialiseCursorLock (MovementMethod movementMethod)
		{
			if (KickStarter.settingsManager.IsInFirstPerson () && movementMethod != MovementMethod.FirstPerson)
			{
				toggleCursorOn = false;
			}
			else// if (!KickStarter.settingsManager.IsInFirstPerson () && movementMethod == MovementMethod.FirstPerson)
			{
				toggleCursorOn = KickStarter.settingsManager.lockCursorOnStart;

				if (toggleCursorOn && !KickStarter.settingsManager.IsInFirstPerson () && KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard && KickStarter.settingsManager.hotspotDetection == HotspotDetection.MouseOver)
				{
					ACDebug.Log ("Starting a non-First Person game with a locked cursor - is this correct?"); 
				}
			}
		}


		/**
		 * <summary>Checks if the cursor's position can be read. This is only ever False if the cursor cannot be dragged on a touch-screen.</summary>
		 * <returns>True if the cursor's position can be read</returns>
		 */
		public bool IsCursorReadable ()
		{
			if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				if (mouseState == MouseState.Normal)
				{
					if (KickStarter.runtimeInventory.SelectedItem != null &&  KickStarter.settingsManager.InventoryDragDrop)
					{
						return true;
					}
					return KickStarter.settingsManager.CanDragCursor ();
				}
			}
			return true;
		}


		/**
		 * Detects the pressing of the numeric keys if they can be used to trigger a Conversation's dialogue options.
		 */
		public void DetectConversationNumerics ()
		{		
			if (activeConversation != null && KickStarter.settingsManager.runConversationsWithKeys)
			{
				Event e = Event.current;
				if (e.isKey && e.type == EventType.KeyDown)
				{
					if (e.keyCode == KeyCode.Alpha1 || e.keyCode == KeyCode.Keypad1)
					{
						activeConversation.RunOption (0);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha2 || e.keyCode == KeyCode.Keypad2)
					{
						activeConversation.RunOption (1);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha3 || e.keyCode == KeyCode.Keypad3)
					{
						activeConversation.RunOption (2);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha4 || e.keyCode == KeyCode.Keypad4)
					{
						activeConversation.RunOption (3);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha5 || e.keyCode == KeyCode.Keypad5)
					{
						activeConversation.RunOption (4);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha6 || e.keyCode == KeyCode.Keypad6)
					{
						activeConversation.RunOption (5);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha7 || e.keyCode == KeyCode.Keypad7)
					{
						activeConversation.RunOption (6);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha8 || e.keyCode == KeyCode.Keypad8)
					{
						activeConversation.RunOption (7);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha9 || e.keyCode == KeyCode.Keypad9)
					{
						activeConversation.RunOption (8);
						return;
					}
				}
			}
		}


		/**
		 * Detects the pressing of the defined input buttons if they can be used to trigger a Conversation's dialogue options.
		 */
		public void DetectConversationInputs ()
		{		
			if (activeConversation != null && KickStarter.settingsManager.runConversationsWithKeys)
			{
				if (InputGetButtonDown ("DialogueOption1"))
				{
					activeConversation.RunOption (0);
				}
				else if (InputGetButtonDown ("DialogueOption2"))
				{
					activeConversation.RunOption (1);
				}
				else if (InputGetButtonDown ("DialogueOption3"))
				{
					activeConversation.RunOption (2);
				}
				else if (InputGetButtonDown ("DialogueOption4"))
				{
					activeConversation.RunOption (3);
				}
				else if (InputGetButtonDown ("DialogueOption5"))
				{
					activeConversation.RunOption (4);
				}
				else if (InputGetButtonDown ("DialogueOption6"))
				{
					activeConversation.RunOption (5);
				}
				else if (InputGetButtonDown ("DialogueOption7"))
				{
					activeConversation.RunOption (6);
				}
				else if (InputGetButtonDown ("DialogueOption8"))
				{
					activeConversation.RunOption (7);
				}
				else if (InputGetButtonDown ("DialogueOption9"))
				{
					activeConversation.RunOption (8);
				}
			}
			
		}
		
		
		/**
		 * Draws a drag-line on screen if the chosen movement method allows for one.
		 */
		public void DrawDragLine ()
		{
			if (KickStarter.settingsManager.drawDragLine && dragEndPosition != Vector2.zero)
			{
				DrawStraightLine.Draw (dragStartPosition, dragEndPosition, KickStarter.settingsManager.dragLineColor, KickStarter.settingsManager.dragLineWidth, true);
			}
		}


		protected void UpdateDragLine ()
		{
			dragEndPosition = Vector2.zero;

			if (dragState == DragState.Player && KickStarter.settingsManager.movementMethod != MovementMethod.StraightToCursor)
			{
				dragEndPosition = GetInvertedMouse ();
				KickStarter.eventManager.Call_OnUpdateDragLine (dragStartPosition, dragEndPosition);
			}
			else
			{
				KickStarter.eventManager.Call_OnUpdateDragLine (Vector2.zero, Vector2.zero);
			}

			if (activeDragElement != null)
			{
				if (mouseState == MouseState.HeldDown)
				{
					if (!activeDragElement.DoDrag (GetDragVector ()))
					{
						activeDragElement = null;
					}
				}
				else if (mouseState == MouseState.Normal)
				{
					if (activeDragElement.CheckStop (GetInvertedMouse ()))
					{
						activeDragElement = null;
					}
				}
			}
		}


		/**
		 * Updates the input variables needed for Direct movement.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateDirectInput ()
		{
			if (KickStarter.settingsManager != null)
			{
				if (activeArrows != null)
				{
					if (activeArrows.arrowPromptType == ArrowPromptType.KeyOnly || activeArrows.arrowPromptType == ArrowPromptType.KeyAndClick)
					{
						Vector2 normalizedVector = new Vector2 (InputGetAxis ("Horizontal"), -InputGetAxis ("Vertical"));

						if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && dragState == DragState.ScreenArrows)
						{
							normalizedVector = GetDragVector () / KickStarter.settingsManager.dragRunThreshold / KickStarter.settingsManager.dragWalkThreshold;
						}

						if (normalizedVector.sqrMagnitude > 0f)
						{
							float threshold = 0.95f;
							if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard)
							{
								threshold = 0.05f;
							}

							if (normalizedVector.x > threshold)
							{
								activeArrows.DoRight ();
							}
							else if (normalizedVector.x < -threshold)
							{
								activeArrows.DoLeft ();
							}
							else if (normalizedVector.y < -threshold)
							{
								activeArrows.DoUp();
							}
							else if (normalizedVector.y > threshold)
							{
								activeArrows.DoDown ();
							}
						}
					}
					
					if (activeArrows != null && (activeArrows.arrowPromptType == ArrowPromptType.ClickOnly || activeArrows.arrowPromptType == ArrowPromptType.KeyAndClick))
					{
						// Arrow Prompt is displayed: respond to mouse clicks
						Vector2 invertedMouse = GetInvertedMouse ();
						if (mouseState == MouseState.SingleClick)
						{
							if (activeArrows.upArrow.rect.Contains (invertedMouse))
							{
								activeArrows.DoUp ();
							}
							
							else if (activeArrows.downArrow.rect.Contains (invertedMouse))
							{
								activeArrows.DoDown ();
							}
							
							else if (activeArrows.leftArrow.rect.Contains (invertedMouse))
							{
								activeArrows.DoLeft ();
							}
							
							else if (activeArrows.rightArrow.rect.Contains (invertedMouse))
							{
								activeArrows.DoRight ();
							}
						}
					}
				}
				
				if (activeArrows == null && KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick)
				{
					float h = 0f;
					float v = 0f;
					bool run;
					
					if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen || KickStarter.settingsManager.movementMethod == MovementMethod.Drag)
					{
						if (KickStarter.settingsManager.IsInFirstPerson () && KickStarter.settingsManager.firstPersonTouchScreen == FirstPersonTouchScreen.CustomInput)
						{
							h = InputGetAxis ("Horizontal");
							v = InputGetAxis ("Vertical");
						}
						else if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.settingsManager.movementMethod == MovementMethod.Direct && KickStarter.settingsManager.directTouchScreen == DirectTouchScreen.CustomInput)
						{
							h = InputGetAxis ("Horizontal");
							v = InputGetAxis ("Vertical");
						}
						else if (dragState != DragState.None)
						{
							h = dragVector.x;
							v = -dragVector.y;
						}
					}
					else
					{
						h = InputGetAxis ("Horizontal");
						v = InputGetAxis ("Vertical");
					}

					if ((isUpLocked && v > 0f) || (isDownLocked && v < 0f))
					{
						v = 0f;
					}
					
					if ((isLeftLocked && h > 0f) || (isRightLocked && h < 0f))
					{
						h = 0f;
					}
					
					if (runLock == PlayerMoveLock.Free)
					{
						if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen || KickStarter.settingsManager.movementMethod == MovementMethod.Drag)
						{
							if (dragStartPosition != Vector2.zero && dragSpeed > KickStarter.settingsManager.dragRunThreshold * 10f)
							{
								run = true;
							}
							else
							{
								run = false;
							}
						}
						else
						{
							if (InputGetAxis ("Run") > 0.1f)
							{
								run = true;
							}
							else
							{
								run = InputGetButton ("Run");
							}

							if (InputGetButtonDown ("ToggleRun") && KickStarter.player)
							{
								KickStarter.player.toggleRun = !KickStarter.player.toggleRun;
							}
						}
					}
					else if (runLock == PlayerMoveLock.AlwaysWalk)
					{
						run = false;
					}
					else
					{
						run = true;
					}
					
					if (KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen && (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson || KickStarter.settingsManager.movementMethod == MovementMethod.Direct) && runLock == PlayerMoveLock.Free && KickStarter.player && KickStarter.player.toggleRun)
					{
						playerIsControlledRunning = !run;
					}
					else
					{
						playerIsControlledRunning = run;
					}

					moveKeys = CreateMoveKeys (h, v);
				}

				if (InputGetButtonDown ("FlashHotspots"))
				{
					FlashHotspots ();
				}
			}
		}


		protected Vector2 CreateMoveKeys (float h, float v)
		{
			if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct && KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen && KickStarter.settingsManager.directMovementType == DirectMovementType.RelativeToCamera)
			{
				if (KickStarter.settingsManager.limitDirectMovement == LimitDirectMovement.FourDirections)
				{
					if (Mathf.Abs (h) > Mathf.Abs (v))
					{
						v = 0f;
					}
					else
					{
						h = 0f;
					}
				}
				else if (KickStarter.settingsManager.limitDirectMovement == LimitDirectMovement.EightDirections)
				{
					if (Mathf.Abs (h) > Mathf.Abs (v))
					{
						v = 0f;
					}
					else if (Mathf.Abs (h) < Mathf.Abs (v))
					{
						h = 0f;
					}
					else if (Mathf.Abs (h) > 0.4f && Mathf.Abs (v) > 0.4f)
					{
						if (h*v > 0)
						{
							h = v;
						}
						else
						{
							h = -v;
						}
					}
					else
					{
						h = v = 0f;
					}
				}
			}

			if (cameraLockSnap)
			{
				Vector2 newMoveKeys = new Vector2 (h, v);
				if (newMoveKeys.sqrMagnitude < 0.01f || Vector2.Angle (newMoveKeys, moveKeys) > 5f)
				{
					cameraLockSnap = false;
					return newMoveKeys;
				}
				return moveKeys;
			}

			return new Vector2 (h, v);
		}


		protected virtual void FlashHotspots ()
		{
			Hotspot[] hotspots = KickStarter.stateHandler.Hotspots.ToArray ();
			foreach (Hotspot hotspot in hotspots)
			{
				if (hotspot.highlight)
				{
					if (hotspot.IsOn () && hotspot.PlayerIsWithinBoundary () && hotspot != KickStarter.playerInteraction.GetActiveHotspot ())
					{
						hotspot.highlight.Flash ();
					}
				}
			}
		}
		

		/**
		 * Disables the active ArrowPrompt.
		 */
		public void RemoveActiveArrows ()
		{
			if (activeArrows)
			{
				activeArrows.TurnOff ();
			}
		}
		

		/**
		 * Records the current click time, so that another click will not register for the duration of clickDelay.
		 */
		public void ResetClick ()
		{
			clickTime = clickDelay;
			hasUnclickedSinceClick = false;
		}
		
		
		protected void ResetDoubleClick ()
		{
			doubleClickTime = doubleClickDelay;
		}
		

		/**
		 * <summary>Checks if a mouse click will be registered.</summary>
		 * <returns>True if a mouse click will be registered</returns>
		 */
		public bool CanClick ()
		{
			if (clickTime <= 0f)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if a mouse double-click will be registered.</summary>
		 * <returns>True if a mouse double-click will be registered</returns>
		 */
		public bool CanDoubleClick ()
		{
			if (doubleClickTime > 0f && clickTime <= 0f)
			{
				return true;
			}
			
			return false;
		}


		/**
		 * <summary>Simulates the pressing of an Input button.</summary>
		 * <param name = "button">The name of the Input button</param>
		 */
		public void SimulateInputButton (string button)
		{
			SimulateInput (SimulateInputType.Button, button, 1f);
		}
		

		/**
		 * <summary>Simulates the pressing of an Input axis.</summary>
		 * <param name = "axis">The name of the Input axis</param>
		 * <param name = "value">The value to assign the Input axis</param>
		 */
		public void SimulateInputAxis (string axis, float value)
		{
			SimulateInput (SimulateInputType.Axis, axis, value);
		}


		/**
		 * <summary>Simulates the pressing of an Input button or axis.</summary>
		 * <param name = "input">The type of Input this is simulating (Button, Axis)</param>
		 * <param name = "axis">The name of the Input button or axis</param>
		 * <param name = "value">The value to assign the Input axis, if input = SimulateInputType.Axis</param>
		 */
		public void SimulateInput (SimulateInputType input, string axis, float value)
		{
			if (!string.IsNullOrEmpty (axis))
			{
				menuInput = input;
				menuButtonInput = axis;
				
				if (input == SimulateInputType.Button)
				{
					menuButtonValue = 1f;
				}
				else
				{
					menuButtonValue = value;
				}

				CancelInvoke ();
				Invoke ("StopSimulatingInput", 0.1f);
			}
		}


		/**
		 * <summary>Checks if the cursor is locked.</summary>
		 * <returns>True if the cursor is locked</returns>
		 */
		public bool IsCursorLocked ()
		{
			return cursorIsLocked;
		}


		protected void StopSimulatingInput ()
		{
			menuButtonInput = string.Empty;
		}


		/**
		 * <summary>Checks if any input button is currently being pressed, simulated or otherwise.</summary>
		 * <returns>True if any input button is currently being pressed, simulated or otherwise.</returns>
		 */
		public bool InputAnyKey ()
		{
			if (menuButtonInput != null && !string.IsNullOrEmpty (menuButtonInput))
			{
				return true;
			}
			return Input.anyKey;
		}


		protected float InputGetAxisRaw (string axis)
		{
			if (string.IsNullOrEmpty (axis))
			{
				return 0f;
			}

			if (InputGetAxisDelegate != null)
			{
				return InputGetAxisDelegate (axis);
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (!Mathf.Approximately (Input.GetAxisRaw (axis), 0f))
				{
					return Input.GetAxisRaw (axis);
				}
			}
			else
			{
				try
				{
					if (!Mathf.Approximately (Input.GetAxisRaw (axis), 0f))
					{
						return Input.GetAxisRaw (axis);
					}
				}
				catch {}
			}
			
			if (!string.IsNullOrEmpty (menuButtonInput) && menuButtonInput == axis && menuInput == SimulateInputType.Axis)
			{
				return menuButtonValue;
			}
			
			return 0f;
		}
		

		/**
		 * <summary>Replaces "Input.GetAxis", allowing for custom overrides.</summary>
		 * <param name = "axis">The Input axis to detect</param>
		 * <returns>The Input axis' value</returns>
		 */
		public float InputGetAxis (string axis)
		{
			if (string.IsNullOrEmpty (axis))
			{
				return 0f;
			}

			if (InputGetAxisDelegate != null)
			{
				return InputGetAxisDelegate (axis);
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (!Mathf.Approximately (Input.GetAxis (axis), 0f))
				{
					return Input.GetAxis (axis);
				}
			}
			else
			{
				try
				{
					if (!Mathf.Approximately (Input.GetAxis (axis), 0f))
					{
						return Input.GetAxis (axis);
					}
				}
				catch {}
			}

			if (!string.IsNullOrEmpty (menuButtonInput) && menuButtonInput == axis && menuInput == SimulateInputType.Axis)
			{
				return menuButtonValue;
			}
			
			return 0f;
		}
		
		
		protected bool InputGetMouseButton (int button)
		{
			if (InputGetMouseButtonDelegate != null)
			{
				return InputGetMouseButtonDelegate (button);
			}

			if (KickStarter.settingsManager.inputMethod != InputMethod.MouseAndKeyboard || KickStarter.settingsManager.defaultMouseClicks)
			{
				return Input.GetMouseButton (button);
			}
			return false;
		}
		
		
		protected Vector2 InputMousePosition (bool _cursorIsLocked)
		{
			if (InputMousePositionDelegate != null)
			{
				return InputMousePositionDelegate (_cursorIsLocked);
			}

			if (_cursorIsLocked)
			{
				return LockedCursorPosition;
			}
			return Input.mousePosition;
		}


		protected Vector2 InputTouchPosition (int index)
		{
			if (InputTouchPositionDelegate != null)
			{
				return InputTouchPositionDelegate (index);
			}

			if (InputTouchCount () > index)
			{
				return Input.GetTouch (index).position;
			}
			return Vector2.zero;			
		}


		protected Vector2 InputTouchDeltaPosition (int index)
		{
			if (InputTouchPositionDelegate != null)
			{
				return InputTouchDeltaPositionDelegate (index);
			}

			if (InputTouchCount () > index)
			{
				Touch t = Input.GetTouch (0);
				if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					return t.deltaPosition * 1.7f;
				}
				return t.deltaPosition * Time.deltaTime / t.deltaTime;
			}
			return Vector2.zero;
		}


		protected TouchPhase InputTouchPhase (int index)
		{
			if (InputGetTouchPhaseDelegate != null)
			{
				return InputGetTouchPhaseDelegate (index);
			}

			return Input.GetTouch (index).phase;
		}


		protected int InputTouchCount ()
		{
			if (InputTouchCountDelegate != null)
			{
				return InputTouchCountDelegate ();
			}

			return Input.touchCount;
		}


		protected Vector2 InputGetFreeAim (bool _cursorIsLocked, float scaleFactor = 1f)
		{
			if (InputGetFreeAimDelegate != null)
			{
				return InputGetFreeAimDelegate (_cursorIsLocked);
			}

			if (_cursorIsLocked)
			{
				return new Vector2 (InputGetAxis ("CursorHorizontal") * scaleFactor, InputGetAxis ("CursorVertical") * scaleFactor);
			}
			return Vector2.zero;
		}
		
		
		protected bool InputGetMouseButtonDown (int button)
		{
			if (InputGetMouseButtonDownDelegate != null)
			{
				return InputGetMouseButtonDownDelegate (button);
			}

			if (KickStarter.settingsManager.inputMethod != InputMethod.MouseAndKeyboard || KickStarter.settingsManager.defaultMouseClicks)
			{
				return Input.GetMouseButtonDown (button);
			}
			return false;
		}
		

		/**
		 * <summary>Replaces "Input.GetButton", allowing for custom overrides.</summary>
		 * <param name = "axis">The Input button to detect</param>
		 * <returns>True if the Input button is being held down this frame</returns>
		 */
		public bool InputGetButton (string axis)
		{
			if (string.IsNullOrEmpty (axis))
			{
				return false;
			}

			if (InputGetButtonDelegate != null)
			{
				return InputGetButtonDelegate (axis);
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetButton (axis))
				{
					return true;
				}
			}
			else
			{
				try
				{
					if (Input.GetButton (axis))
					{
						return true;
					}
				}
				catch {}
			}

			if (!string.IsNullOrEmpty (menuButtonInput) && menuButtonInput == axis && menuInput == SimulateInputType.Button)
			{
				if (menuButtonValue > 0f)
				{
					//ResetClick ();
					StopSimulatingInput ();	
					return true;
				}
				
				StopSimulatingInput ();
			}

			return false;
		}
		

		/**
		 * <summary>Replaces "Input.GetButton", allowing for custom overrides.</summary>
		 * <param name = "axis">The Input button to detect</param>
		 * <param name = "showError">If True, then an error message will appear in the Console window if the button is not defined in the Input manager</param>
		 * <returns>True if the Input button was first pressed down this frame</returns>
		 */
		public bool InputGetButtonDown (string axis, bool showError = false)
		{
			if (string.IsNullOrEmpty (axis))
			{
				return false;
			}

			if (InputGetButtonDownDelegate != null)
			{
				return InputGetButtonDownDelegate (axis);
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetButtonDown (axis))
				{
					return true;
				}
			}
			else
			{
				try
				{
					if (Input.GetButtonDown (axis))
					{
						return true;
					}
				}
				catch
				{
					if (showError)
					{
						ACDebug.LogWarning ("Cannot find Input button '" + axis + "' - please define it in Unity's Input Manager (Edit -> Project settings -> Input).");
					}
				}
			}

			if (!string.IsNullOrEmpty (menuButtonInput) && menuButtonInput == axis && menuInput == SimulateInputType.Button)
			{
				if (menuButtonValue > 0f)
				{
					//ResetClick ();
					StopSimulatingInput ();	
					return true;
				}
				
				StopSimulatingInput ();
			}
			
			return false;
		}


		/**
		 * <summary>Replaces "Input.GetButtonUp".</summary>
		 * <param name = "axis">The Input button to detect</param>
		 * <returns>True if the Input button is released</returns>
		 */
		public bool InputGetButtonUp (string axis)
		{
			if (string.IsNullOrEmpty (axis))
			{
				return false;
			}

			if (InputGetButtonUpDelegate != null)
			{
				return InputGetButtonUpDelegate (axis);
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetButtonUp (axis))
				{
					return true;
				}
			}
			else
			{
				try
				{
					if (Input.GetButtonUp (axis))
					{
						return true;
					}
				}
				catch {}
			}
			return false;
		}
		
		
		protected void SetDragState ()
		{
			DragState oldDragState = dragState;

			if (KickStarter.runtimeInventory.SelectedItem != null &&  KickStarter.settingsManager.InventoryDragDrop && (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused))
			{
				if (dragVector.magnitude >= KickStarter.settingsManager.dragDropThreshold)
				{
					dragState = DragState.Inventory;
				}
				else
				{
					dragState = DragState.PreInventory;
				}
			}
			else if (activeDragElement != null && (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused))
			{
				dragState = DragState.Menu;
			}
			else if (activeArrows != null && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				dragState = DragState.ScreenArrows;
			}
			else if (dragObject != null)
			{
				dragState = DragState.Moveable;
			}
			else if (KickStarter.mainCamera.attachedCamera && KickStarter.mainCamera.attachedCamera.isDragControlled)
			{
				if (!KickStarter.playerInteraction.IsMouseOverHotspot ())
				{
					dragState = DragState._Camera;

					if (!cursorIsLocked && (deltaDragMouse.magnitude * Time.deltaTime <= 1f) && (GetInvertedMouse () - dragStartPosition).magnitude < 10f)
					{
						dragState = DragState.None;
					}
				}
			}
			else if ((KickStarter.settingsManager.movementMethod == MovementMethod.Drag || KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor ||
					  (KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen))
					&& KickStarter.settingsManager.movementMethod != MovementMethod.None && KickStarter.stateHandler.IsInGameplay ())
			{
				if (!KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerMenus.IsInteractionMenuOn ())
				{
					if (KickStarter.playerInteraction.IsMouseOverHotspot ())
					{}
					else
					{
						dragState = DragState.Player;
					}
				}
			}
			else
			{
				dragState = DragState.None;
			}

			if (oldDragState == DragState.None && dragState != DragState.None)
			{
				resetMouseDelta = true;
				lastMousePosition = mousePosition;
			}
		}


		protected void SetDragStateTouchScreen ()
		{
			if (KickStarter.runtimeInventory.SelectedItem != null &&  KickStarter.settingsManager.InventoryDragDrop && (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused))
			{}
			else if (activeDragElement != null && (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused))
			{}
			else if (activeArrows != null && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{}
			else if (dragObject != null)
			{}
			else if (KickStarter.mainCamera.attachedCamera && KickStarter.mainCamera.attachedCamera.isDragControlled)
			{}
			else if ((KickStarter.settingsManager.movementMethod == MovementMethod.Drag || KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor ||
					  (KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen))
					  && KickStarter.settingsManager.movementMethod != MovementMethod.None && KickStarter.stateHandler.IsInGameplay ())
			{
				if (!KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerMenus.IsInteractionMenuOn ())
				{
					if (KickStarter.playerInteraction.IsMouseOverHotspot ())
					{}
					else
					{
						dragState = DragState.Player;
					}
				}
			}
			else
			{
				dragState = DragState.None;
			}
		}


		protected void UpdateDrag ()
		{
			if (dragState != DragState.None)
			{
				// Calculate change in mouse position
				if (freeAim.sqrMagnitude > 0f)
				{
					deltaDragMouse = freeAim * 500f / Time.deltaTime;
				}
				else
				{
					deltaDragMouse = (mousePosition - lastMousePosition) / Time.deltaTime;
				}
			}

			if (dragObject && KickStarter.stateHandler.gameState != GameState.Normal)
			{
				LetGo ();
				return;
			}

			if (mouseState == MouseState.HeldDown && dragState == DragState.None && KickStarter.stateHandler.CanInteractWithDraggables () && !KickStarter.playerMenus.IsMouseOverMenu ())
			{
				Grab ();
			}
			else if (dragState == DragState.Moveable)
			{
				if (dragObject)
				{
					if (dragObject.IsHeld && dragObject.IsOnScreen () && dragObject.IsCloseToCamera (KickStarter.settingsManager.moveableRaycastLength))
					{
						//Drag ();
					}
					else
					{
						LetGo ();
					}
				}
			}
			else if (dragObject)
			{
				LetGo ();
			}
		}


		public void _FixedUpdate ()
		{
			if (mouseState == MouseState.HeldDown && dragState == DragState.None && KickStarter.stateHandler.CanInteract () && !KickStarter.playerMenus.IsMouseOverMenu ())
			{}
			else if (dragState == DragState.Moveable)
			{
				if (dragObject)
				{
					if (dragObject.IsHeld && dragObject.IsOnScreen () && dragObject.IsCloseToCamera (KickStarter.settingsManager.moveableRaycastLength))
					{
						Drag ();
					}
				}
			}
		}


		/**
		 * <summary>Enables or disables the free-aiming lock.</summary>
		 * <param name = "_state">If True, the free-aiming lock is enabled, and free-aiming is disabled</param>
		 */
		public void SetFreeAimLock (bool _state)
		{
			freeAimLock = _state;
		}


		/**
		 * <summary>Forces the letting-go of the currently-held DragBase, if set.</summary>
		 */
		public void LetGo ()
		{
			if (dragObject != null)
			{
				dragObject.LetGo ();
				dragObject = null;
			}
		}
		
		
		protected void Grab ()
		{
			if (dragObject)
			{
				dragObject.LetGo ();
				dragObject = null;
			}
			else if (canDragMoveable)
			{
				canDragMoveable = false;
				
				Ray ray = KickStarter.CameraMain.ScreenPointToRay (mousePosition); 
				RaycastHit hit = new RaycastHit ();
				
				if (Physics.Raycast (ray, out hit, KickStarter.settingsManager.moveableRaycastLength))
				{
					DragBase dragBase = hit.transform.GetComponent <DragBase>();
					if (dragBase != null && dragBase.PlayerIsWithinBoundary ())
					{
						dragObject = dragBase;
						dragObject.Grab (hit.point);
						lastCameraPosition = KickStarter.CameraMain.transform.position;

						KickStarter.eventManager.Call_OnGrabMoveable (dragObject);
					}
				}
			}
		}

		
		protected void Drag ()
		{
			// Convert to a 3D force
			if (dragObject.invertInput)
			{
				dragForce = (-KickStarter.CameraMain.transform.right * deltaDragMouse.x) + (-KickStarter.CameraMain.transform.up * deltaDragMouse.y);
			}
			else
			{
				dragForce = (KickStarter.CameraMain.transform.right * deltaDragMouse.x) + (KickStarter.CameraMain.transform.up * deltaDragMouse.y);
			}

			// Scale force with distance to camera, to lessen effects when close
			float distanceToCamera = (KickStarter.CameraMain.transform.position - dragObject.transform.position).magnitude;
			
			// Incoporate camera movement
			if (dragObject.playerMovementInfluence > 0f)
			{
				Vector3 deltaCamera = KickStarter.CameraMain.transform.position - lastCameraPosition;
				dragForce += deltaCamera * 100000f * dragObject.playerMovementInfluence;
			}

			dragForce /= Time.fixedDeltaTime * 50f;
			dragObject.ApplyDragForce (dragForce, mousePosition, distanceToCamera);
			
			lastCameraPosition = KickStarter.CameraMain.transform.position;
		}
		

		/**
		 * <summary>Gets the drag vector.</summary>
		 * <returns>The drag vector</returns>
		 */
		public Vector2 GetDragVector ()
		{
			if (dragState == AC.DragState._Camera)
			{
				return deltaDragMouse;
			}
			return dragVector;
		}
		

		/**
		 * <summary>Enables or disabled the Player's "up movement" lock.</summary>
		 * <param name = "state">If True, the "up movement" lock is enabled, and the player cannot move up</param>
		 */
		public void SetUpLock (bool state)
		{
			isUpLocked = state;
		}


		/**
		 * <summary>Enables or disabled the Player's "left movement" lock.</summary>
		 * <param name = "state">If True, the "up movement" lock is enabled, and the player cannot move left</param>
		 */
		public void SetLeftLock (bool state)
		{
			isLeftLocked = state;
		}


		/**
		 * <summary>Enables or disabled the Player's "right movement" lock.</summary>
		 * <param name = "state">If True, the "up movement" lock is enabled, and the player cannot move right</param>
		 */
		public void SetRightLock (bool state)
		{
			isRightLocked = state;
		}


		/**
		 * <summary>Enables or disabled the Player's "down movement" lock.</summary>
		 * <param name = "state">If True, the "up movement" lock is enabled, and the player cannot move down</param>
		 */
		public void SetDownLock (bool state)
		{
			isDownLocked = state;
		}


		/**
		 * <summary>Enables or disabled the Player's ability to jump.</summary>
		 * <param name = "state">If True, the "jump" lock is enabled, and the player cannot jump</param>
		 */
		public void SetJumpLock (bool state)
		{
			isJumpLocked = state;
		}


		/** True if the Player's ability to jump has been disabled */
		public bool IsJumpLocked
		{
			get
			{
				return isJumpLocked;
			}
		}


		/**
		 * <summary>Checks if the Player can be directly-controlled during gameplay.</summary>
		 * <returns>True if the Player can be directly-controlled during gameplay.</returns>
		 */
		public bool CanDirectControlPlayer ()
		{
			return !isUpLocked;
		}
		

		/**
		 * <summary>Checks if the active ArrowPrompt prevents Hotspots from being interactive.</summary>
		 * <returns>True if the active ArrowPrompt prevents Hotspots from being interactive</returns>
		 */
		public bool ActiveArrowsDisablingHotspots ()
		{
			if (activeArrows != null && activeArrows.disableHotspots)
			{
				return true;
			}
			return false;
		}
		

		protected void ToggleCursor ()
		{
			if (dragObject != null && !dragObject.CanToggleCursor ())
			{
				return;
			}
			toggleCursorOn = !toggleCursorOn;
		}


		/**
		 * <summary>Sets the lock state of the in-game cursor manually. When locked, the cursor will be placed in the centre of the screen during gameplay.</summary>
		 * <param name = "lockState">If True, the cursor will be locked during gameplay</param>
		 */
		public void SetInGameCursorState (bool lockState)
		{
			toggleCursorOn = lockState;
		}


		/**
		 * <summary>Gets the locked state of the cursor during gameplay (i.e. when the game is not paused).</summary>
		 * <returns>True if the in-game cursor is locked in the centre of the screen</returns>
		 */
		public bool GetInGameCursorState ()
		{
			return toggleCursorOn;
		}
		

		/**
		 * <summary>Checks if a specific DragBase object is being held by the player.</summary>
		 * <param name "_dragBase">The DragBase to check for</param>
		 * <returns>True if the DragBase object is being held by the Player</returns>
		 */
		public bool IsDragObjectHeld (DragBase _dragBase)
		{
			if (_dragBase == null || dragObject == null)
			{
				return false;
			}
			if (_dragBase == dragObject)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if any DragBase object is being held by the player.</summary>
		 * <returns>True if any DragBase object is being held by the Player</returns>
		 */
		public bool IsDragObjectHeld ()
		{
			return (dragObject != null);
		}


		/**
		 * <summary>Gets the factor by which Player movement is slowed when holding a DragBase object.</summary>
		 * <returns>The factor by which Player movement is slowed when holding a DragBase object</returns>
		 */
		public float GetDragMovementSlowDown ()
		{
			if (dragObject != null)
			{
				return (1f - dragObject.playerMovementReductionFactor);
			}
			return 1f;
		}
		
		
		protected float GetDeltaTime ()
		{
			return Time.unscaledDeltaTime;
		}


		/**
		 * <summary>Sets the timeScale.</summary>
		 * <param name = "_timeScale">The new timeScale. A value of 0 will have no effect<param>
		 */
		public void SetTimeScale (float _timeScale)
		{
			if (_timeScale > 0f)
			{
				timeScale = _timeScale;
				if (KickStarter.stateHandler.gameState != GameState.Paused)
				{
					Time.timeScale = _timeScale;
				}
			}
		}


		/**
		 * <summary>Assigns an AnimationCurve that controls the timeScale over time.</summary>
		 * <param name = "_timeCurve">The AnimationCurve to use</param>
		 */
		public void SetTimeCurve (AnimationCurve _timeCurve)
		{
			timeCurve = _timeCurve;
			changeTimeStart = Time.time;
		}


		/**
		 * <summary>Checks if time is being controlled by an AnimationCurve.</summary>
		 * <returns>True if time is being controlled by an AnimationCurve.</returns>
		 */
		public bool HasTimeCurve ()
		{
			if (timeCurve != null)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Get what kind of object is currently being dragged (None, Player, Inventory, Menu, ScreenArrows, Moveable, _Camera).</summary>
		 * <returns>What kind of object is currently being dragged (None, Player, Inventory, Menu, ScreenArrows, Moveable, _Camera).</returns>
		 */
		public DragState GetDragState ()
		{
			return dragState;
		}


		/**
		 * <summary>Gets the current state of the mouse buttons (Normal, SingleClick, RightClick, DoubleClick, HeldDown, LetGo).</summary>
		 * <returns>The current state of the mouse buttons (Normal, SingleClick, RightClick, DoubleClick, HeldDown, LetGo).</returns>
		 */
		public MouseState GetMouseState ()
		{
			return mouseState;
		}


		/**
		 * Resets the mouse click so that nothing else will be affected by it this frame.
		 */
		public void ResetMouseClick ()
		{
			mouseState = MouseState.Normal;
		}


		/**
		 * <summary>Gets the input movement as a vector</summary>
		 * <returns>The input movement as a vector</returns>
		 */
		public Vector2 GetMoveKeys ()
		{
			return moveKeys;
		}


		/**
		 * <summary>Checks if the Player is running due to user-controlled input.</summary>
		 * <returns>True if the Player is running due to user-controller input</returns>
		 */
		public bool IsPlayerControlledRunning ()
		{
			return playerIsControlledRunning;
		}


		/**
		 * <summary>Assigns a MenuDrag element as the one to drag.</summary>
		 * <param name = "menuDrag">The MenuDrag to begin dragging</param>
		 */
		public void SetActiveDragElement (MenuDrag menuDrag)
		{
			activeDragElement = menuDrag;
		}


		/**
		 * <summary>Checks if the last mouse click made was a double-click.</summary>
		 * <returns>True if the last mouse click made was a double-click</returns>
		 */
		public bool LastClickWasDouble ()
		{
			return lastClickWasDouble;
		}


		/**
		 * Resets the speed of "Drag" Player input.
		 */
		public void ResetDragMovement ()
		{
			dragSpeed = 0f;
		}


		/**
		 * <summary>Checks if the magnitude of "Drag" Player input is above the minimum needed to move the Player.</summary>
		 * <returns>True if the magnitude of "Drag" Player input is above the minimum needed to move the Player.</returns>
		 */
		public bool IsDragMoveSpeedOverWalkThreshold ()
		{
			if (dragSpeed > KickStarter.settingsManager.dragWalkThreshold * 10f)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the cursor's position is within the boundary of the screen.</summary>
		 * <returns>True if the cursor's position is within the boundary of the screen</returns>
		 */
		public bool IsMouseOnScreen ()
		{
			return mouseIsOnScreen;
		}


		/**
		 * <summary>Gets the free-aim input vector.</summary>
		 * <returns>The free-aim input vector</returns>
		 */
		public Vector2 GetFreeAim ()
		{
			return freeAim;
		}


		protected LerpUtils.Vector2Lerp freeAimLerp = new LerpUtils.Vector2Lerp ();
		protected virtual Vector2 GetSmoothFreeAim (Vector2 targetFreeAim)
		{
			if (KickStarter.settingsManager.freeAimSmoothSpeed <= 0f)
			{
				return targetFreeAim;
			}

			float factor = 1f;
			if (dragObject != null)
			{
				factor = 1f - dragObject.playerMovementReductionFactor;
			}

			return freeAimLerp.Update (freeAim, targetFreeAim * factor, KickStarter.settingsManager.freeAimSmoothSpeed);
		}


		/**
		 * <summary>Checks if free-aiming is locked.</summary>
		 * <returns>True if free-aiming is locked</returns>
		 */
		public bool IsFreeAimingLocked ()
		{
			return freeAimLock;
		}


		/**
		 * <summary>Checks if the Player is prevented from being moved directly in all four directions.</summary>
		 * <returns>True if the Player is prevented from being moved directly in all four direction</returns>
		 */
		public bool AllDirectionsLocked ()
		{
			if (isDownLocked && isUpLocked && isLeftLocked && isRightLocked)
			{
				return true;
			}
			return false;
		}


		/**
		 * Resets the mouse and assigns the correct gameState in StateHandler after loading a save game.
		 */
		public void ReturnToGameplayAfterLoad ()
		{
			pendingOptionConversation = null;

			if (activeConversation)
			{
				KickStarter.stateHandler.gameState = GameState.DialogOptions;
			}
			else
			{
				KickStarter.stateHandler.gameState = GameState.Normal;
			}
			ResetMouseClick ();
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			mainData.timeScale = KickStarter.playerInput.timeScale;
			mainData.activeArrows = (activeArrows != null) ? Serializer.GetConstantID (activeArrows.gameObject) : 0;
			mainData.activeConversation = (activeConversation != null) ? Serializer.GetConstantID (activeConversation.gameObject) : 0;
			mainData.canKeyboardControlMenusDuringGameplay = canKeyboardControlMenusDuringGameplay;

			return mainData;
		}
		
		
		/**
		 * <summary>Updates its own variables from a MainData class.</summary>
		 * <param name = "mainData">The MainData class to load from</param>
		 */
		public void LoadMainData (MainData mainData)
		{
			// Active screen arrows
			RemoveActiveArrows ();
			ArrowPrompt loadedArrows = Serializer.returnComponent <ArrowPrompt> (mainData.activeArrows);
			if (loadedArrows)
			{
				loadedArrows.TurnOn ();
			}
			
			// Active conversation
			activeConversation = Serializer.returnComponent <Conversation> (mainData.activeConversation);
			pendingOptionConversation = null;
			timeScale = mainData.timeScale;

			canKeyboardControlMenusDuringGameplay = mainData.canKeyboardControlMenusDuringGameplay;

			if (mainData.toggleCursorState > 0)
			{
				toggleCursorOn = (mainData.toggleCursorState == 1) ? true : false;
			}
		}


		/**
		 * <summary>Updates a PlayerData class with its own variables that need saving.</summary>
		 * <param name = "playerData">The original PlayerData class</param>
		 * <returns>The updated PlayerData class</returns>
		 */
		public PlayerData SavePlayerData (PlayerData playerData)
		{
			playerData.playerUpLock = isUpLocked;
			playerData.playerDownLock = isDownLocked;
			playerData.playerLeftlock = isLeftLocked;
			playerData.playerRightLock = isRightLocked;
			playerData.playerRunLock = (int) runLock;
			playerData.playerFreeAimLock = IsFreeAimingLocked ();
			
			return playerData;
		}


		/**
		 * <summary>Updates its own variables from a PlayerData class.</summary>
		 * <param name = "playerData">The PlayerData class to load from</param>
		 */
		public void LoadPlayerData (PlayerData playerData)
		{
			SetUpLock (playerData.playerUpLock);
			isDownLocked = playerData.playerDownLock;
			isLeftLocked = playerData.playerLeftlock;
			isRightLocked = playerData.playerRightLock;
			runLock = (PlayerMoveLock) playerData.playerRunLock;
			SetFreeAimLock (playerData.playerFreeAimLock);
		}


		/**
		 * <summary>Controls an OnGUI-based Menu with keyboard or Controller inputs.</summary>
		 * <param name = "menu">The Menu to control</param>
		 */
		public virtual void InputControlMenu (Menu menu)
		{
			if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen || menu.menuSource != MenuSource.AdventureCreator)
			{
				return;
			}

			if (menu.IsOn () && menu.CanCurrentlyKeyboardControl ())
			{
				menu.AutoSelect ();

				// Menu option changing
				if (!KickStarter.playerMenus.IsCyclingInteractionMenu ())
				{
					if (KickStarter.stateHandler.gameState == GameState.DialogOptions ||
						KickStarter.stateHandler.gameState == GameState.Paused ||
						(KickStarter.stateHandler.IsInGameplay () && canKeyboardControlMenusDuringGameplay))
					{
						Vector2 rawInput = new Vector2 (InputGetAxisRaw ("Horizontal"), InputGetAxisRaw ("Vertical"));
						scrollingLocked = menu.GetNextSlot (rawInput, scrollingLocked);

						if (InputGetAxisRaw ("Vertical") < 0.05f && InputGetAxisRaw ("Vertical") > -0.05f && InputGetAxisRaw ("Horizontal") < 0.05f && InputGetAxisRaw ("Horizontal") > -0.05f)
						{
							scrollingLocked = false;
						}
					}
				}
			}
		}


		/**
		 * <summary>Ends the active Conversation.</summary>
		 */
		public void EndConversation ()
		{
			activeConversation = null;
		}


		/**
		 * <summary>Checks if a Conversation is currently active</summary>
		 * <param name = "alsoPendingOption">If True, then the method will return True if a Conversation is not active, but is in the delay gap between choosing an option and running it</param>
		 * <returns>True if a Conversation is currently active</returns>
		 */
		public bool IsInConversation (bool alsoPendingOption = false)
		{
			if (activeConversation != null)
			{
				return true;
			}
			if (pendingOptionConversation != null && alsoPendingOption)
			{
				return true;
			}
			return false;
		}


		/** A Conversation that has ended, but has yet to run the response */
		public Conversation PendingOptionConversation
		{
			get
			{
				return pendingOptionConversation;
			}
			set
			{
				pendingOptionConversation = value;
			}
		}


		protected virtual Vector2 LockedCursorPosition
		{
			get
			{
				return new Vector2 ( ACScreen.width / 2f, ACScreen.height / 2f);
			}
		}

	}
	
}