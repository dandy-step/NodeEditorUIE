using System.IO;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class SetValueNode : BaseNode {
     TextField value = null;

    public SetValueNode() {
        LoadTemplateAndStyleFromName("SetValueNode");
        value = this.Q<TextField>("varTextField");
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
            BinaryReader valueReader = new BinaryReader(stream);
            value.value = valueReader.ReadString();
            valueReader.Dispose();
        }
    }
}