using System;
using System.Collections.Generic;
using Paridhwani;
class TeaKeyboardChecks {
 static int checks;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static void Main(){
  var keyboard=new TeaKeyboardModel();keyboard.Open("");
  Check(keyboard.IsOpen&&keyboard.Selected==0&&keyboard.KeyLabel(0)=="q","opening selects q");
  string[] expectedRows={"qwertyuiop","asdfghjkl","zxcvbnm"};
  for(int row=0;row<3;row++){
   string actual="";for(int col=0;col<TeaKeyboardModel.RowLength(row);col++)actual+=keyboard.KeyLabel(TeaKeyboardModel.RowStart(row)+col);
   Check(actual==expectedRows[row],"QWERTY row "+row);
  }
  Check(TeaKeyboardModel.RowLength(3)==4 && keyboard.KeyLabel(TeaKeyboardModel.Space)=="Space" && keyboard.KeyLabel(TeaKeyboardModel.Backspace)=="Backspace" && keyboard.KeyLabel(TeaKeyboardModel.Send)=="Send" && keyboard.KeyLabel(TeaKeyboardModel.Cancel)=="Hide","action row has four usable keys");
  for(int row=0;row<TeaKeyboardModel.RowCount;row++)for(int col=0;col<TeaKeyboardModel.RowLength(row);col++){
   int index=TeaKeyboardModel.RowStart(row)+col;keyboard.Select(index);keyboard.Move(0,0,0);keyboard.Move(-1,0,0);
   Check(keyboard.Selected==TeaKeyboardModel.RowStart(row)+Math.Max(0,col-1),"left moves one key or stops at row edge");
   keyboard.Select(index);keyboard.Move(0,0,0);keyboard.Move(1,0,0);
   Check(keyboard.Selected==TeaKeyboardModel.RowStart(row)+Math.Min(TeaKeyboardModel.RowLength(row)-1,col+1),"right moves one key or stops at row edge");
   foreach(int direction in new[]{-1,1}){keyboard.Select(index);keyboard.Move(0,0,0);keyboard.Move(0,direction,0);int wanted=Math.Max(0,Math.Min(TeaKeyboardModel.RowCount-1,row+direction));Check(TeaKeyboardModel.RowOf(keyboard.Selected)==wanted,"vertical move stays in adjacent row");}
  }
  keyboard.Open("");Check(keyboard.Move(1,0,0)&&keyboard.Selected==1,"initial move immediate");
  Check(!keyboard.Move(1,0,.39)&&keyboard.Selected==1,"held direction initial delay");
  Check(keyboard.Move(1,0,.4)&&keyboard.Selected==2,"held direction first repeat");
  Check(!keyboard.Move(1,0,.48)&&keyboard.Selected==2,"repeat rate bounded");
  Check(keyboard.Move(1,0,.50)&&keyboard.Selected==3,"held direction next repeat");
  keyboard.Move(0,0,.51);Check(keyboard.Move(1,0,.52)&&keyboard.Selected==4,"release re-arms immediate movement");
  keyboard.Select(0);Check(keyboard.Activate(1)==TeaKeyboardAction.Edited&&keyboard.Text=="q","controller enters selected character");
  Check(keyboard.Activate(1)==TeaKeyboardAction.None&&keyboard.Text=="q","same-frame duplicate confirm discarded");
  Check(keyboard.Activate(2)==TeaKeyboardAction.Edited&&keyboard.Text=="qq","repeated confirm enters another character");
  Check(keyboard.InsertSpace(3)==TeaKeyboardAction.Edited&&keyboard.Text=="qq ","right shoulder inserts space");
  Check(keyboard.Delete(3)==TeaKeyboardAction.None&&keyboard.Text=="qq ","same-frame duplicate edit discarded");
  Check(keyboard.Delete(4)==TeaKeyboardAction.Edited&&keyboard.Text=="qq","left shoulder deletes");
  keyboard.SetText("a\u0301");keyboard.Delete(5);Check(keyboard.Text=="","backspace deletes combined character once");
  keyboard.SetText("x\ud83d\ude00");keyboard.Delete(6);Check(keyboard.Text=="x","backspace preserves surrogate pairs");
  keyboard.SetText(new string('a',512));keyboard.Select(1);keyboard.Activate(7);Check(keyboard.Text.Length==512,"grid length bound");
  keyboard.SetText(new string('x',511)+"\ud83d\ude00");Check(keyboard.Text.Length==511,"text bound does not split surrogate");
  keyboard.SetText("where is ananya");Check(keyboard.Dismiss(8)==TeaKeyboardAction.Cancel&&!keyboard.IsOpen&&keyboard.Text=="where is ananya","hide retains unsent draft");
  Check(keyboard.Activate(9)==TeaKeyboardAction.None&&!keyboard.Move(1,0,2),"closed keyboard ignores input");
  keyboard.Open(keyboard.Text);Check(keyboard.Selected==0&&keyboard.Text=="where is ananya","reopen resets focus and restores draft");
  keyboard.Type('?',10);keyboard.Type('!',10);Check(keyboard.Text=="where is ananya?!","physical input accepts multiple characters per frame");
  Check(keyboard.Submit(11)==TeaKeyboardAction.Submit&&!keyboard.IsOpen,"submit closes once");
  Check(keyboard.Submit(11)==TeaKeyboardAction.None&&keyboard.Submit(12)==TeaKeyboardAction.None,"closed submission cannot duplicate");
  keyboard.Open("");keyboard.ToggleCaps();keyboard.Select(0);keyboard.Activate(20);Check(keyboard.Text=="Q"&&keyboard.KeyLabel(1)=="W","caps changes displayed and entered letters");
  keyboard.ToggleSymbols();var symbols=new HashSet<string>();for(int i=0;i<26;i++)symbols.Add(keyboard.KeyLabel(i));
  Check(symbols.Count==26&&keyboard.KeyLabel(0)=="1"&&keyboard.KeyLabel(25)=="%","symbol page contains distinct digits and punctuation");
  keyboard.Select(10);keyboard.Activate(21);Check(keyboard.Text=="Q-","symbol page enters displayed character");
  keyboard.ToggleSymbols();Check(keyboard.KeyLabel(0)=="Q","returning from symbols preserves caps");
  keyboard.ToggleCaps();Check(keyboard.KeyLabel(0)=="q","caps can be turned off");
  Console.WriteLine("TEA_KEYBOARD_CHECKED: "+checks+" assertions; QWERTY rows, navigation, editing, repeat and shortcuts.");
 }
}
