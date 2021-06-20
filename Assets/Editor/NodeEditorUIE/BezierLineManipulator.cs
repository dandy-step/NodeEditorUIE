using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;

//manipulator for assigning links and drawing them when dragging from a vert
class BezierLineManipulator : MouseManipulator {
    bool enabled = false;
    Vector2 startPos;
    IMGUIContainer mouseContainer;
    NodeContainer nodeContainer;
    Vector2 currPos;
    NodeVert hoveredVert = null;

    public BezierLineManipulator() {
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    protected override void RegisterCallbacksOnTarget() {
        target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        target.RegisterCallback<KeyDownEvent>(DeleteLink);
    }

    protected override void UnregisterCallbacksFromTarget() {
        target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
        target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        target.UnregisterCallback<KeyDownEvent>(DeleteLink);
    }

    public void DeleteLink(KeyDownEvent evt) {      //handles deletion of assigned vert links on key press
        if ((evt.keyCode == KeyCode.Escape) || (evt.keyCode == KeyCode.Delete)) {
            ((NodeVert)target).LinkTo(null);
            hoveredVert = null;
            target.SendEvent(new MouseUpEvent());
        }
    }

    public void OnGUIDraw() {
        Handles.DrawBezier(startPos, currPos, startPos, currPos, Color.white * 0.88f, null, 5f);
    }

    public void OnMouseDown(MouseDownEvent evt) {
        if (CanStartManipulation(evt)) {
            enabled = true;
            target.CaptureMouse();
            evt.StopImmediatePropagation();
            mouseContainer = new IMGUIContainer();      //create new IMGUIContainer to draw the bezier elements
            mouseContainer.style.width = EditorWindow.GetWindow<NodeEditorUIE>().nodeContainer.layout.width;
            mouseContainer.style.height = EditorWindow.GetWindow<NodeEditorUIE>().nodeContainer.layout.height;
            mouseContainer.style.position = Position.Absolute;
            mouseContainer.BringToFront();
            mouseContainer.style.top = 0;
            mouseContainer.style.left = 0;
            mouseContainer.onGUIHandler = OnGUIDraw;
            nodeContainer = EditorWindow.GetWindow<NodeEditorUIE>().nodeContainer;
            nodeContainer.Add(mouseContainer);
            currPos = nodeContainer.WorldToLocal(evt.originalMousePosition);
            startPos = nodeContainer.WorldToLocal(target.worldBound.center);

            //disable all verts incompatible by type or ID with the vert we started the interaction with
            foreach (VisualElement v in nodeContainer.Children()) {
                if (nodeContainer.IsNodeType(v)) {
                    NodeVert[] vertList = ((BaseNode)v).GetVerts();
                    for (int i = 0; i < vertList.Length; i++) {
                        if (!((NodeVert)target).IsCompatibleWith(vertList[i])) {
                            vertList[i].SetEnabled(false);
                        }
                    }
                }
            } 
        }
    }

    public void OnMouseMove(MouseMoveEvent evt) {
        if (enabled) {
            currPos = nodeContainer.WorldToLocal(evt.originalMousePosition);
            target.MarkDirtyRepaint();

            List<VisualElement> nodeList = nodeContainer.Children().ToList();
            hoveredVert = null;
            for (int i = 0; i < nodeList.Count; i++) {
                if (nodeList[i].worldBound.Contains(evt.mousePosition)) {
                    if (nodeContainer.IsNodeType(nodeList[i])) {                        //are we inside a node?
                        NodeVert[] verts = ((BaseNode)nodeList[i]).GetVerts();          //get node verts
                        for (int v = 0; v < verts.Length; v++) {
                            if ((verts[v].worldBound.Contains(evt.mousePosition))) {    //but are we inside a vert?
                                hoveredVert = verts[v];                                 //get hovered vert
                            }
                        }
                    }
                }
            }
        }
    }

    public void OnMouseUp(MouseUpEvent evt) {
        enabled = false;
        target.ReleaseMouse();
        EditorWindow.GetWindow<NodeEditorUIE>().nodeContainer.Remove(mouseContainer);
        EditorWindow.GetWindow<NodeEditorUIE>().currActiveVert = null;

        //link to hovered vert, update linkFrom/linkTo variables
        if (hoveredVert != null) {
            ((NodeVert)target).LinkTo(hoveredVert);
            hoveredVert = null;
        }

        //re-enable previously disabled verts
        foreach (VisualElement v in nodeContainer.Children()) {
            if (nodeContainer.IsNodeType(v)) {
                NodeVert[] vertList = ((BaseNode)v).GetVerts();
                for (int i = 0; i < vertList.Length; i++) {
                    vertList[i].SetEnabled(true);
                }
            }
        }
    }
}
