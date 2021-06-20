using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System;

public class ComparisonNode : BaseNode {
    EnumField logicOptions = null;

    public enum LogicNodeOption {
        GREATER_THAN,
        GREATER_OR_EQUAL_TO,
        SMALLER_THAN,
        SMALLER_OR_EQUAL_TO,
        EQUAL_TO,
    }

    public ComparisonNode() {
        LogicNodeOption options = new LogicNodeOption();
        logicOptions = new EnumField(options);
        nodeContentContainer.style.flexDirection = FlexDirection.Row;
        nodeContentContainer.style.flexDirection = FlexDirection.Row;
        AddContentVertex(new LabeledNodeVert("In Value"));
        nodeContentContainer.Add(logicOptions);
        LabeledNodeVert testValueVert = new LabeledNodeVert("Test Value");
        testValueVert.style.flexDirection = FlexDirection.RowReverse;
        AddContentVertex(testValueVert);
    }

    public override byte[] SerializeNodeAsBytes() {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(logicOptions.value.ToString());
        editorData = stream.ToArray();
        writer.Dispose();
        return base.SerializeNodeAsBytes();
    }

    public override void RestoreNodeFromBytes(BinaryReader reader) {
        base.RestoreNodeFromBytes(reader);

        if (editorData != null) {
            MemoryStream stream = new MemoryStream(editorData);
            BinaryReader dataReader = new BinaryReader(stream);
            logicOptions.value = (LogicNodeOption)Enum.Parse(typeof(LogicNodeOption), dataReader.ReadString());
            dataReader.Dispose();
        }
    }
}
