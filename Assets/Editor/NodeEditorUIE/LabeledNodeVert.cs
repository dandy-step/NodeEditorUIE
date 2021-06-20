using UnityEngine.UIElements;

public class LabeledNodeVert : NodeVertContainer {
    public TextElement label;

    public LabeledNodeVert(string labelText) : base() {
        label = new TextElement();
        label.text = labelText;
        this.EnableInClassList("LabeledVert", true);
        vertUIContainer.Add(label);
        vert.id = labelText + "_vert";
    }
}
