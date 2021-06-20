using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;

//base node class - handles ID generation, vert accounting, helper functions to add layout elements, and extensible default behaviours so each node can have their own double click/contextual menu routines
public class BaseNode : VisualElement {
    VisualElement topVertContainer;
    VisualElement bottomVertContainer;
    private TextElement idLabel;
    public VisualElement nodeContentContainer;
    TextElement text;
    BaseNode firstNode;
    //List<string> vertIDs;
    public string id;
    private List<NodeVert> verts = new List<NodeVert>();
    double doubleClickTimestamp;
    public static string TOP_VERT_DEFAULT_ID = "topVert";
    public static string BOTTOM_VERT_DEFAULT_ID = "bottomVert";
    protected byte[] editorData = null;      //byte data for the editor that this node type operates on

    //compat list: node type it can link to, vert id pattern (or type?) it can link to

    public BaseNode() {
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(NodeEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "GenericNode.uxml");
        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(NodeEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "NodeStyles.uss");
        asset.CloneTree(this);
        this.styleSheets.Add(styleSheet);
        this.style.position = Position.Absolute;
        this.AddManipulator(new NodeDragManipulator());
        this.AddManipulator(new ContextualMenuManipulator(NodeContextualMenu));
        topVertContainer = this.Q<VisualElement>("topVertContainer");
        bottomVertContainer = this.Q<VisualElement>("bottomVertContainer");
        nodeContentContainer = this.Q<VisualElement>("nodeContentContainer");
        id = Guid.NewGuid().ToString();
        
        AddTopVertex(TOP_VERT_DEFAULT_ID);
        AddBottomVertex(BOTTOM_VERT_DEFAULT_ID);
        this.RegisterCallback<MouseDownEvent>(DoubleClickBehaviour, TrickleDown.TrickleDown);
        nodeContentContainer.RegisterCallback<GeometryChangedEvent>(UpdateVertList);
        List<NodeVert> _verts = new List<NodeVert>();
        GetVerts(this, _verts);     //refresh verts for verts added after base class constructor
        verts = _verts;
    }
    
    //helper function to automatically fetch the UXML and set the style class based on the name, given that it's in the default UXML directory
    protected void LoadTemplateAndStyleFromName(string name) {
        VisualTreeAsset nodeTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(NodeEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + name + ".uxml");
        StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(NodeEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "NodeStyles.uss");
        if ((nodeTemplate != null) && (style != null)) {
            nodeTemplate.CloneTree(nodeContentContainer);
            EnableInClassList(name, true);
        } else {
            Debug.Log("Failed to load UXML or USS for " + name + "!");
        }
    }

    protected virtual void NodeContextualMenu(ContextualMenuPopulateEvent evt) {
        evt.menu.AppendAction("Delete node", (x) => {  DeleteNode(); }, DropdownMenuAction.Status.Normal);
    }

    protected void DeleteNode() {
        NodeContainer nodeContainer = this.GetFirstAncestorOfType<NodeContainer>();

        for (int i = 0; i < verts.Count; i++) {
            if (!string.IsNullOrEmpty(verts[i].linkedToNodeID)) {
                nodeContainer.GetNodeByID(verts[i].linkedToNodeID).GetVertByID(verts[i].linkedToVertID).linkedByVertID = "";
                nodeContainer.GetNodeByID(verts[i].linkedToNodeID).GetVertByID(verts[i].linkedToVertID).linkedByNodeID = "";
            }

            if (!string.IsNullOrEmpty(verts[i].linkedByNodeID)) {
                nodeContainer.GetNodeByID(verts[i].linkedByNodeID).GetVertByID(verts[i].linkedByVertID).linkedToVertID = "";
                nodeContainer.GetNodeByID(verts[i].linkedByNodeID).GetVertByID(verts[i].linkedByVertID).linkedToNodeID = "";
            }
        }

        this.RemoveFromHierarchy();
    }

    //update vert list on geometry update
    public void UpdateVertList(GeometryChangedEvent evt) {
        List<VisualElement> children = nodeContentContainer.Children().ToList();
        List<NodeVert> vertList = new List<NodeVert>();
        GetVerts(this, vertList);
        verts = vertList;
        foreach (NodeVert vert in verts) {
            vert.nodeOwnerID = this.id;
        }
    }

    //hide/restore top or bottom containers for verts
    public void SetTopVertContainerVisibility(bool state) {
        topVertContainer.style.display = (state == true) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetBottomVertContainerVisibility(bool state) {
        bottomVertContainer.style.display = (state == true) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    //recursive helper function that retrieves each child of type NodeVert and adds it to a passed list
    public void GetVerts(VisualElement elem, in List<NodeVert> vertList) {
        List<VisualElement> children = elem.Children().ToList();
        for (int i = 0; i < children.Count; i++) {
            if (children[i].GetType() == typeof(NodeVert)) {
                vertList.Add((NodeVert)children[i]);
            } else {
                if (children[i].childCount > 0) {
                    GetVerts(children[i], vertList);
                }
            }
        }
    }

    public string GetID() {
        return id;
    }

    public void SetID(string _id) {
        id = _id;
    }

    public enum VertType {
        TOP_VERT,
        BOTTOM_VERT,
        CONTENT_VERT
    }

    protected virtual void DoubleClickBehaviour(MouseDownEvent evt) { }

    public void AddTopVertex(string vertID) {
        if (GetVertByID(vertID) == null) {
            NodeVert vert = new NodeVert();
            vert.id = vertID;
            vert.hostContainer = topVertContainer;
            vert.nodeOwnerID = id;
            topVertContainer.Add(vert);
            verts.Add((NodeVert)topVertContainer.ElementAt(topVertContainer.IndexOf(vert)));
        }
    }

    public void AddBottomVertex(string vertID) {
        if (GetVertByID(vertID) == null) {
            NodeVert vert = new NodeVert();
            vert.id = vertID;
            vert.hostContainer = bottomVertContainer;
            vert.nodeOwnerID = id;
            bottomVertContainer.Add(vert);
            verts.Add((NodeVert)bottomVertContainer.ElementAt(bottomVertContainer.IndexOf(vert)));
        }
    }

    //handles individual vertex, manually created and passed to this function
    public void AddContentVertex(NodeVert vert) {
        if (GetVertByID(vert.id) == null) {
            vert.nodeOwnerID = id;
            vert.hostContainer = contentContainer;
            nodeContentContainer.Add(vert);
            verts.Add(((NodeVert)nodeContentContainer[nodeContentContainer.IndexOf(vert)]));
        }
    }

    //same, but for LabeledNodeVert
    public void AddContentVertex(LabeledNodeVert labelVert) {
        if (GetVertByID(labelVert.vert.id) == null) {
            labelVert.vert.nodeOwnerID = id;
            labelVert.vert.hostContainer = contentContainer;
            nodeContentContainer.Add(labelVert);
            verts.Add(labelVert.vert);
        }
    }
    
    //adds node to a specific, known container
    public void AddVertToContainer(NodeVert vert, string container) {
        VisualElement containerVE = this.Q<VisualElement>(container);

        if (containerVE != null) {
            vert.hostContainer = containerVE;
            containerVE.Add(vert);
            verts.Add(((NodeVert)containerVE[containerVE.IndexOf(vert)]));
        } else {
            Debug.Log("Failed to add vert to named container " + container);
        }
    }

    //sets disabled style to vertex - used when dragging, to indicate valid node link options
    public void DisableVertex(string id) {
        NodeVert foundVert = verts.Find(x => x.id == id);
        if (foundVert != null) {
            foundVert.SetEnabled(false);
            foundVert.pickingMode = PickingMode.Ignore;
        } else {
            Debug.Log("Failed to disable vert with id " + id);
        }
    }

    public NodeVert GetVertByID(string id) {
        return verts.Find((x) => x.id == id);
    }

    public virtual NodeVert[] GetVerts() {
        return verts.ToArray();
    }

    public void SetEditorData(byte[] data) {
        editorData = data;
    }

    //default save function - in order to serialize custom data, override this method and do it before calling the base method (or append the data)
    public virtual byte[] SerializeNodeAsBytes() {
        long sizeValuePosition = 0;
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(this.GetType().ToString());                                //write node type
        writer.Write((editorData != null) ? editorData.Length : 0);             //write the length of editorData
        writer.Write(this.layout.x);                                            //write X/Y node coords
        writer.Write(this.layout.y);
        sizeValuePosition = writer.BaseStream.Position;                         //store position of data start to calculate size later
        writer.Seek(sizeof(int), SeekOrigin.Current);                           //skip 4 bytes
        writer.Write(this.id);                                                  //write node ID         
        writer.Write(verts.Count);                                              //write vert count
        for (int i = 0; i < verts.Count; i++) {                                 //write individual vert data: id, container, links
            writer.Write(verts[i].id);
            writer.Write((string.IsNullOrEmpty(verts[i].hostContainer.name)) ? "" : verts[i].hostContainer.name);
            writer.Write(verts[i].linkedByNodeID != null ? verts[i].linkedByNodeID : "");
            writer.Write(verts[i].linkedByVertID != null ? verts[i].linkedByVertID : "");
            writer.Write(verts[i].linkedToNodeID != null ? verts[i].linkedToNodeID : "");
            writer.Write(verts[i].linkedToVertID != null ? verts[i].linkedToVertID : "");
        }

        long endValuePosition = writer.BaseStream.Position;

        if ((editorData != null) && (editorData.Length > 0)) {
            writer.Write(editorData);
        }

        writer.Seek((int)sizeValuePosition, SeekOrigin.Begin);
        writer.Write((int)(endValuePosition - sizeValuePosition));              //write size
        byte[] data = stream.ToArray();
        writer.Dispose();
        return data;
    }

    //restore the data
    public virtual void RestoreNodeFromBytes(BinaryReader reader) {
        this.UpdateVertList(new GeometryChangedEvent());                    //update vert list at start, we call GetVertByID later
        int dataLength = reader.ReadInt32();                                //read size of editorData
        this.style.left = reader.ReadSingle();                              //restore position of node
        this.style.top = reader.ReadSingle();
        reader.ReadInt32();
        SetID(reader.ReadString());                                         //read ID
        int vertCount = reader.ReadInt32();                                 //restore each vert, create new one if it doesn't exist
        for (int i = 0; i < vertCount; i++) {
            string vertID = reader.ReadString();
            string vertHostContainer = reader.ReadString();
            NodeVert vert = this.GetVertByID(vertID);
            if (vert == null) {
                vert = new NodeVert();
                vert.id = vertID;
                AddVertToContainer(vert, vertHostContainer);
            }

            vert.linkedByNodeID = reader.ReadString();
            vert.linkedByVertID = reader.ReadString();
            vert.linkedToNodeID = reader.ReadString();
            vert.linkedToVertID = reader.ReadString();
        }

        this.UpdateVertList(new GeometryChangedEvent());
        if (dataLength > 0) {
            editorData = reader.ReadBytes(dataLength);                  //if we had editorData, read it back
        }
    }
}
