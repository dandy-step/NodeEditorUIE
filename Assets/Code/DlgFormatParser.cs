using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DlgFormatData {
    public ushort mainSpeaker;
    public ushort characterCount;
    public ushort entryCount;
    public ushort entryStartOffset;
    public List<string> charPrefabPath = new List<string>();
    public List<string> charIdles = new List<string>();
    public List<string> charName = new List<string>();
    public List<DlgBaseEntry> dialogueEntries = new List<DlgBaseEntry>();
    public List<Object> charPrefabAssets = new List<Object>();
}

public class DlgBaseEntry { }
public class DlgDialogueEntry : DlgBaseEntry {
    public int speakerIndex;
    public bool hasNoCharacterModel;
    public string customSpeakerLabel;
    public string animationClipName;
    public string text;
    public bool supportsAnimations;
    public bool animIsIdle;
}

public class DlgMultipleChoiceEntry : DlgBaseEntry {
    public ushort choiceCount;
    public string choiceQuestion;
    public string[] choiceText;
}

public class DlgFormatParser
{

    public int Get16ByteAlignedValue(int value) {
        return ((value / 16) + 1) * 16;
    }

    public DlgFormatData ParseDlgFile(byte[] data) {
        DlgFormatData parsedData = new DlgFormatData();
        MemoryStream memDataStream = new MemoryStream(data);
        BinaryReader reader = new BinaryReader(memDataStream);
        string magicNumber = new string(reader.ReadChars(4));
        if (magicNumber == "DIAL") {
            Debug.Log("Valid DLG file header!");
            reader.BaseStream.Seek(8, SeekOrigin.Begin);
            parsedData.mainSpeaker = reader.ReadUInt16();
            reader.BaseStream.Seek(16, SeekOrigin.Begin);
            parsedData.characterCount = (ushort)reader.ReadInt16();
            parsedData.entryCount = (ushort)reader.ReadInt16();
            parsedData.entryStartOffset = (ushort)reader.ReadInt16();
            reader.BaseStream.Seek(32, SeekOrigin.Begin);
            for (int i = 0; i < parsedData.characterCount; i++) {
                parsedData.charPrefabPath.Add(reader.ReadString());
                if (parsedData.charPrefabPath[i].Length > 0) {
                }
            }

            for (int i = 0; i < parsedData.characterCount; i++) {
                parsedData.charIdles.Add(reader.ReadString());
            }

            reader.BaseStream.Seek(parsedData.entryStartOffset, SeekOrigin.Begin);

            while (reader.BaseStream.Position < reader.BaseStream.Length) {
                string entryType = new string(reader.ReadChars(4));
                switch (entryType) {
                    case "DENT": {
                            long startPos = reader.BaseStream.Position;
                            DlgDialogueEntry entry = new DlgDialogueEntry();
                            int textSize = 0;
                            int animClipNameSize = 0;
                            int customLabelSize = 0;
                            entry.speakerIndex = reader.ReadInt16();
                            customLabelSize = reader.ReadUInt16();
                            entry.supportsAnimations = reader.ReadBoolean();
                            entry.animIsIdle = reader.ReadBoolean();
                            animClipNameSize = reader.ReadUInt16();
                            textSize = reader.ReadInt16();
                            reader.ReadInt32();
                            reader.BaseStream.Seek(startPos - 4, SeekOrigin.Begin);
                            reader.BaseStream.Seek(32, SeekOrigin.Current);
                            entry.customSpeakerLabel = new string(reader.ReadChars(customLabelSize));
                            entry.animationClipName = new string(reader.ReadChars(animClipNameSize < ushort.MaxValue ? animClipNameSize : 0));
                            entry.text = new string(reader.ReadChars(textSize));
                            if (parsedData.charPrefabPath[entry.speakerIndex] == "") {
                                entry.hasNoCharacterModel = true;
                            }

                            parsedData.dialogueEntries.Add(entry);
                            reader.BaseStream.Seek(Get16ByteAlignedValue((int)reader.BaseStream.Position), SeekOrigin.Begin);
                            break;
                        }
                    case "CHOO": {
                            DlgMultipleChoiceEntry entry = new DlgMultipleChoiceEntry();
                            entry.choiceCount = reader.ReadUInt16();
                            entry.choiceQuestion = reader.ReadString();
                            entry.choiceText = new string[entry.choiceCount];
                            for (int i = 0; i < entry.choiceCount; i++) {
                                entry.choiceText[i] = reader.ReadString();
                            }

                            parsedData.dialogueEntries.Add(entry);
                            reader.BaseStream.Seek(Get16ByteAlignedValue((int)reader.BaseStream.Position), SeekOrigin.Begin);
                            break;
                        }
                    default:
                        if (reader.BaseStream.Length - reader.BaseStream.Position <= 16) {
                            reader.BaseStream.Seek(0, SeekOrigin.End);
                            break;
                        } else {
                            throw new InvalidDataException("Bad header for dlg file entry at file pos " + reader.BaseStream.Position + "!");
                        }
                }
            }
        }

        reader.Close();
        memDataStream.Close();
        return parsedData;
    }
}
