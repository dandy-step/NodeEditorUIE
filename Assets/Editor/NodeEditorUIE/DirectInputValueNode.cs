using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;

public class DirectInputValueNode : BaseNode {
    TextField value = null;
    Label type = null;
    string defaultValue = "<enter>";

    public DirectInputValueNode() {
        VisualTreeAsset nodeTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(NodeEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "DirectInputValueNode.uxml");
        StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(NodeEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "NodeStyles.uss");
        nodeTemplate.CloneTree(nodeContentContainer);
        nodeContentContainer.styleSheets.Add(style);
        this.EnableInClassList("DirectInputValueNode", true);
        //nodeContentContainer.parent.style.backgroundColor = Color.cyan * 0.65f;
        value = nodeContentContainer.Q<TextField>("inputField");
        value.value = defaultValue;
        type = nodeContentContainer.Q<Label>("typeLabel");
        GetVertByID(TOP_VERT_DEFAULT_ID).Hide();
        value.RegisterValueChangedCallback<string>(AutodetectType);
        type.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
        AutodetectType(new ChangeEvent<string>());
    }

    void AutodetectType(ChangeEvent<string> evt) {
        if (value.value.Length == 0) {
            value.value = defaultValue;
        }

        value.value = value.value.Replace('.', ',');

        int intTry = 0;
        float floatTry = 0;
        if (int.TryParse(value.text, out intTry)) {
            type.text = "Int";
        } else if (float.TryParse(value.text, out floatTry)) {
            type.text = "Float";
        } else {
            type.text = "String";
        }
    }

    public override byte[] SerializeNodeAsBytes() {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(value.value);

        editorData = stream.ToArray();
        writer.Dispose();
        return base.SerializeNodeAsBytes();
    }

    public override void RestoreNodeFromBytes(BinaryReader reader) {
        base.RestoreNodeFromBytes(reader);
        if (editorData != null) {
            MemoryStream stream = new MemoryStream(editorData);
            BinaryReader dataReader = new BinaryReader(stream);
            value.value = dataReader.ReadString();
            dataReader.Dispose();
        }
    }
}
