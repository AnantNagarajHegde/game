using System;
using System.IO;
using UnityEngine;

namespace Paridhwani
{
    [Serializable]
    public sealed class TeaProgress
    {
        public int version = 1;
        public bool greeted, drinkOrdered, directionsKnown, signRead, ananyaRoutineKnown;
        public bool arrivalAcknowledged,metSomanna,messLeadKnown,mealOrdered,servingComplete,shoppingNoteCollected,marketLeadKnown;
        public bool marketReceiptCollected,purchaseTimeKnown,marketAccountKnown,busDepartureKnown,riverReportKnown;
        public bool giriAccountKnown,bicycleFrameObserved,bicycleStandObserved,bookingSlipFound,bookingSlipRead,riverDeductionKnown,templeLeadKnown;
        public bool gurumurthyAccountKnown,rathnammaAccountKnown,witnessAccountsCompared,guddaFamilyLeadKnown;
        public bool libraryAccessGranted,archiveShelfLocated,deedReferenceFound,thursdayStarted;
        public bool noticeIdentified,deadlineKnown,deedRequired,checkpostDestinationKnown;
        public bool checkpostVisited,checkpostAccountKnown,ananyaLocationReported,ridgeRouteOpen,trailInstructionsUnderstood,trailPermission;
        public bool ridgeWayfindingKnown,ridgeCrossed,guddaManeReached,ananyaFound,disappearanceExplained,farmHospitalityKnown,trunkLocationKnown;
        public bool trunkOpened,deedPurposeKnown,deedRestrictionKnown,deedBeneficiaryKnown,deedConceptsReviewed,deedRetrieved;
        public bool returnJourneyStarted,returnedWithAnanya,fridayFinaleStarted;
        public bool deedPresented,finalePurposePresented,finaleRestrictionPresented,finaleBeneficiaryPresented,workSuspended;
        public bool reviewOutcomeKnown,libraryProtected,epilogueStarted,storyCompleted;
        public StoryEnding endingChoice;
        public string drink = "";
        public bool english = true, roman = true, muted;
        public bool reducedMotion,invertY;
        public float masterVolume=1;
        public VillageQuality quality;
        public float textScale = 1f, sensitivity = 0.12f;
        public Vector3 position = new Vector3(2.1f, 0.2f, -5.7f);
        public float heading = -18f;
        public string Objective => storyCompleted ? "Story complete - revisit the village or practise again" : epilogueStarted ? "Meet Somanna or Ananya and choose what comes next" : workSuspended ? "Works suspended - await the council review outcome" : fridayFinaleStarted ? "Present the protected original at the library court" : returnedWithAnanya ? "Confirm Friday morning preparations with Ananya" : returnJourneyStarted ? "Return with Ananya via the checkpost and auto" : deedRetrieved ? "Prepare the return journey with Ananya" : deedConceptsReviewed ? "Protect the original deed in its folder" : trunkOpened ? "Read and review the three deed clauses" : trunkLocationKnown ? "Open the family trunk in the farm archive" : disappearanceExplained ? "Ask Kamalamma about the original deed" : ananyaFound ? "Hear why Ananya could not return" : guddaManeReached ? "Meet Ananya on the farmhouse veranda" : ridgeWayfindingKnown ? "Follow the fenced path to Gudda Mane" : trailPermission ? "Checked ridge approach available - review the trailhead" : trailInstructionsUnderstood ? "Wait for Kencha to confirm the ridge is checked" : checkpostAccountKnown ? "Review Kencha\u0027s safe ridge directions" : checkpostVisited ? "Ask Kencha about Ananya and the road closure" : checkpostDestinationKnown ? "Ask Raghavendra for a ride to the forest checkpost" : deadlineKnown ? "Compare the archive reference with the notice checklist" : noticeIdentified ? "Read the clearance day and time" : thursdayStarted ? "Read the public panchayat notice" : deedReferenceFound ? "Return to Somanna when ready to rest" : libraryAccessGranted ? "Read the catalogue inside the library" : guddaFamilyLeadKnown ? "Bring the Gudda Mane family name to Somanna" : witnessAccountsCompared ? "Ask Rathnamma about the Gudda Mane family" : gurumurthyAccountKnown && rathnammaAccountKnown ? "Compare the two witness accounts" : templeLeadKnown ? "Speak to Gurumurthy and Rathnamma at the northern stop" :
            riverDeductionKnown ? "Ask Giri about witnesses near the northern stop" :
            RiverInvestigation.CanDeduce(this) ? "Compare the bicycle and booking evidence with Giri" :
            bookingSlipFound && !bookingSlipRead ? "Read the advance booking slip" :
            giriAccountKnown ? "Inspect the bicycle’s frame, stand and carrier pocket" :
            riverReportKnown ? "Follow the river path and speak to Giri" :
            busDepartureKnown ? "Read Giri’s notice beside the timetable" :
            marketAccountKnown ? "Check Route 14 at the bus-stand timetable" :
            purchaseTimeKnown ? "Ask Basavaraju about Ananya’s journey" :
            marketReceiptCollected ? "Read the purchase day and time on the ledger" :
            marketLeadKnown ? "Show Ananya’s shopping note to Basavaraju" :
            shoppingNoteCollected ? "Ask Shanthakka about the shopping note" :
            messLeadKnown ? "Visit Shanthakka at the mess across the lane" :
            metSomanna ? "Ask Somanna about Ananya" :
            directionsKnown ? "Follow the lane towards the library" :
            drinkOrdered ? "Ask Manjunatha where the library is" : "Introduce yourself at the tea stall";
    }

    public interface ITeaProgressCodec
    {
        string Encode(TeaProgress value);
        TeaProgress Decode(string json);
    }
    public sealed class UnityTeaProgressCodec : ITeaProgressCodec
    {
        public string Encode(TeaProgress value) => JsonUtility.ToJson(value,true);
        public TeaProgress Decode(string json) => JsonUtility.FromJson<TeaProgress>(json);
    }
    // The codec is injectable for real filesystem tests without launching Unity.
    public sealed class TeaSaveStore
    {
        readonly string path;
        readonly ITeaProgressCodec codec;
        bool preserveBeforeWrite,recoveredBackup;
        public string LastFailureDetail {get;private set;}
        public TeaSaveStore(string path,ITeaProgressCodec codec=null)
        { this.path=Path.GetFullPath(path); this.codec=codec??new UnityTeaProgressCodec(); }
        public bool HasValidSave => TryRead(path,out _) || TryRead(path+".bak",out _);
        public bool Write(TeaProgress progress,out string error)
        {
            string operation="create directory";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if(preserveBeforeWrite)
                {
                    string suffix=".unreadable-"+Guid.NewGuid().ToString("N");
                    operation="preserve primary";if(File.Exists(path)) File.Copy(path,path+suffix);
                    operation="preserve backup";if(File.Exists(path+".bak") && !recoveredBackup) File.Copy(path+".bak",path+".bak"+suffix);
                    preserveBeforeWrite=false;
                }
                string temp=path+".tmp";
                operation="write temp";File.WriteAllText(temp,codec.Encode(progress));
                operation="replace primary";if(File.Exists(path)) File.Replace(temp,path,recoveredBackup?null:path+".bak");
                else File.Move(temp,path);
                recoveredBackup=false; LastFailureDetail=null; error=null; return true;
            }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {LastFailureDetail=operation+" / "+e.GetType().Name+": "+e.Message;error="Progress could not be saved. Please try again.";return false;}
        }
        public TeaProgress Read(out string error)
        {
            error=null; preserveBeforeWrite=false; recoveredBackup=false;
            if(TryRead(path,out var value)) return value;
            if(TryRead(path+".bak",out value))
            {recoveredBackup=true;preserveBeforeWrite=File.Exists(path);error="Recovered your previous saved visit.";return value;}
            if(!File.Exists(path) && !File.Exists(path+".bak")) return new TeaProgress();
            preserveBeforeWrite=true;
            error="The visit could not be read. Its files will be preserved before saving a fresh visit.";
            return new TeaProgress();
        }
        public static bool SupportedPosition(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z) && v.y>=-1 && v.y<=4 &&
            ((v.x>=-30 && v.x<=13 && v.z>=-13 && v.z<=34) || CheckpostInvestigation.IsCheckpostPosition(v));
        bool TryRead(string file,out TeaProgress p)
        {
            p=null;
            try
            {
                if(!File.Exists(file)) return false;
                string json=File.ReadAllText(file);
                if(!json.TrimStart().StartsWith("{") || !json.Contains("\"version\"")) return false;
                p=codec.Decode(json); if(p==null || p.version!=1) return false;
                if(!SupportedPosition(p.position)) p.position=new Vector3(2.1f,.2f,-5.7f);
                if(p.endingChoice!=StoryEnding.None && p.endingChoice!=StoryEnding.Stay && p.endingChoice!=StoryEnding.Depart) {p.endingChoice=StoryEnding.None;p.storyCompleted=false;}
                p.masterVolume=json.Contains("\"masterVolume\"")?PresentationPolicy.Volume(p.masterVolume):1;
                p.quality=PresentationPolicy.ValidQuality(p.quality);
                if(!float.IsFinite(p.heading)) p.heading=-18;
                // Avoid native engine calls during file recovery and clamp old/new preferences consistently.
                p.textScale=Clamp(p.textScale,.85f,1.3f,1); p.sensitivity=Clamp(p.sensitivity,.05f,.3f,.12f);
                if(p.drink!="tea" && p.drink!="water") p.drink=p.drinkOrdered?"tea":"";
                return true;
            }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is FormatException)
            {return false;}
        }
        static float Clamp(float value,float min,float max,float fallback) => !float.IsFinite(value)?fallback:Math.Min(max,Math.Max(min,value));
    }
}
