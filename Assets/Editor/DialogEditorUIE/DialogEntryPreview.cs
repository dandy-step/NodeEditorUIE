using UnityEditor;
using UnityEngine.UIElements;
using DialogTool;

//this is a custom VE that represents what a DialogEntry looks like in a node - you could easily replace this with just the native UI elements of your editor, if you'd like - it just probably won't look very good
public partial class DialogEditorUIE {
    public class DialogEntryPreview : VisualElement {
        VisualElement root;
        TextElement speakerName;
        TextElement dialog;
        VisualElement fadeEffect;

        public DialogEntryPreview(DialogEntry entry) {      //pass a DialogEntry to the constuctor to automatically fill relevant data
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(NodeEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "DialogEntryPreview.uxml");
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(NodeEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "DialogEntryPreview.uss");
            root = asset.CloneTree();
            root.styleSheets.Add(style);
            speakerName = root.Q<TextElement>("speakerName");
            dialog = root.Q<TextElement>("dialog");
            string entrySpeaker = entry.GetSpeakerName();
            if (entrySpeaker.Length > 0) {
                speakerName.text = entry.GetSpeakerName();
            } else {
                speakerName.style.display = DisplayStyle.None;
                root.style.flexDirection = FlexDirection.RowReverse;
            }

            dialog.text = entry.GetDialogText();
            speakerName.style.backgroundColor = entry.character.editorColor;
            root.style.backgroundColor = entry.character.editorColor * 0.9f;
            this.Add(root);
        }
    }
}
