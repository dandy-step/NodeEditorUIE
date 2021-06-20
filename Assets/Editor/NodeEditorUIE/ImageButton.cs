using UnityEngine;
using System;
using UnityEngine.UIElements;

//custom VisualElement for icon buttons
public class ImageButton : Button {
    Image icon = null;

    public ImageButton(int size, Texture2D img) : this(null, size, img) { }

    public ImageButton(Action action, int size, Texture2D img) : base(action) {
        icon = new Image() { image = img };
        icon.style.paddingBottom = 2;
        icon.style.paddingLeft = 2;
        icon.style.paddingRight = 2;
        icon.style.paddingTop = 2;
        Add(icon);
        style.width = size;
        style.height = size;
        icon.StretchToParentSize();
    }
}
