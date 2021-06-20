using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;

public class TextureNode : BaseNode {
    ObjectField baseTexField = null;
    ObjectField secTexField = null;

    public TextureNode() {
        //Example: create node UI from code, with no UXML or USS asset associated

        //hide default verts and containers
        GetVertByID(TOP_VERT_DEFAULT_ID).Hide();
        GetVertByID(BOTTOM_VERT_DEFAULT_ID).Hide();

        nodeContentContainer.parent.style.backgroundColor = new Color(0.45f, 0.11f, 0.3f);
        nodeContentContainer.style.alignItems = Align.FlexEnd;
        VisualElement baseTexContainer = new VisualElement() { name = "baseTexContainer" };
        VisualElement secondaryTexContainer = new VisualElement() { name = "secTexContainer" };
        baseTexContainer.style.flexDirection = FlexDirection.Row;
        secondaryTexContainer.style.flexDirection = FlexDirection.Row;
        nodeContentContainer.Add(baseTexContainer);
        nodeContentContainer.Add(secondaryTexContainer);

        baseTexContainer.Add(new TextElement() { text = "Base Tex: " });
        baseTexField = new ObjectField();
        baseTexField.objectType = typeof(Texture2D);
        baseTexContainer.Add(baseTexField);
        AddVertToContainer(new NodeVert() { id = "baseTexVert" }, "baseTexContainer");

        secondaryTexContainer.Add(new TextElement() { text = "Secondary Tex: " });
        secTexField = new ObjectField();
        secTexField.objectType = typeof(Texture2D);
        secondaryTexContainer.Add(secTexField);
        AddVertToContainer(new NodeVert() { id = "secTexVert" }, "secTexContainer");
    }

    //write object path to disk
    public override byte[] SerializeNodeAsBytes() {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(AssetDatabase.GetAssetPath(baseTexField.value));
        writer.Write(AssetDatabase.GetAssetPath(secTexField.value));
        editorData = stream.ToArray();
        writer.Dispose();

        return base.SerializeNodeAsBytes();
    }

    public override void RestoreNodeFromBytes(BinaryReader reader) {
        base.RestoreNodeFromBytes(reader);

        if (editorData != null) {
            MemoryStream stream = new MemoryStream(editorData);
            BinaryReader dataReader = new BinaryReader(stream);
            string baseTexPath = dataReader.ReadString();
            string secTexPath = dataReader.ReadString();

            if (!string.IsNullOrEmpty(baseTexPath)) {
                baseTexField.value = AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexPath);
            }

            if (!string.IsNullOrEmpty(secTexPath)) {
                secTexField.value = AssetDatabase.LoadAssetAtPath<Texture2D>(secTexPath);
            }

            dataReader.Dispose();
        }
    } 
}
