using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;

//static class to globally access the core paths for this editor
public static class NodeEditorUIEPaths {
    public static readonly string EDITOR_ROOT_PATH = "Assets/Editor/NodeEditorUIE/";
    public static readonly string EDITOR_UXML_USS_ASSETS_ROOT_PATH = EDITOR_ROOT_PATH + "UXML & USS/";
    public static readonly string EDITOR_GRAPHIC_ASSETS_ROOT_PATH = EDITOR_ROOT_PATH + "Graphics/";
    public static readonly string NODEMAP_SAVE_DIRECTORY_PATH = "Assets/Data/Nodemaps/";
    public static readonly string NODEMAP_SERIALIZE_PATH = EDITOR_ROOT_PATH + "Data/nodeserialize.nmap";
}

public class NodeEditorUIE : EditorWindow, ISerializationCallbackReceiver {
    VisualElement root;
    VisualElement buttonBar;
    public NodeContainer nodeContainer;
    public NodeVert currActiveVert;
    public IMGUIContainer mainGUIContainer = null;
    bool hasSerializationData = false;
    bool holdingCTRL = false;
    [SerializeField] public byte[] serializedData = null;
    Vector2 windowSize = Vector2.one;

    [MenuItem("Custom Editors/NodeEditorUIE")]
    static void Init() {
        NodeEditorUIE window = GetWindow<NodeEditorUIE>();
        window.Show();
    }


    private void OnEnable() {
        //main function to set up window, creates core UI elements
        root = rootVisualElement;
        root.style.position = Position.Relative;
        root.style.flexGrow = 1;
        this.wantsMouseEnterLeaveWindow = true;

        //check for and generate missing paths
        string[] pathsToCheck = new string[] { NodeEditorUIEPaths.EDITOR_ROOT_PATH, NodeEditorUIEPaths.NODEMAP_SAVE_DIRECTORY_PATH };

        for (int i = 0; i < pathsToCheck.Length; i++) {
            if (!Directory.Exists(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + pathsToCheck[i])) {
                Debug.Log("Creating missing folder at " + (Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + pathsToCheck[i]));
                Directory.CreateDirectory(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + pathsToCheck[i]);
            }
        }

        //create buttonbar and buttons
        buttonBar = new VisualElement();
        buttonBar.style.height = 26;
        buttonBar.style.backgroundColor = Color.grey;
        buttonBar.style.justifyContent = Justify.FlexStart;
        buttonBar.style.flexDirection = FlexDirection.Row;
        root.Add(buttonBar);
        ImageButton typeButton = new ImageButton(24, AssetDatabase.LoadAssetAtPath<Texture2D>(NodeEditorUIEPaths.EDITOR_GRAPHIC_ASSETS_ROOT_PATH + "add_node.png"));
        typeButton.clickable = null;
        ContextualMenuManipulator man = new ContextualMenuManipulator(ShowTypesMenu);
        man.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse });
        typeButton.AddManipulator(man);
        buttonBar.Add(typeButton);

        buttonBar.Add(new ImageButton(() => {
            string savePath = EditorUtility.SaveFilePanel("Save to:", NodeEditorUIEPaths.NODEMAP_SAVE_DIRECTORY_PATH, "nodemap_" + EditorApplication.timeSinceStartup, "nmap");
            if (!string.IsNullOrEmpty(savePath)) {
                SaveFunction(savePath);
            }
        }, 24, AssetDatabase.LoadAssetAtPath<Texture2D>(NodeEditorUIEPaths.EDITOR_GRAPHIC_ASSETS_ROOT_PATH + "save_file.png")));

        buttonBar.Add(new ImageButton(() => {
            string loadPath = EditorUtility.OpenFilePanelWithFilters("Open nodemap:", NodeEditorUIEPaths.NODEMAP_SAVE_DIRECTORY_PATH, new string[] { "Nodemaps", "nmap" });
            if (!string.IsNullOrEmpty(loadPath)) {
                RestoreData(loadPath);
            }
        }, 24, AssetDatabase.LoadAssetAtPath<Texture2D>(NodeEditorUIEPaths.EDITOR_GRAPHIC_ASSETS_ROOT_PATH + "open_file.png")));

        buttonBar.Add(new ImageButton(() => {
            this.Close();
            EditorWindow.GetWindow<NodeEditorUIE>();
        }, 24, AssetDatabase.LoadAssetAtPath<Texture2D>(NodeEditorUIEPaths.EDITOR_GRAPHIC_ASSETS_ROOT_PATH + "new_nodemap.png")));

        nodeContainer = new NodeContainer();
        //nodeContainer.usageHints = UsageHints.GroupTransform;     //should be enabled for better performance, but buggy in Unity 2019?
        nodeContainer.Add(new EntryNode());
        root.RegisterCallback<WheelEvent>((x) => { HandleNodeZoom(x); }, TrickleDown.TrickleDown);
        ScrollView scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        scroll.contentViewport.RegisterCallback<WheelEvent>(ScrollWheelHandler);
        scroll.focusable = true;
        root.Add(scroll);
        scroll.style.flexGrow = 1;
        scroll.contentContainer.style.flexGrow = 1;
        
        scroll.Add(nodeContainer);

        //container for IMGUIContainer that draws the beziers
        VisualElement bezierElementContainer = new VisualElement();
        bezierElementContainer.style.position = Position.Absolute;
        bezierElementContainer.pickingMode = PickingMode.Ignore;
        bezierElementContainer.style.overflow = Overflow.Hidden;
        bezierElementContainer.StretchToParentSize();
        root.Add(bezierElementContainer);

        //bezier draw element
        mainGUIContainer = new IMGUIContainer();
        mainGUIContainer.style.position = Position.Absolute;

        //manually offset the container and element bounds because otherwise it ignores the layout and draws in front of other UI elements
        mainGUIContainer.style.top = new StyleLength((float)-buttonBar.style.height.value.value);
        bezierElementContainer.style.top = new StyleLength((float)buttonBar.style.height.value.value);

        mainGUIContainer.onGUIHandler = IMGUIDraw;              //set OnGUI callback
        mainGUIContainer.pickingMode = PickingMode.Ignore;
        bezierElementContainer.Add(mainGUIContainer);
        root.RegisterCallback<KeyDownEvent>(GetKeyDown, TrickleDown.TrickleDown);  //for zoom control hotkeys     
        root.RegisterCallback<KeyUpEvent>(GetKeyUp, TrickleDown.NoTrickleDown);
    }

    public void ScrollWheelHandler(WheelEvent evt) {
        //eat wheel event if we're zooming
        if (holdingCTRL) {
            evt.StopImmediatePropagation();
        }
    }

    public void GetKeyDown(KeyDownEvent evt) {
        //detect holding CTRL as hotkey for zoom
        if (evt.keyCode == KeyCode.LeftControl) {
            holdingCTRL = true;
        }
    }

    public void GetKeyUp(KeyUpEvent evt) {
        //clear holding CTRL flag
        if (evt.keyCode == KeyCode.LeftControl) {
            holdingCTRL = false;
        }
    }

    public void ShowTypesMenu(ContextualMenuPopulateEvent evt) {
        //get all subtypes of BaseNode from the Assembly
        Type[] nodeTypes = System.Reflection.Assembly.GetAssembly(typeof(BaseNode)).GetTypes().Where(x => x.IsClass && x.IsSubclassOf(typeof(BaseNode))).ToArray();

        //populate contextual menu with the string for each type
        for (int i = 0; i < nodeTypes.Length; i++) {
            if (nodeTypes[i] != typeof(EntryNode)) {
                evt.menu.AppendAction(nodeTypes[i].ToString(), AddNewNodeFromType, DropdownMenuAction.Status.Normal);
            }
        }
    }

    public void AddNewNodeFromType(DropdownMenuAction act) {
        object o = Activator.CreateInstance(Type.GetType(act.name));
        VisualElement elem = (VisualElement)o;
        elem.style.top = ((ScrollView)nodeContainer.GetFirstAncestorOfType<ScrollView>()).scrollOffset.y;       //add according to scroll position rather than always on top
        nodeContainer.Add((VisualElement)o);
    }

    //so it can't be manually spawned from outside the class, as there should only ever be one instance of this node, it lives here
    protected class EntryNode : BaseNode {
        public EntryNode() {
            LoadTemplateAndStyleFromName("EntryNode");
            SetTopVertContainerVisibility(false);
        }

        protected override void NodeContextualMenu(ContextualMenuPopulateEvent evt) { } //disable contextual menu for entry node
    }

    //serialize all nodes to bytes and write them to file. nodes can override and extend the save functionality for custom serialization
    public void SaveFunction(string savePath) {
        if (!string.IsNullOrEmpty(savePath)) {
            FileStream file = new FileStream(savePath, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(file);

            //nmap format: 4 byte magic number | 4 byte node count
            //data starts at +32b
            writer.Write("NMAP");
            writer.Write(nodeContainer.childCount);
            writer.Seek(32, SeekOrigin.Begin);
            for (int i = 0; i < nodeContainer.childCount; i++) {
                if (nodeContainer.IsNodeType(nodeContainer[i])) {
                    writer.Write(((BaseNode)nodeContainer[i]).SerializeNodeAsBytes());
                }
            }

            writer.Dispose();
        }
    }

    //load from file path
    public void RestoreData(string path) {
        nodeContainer.Clear();
        FileStream file = new FileStream(path, FileMode.Open);
        BinaryReader reader = new BinaryReader(file);
        RestoreData(reader.ReadBytes((int)file.Length));
        reader.Dispose();
    }

    //main load function - load the data back, create and add a blank node of the type specified in the file to the hierarchy, and then restore the values
    public void RestoreData(byte[] byteData) {
        nodeContainer.Clear();
        MemoryStream byteStream = new MemoryStream(byteData);
        BinaryReader reader = new BinaryReader(byteStream);

        string magicNumber = reader.ReadString();
        if (magicNumber == "NMAP") {
            int entryCount = reader.ReadInt32();
            if (entryCount > 0) {
                reader.BaseStream.Seek(32, SeekOrigin.Begin);
                for (int i = 0; i < entryCount; i++) {
                    Type entryType = Type.GetType(reader.ReadString());
                    if (entryType != null) {
                        object o = Activator.CreateInstance(entryType);
                        if (nodeContainer.IsNodeType((VisualElement)o)) {
                            nodeContainer.Add((VisualElement)o);
                            ((BaseNode)o).RestoreNodeFromBytes(reader);
                        }
                    }
                }
            }

            //resize nodeContainer to accomodate new data
            nodeContainer.MarkDirtyRepaint();
            nodeContainer.UpdateDimensions();
        }

        reader.Dispose();
    }

    //generates a kink value for the beziers according to the vert position on the parent node - if closer to left/right edge, it will kink in that direction, if closer to up/down edge, it will kink vertically
    Vector2 CalculateBezierKink(NodeVert vert) {
        int tolerance = 6;
        int kinkFactor = 35;
        Vector2 kink = Vector2.one;
        float XBias = (vert.GetFirstAncestorOfType<BaseNode>().worldBound.center - vert.worldBound.center).x;
        float YBias = (vert.GetFirstAncestorOfType<BaseNode>().worldBound.center - vert.worldBound.center).y;

        if (XBias > tolerance) {
            kink.x = kinkFactor;
        } else if (Mathf.Abs(XBias) > tolerance) {
            kink.x = -kinkFactor;
        } else {
            kink.x = 0;
        }

        if (YBias > tolerance) {
            kink.y = kinkFactor;
        } else if (Mathf.Abs(YBias) > tolerance) {
            kink.y = -kinkFactor;
        } else {
            kink.y = 0;
        }

        //retain the dominant kink axis, zero out the other one
        kink.x = Mathf.Abs(XBias) > Mathf.Abs(YBias) ? kink.x : 0;
        kink.y = Mathf.Abs(YBias) > Mathf.Abs(XBias) ? kink.y : 0;
        return kink;
    }

    //draw all bezier curves here - relies on Handles and needs to be redrawn, so it's on an old-fashioned IMGUIContainer wich has a OnGUI callback that runs every frame
    public void IMGUIDraw() {
        Handles.CubeHandleCap(this.GetInstanceID(), Vector3.zero, Quaternion.identity, 3f, EventType.Repaint);

        //gather all nodes, get their verts, draw if they have a link
        List<VisualElement> nodeList = nodeContainer.Children().ToList();
        for (int i = 0; i < nodeContainer.childCount; i++) {
            if (nodeContainer.IsNodeType(nodeList[i])) {
                NodeVert[] vertList = ((BaseNode)nodeList[i]).GetVerts();

                for (int v = 0; v < vertList.Length; v++) {
                    if ((!string.IsNullOrEmpty(vertList[v].linkedToNodeID)) && (!string.IsNullOrEmpty(vertList[v].linkedToVertID))) {
                        BaseNode nodeTo = nodeContainer.GetNodeByID(vertList[v].linkedToNodeID);
                        if (nodeTo != null) {
                            NodeVert[] verts = nodeTo.GetVerts();
                            foreach (NodeVert vert in verts) {
                                if (vert.id == vertList[v].linkedToVertID) {
                                    Vector2 drawFrom = root.WorldToLocal(vertList[v].worldBound.center);
                                    Vector2 drawTo = root.WorldToLocal(vert.worldBound.center);
                                    Handles.DrawBezier(drawFrom, drawTo, drawFrom - CalculateBezierKink(vertList[v]), drawTo - CalculateBezierKink(vert), ((vertList[v].GetTintColor() == Color.black) ? Color.white * 0.88f: vertList[v].GetTintColor()), null, 5f * (nodeContainer.transform.scale.x));
                                }
                            }
                        } else {
                            //clean link if node has been deleted
                            vertList[v].LinkTo(null);
                        }
                    }
                }
            }
        }
    }

    public void HandleNodeZoom(WheelEvent evt) {
        if (holdingCTRL) {
            nodeContainer.transform.scale = Vector3.Min(Vector3.one, nodeContainer.transform.scale * ((evt.delta.y > 0) ? 0.965f : 1.035f));
        }
    }

    private void OnGUI() {
        //restore serialized data here in between assembly reloads
        if (hasSerializationData) {
            RestoreData(serializedData);
            serializedData = null;
            hasSerializationData = false;
        }

        //refresh nodeContainer dimensions to account for editor window resizing
        if (new Vector2(this.position.width, this.position.height) != windowSize) {
            nodeContainer.UpdateDimensions();
            windowSize = new Vector2(this.position.width, this.position.height);
        }
    }

    private void OnDestroy() {
        //prompt for save before closing the editor, allow cancel
        switch (EditorUtility.DisplayDialogComplex("NodeEditUIE", "Save nodemap?", "Ok", "Nope", "Cancel")) {
            case 0: {
                    string savePath = EditorUtility.SaveFilePanel("Save to:", NodeEditorUIEPaths.NODEMAP_SAVE_DIRECTORY_PATH, "nodemap_" + EditorApplication.timeSinceStartup, "nmap");
                    if (!string.IsNullOrEmpty(savePath)) {
                        SaveFunction(savePath);
                    }
                    break;
                }
            case 2: {
                    NodeEditorUIE newWindow = EditorWindow.CreateInstance<NodeEditorUIE>();
                    string tempFile = Path.GetTempFileName();
                    SaveFunction(tempFile);
                    BinaryReader reader = new BinaryReader(File.OpenRead(tempFile));
                    newWindow.RestoreData(reader.ReadBytes((int)reader.BaseStream.Length));
                    reader.Dispose();
                    File.Delete(tempFile);
                    newWindow.Show();
                    break;
                }
        }
    }
       
    public void OnBeforeSerialize() {
        //generate serialization data only if there's none already assigned
        if (!hasSerializationData) {
            SaveFunction(NodeEditorUIEPaths.NODEMAP_SERIALIZE_PATH);
            BinaryReader serializationFileReader = new BinaryReader(File.OpenRead(NodeEditorUIEPaths.NODEMAP_SERIALIZE_PATH));
            serializedData = serializationFileReader.ReadBytes((int)serializationFileReader.BaseStream.Length);
            hasSerializationData = true;
        }
    }

    //required by interface
    public void OnAfterDeserialize() { }
}
