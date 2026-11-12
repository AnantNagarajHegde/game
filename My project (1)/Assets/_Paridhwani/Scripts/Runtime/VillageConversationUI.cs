using UnityEngine.UIElements;

namespace Paridhwani
{
    public sealed partial class TeaStallUI
    {
        VisualElement AddConversationPhrases(ScrollView scroll, TeaTurn turn)
        {
            if(!session.ConversationPhrasesVisible || turn.Choices.Count==0)return null;
            var list=new VisualElement { name="dialogue-phrase-list" };
            Class(list,"conversation-phrases"); scroll.Add(list);
            Add(list,"CHOOSE A PHRASE","section-title");
            Add(list,"Say one of these, or type your own message. Choosing a phrase helps you practise and continue.","small");
            var buttons=new Button[turn.Choices.Count];
            for(int i=0;i<buttons.Length;i++)
            {
                int index=i;
                var button=Button(list,"",()=>session.ChooseConversationPhrase(turn,index),"choice");
                button.name="dialogue-phrase-"+i; buttons[i]=button;
                Line(button,turn.Choices[i].Line,true);
                button.RegisterCallback<NavigationMoveEvent>(e=> {
                    int step=e.direction==NavigationMoveEvent.Direction.Down?1:e.direction==NavigationMoveEvent.Direction.Up?-1:0;
                    if(step==0)return;
                    int next=index+step;
                    e.StopImmediatePropagation();
                    if(next>=0 && next<buttons.Length) { buttons[next].Focus(); scroll.ScrollTo(buttons[next]); }
                    else root.Q<Button>("dialogue-phrases")?.Focus();
                });
            }
            return list;
        }
    }
}
