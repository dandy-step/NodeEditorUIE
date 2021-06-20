using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;

//custom manipulator to enable dragging and reordering the elements in the list, which wasn't a built-in manipulator for UIElements as of Unity 2019.1 - handles highlighting the item being dragged over and repositioning of items
public class DragManipulator : MouseManipulator {
    public VisualElement draggedElement = null;
    public Image floatingElement = null;
    public VisualElement hoveredElement = null;
    private VisualElement lastHoveredElement = null;
    public Tuple<StyleLength, StyleLength, StyleColor, StyleFloat> originalHoverElementMargins;
    private Vector2 dragOrigin = Vector2.zero;
    public bool enabled, distanceTriggered;

    public DragManipulator() {
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    protected override void RegisterCallbacksOnTarget() {
        target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.NoTrickleDown);
        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        target.RegisterCallback<KeyDownEvent>(EscapeBailout, TrickleDown.NoTrickleDown);
    }

    protected override void UnregisterCallbacksFromTarget() {
        target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.NoTrickleDown);
        target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        target.UnregisterCallback<KeyDownEvent>(EscapeBailout, TrickleDown.NoTrickleDown);
    }

    public void EscapeBailout(KeyDownEvent evt) {
        if ((enabled) && (evt.keyCode == KeyCode.Escape)) {
            draggedElement.ReleaseMouse();
            if (floatingElement != null) {
                floatingElement.parent.Remove(floatingElement);
                floatingElement = null;
            }

            if (lastHoveredElement != null) {
                lastHoveredElement.EnableInClassList("HoverStyle", false);
                lastHoveredElement = null;
            }

            if (hoveredElement != null) {
                hoveredElement.EnableInClassList("HoverStyle", false);
                hoveredElement = null;
            }

            enabled = false;
        }
    }

    public void OnMouseDown(MouseDownEvent evt) {     
        if ((CanStartManipulation(evt)) && (CanStopManipulation(evt))) {
            if ((evt.propagationPhase == PropagationPhase.BubbleUp) && (!evt.isImmediatePropagationStopped)) {    //ignore events intended for specific UI classes
                enabled = true;
                dragOrigin = evt.mousePosition;
                draggedElement = target;
                evt.StopImmediatePropagation();
                //evt.target.CaptureMouse();
            } else {
                Debug.Log("ignoring drag, event phase: " + evt.propagationPhase);
            }
        }
    }

    public void OnMouseMove(MouseMoveEvent evt) {
        if (enabled) {
            draggedElement.CaptureMouse();
            if (!distanceTriggered) {
                if (Vector2.Distance(dragOrigin, evt.mousePosition) > 15f) {
                    distanceTriggered = true;
                } else {
                    return;
                }
            }

            if (floatingElement == null) {
                floatingElement = new Image();

                Vector2 pos;
                int width = (int)draggedElement.contentRect.width;
                int height = (int)draggedElement.contentRect.height;
                Texture2D capture = new Texture2D(width, height);

                pos = EditorWindow.focusedWindow.position.position + draggedElement.worldBound.position;
                Color[] pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(pos, width, height);
                capture.SetPixels(pixels);
                capture.Apply();

                floatingElement.image = capture;
                floatingElement.style.opacity = 0.25f;
                floatingElement.style.position = Position.Absolute;
                EditorWindow.focusedWindow.rootVisualElement.Add(floatingElement);
            }

            floatingElement.style.left = evt.mousePosition.x;
            floatingElement.style.top = evt.mousePosition.y;

            foreach (VisualElement child in draggedElement.parent.Children()) {
                if (child.ContainsPoint(child.WorldToLocal(new Vector2(evt.mousePosition.x, evt.mousePosition.y)))) {
                    hoveredElement = child;
                    if (originalHoverElementMargins == null) {
                        originalHoverElementMargins = new Tuple<StyleLength, StyleLength, StyleColor, StyleFloat>(hoveredElement.style.paddingTop, hoveredElement.style.paddingBottom, hoveredElement.style.backgroundColor, hoveredElement.style.opacity);
                    }
                    break;
                }
            }

            if ((lastHoveredElement != null) && (hoveredElement != lastHoveredElement)) {
                lastHoveredElement.EnableInClassList("HoverStyle", false);
                hoveredElement.AddToClassList("HoverStyle");
                hoveredElement.EnableInClassList("HoverStyle", true);
            }

            lastHoveredElement = hoveredElement;
        }
    }

    public void OnMouseUp(MouseUpEvent evt) {
        if ((enabled) && (lastHoveredElement != null)) {
            lastHoveredElement.EnableInClassList("HoverStyle", false);

            if (lastHoveredElement != draggedElement) {
                int originalIndex = draggedElement.parent.IndexOf(draggedElement);
                int newIndex = draggedElement.parent.IndexOf(lastHoveredElement);
                VisualElement draggedParent = draggedElement.parent;

                draggedParent.Remove(draggedElement);
                draggedParent.Insert(newIndex, draggedElement);
                VisualElement newEntry = draggedParent.ElementAt(newIndex);
                newEntry.experimental.animation.Start(0.25f, 1f, 1250, (x, y) => { newEntry.style.opacity = new StyleFloat(y); });
            }

            floatingElement.parent.Remove(floatingElement);
        }

        dragOrigin = Vector2.zero;
        draggedElement.ReleaseMouse();
        floatingElement = null;
        draggedElement = null;
        hoveredElement = null;
        lastHoveredElement = null;
        enabled = false;
        distanceTriggered = false;
        evt.StopImmediatePropagation();
    }
}