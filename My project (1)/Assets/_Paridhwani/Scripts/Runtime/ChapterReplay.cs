using UnityEngine;
namespace Paridhwani {
 public enum ReplayChapter {Arrival,Library,Mess,Market,River,Temple,Archive,Panchayat,Checkpost,Ridge,Farm,Deed,Return,Council,Epilogue}
 public sealed class ChapterReplayNavigation {
 public string OriginMode {get;private set;}="pause";
 public string BackLabel => OriginMode=="main-menu"?"Back to title":OriginMode=="credits"?"Back to credits":"Back to Pause";
 public void OpenFrom(string mode) {
 if(mode=="chapter-replay")return;
 OriginMode=mode=="main-menu" || mode=="credits"?mode:"pause";
 }
 }
 public static class ChapterReplay {
 public static string Name(ReplayChapter c) {string[] names={"Arrival and tea","Library introduction","Shanthakka's mess","Market and timetable","River investigation","Temple witnesses","Library archive","Panchayat notice","Journey to the checkpost","Marked ridge","Gudda Mane reunion","Trunk and original deed","Return with Ananya","Library court council","Later review and epilogue"};return (int)c>=0 && (int)c<names.Length?names[(int)c]:"Unknown chapter";}
 public static bool IsUnlocked(TeaProgress p,ReplayChapter c) {
 switch(c) {
 case ReplayChapter.Arrival:return true;case ReplayChapter.Library:return p.directionsKnown;case ReplayChapter.Mess:return p.messLeadKnown;case ReplayChapter.Market:return p.marketLeadKnown;case ReplayChapter.River:return p.riverReportKnown;case ReplayChapter.Temple:return p.templeLeadKnown;case ReplayChapter.Archive:return p.guddaFamilyLeadKnown;case ReplayChapter.Panchayat:return p.thursdayStarted;case ReplayChapter.Checkpost:return p.checkpostDestinationKnown;case ReplayChapter.Ridge:return p.trailInstructionsUnderstood;case ReplayChapter.Farm:return p.guddaManeReached;case ReplayChapter.Deed:return p.trunkLocationKnown;case ReplayChapter.Return:return p.deedRetrieved;case ReplayChapter.Council:return p.fridayFinaleStarted;case ReplayChapter.Epilogue:return p.workSuspended;default:return false;
 }
 }
 public static TeaProgress Seed(ReplayChapter chapter,TeaProgress preferences) {
 int n=(int)chapter;if(n<0 || n>14) throw new System.ArgumentOutOfRangeException(nameof(chapter));
 var p=new TeaProgress {arrivalAcknowledged=true};PresentationPolicy.Copy(preferences,p);
 if(n>=1) {p.greeted=p.drinkOrdered=p.directionsKnown=p.signRead=true;p.drink="tea";}
 if(n>=2) p.metSomanna=p.messLeadKnown=true;
 if(n>=3) p.mealOrdered=p.servingComplete=p.shoppingNoteCollected=p.marketLeadKnown=true;
 if(n>=4) p.marketReceiptCollected=p.purchaseTimeKnown=p.marketAccountKnown=p.busDepartureKnown=p.riverReportKnown=true;
 if(n>=5) p.giriAccountKnown=p.bicycleFrameObserved=p.bicycleStandObserved=p.bookingSlipFound=p.bookingSlipRead=p.riverDeductionKnown=p.templeLeadKnown=true;
 if(n>=6) p.gurumurthyAccountKnown=p.rathnammaAccountKnown=p.witnessAccountsCompared=p.guddaFamilyLeadKnown=true;
 if(n>=7) p.libraryAccessGranted=p.archiveShelfLocated=p.deedReferenceFound=p.thursdayStarted=true;
 if(n>=8) p.noticeIdentified=p.deadlineKnown=p.deedRequired=p.checkpostDestinationKnown=true;
 if(n>=9) p.checkpostVisited=p.checkpostAccountKnown=p.ananyaLocationReported=p.ridgeRouteOpen=p.trailInstructionsUnderstood=p.trailPermission=true;
 if(n>=10) p.ridgeWayfindingKnown=p.ridgeCrossed=p.guddaManeReached=true;
 if(n>=11) p.ananyaFound=p.disappearanceExplained=p.farmHospitalityKnown=p.trunkLocationKnown=true;
 if(n>=12) p.trunkOpened=p.deedPurposeKnown=p.deedRestrictionKnown=p.deedBeneficiaryKnown=p.deedConceptsReviewed=p.deedRetrieved=true;
 if(n>=13) p.returnJourneyStarted=p.returnedWithAnanya=p.fridayFinaleStarted=true;
 if(n>=14) p.deedPresented=p.finalePurposePresented=p.finaleRestrictionPresented=p.finaleBeneficiaryPresented=p.workSuspended=true;
 switch(chapter) {
 case ReplayChapter.Library:p.position=new Vector3(0,.2f,9);break;case ReplayChapter.Mess:p.position=new Vector3(3.7f,.2f,2);break;case ReplayChapter.Market:p.position=new Vector3(4,.2f,8.9f);break;case ReplayChapter.River:p.position=new Vector3(1.4f,.2f,6.5f);break;case ReplayChapter.Temple:p.position=new Vector3(1,.2f,23.4f);break;case ReplayChapter.Archive:p.position=new Vector3(-4.6f,.2f,9);break;case ReplayChapter.Panchayat:p.position=new Vector3(1,.2f,19.8f);break;case ReplayChapter.Checkpost:p.position=new Vector3(6.5f,.2f,-5.8f);break;case ReplayChapter.Ridge:p.position=new Vector3(48,.2f,10);break;case ReplayChapter.Farm:p.position=new Vector3(48,.2f,38.5f);break;case ReplayChapter.Deed:p.position=new Vector3(50.4f,.2f,45.6f);break;case ReplayChapter.Return:p.position=new Vector3(46,.2f,38.7f);break;case ReplayChapter.Council:case ReplayChapter.Epilogue:p.position=new Vector3(-2.2f,.2f,7.3f);break;
 }
 p.heading=chapter==ReplayChapter.River?-90:0;return p;
 }
 }
 public sealed partial class TeaStallSession {
 public string PracticeChapterName {get;private set;}="Arrival and tea";
 public void StartChapterPractice(ReplayChapter chapter) {
 if(IsPracticeVisit || !ChapterReplay.IsUnlocked(Progress,chapter))return;
 Save();if(!LastSaveSucceeded)return;completedStoryStore=saves;
 var next=ChapterReplay.Seed(chapter,Progress);PracticeChapterName=ChapterReplay.Name(chapter);Progress=next;
 saves=new TeaSaveStore(System.IO.Path.Combine(Application.temporaryCachePath,"paridhwani-practice-"+System.Guid.NewGuid().ToString("N")+".json"));
 EndForJourney();ResetActorsToOrigins();player.Restore(Progress.position,Progress.heading);returnTrail.Reset(Progress.position);ApplyState();Save();ui.ShowPracticeArrival();SetCursor(true);
 }
 }
}
