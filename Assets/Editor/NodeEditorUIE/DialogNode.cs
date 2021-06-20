using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;

public class DialogNode : BaseNode {
    DialogEditorUIE dataEditorWindow = null;
    VisualElement preview = null;
    double doubleClickTimestamp;
    public bool useNativeEditorUI = false;

    public DialogNode() : base() {
        EnableInClassList("DialogNode", true);
        LabeledNodeVert flagVert = new LabeledNodeVert("Flag Logic");               //example
        LabeledNodeVert backgroundVert = new LabeledNodeVert("Background Texture"); //example
        backgroundVert.style.flexDirection = FlexDirection.RowReverse;
        backgroundVert.vert.compatibleTypes.Add(typeof(TextureNode));
        AddContentVertex(flagVert);
        AddContentVertex(backgroundVert);
        RefreshEditorPreview();
    }

    //default start state, empty list or no editor association
    public VisualElement EmptyContent() {
        preview = new VisualElement();
        preview.style.alignSelf = Align.Center;
        preview.style.backgroundColor = Color.black;
        TextElement emptyWarning = new TextElement();
        emptyWarning.text = "<no data!>";
        emptyWarning.style.color = Color.red;
        preview.Add(emptyWarning);
        return preview;
    }

    //ask the editor for a preview, or generate an empty one
    public void RefreshEditorPreview() {
        if (preview != null) {
            preview.parent.Remove(preview);
        }
        
        if (dataEditorWindow == null) {
            preview = EmptyContent();
        } else {
            preview = dataEditorWindow.GetNodePreview();
        }

        UpdateVertList(new GeometryChangedEvent());
        nodeContentContainer.Add(preview);
    }

    protected override void NodeContextualMenu(ContextualMenuPopulateEvent evt) {
        base.NodeContextualMenu(evt);

        if (dataEditorWindow != null) {
            evt.menu.AppendAction("Use native editor UI", (x) => { useNativeEditorUI = !useNativeEditorUI; RefreshEditorPreview(); }, (useNativeEditorUI == true) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }
    }

    protected override void DoubleClickBehaviour(MouseDownEvent evt) {
        //double click detection, open editor
        double newClickTimestamp = EditorApplication.timeSinceStartup;
        if ((newClickTimestamp - doubleClickTimestamp) <= 0.3f) {
            if (dataEditorWindow == null) {
                dataEditorWindow = EditorWindow.CreateInstance<DialogEditorUIE>();
                dataEditorWindow.nodeOwnerID = GetID();
                if (editorData != null) {
                    dataEditorWindow.RestoreDialogFile(editorData);
                }

                dataEditorWindow.Show();
                evt.StopPropagation();
            } else {
                dataEditorWindow.Focus();
                evt.StopPropagation();
            }
        } else {
            doubleClickTimestamp = newClickTimestamp;
        }
    }

    //send the data in this node back to a new editor instance
    public override void RestoreNodeFromBytes(BinaryReader reader) {
        base.RestoreNodeFromBytes(reader);
        if (editorData != null) {
            dataEditorWindow = EditorWindow.CreateInstance<DialogEditorUIE>();
            dataEditorWindow.nodeOwnerID = GetID();
            dataEditorWindow.RestoreDialogFile(editorData);
            RefreshEditorPreview();
            EditorWindow.DestroyImmediate(dataEditorWindow);
            dataEditorWindow = null;
        }
    }
}
