using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.IO;

namespace DialogTool {
    public class DraggableBase : VisualElement{
        public Color dragColor;
    }

    public static class Constants {
        public static Color multipleTargetsColor = Color.white;

        //default colors for various choices
        public static Color[] colors = new Color[] {
            new Color32(0xe6, 0x19, 0x4B, 0xFF),
            new Color32(0x3c, 0xb4, 0x4b, 0xFF),
            new Color32(0xff, 0xe1, 0x19, 0xFF),
            new Color32(0x43, 0x63, 0xd8, 0xFF),
            new Color32(0xf5, 0x82, 0x31, 0xFF),
            new Color32(0x91, 0x1e, 0xb4, 0xFF),
            new Color32(0x42, 0xd4, 0xf4, 0xFF),
            new Color32(0xf0, 0x32, 0xe6, 0xFF),
            new Color32(0xbf, 0xef, 0x45, 0xFF),
            new Color32(0xfa, 0xbe, 0xbe, 0xFF),
            new Color32(0x46, 0x99, 0x90, 0xFF),
            new Color32(0xe6, 0xbe, 0xff, 0xFF),
            new Color32(0x9A, 0x63, 0x24, 0xFF),
            new Color32(0x80, 0x00, 0x00, 0xFF),
            new Color32(0xaa, 0xff, 0xc3, 0xFF),
            new Color32(0x80, 0x80, 0x00, 0xFF),
            new Color32(0xff, 0xd8, 0xb1, 0xFF),
            new Color32(0x00, 0x00, 0x75, 0xFF),
        };

        static public Color GoodContrastTextColor(Color col) {
            return ((col.maxColorComponent > (0xf0 / 255f)) ? Color.black : Color.white);
        }
    }

    public interface ILineDraggable {
        VisualElement GetDragTargets(Vector2 mousePos);
    }

    public interface IColorDragTargettable {
        void SetTargetted(ILineDraggable targetingThis);
    }

    //logic for drawing connecting lines between UI elements, includes the interface definition above. When this project started, Unity didn't have a way to do this natively, and I'm not sure whether it does now. Either way, it goes unused here
    public class DrawLineElement : ImmediateModeElement {
        public ILineDraggable container;
        public DragLinkElement source;
        public Vector2 initialPos;
        public Button testButton;

        public DrawLineElement(DragLinkElement _source, ILineDraggable _container) {
            container = _container;
            source = _source;
            this.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            this.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        public void OnMouseMove(MouseMoveEvent evt) {
            VisualElement dragTarget = container.GetDragTargets(evt.mousePosition);

            if (dragTarget != null) {
                dragTarget.style.backgroundColor = source.color;
                dragTarget.style.color = Constants.GoodContrastTextColor(dragTarget.style.backgroundColor.value);

                if (source.target != null) {
                    if (source.target.GetFirstAncestorOfType<DialogChunk>() != null) {
                        DialogChunk chunk = source.target.GetFirstAncestorOfType<DialogChunk>();
                        if (chunk.targettedBy.Count == 1) {
                            //chunk.chunkLabel.style.backgroundColor = chunk.targettedBy[0].color;
                        } else {
                            chunk.chunkLabel.style.backgroundColor = Constants.multipleTargetsColor;
                        }

                        chunk.chunkLabel.style.color = Constants.GoodContrastTextColor(chunk.chunkLabel.style.backgroundColor.value);
                    }
                }

                source.target = dragTarget;
                
                if (dragTarget.GetType() == typeof(DialogChunk)) {
                    DialogChunk chunk = (DialogChunk)dragTarget;
                }

                RenderLineElement renderLine = ((VisualElement)container).GetFirstOfType<RenderLineElement>();

                if (renderLine == null) {
                    VisualElement containerVE = (VisualElement)container;

                    renderLine = new RenderLineElement();
                    renderLine.style.position = Position.Absolute;
                    renderLine.pickingMode = PickingMode.Ignore;
                    renderLine.style.width = containerVE.layout.width;
                    renderLine.style.height = containerVE.layout.height;
                    renderLine.style.opacity = 0.15f;

                    containerVE.Add(renderLine);
                }

                renderLine.dragLinks.Add(source);
                parent.Remove(this);
            }
        }

        public void OnMouseUp(MouseUpEvent evt) {
            parent.Remove(this);
        }

        protected override void ImmediateRepaint() {
            Handles.DrawBezier(initialPos, Event.current.mousePosition, initialPos + (Vector2.right * 32f), Event.current.mousePosition, source.color, null, 4f);
            MarkDirtyRepaint();
        }
    }

    public class RenderLineElement : ImmediateModeElement {
        public List<DragLinkElement> dragLinks = new List<DragLinkElement>();

        protected override void ImmediateRepaint() {
            foreach (DragLinkElement link in dragLinks) {
                Handles.color = Color.cyan;
                Handles.DrawBezier(this.WorldToLocal(link.worldBound.center), this.WorldToLocal(link.target.worldBound.center), this.WorldToLocal(link.worldBound.center) + (Vector2.right * 100f), this.WorldToLocal(link.target.worldBound.center) + (Vector2.down * 20f), link.color, null, 4f);
            }

            MarkDirtyRepaint();
        }
    }

    public class DragLinkElement : Image {
        public VisualElement target = null;
        public Color color = Color.magenta;

        public DragLinkElement() {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(NodeEditorUIEPaths.EDITOR_GRAPHIC_ASSETS_ROOT_PATH + "default_vert.png");
            this.style.backgroundImage = tex;
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        public class Factory : UxmlFactory<DragLinkElement> { }

        public void OnMouseDown(MouseDownEvent evt) {
            MultipleChoiceContainer container = GetFirstAncestorOfType<MultipleChoiceContainer>();

            if (container != null) {
                DrawLineElement element = new DrawLineElement(this, container);
                element.style.position = Position.Absolute;
                element.style.width = container.layout.width;
                element.style.height = container.layout.height;
                element.initialPos = container.WorldToLocal(this.worldBound.center);

                container.Add(element);
                evt.StopImmediatePropagation();
            }
        }
    }

    //
    public class MultipleChoiceContainer : VisualElement, ILineDraggable {
        public int uniqueColorIndex = 0;
        List<string> choices;
        public VisualElement dialogChunkContainer;
        public VisualElement choiceContainer;
        public TextField choiceQuestion;
        public List<VisualElement> labels = new List<VisualElement>();

        public MultipleChoiceContainer() {
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(DialogEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "MultipleChoiceContainer.uss");
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DialogEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "MultipleChoiceContainer.uxml");
            VisualElement ui = tree.CloneTree();
            ui.styleSheets.Add(style);
            choiceContainer = ui.Q<VisualElement>("choice_container");

            Button addChoiceButton = ui.Q<Button>("add_choice_button");
            addChoiceButton.RegisterCallback<MouseUpEvent>(AddChoice);
            Add(ui);

            choiceQuestion = ui.Q<TextField>("question_text");
            this.AddManipulator(new DragManipulator());
        }

        public void AddChoice(MouseUpEvent evt) {
            ChoiceEntry entry = new ChoiceEntry(this);
            choiceContainer.Add(entry);
        }

        public void AddDialogChunk(MouseUpEvent evt) {
            DialogChunk chunk = new DialogChunk();
            labels.Add(chunk.chunkLabel);
            dialogChunkContainer.Add(chunk);
        }

        //unused here
        public VisualElement GetDragTargets(Vector2 mousePos) {
            //labels for chunk dialog containers
            foreach (VisualElement child in dialogChunkContainer.Children()) {
                if (child.GetType() == typeof(DialogChunk)) {
                    if (((DialogChunk)child).chunkLabel.worldBound.Contains(mousePos)) {
                        return ((DialogChunk)child).chunkLabel;
                    }
                }
            }

            return null;
        }

        //save data container
        class MultipleChoiceEntrySaveData {
            public char[] magicNumber = new char[4] { 'C', 'H', 'O', 'O' };         //0
            public ushort entryLength = 0;
            public ushort choiceCount = 0;
            public string choiceQuestion;
            public List<string> choices = new List<string>();
        }

        //same as any other entry, it gets put into binary format and then returned
        public byte[] GenerateSaveData(List<string> assetNames) {
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    MultipleChoiceEntrySaveData data = new MultipleChoiceEntrySaveData();
                    writer.Write(data.magicNumber);
                    IEnumerable<VisualElement> choiceEntries = choiceContainer.Children();

                    foreach (VisualElement child in choiceEntries) {
                        if (child.GetType() == typeof(ChoiceEntry)) {
                            data.choiceCount++;
                        }
                    }

                    writer.Write(data.choiceCount);
                    data.choiceQuestion = choiceQuestion.text;
                    writer.Write(data.choiceQuestion);
                    foreach (VisualElement child in choiceEntries) {
                        if (child.GetType() == typeof(ChoiceEntry)) {
                            writer.Write(((ChoiceEntry)child).choiceText.text);
                        }
                    }

                    byte[] byteData = stream.ToArray();
                    writer.Close();
                    return byteData;
                }
            }
        }


        public void RestoreSaveData(BinaryReader reader, List<CharacterData> characterList) {
            long originalPosition = reader.BaseStream.Position;
            MultipleChoiceEntrySaveData data = new MultipleChoiceEntrySaveData();
            //data.magicNumber = reader.ReadChars(4);
            data.choiceCount = reader.ReadUInt16();
            data.choiceQuestion = reader.ReadString();
            choiceQuestion.value = data.choiceQuestion;
            for (int i = 0; i < data.choiceCount; i++) {
                ChoiceEntry entry = new ChoiceEntry(this, reader.ReadString());
                choiceContainer.Add(entry);
            }
        }
    }

    //unused here
    public class DialogChunk : VisualElement, IColorDragTargettable {
        public VisualElement dialogContainer;
        public VisualElement chunkLabel;
        private bool minimized = false;
        private bool clickedDown = false;

        //interface stuff
        public List<ILineDraggable> targettedBy = new List<ILineDraggable>();

        public DialogChunk() {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DialogEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "DialogChunk.uxml");
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(DialogEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "DialogChunk.uss");
            VisualElement ui = tree.CloneTree();
            dialogContainer = ui.Q<VisualElement>("dialog_container");
            chunkLabel = ui.Q<VisualElement>("chunk_label_box");
            chunkLabel.style.backgroundColor = Color.grey;

            chunkLabel.RegisterCallback<MouseDownEvent>(DummyEvent);
            chunkLabel.RegisterCallback<MouseUpEvent>(MinimizeChunk);
            this.AddManipulator(new DragManipulator());
            ui.styleSheets.Add(style);
            Add(ui);

            dialogContainer.Add(new DialogEntry());
        }

        public void DummyEvent(MouseDownEvent evt) {
            clickedDown = true;
            evt.StopImmediatePropagation();
        }

        public void MinimizeChunk(MouseUpEvent evt) {
            if (clickedDown) {
                if (!minimized) {
                    minimized = true;
                    this.EnableInClassList("Minimized", true);
                    this.style.flexGrow = 0;
                    dialogContainer.visible = false;
                } else {
                    minimized = false;
                    this.EnableInClassList("Minimized", false);
                    this.style.flexGrow = 1;
                    dialogContainer.visible = true;
                }

                clickedDown = false;
            }
        }

        public void SetTargetted(ILineDraggable targettingThis) {
            if (!targettedBy.Contains(targettingThis)) {
                targettedBy.Add(targettingThis);
            }
        }
    }

    public class ChoiceEntry : VisualElement {
        public MultipleChoiceContainer multipleChoiceElement;
        public Box textBox;
        public TextField choiceText;
        ContextualMenuManipulator mainMenuContextual;
        public VisualElement ui;
        //public DragLinkElement dragGraphic;
        public Color color;

        public ChoiceEntry(MultipleChoiceContainer _multipleChoiceElement, string choiceString) {
            multipleChoiceElement = _multipleChoiceElement;

            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DialogEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "ChoiceEntry.uxml");
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(DialogEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "ChoiceEntry.uss");

            ui = tree.CloneTree();
            ui.styleSheets.Add(style);
            textBox = ui.Q<Box>("entry_text_box");
            choiceText = ui.Q<TextField>("entry_text");
            //dragGraphic = ui.Q<DragLinkElement>("drag_graphic");

            color = Constants.colors[multipleChoiceElement.uniqueColorIndex++];
            textBox.style.backgroundImage = null;
            textBox.style.backgroundColor = color;

            mainMenuContextual = new ContextualMenuManipulator(MainMenu);
            mainMenuContextual.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.RightMouse });
            multipleChoiceElement.AddManipulator(mainMenuContextual);

            choiceText.value = choiceString;
            Add(ui);
        }

        public void MainMenu(ContextualMenuPopulateEvent evt) {
            evt.menu.AppendAction("Delete entry", DeleteEntry);
            evt.StopPropagation();
        }

        public void DeleteEntry(DropdownMenuAction evt) {
            EditorWindow.GetWindow<DialogEditorUIE>().RemoveEntry(textBox.GetFirstAncestorOfType<MultipleChoiceContainer>());
        }

        public ChoiceEntry(MultipleChoiceContainer _multipleChoiceElement) : this(_multipleChoiceElement, "<Enter text for choice>") { }
    }
}
