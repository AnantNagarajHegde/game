using UnityEngine;
namespace Paridhwani.Editor {
 public static partial class TeaStallSceneBuilder {
 static void AddEpilogueChapter(TeaStallSession s) {
 s.epilogueSet=new GameObject("Reopened library epilogue");
 var board=Box("Reopened library board",V(.2f,1.6f,12.5f),V(2.2f,.6f,.08f),"Sign",false);board.transform.SetParent(s.epilogueSet.transform,true);
 Text("LIBRARY OPEN / AFTER REVIEW",V(.2f,1.6f,12.44f),.05f,false,0);var text=GameObject.Find("Shaped sign: LIBRARY OPEN / AFTER REVIEW");if(text) text.transform.SetParent(board.transform,true);
 var table=Box("Community reading table",V(-.2f,.65f,11.2f),V(1.4f,1.1f,.7f),"Wood");table.transform.SetParent(s.epilogueSet.transform,true);
 for(int i=0;i<5;i++) {var book=Box("Reopened library book",V(-.5f+i*.16f,1.23f,11.2f),V(.13f,.035f,.35f),i%2==0?"Cream":"Green",false);book.transform.SetParent(s.epilogueSet.transform,true);}
 }
 }
}
