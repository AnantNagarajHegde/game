using System;
using System.Globalization;

namespace Paridhwani {
 public enum TeaKeyboardAction { None, Edited, Submit, Cancel }
 // Pure editing/navigation policy, shared by the visible keyboard and executable checks.
 public sealed class TeaKeyboardModel {
  public const int Columns=10,RowCount=4,KeyCount=30,MaxLength=512;
  public const int Space=26,Backspace=27,Send=28,Cancel=29;
  public string Text {get;private set;}="";
  public int Selected {get;private set;}
  public bool IsOpen {get;private set;}
  public bool Caps {get;private set;}
  public bool Symbols {get;private set;}
  const string LetterKeys="qwertyuiopasdfghjklzxcvbnm";
  const string SymbolKeys="1234567890-/:;()@&+.,?!'#%";
  static readonly int[] RowStarts={0,10,19,26}, RowLengths={10,9,7,4};
  int lastActionFrame=-1,heldX,heldY;
  double nextMove;
  public void Open(string draft){SetText(draft);Selected=0;IsOpen=true;Caps=false;Symbols=false;heldX=heldY=0;nextMove=0;lastActionFrame=-1;}
  public void Close(){IsOpen=false;heldX=heldY=0;}
  public void SetText(string text){
   text=text??"";if(text.Length<=MaxLength){Text=text;return;}
   int end=MaxLength;if(char.IsHighSurrogate(text[end-1]))end--;Text=text.Substring(0,end);
  }
  public void Select(int index){Selected=Math.Max(0,Math.Min(KeyCount-1,index));}
  public static string Label(int index)=>index>=0&&index<26?LetterKeys[index].ToString():index==Space?"Space":index==Backspace?"Backspace":index==Send?"Send":"Hide";
  public static int RowStart(int row)=>RowStarts[row];
  public static int RowLength(int row)=>RowLengths[row];
  public static float RowInset(int row)=>row==3?0:(Columns-RowLengths[row])*.5f;
  public static int RowOf(int index){for(int row=RowCount-1;row>=0;row--)if(index>=RowStarts[row])return row;return 0;}
  static float KeyCenter(int row,int column)=>row==3?(column+.5f)*Columns/RowLengths[row]:RowInset(row)+column+.5f;
  static int ClosestColumn(int row,float center){
   int result=0;float best=float.MaxValue;
   for(int col=0;col<RowLengths[row];col++){float distance=Math.Abs(KeyCenter(row,col)-center);if(distance<best){best=distance;result=col;}}
   return result;
  }
  public string KeyLabel(int index)=>index>=26?Label(index):Symbols?SymbolKeys[index].ToString():Caps?Label(index).ToUpperInvariant():Label(index);
  public void ToggleCaps(){if(IsOpen)Caps=!Caps;}
  public void ToggleSymbols(){if(IsOpen)Symbols=!Symbols;}
  public bool Move(int horizontal,int vertical,double now){
   if(!IsOpen)return false;
   horizontal=Math.Sign(horizontal);vertical=Math.Sign(vertical);
   if(horizontal!=0)vertical=0;
   if(horizontal==0&&vertical==0){heldX=heldY=0;return false;}
   bool changed=horizontal!=heldX||vertical!=heldY;
   if(!changed&&now<nextMove)return false;
   heldX=horizontal;heldY=vertical;nextMove=now+(changed?.4:.09);
   int row=RowOf(Selected),column=Selected-RowStarts[row],target;
   if(horizontal!=0)target=RowStarts[row]+Math.Max(0,Math.Min(RowLengths[row]-1,column+horizontal));
   else {int nextRow=Math.Max(0,Math.Min(RowCount-1,row+vertical));target=RowStarts[nextRow]+ClosestColumn(nextRow,KeyCenter(row,column));}
   bool moved=target!=Selected;Selected=target;return moved;
  }
  bool Take(int frame){if(!IsOpen||lastActionFrame==frame)return false;lastActionFrame=frame;return true;}
  public TeaKeyboardAction Activate(int frame){
   if(!Take(frame))return TeaKeyboardAction.None;
   if(Selected==Send){Close();return TeaKeyboardAction.Submit;}
   if(Selected==Cancel){Close();return TeaKeyboardAction.Cancel;}
   if(Selected==Backspace){Erase();return TeaKeyboardAction.Edited;}
   Append(Selected==Space?" ":KeyLabel(Selected));return TeaKeyboardAction.Edited;
  }
  public TeaKeyboardAction Submit(int frame){Select(Send);return Activate(frame);}
  public TeaKeyboardAction Dismiss(int frame){Select(Cancel);return Activate(frame);}
  public TeaKeyboardAction Type(char character,int frame){
   if(char.IsControl(character)||!IsOpen)return TeaKeyboardAction.None;
   Append(character.ToString());return TeaKeyboardAction.Edited;
  }
  public TeaKeyboardAction Delete(int frame){if(!Take(frame))return TeaKeyboardAction.None;Erase();return TeaKeyboardAction.Edited;}
  public TeaKeyboardAction InsertSpace(int frame){if(!Take(frame))return TeaKeyboardAction.None;Append(" ");return TeaKeyboardAction.Edited;}
  void Append(string value){if(Text.Length+value.Length<=MaxLength)Text+=value;}
  void Erase(){if(Text.Length>0){var starts=StringInfo.ParseCombiningCharacters(Text);Text=Text.Substring(0,starts[starts.Length-1]);}}
 }
}
