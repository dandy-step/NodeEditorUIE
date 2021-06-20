using UnityEditor;
using UnityEngine.UIElements;
using System.IO;

public class GetValueNode : BaseNode {
    TextField value = null;

    public GetValueNode() {
        LoadTemplateAndStyleFromName("GetValueNode");
        GetVertByID(TOP_VERT_DEFAULT_ID).Hide();
        value = this.Q<TextField>("varTextField");
    }

    public override byte[] SerializeNodeAsBytes() {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(value.value);
        editorData = stream.GetBuffer();
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