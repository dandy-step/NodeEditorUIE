using UnityEngine;
using UnityEngine.UIElements;
using System;
using DialogTool;

//handles dragging of individual DialogEntries on the list - generates a floating preview while you drag, highlights the new position, and updates the list automatically to reflect it
public class DialogDragger : MouseManipulator {
    float dragDistanceTrigger = 50f;
    private bool active = false;
    private bool dragConfirmed = false;
    ScrollView root;
    DialogEntry draggedElement;
    DialogEntry floatingElement;
    Vector2 dragOrigin;

    public DialogDragger(ScrollView _root) {
        root = _root;
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        active = false;
    }

    protected override void RegisterCallbacksOnTarget() {
        target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        target.RegisterCallback<MouseMoveEvent>(OnDrag);
        target.RegisterCallback<MouseUpEvent>(OnRelease);
    }

    protected override void UnregisterCallbacksFromTarget() {
        throw new NotImplementedException();
    }

    void OnMouseDown(MouseDownEvent evt) {
        if (active) {
            evt.StopImmediatePropagation();
            return;
        }

        if ((CanStartManipulation(evt) && !active)) {
            foreach (VisualElement child in root.Children()) {
                if (child.GetType() == typeof(DialogEntry)) {
                    if (child.layout.Contains((evt.localMousePosition + root.scrollOffset))) {
                        active = true;
                        Debug.Log("Found dragged element with id " + ((DialogEntry)child).id);
                        draggedElement = (DialogEntry)child;
                    } else {
                        Debug.Log("NOT DRAGGING: "+ evt.localMousePosition + root.scrollOffset);
                    }
                }
            }

            evt.StopImmediatePropagation();
        }
    }

    void OnDrag(MouseMoveEvent evt) {
        if (!active) {
            return;
        } else {
            if (!dragConfirmed) {
                if (Vector2.Distance((evt.localMousePosition + root.scrollOffset), dragOrigin) > dragDistanceTrigger) {
                    dragConfirmed = true;
                    floatingElement = new DialogEntry(draggedElement.id, draggedElement.GetDialogText(), draggedElement.character);
                    draggedElement.parent.Add(floatingElement);
                    dragOrigin = evt.localMousePosition + root.scrollOffset;
                    floatingElement.style.position = Position.Absolute;
                    floatingElement.Q<VisualElement>("dialog_entry_container").style.opacity = 0.15f;
                    floatingElement.name = "Floating Dragged Element";
                    root.Add(floatingElement);
                }
            }

            if (dragConfirmed && (floatingElement != null)) {
                if (Input.GetKeyDown(KeyCode.Escape)) {
                    evt.StopImmediatePropagation();
                    return;
                } else {
                    floatingElement.style.left = (evt.localMousePosition.x - (floatingElement.layout.width / 2)) + root.scrollOffset.x;
                    floatingElement.style.top = (evt.localMousePosition.y - (floatingElement.layout.height / 2)) + root.scrollOffset.y;

                    //highlight background node
                    VisualElement hoveredEntry = GetHoveredElement(root, evt.localMousePosition + root.scrollOffset);

                    if (hoveredEntry.GetType() != typeof(DialogEntry)) {
                        return;
                    }

                    foreach (DialogEntry child in root.Children()) {
                        if (child == hoveredEntry) {
                            child.SetBackgroundColor(Color.yellow);
                        } else {
                            child.SetBackgroundColor(child.character.editorColor);
                        }
                    }
                    evt.PreventDefault();
                    evt.StopImmediatePropagation();
                }
            }
        }
    }

    private VisualElement GetHoveredElement(ScrollView root, Vector2 mousePos) {
        foreach (VisualElement child in root.Children()) {
            if ((child != floatingElement) && (child.layout.Contains(mousePos))) {
                if (child.GetType() == typeof(DialogEntry)) {
                    return (DialogEntry)root.ElementAt(root.IndexOf(child));
                }
            }
        }

        return new VisualElement();
    }

    void OnRelease(MouseUpEvent evt) {
        if (active && dragConfirmed) {
            DialogEntry dropElement = (DialogEntry) GetHoveredElement(root, evt.localMousePosition + root.scrollOffset);

            if (dropElement != null) {
                DialogEntry newEntry = new DialogEntry(draggedElement.id, draggedElement.GetDialogText(), draggedElement.character);
                newEntry.SetDialogText(draggedElement.GetDialogText());
                newEntry.SetSpeakerName(draggedElement.GetSpeakerName());
                newEntry.id = draggedElement.id;
                draggedElement.parent.Add(newEntry);
                root.Insert((draggedElement.layout.position.y > dropElement.layout.position.y) ? root.IndexOf(dropElement) : root.IndexOf(dropElement) + 1, newEntry);
                root.Remove(draggedElement);
                dropElement.SetBackgroundColor(dropElement.character.editorColor);
            }
            floatingElement.parent.Remove(floatingElement);
        }

        active = false;
        dragConfirmed = false;
    }
}
