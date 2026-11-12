using System;
using UnityEngine;

namespace Paridhwani
{
    // Chapter entry facts, unlocks and practice preparation share one ordered definition.
    // TeaProgress retains its serialized fields so existing visits remain compatible.
    public static class VillageStory
    {
        sealed class Chapter
        {
            public readonly string Name;
            public readonly Func<TeaProgress,bool> Reached;
            public readonly Action<TeaProgress> Prepare;
            public readonly Vector3 Position;
            public readonly float Heading;
            public Chapter(string name, Func<TeaProgress,bool> reached, Action<TeaProgress> prepare, Vector3 position, float heading=0)
            { Name=name; Reached=reached; Prepare=prepare; Position=position; Heading=heading; }
        }

        static readonly Chapter[] chapters={
            new Chapter("Arrival and tea",p=>true,p=>{},new Vector3(2.1f,.2f,-5.7f)),
            new Chapter("Library introduction",p=>p.directionsKnown,p=> {
                p.greeted=p.drinkOrdered=p.directionsKnown=p.signRead=true; p.drink="tea";
            },new Vector3(0,.2f,9)),
            new Chapter("Shanthakka's mess",p=>p.messLeadKnown,p=>p.metSomanna=p.messLeadKnown=true,new Vector3(3.7f,.2f,2)),
            new Chapter("Market and timetable",p=>p.marketLeadKnown,p=>p.mealOrdered=p.servingComplete=p.shoppingNoteCollected=p.marketLeadKnown=true,new Vector3(4,.2f,8.9f)),
            new Chapter("River investigation",p=>p.riverReportKnown,p=>p.marketReceiptCollected=p.purchaseTimeKnown=p.marketAccountKnown=p.busDepartureKnown=p.riverReportKnown=true,new Vector3(1.4f,.2f,6.5f),-90),
            new Chapter("Temple witnesses",p=>p.templeLeadKnown,p=>p.giriAccountKnown=p.bicycleFrameObserved=p.bicycleStandObserved=p.bookingSlipFound=p.bookingSlipRead=p.riverDeductionKnown=p.templeLeadKnown=true,new Vector3(1,.2f,23.4f)),
            new Chapter("Library archive",p=>p.guddaFamilyLeadKnown,p=>p.gurumurthyAccountKnown=p.rathnammaAccountKnown=p.witnessAccountsCompared=p.guddaFamilyLeadKnown=true,new Vector3(-4.6f,.2f,9)),
            new Chapter("Panchayat notice",p=>p.thursdayStarted,p=>p.libraryAccessGranted=p.archiveShelfLocated=p.deedReferenceFound=p.thursdayStarted=true,new Vector3(1,.2f,19.8f)),
            new Chapter("Journey to the checkpost",p=>p.checkpostDestinationKnown,p=>p.noticeIdentified=p.deadlineKnown=p.deedRequired=p.checkpostDestinationKnown=true,new Vector3(6.5f,.2f,-5.8f)),
            new Chapter("Marked ridge",p=>p.trailInstructionsUnderstood,p=>p.checkpostVisited=p.checkpostAccountKnown=p.ananyaLocationReported=p.ridgeRouteOpen=p.trailInstructionsUnderstood=p.trailPermission=true,new Vector3(48,.2f,10)),
            new Chapter("Gudda Mane reunion",p=>p.guddaManeReached,p=>p.ridgeWayfindingKnown=p.ridgeCrossed=p.guddaManeReached=true,new Vector3(48,.2f,38.5f)),
            new Chapter("Trunk and original deed",p=>p.trunkLocationKnown,p=>p.ananyaFound=p.disappearanceExplained=p.farmHospitalityKnown=p.trunkLocationKnown=true,new Vector3(50.4f,.2f,45.6f)),
            new Chapter("Return with Ananya",p=>p.deedRetrieved,p=>p.trunkOpened=p.deedPurposeKnown=p.deedRestrictionKnown=p.deedBeneficiaryKnown=p.deedConceptsReviewed=p.deedRetrieved=true,new Vector3(46,.2f,38.7f)),
            new Chapter("Library court council",p=>p.fridayFinaleStarted,p=>p.returnJourneyStarted=p.returnedWithAnanya=p.fridayFinaleStarted=true,new Vector3(-2.2f,.2f,7.3f)),
            new Chapter("Later review and epilogue",p=>p.workSuspended,p=>p.deedPresented=p.finalePurposePresented=p.finaleRestrictionPresented=p.finaleBeneficiaryPresented=p.workSuspended=true,new Vector3(-2.2f,.2f,7.3f))
        };

        public static string ChapterName(ReplayChapter chapter)=>Valid(chapter)?chapters[(int)chapter].Name:"Unknown chapter";
        public static bool Reached(TeaProgress progress, ReplayChapter chapter)=>progress!=null && Valid(chapter) && chapters[(int)chapter].Reached(progress);
        static bool Valid(ReplayChapter chapter)=>(int)chapter>=0 && (int)chapter<chapters.Length;
        public static TeaProgress PrepareChapter(ReplayChapter chapter, TeaProgress preferences)
        {
            if(!Valid(chapter))throw new ArgumentOutOfRangeException(nameof(chapter));
            var progress=new TeaProgress { arrivalAcknowledged=true };
            PresentationPolicy.Copy(preferences,progress);
            for(int i=0;i<=(int)chapter;i++)chapters[i].Prepare(progress);
            progress.position=chapters[(int)chapter].Position;
            progress.heading=chapters[(int)chapter].Heading;
            return progress;
        }

        sealed class ObjectiveRule
        {
            public readonly Func<TeaProgress,bool> When;
            public readonly string Text;
            public ObjectiveRule(Func<TeaProgress,bool> when, string text) { When=when; Text=text; }
        }

        // Later story facts take precedence. Intermediate evidence is still checked explicitly.
        static readonly ObjectiveRule[] objectives={
            new ObjectiveRule(p=>p.storyCompleted,"Story complete - revisit the village or practise again"),
            new ObjectiveRule(p=>p.epilogueStarted,"Meet Somanna or Ananya and choose what comes next"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Epilogue),"Works suspended - await the council review outcome"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Council),"Present the protected original at the library court"),
            new ObjectiveRule(p=>p.returnedWithAnanya,"Confirm Friday morning preparations with Ananya"),
            new ObjectiveRule(p=>p.returnJourneyStarted,"Return with Ananya via the checkpost and auto"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Return),"Prepare the return journey with Ananya"),
            new ObjectiveRule(p=>p.deedConceptsReviewed,"Protect the original deed in its folder"),
            new ObjectiveRule(p=>p.trunkOpened,"Read and review the three deed clauses"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Deed),"Open the family trunk in the farm archive"),
            new ObjectiveRule(p=>p.disappearanceExplained,"Ask Kamalamma about the original deed"),
            new ObjectiveRule(p=>p.ananyaFound,"Hear why Ananya could not return"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Farm),"Meet Ananya on the farmhouse veranda"),
            new ObjectiveRule(p=>p.ridgeWayfindingKnown,"Follow the fenced path to Gudda Mane"),
            new ObjectiveRule(p=>p.trailPermission,"Checked ridge approach available - review the trailhead"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Ridge),"Wait for Kencha to confirm the ridge is checked"),
            new ObjectiveRule(p=>p.checkpostAccountKnown,"Review Kencha\u0027s safe ridge directions"),
            new ObjectiveRule(p=>p.checkpostVisited,"Ask Kencha about Ananya and the road closure"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Checkpost),"Ask Raghavendra for a ride to the forest checkpost"),
            new ObjectiveRule(p=>p.deadlineKnown,"Compare the archive reference with the notice checklist"),
            new ObjectiveRule(p=>p.noticeIdentified,"Read the clearance day and time"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Panchayat),"Read the public panchayat notice"),
            new ObjectiveRule(p=>p.deedReferenceFound,"Return to Somanna when ready to rest"),
            new ObjectiveRule(p=>p.libraryAccessGranted,"Read the catalogue inside the library"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Archive),"Bring the Gudda Mane family name to Somanna"),
            new ObjectiveRule(p=>p.witnessAccountsCompared,"Ask Rathnamma about the Gudda Mane family"),
            new ObjectiveRule(p=>p.gurumurthyAccountKnown && p.rathnammaAccountKnown,"Compare the two witness accounts"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Temple),"Speak to Gurumurthy and Rathnamma at the northern stop"),
            new ObjectiveRule(p=>p.riverDeductionKnown,"Ask Giri about witnesses near the northern stop"),
            new ObjectiveRule(p=>RiverInvestigation.CanDeduce(p),"Compare the bicycle and booking evidence with Giri"),
            new ObjectiveRule(p=>p.bookingSlipFound && !p.bookingSlipRead,"Read the advance booking slip"),
            new ObjectiveRule(p=>p.giriAccountKnown,"Inspect the bicycle’s frame, stand and carrier pocket"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.River),"Follow the river path and speak to Giri"),
            new ObjectiveRule(p=>p.busDepartureKnown,"Read Giri’s notice beside the timetable"),
            new ObjectiveRule(p=>p.marketAccountKnown,"Check Route 14 at the bus-stand timetable"),
            new ObjectiveRule(p=>p.purchaseTimeKnown,"Ask Basavaraju about Ananya’s journey"),
            new ObjectiveRule(p=>p.marketReceiptCollected,"Read the purchase day and time on the ledger"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Market),"Show Ananya’s shopping note to Basavaraju"),
            new ObjectiveRule(p=>p.shoppingNoteCollected,"Ask Shanthakka about the shopping note"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Mess),"Visit Shanthakka at the mess across the lane"),
            new ObjectiveRule(p=>p.metSomanna,"Ask Somanna about Ananya"),
            new ObjectiveRule(p=>Reached(p,ReplayChapter.Library),"Follow the lane towards the library"),
            new ObjectiveRule(p=>p.drinkOrdered,"Ask Manjunatha where the library is"),
            new ObjectiveRule(p=>p.greeted,"Order tea or water at the tea stall"),
        };
        public static string CurrentObjective(TeaProgress progress)
        {
            if(progress!=null)foreach(var objective in objectives)if(objective.When(progress))return objective.Text;
            return "Introduce yourself at the tea stall";
        }
    }
}
