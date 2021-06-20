using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using DialogTool;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class DialogEditorUIEPaths {
    public static readonly string EDITOR_ROOT_PATH = "Assets/Editor/DialogEditorUIE/";
    public static readonly string EDITOR_UXML_USS_ASSETS_ROOT_PATH = EDITOR_ROOT_PATH + "UXML & USS/";
    public static readonly string EDITOR_GRAPHIC_ASSETS_ROOT_PATH = EDITOR_ROOT_PATH + "Graphics/";
    public static readonly string DIALOG_SAVE_DIRECTORY_PATH = "/Editor/Data/Dialog/";
    public static readonly string CHARACTER_FOLDER_PATH = "Assets/Prefabs/Characters";
    public static readonly string PLAYBACK_PREVIEW_PATH = "Assets/Resources/playback.dlg";
}

[Serializable]
public partial class DialogEditorUIE : EditorWindow, ISerializationCallbackReceiver {
    public const string ANIM_ERROR_VALUE = "INVALID";
    public const string DIALOG_TYPE = "DENT";
    public const string CHOICE_TYPE = "CHOO";

    ScrollView scrollView;
    //ListView listView = null;             //for future ListView upgrade
    int idCounter = 0;
    public static List<CharacterData> characters = new List<CharacterData>();
    float characterAdvance;
    [SerializeField] string serializationFilePath;
    public Vector2 serializedScrollPosition = Vector2.zero;
    public static VisualElement root;
    public UnityEngine.SceneManagement.Scene prePlaybackScene;
    public static CharacterData mainSpeaker = null;
    [SerializeField] bool hasSerializedData = false;
    public string nodeOwnerID = "";

    public string[] speakerBoilerplateDialog = new string[] {
        "This is what we're going to do. You can deal with it, or you can cry about it.",
        "Either way, we're going ahead with it. There's nothing about it to discuss.",
        "Make sure you're ready.",
        "Don't forget the stuff.",
        "Get up to speed, kid.",
        "Just give me an excuse. I'll make you regret it.",
        "Don't worry about a thing.",
        "You always say that. You know what, I'm starting to worry.",
        "Good. You should be worried.",
        "Yes.",
        "No way!",
        "What kind of plan is that? You seriously expect me to go ahead with this? You must be out of your mind...",
        "Stop worrying so much and just go.",
        "Honestly, we're screwed either way. You should be worried.",
        "I got it covered.",
        "Bring me back th-",
        "For sure, dude!",
        "Not cool, man.",
        "You ever decided on that thing?",
        "What are you talking about?",
        "Sounds good to me.",
        "Not even close."
    };

    public string[] environmentalBoilerplateDialog = new string[] {
        "The wind was blowing through the canopy, giving the halls somewhat of an eerie feeling.",
        "I felt a shiver down my spine.",
        "It was a late afternoon in September when they finally decided to go ahead with it.",
        "My stomach felt like a ton of bricks.",
        "He recoiled at her response.",
        "Just then, it started to rain.",
        "The leaves covered the pavement, giving the asphalt road a reddish-brown tint.",
        "The light was shining through the skylight.",
        "She gasped at his response.",
        "He saw her face scrunch up.",
        "There was a smell of iron in the air."
    }; 

    //[MenuItem("Custom Editors/DialogEditorUIE")]
    public static void Init() {
        //Create new instance of our custom editor
        DialogEditorUIE window = (DialogEditorUIE)CreateInstance(typeof(DialogEditorUIE));
        window.Show();
    }

    public void InitializeDefaultValues() {
        //listView = null;
        scrollView.Clear();
        characters.Clear();
        characterAdvance = 0f;
        AddCharacter(GenerateBlankCharacter());
    }

    //create buttons and UI
    public void OnEnable() {
        root = rootVisualElement;
        VisualElement buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        root.Add(buttonContainer);

        Button newFileButton = new Button(NewFile) { text = "New" };
        buttonContainer.Add(newFileButton);

        Button addButton = new Button(AddDialogEntry) { text = "Add Entry" };
        buttonContainer.Add(addButton);

        Button addChoiceTestButton = new Button(AddChoiceEntry) { text = "Add Choice" };
        buttonContainer.Add(addChoiceTestButton);

        Button addCharacterButton = new Button(LoadCharacter) { text = "Add Chars" };
        buttonContainer.Add(addCharacterButton);

        Button saveFileButton = new Button(() => { SaveFile(); }) { text = "SAVE" };
        buttonContainer.Add(saveFileButton);

        Button loadFileButton = new Button(() => { OpenFile(); }) { text = "LOAD" };
        buttonContainer.Add(loadFileButton);

        Button randomConversationButton = new Button(RandomConversation) { text = "Random" };
        buttonContainer.Add(randomConversationButton);

        Button playDlgButton = new Button(PlayDlg) { text = "Preview >" };
        buttonContainer.Add(playDlgButton);

        VisualElement detailsContainer = new VisualElement();
        detailsContainer.style.flexDirection = FlexDirection.Row;
        root.Add(detailsContainer);


        scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.style.flexGrow = 1;
        root.Add(scrollView);

        InitializeDefaultValues();
    }

    public void UpdateNode(GeometryChangedEvent evt) {
        if (!string.IsNullOrEmpty(nodeOwnerID)) {
            GetNodePreview();
        }
    } 

    //adds main speaker graphic to the conversation entries
    public void SetMainSpeaker(CharacterData _mainSpeaker) {
        mainSpeaker = _mainSpeaker;
        IEnumerable<VisualElement> children = scrollView.Children();
        foreach (VisualElement child in children) {
            if (child.GetType() == typeof(DialogEntry)) {
                DialogEntry entry = (DialogEntry)child;
                if (entry.character.characterName == mainSpeaker.characterName) {
                    entry.mainSpeakerIcon.style.display = DisplayStyle.Flex;
                } else {
                    entry.mainSpeakerIcon.style.display = DisplayStyle.None;
                }
            }
        }
    }

    //updates idle animations throughout list for the same character
    public void UpdateIdles(string idleClip, CharacterData character) {
        IEnumerable<VisualElement> children = scrollView.Children();
        foreach  (VisualElement child in children) {
            if (child.GetType() == typeof(DialogEntry)) {
                DialogEntry entry = (DialogEntry)child;
                if (entry.character == character) {
                    if (entry.GetAnimClipName() == ANIM_ERROR_VALUE) {
                        entry.SetAnimClip("");
                    }

                    entry.SetIdleClip(idleClip);
                    entry.UpdateAnimationUI();
                }
            }
        }
    }

    public string GetCurrentCharacterIdle(DialogEntry requester) {
        VisualElement[] children = scrollView.Children().ToArray();
        int requesterIndex = scrollView.IndexOf(requester);
        if (requesterIndex > 0) {
            for (int i = requesterIndex; i <= 0; i--) {
                if (children[i].GetType() == typeof(DialogEntry)) {
                    if (((DialogEntry)children[i]).character == requester.character) {
                        return ((DialogEntry)children[i]).GetIdleClipName();
                    }
                }
            }
        }

        Debug.Log("Failed to find a previous idle for character " + requester.character.characterName);
        return "";
    }

    //previews DLG file in-game
    public void PlayDlg() {
        //save temp dlg file
        GenerateDialogFile(true, "Assets/Resources/playback.dlg");

        //find manager and set the paths
        FileStream file = File.OpenRead("Assets/Resources/playback.dlg");
        DialoguePlaybackData dlgData = FindObjectOfType<DialoguePlaybackData>();
        if (dlgData) {
            dlgData.data = new byte[file.Length + 1];
            file.Read(dlgData.data, 0, (int)file.Length);
            dlgData.dlgFilePath = "Assets/Resources/playback.dlg";
            file.Close();
            EditorApplication.EnterPlaymode();
        } else {
            EditorUtility.DisplayDialog("Error", "Please use the DialogEditorPlayback scene to preview the file", "Ok");
            file.Close();
        }

        //play game
    }

    //adds a new multiple choice entry
    public void AddChoiceEntry() {
        MultipleChoiceContainer entry = new MultipleChoiceContainer();
        scrollView.Add(entry);
    }

    private void OnGUI() {
        if (hasSerializedData) {
            Debug.Log("Restoring serialized data on OnGUI");
            //InitializeDefaultValues();
            RestoreDialogFile(serializationFilePath);
            Debug.Log("Discarding serialized data");
            hasSerializedData = false;
            serializationFilePath = "";
        }

        //restore scrollbar position by restoring the value when the scrollView layout is inflated, usually it would be remembered by setting viewDataKey, but it doesn't seem to work - maybe because we're storing custom VisualElements rather than built-in
        if ((!float.IsNaN(scrollView.contentContainer.layout.height)) && (serializedScrollPosition != Vector2.zero)) {
            scrollView.scrollOffset = serializedScrollPosition;
            serializedScrollPosition = Vector2.zero;
        }
    }

    //opens previously generated DLG file
    public bool OpenFile() {
        string fileOpenPath = EditorUtility.OpenFilePanel("Open Dialog File", "Assets/Data/Dialog/", "dlg");
        if (fileOpenPath != "") {
            InitializeDefaultValues();
            NewFile();
            RestoreDialogFile(fileOpenPath);
            return true;
        } else {
            return false;
        }
    }

    //generates a new DLG file to save to disk
    public bool SaveFile() {
        string saveFilePath = EditorUtility.SaveFilePanel("Save Dialog File", "Assets/Data/Dialog/", "dialog_" + DateTime.Now.Ticks, "dlg");
        if (saveFilePath != "") {
            GenerateDialogFile(false, saveFilePath);
            return true;
        } else {
            return false;
        }
    }

    //creates a new file
    public void NewFile() {
        if (scrollView.childCount > 0) {
            int answer = EditorUtility.DisplayDialogComplex("Warning", "Do you want to save your current work before creating a new file?", "Yes", "No", "Cancel");

            switch (answer) {
                case 0:
                    if (SaveFile()) {
                        goto case 1;
                    } else {
                        return;
                    }
                case 1:
                    InitializeDefaultValues();
                    break;
                case 2:
                    return;
            }
        } else {
            InitializeDefaultValues();
        }
    }

    //generates a blank character, used for non-prefab (narrator, environmental) speakers
    public CharacterData GenerateBlankCharacter() {
        CharacterData blankChar = CreateInstance<CharacterData>();
        blankChar.characterName = "";
        blankChar.prefabPath = "";
        return blankChar;
    }

    //generates a random conversation
    public void RandomConversation() {
        if (characters.Count > 1) {
            int lastSpeaker = characters.Count + 1;
            string lastDialog = "";
            int repeatSpeakerCount = 0;

            for (int i = 0; i < 10; i++) {
                int speakerSelection = UnityEngine.Random.Range(0, characters.Count);
                string dialogSelection = lastDialog;

                if (speakerSelection == lastSpeaker) {
                    repeatSpeakerCount++;
                    if (repeatSpeakerCount < 3) {
                        while (speakerSelection == lastSpeaker) {
                            speakerSelection = UnityEngine.Random.Range(0, characters.Count);
                        }
                    }
                } else {
                    repeatSpeakerCount = 0;
                }

                while (dialogSelection.Equals(lastDialog)) {
                    dialogSelection = characters[speakerSelection].prefabPath != "" ? speakerBoilerplateDialog[UnityEngine.Random.Range(0, speakerBoilerplateDialog.Length)] : environmentalBoilerplateDialog[UnityEngine.Random.Range(0, environmentalBoilerplateDialog.Length)];
                }

                DialogEntry entry = new DialogEntry(idCounter, dialogSelection, characters[speakerSelection]);
                scrollView.Add(entry);
                idCounter++;
                scrollView.Add(entry);
                lastSpeaker = speakerSelection;
                lastDialog = dialogSelection;
            }
        } else {
            EditorUtility.DisplayDialog("Sorry", "Please add at least two characters to generate a conversation", "Ok");
        }
    }

    //adds a new text entry 
    public void AddDialogEntry() {
        if (characters.Count > 0) {
            DialogEntry entry = new DialogEntry(idCounter, "", characters[0]);
            scrollView.Add(entry);
            idCounter++;
        } else {
            EditorUtility.DisplayDialog("Sorry!", "Please add a character first", "Ok");
        }
    }

    //loads a character from prefab
    public void LoadCharacter() {
        string filePath = EditorUtility.OpenFilePanel("Choose character prefab", "Assets/Prefabs/Characters/", "prefab");
        if (filePath != "") {
            GameObject data = PrefabUtility.LoadPrefabContents(filePath);
            if (data.GetComponent<Character>() != null) {
                CharacterData character = data.GetComponent<Character>().data;
                character.prefabPath = filePath.Substring(filePath.LastIndexOf("Assets/"));
                AddCharacter(character);
            }

            PrefabUtility.UnloadPrefabContents(data);
        }
    }

    //adds a new character, fails if it already exists
    public void AddCharacter(CharacterData character) {
        CharacterData alreadyAdded = characters.Find(x => (string.Equals(x.characterName,character.characterName)));

        if (alreadyAdded == null) {
            if (character.prefabPath != "") {
                character.editorAdvance = characterAdvance;
                characterAdvance += 6f;
            } else {
                character.editorAdvance = 0f;
            }

            characters.Add(character);
            return;
        } else {
            Debug.Log("Failed to add character, already existed?");
            Debug.Log("This character was " + character.name + " path: " + character.prefabPath);
        }
    }

    //cycles between characters
    public CharacterData CycleCharacter(CharacterData currCharacter) {
        for (int i = 0; i < characters.Count; i++) {
            if (characters[i] == currCharacter) {
                if (i < characters.Count - 1) {
                    return characters[i + 1];
                } else {
                    return characters[0];
                }
            }
        }

        throw new Exception("Couldn't cycle character!");
    }

    public void RemoveEntry(VisualElement entry) {
        scrollView.Remove(entry);
    }

    private class DialogFileSaveData {
        public char[] magicNumber = { 'D', 'I', 'A', 'L' };
        public ushort mainSpeakerIndex;     //who is the main speaker for this conversation
        public ushort characterCount;       //how many characters in this conversation
        public ushort entryCount = 0;       //how many entries in this conversation
        public string[] characterIdles;     //character idle anims
        public uint entryDataOffset;        //offset into the serialized entry data start
    }

    public int Get16ByteAlignedValue(int value) {
        return ((value / 16) + 1) * 16;
    }

    //generates a binary DLG file that contains every single entry in the list, serialized
    public void GenerateDialogFile(bool saveBundle, string saveFilePath) {
        using (FileStream saveFile = new FileStream(saveFilePath, FileMode.Create)) {
            using (BinaryWriter writer = new BinaryWriter(saveFile)) {
                List<string> paths = new List<string>();
                List<string> idles = new List<string>();
                ushort mainSpeakerIndex = 0;

                //grab the file path for each character prefab
                for (int i = 0; i < characters.Count; i++) {
                    //Debug.Log("Gen: Writing prefab path " + characters[i].prefabPath + " for charIndex " + i);
                    if (characters[i].prefabPath != "") {
                        paths.Add(characters[i].prefabPath.Substring(characters[i].prefabPath.LastIndexOf("Assets/")));
                        if (mainSpeaker != null) {
                            if (mainSpeaker.characterName == characters[i].characterName) {
                                mainSpeakerIndex = (ushort)i;
                            }
                        }
                    } else {
                        if (i > 0) {
                            Debug.Log("Found empty prefab path for a character with non-zero index! This shouldn't happen, unless you manually added empty characters!");
                        }

                        paths.Add("");
                    }
                }

                for (int i = 0; i < characters.Count; i++) {
                    if (!string.IsNullOrEmpty(characters[i].prefabPath)) {
                        //idles.Add(GetCurrentCharacterIdle(characters));
                        idles.Add("");
                    } else {
                        idles.Add("");
                    }
                }

                //generate an AssetBundle for use in-game if requested
                if (saveBundle) {
                    Debug.Log("Generating AssetBundle");
                    AssetBundleBuild charBundle = new AssetBundleBuild();
                    charBundle.assetBundleName = "characterBundle";
                    charBundle.assetNames = paths.ToArray();

                    AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(Application.dataPath + "/AssetBundles/", new AssetBundleBuild[] { charBundle }, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

                    AssetBundle bundle = AssetBundle.LoadFromFile("Assets/AssetBundles/" + charBundle.assetBundleName);
                    Debug.Log("Generated AssetBundle at " + "AssetBundles/" + charBundle.assetBundleName);
                    bundle.Unload(true);
                }

                DialogFileSaveData data = new DialogFileSaveData();
                data.characterCount = (ushort)paths.Count;

                //write file data
                writer.Write(data.magicNumber);
                writer.Seek(8, SeekOrigin.Begin);
                writer.Write(mainSpeakerIndex);
                writer.Seek(16, SeekOrigin.Begin);
                writer.Write(data.characterCount);
                writer.Seek(32, SeekOrigin.Begin);


                for (int i = 0; i < data.characterCount; i++) {
                    writer.Write(paths[i]);
                }

                for (int i = 0; i < data.characterCount; i++) {
                    writer.Write(idles[i]);
                }

                data.entryDataOffset = (uint)writer.Seek(Get16ByteAlignedValue((int)writer.BaseStream.Position), SeekOrigin.Begin);

                //write entry data
                bool ignoreAnimatorCheck = false;
                foreach (VisualElement child in scrollView.Children()) {
                    if (child.GetType() == typeof(DialogEntry)) {
                        //data sanity check, skip if we're serializing or in node mode
                        if ((saveFilePath == serializationFilePath) || (saveFilePath == DialogEditorUIEPaths.DIALOG_SAVE_DIRECTORY_PATH) || (nodeOwnerID != null)) {
                            //Debug.Log("Ignoring sanity checks for serializer/previewer");
                        } else {
                            DialogEntry.SANITY_CHECK_FAIL sanity = ((DialogEntry)child).CheckSanity();
                            if (sanity != DialogEntry.SANITY_CHECK_FAIL.OKAY) {
                                if (sanity == DialogEntry.SANITY_CHECK_FAIL.BAD_ANIM_NO_IDLE) {
                                    EditorUtility.DisplayDialog("", "Please make sure all characters have an idle clip set up!", "Ok");
                                    writer.Close();
                                    File.Delete(saveFilePath);
                                    return;
                                } else if (sanity == DialogEntry.SANITY_CHECK_FAIL.BAD_ANIM_CONTROLLER) {
                                    if (!ignoreAnimatorCheck) {
                                        if (!EditorUtility.DisplayDialog("", "Trying to save using character with no animations. Save anyway?", "Ok", "Cancel")) {
                                            writer.Close();
                                            File.Delete(saveFilePath);
                                            return;
                                        } else {
                                            ignoreAnimatorCheck = true;
                                        }
                                    }
                                }
                            }
                        }
                        byte[] entryData = ((DialogEntry)child).GenerateSaveData(paths);
                        writer.Write(entryData);
                    } else if (child.GetType() == typeof(MultipleChoiceContainer)) {
                        byte[] entryData = ((MultipleChoiceContainer)child).GenerateSaveData(paths);
                        writer.Write(entryData);
                    } else {
                        Debug.Log("Found bad entry while trying to generate DLG file");
                    }

                    writer.Seek(Get16ByteAlignedValue((int)writer.BaseStream.Position), SeekOrigin.Begin);
                    data.entryCount++;
                }

                long currentPos = writer.Seek(0, SeekOrigin.Current);
                //Debug.Log("Wrote " + currentPos + " bytes during serialization");
                writer.Seek(18, SeekOrigin.Begin);
                writer.Write(data.entryCount);
                writer.Write(data.entryDataOffset);
                writer.Seek((int)currentPos, SeekOrigin.Begin);
                writer.Write("__EOF__");
                writer.Close();
            }
        }
    }

    //restore a file
    public void RestoreDialogFile(byte[] inFileData) {
        BinaryReader inFile = new BinaryReader(new MemoryStream(inFileData));
        this.RestoreDialogFile(inFile);
    }


    public void RestoreDialogFile(string filePath) {
        using (FileStream stream = File.Open(filePath, FileMode.Open)) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                this.RestoreDialogFile(reader);
            }
        }
    }

    public void RestoreDialogFile(BinaryReader reader) {
        DialogFileSaveData data = new DialogFileSaveData();

        if (reader.ReadChars(data.magicNumber.Length).SequenceEqual(data.magicNumber)) {
            reader.BaseStream.Seek(8, SeekOrigin.Begin);
            data.mainSpeakerIndex = reader.ReadUInt16();
            reader.BaseStream.Seek(16, SeekOrigin.Begin);
            data.characterCount = reader.ReadUInt16();
            data.entryCount = reader.ReadUInt16();
            data.entryDataOffset = reader.ReadUInt32();
            reader.BaseStream.Seek(32, SeekOrigin.Begin);

            List<string> assetPaths = new List<string>();
            List<string> characterIdles = new List<string>();
            characters = new List<CharacterData>();
            for (int i = 0; i < data.characterCount; i++) {
                assetPaths.Add(reader.ReadString());

                if (assetPaths[i] != "") {
                    CharacterData character = (((GameObject)AssetDatabase.LoadAssetAtPath(assetPaths[i], typeof(GameObject))).GetComponent<Character>().data);
                    character.prefabPath = assetPaths[i];
                    AddCharacter(character);

                    if (i == data.mainSpeakerIndex) {
                        mainSpeaker = character;
                    }
                } else {
                    //Debug.Log("Found blank asset path for char index " + i);
                    AddCharacter(GenerateBlankCharacter());
                }
            }

            for (int i = 0; i < data.characterCount; i++) {
                characterIdles.Add(reader.ReadString());
            }

            reader.BaseStream.Seek(data.entryDataOffset, SeekOrigin.Begin);
            for (int i = 0; i < data.entryCount; i++) {
                string magicNumber = new string(reader.ReadChars(4));
                if (magicNumber == DIALOG_TYPE) {
                    DialogEntry entry = new DialogEntry(idCounter, "", GenerateBlankCharacter());
                    entry.RestoreSaveData(reader, characters, characterIdles);
                    scrollView.Add(entry);
                    reader.BaseStream.Seek(Get16ByteAlignedValue((int)reader.BaseStream.Position), SeekOrigin.Begin);
                } else if (magicNumber == CHOICE_TYPE) {
                    MultipleChoiceContainer entry = new MultipleChoiceContainer();
                    entry.RestoreSaveData(reader, characters);
                    scrollView.Add(entry);
                    reader.BaseStream.Seek(Get16ByteAlignedValue((int)reader.BaseStream.Position), SeekOrigin.Begin);
                }
            }
            reader.Close();
        }
    }

    //** NodeEditorUIE Hookup Start **

    //generates editor data for the node in byte format - same format used by the editor. writes to a temp file, reads back the bytes, closes the stream, returns the data
    public byte[] GenerateNodeData() {
        string tempFile = Path.GetTempFileName();
        GenerateDialogFile(false, tempFile);
        BinaryReader file = new BinaryReader(File.OpenRead(tempFile));
        byte[] data = file.ReadBytes((int)file.BaseStream.Length);
        file.Dispose();
        File.Delete(tempFile);
        return data;
    }

    //survive data across Assembly reloads
    public void OnBeforeSerialize() {
        if (!hasSerializedData) {
            hasSerializedData = true;
            if (string.IsNullOrEmpty(serializationFilePath)) {
                serializationFilePath = Application.dataPath + "/Data/dialogSerialization.lock";
            }

            serializedScrollPosition = scrollView.scrollOffset;
            GenerateDialogFile(false, serializationFilePath);
        } else {
            //Debug.Log("Skipping serialized data serialization.");
        }
    }

    public void OnAfterDeserialize() { }    //required by interface

    //check if our node owner ID is still good - has it been deleted, has it been changed?
    public bool IsNodeOwnerIDValid() {
        if (!string.IsNullOrEmpty(nodeOwnerID)) {
            if (EditorWindow.HasOpenInstances<NodeEditorUIE>()) {
                NodeEditorUIE nodeWindow = EditorWindow.GetWindow<NodeEditorUIE>();
                if (nodeWindow) {
                    BaseNode node = nodeWindow.nodeContainer.GetNodeByID(nodeOwnerID);
                    if (node != null) { 
                        return true; 
                    }
                }
            }
        }

        return false;
    }

    //get our node owner
    public DialogNode GetNodeOwner() {
        if (!string.IsNullOrEmpty(nodeOwnerID)) {
            if (IsNodeOwnerIDValid()) {
                NodeEditorUIE nodeWindow = EditorWindow.GetWindow<NodeEditorUIE>();
                return (DialogNode)nodeWindow.nodeContainer.GetNodeByID(nodeOwnerID);
            }
        }

        return null;
    }

    //generate a preview, which gets placed directly into the content container of our node
    public VisualElement GetNodePreview() {
        int displayItemCount = 6;
        VisualElement preview = new VisualElement();        //create new VE
        preview.style.maxWidth = 300;
        bool nativeUI = false;
        if (IsNodeOwnerIDValid()) {
            nativeUI = GetNodeOwner().useNativeEditorUI;
        }

        if (scrollView.childCount > 0) {
            for (int i = 0; i < Math.Min(scrollView.childCount, displayItemCount); i++) {
                //add elements to our preview - in this case, we're showing the first 6 dialog entries in the node
                if (scrollView[i].GetType() == typeof(DialogEntry)) {
                    if (!nativeUI) {
                        DialogEntryPreview item = new DialogEntryPreview((DialogEntry)scrollView[i]);
                        preview.Add(item);
                    } else {
                        DialogEntry elem = ((DialogEntry)scrollView[i]);
                        DialogEntry item = new DialogEntry();
                        item.SetDialogText(elem.GetDialogText());
                        item.SetCharacter(elem.character);
                        item.SetSpeakerName(elem.GetSpeakerName());
                        preview.Add(item);
                    }
                }
            }

            //if we have a multiple choice container as the last item, we want to add verts to branch from each choice
            if (scrollView[scrollView.childCount - 1].GetType() == typeof(MultipleChoiceContainer)) {
                MultipleChoiceContainer choice = (MultipleChoiceContainer)scrollView[scrollView.childCount - 1];
                DialogNode node = GetNodeOwner();

                node.GetVertByID("bottomVert").Hide();      //hide bottom vert, we're replacing it
                for (int i = 0; i < choice.choiceContainer.childCount; i++) {
                    node.AddBottomVertex("choice" + i.ToString());      //repeat vert adds get ignored, so you can assume this
                    NodeVert vert = node.GetVertByID("choice" + i.ToString());
                    ChoiceEntry choiceEntry = ((ChoiceEntry)choice.choiceContainer[i]);
                    vert.Colorize(choiceEntry.textBox.style.backgroundColor.value);     //set vert color to same as choice color
                    vert.hoverDesc = choiceEntry.choiceText.text;       //set vert hover text to be the text of the choice
                }
            }
        } else {
            preview = GetNodeOwner().EmptyContent();
        }

        return preview;
    }

    private void OnDestroy() {
        //send current data of the editor to the node
        if (IsNodeOwnerIDValid()) {
            DialogNode node = GetNodeOwner();
            node.SetEditorData(GenerateNodeData());
        }
    }

    private void OnLostFocus() {
        //generate a preview when you switch windows
        if (IsNodeOwnerIDValid()) {
            DialogNode node = GetNodeOwner();
            node.RefreshEditorPreview();
        }
    }

    //** NodeEditorUIE Hookup End**
}
