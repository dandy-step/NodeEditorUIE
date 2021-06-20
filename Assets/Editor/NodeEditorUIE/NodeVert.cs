using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;

//TODO: add factory method to invoke from UXML
public class NodeVert : VisualElement {
    public string nodeOwnerID;
    public string linkedByNodeID;
    public string linkedByVertID;
    public string linkedToNodeID;
    public string linkedToVertID;
    public string id;
    public string hoverDesc;
    bool canLinkToSelf = false;
    //bool canLinkToMultiple = false;
    Image vertTex;
    public List<Type> compatibleTypes = new List<Type>();
    public List<string> compatibleIDs = new List<string>();
    NodeContainer nodeContainer = null;
    public VisualElement hostContainer;

    public NodeVert() {
        id = Guid.NewGuid().ToString();
        vertTex = new Image();
        vertTex.style.width = 10;
        vertTex.style.height = 10;
        vertTex.image = AssetDatabase.LoadAssetAtPath<Texture2D>(NodeEditorUIEPaths.EDITOR_GRAPHIC_ASSETS_ROOT_PATH + "default_vert.png");
        vertTex.tintColor = Color.black;
        this.Add(vertTex);
        this.style.paddingLeft = 7;
        this.style.paddingRight = 7;
        this.style.paddingTop = 7;
        this.style.paddingBottom = 7;
        this.style.alignSelf = Align.Center;
        this.style.flexDirection = FlexDirection.Row;
        this.style.alignContent = Align.Center;
        this.style.alignItems = Align.Center;
        this.AddManipulator(new BezierLineManipulator());
        this.RegisterCallback<TooltipEvent>((x) => { x.tooltip = (string.IsNullOrEmpty(hoverDesc)) ? id : hoverDesc; x.rect = this.worldBound; x.StopImmediatePropagation(); });
    }

    public void Hide() {
        this.style.display = DisplayStyle.None;
    }

    public void LinkTo(NodeVert linkToVert) {
        if (linkToVert != null) {
            if (IsCompatibleWith(linkToVert)) {
                if (nodeContainer.GetNodeByID(linkToVert.nodeOwnerID) != null) {
                    linkedToNodeID = linkToVert.nodeOwnerID;
                } else {
                    Debug.Log("Detected vert with stale owner ID " + linkToVert.nodeOwnerID + "!");
                }
                linkedToVertID = linkToVert.id;
            } else {
                Debug.Log("Tried to link with incompatible vert? This should not happen!");
            }
        } else {
            linkedToNodeID = null;
            linkedToVertID = null;
        }
    }

    public bool IsCompatibleWith(NodeVert linkToVert) {
        if (nodeContainer == null) {
            nodeContainer = this.GetFirstAncestorOfType<NodeContainer>();
        }

        BaseNode targetNode = nodeContainer.GetNodeByID(linkToVert.nodeOwnerID);

        if (!canLinkToSelf) {
            if (nodeOwnerID == linkToVert.nodeOwnerID) {
                return false;
            }
        }

        if (compatibleTypes.Count > 0) {
            if (!compatibleTypes.Contains(targetNode.GetType())) {
                Debug.Log("Failed to link because of incompatible type!");
                return false;
            }
        }
        if (compatibleIDs.Count > 0) {
            if (!compatibleIDs.Contains(linkToVert.id)) {
                Debug.Log("Failed to link because of incompatible vert ID!");
                return false;
            }
        }

        return true;
    }

    public void Colorize(Color col) {
        vertTex.tintColor = col;
    }

    public Color GetTintColor() {
        return vertTex.tintColor;
    }

    public string GetID() {
        return id;
    }

    public void SetID(string _id) {
        if (_id != id) {
            //get vert list from owner and update value 
        }
    }
}
