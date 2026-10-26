using UnityEngine.UIElements;
namespace Paridhwani
{
    public sealed partial class TeaStallUI
    {
        public void ShowRidgeActivity(string feedback="")
        {
            var card=Card("evidence","paper"); Class(card,"evidence-card");
            var header=new VisualElement(); Class(header,"row"); card.Add(header);
            Add(header,"RIDGE / STONE MARKER","eyebrow"); Button(header,session.EvidenceBackLabel+" [Esc]",session.Close,"quiet-button");
            var scroll=new ScrollView(); Class(scroll,"evidence-scroll"); card.Add(scroll);
            Add(scroll,"The sheltered bend","title");
            Line(scroll,new TeaLine("ಎಡಕ್ಕೆ ಹೋಗಿ. ಬೇಲಿಯ ಪಕ್ಕದಲ್ಲಿ ನಡೆಯಿರಿ.","Edakke hogi. Beliya pakkadalli nadeyiri.","Go left. Walk beside the fence."));
            Add(scroll,"The left trail bends beside the fence towards Gudda Mane. The old road remains closed. Cleared branches have been stacked outside the walking line.","body");
            if(feedback!="") Add(scroll,feedback,"feedback");
            if(!RidgeInvestigation.CanWalk(session.Progress,session.ridgeRouteChecked)) Add(scroll,"The checked route and Kencha's permission are required before continuing. Return to his shelter if the route is closed.","body");
            else if(!session.Progress.ridgeWayfindingKnown) {
                Add(scroll,"WHAT COMES NEXT?","section-title");
                Answers(scroll,new[]{new TeaLine("ಎಡಕ್ಕೆ. ಬೇಲಿಯ ಪಕ್ಕದಲ್ಲಿ.","Edakke. Beliya pakkadalli.","Left; continue beside the fence"),new TeaLine("ಬೇಲಿ ದಾಟಿ.","Beli daati.","Climb over the fence"),new TeaLine("ಹಳೆಯ ರಸ್ತೆ.","Haleya raste.","Use the closed old road")});
            } else Add(scroll,"Direction recorded. Close this page and follow the fenced path through the shelter bend to the farmhouse veranda. You can walk back along the same route.","success");
            Add(scroll,"No jumping, stamina test or reading countdown. Tab opens your field notes and returns here.","small");
            var aids=new VisualElement(); Class(aids,"aids"); card.Add(aids);
            var english=new Toggle("English meaning"){value=session.Progress.english}; english.RegisterValueChangedCallback(e=>{session.Progress.english=e.newValue;session.SettingsChanged();ShowRidgeActivity();}); aids.Add(english);
            var roman=new Toggle("Roman letters"){value=session.Progress.roman}; roman.RegisterValueChangedCallback(e=>{session.Progress.roman=e.newValue;session.SettingsChanged();ShowRidgeActivity();}); aids.Add(roman);
        }
    }
}
