using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

//main VE for holding nodes, with some helper functions for validation and ID lookup
public class NodeContainer : VisualElement {
    int boundsMargin = 200;

    public NodeContainer() {
        this.style.flexGrow = 1;
        this.style.position = Position.Relative;
        this.contentContainer.style.flexGrow = 1;
        this.RegisterCallback<GeometryChangedEvent>((x) => { UpdateDimensions(); });
    }

    //calculates the bounds for the nodes, and manually updates the scrollview container to account for it (since the nodes have absolute positioning, we can't rely on generated values)
    public void UpdateDimensions() {
        List<VisualElement> nodes = contentContainer.Children().ToList();
        Rect maxBounds = new Rect();
        for (int i = 0; i < nodes.Count; i++) {
            if (nodes[i].GetType().IsSubclassOf(typeof(BaseNode))) {
                if ((nodes[i].layout.x < maxBounds.x)) {
                    maxBounds.x = nodes[i].layout.x;
                }

                if ((nodes[i].layout.y < maxBounds.y)) {
                    maxBounds.y = nodes[i].layout.y;
                }

                if ((nodes[i].layout.x + nodes[i].layout.width) > maxBounds.width) {
                    maxBounds.width = nodes[i].layout.x + nodes[i].layout.width;
                }

                if ((nodes[i].layout.y + nodes[i].layout.height) > maxBounds.height) {
                    maxBounds.height = nodes[i].layout.y + nodes[i].layout.height;
                }
            }
        }

        this.style.width = (-maxBounds.x + maxBounds.width) + boundsMargin;
        this.style.height = (-maxBounds.y + maxBounds.height) + boundsMargin;
        this.style.left = -maxBounds.x;
        this.style.top = -maxBounds.y;
        this.GetFirstAncestorOfType<ScrollView>().contentContainer.style.width = this.style.width;
        this.GetFirstAncestorOfType<ScrollView>().contentContainer.style.height = this.style.height;
        this.GetFirstAncestorOfType<ScrollView>().contentViewport.style.width = this.GetFirstAncestorOfType<ScrollView>().layout.width;
        this.GetFirstAncestorOfType<ScrollView>().contentViewport.style.height = this.GetFirstAncestorOfType<ScrollView>().layout.height;
        
    }

    public BaseNode GetNodeByID(string id) {
        List<VisualElement> nodes = this.Children().ToList();
        for (int i = 0; i < nodes.Count; i++) {
            if (IsNodeType(nodes[i])) {
                if (string.Equals(((BaseNode)nodes[i]).id, id)) {
                    return (BaseNode)nodes[i];
                }
            }
        }

        Debug.Log("Failed to find node with id " + id);
        return null;
    }

    //checks if element passed is a subclass of BaseNode, and thus a valid node
    public bool IsNodeType(VisualElement elem) {
        return ((elem.GetType() == typeof(BaseNode)) || (elem.GetType().IsSubclassOf(typeof(BaseNode))));
    }
}
