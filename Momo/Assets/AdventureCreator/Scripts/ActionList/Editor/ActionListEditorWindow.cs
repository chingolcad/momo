using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	public class ActionListEditorWindow : EditorWindow
	{

		public ActionListEditorWindowData windowData = new ActionListEditorWindowData ();

		private DragMode dragMode = DragMode.None;
		private enum DragMode { None, Node, Wire, Marquee, ScrollbarHorizontal, ScrollbarVertical };

		private bool canMarquee = true;
		private Rect marqueeRect = new Rect (0f, 0f, 0f, 0f);
		private bool marqueeShift = false;
		private bool isAutoArranging = false;
		private bool showProperties = false;
		private int focusActionIndex;

		private float zoom = 1f;
		private const float zoomMin = 0.15f;
		private const float zoomMax = 1f;
		
		private Action actionChanging = null;
		private bool resultType;
		private int multipleResultType;
		private int offsetChanging = 0;
		private int numActions = 0;

		private const int maxFavourites = 10;

		private Vector2 scrollPosition = Vector2.zero;
		private Vector2 menuPosition;
		
		private ActionsManager actionsManager;
		private static ActionListEditorWindow mainInstance;
		private Rect startLabelRect = new Rect (16, -2, 100, 20);
		private const string startLabel = "START";
		private Vector2 startNodeRect = new Vector2 (14, 14);
		private Vector2 socketSize = new Vector2 (16, 16);
		private int actionDragging = -1;
		private Vector2 marqueeStartPosition;
		private Vector2 scrollLimits;
		private Rect dragRectRelative;

		private bool hasDraggedWire;
		private const float dragMargin = 0.15f;

		private float scrollBarOffset;
		private Vector2 propertiesScroll;
		private const float propertiesBoxWidth = 360f;
		private const float scrollbarSelectedSizeFactor = 2.5f;


		[MenuItem ("Adventure Creator/Editors/ActionList Editor", false, 1)]
		private static void Init ()
		{
			ActionListEditorWindow window = CreateWindow ();
			window.Repaint ();
			window.Show ();
			window.titleContent.text = "ActionList Editor";
			window.windowData = new ActionListEditorWindowData ();
		}


		public static void OpenForActionList (ActionList _target)
		{
			if (_target.source == ActionListSource.AssetFile)
			{
				if (_target.assetFile != null)
				{
					Init (_target.assetFile);
				}
			}
			else
			{
				Init (_target);
			}
		}


		private static ActionListEditorWindow CreateWindow ()
		{
			if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().actionsManager != null && AdvGame.GetReferences ().actionsManager.allowMultipleActionListWindows == false)
			{
				return (ActionListEditorWindow) EditorWindow.GetWindow (typeof (ActionListEditorWindow));
			}
			else
			{
				return CreateInstance <ActionListEditorWindow>();
			}
		}


		public static void Init (ActionList actionList)
		{
			if (actionList.source == ActionListSource.AssetFile)
			{
				if (actionList.assetFile != null)
				{
					ActionListEditorWindow.Init (actionList.assetFile);
				}
				else
				{
					ACDebug.Log ("Cannot open ActionList Editor window, as no ActionList asset file has been assigned to " + actionList.gameObject.name + ".");
				}
			}
			else
			{
				ActionListEditorWindow window = CreateWindow ();
				window.AssignNewSource (new ActionListEditorWindowData (actionList));
			}
		}


		public static void Init (ActionListAsset actionListAsset)
		{
			ActionListEditorWindow window = CreateWindow ();
			ActionListEditorWindowData windowData = new ActionListEditorWindowData (actionListAsset);
			window.AssignNewSource (windowData);
		}


		public void AssignNewSource (ActionListEditorWindowData _data)
		{
			scrollPosition = Vector2.zero;
			zoom = 1f;
			showProperties = false;
			titleContent.text = "ActionList Editor";
			windowData = _data;
			Repaint ();
			Show ();
		}


		private void OnEnable ()
		{
			if (AdvGame.GetReferences ())
			{
				if (AdvGame.GetReferences ().actionsManager)
				{
					actionsManager = AdvGame.GetReferences ().actionsManager;
					AdventureCreator.RefreshActions ();
				}
				else
				{
					ACDebug.LogError ("An Actions Manager is required - please use the Game Editor window to create one.");
				}
			}
			else
			{
				ACDebug.LogError ("A References file is required - please use the Game Editor window to create one.");
			}
			
			UnmarkAll ();

			if (windowData != null && windowData.isLocked)
			{
				mainInstance = this;
			}
		}


		private void OnDisable ()
		{
			if (mainInstance == this)
			{
				mainInstance = null;
			}
		}


		public static ActionListEditorWindow MainInstance
		{
			get
			{
				return mainInstance;
			}
		}

		
		private void PanAndZoomWindow (Event e)
		{
			ActionListEditorScrollWheel scrollWheel = (AdvGame.GetReferences () && AdvGame.GetReferences ().actionsManager) ? AdvGame.GetReferences ().actionsManager.actionListEditorScrollWheel : ActionListEditorScrollWheel.PansWindow;
			bool invertPanning = (AdvGame.GetReferences () && AdvGame.GetReferences ().actionsManager) ? AdvGame.GetReferences ().actionsManager.invertPanning : false;
			float speedFactor = (AdvGame.GetReferences () && AdvGame.GetReferences ().actionsManager) ? AdvGame.GetReferences ().actionsManager.panSpeed : 1f;
			
			invertPanning = !invertPanning;

			if (dragMode == DragMode.Wire)
			{
				if (e.type == EventType.MouseDrag)
				{
					hasDraggedWire = true;
				}
				ApplyEdgePanning (e);
				return;
			}

			if (dragMode == DragMode.Marquee || dragMode == DragMode.ScrollbarHorizontal || dragMode == DragMode.ScrollbarVertical)
			{
				return;
			}

			if (e.alt)
			{
				scrollWheel = (scrollWheel == ActionListEditorScrollWheel.PansWindow)
							  ? ActionListEditorScrollWheel.ZoomsWindow
							  : ActionListEditorScrollWheel.PansWindow; 
			}

			if (scrollWheel == ActionListEditorScrollWheel.ZoomsWindow && e.type == EventType.ScrollWheel)
			{
				Vector2 originalMousePos = (e.mousePosition + ScrollPosition) / Zoom;
				
				float zoomDelta = -e.delta.y * speedFactor / 80.0f;
				Zoom += zoomDelta;

				ScrollPosition = originalMousePos * Zoom - e.mousePosition;

				UseEvent (e);
			}

			if (scrollWheel == ActionListEditorScrollWheel.PansWindow && e.type == EventType.ScrollWheel)
			{
				Vector2 delta = e.delta * speedFactor * 8f;
				if (invertPanning)
				{
					ScrollPosition += delta;
				}
				else
				{
					ScrollPosition -= delta;
				}

				UseEvent (e);
			}
			else if (e.type == EventType.MouseDrag && e.button == 2)
			{
				Vector2 delta = e.delta * speedFactor * 1.25f;
				if (invertPanning)
				{
					ScrollPosition += delta;
				}
				else
				{
					ScrollPosition -= delta;
				}

				UseEvent (e);
			}
			else if (e.type == EventType.KeyDown && !EditorGUIUtility.editingTextField)
			{
				if (e.keyCode == KeyCode.Home)
				{
					if (e.alt)
					{
						zoom = 1f;
					}
					else
					{
						ScrollPosition = Vector2.zero;
					}
					UseEvent (e);
				}
				else if (e.keyCode == KeyCode.PageUp)
				{
					if (e.alt)
					{
						Zoom += 0.2f;
					}
					else
					{
						ScrollPosition -= new Vector2 (0f, CanvasHeight);
					}
					UseEvent (e);
				}
				else if (e.keyCode == KeyCode.PageDown)
				{
					if (e.alt)
					{
						Zoom -= 0.2f;
					}
					else
					{
						ScrollPosition += new Vector2 (0f, CanvasHeight);
					}
					UseEvent (e);
				}
				/*else if (!Application.isPlaying && !EditorGUIUtility.editingTextField)
				{
					#if UNITY_EDITOR_OSX
					if (e.command)
					#else
					if (e.control)
					#endif
					{
						if (e.keyCode == KeyCode.C && NumActionsMarked > 0)
						{
							PerformEmptyCallBack ("Copy selected");
						}
						else if (e.keyCode == KeyCode.X && NumActionsMarked > 0)
						{
							PerformEmptyCallBack ("Cut selected");
						}
						else if (e.keyCode == KeyCode.V && AdvGame.copiedActions != null && AdvGame.copiedActions.Count > 0)
						{
							menuPosition = e.mousePosition + ScrollPosition;
							EmptyCallback ("Paste copied Action(s)");
						}
					}
				}*/
			}
		}
		

		private void DragNodes (Event e)
		{
			if (dragMode != DragMode.None && dragMode != DragMode.Node)
			{
				return;
			}
			//if (actionDragging >= 1) GUI.Box (new Rect (dragRectRelative.position + Actions[actionDragging].nodeRect.position - ScrollPosition, dragRectRelative.size), "", CustomStyles.IconMarquee);

			if (e.type == EventType.MouseDown)
			{
				if (e.button == 0)
				{
					for (int i = Actions.Count - 1; i >= 1; i--)
					{
						if (Actions[i] != null)
						{
							Rect rect = Actions[i].nodeRect;
							rect.position -= scrollPosition;

							if (rect.Contains (e.mousePosition))
							{
								BeginDrag (i);
								UseEvent (e);
								return;
							}
						}
					}
				}
			}
			else if (e.type == EventType.MouseDrag)
			{
				if (e.button == 0 && actionDragging >= 0)
				{
					UpdateDrag (e.delta);
					UseEvent (e);
				}
			}
			else if (e.rawType == EventType.MouseUp)
			{
				if (actionDragging >= 0)
				{
					actionDragging = -1;
					dragMode = DragMode.None;
					GUI.changed = true;
					UseEvent (e);
				}
			}
		}


		private void BeginDrag (int index)
		{
			dragMode = DragMode.Node;
			actionDragging = index;

			UpdateDrag (Vector2.zero, true);

			GUI.changed = true;
		}


		private void CalculateDragRectRelative ()
		{
			Rect dragRect = Actions[actionDragging].nodeRect;
			for (int i = 1; i < Actions.Count; i++)
			{
				if (Actions[i] != null && Actions[i].isMarked && i != actionDragging)
				{
					if (Actions[i].nodeRect.x < dragRect.x)
					{
						float leftShift = dragRect.x - Actions[i].nodeRect.x;
						dragRect.x -= leftShift;
						dragRect.width += leftShift;
					}

					if ((Actions[i].nodeRect.x + Actions[i].nodeRect.width) > (dragRect.x + dragRect.width))
					{
						dragRect.width = Actions[i].nodeRect.x + Actions[i].nodeRect.width - dragRect.x;
					}

					if (Actions[i].nodeRect.y < dragRect.y)
					{
						float topShift = dragRect.y - Actions[i].nodeRect.y;
						dragRect.y -= topShift;
						dragRect.height += topShift;
					}

					if ((Actions[i].nodeRect.y + Actions[i].nodeRect.height) > (dragRect.y + dragRect.height))
					{
						dragRect.height = Actions[i].nodeRect.y + Actions[i].nodeRect.height - dragRect.y;
					}
				}
			}

			float leftOverspill = ScrollPosition.x - dragRect.x;
			if (leftOverspill > 0f)
			{
				dragRect.x += leftOverspill;
				dragRect.width -= leftOverspill;
			}

			float rightOverspill = CanvasWidth + (ScrollPosition.x - dragRect.x - dragRect.width) * Zoom;
			if (rightOverspill < 0f)
			{
				dragRect.width += rightOverspill;
			}

			float topOverspill = ScrollPosition.y - dragRect.y;
			if (topOverspill > 0f)
			{
				dragRect.y += topOverspill;
				dragRect.height -= topOverspill;
			}

			float bottomOverspill = CanvasHeight + (ScrollPosition.y - dragRect.y - dragRect.height - 60f) * Zoom;
			if (bottomOverspill < 0f)
			{
				dragRect.height += bottomOverspill;
			}

			dragRectRelative = new Rect (dragRect.position - Actions[actionDragging].nodeRect.position, dragRect.size);
		}


		private void UpdateDrag (Vector2 delta, bool forceUpdate = false)
		{
			// Limit hard edges
			if ((delta.x + Actions[actionDragging].nodeRect.x + dragRectRelative.x) < 1f)
			{
				delta.x = 0f;
			}
			if ((delta.y + Actions[actionDragging].nodeRect.y + dragRectRelative.y) < 15f)
			{
				delta.y = 0f;
			}
			if ((delta.x + Actions[actionDragging].nodeRect.x + Actions[actionDragging].nodeRect.width + dragRectRelative.x) > (scrollLimits.x+ CanvasWidth) / zoom)
			{
				Debug.Log ("X: " + (delta.x + Actions[actionDragging].nodeRect.x + Actions[actionDragging].nodeRect.width + dragRectRelative.x) + ", ScrollLimits: " + scrollLimits.x + ", Zoom: " + zoom + ", CW: " + CanvasWidth + ", = " + (scrollLimits.x + CanvasWidth) / zoom);
				delta.x = 0f;
			}
			if ((delta.y + Actions[actionDragging].nodeRect.y + Actions[actionDragging].nodeRect.height + dragRectRelative.y) > (scrollLimits.y + CanvasHeight) / zoom)
			{
				delta.y = 0f;
			}

			if (delta.sqrMagnitude > 0f || forceUpdate)
			{
				CalculateDragRectRelative ();
			}

			for (int i=1; i< Actions.Count; i++)
			{
				if (Actions[i] != null && (Actions[i].isMarked || i == actionDragging))
				{
					Actions[i].nodeRect.position += delta;
					GUI.changed = true;
					
					if (i == actionDragging)
					{
						Vector2 overspill = Vector2.zero;
						
						if (delta.x > 0f || forceUpdate)
						{
							float rightNodeEdge = Actions[i].nodeRect.x + dragRectRelative.x + dragRectRelative.width;
							float rightSpill = (rightNodeEdge - ScrollPosition.x) * Zoom - CanvasWidth;
							if (rightSpill > 0f) overspill.x = rightSpill;
						}
						if (delta.x < 0f || forceUpdate)
						{
							float leftNodeEdge = Actions[i].nodeRect.x + dragRectRelative.x;
							float leftSpill = (leftNodeEdge - ScrollPosition.x) * Zoom;
							if (leftSpill < 0f) overspill.x = leftSpill;
						}

						if (delta.y > 0f || forceUpdate)
						{
							float bottomNodeEdge = Actions[i].nodeRect.y + dragRectRelative.y + dragRectRelative.height + 60f;
							float topSpill = (bottomNodeEdge - ScrollPosition.y) * Zoom - CanvasHeight;
							if (topSpill > 0f) overspill.y = topSpill;
						}
						if (delta.y < 0f || forceUpdate)
						{
							float topNodeEdge = Actions[i].nodeRect.y + dragRectRelative.y;
							float bottomSpill = (topNodeEdge - ScrollPosition.y) * Zoom;
							if (bottomSpill < 0f) overspill.y = bottomSpill;
						}
						
						ScrollPosition += overspill / Zoom;
					}
				}
			}			
		}


		private void ApplyEdgePanning (Event e)
		{
			bool autoPanNearWindowEdge = (AdvGame.GetReferences () && AdvGame.GetReferences ().actionsManager) ? AdvGame.GetReferences ().actionsManager.autoPanNearWindowEdge : false;
			if (!autoPanNearWindowEdge)
			{
				return;
			}

			if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
			{
				float panningSpeed = (AdvGame.GetReferences () && AdvGame.GetReferences ().actionsManager) ? AdvGame.GetReferences ().actionsManager.panSpeed : 1f;
				float maxSpeed = 10f * panningSpeed;

				Vector2 edgeScroll = Vector2.zero;

				float leftMargin = CanvasWidth * dragMargin;
				float rightMargin = CanvasWidth * (1f - dragMargin);

				if (e.mousePosition.x > rightMargin)
				{
					edgeScroll.x = Mathf.Min (e.mousePosition.x - rightMargin, maxSpeed);
				}
				else if (e.mousePosition.x < leftMargin)
				{
					edgeScroll.x = Mathf.Max (e.mousePosition.x - leftMargin, -maxSpeed);
				}

				float topMargin = CanvasHeight * dragMargin;
				float bottomMargin = CanvasHeight * (1f - dragMargin);

				if (e.mousePosition.y + 20f > bottomMargin)
				{
					edgeScroll.y = Mathf.Min (e.mousePosition.y + 20f - bottomMargin, maxSpeed);
				}
				else if (e.mousePosition.y - 20f < topMargin)
				{
					edgeScroll.y = Mathf.Max (e.mousePosition.y - 20f - topMargin, -maxSpeed);
				}

				ScrollPosition += edgeScroll;
			}
		}


		private void DrawScrollbars (Event e)
		{
			float scrollbarWidth = 12f;
			float scrollbarMargin = 20f;

			Vector2 drawArea = new Vector2 (position.width - ((showProperties) ? propertiesBoxWidth - 7f : 0f), position.height - 40f);
			Vector2 maxScrollbarSize = new Vector2 (drawArea.x - 2f * scrollbarMargin, drawArea.y - 2f * scrollbarMargin);

			// Horizontal
			if (CanDrawHorizontalScrollbar)
			{
				float xScrollSize = CanvasWidth / (CanvasWidth + scrollLimits.x);
				float xValue = ScrollPosition.x / scrollLimits.x * Zoom;
				float xFreedom = maxScrollbarSize.x - (xScrollSize * maxScrollbarSize.x);

				Vector2 horizontalLeft = new Vector2 (xValue * xFreedom + scrollbarMargin, drawArea.y + 8f);
				Vector2 horizontalSize = new Vector2 (xScrollSize * maxScrollbarSize.x, scrollbarWidth / 2f);
				if (horizontalSize.x < horizontalSize.y) horizontalSize.x = horizontalSize.y;

				Rect drawnHorizontalRect = new Rect (horizontalLeft, horizontalSize);

				Rect horizontalRect = drawnHorizontalRect;
				horizontalRect.size = new Vector2 (horizontalSize.x, horizontalSize.y * scrollbarSelectedSizeFactor);
				horizontalRect.center = drawnHorizontalRect.center;

				GUI.Box (drawnHorizontalRect, string.Empty, (dragMode == DragMode.ScrollbarHorizontal) ? CustomStyles.ToolbarInverted : CustomStyles.Toolbar);

				if (dragMode == DragMode.None && e.type == EventType.MouseDown)
				{
					if (e.button == 0)
					{
						if (horizontalRect.Contains (e.mousePosition))
						{
							dragMode = DragMode.ScrollbarHorizontal;
							scrollBarOffset = e.mousePosition.x - horizontalLeft.x;
							UseEvent (e);
							return;
						}
						else if (e.mousePosition.y > horizontalRect.y && e.mousePosition.y < horizontalRect.yMax)
						{
							if (e.mousePosition.x < horizontalRect.xMin)
							{
								// Left
								float offset = horizontalRect.width/ 2f;
								if ((e.mousePosition.x - offset - scrollbarMargin) < 0f)
								{
									offset = e.mousePosition.x - scrollbarMargin;
								}

								xValue = (e.mousePosition.x - offset - scrollbarMargin) / xFreedom;
								ScrollPosition = new Vector2 (xValue / Zoom * scrollLimits.x, ScrollPosition.y);

								dragMode = DragMode.ScrollbarHorizontal;
								scrollBarOffset = offset;
								UseEvent (e);
								Repaint ();
								return;
							}
							else if (e.mousePosition.x > horizontalRect.xMax)
							{
								// Right
								float offset = horizontalRect.width / 2f;
								if ((e.mousePosition.x + offset) > CanvasWidth)
								{
									offset = CanvasWidth - e.mousePosition.x;
								}

								xValue = (e.mousePosition.x - horizontalRect.width + offset - scrollbarMargin) / xFreedom;
								ScrollPosition = new Vector2 (xValue / Zoom * scrollLimits.x, ScrollPosition.y);

								dragMode = DragMode.ScrollbarHorizontal;
								scrollBarOffset = horizontalRect.width - offset;
								UseEvent (e);
								Repaint ();
								return;
							}
						}
					}
				}
				else if (e.type == EventType.MouseDrag && e.button == 0)
				{
					if (dragMode == DragMode.ScrollbarHorizontal)
					{
						float newHorizontalLeft = e.mousePosition.x - scrollBarOffset;
						float newXValue = (newHorizontalLeft - scrollbarMargin) / xFreedom;
						float newScrollPosition = newXValue / Zoom * scrollLimits.x;

						ScrollPosition = new Vector2 (newScrollPosition, ScrollPosition.y);
						UseEvent (e);
					}
				}
				else if (e.rawType == EventType.MouseUp)
				{
					if (dragMode == DragMode.ScrollbarHorizontal)
					{
						dragMode = DragMode.None;
						UseEvent (e);
					}
				}
			}

			// Vertical
			if (CanDrawVerticalScrollbar)
			{
				float yScrollSize = CanvasHeight / (CanvasHeight + scrollLimits.y);
				float yValue = ScrollPosition.y / scrollLimits.y * Zoom;
				float yFreedom = maxScrollbarSize.y - (yScrollSize * maxScrollbarSize.y);

				Vector2 verticalTop = new Vector2 (drawArea.x - 14f, yValue * yFreedom + scrollbarMargin + scrollbarMargin);
				Vector2 verticalSize = new Vector2 (scrollbarWidth / 2f, yScrollSize * maxScrollbarSize.y);
				if (verticalSize.y < verticalSize.x) verticalSize.y = verticalSize.x;

				Rect drawnVerticalRect = new Rect (verticalTop, verticalSize);
				Rect verticalRect = drawnVerticalRect;

				verticalRect.size = new Vector2 (verticalSize.x * scrollbarSelectedSizeFactor, verticalSize.y);
				verticalRect.center = drawnVerticalRect.center;

				GUI.Box (drawnVerticalRect, string.Empty, (dragMode == DragMode.ScrollbarVertical) ? CustomStyles.ToolbarInverted : CustomStyles.Toolbar);

				if (dragMode == DragMode.None && e.type == EventType.MouseDown)
				{
					if (e.button == 0)
					{
						if (verticalRect.Contains (e.mousePosition))
						{
							dragMode = DragMode.ScrollbarVertical;
							scrollBarOffset = e.mousePosition.y - verticalTop.y;
							UseEvent (e);
							return;
						}
						else if (e.mousePosition.x > verticalRect.x && e.mousePosition.x < verticalRect.xMax)
						{
							if (e.mousePosition.y < verticalRect.yMin)
							{
								// Above
								float offset = verticalRect.height / 2f;
								if ((e.mousePosition.y - offset) < (scrollbarMargin + scrollbarMargin))
								{
									offset = e.mousePosition.y - scrollbarMargin - scrollbarMargin;
								}

								yValue = (e.mousePosition.y - offset - scrollbarMargin - scrollbarMargin) / yFreedom;
								ScrollPosition = new Vector2 (ScrollPosition.x, yValue / Zoom * scrollLimits.y);

								dragMode = DragMode.ScrollbarVertical;
								scrollBarOffset = offset;
								UseEvent (e);
								Repaint ();
								return;
							}
							else if (e.mousePosition.y > verticalRect.yMax)
							{
								// Below
								float offset = verticalRect.height / 2f;
								if ((e.mousePosition.y + offset) > CanvasHeight)
								{
									offset = CanvasHeight - e.mousePosition.y;
								}

								yValue = (e.mousePosition.y - verticalRect.height + offset - scrollbarMargin - scrollbarMargin) / yFreedom;
								ScrollPosition = new Vector2 (ScrollPosition.x, yValue / Zoom * scrollLimits.y);

								dragMode = DragMode.ScrollbarVertical;
								scrollBarOffset = verticalRect.height - offset;
								UseEvent (e);
								Repaint ();
								return;
							}
						}
					}
				}
				else if (e.type == EventType.MouseDrag && e.button == 0)
				{
					if (dragMode == DragMode.ScrollbarVertical)
					{
						float newVerticalTop = e.mousePosition.y - scrollBarOffset - 20f;
						float newYValue = (newVerticalTop - scrollbarMargin) / yFreedom;
						float newScrollPosition = newYValue / Zoom * scrollLimits.y;

						ScrollPosition = new Vector2 (ScrollPosition.x, newScrollPosition);
						UseEvent (e);
					}
				}
				else if (e.rawType == EventType.MouseUp)
				{
					if (dragMode == DragMode.ScrollbarVertical)
					{
						dragMode = DragMode.None;
						UseEvent (e);
					}
				}
			}
		}


		private void DrawMarquee (bool isAsset, Event e)
		{
			if (dragMode != DragMode.None && dragMode != DragMode.Marquee)
			{
				return;
			}

			if (!canMarquee)
			{
				dragMode = DragMode.None;
				return;
			}

			if (dragMode == DragMode.Marquee)
			{
				ApplyEdgePanning (e);
			}

			if (e.type == EventType.MouseDown && e.button == 0 && dragMode == DragMode.None)
			{
				if (e.mousePosition.y > 24 && e.mousePosition.y < CanvasHeight - 22 && e.mousePosition.x < CanvasWidth)
				{
					marqueeStartPosition = e.mousePosition + ScrollPosition * Zoom;
					dragMode = DragMode.Marquee;
					marqueeShift = false;

					marqueeRect = new Rect (marqueeStartPosition.x, marqueeStartPosition.y, 0f, 0f);
					marqueeRect.position -= ScrollPosition * Zoom;

					e.Use ();
					GUI.changed = true;
				}
			}
			else if (e.rawType == EventType.MouseUp)
			{
				if (dragMode == DragMode.Marquee)
				{
					MarqueeSelect (marqueeShift);
					e.Use ();
					GUI.changed = true;
					dragMode = DragMode.None;
				}
			}
			else if (e.type == EventType.MouseDrag)
			{
				Vector2 marqueeOffset = e.mousePosition + ScrollPosition * Zoom;

				marqueeRect = new Rect (Mathf.Min (marqueeOffset.x, marqueeStartPosition.x),
									Mathf.Min (marqueeOffset.y, marqueeStartPosition.y),
									Mathf.Abs (marqueeOffset.x - marqueeStartPosition.x),
									Mathf.Abs (marqueeOffset.y - marqueeStartPosition.y));

				marqueeRect.position -= ScrollPosition * Zoom;
			}

			if (dragMode == DragMode.Marquee)
			{
				if (e.shift)
				{
					marqueeShift = true;
				}

				if (marqueeRect.size.x > 0f && marqueeRect.size.y > 0f)
				{
					GUI.Box (marqueeRect, string.Empty, CustomStyles.IconMarquee);
					GUI.changed = true;
				}
			}
		}


		private Rect ConvertMarqueeRect ()
		{
			Rect convertedRect = marqueeRect;
   
			if (convertedRect.width < 0f)
			{
				convertedRect.x += convertedRect.width;
				convertedRect.width *= -1f;
			}
			if (convertedRect.height < 0f)
			{
				convertedRect.y += convertedRect.height;
				convertedRect.height *= -1f;
			}

			convertedRect.y -= 24f;

			// Correct for zooming
			convertedRect.x /= zoom;
			convertedRect.y /= zoom;
			convertedRect.width /= zoom;
			convertedRect.height /= zoom;

			// Correct for panning
			convertedRect.position += ScrollPosition;

			return convertedRect;
		}
		
		
		private void MarqueeSelect (bool isCumulative)
		{
			Rect rect = ConvertMarqueeRect ();

			if (!isCumulative)
			{
				UnmarkAll ();
			}

			foreach (Action action in Actions)
			{
				if (action == null) continue;

				if (action.nodeRect.Overlaps (rect) || rect.Overlaps (action.nodeRect))
				{
					action.isMarked = true;
				}
			}
		}


		private void UpdateScrollLimits ()
		{
			scrollLimits = Vector2.zero;
			foreach (Action action in Actions)
			{
				if (action != null)
				{
					float rightMax = action.nodeRect.x + action.nodeRect.width + 500f;
					rightMax *= Zoom;
					if (scrollLimits.x < rightMax) scrollLimits.x = rightMax;

					float bottomMax = action.nodeRect.y + action.nodeRect.height + 500f;
					bottomMax *= Zoom;
					if (scrollLimits.y < bottomMax) scrollLimits.y = bottomMax;
				}
			}

			//GUI.Box (new Rect (new Vector2 (0f, 24f) - ScrollPosition * Zoom, scrollLimits), "", CustomStyles.IconMarquee);
			scrollLimits.x = Mathf.Max (0f, scrollLimits.x - (CanvasWidth - 4f));
			scrollLimits.y = Mathf.Max (0f, scrollLimits.y - (CanvasHeight - 50f));
		}


		private void OnGUI ()
		{
			if (isAutoArranging)
			{
				return;
			}

			if (!windowData.isLocked)
			{
				if (Selection.activeObject && Selection.activeObject is ActionListAsset)
				{
					windowData.targetAsset = (ActionListAsset) Selection.activeObject;
					windowData.target = null;
				}
				else if (Selection.activeGameObject && Selection.activeGameObject.GetComponent <ActionList>())
				{
					windowData.targetAsset = null;
					windowData.target = Selection.activeGameObject.GetComponent<ActionList>();
				}
			}

			if (windowData.targetAsset != null)
			{
				UpdateScrollLimits ();
				ActionListAssetEditor.ResetList (windowData.targetAsset);
				
				if (showProperties)
				{
					PropertiesGUI (true);
				}

				if (CanvasWidth > 0f)
				{
					DrawGrid ();
					PanAndZoomWindow (Event.current);
					NodesGUI (true, Event.current);
					DrawMarquee (true, Event.current);
				}
				
				TopToolbarGUI (true);
				BottomToolbarGUI (true);

				if (GUI.changed)
				{
					EditorUtility.SetDirty (windowData.targetAsset);
				}
			}
			else if (windowData.target != null)
			{
				UpdateScrollLimits ();
				ActionListEditor.ResetList (windowData.target);

				if (showProperties)
				{
					PropertiesGUI (false);
				}

				if (windowData.target.source != ActionListSource.AssetFile)
				{
					if (CanvasWidth > 0f)
					{
						DrawGrid ();
						PanAndZoomWindow (Event.current);
						NodesGUI (false, Event.current);
						DrawMarquee (false, Event.current);
					}
					TopToolbarGUI (false);
				}

				BottomToolbarGUI (false);

				if (GUI.changed)
				{
					UnityVersionHandler.CustomSetDirty (windowData.target);
				}
			}
			else
			{
				TopToolbarGUI (false);
				DrawEmptyNotice ();
			}

			if (GUI.changed) Repaint ();
		}


		private void BottomToolbarGUI (bool isAsset)
		{
			bool noList = false;

			if ((isAsset && windowData.targetAsset == null) || (!isAsset && windowData.target == null) || (!isAsset && !windowData.target.gameObject.activeInHierarchy))
			{
				noList = true;
			}

			GUILayout.BeginArea (new Rect (0, position.height - 24, position.width, 24), CustomStyles.Toolbar);
			string labelText;
			if (noList)
			{
				labelText = "No ActionList selected";
			}
			else if (isAsset)
			{
				labelText = "Editing ActionList asset: " + windowData.targetAsset.name;
			}
			else
			{
				labelText = "Editing " + windowData.target.GetType ().ToString ().Replace ("AC.", "") + ": " + windowData.target.gameObject.name;
			}

			if (GUI.Button (new Rect (10, 0, 18, 18), "", (windowData.isLocked) ? CustomStyles.IconLock : CustomStyles.IconUnlock))
			{
				windowData.isLocked = !windowData.isLocked;
			}

			GUI.Label (new Rect (30,2,50,20), labelText, CustomStyles.LabelToolbar);
			if ((isAsset && windowData.targetAsset != null) || (!isAsset && windowData.target != null))
			{
				if (GUI.Button (new Rect (position.width - 200, 0, 100, 20), "Ping object", EditorStyles.miniButtonLeft))
				{
					if (windowData.targetAsset != null)
					{
						EditorGUIUtility.PingObject (windowData.targetAsset);
					}
					else if (windowData.target != null)
					{
						EditorGUIUtility.PingObject (windowData.target.gameObject);
					}
				}

				string propertiesLabel = (showProperties) ? "Hide properties" : "Show properties";
				if (GUI.Button (new Rect (position.width - 100, 0, 100, 20), propertiesLabel, EditorStyles.miniButtonRight))
				{
					showProperties = !showProperties;
				}
			}

			GUILayout.EndArea ();
		}


		private void TopToolbarGUI (bool isAsset)
		{
			bool noList = false;
			bool showLabel = false;
			float buttonWidth = 20f;
			if (position.width > 480)
			{
				buttonWidth = 60f;
				showLabel = true;
			}

			if ((isAsset && windowData.targetAsset == null) || (!isAsset && windowData.target == null) || (!isAsset && !windowData.target.gameObject.activeInHierarchy))
			{
				noList = true;
			}

			GUILayout.BeginArea (new Rect (0,0,position.width,24), CustomStyles.Toolbar);

			float midX = position.width * 0.4f;

			if (noList)
			{
				GUI.enabled = false;
			}

			if (ToolbarButton (10f, buttonWidth, showLabel, "Insert", CustomStyles.IconInsert))
			{
				menuPosition = new Vector2 (70f, 30f) + ScrollPosition;
				PerformEmptyCallBack ("Add new Action");
			}

			if (!noList && NumActionsMarked > 0)
			{
				GUI.enabled = true;
			}
			else
			{
				GUI.enabled = false;
			}
			
			if (ToolbarButton (buttonWidth+10f, buttonWidth, showLabel, "Delete", CustomStyles.IconDelete))
			{
				PerformEmptyCallBack ("Delete selected");
			}

			if (!noList)
			{
				GUI.enabled = true;
			}

			if (ToolbarButton (position.width-(buttonWidth*3f), buttonWidth*1.5f, showLabel, "Auto-arrange", CustomStyles.IconAutoArrange))
			{
				//AutoArrange (isAsset);
				PerformEmptyCallBack ("Auto-arrange");
			}

			GUI.enabled = (noList) ? false : Application.isPlaying;

			if (ToolbarButton (position.width-buttonWidth, buttonWidth, showLabel, "Run", CustomStyles.IconPlay))
			{
				if (isAsset)
				{
					AdvGame.RunActionListAsset (windowData.targetAsset);
				}
				else
				{
					windowData.target.Interact ();
				}
			}

			if (!noList && NumActionsMarked > 0 && !Application.isPlaying)
			{
				GUI.enabled = true;
			}
			else
			{
				GUI.enabled = false;
			}

			if (ToolbarButton (midX - buttonWidth, buttonWidth, showLabel, "Cut", CustomStyles.IconCut))
			{
				PerformEmptyCallBack ("Cut selected");
			}

			if (ToolbarButton (midX, buttonWidth, showLabel, "Copy", CustomStyles.IconCopy))
			{
				PerformEmptyCallBack ("Copy selected");
			}

			if (!noList && AdvGame.copiedActions != null && AdvGame.copiedActions.Count > 0)
			{
				GUI.enabled = true;
			}
			else
			{
				GUI.enabled = false;
			}

			if (ToolbarButton (midX + buttonWidth, buttonWidth, showLabel, "Paste", CustomStyles.IconPaste))
			{
				menuPosition = new Vector2 (70f, 30f) + ScrollPosition;
				EmptyCallback ("Paste copied Action(s)");
			}

			GUI.enabled = true;

			GUILayout.EndArea ();
		}


		private void PropertiesGUI (bool isAsset)
		{
			GUILayout.BeginArea (new Rect (position.width - propertiesBoxWidth, 22, propertiesBoxWidth, position.height - 40));
			propertiesScroll = GUILayout.BeginScrollView (propertiesScroll);
			if (isAsset && windowData.targetAsset != null)
			{
				ActionListAssetEditor.ShowPropertiesGUI (windowData.targetAsset);
			}
			else if (!isAsset && windowData.target != null)
			{
				if (windowData.target is Cutscene)
				{
					Cutscene cutscene = (Cutscene) windowData.target;
					CutsceneEditor.PropertiesGUI (cutscene);
				}
				else if (windowData.target is AC_Trigger)
				{
					AC_Trigger cutscene = (AC_Trigger) windowData.target;
					AC_TriggerEditor.PropertiesGUI (cutscene);
				}
				else if (windowData.target is DialogueOption)
				{
					DialogueOption cutscene = (DialogueOption) windowData.target;
					DialogueOptionEditor.PropertiesGUI (cutscene);
				}
				else if (windowData.target is Interaction)
				{
					Interaction cutscene = (Interaction) windowData.target;
					InteractionEditor.PropertiesGUI (cutscene);
				}
			}
			GUILayout.EndScrollView ();
			GUILayout.EndArea ();
		}


		private bool ToolbarButton (float startX, float width, bool showLabel, string label, GUIStyle guiStyle)
		{
			if (showLabel)
			{
				return GUI.Button (new Rect (startX,2,width,20), label, guiStyle);
			}
			return GUI.Button (new Rect (startX,2,20,20), "", guiStyle);
		}
		

		private void NodeWindow (int i)
		{
			if (actionsManager == null)
			{
				OnEnable ();
			}
			if (actionsManager == null)
			{
				return;
			}
			
			bool isAsset = false;
			Action _action = Actions[i];
			List<ActionParameter> parameters = null;
			
			if (windowData.targetAsset != null)
			{
				isAsset = _action.isAssetFile = true;
				if (windowData.targetAsset.useParameters)
				{
					parameters = windowData.targetAsset.GetParameters ();
				}
			}
			else
			{
				if (!(windowData.target is RuntimeActionList && Application.isPlaying))
				{
					_action.isAssetFile = false;
				}

				if (windowData.target.useParameters)
				{
					parameters = windowData.target.parameters;
				}
			}

			if (_action.showComment)
			{
				Color _color = GUI.color;
				GUI.color = new Color (1f, 1f, 0.5f, 1f);
				EditorStyles.textField.wordWrap = true;
				_action.comment = EditorGUILayout.TextArea (_action.comment, GUILayout.MaxWidth (280f));
				GUI.color = _color;
				EditorGUILayout.Space ();
			}

			if (!actionsManager.DoesActionExist (_action.GetType ().ToString ()))
			{
				EditorGUILayout.HelpBox ("This Action type is not listed in the Actions Manager", MessageType.Warning);
			}
			else
			{
				int typeIndex = KickStarter.actionsManager.GetActionTypeIndex (_action);
				int newTypeIndex = ActionListEditor.ShowTypePopup (_action, typeIndex);

				if (newTypeIndex >= 0)
				{
					// Rebuild constructor if Subclass and type string do not match
					Vector2 currentPosition = new Vector2 (_action.nodeRect.x, _action.nodeRect.y);
					
					// Store "After running data" to transfer over
					ActionEnd _end = new ActionEnd ();
					_end.resultAction = _action.endAction;
					_end.skipAction = _action.skipAction;
					_end.linkedAsset = _action.linkedAsset;
					_end.linkedCutscene = _action.linkedCutscene;
					
					if (isAsset)
					{
						Undo.RecordObject (windowData.targetAsset, "Change Action type");
						
						Action newAction = ActionListAssetEditor.RebuildAction (_action, newTypeIndex, windowData.targetAsset, i, _end);
						newAction.nodeRect.position = currentPosition;

						ActionListAssetEditor.DeleteAction (_action, windowData.targetAsset);
					}
					else
					{
						Undo.RecordObject (windowData.target, "Change Action type");
						
						Action newAction = ActionListEditor.RebuildAction (_action, newTypeIndex, windowData.target, i, _end);
						newAction.nodeRect.position = currentPosition;

						ActionListEditor.DeleteAction (_action, windowData.target);
					}
				}

				_action.ShowGUI (parameters);
			}
			
			if (_action.endAction == ResultAction.Skip || _action.numSockets == 2 || _action is ActionCheckMultiple || _action is ActionParallel)
			{
				_action.SkipActionGUI (Actions, true);
			}

			_action.isDisplayed = EditorGUI.Foldout (new Rect (10,1,20,16), _action.isDisplayed, string.Empty);
			
			if (GUI.Button (new Rect(273,3,16,16), " ", CustomStyles.IconCogNode))
			{
				CreateNodeMenu (i, _action);
			}
			
			if (i == 0)
			{
				_action.nodeRect.position = startNodeRect;
			}
		}
		
		
		private void EmptyNodeWindow (int i)
		{
			Action _action = Actions[i];
			bool isAsset = (windowData.targetAsset != null);

			if (_action.endAction == ResultAction.Skip || _action.numSockets == 2 || _action is ActionCheckMultiple || _action is ActionParallel)
			{
				_action.SkipActionGUI (Actions, !isAsset);
			}
			
			_action.isDisplayed = EditorGUI.Foldout (new Rect (10,1,20,16), _action.isDisplayed, "");

			if (_action.showComment)
			{
				Color _color = GUI.color;
				GUI.color = new Color (1f, 1f, 0.5f, 1f);
				EditorStyles.textField.wordWrap = true;
				_action.comment = EditorGUILayout.TextArea (_action.comment, GUILayout.MaxWidth (280f));
				GUI.color = _color;
			}

			if (GUI.Button (new Rect(273,3,16,16), " ", CustomStyles.IconCogNode))
			{
				CreateNodeMenu (i, _action);
			}

			if (i == 0)
			{
				_action.nodeRect.position = startNodeRect;
			}
		}


		private bool IsActionInView (Action action)
		{
			float height = action.nodeRect.height;

			if (isAutoArranging || action.isMarked)
			{
				return true;
			}
			if (action.nodeRect.y > ScrollPosition.y + CanvasHeight / zoom)
			{
				return false;
			}
			if (action.nodeRect.y + height < ScrollPosition.y)
			{
				return false;
			}
			if (action.nodeRect.x > ScrollPosition.x + CanvasWidth / zoom)
			{
				return false;
			}
			if (action.nodeRect.x + action.nodeRect.width < ScrollPosition.x)
			{
				return false;
			}
			return true;
		}
		
		
		private void LimitWindow (Action action)
		{
			bool update = false;

			if (action.nodeRect.x < 1)
			{
				action.nodeRect.x = 1;
				update = true;
			}
			
			if (action.nodeRect.y < 14)
			{
				action.nodeRect.y = 14;
				update = true;
			}

			if (update)
			{
				GUI.changed = true;
				Repaint ();
			}
		}
		
		
		private void NodesGUI (bool isAsset, Event e)
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().actionsManager)
			{
				actionsManager = AdvGame.GetReferences ().actionsManager;
			}
			if (actionsManager == null)
			{
				GUILayout.Space (30f);
				EditorGUILayout.HelpBox ("An Actions Manager asset file must be assigned in the Game Editor Window", MessageType.Warning);
				OnEnable ();
				return;
			}
			if (!isAsset && UnityVersionHandler.IsPrefabFile (windowData.target.gameObject))
			{
				GUILayout.Space (30f);
				EditorGUILayout.HelpBox ("Scene-based Actions can not live in prefabs - use ActionList assets instead.", MessageType.Info);
				return;
			}
			if (!isAsset && windowData.target != null)
			{
				if (windowData.target.source == ActionListSource.AssetFile)
				{
					GUILayout.Space (30f);
					EditorGUILayout.HelpBox ("Cannot view Actions, since this object references an Asset file.", MessageType.Info);
					return;
				}
			}

			bool loseConnection = false;
			
			if (dragMode == DragMode.Wire)
			{
				if (e.rawType == EventType.MouseUp)
				{
					loseConnection = true;
				}
			}
			
			numActions = Actions.Count;
			if (numActions < 1)
			{
				numActions = 1;
				if (isAsset)
				{
					ActionListAssetEditor.AddAction (ActionsManager.GetDefaultAction (), -1, windowData.targetAsset);
				}
				else
				{
					ActionListEditor.AddAction (ActionsManager.GetDefaultAction (), -1, windowData.target);
				}
			}
			numActions = Actions.Count;

			DrawScrollbars (e);

			EditorZoomArea.Begin (zoom, new Rect (0, 24, CanvasWidth / zoom, CanvasHeight / zoom - 44));
			GUI.BeginGroup (new Rect (0, 0, CanvasWidth / zoom, CanvasHeight / zoom - 44));

			BeginWindows ();

			canMarquee = true;

			DragNodes (e);

			for (int i=0; i<numActions; i++)
			{
				FixConnections (i, isAsset);
				
				Action _action = Actions[i];

				if (_action == null) continue;

				if (i == 0)
				{
					GUI.Label (new Rect (startLabelRect.position - ScrollPosition, startLabelRect.size), startLabel, Resource.NodeSkin.label);
					if (Mathf.Approximately (_action.nodeRect.x, 50) && Mathf.Approximately (_action.nodeRect.y, 50))
					{
						// Upgrade
						_action.nodeRect.x = _action.nodeRect.y = 14;
						MarkAll ();
						PerformEmptyCallBack ("Expand selected");
						UnmarkAll ();
					}
				}

				if (IsActionInView (_action))
				{
					GUIStyle nodeStyle = CustomStyles.NodeNormal;

					Color originalColor = GUI.color;
					GUI.color = (_action.overrideColor != Color.white)
								? _action.overrideColor
								: actionsManager.GetActionTypeColor (_action);

					//Rect marqueeOffsetRect = marqueeRect;
					//marqueeOffsetRect.position += ScrollPosition + new Vector2 (0f, -24f);
					
					if (_action.isRunning && Application.isPlaying)
					{
						nodeStyle = CustomStyles.NodeRunning;
					}
					else if (_action.isMarked ||
							(dragMode == DragMode.Wire && _action.nodeRect.Contains (e.mousePosition + ScrollPosition)) ||
							(dragMode == DragMode.Marquee && (_action.nodeRect.Overlaps (ConvertMarqueeRect ()) || ConvertMarqueeRect ().Overlaps (_action.nodeRect))))
					{
						nodeStyle = CustomStyles.NodeSelected;
					}
					else if (_action.isBreakPoint)
					{
						nodeStyle = CustomStyles.NodeBreakpoint;
					}
					else if (!_action.isEnabled)
					{
						nodeStyle = CustomStyles.NodeDisabled;
					}

					_action.AssignParentList (windowData.target);

					if (_action.nodeRect.width == 0)
					{
						_action.nodeRect.width = 300;
					}

					string label = "(" + i + ") " + actionsManager.GetActionTypeLabel (_action, false);
					if (!_action.isDisplayed)
					{
						_action.nodeRect.height = 21f;

						if (_action.showComment)
						{
							GUIContent content = new GUIContent (_action.comment);
							float commentHeight = nodeStyle.CalcHeight (content, _action.nodeRect.width);
							_action.nodeRect.height += commentHeight + 5;
						}

						string extraLabel = _action.SetLabel ();
						if (!string.IsNullOrEmpty (extraLabel))
						{
							extraLabel = " (" + extraLabel + ")";
						}
						if (_action is ActionComment)
						{
							if (extraLabel.Length > 40)
							{
								extraLabel = extraLabel.Substring (0, 40) + "..)";
							}
							label = extraLabel;
						}
						else
						{
							if (extraLabel.Length > 15)
							{
								extraLabel = extraLabel.Substring (0, 15) + "..)";
							}
							label += extraLabel;
						}

						Rect tempRect = _action.nodeRect;
						tempRect.position -= ScrollPosition;
						tempRect = GUI.Window (i, tempRect, EmptyNodeWindow, label, nodeStyle);
						tempRect.position += ScrollPosition;

						_action.nodeRect.position = tempRect.position;
					}
					else
					{
						Rect tempRect = _action.nodeRect;
						bool adjustHeight = (e.type != EventType.Repaint);

						if (adjustHeight)
						{
							tempRect.height = 20f;
						}

						tempRect.position -= ScrollPosition;
						tempRect = GUILayout.Window (i, tempRect, NodeWindow, label, nodeStyle);
						tempRect.position += ScrollPosition;

						_action.nodeRect.width = tempRect.width;

						if (!adjustHeight)
						{
							_action.nodeRect.height = tempRect.height;
						}
					}

					GUI.color = originalColor;
				}
				LimitWindow (_action);
				DrawSockets (_action, isAsset, e);
				
				if (isAsset)
				{
					windowData.targetAsset = ActionListAssetEditor.ResizeList (windowData.targetAsset, numActions);
				}
				else
				{
					windowData.target = ActionListEditor.ResizeList (windowData.target, numActions);
				}
				
				if (dragMode == DragMode.Wire && loseConnection && hasDraggedWire && _action.nodeRect.Contains (e.mousePosition + ScrollPosition))
				{
					Reconnect (actionChanging, _action, isAsset);
				}

				if (dragMode != DragMode.Marquee && _action.nodeRect.Contains (e.mousePosition + ScrollPosition))
				{
					canMarquee = false;
				}
			}

			if (loseConnection && dragMode == DragMode.Wire)
			{
				if (/*hasDraggedWire &&*/ e.mousePosition.x > 0f && e.mousePosition.x < CanvasWidth / Zoom && e.mousePosition.y > 0f && e.mousePosition.y < (CanvasHeight - 45f) / Zoom)
				{
					EndConnect (actionChanging, e.mousePosition + ScrollPosition, isAsset);
				}
				actionChanging = null;
				dragMode = DragMode.None;
				GUI.changed = true;
			}

			if (dragMode == DragMode.Wire)
			{	
				bool onSide = (actionChanging is ActionCheck || actionChanging is ActionCheckMultiple || actionChanging is ActionParallel);
				AdvGame.DrawNodeCurve (new Rect (actionChanging.nodeRect.position - ScrollPosition, actionChanging.nodeRect.size), e.mousePosition, Color.black, offsetChanging, onSide, false, actionChanging.isDisplayed);
			}

			
			if (e.type == EventType.ContextClick && dragMode == DragMode.None)
			{
				menuPosition = e.mousePosition + ScrollPosition;
				CreateEmptyMenu (isAsset);
			}

			EndWindows ();
			GUI.EndGroup ();
			EditorZoomArea.End ();
		}


		private void SetMarked (bool state)
		{
			if (Actions != null && Actions.Count > 0)
			{
				foreach (Action action in Actions)
				{
					if (action)
					{
						action.isMarked = state;
					}
				}
			}
		}
		
		
		private void UnmarkAll ()
		{
			SetMarked (false);
		}


		private void MarkAll ()
		{
			SetMarked (true);
		}
			
		
		private Action InsertAction (int i, Vector2 _position, bool isAsset)
		{
			List<Action> actionList = new List<Action>();
			if (isAsset)
			{
				actionList = windowData.targetAsset.actions;
				Undo.RecordObject (windowData.targetAsset, "Create action");
				ActionListAssetEditor.AddAction (ActionsManager.GetDefaultAction (), i+1, windowData.targetAsset);
			}
			else
			{
				actionList = windowData.target.actions;
				ActionListEditor.ModifyAction (windowData.target, windowData.target.actions[i], "Insert after");
			}
			
			numActions ++;
			UnmarkAll ();
			
			actionList [i+1].nodeRect.x = _position.x - 150;
			actionList [i+1].nodeRect.y = _position.y;
			actionList [i+1].endAction = ResultAction.Stop;
			actionList [i+1].isDisplayed = true;
			
			return actionList [i+1];
		}
		
		
		private void FixConnections (int i, bool isAsset)
		{
			List<Action> actionList = new List<Action>();
			if (isAsset)
			{
				actionList = windowData.targetAsset.actions;
			}
			else
			{
				actionList = windowData.target.actions;
			}

			if (actionList[i] == null) return;
			
			if (actionList[i].numSockets == 0)
			{
				actionList[i].endAction = ResultAction.Stop;
			}
			
			else if (actionList[i] is ActionCheck)
			{
				ActionCheck tempAction = (ActionCheck) actionList[i];
				if (tempAction.resultActionTrue == ResultAction.Skip && !actionList.Contains (tempAction.skipActionTrueActual))
				{
					if (tempAction.skipActionTrue >= actionList.Count)
					{
						tempAction.resultActionTrue = ResultAction.Stop;
					}
				}
				if (tempAction.resultActionFail == ResultAction.Skip && !actionList.Contains (tempAction.skipActionFailActual))
				{
					if (tempAction.skipActionFail >= actionList.Count)
					{
						tempAction.resultActionFail = ResultAction.Stop;
					}
				}
			}
			else if (actionList[i] is ActionCheckMultiple)
			{
				ActionCheckMultiple tempAction = (ActionCheckMultiple) actionList[i];
				foreach (ActionEnd ending in tempAction.endings)
				{
					if (ending.resultAction == ResultAction.Skip && !actionList.Contains (ending.skipActionActual))
					{
						if (ending.skipAction >= actionList.Count)
						{
							ending.resultAction = ResultAction.Stop;
						}
					}
				}
			}
			else if (actionList[i] is ActionParallel)
			{
				ActionParallel tempAction = (ActionParallel) actionList[i];
				foreach (ActionEnd ending in tempAction.endings)
				{
					if (ending.resultAction == ResultAction.Skip && !actionList.Contains (ending.skipActionActual))
					{
						if (ending.skipAction >= actionList.Count)
						{
							ending.resultAction = ResultAction.Stop;
						}
					}
				}
			}
			else
			{
				if (actionList[i].endAction == ResultAction.Skip && !actionList.Contains (actionList[i].skipActionActual))
				{
					if (actionList[i].skipAction >= actionList.Count)
					{
						actionList[i].endAction = ResultAction.Stop;
					}
				}
			}
		}
		
		
		private void EndConnect (Action action1, Vector2 mousePosition, bool isAsset)
		{
			List<Action> actionList = (isAsset) ? windowData.targetAsset.actions : windowData.target.actions;

			dragMode = DragMode.None;
			
			if (action1 is ActionCheck)
			{
				ActionCheck tempAction = (ActionCheck) action1;
				if (resultType)
				{
					if (actionList.IndexOf (action1) == actionList.Count - 1 && tempAction.resultActionTrue != ResultAction.Skip)
					{
						InsertAction (actionList.IndexOf (action1), mousePosition, isAsset);
						tempAction.resultActionTrue = ResultAction.Continue;

						if (tempAction.resultActionFail == ResultAction.Continue)
						{
							tempAction.resultActionFail = ResultAction.Stop;
						}
					}
					else if (tempAction.resultActionTrue == ResultAction.Stop)
					{
						tempAction.resultActionTrue = ResultAction.Skip;
						tempAction.skipActionTrueActual = InsertAction (actionList.Count-1, mousePosition, isAsset);
					}
					else
					{
						tempAction.resultActionTrue = ResultAction.Stop;
					}
				}
				else
				{
					if (actionList.IndexOf (action1) == actionList.Count - 1 && tempAction.resultActionFail != ResultAction.Skip)
					{
						InsertAction (actionList.IndexOf (action1), mousePosition, isAsset);
						tempAction.resultActionFail = ResultAction.Continue;

						if (tempAction.resultActionTrue == ResultAction.Continue)
						{
							tempAction.resultActionTrue = ResultAction.Stop;
						}
					}
					else if (tempAction.resultActionFail == ResultAction.Stop)
					{
						tempAction.resultActionFail = ResultAction.Skip;
						tempAction.skipActionFailActual = InsertAction (actionList.Count-1, mousePosition, isAsset);
					}
					else
					{
						tempAction.resultActionFail = ResultAction.Stop;
					}
				}
			}
			else if (action1 is ActionCheckMultiple)
			{
				ActionCheckMultiple tempAction = (ActionCheckMultiple) action1;
				ActionEnd ending = tempAction.endings [multipleResultType];
				
				if (actionList.IndexOf (action1) == actionList.Count - 1 && ending.resultAction != ResultAction.Skip)
				{
					InsertAction (actionList.IndexOf (action1), mousePosition, isAsset);
					ending.resultAction = ResultAction.Continue;

					foreach (ActionEnd otherEnding in tempAction.endings)
					{
						if (otherEnding != ending && otherEnding.resultAction == ResultAction.Continue)
						{
							otherEnding.resultAction = ResultAction.Stop;
						}
					}
				}
				else if (ending.resultAction == ResultAction.Stop)
				{
					ending.resultAction = ResultAction.Skip;
					ending.skipActionActual = InsertAction (actionList.Count-1, mousePosition, isAsset);
				}
				else
				{
					ending.resultAction = ResultAction.Stop;
				}
			}
			else if (action1 is ActionParallel)
			{
				ActionParallel tempAction = (ActionParallel) action1;
				ActionEnd ending = tempAction.endings [multipleResultType];
				
				if (actionList.IndexOf (action1) == actionList.Count - 1 && ending.resultAction != ResultAction.Skip)
				{
					InsertAction (actionList.IndexOf (action1), mousePosition, isAsset);
					ending.resultAction = ResultAction.Continue;

					foreach (ActionEnd otherEnding in tempAction.endings)
					{
						if (otherEnding != ending && otherEnding.resultAction == ResultAction.Continue)
						{
							otherEnding.resultAction = ResultAction.Stop;
						}
					}
				}
				else if (ending.resultAction == ResultAction.Stop)
				{
					ending.resultAction = ResultAction.Skip;
					ending.skipActionActual = InsertAction (actionList.Count-1, mousePosition, isAsset);
				}
				else
				{
					ending.resultAction = ResultAction.Stop;
				}
			}
			else
			{
				if (actionList.IndexOf (action1) == actionList.Count - 1 && action1.endAction != ResultAction.Skip)
				{
					InsertAction (actionList.IndexOf (action1), mousePosition, isAsset);
					action1.endAction = ResultAction.Continue;
				}
				else if (action1.endAction == ResultAction.Stop)
				{
					// Remove bad "end" connection
					float x = mousePosition.x;
					foreach (AC.Action action in actionList)
					{
						if (action.nodeRect.x > x && !(action is ActionCheck) && !(action is ActionCheckMultiple || action is ActionParallel) && action.endAction == ResultAction.Continue)
						{
							// Is this the "last" one?
							int i = actionList.IndexOf (action);
							if (actionList.Count == (i+1))
							{
								action.endAction = ResultAction.Stop;
							}
						}
					}
					
					action1.endAction = ResultAction.Skip;
					action1.skipActionActual = InsertAction (actionList.Count-1, mousePosition, isAsset);
				}
				else
				{
					action1.endAction = ResultAction.Stop;
				}
			}

			actionChanging = null;
			offsetChanging = 0;
			
			if (isAsset)
			{
				EditorUtility.SetDirty (windowData.targetAsset);
			}
			else
			{
				UnityVersionHandler.CustomSetDirty (windowData.target, true);
			}
		}
		
		
		private void Reconnect (Action action1, Action action2, bool isAsset)
		{
			dragMode = DragMode.None;
			
			if (action1 is ActionCheck)
			{
				ActionCheck actionCheck = (ActionCheck) action1;
				
				if (resultType)
				{
					actionCheck.resultActionTrue = ResultAction.Skip;
					if (action2 != null)
					{
						actionCheck.skipActionTrueActual = action2;
					}
				}
				else
				{
					actionCheck.resultActionFail = ResultAction.Skip;
					if (action2 != null)
					{
						actionCheck.skipActionFailActual = action2;
					}
				}
			}
			else if (action1 is ActionCheckMultiple)
			{
				ActionCheckMultiple actionCheckMultiple = (ActionCheckMultiple) action1;
				
				ActionEnd ending = actionCheckMultiple.endings [multipleResultType];
				
				ending.resultAction = ResultAction.Skip;
				if (action2 != null)
				{
					ending.skipActionActual = action2;
				}
			}
			else if (action1 is ActionParallel)
			{
				ActionParallel actionParallel = (ActionParallel) action1;
				ActionEnd ending = actionParallel.endings [multipleResultType];
				
				ending.resultAction = ResultAction.Skip;
				if (action2 != null)
				{
					ending.skipActionActual = action2;
				}
			}
			else
			{
				action1.endAction = ResultAction.Skip;
				action1.skipActionActual = action2;
			}
			
			actionChanging = null;
			offsetChanging = 0;
			
			if (isAsset)
			{
				EditorUtility.SetDirty (windowData.targetAsset);
			}
			else
			{
				UnityVersionHandler.CustomSetDirty (windowData.target, true);
			}
		}
		
		
		private void DrawSockets (Action action, bool isAsset, Event e)
		{
			if (action == null) return;

			List<Action> actionList = new List<Action>();
			if (isAsset)
			{
				actionList = windowData.targetAsset.actions;
			}
			else
			{
				actionList = windowData.target.actions;
			}
			
			int i = actionList.IndexOf (action);
			
			if (action.numSockets == 0)
			{
				return;
			}
			
			if (!action.isDisplayed && (action is ActionCheck || action is ActionCheckMultiple || action is ActionParallel))
			{
				action.DrawOutWires (actionList, i, 0, scrollPosition);
				return;
			}
			
			int offset = 0;
			
			if (action is ActionCheck)
			{
				ActionCheck actionCheck = (ActionCheck) action;
				if (actionCheck.resultActionFail != ResultAction.RunCutscene)
				{
					if (actionCheck.resultActionFail != ResultAction.Skip || action.showOutputSockets)
					{
						Vector2 buttonPosition = new Vector2 (action.nodeRect.x + action.nodeRect.width - 2, action.nodeRect.y + action.nodeRect.height - 22);
						Rect buttonRect = new Rect (buttonPosition - scrollPosition, socketSize);
						
						if (e.isMouse && dragMode == DragMode.None && e.type == EventType.MouseDown && action.isEnabled && buttonRect.Contains(e.mousePosition))
						{
							if (e.button == 0)
							{
								offsetChanging = 10;
								resultType = false;
								actionChanging = action;
								dragMode = DragMode.Wire;
								hasDraggedWire = false;
							}
							else if (e.button == 1)
							{
								if (actionCheck.resultActionFail == ResultAction.Continue && (actionList.IndexOf (action) < actionList.Count - 1))
								{
									CreateSocketMenu (actionList.IndexOf (action) + 1);
								}
								else if (actionCheck.resultActionFail == ResultAction.Skip)
								{
									CreateSocketMenu (actionCheck.skipActionFail);
								}
							}
						}

						GUI.Button (buttonRect, "", CustomStyles.IconSocket);
					}

					if (actionCheck.resultActionFail == ResultAction.Skip)
					{
						offset = 17;
					}
				}
				if (actionCheck.resultActionTrue != ResultAction.RunCutscene)
				{
					if (actionCheck.resultActionTrue != ResultAction.Skip || action.showOutputSockets)
					{
						Vector2 buttonPosition = new Vector2 (action.nodeRect.x + action.nodeRect.width - 2, action.nodeRect.y + action.nodeRect.height - 40 - offset);
						Rect buttonRect = new Rect (buttonPosition - scrollPosition, socketSize);

						if (e.isMouse && dragMode == DragMode.None && e.type == EventType.MouseDown && action.isEnabled && buttonRect.Contains(e.mousePosition))
						{
							if (e.button == 0)
							{
								offsetChanging = 30 + offset;
								resultType = true;
								actionChanging = action;
								dragMode = DragMode.Wire;
								hasDraggedWire = false;
							}
							else if (e.button == 1)
							{
								if (actionCheck.resultActionTrue == ResultAction.Continue && (actionList.IndexOf (action) < actionList.Count - 1))
								{
									CreateSocketMenu (actionList.IndexOf (action) + 1);
								}
								else if (actionCheck.resultActionTrue == ResultAction.Skip)
								{
									CreateSocketMenu (actionCheck.skipActionTrue);
								}
							}
						}
						
						GUI.Button (buttonRect, "", CustomStyles.IconSocket);
					}
				}
			}
			else if (action is ActionCheckMultiple)
			{
				ActionCheckMultiple actionCheckMultiple = (ActionCheckMultiple) action;

				int totalHeight = 20;
				for (int j = actionCheckMultiple.endings.Count-1; j>=0; j--)
				{
					ActionEnd ending = actionCheckMultiple.endings [j];

					if (ending.resultAction != ResultAction.RunCutscene)
					{
						if (ending.resultAction != ResultAction.Skip || action.showOutputSockets)
						{
							Vector2 buttonPosition = new Vector2 (action.nodeRect.x + action.nodeRect.width - 2, action.nodeRect.y + action.nodeRect.height - totalHeight);
							Rect buttonRect = new Rect (buttonPosition - scrollPosition, socketSize);

							if (e.isMouse && dragMode == DragMode.None && e.type == EventType.MouseDown && action.isEnabled && buttonRect.Contains(e.mousePosition))
							{
								if (e.button == 0)
								{
									offsetChanging = totalHeight - 10;
									multipleResultType = actionCheckMultiple.endings.IndexOf (ending);
									actionChanging = action;
									dragMode = DragMode.Wire;
									hasDraggedWire = false;
								}
								else if (e.button == 1)
								{
									if (ending.resultAction == ResultAction.Continue && (actionList.IndexOf (action) < actionList.Count - 1))
									{
										CreateSocketMenu (actionList.IndexOf (action) + 1);
									}
									else if (ending.resultAction == ResultAction.Skip)
									{
										CreateSocketMenu (ending.skipAction);
									}
								}
							}
							
							GUI.Button (buttonRect, string.Empty, CustomStyles.IconSocket);
						}
					}

					if (ending.resultAction == ResultAction.Skip)
					{
						totalHeight += 44;
					}
					else
					{
						totalHeight += 26;
					}
				}
			}
			else if (action is ActionParallel)
			{
				ActionParallel actionParallel = (ActionParallel) action;

				int totalHeight = 20;
				for (int j = actionParallel.endings.Count-1; j>=0; j--)
				{
					ActionEnd ending = actionParallel.endings [j];

					if (ending.resultAction != ResultAction.RunCutscene)
					{
						if (ending.resultAction != ResultAction.Skip || action.showOutputSockets)
						{
							Vector2 buttonPosition = new Vector2 (action.nodeRect.x + action.nodeRect.width - 2, action.nodeRect.y + action.nodeRect.height - totalHeight);
							Rect buttonRect = new Rect (buttonPosition - scrollPosition, socketSize);

							if (e.isMouse && dragMode == DragMode.None && e.type == EventType.MouseDown && action.isEnabled && buttonRect.Contains(e.mousePosition))
							{
								if (e.button == 0)
								{
									offsetChanging = totalHeight - 10;
									multipleResultType = actionParallel.endings.IndexOf (ending);
									actionChanging = action;
									dragMode = DragMode.Wire;
									hasDraggedWire = false;
								}
								else if (e.button == 1)
								{
									if (ending.resultAction == ResultAction.Continue && (actionList.IndexOf (action) < actionList.Count - 1))
									{
										CreateSocketMenu (actionList.IndexOf (action) + 1);
									}
									else if (ending.resultAction == ResultAction.Skip)
									{
										CreateSocketMenu (ending.skipAction);
									}
								}
							}
							
							GUI.Button (buttonRect, string.Empty, CustomStyles.IconSocket);
						}
					}

					if (ending.resultAction == ResultAction.Skip)
					{
						totalHeight += 44;
					}
					else
					{
						totalHeight += 26;
					}
				}
			}
			else
			{
				// Base class
				if (action.endAction != ResultAction.RunCutscene)
				{
					if (action.endAction != ResultAction.Skip || action.showOutputSockets)
					{
						Vector2 buttonPosition = new Vector2 (action.nodeRect.x + action.nodeRect.width / 2f - 8, action.nodeRect.y + action.nodeRect.height);
						Rect buttonRect = new Rect (buttonPosition - scrollPosition, socketSize);

						if (e.isMouse && dragMode == DragMode.None && e.type == EventType.MouseDown && action.isEnabled && buttonRect.Contains(e.mousePosition))
						{
							if (e.button == 0)
							{
								offsetChanging = 10;
								actionChanging = action;
								dragMode = DragMode.Wire;
								hasDraggedWire = false;
							}
							else if (e.button == 1)
							{
								if (action.endAction == ResultAction.Continue && (actionList.IndexOf (action) < actionList.Count - 1))
								{
									CreateSocketMenu (actionList.IndexOf (action) + 1);
								}
								else if (action.endAction == ResultAction.Skip)
								{
									CreateSocketMenu (action.skipAction);
								}
							}
						}
						
						GUI.Button (buttonRect, "", CustomStyles.IconSocket);
					}
				}
			}

			action.DrawOutWires (actionList, i, offset, scrollPosition);
		}


		private void FocusOnActions (bool onlyIfNotInView = false, bool onlyMarked = true)
		{
			if (NumActionsMarked > 0 || !onlyMarked)
			{
				Vector2 maxCorner = Actions[0].nodeRect.position;
				for (int i=0; i<Actions.Count; i++)
				{
					if (Actions[i].isMarked || !onlyMarked)
					{
						maxCorner.x = Mathf.Max (maxCorner.x, Actions[i].nodeRect.x + Actions[i].nodeRect.width);
						maxCorner.y = Mathf.Max (maxCorner.y, Actions[i].nodeRect.y + Actions[i].nodeRect.height);
					}
				}

				Vector2 minCorner = maxCorner - new Vector2 (300f, 50f);
				for (int i=0; i<Actions.Count; i++)
				{
					if (Actions[i].isMarked || !onlyMarked)
					{
						minCorner.x = Mathf.Min (minCorner.x, Actions[i].nodeRect.x);
						minCorner.y = Mathf.Min (minCorner.y, Actions[i].nodeRect.y);
					}
				}

				Vector2 relativeScale = new Vector2 ((maxCorner.x - minCorner.x) / CanvasWidth, (maxCorner.y - minCorner.y) / CanvasHeight);
				float largestScale = Mathf.Max (relativeScale.x, relativeScale.y);
				Zoom = 1f / largestScale;

				if (onlyIfNotInView)
				{
					if ((minCorner.x < ScrollPosition.x) || 
						(maxCorner.x > ScrollPosition.x + CanvasWidth) ||
						(minCorner.y < ScrollPosition.y) ||
						(maxCorner.y > ScrollPosition.y + CanvasHeight))
					{
						// OK, move
					}
					else
					{
						return;
					}
				}

				if (!onlyMarked)
				{
					ScrollPosition = Vector2.zero;
				}
				else
				{
					ScrollPosition = new Vector2 ((maxCorner.x + minCorner.x) / 2f, (maxCorner.y + minCorner.y) / 2f) - (position.size / 2f);
				}
			}
		}


		private void FocusOnAction (int i)
		{
			UnmarkAll ();

			if (Actions.Count > i && i >= 0 && Actions[i] != null)
			{
				Zoom = 1f;

				Vector2 centre = Actions[i].nodeRect.center;
				ScrollPosition = new Vector2 (centre.x - CanvasWidth / 2f, centre.y - CanvasHeight / 2f);
				Repaint ();
			}
		}
		
		
		private void CreateEmptyMenu (bool isAsset)
		{
			EditorGUIUtility.editingTextField = false;
			GenericMenu menu = new GenericMenu ();
			menu.AddItem (new GUIContent ("Add new Action"), false, EmptyCallback, "Add new Action");
			if (AdvGame.copiedActions != null && AdvGame.copiedActions.Count > 0)
			{
				//menu.AddSeparator ("");
				menu.AddItem (new GUIContent ("Paste copied Action(s)"), false, EmptyCallback, "Paste copied Action(s)");
			}

			if (KickStarter.actionsManager.GetNumFavouriteActions () > 0)
			{
				for (int j=1; j<maxFavourites; j++)
				{
					string label = KickStarter.actionsManager.GetFavouriteActionLabel (j);
					if (string.IsNullOrEmpty (label)) continue;
					menu.AddItem (new GUIContent ("Paste favourite/Slot " + j.ToString () + " (" + label + ")"), false, EmptyCallback, "Paste Favourite " + j.ToString ());
				}
			}
			
			menu.AddSeparator (string.Empty);
			menu.AddItem (new GUIContent ("Select all"), false, EmptyCallback, "Select all");
			
			if (NumActionsMarked > 0)
			{
				menu.AddItem (new GUIContent ("Deselect all"), false, EmptyCallback, "Deselect all");
				menu.AddSeparator (string.Empty);
				if (!Application.isPlaying)
				{
					menu.AddItem (new GUIContent ("Cut selected"), false, EmptyCallback, "Cut selected");
					menu.AddItem (new GUIContent ("Copy selected"), false, EmptyCallback, "Copy selected");
				}
				menu.AddItem (new GUIContent ("Delete selected"), false, EmptyCallback, "Delete selected");
				menu.AddSeparator (string.Empty);
				menu.AddItem (new GUIContent ("Collapse selected"), false, EmptyCallback, "Collapse selected");
				menu.AddItem (new GUIContent ("Expand selected"), false, EmptyCallback, "Expand selected");
				menu.AddSeparator (string.Empty);
				menu.AddItem (new GUIContent ("Comment selected"), false, EmptyCallback, "Comment selected");
				menu.AddItem (new GUIContent ("Uncomment selected"), false, EmptyCallback, "Uncomment selected");
				menu.AddSeparator (string.Empty);
				menu.AddItem (new GUIContent ("Output sockets/Show"), false, EmptyCallback, "Show output socket(s)");
				menu.AddItem (new GUIContent ("Output sockets/Hide"), false, EmptyCallback, "Hide output socket(s)");

				menu.AddItem (new GUIContent ("Color/Default"), false, EmptyCallback, "ColorDefault");
				menu.AddItem (new GUIContent ("Color/Blue"), false, EmptyCallback, "ColorBlue");
				menu.AddItem (new GUIContent ("Color/Red"), false, EmptyCallback, "ColorRed");
				menu.AddItem (new GUIContent ("Color/Green"), false, EmptyCallback, "ColorGreen");
				menu.AddItem (new GUIContent ("Color/Yellow"), false, EmptyCallback, "ColorYellow");
				menu.AddItem (new GUIContent ("Color/Cyan"), false, EmptyCallback, "ColorCyan");
				menu.AddItem (new GUIContent ("Color/Purple"), false, EmptyCallback, "ColorMagenta");

				if (NumActionsMarked == 1)
				{
					menu.AddSeparator ("");
					menu.AddItem (new GUIContent ("Move to front"), false, EmptyCallback, "Move to front");
				}
			}
			
			menu.AddSeparator (string.Empty);

			menu.AddItem (new GUIContent ("View/Reset"), false, EmptyCallback, "ViewReset");
			menu.AddItem (new GUIContent ("View/All"), false, EmptyCallback, "ViewAll");

			if (NumActionsMarked > 0)
			{
				menu.AddItem (new GUIContent ("View/Selected"), false, EmptyCallback, "ViewSelected");
			}

			for (int i = 0; i < Actions.Count; i++)
			{
				string actionLabel = "(" + i.ToString () + ") " + Actions[i].category + ": " + Actions[i].title;
				menu.AddItem (new GUIContent ("View/Action/" + actionLabel), false, EmptyCallback, "ViewFrame" + i.ToString ());
			}

			menu.AddSeparator (string.Empty);

			if (NumActionsMarked > 1)
			{
				menu.AddItem (new GUIContent ("Align/Horizontally"), false, EmptyCallback, "AlignHorizontally");
				menu.AddItem (new GUIContent ("Align/Vertically"), false, EmptyCallback, "AlignVertically");
			}

			menu.AddItem (new GUIContent ("Auto-arrange"), false, EmptyCallback, "Auto-arrange");

			Matrix4x4 originalMatrix = GUI.matrix;
			GUI.matrix = GetMenuScaleMatrix ();

			menu.ShowAsContext ();

			GUI.matrix = originalMatrix;
		}
		
		
		private void CreateNodeMenu (int i, Action _action)
		{
			EditorGUIUtility.editingTextField = false;
			UnmarkAll ();
			_action.isMarked = true;

			GenericMenu menu = new GenericMenu ();

			if (!Application.isPlaying)
			{
				menu.AddItem (new GUIContent ("Cut"), false, EmptyCallback, "Cut selected");
				menu.AddItem (new GUIContent ("Copy"), false, EmptyCallback, "Copy selected");
				if (AdvGame.copiedActions.Count > 0)
				{
					menu.AddItem (new GUIContent ("Paste after"), false, EmptyCallback, "Paste after");
				}
				menu.AddSeparator (string.Empty);
			}
			menu.AddItem (new GUIContent ("Insert after"), false, EmptyCallback, "Insert after");
			menu.AddItem (new GUIContent ("Delete"), false, EmptyCallback, "Delete selected");
			
			if (i>0)
			{
				menu.AddSeparator (string.Empty);
				menu.AddItem (new GUIContent ("Move to front"), false, EmptyCallback, "Move to front");
			}

			menu.AddSeparator (string.Empty);
			menu.AddItem (new GUIContent ("Toggle breakpoint"), false, EmptyCallback, "Toggle breakpoint");
			menu.AddItem (new GUIContent ("Toggle comment"), false, EmptyCallback, "Toggle comment");
			menu.AddItem (new GUIContent ("Toggle output socket(s)"), false, EmptyCallback, "Toggle output socket(s)");

			menu.AddItem (new GUIContent ("Color/Default"), false, EmptyCallback, "ColorDefault");
			menu.AddItem (new GUIContent ("Color/Blue"), false, EmptyCallback, "ColorBlue");
			menu.AddItem (new GUIContent ("Color/Red"), false, EmptyCallback, "ColorRed");
			menu.AddItem (new GUIContent ("Color/Green"), false, EmptyCallback, "ColorGreen");
			menu.AddItem (new GUIContent ("Color/Yellow"), false, EmptyCallback, "ColorYellow");
			menu.AddItem (new GUIContent ("Color/Cyan"), false, EmptyCallback, "ColorCyan");
			menu.AddItem (new GUIContent ("Color/Purple"), false, EmptyCallback, "ColorMagenta");

			for (int j=1; j<=maxFavourites; j++)
			{
				string label = KickStarter.actionsManager.GetFavouriteActionLabel (j);
				if (!string.IsNullOrEmpty (label)) label = " (" + label + ")";
				menu.AddItem (new GUIContent ("Favourite/Slot " + j.ToString () + label), false, EmptyCallback, "SetFavourite" + j.ToString ());
			}

			menu.AddSeparator (string.Empty);
			menu.AddItem (new GUIContent ("Edit Script"), false, EmptyCallback, "EditSource");

			Matrix4x4 originalMatrix = GUI.matrix;
			GUI.matrix = GetMenuScaleMatrix ();

			menu.ShowAsContext ();

			GUI.matrix = originalMatrix;
		}


		private Matrix4x4 GetMenuScaleMatrix ()
		{
			return GUI.matrix * Matrix4x4.Scale (new Vector3 (1f / zoom, 1f / zoom, 1f / zoom));
		}


		private void CreateSocketMenu (int i)
		{
			focusActionIndex = i;
			EditorGUIUtility.editingTextField = false;

			GenericMenu menu = new GenericMenu ();

			menu.AddItem (new GUIContent ("Frame linked Action"), false, SocketCallBack, "Focus on linked Action");
			
			Matrix4x4 originalMatrix = GUI.matrix;
			GUI.matrix = GetMenuScaleMatrix ();

			menu.ShowAsContext ();

			GUI.matrix = originalMatrix;
		}
		

		private void EmptyCallback (object obj)
		{
			PerformEmptyCallBack (obj.ToString ());
		}


		private void SocketCallBack (object obj)
		{
			PerformSocketCallBack (obj.ToString ());
		}


		private void PerformEmptyCallBack (string objString)
		{
			bool isAsset = false;
			bool doUndo = (objString != "Copy selected" && !objString.StartsWith ("SetFavourite"));
			List<Action> actionList = new List<Action>();
			if (windowData.targetAsset != null)
			{
				isAsset = true;
				actionList = windowData.targetAsset.actions;
			}
			else
			{
				actionList = windowData.target.actions;
			}

			if (doUndo)
			{
				if (isAsset)
				{
					Undo.SetCurrentGroupName (objString);
					Undo.RecordObjects (new UnityEngine.Object [] {  windowData.targetAsset }, objString);
					Undo.RecordObjects (windowData.targetAsset.actions.ToArray (), objString);
				}
				else
				{
					Undo.SetCurrentGroupName (objString);
					Undo.RecordObjects (new UnityEngine.Object [] {  windowData.target }, objString);
					Undo.RecordObjects (windowData.target.actions.ToArray (), objString);
				}
			}

			foreach (Action action in actionList)
			{
				action.SkipActionGUI (actionList, false);
			}

			if (objString == "Add new Action")
			{
				Action currentAction = actionList[actionList.Count - 1];
				if (currentAction.endAction == ResultAction.Continue)
				{
					currentAction.endAction = ResultAction.Stop;
				}

				if (isAsset)
				{
					ActionListAssetEditor.ModifyAction (windowData.targetAsset, currentAction, "Insert after");
				}
				else
				{
					ActionListEditor.ModifyAction (windowData.target, null, "Insert end");
				}

				actionList[actionList.Count - 1].nodeRect.position = menuPosition;
				actionList[actionList.Count - 1].isDisplayed = true;
			}
			else if (objString == "Paste copied Action(s)")
			{
				if (AdvGame.copiedActions.Count == 0)
				{
					return;
				}

				int offset = actionList.Count;
				UnmarkAll ();

				Action currentLastAction = actionList[actionList.Count - 1];
				if (currentLastAction.endAction == ResultAction.Continue)
				{
					currentLastAction.endAction = ResultAction.Stop;
				}

				Vector2 firstPosition = new Vector2 (AdvGame.copiedActions[0].nodeRect.x, AdvGame.copiedActions[0].nodeRect.y);
				foreach (Action actionToCopy in AdvGame.copiedActions)
				{
					if (actionToCopy == null)
					{
						ACDebug.LogWarning ("Error when pasting Action - cannot find original. Did you change scene before pasting? If you need to transfer Actions between scenes, copy them to an ActionList asset first.");
						continue;
					}

					AC.Action duplicatedAction = Object.Instantiate (actionToCopy) as AC.Action;
					duplicatedAction.PrepareToPaste (offset);

					if (AdvGame.copiedActions.IndexOf (actionToCopy) == 0)
					{
						duplicatedAction.nodeRect.position = menuPosition;
					}
					else
					{
						duplicatedAction.nodeRect.position = menuPosition + (actionToCopy.nodeRect.position - firstPosition);
					}

					duplicatedAction.isMarked = true;

					if (isAsset)
					{
						ActionListAssetEditor.AddAction (duplicatedAction, -1, windowData.targetAsset);
					}
					else
					{
						ActionListEditor.AddAction (duplicatedAction, -1, windowData.target);
					}
				}
				AdvGame.DuplicateActionsBuffer ();
				FocusOnActions (true, true);
			}
			else if (objString == "Select all")
			{
				foreach (Action action in actionList)
				{
					action.isMarked = true;
				}
			}
			else if (objString == "Deselect all")
			{
				foreach (Action action in actionList)
				{
					action.isMarked = false;
				}
			}
			else if (objString == "Expand selected")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.isDisplayed = true;
					}
				}
			}
			else if (objString == "Collapse selected")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.isDisplayed = false;
					}
				}
			}
			else if (objString == "Comment selected")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.showComment = true;
					}
				}
			}
			else if (objString == "Uncomment selected")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.showComment = false;
					}
				}
			}
			else if (objString == "Show output socket(s)")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.showOutputSockets = true;
					}
				}
			}
			else if (objString == "Hide output socket(s)")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.showOutputSockets = false;
					}
				}
			}
			else if (objString == "Cut selected")
			{
				List<Action> copyList = new List<Action> ();
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						Action copyAction = Object.Instantiate (action) as Action;
						copyAction.name = copyAction.name.Replace ("(Clone)", "");
						copyAction.PrepareToCopy (actionList.IndexOf (action), actionList);
						copyAction.isMarked = false;
						copyList.Add (copyAction);
					}
				}

				AdvGame.copiedActions = copyList;
				PerformEmptyCallBack ("Delete selected");
			}
			else if (objString == "Copy selected")
			{
				List<Action> copyList = new List<Action> ();
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						Action copyAction = Object.Instantiate (action) as Action;
						copyAction.name = copyAction.name.Replace ("(Clone)", "");
						copyAction.PrepareToCopy (actionList.IndexOf (action), actionList);
						copyAction.ClearIDs ();
						copyAction.isMarked = false;
						copyList.Add (copyAction);
					}
				}

				AdvGame.copiedActions = copyList;
				UnmarkAll ();
			}
			else if (objString == "Delete selected")
			{
				while (NumActionsMarked > 0)
				{
					foreach (Action action in actionList)
					{
						if (action.isMarked)
						{
							// Work out what has to be re-connected to what after deletion
							Action targetAction = null;
							if (action is ActionCheck || action is ActionCheckMultiple || action is ActionParallel) { }
							else
							{
								if (action.endAction == ResultAction.Skip && action.skipActionActual)
								{
									targetAction = action.skipActionActual;
								}
								else if (action.endAction == ResultAction.Continue && actionList.IndexOf (action) < (actionList.Count - 1))
								{
									targetAction = actionList[actionList.IndexOf (action) + 1];
								}

								foreach (Action _action in actionList)
								{
									if (action != _action)
									{
										_action.FixLinkAfterDeleting (action, targetAction, actionList);
									}
								}

								if (targetAction != null && actionList.IndexOf (action) == 0)
								{
									// Deleting first, so find new first
									actionList.Remove (targetAction);
									actionList.Insert (0, targetAction);
								}
							}

							if (isAsset)
							{
								ActionListAssetEditor.DeleteAction (action, windowData.targetAsset);
							}
							else
							{
								ActionListEditor.DeleteAction (action, windowData.target);
							}

							numActions--;
							break;
						}
					}
				}
				if (actionList.Count == 0)
				{
					if (isAsset)
					{
						ActionListAssetEditor.AddAction (ActionsManager.GetDefaultAction (), -1, windowData.targetAsset);
					}
				}
			}
			else if (objString == "Move to front")
			{
				for (int i = 0; i < actionList.Count; i++)
				{
					Action action = actionList[i];
					if (action.isMarked)
					{
						action.isMarked = false;

						if (i > 0)
						{
							bool hasConnection = false;
							bool isLast = (i == actionList.Count - 1);

							if (action is ActionParallel)
							{
								ActionParallel actionParallel = (ActionParallel)action;

								foreach (ActionEnd ending in actionParallel.endings)
								{
									if (ending.resultAction == ResultAction.Continue)
									{
										if (isLast)
										{
											ending.resultAction = ResultAction.Stop;
										}
										else
										{
											ending.resultAction = ResultAction.Skip;
											ending.skipActionActual = actionList[i + 1];
											hasConnection = true;
										}
									}
									else if (ending.resultAction != ResultAction.Stop)
									{
										hasConnection = true;
									}
								}
							}
							else if (action is ActionCheck)
							{
								ActionCheck actionCheck = (ActionCheck)action;

								if (actionCheck.resultActionTrue == ResultAction.Continue)
								{
									if (isLast)
									{
										actionCheck.resultActionTrue = ResultAction.Stop;
									}
									else
									{
										actionCheck.resultActionTrue = ResultAction.Skip;
										actionCheck.skipActionTrueActual = actionList[i + 1];
										hasConnection = true;
									}
								}
								else if (actionCheck.resultActionTrue != ResultAction.Stop)
								{
									hasConnection = true;
								}

								if (actionCheck.resultActionFail == ResultAction.Continue)
								{
									if (isLast)
									{
										actionCheck.resultActionFail = ResultAction.Stop;
									}
									else
									{
										actionCheck.resultActionFail = ResultAction.Skip;
										actionCheck.skipActionFailActual = actionList[i + 1];
										hasConnection = true;
									}
								}
								else if (actionCheck.resultActionFail != ResultAction.Stop)
								{
									hasConnection = true;
								}
							}
							else if (action is ActionCheckMultiple)
							{
								ActionCheckMultiple actionCheckMultiple = (ActionCheckMultiple)action;

								foreach (ActionEnd ending in actionCheckMultiple.endings)
								{
									if (ending.resultAction == ResultAction.Continue)
									{
										if (isLast)
										{
											ending.resultAction = ResultAction.Stop;
										}
										else
										{
											ending.resultAction = ResultAction.Skip;
											ending.skipActionActual = actionList[i + 1];
											hasConnection = true;
										}
									}
									else if (ending.resultAction != ResultAction.Stop)
									{
										hasConnection = true;
									}
								}
							}
							else
							{
								if (action.endAction == ResultAction.Continue)
								{
									if (isLast)
									{
										action.endAction = ResultAction.Stop;
									}
									else
									{
										action.endAction = ResultAction.Skip;
										action.skipActionActual = actionList[i + 1];
										hasConnection = true;
									}
								}
								else if (action.endAction != ResultAction.Stop)
								{
									hasConnection = true;
								}
							}

							actionList[0].nodeRect.x += 30f;
							actionList[0].nodeRect.y += 30f;
							actionList.Remove (action);
							actionList.Insert (0, action);

							if (!hasConnection && actionList.Count > 1)
							{
								if (action is ActionParallel)
								{ }
								else if (action is ActionCheckMultiple)
								{ }
								else if (action is ActionCheck)
								{ }
								else
								{
									action.endAction = ResultAction.Skip;
									action.skipActionActual = actionList[1];
								}
							}
						}
					}
				}
			}
			else if (objString == "Auto-arrange")
			{
				AutoArrange (isAsset);
			}
			else if (objString == "Toggle breakpoint")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.isBreakPoint = !action.isBreakPoint;
						action.isMarked = false;
					}
				}
			}
			else if (objString == "Toggle comment")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.showComment = !action.showComment;
						action.isMarked = false;
					}
				}
			}
			else if (objString == "Toggle output socket(s)")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.showOutputSockets = !action.showOutputSockets;
						action.isMarked = false;
					}
				}
			}
			else if (objString == "Insert after")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.isMarked = false;

						int newIndex = actionList.IndexOf (action) + 1;

						if (isAsset)
						{
							ActionListAssetEditor.ModifyAction (windowData.targetAsset, action, "Insert after");
						}
						else
						{
							ActionListEditor.ModifyAction (windowData.target, action, "Insert after");
						}

						actionList[newIndex].nodeRect.x = action.nodeRect.x + 50;
						actionList[newIndex].nodeRect.y = action.nodeRect.y + 100;
						actionList[newIndex].isDisplayed = true;

						if (action is ActionParallel)
						{
							ActionParallel actionParallel = (ActionParallel)action;
							if (actionParallel.endings != null && actionParallel.endings.Count > 0)
							{
								actionParallel.endings[0].resultAction = ResultAction.Continue;
							}
						}
						else if (action is ActionCheck)
						{
							ActionCheck actionCheck = (ActionCheck)action;
							actionCheck.resultActionTrue = ResultAction.Continue;
						}
						else if (action is ActionCheckMultiple)
						{
							ActionCheckMultiple actionCheckMultiple = (ActionCheckMultiple)action;
							if (actionCheckMultiple.endings != null && actionCheckMultiple.endings.Count > 0)
							{
								actionCheckMultiple.endings[0].resultAction = ResultAction.Continue;
							}
						}
						else
						{
							action.endAction = ResultAction.Continue;
						}

						break;
					}
				}
			}
			else if (objString == "Paste after")
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.isMarked = false;

						int offset = actionList.IndexOf (action) + 1;
						Vector2 initialPosition = new Vector2 (action.nodeRect.x + 50, action.nodeRect.y + 100);

						//
						Vector2 firstPosition = new Vector2 (AdvGame.copiedActions[0].nodeRect.x, AdvGame.copiedActions[0].nodeRect.y);
						foreach (Action actionToCopy in AdvGame.copiedActions)
						{
							if (actionToCopy == null)
							{
								ACDebug.LogWarning ("Error when pasting Action - cannot find original. Did you change scene before pasting? If you need to transfer Actions between scenes, copy them to an ActionList asset first.");
								continue;
							}

							int ownIndex = AdvGame.copiedActions.IndexOf (actionToCopy);

							AC.Action duplicatedAction = Object.Instantiate (actionToCopy) as AC.Action;
							duplicatedAction.PrepareToPaste (offset);

							if (ownIndex == 0)
							{
								duplicatedAction.nodeRect.x = initialPosition.x;
								duplicatedAction.nodeRect.y = initialPosition.y;
							}
							else
							{
								duplicatedAction.nodeRect.x = initialPosition.x + (actionToCopy.nodeRect.x - firstPosition.x);
								duplicatedAction.nodeRect.y = initialPosition.y + (actionToCopy.nodeRect.y - firstPosition.y);
							}

							duplicatedAction.isMarked = true;
							if (isAsset)
							{
								ActionListAssetEditor.AddAction (duplicatedAction, offset + ownIndex, windowData.targetAsset);
							}
							else
							{
								ActionListEditor.AddAction (duplicatedAction, offset + ownIndex, windowData.target);
							}
						}

						if (action is ActionParallel)
						{
							ActionParallel actionParallel = (ActionParallel)action;
							if (actionParallel.endings != null && actionParallel.endings.Count > 0)
							{
								actionParallel.endings[0].resultAction = ResultAction.Continue;
							}
						}
						else if (action is ActionCheck)
						{
							ActionCheck actionCheck = (ActionCheck)action;
							actionCheck.resultActionTrue = ResultAction.Continue;
						}
						else if (action is ActionCheckMultiple)
						{
							ActionCheckMultiple actionCheckMultiple = (ActionCheckMultiple)action;
							if (actionCheckMultiple.endings != null && actionCheckMultiple.endings.Count > 0)
							{
								actionCheckMultiple.endings[0].resultAction = ResultAction.Continue;
							}
						}
						else
						{
							action.endAction = ResultAction.Continue;
						}

						AdvGame.DuplicateActionsBuffer ();
						break;
					}
				}
			}
			else if (objString.StartsWith ("Color"))
			{
				Color newColor = Color.white;
				if (objString == "ColorBlue") newColor = Color.blue;
				if (objString == "ColorRed") newColor = Color.red;
				if (objString == "ColorGreen") newColor = Color.green;
				if (objString == "ColorYellow") newColor = Color.yellow;
				if (objString == "ColorCyan") newColor = Color.cyan;
				if (objString == "ColorMagenta") newColor = Color.magenta;

				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						action.overrideColor = newColor;
					}
				}
			}
			else if (objString == "AlignVertically")
			{
				float medianY = 0f;
				int numActions = 0;
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						medianY += action.nodeRect.y;
						numActions++;
					}
				}

				if (numActions > 0)
				{
					medianY /= (float)numActions;

					if (actionList.Count > 0 && actionList[0] != null && actionList[0].isMarked)
					{
						medianY = actionList[0].nodeRect.y;
					}

					foreach (Action action in actionList)
					{
						if (action.isMarked)
						{
							action.nodeRect.y = medianY; ;
							action.isMarked = false;
						}
					}
				}
			}
			else if (objString == "AlignHorizontally")
			{
				float medianX = 0f;
				int numActions = 0;
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						medianX += action.nodeRect.x;
						numActions++;
					}
				}

				if (numActions > 0)
				{
					medianX /= (float)numActions;

					if (actionList.Count > 0 && actionList[0] != null && actionList[0].isMarked)
					{
						medianX = actionList[0].nodeRect.x;
					}

					foreach (Action action in actionList)
					{
						if (action.isMarked)
						{
							action.nodeRect.x = medianX; ;
							action.isMarked = false;
						}
					}
				}
			}
			else if (objString == "ViewReset")
			{
				ScrollPosition = Vector2.zero;
				Zoom = 1f;
			}
			else if (objString == "ViewAll")
			{
				Vector2 maxCorner = Actions[0].nodeRect.position;
				for (int i=1; i<Actions.Count; i++)
				{
					maxCorner.x = Mathf.Max (maxCorner.x, Actions[i].nodeRect.x + Actions[i].nodeRect.width + 30f);
					maxCorner.y = Mathf.Max (maxCorner.y, Actions[i].nodeRect.y + Actions[i].nodeRect.height + 130f);
				}
				
				ScrollPosition = Vector2.zero;

				Vector2 relativeScale = new Vector2 (maxCorner.x / CanvasWidth, maxCorner.y / CanvasHeight);
				float largestScale = Mathf.Max (relativeScale.x, relativeScale.y);
				Zoom = 1f / largestScale;
			}
			else if (objString.StartsWith ("ViewSelected"))
			{
				FocusOnActions (false, true);
			}
			else if (objString.StartsWith ("ViewFrame"))
			{
				int _frameIndex = -1;
				string frameText = objString.Substring (9);
				if (int.TryParse (frameText, out _frameIndex))
				{
					FocusOnAction (_frameIndex);
				}
			}
			else if (objString.StartsWith ("SetFavourite"))
			{
				int _favouriteID = -1;
				string favouriteIDText = objString.Substring (12);

				if (int.TryParse (favouriteIDText, out _favouriteID))
				{
					foreach (Action action in actionList)
					{
						if (action.isMarked)
						{
							KickStarter.actionsManager.SetFavourite (action, _favouriteID);
							action.isMarked = false;
							break;
						}
					}
				}
			}
			else if (objString.StartsWith ("Paste Favourite "))
			{
				int _favouriteID = -1;
				string favouriteIDText = objString.Substring (16);

				if (int.TryParse (favouriteIDText, out _favouriteID))
				{
					Action newAction = KickStarter.actionsManager.GenerateFavouriteAction (_favouriteID);
					if (newAction != null)
					{
						Action currentAction = actionList[actionList.Count - 1];
						if (currentAction.endAction == ResultAction.Continue)
						{
							currentAction.endAction = ResultAction.Stop;
						}

						if (isAsset)
						{
							ActionListAssetEditor.AddAction (newAction, -1, windowData.targetAsset);
						}
						else
						{
							ActionListEditor.AddAction (newAction, -1, windowData.target);
						}

						newAction.nodeRect.position = menuPosition;
						newAction.isDisplayed = true;
					}
				}
			}
			else if (objString.StartsWith ("EditSource"))
			{
				foreach (Action action in actionList)
				{
					if (action.isMarked)
					{
						var script = MonoScript.FromScriptableObject (action);
						if (script != null)
						{
							AssetDatabase.OpenAsset (script);
							action.isMarked = false;
							break;
						}
					}
				}
			}

			foreach (Action action in actionList)
			{
				action.SkipActionGUI (actionList, false);
			}

			if (doUndo)
			{
				if (isAsset)
				{
					Undo.RecordObjects (new UnityEngine.Object [] { windowData.targetAsset }, objString);
					Undo.RecordObjects (windowData.targetAsset.actions.ToArray (), objString);
				}
				else
				{
					Undo.RecordObjects (new UnityEngine.Object [] { windowData.target }, objString);
					Undo.RecordObjects (windowData.target.actions.ToArray (), objString);
				}
				Undo.CollapseUndoOperations (Undo.GetCurrentGroup ());
			}

			if (isAsset)
			{
				EditorUtility.SetDirty (windowData.targetAsset);
			}
			else
			{
				EditorUtility.SetDirty (windowData.target);
			}

			if (objString.StartsWith ("Delete selected") && Actions.Count == 1)
			{
				ScrollPosition = Vector2.zero;
			}

			UpdateScrollLimits ();
			ScrollPosition = ScrollPosition;
			Repaint ();
		}


		private void PerformSocketCallBack (string objString)
		{
			if (objString == "Focus on linked Action")
			{
				FocusOnAction (focusActionIndex);
			}
		}
		
		
		private void AutoArrange (bool isAsset)
		{
			isAutoArranging = true;

			List<Action> actionList = (isAsset) ? windowData.targetAsset.actions : windowData.target.actions;
			foreach (Action action in actionList)
			{
				// Fix reconnection error from non-displayed Actions
				if (action.endAction == ResultAction.Skip || action.numSockets == 2 || action is ActionCheckMultiple || action is ActionParallel)
				{
					if (isAsset)
					{
						action.SkipActionGUI (actionList, false);
					}
					else
					{
						action.SkipActionGUI (actionList, false);
					}
				}

				action.isMarked = true;
				if (actionList.IndexOf (action) != 0)
				{
					action.nodeRect.x = action.nodeRect.y = -10;
				}
			}
			
			DisplayActionsInEditor _display = DisplayActionsInEditor.ArrangedVertically;
			if (AdvGame.GetReferences ().actionsManager && AdvGame.GetReferences ().actionsManager.displayActionsInEditor == DisplayActionsInEditor.ArrangedHorizontally)
			{
				_display = DisplayActionsInEditor.ArrangedHorizontally;
			}

			ArrangeFromIndex (actionList, 0, 0, 14, _display);

			int i=1;
			float maxValue = 0f;
			foreach (Action _action in actionList)
			{
				if (_display == DisplayActionsInEditor.ArrangedVertically)
				{
					maxValue = Mathf.Max (maxValue, _action.nodeRect.y + _action.nodeRect.height);
				}
				else
				{
					maxValue = Mathf.Max (maxValue, _action.nodeRect.x);
				}
			}

			foreach (Action _action in actionList)
			{
				if (_action.isMarked)
				{
					// Wasn't arranged
					if (_display == DisplayActionsInEditor.ArrangedVertically)
					{
						_action.nodeRect.x = 14;
						_action.nodeRect.y = maxValue + 14*i;
						ArrangeFromIndex (actionList, actionList.IndexOf (_action), 0, 14, _display);
					}
					else
					{
						_action.nodeRect.x = maxValue + 350*i;
						_action.nodeRect.y = 14;
						ArrangeFromIndex (actionList, actionList.IndexOf (_action), 0, 14, _display);
					}
					_action.isMarked = false;
					i++;
				}
			}

			isAutoArranging = false;

			UpdateScrollLimits ();
			ScrollPosition = Vector2.zero;
		}
		
		
		private void ArrangeFromIndex (List<Action> actionList, int i, int depth, float minValue, DisplayActionsInEditor _display)
		{
			while (i > -1 && actionList.Count > i)
			{
				Action _action = actionList[i];
				
				if (i > 0 && _action.isMarked)
				{
					if (_display == DisplayActionsInEditor.ArrangedVertically)
					{
						_action.nodeRect.x = 14 + (350 * depth);

						// Find top-most Y position
						float yPos = minValue;
						bool doAgain = true;
						
						while (doAgain)
						{
							int numChanged = 0;
							foreach (Action otherAction in actionList)
							{
								if (otherAction != _action && Mathf.Approximately (otherAction.nodeRect.x, _action.nodeRect.x) && otherAction.nodeRect.y >= yPos)
								{
									yPos = otherAction.nodeRect.y + otherAction.nodeRect.height + 30f;
									numChanged ++;
								}
							}
							
							if (numChanged == 0)
							{
								doAgain = false;
							}
						}
						_action.nodeRect.y = yPos;
					}
					else
					{
						_action.nodeRect.y = 14 + (260 * depth);

						// Find left-most X position
						float xPos = minValue + 350;
						bool doAgain = true;
						
						while (doAgain)
						{
							int numChanged = 0;
							foreach (Action otherAction in actionList)
							{
								if (otherAction != _action && Mathf.Approximately (otherAction.nodeRect.x, xPos) && Mathf.Approximately (otherAction.nodeRect.y, _action.nodeRect.y))
								{
									xPos += 350;
									numChanged ++;
								}
							}
							
							if (numChanged == 0)
							{
								doAgain = false;
							}
						}
						_action.nodeRect.x = xPos;
					}
				}
				
				if (_action.isMarked == false)
				{
					return;
				}
				
				_action.isMarked = false;

				float newMinValue = 0f;
				if (_display == DisplayActionsInEditor.ArrangedVertically)
				{
					newMinValue = _action.nodeRect.y + _action.nodeRect.height + 30f;
				}
				else
				{
					newMinValue = _action.nodeRect.x;
				}
				
				if (_action is ActionCheckMultiple)
				{
					ActionCheckMultiple _actionCheckMultiple = (ActionCheckMultiple) _action;
					
					for (int j=_actionCheckMultiple.endings.Count-1; j>=0; j--)
					{
						ActionEnd ending = _actionCheckMultiple.endings [j];
						if (j >= 0)
						{
							if (ending.resultAction == ResultAction.Skip)
							{
								int newDepth = depth;
								for (int k = 0; k<j; k++)
								{
									ActionEnd prevEnding = _actionCheckMultiple.endings [k];
									if (prevEnding.resultAction == ResultAction.Continue || 
									    (prevEnding.resultAction == ResultAction.Skip && prevEnding.skipAction != i))
									{
										newDepth ++;
									}
								}

								ArrangeFromIndex (actionList, actionList.IndexOf (ending.skipActionActual), newDepth, newMinValue, _display);
							}
							else if (ending.resultAction == ResultAction.Continue)
							{
								ArrangeFromIndex (actionList, i+1, depth+j, newMinValue, _display);
							}
						}
					}
				}
				if (_action is ActionParallel)
				{
					ActionParallel _ActionParallel = (ActionParallel) _action;
					
					for (int j=_ActionParallel.endings.Count-1; j>=0; j--)
					{
						ActionEnd ending = _ActionParallel.endings [j];
						if (j >= 0) // Want this to run for all, now
						{
							if (ending.resultAction == ResultAction.Skip)
							{
								ArrangeFromIndex (actionList, actionList.IndexOf (ending.skipActionActual), depth+j, newMinValue, _display);
							}
							else if (ending.resultAction == ResultAction.Continue)
							{
								ArrangeFromIndex (actionList, i+1, depth+j, newMinValue, _display);
							}
						}
						else
						{
							if (ending.resultAction == ResultAction.Skip)
							{
								i = actionList.IndexOf (ending.skipActionActual);
							}
							else if (ending.resultAction == ResultAction.Continue)
							{
								i++;
							}
							else
							{
								i = -1;
							}
						}
					}
				}
				else if (_action is ActionCheck)
				{
					ActionCheck _actionCheck = (ActionCheck) _action;

					if (_actionCheck.resultActionFail == ResultAction.Stop || _actionCheck.resultActionFail == ResultAction.RunCutscene)
					{
						if (_actionCheck.resultActionTrue == ResultAction.Skip)
						{
							i = actionList.IndexOf (_actionCheck.skipActionTrueActual);
						}
						else if (_actionCheck.resultActionTrue == ResultAction.Continue)
						{
							i++;
						}
						else
						{
							i = -1;
						}
					}
					else
					{
						if (_actionCheck.resultActionTrue == ResultAction.Skip)
						{
							ArrangeFromIndex (actionList, actionList.IndexOf (_actionCheck.skipActionTrueActual), depth+1, newMinValue, _display);
						}
						else if (_actionCheck.resultActionTrue == ResultAction.Continue)
						{
							ArrangeFromIndex (actionList, i+1, depth+1, newMinValue, _display);
						}
						
						if (_actionCheck.resultActionFail == ResultAction.Skip)
						{
							i = actionList.IndexOf (_actionCheck.skipActionFailActual);
						}
						else if (_actionCheck.resultActionFail == ResultAction.Continue)
						{
							i++;
						}
						else
						{
							i = -1;
						}
					}
				}
				else
				{
					if (_action.endAction == ResultAction.Skip)
					{
						i = actionList.IndexOf (_action.skipActionActual);
					}
					else if (_action.endAction == ResultAction.Continue)
					{
						i++;
					}
					else
					{
						i = -1;
					}
				}
			}
		}


		public Vector2 CentrePosition
		{
			get
			{
				return (scrollPosition + (new Vector2 (CanvasWidth, CanvasHeight) / 2f));
			}
		}


		public List<Action> GetActions ()
		{
			if (windowData.targetAsset != null)
			{
				return windowData.targetAsset.actions;
			}
			else if (windowData.target != null)
			{
				return windowData.target.actions;
			}
			return null;
		}


		private void DrawGrid ()
		{
			DrawGrid (20, 0.2f, Color.gray);
			DrawGrid (100, 0.4f, Color.gray);
		}


		private void DrawEmptyNotice ()
		{
			Vector2 size = new Vector2 (Mathf.Min (350f, position.size.x), Mathf.Min (70f, position.size.y));
			Rect rect = new Rect (Vector2.zero, size);
			rect.center = position.size / 2f;
			GUI.BeginGroup (rect);
			GUILayout.Label ("No ActionList assigned", CustomStyles.subHeader);
			GUILayout.Label ("Click an ActionList's Hierarchy node icon,\nor the 'Action List Editor' button in it's Inspector.");
			GUI.EndGroup ();
		}


		private void DrawGrid (float gridSpacing, float gridOpacity, Color gridColor)
		{
			gridSpacing *= zoom;

			int widthDivs = Mathf.CeilToInt (CanvasWidth / gridSpacing);
			int heightDivs = Mathf.CeilToInt (CanvasHeight / gridSpacing);

			Handles.BeginGUI ();
			Handles.color = new Color (gridColor.r, gridColor.g, gridColor.b, gridOpacity);

			Vector3 newOffset = new Vector3 ((-scrollPosition.x * zoom) % gridSpacing, (-scrollPosition.y * zoom) % gridSpacing, 0);

			for (int i = 0; i < widthDivs + 1; i++)
			{
				Vector3 start = new Vector3 (gridSpacing * i, -gridSpacing, 0) + newOffset;
				Vector3 end = new Vector3 (gridSpacing * i, CanvasHeight + gridSpacing, 0f) + newOffset;

				if (end.x > CanvasWidth) continue;
				if (end.y > CanvasHeight) end.y = CanvasHeight;

				end.y -= 18;

				Handles.DrawLine (start, end);
			}

			for (int j = 0; j < heightDivs + 1; j++)
			{
				Vector3 start = new Vector3 (-gridSpacing, gridSpacing * j, 0) + newOffset;
				Vector3 end = new Vector3 (CanvasWidth + gridSpacing, gridSpacing * j, 0f) + newOffset;

				if (end.x > CanvasWidth) end.x = CanvasWidth;
				if (end.y > CanvasHeight) continue;

				start.y -= 18;
				end.y -= 18;

				Handles.DrawLine (start, end);
			}

			Handles.color = Color.white;
			Handles.EndGUI ();
		}


		private void UseEvent (Event e)
		{
			e.Use ();
		}


		private Vector2 ScrollPosition
		{
			get
			{
				return scrollPosition;
			}
			set
			{
				Vector2 oldValue = scrollPosition;
				scrollPosition.x = Mathf.Clamp (value.x, 0f, scrollLimits.x / Zoom);
				scrollPosition.y = Mathf.Clamp (value.y, 0f, scrollLimits.y / Zoom);
				if (oldValue != scrollPosition) GUI.changed = true;
			}
		}


		private float Zoom
		{
			get
			{
				return zoom;
			}
			set
			{
				float oldValue = zoom;
				zoom = Mathf.Clamp (value, zoomMin, zoomMax);
				if (oldValue != zoom) GUI.changed = true;
			}
		}


		private List<Action> Actions
		{
			get
			{
				if (windowData.targetAsset != null)
				{
					return windowData.targetAsset.actions;
				}
				if (windowData.target != null)
				{
					return windowData.target.actions;
				}
				return null;
			}
		}


		private int NumActionsMarked
		{
			get
			{
				int i = 0;
				foreach (Action action in Actions)
				{
					if (action != null && action.isMarked)
					{
						i++;
					}
				}

				return i;
			}
		}


		private float CanvasWidth
		{
			get
			{
				float offset = 0f;
				if (showProperties)
				{
					offset = propertiesBoxWidth - 7f;
				}
				if (CanDrawVerticalScrollbar)
				{
					offset += 20f;
				}
				return position.width - offset;
			}
		}


		private float CanvasHeight
		{
			get
			{
				float offset = 0f;
				if (CanDrawHorizontalScrollbar)
				{
					offset = 20f;
				}
				return position.height - offset;
			}
		}


		private bool CanDrawHorizontalScrollbar
		{
			get
			{
				float width = (showProperties) ? (position.width - propertiesBoxWidth + 7f) : position.width;
				float xScrollSize = width / (width + scrollLimits.x);
				return (xScrollSize < 1f);
			}
		}


		private bool CanDrawVerticalScrollbar
		{
			get
			{
				float yScrollSize = position.height / (position.height + scrollLimits.y);
				return (yScrollSize < 1f);
			}
		}

	}

}