public class BoolLogicNode : BaseNode {
    public BoolLogicNode() {
        LoadTemplateAndStyleFromName("BoolLogicNode");
        NodeVert trueVert = new NodeVert();
        trueVert.id = "trueVert";
        NodeVert falseVert = new NodeVert();
        falseVert.id = "falseVert";
        AddVertToContainer(trueVert, "trueContainer");
        AddVertToContainer(falseVert, "falseContainer");
        SetBottomVertContainerVisibility(false);
    }
}