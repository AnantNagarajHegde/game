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
        public string drink = "";
        public bool english = true, roman = true, muted;
        public float textScale = 1f, sensitivity = 0.12f;
        public Vector3 position = new Vector3(2.1f, 0.2f, -5.7f);
        public float heading = -18f;
        public string Objective => checkpostDestinationKnown ? "Panchayat evidence complete - forest checkpost journey comes next" : deadlineKnown ? "Compare the archive reference with the notice checklist" : noticeIdentified ? "Read the clearance day and time" : thursdayStarted ? "Read the public panchayat notice" : deedReferenceFound ? "Return to Somanna when ready to rest" : libraryAccessGranted ? "Read the catalogue inside the library" : guddaFamilyLeadKnown ? "Bring the Gudda Mane family name to Somanna" : witnessAccountsCompared ? "Ask Rathnamma about the Gudda Mane family" : gurumurthyAccountKnown && rathnammaAccountKnown ? "Compare the two witness accounts" : templeLeadKnown ? "Speak to Gurumurthy and Rathnamma at the northern stop" :
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

    // File storage is injectable so smoke checks never touch the player's real save.
    public sealed class TeaSaveStore
    {
        readonly string path;
        public TeaSaveStore(string path) { this.path = path; }
        public bool HasValidSave => TryRead(path,out _) || TryRead(path+".bak",out _);
        public bool Write(TeaProgress progress, out string error)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(progress, true));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                error = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { error = "Progress could not be saved. Please try again."; Debug.LogWarning(e.Message); return false; }
        }
        public TeaProgress Read(out string error)
        {
            error = null;
            if (!File.Exists(path)) return new TeaProgress();
            if (TryRead(path, out TeaProgress data)) return data;
            if (TryRead(path + ".bak", out data))
            { error = "Recovered your previous saved visit."; return data; }
            error = "The saved visit could not be read. Starting at the bus stand.";
            return new TeaProgress();
        }
        static bool TryRead(string file, out TeaProgress p)
        {
            p = null;
            try
            {
                if (!File.Exists(file)) return false;
                string json = File.ReadAllText(file);
                if (!json.TrimStart().StartsWith("{") || !json.Contains("\"version\"")) return false;
                p = JsonUtility.FromJson<TeaProgress>(json);
                if (p == null || p.version != 1) return false;
                if (!float.IsFinite(p.position.x) || !float.IsFinite(p.position.y) ||
                    !float.IsFinite(p.position.z) || p.position.x < -30 || p.position.x > 13 ||
                    p.position.z < -13 || p.position.z > 34 || p.position.y < -1 || p.position.y > 4)
                    p.position = new Vector3(2.1f, .2f, -5.7f);
                if (!float.IsFinite(p.heading)) p.heading = -18;
                p.textScale = float.IsFinite(p.textScale) ? Mathf.Clamp(p.textScale, .85f, 1.3f) : 1;
                p.sensitivity = float.IsFinite(p.sensitivity) ? Mathf.Clamp(p.sensitivity, .05f, .3f) : .12f;
                if (p.drink != "tea" && p.drink != "water") p.drink = p.drinkOrdered ? "tea" : "";
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { return false; }
        }
    }
}
