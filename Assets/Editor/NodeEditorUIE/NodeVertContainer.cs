using UnityEngine.UIElements;

//this VisualElement holds a graphic for a vert, as well as a container for some extra data (a label, for example). We need this because we're adding a manipulator to the graphic, and if we want to restrict the trigger to just the area of the graphic, it needs to be in its own container
public class NodeVertContainer : VisualElement {
    public VisualElement vertGraphicContainer;
    public VisualElement vertUIContainer;
    public NodeVert vert;

    public NodeVertContainer() {
        vertGraphicContainer = new VisualElement();
        vertUIContainer = new VisualElement();
        vert = new NodeVert();
        vertGraphicContainer.Add(vert);
        this.Add(vertGraphicContainer);
        this.Add(vertUIContainer);
        this.style.flexDirection = FlexDirection.Row;
    }
}
