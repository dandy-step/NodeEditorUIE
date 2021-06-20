using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class CheckFlagNode : BaseNode {
    //sample flags to fetch, in practice you'd have a flag database in the game state that you would pull from and display here
    enum flagOptions {
        PICKED_UP_GUN,
        HAS_INTERRUPT_ITEM,
        BOUGHT_SANDWICH,
        KILLED_MONSTER,
    }

    public CheckFlagNode() {
        VisualTreeAsset checkFlagTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(NodeEditorUIEPaths.EDITOR_UXML_USS_ASSETS_ROOT_PATH + "CheckFlagNode.uxml");
        EnableInClassList("CheckFlagNode", true);
        checkFlagTemplate.CloneTree(nodeContentContainer);
        EnumField flags = nodeContentContainer.Q<EnumField>("flagEnumField");
        flagOptions test = new flagOptions();
        flags.Init(test);
        LabeledNodeVert trueVert = new LabeledNodeVert("If Set");
        trueVert.name = "ifSet";
        LabeledNodeVert falseVert = new LabeledNodeVert("If Not Set");
        falseVert.name = "ifNotSet";
        falseVert.style.flexDirection = FlexDirection.RowReverse;
        VisualElement labelVertContainer = nodeContentContainer.Q<VisualElement>("labelVertContainer");
        trueVert.vert.hostContainer = labelVertContainer;
        falseVert.vert.hostContainer = labelVertContainer;
        labelVertContainer.Add(trueVert);
        labelVertContainer.Add(falseVert);
        GetVertByID(BOTTOM_VERT_DEFAULT_ID).Hide();
    }
}
