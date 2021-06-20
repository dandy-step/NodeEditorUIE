using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using System.Collections.Generic;

namespace DialogTool {
    public class ScrollingLabel : ScrollView {
        string text;
        private TextElement label;
        public float startWaitValue = 1f;
        public float endWaitValue = 1.25f;
        ValueAnimation<float> scrollAnim = null;
        ValueAnimation<float> anim = null;

        public class Factory : UxmlFactory<ScrollingLabel, Traits> { }

        public class Traits : UxmlTraits {
            UxmlStringAttributeDescription text = new UxmlStringAttributeDescription() { name = "scrollingLabel" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                ((ScrollingLabel)ve).label.text = text.GetValueFromBag(bag, cc);
            }
        }

        public ScrollingLabel() : this("Default Label") { }

        public ScrollingLabel(string labelText) {
            EnableInClassList("unity-button", true);
            showHorizontal = false;
            showVertical = false;
            label = new TextElement() { text = "Really Long Text Goes Here Dude Look At This" };
            text = label.text;
            this.AddManipulator(new Clickable(() => { }));  //fixes drag event capturing
            this.contentViewport.style.flexDirection = FlexDirection.Row;
            this.contentContainer.style.flexDirection = FlexDirection.Row;
            Add(label);

            this.RegisterCallback<TooltipEvent>((x) => { x.tooltip = label.text; x.rect = worldBound;}, TrickleDown.TrickleDown);
            label.RegisterCallback<GeometryChangedEvent>(CheckIfScrollNecessary, TrickleDown.NoTrickleDown);
            this.horizontalScroller.style.display = DisplayStyle.None;
            this.verticalScroller.style.display = DisplayStyle.None;
        }

        public void DoAnim(VisualElement x, float y) {
            this.showHorizontal = false;
            this.horizontalScroller.style.display = DisplayStyle.None;
            anim.to = horizontalScroller.highValue;
            Debug.Log(y + "/" + horizontalScroller.highValue);
            this.horizontalScroller.value = y;
            if (y == anim.to) {
                horizontalScroller.value = 0;
                anim.Stop();
                anim.durationMs = 4000;
                anim.Start();
            }
        }

        public string GetLabelText() {
            return label.text;
        }

        public void SetLabelText(string newText) {
            label.text = newText;
        }

        public void ResetTextScroll() {
            if (label.layout.width > layout.width) {
                scrollAnim = label.experimental.animation.Start(horizontalScroller.lowValue, horizontalScroller.highValue, 10000, (x, y) => { UpdateTextScroll(x, y); });
                scrollAnim.easingCurve = Easing.InOutBack;
                scrollAnim.onAnimationCompleted = ResetTextScroll;
                scrollAnim.autoRecycle = true;
            } else {
                Debug.Log("not resetting anim because of layout dims");
            }
        }

        public void UpdateTextScroll(VisualElement ui, float value) {
            scrollAnim.to = horizontalScroller.highValue;
            scrollAnim.durationMs = Mathf.Max(1000 * ((((int)scrollAnim.to + (int)layout.width)) / 25), 3000);
            horizontalScroller.style.display = DisplayStyle.None;
            horizontalScroller.value = value;
        }

        public void CheckIfScrollNecessary(GeometryChangedEvent evt) {
            if (evt.newRect.width > layout.width) {
                contentViewport.style.justifyContent = Justify.FlexStart;
                scrollAnim = label.experimental.animation.Start(horizontalScroller.lowValue, horizontalScroller.highValue, 10000, (x, y) => { UpdateTextScroll(x, y); });
                scrollAnim.easingCurve = Easing.InOutBack;
                scrollAnim.onAnimationCompleted = ResetTextScroll;
                scrollAnim.autoRecycle = true;
            } else {
                contentViewport.style.justifyContent = Justify.Center;
                if (scrollAnim != null) {
                    scrollAnim.Stop();
                }
            }
        }
    }
}