using System;
using UnityEngine;

namespace Paridhwani
{
    // Keyboard focus belongs to the current modal. Text entry never submits story choices.
    public static class VillageInputPolicy
    {
        public static bool NotebookShortcut(string mode,bool editingText) =>
            !editingText && (mode=="" || mode=="notebook");
        public static bool ChoiceShortcut(string mode,bool editingText) =>
            !editingText && (mode=="conversation" || mode=="evidence");
        public static bool SaveShortcut(string mode,bool editingText,bool titleSettings) =>
            !editingText && !titleSettings && (mode=="" || mode=="conversation" || mode=="evidence" ||
            mode=="inspection" || mode=="notebook" || mode=="pause");
        public static Vector2 WalkInput(Vector2 input)
        {
            if(!float.IsFinite(input.x) || !float.IsFinite(input.y)) return Vector2.zero;
            double length=Math.Sqrt((double)input.x*input.x+(double)input.y*input.y);
            return length>1 ? new Vector2((float)(input.x/length),(float)(input.y/length)) : input;
        }
        public static bool ControllerNotebookShortcut(string mode,bool editingText) =>
            !editingText && (mode=="" || mode=="conversation" || mode=="evidence" || mode=="inspection" || mode=="notebook");
        // A stick expresses a turn rate; mouse input already expresses a distance this frame.
        public static Vector2 ControllerLookDelta(Vector2 stick,float seconds,float sensitivity)
        {
            if(!float.IsFinite(seconds) || seconds<=0 || !float.IsFinite(sensitivity)) return Vector2.zero;
            return WalkInput(stick)*(150f*seconds*Math.Clamp(sensitivity,.05f,.3f)/.12f);
        }
    }
}
