using UnityEngine;
using UnityEngine.UIElements;

class NodeDragManipulator : MouseManipulator {
    bool dragging = false;
    VisualElement parent = null;
    Vector2 mousePosStartOffset;

    public NodeDragManipulator() {
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    protected override void RegisterCallbacksOnTarget() {
        target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.NoTrickleDown);
        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        //target.RegisterCallback<KeyDownEvent>(EscapeBailout, TrickleDown.NoTrickleDown);
    }

    protected override void UnregisterCallbacksFromTarget() {
        target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.NoTrickleDown);
        target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        //target.UnregisterCallback<KeyDownEvent>(EscapeBailout, TrickleDown.NoTrickleDown);
    }

    public void OnMouseDown(MouseDownEvent evt) {
        if (CanStartManipulation(evt)) {
            target.BringToFront();
            parent = target.parent;
            evt.StopImmediatePropagation();
            dragging = true;
            mousePosStartOffset = evt.localMousePosition;
        }
    }

    public void OnMouseMove(MouseMoveEvent evt) {
        if (dragging) {
            evt.StopImmediatePropagation();
            target.style.left = target.GetFirstAncestorOfType<VisualElement>().WorldToLocal(evt.mousePosition).x - mousePosStartOffset.x;
            target.style.top = target.GetFirstAncestorOfType<VisualElement>().WorldToLocal(evt.mousePosition).y - mousePosStartOffset.y;
            target.CaptureMouse();
        }
    }

    public void OnMouseUp(MouseUpEvent evt) {
        if (dragging) {
            dragging = false;
            VisualElement container = target.GetFirstAncestorOfType<NodeContainer>();
            if (container != null) {
                ((NodeContainer)container).UpdateDimensions();
            } else {
                Debug.Log("Couldn't find a parent of type NodeContainer for the node you dragged.");
            }

            target.ReleaseMouse();
        }
    }
}
