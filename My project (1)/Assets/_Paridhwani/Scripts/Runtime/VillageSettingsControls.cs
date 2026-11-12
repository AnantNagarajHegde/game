using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Paridhwani
{
    // Keep Unity's native input, focus, and change events; replace only presentation.
    public sealed class VillageSwitch : Toggle
    {
        readonly Label stateLabel;

        public VillageSwitch(string label) : base(label)
        {
            AddToClassList("village-switch");
            var input = this.Q(className: Toggle.inputUssClassName);
            var thumb = new VisualElement { pickingMode = PickingMode.Ignore };
            thumb.AddToClassList("switch-thumb"); input.Add(thumb);
            stateLabel = new Label { name = "switch-state", pickingMode = PickingMode.Ignore, enableRichText = false };
            stateLabel.AddToClassList("switch-state"); Add(stateLabel);
            RefreshState();
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshState();
        }

        void RefreshState()
        {
            EnableInClassList("switch-on", value);
            if (stateLabel != null) stateLabel.text = value ? "On" : "Off";
        }
    }

    public sealed class VillageSlider : Slider
    {
        readonly Label readout;
        readonly Func<float, string> format;

        public VillageSlider(string label, float low, float high, string lowLabel = "0%", string highLabel = "100%", Func<float, string> format = null)
            : base(label, low, high)
        {
            this.format = format ?? (number => Mathf.RoundToInt(Mathf.InverseLerp(low, high, number) * 100) + "%");
            AddToClassList("village-slider");
            fill = true;
            pageSize = 0;
            readout = new Label { name = "slider-value", pickingMode = PickingMode.Ignore, enableRichText = false };
            readout.AddToClassList("slider-value"); Add(readout);
            var range = new VisualElement { pickingMode = PickingMode.Ignore };
            range.AddToClassList("slider-range");
            range.Add(new Label(lowLabel) { pickingMode = PickingMode.Ignore });
            range.Add(new Label(highLabel) { pickingMode = PickingMode.Ignore });
            Add(range);
            tooltip = "Drag to adjust. Arrow keys or the controller adjust the selected slider.";
            RefreshReadout();
        }

        public override void SetValueWithoutNotify(float newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshReadout();
        }

        void RefreshReadout()
        {
            if (readout != null && format != null) readout.text = format(value);
        }
    }
}
