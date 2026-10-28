#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Paridhwani
{
    // Editor-only, opt-in presentation regression. Never opens the player's main save.
    public sealed class PresentationDemoPlaytest : MonoBehaviour
    {
        const string Menu = "Paridhwani/Run Presentation Demo Playtest";
        const string PendingKey = "Paridhwani.PresentationDemoPlaytest";
        [Serializable]
        sealed class Report
        {
            public string state = "running", startedUtc, completedUtc, unityVersion, sourceSha256, failure;
            public int checks, width, height;
            public List<string> captures = new List<string>();
        }
        Report report;
        TeaStallSession session;
        string output;
        float deadline;
        bool finished, originalBackground;
        int originalWidth, originalHeight;
        FullScreenMode originalMode;

        [MenuItem(Menu, true)]
        static bool Available() => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer;

        [MenuItem(Menu)]
        public static void RunFromMenu()
        {
            if (!Available()) { Debug.LogWarning("Wait for the current playtest, build or compilation to finish."); return; }
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(TeaStallSession.AutomatedTestKey, false);
            try
            {
                if (!EditorApplication.ExecuteMenuItem("Paridhwani/Play Isolated Test Visit") || !EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("The isolated test visit did not start. Open the current benchmark scene and run its checks first.");
            }
            catch { SessionState.SetBool(PendingKey, false); throw; }
        }

        [InitializeOnLoadMethod]
        static void Register()
        {
            EditorApplication.playModeStateChanged -= PlayState;
            EditorApplication.playModeStateChanged += PlayState;
        }
        static void PlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(PendingKey, false);
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            new GameObject("Presentation demo verification").AddComponent<PresentationDemoPlaytest>().Begin();
        }

        void Begin()
        {
            originalBackground = Application.runInBackground;
            originalWidth = Screen.width; originalHeight = Screen.height; originalMode = Screen.fullScreenMode;
            Application.runInBackground = true;
            deadline = Time.realtimeSinceStartup + 180;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "verification", "Temp", "PresentationPlaytests",
                "run-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8)));
            Directory.CreateDirectory(output);
            report = new Report { startedUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion };
            try { report.sourceSha256 = SourceDigest(); WriteReport(); StartCoroutine(Guarded()); }
            catch (Exception error) { Complete(error.ToString()); }
        }
        // Includes content and paths, so changed sources cannot be mistaken for a tested revision.
        static string SourceDigest()
        {
            string root = Path.Combine(Application.dataPath, "_Paridhwani");
            var paths = new List<string>();
            foreach (var path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".uss", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) paths.Add(path);
            paths.Sort(StringComparer.Ordinal);
            using (var sha = SHA256.Create())
            {
                foreach (var path in paths)
                {
                    var bytes = Encoding.UTF8.GetBytes(path.Substring(root.Length).Replace('\\', '/') + "\n" + File.ReadAllText(path).Replace("\r\n", "\n") + "\n");
                    sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
            }
        }
        void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("Presentation demo playtest: " + message);
            report.checks++;
        }
        void WriteReport() => File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
        void Complete(string failure)
        {
            if (finished) return;
            finished = true;
            if (report != null)
            {
                report.failure = failure ?? ""; report.state = failure == null ? "passed" : "failed";
                report.completedUtc = DateTime.UtcNow.ToString("O"); WriteReport();
                if (failure == null) Debug.Log("PARIDHWANI_PRESENTATION_PLAYTEST_OK " + output);
                else Debug.LogError("PARIDHWANI_PRESENTATION_PLAYTEST_FAILED " + output + "\n" + failure);
            }
            EditorApplication.isPlaying = false;
        }
        void Update() { if (!finished && report != null && Time.realtimeSinceStartup > deadline) Complete("Presentation checks exceeded 180 seconds."); }
        void OnDestroy()
        {
            Application.runInBackground = originalBackground;
            Screen.SetResolution(originalWidth, originalHeight, originalMode);
            if (!finished && report != null) Complete("Play mode stopped or scripts reloaded before presentation verification completed.");
        }
        IEnumerator Guarded()
        {
            var stack = new Stack<IEnumerator>(); stack.Push(Checks());
            while (stack.Count > 0)
            {
                bool next; object current = null;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception error) { Complete(error.ToString()); yield break; }
                if (!next) { stack.Pop(); continue; }
                if (current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
            Complete(null);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(output, name + ".png"); ScreenCapture.CaptureScreenshot(path);
            float until = Time.realtimeSinceStartup + 5;
            do { yield return null; } while ((!File.Exists(path) || new FileInfo(path).Length < 5000) && Time.realtimeSinceStartup < until);
            Check(File.Exists(path) && new FileInfo(path).Length >= 5000, "missing rendered capture: " + name);
            report.captures.Add(name + ".png"); WriteReport();
        }
        VisualElement Root => session.ui.GetComponent<UIDocument>().rootVisualElement;
        void Visible(VisualElement element, string name)
        {
            Check(element != null && float.IsFinite(element.worldBound.width) && element.worldBound.width > 0 && element.worldBound.height > 0 &&
                element.worldBound.xMin >= Root.worldBound.xMin - 2 && element.worldBound.xMax <= Root.worldBound.xMax + 2 &&
                element.worldBound.yMin >= Root.worldBound.yMin - 2 && element.worldBound.yMax <= Root.worldBound.yMax + 2, name + " stays inside the viewport");
        }
        IEnumerator WalkTo(Vector3 target)
        {
            session.player.blocked = true;
            var controller = session.player.GetComponent<CharacterController>();
            float until = Time.realtimeSinceStartup + 15;
            while (true)
            {
                Vector3 delta = target - session.player.transform.position; delta.y = 0;
                if (delta.magnitude < .16f) break;
                if (Time.realtimeSinceStartup >= until) throw new InvalidOperationException("Presentation walking route blocked at " + session.player.transform.position + " toward " + target);
                session.player.visual.rotation = Quaternion.LookRotation(delta);
                controller.Move(delta.normalized * Mathf.Min(delta.magnitude, 3.6f * Time.unscaledDeltaTime));
                yield return null;
            }
        }
        IEnumerator CharacterSampleViews()
        {
            var tea=Array.Find(session.villagers,n=>n && n.role==VillageNpcRole.TeaVendor);
            if(!session.player.visual.Find("Head/CartoonPlayer_Face"))yield break;
            Check(!ShaderUtil.ShaderHasError(Shader.Find("Paridhwani/CharacterInk")),"character sample shader compiles");
            foreach(var actor in new[]{session.player.gameObject,tea.gameObject})
            {
                var rig=actor.GetComponentInChildren<VillageCharacter>();
                Check(rig && rig.head && rig.leftArm && rig.rightArm && rig.leftLeg && rig.rightLeg,"sample preserves animation pivots");
                foreach(var filter in actor.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=filter.sharedMesh;if(!mesh || !mesh.name.StartsWith("Cartoon"))continue;
                    var points=mesh.vertices;var indices=mesh.triangles;
                    Check(points.Length>3 && indices.Length>2,"sample mesh is populated: "+mesh.name);
                    foreach(var point in points)Check(float.IsFinite(point.x)&&float.IsFinite(point.y)&&float.IsFinite(point.z),"finite sample vertex");
                    for(int i=0;i<indices.Length;i+=3)Check(Vector3.Cross(points[indices[i+1]]-points[indices[i]],points[indices[i+2]]-points[indices[i]]).sqrMagnitude>1e-14f,"nondegenerate sample triangle: "+mesh.name);
                }
            }
            // Additional portraits use the real game camera in Play mode. Normal
            // gameplay/conversation captures above remain the scale/readability reference.
            session.Close();yield return new WaitForSecondsRealtime(.8f);
            var camera=session.player.view;Vector3 savedPosition=session.player.transform.position;float savedHeading=session.player.Heading;
            Quaternion savedVisual=session.player.visual.rotation;
            session.player.enabled=false;session.player.visual.rotation=Quaternion.identity;
            var root=Root;var display=root.style.display;
            session.ui.ShowInspection("Character sample","","","");root.style.display=DisplayStyle.None;
            camera.transform.position=savedPosition+new Vector3(0,1.50f,1.65f);camera.transform.LookAt(savedPosition+Vector3.up*1.35f);
            yield return new WaitForSecondsRealtime(.3f);yield return Capture("Character-player-front");
            camera.transform.position=tea.transform.position+new Vector3(.15f,1.55f,-1.7f);camera.transform.LookAt(tea.transform.position+Vector3.up*1.5f);
            yield return new WaitForSecondsRealtime(.3f);yield return Capture("Character-Manjunatha-front");
            root.style.display=display;session.ui.HideModal();session.player.enabled=true;
            session.player.Restore(savedPosition,savedHeading);session.player.visual.rotation=savedVisual;
            yield return new WaitForSecondsRealtime(.7f);
        }
        IEnumerator Rehearse(ReplayChapter chapter)
        {
            // Walk through CharacterController collisions; invoke authored actions programmatically.
            // This is a scripted route regression, not a keyboard/gamepad usability test.
            switch (chapter)
            {
                case ReplayChapter.Arrival:
                    yield return WalkTo(new Vector3(0, 0, -4.8f));
                    yield return WalkTo(new Vector3(-4.2f, 0, -4.8f));
                    yield return WalkTo(new Vector3(-4.2f, 0, -1.65f));
                    session.player.SetLookAngles(0, 17); yield return new WaitForSecondsRealtime(.4f);
                    if(session.Nearby!="Talk to Manjunatha")
                    {
                        var tea=Array.Find(session.villagers,n=>n && n.role==VillageNpcRole.TeaVendor);
                        Physics.Linecast(session.player.EyePosition,tea.eyes.position,out var hit,~(1<<2),QueryTriggerInteraction.Ignore);
                        yield return Capture("Arrival-approach-failed");
                        Check(false,"demo tea approach: nearby="+session.Nearby+", player="+session.player.transform.position+", vendor="+tea.transform.position+", eyes="+tea.eyes.position+", sightHit="+(hit.collider?hit.collider.name:"none")+", forward="+session.player.view.transform.forward+", transition="+session.player.IsTransitioning+", modal="+session.ui.Mode);
                    }
                    Check(session.Nearby == "Talk to Manjunatha", "demo tea route reaches the vendor");
                    session.Interact(); yield return new WaitForSecondsRealtime(.9f);
                    session.Choose(0); session.Choose(0); session.Choose(0);
                    Check(session.Progress.greeted && session.Progress.drinkOrdered && session.Progress.directionsKnown, "tea demo earns drink and library directions");
                    yield return Capture("Arrival-conversation");
                    session.OpenNotebook(); yield return Capture("Arrival-notebook"); session.Close();
                    yield return CharacterSampleViews();
                    break;
                case ReplayChapter.River:
                    foreach (var point in new[] { new Vector3(-10, 0, 6.5f), new Vector3(-14, 0, 6.5f), new Vector3(-19, 0, 6.5f), new Vector3(-21, 0, 6.5f), new Vector3(-23.5f, 0, 2.2f) })
                        yield return WalkTo(point);
                    session.player.SetLookAngles(0, 17); yield return new WaitForSecondsRealtime(.4f);
                    Check(session.Nearby == "Talk to Giri", "demo river path reaches Giri");
                    session.Interact(); yield return new WaitForSecondsRealtime(.9f);
                    session.Choose(0); session.Choose(0); yield return new WaitForSecondsRealtime(.9f);
                    Check(session.player.IsInspecting, "demo inspection frames the bicycle");
                    session.SelectBicyclePart(BicyclePart.Frame); session.SelectBicyclePart(BicyclePart.Stand); session.SelectBicyclePart(BicyclePart.CarrierPocket);
                    session.SubmitInvestigationAnswer(0); Check(!session.Progress.bookingSlipFound, "demo rejects wrong pocket answer");
                    session.SubmitInvestigationAnswer(1);
                    session.OpenActivity(VillageActivity.BookingSlip); session.SubmitInvestigationAnswer(0);
                    Check(session.Progress.bookingSlipRead, "demo booking interpreted");
                    yield return Capture("River-booking");
                    session.Close(); session.Close(); yield return new WaitForSecondsRealtime(.9f);
                    Check(session.Turn != null && session.Turn.Id == "deduce", "demo evidence returns to Giri");
                    session.Choose(0); session.SubmitInvestigationAnswer(0); Check(!session.Progress.riverDeductionKnown, "demo rejects unwitnessed accident inference");
                    session.SubmitInvestigationAnswer(2);
                    Check(session.Progress.riverDeductionKnown && !session.Progress.templeLeadKnown, "demo earns qualified inference before the next lead");
                    yield return Capture("River-deduction");
                    break;
                case ReplayChapter.Deed:
                    session.player.blocked = true; yield return new WaitForSecondsRealtime(.4f);
                    Check(session.Nearby == "Inspect the family trunk", "deed demo starts at the physical trunk");
                    session.Interact(); yield return new WaitForSecondsRealtime(.9f);
                    session.TrunkAttempt = "1200"; session.SubmitTrunkCode(); Check(!session.Progress.trunkOpened, "demo wrong code leaves trunk locked");
                    session.TrunkAttempt = "1207"; session.SubmitTrunkCode();
                    Check(session.Progress.trunkOpened && !session.Progress.deedRetrieved, "demo unlock still requires deed review");
                    session.OpenActivity(VillageActivity.DeedReview);
                    session.SubmitInvestigationAnswer(0); session.SubmitInvestigationAnswer(2); session.SubmitInvestigationAnswer(1); session.ProtectDeed();
                    Check(session.Progress.deedRetrieved && !session.deedPaper.activeSelf, "demo reviewed original protected");
                    yield return Capture("Deed-protected");
                    break;
                case ReplayChapter.Council:
                    session.player.blocked = true; yield return new WaitForSecondsRealtime(.4f);
                    Check(session.Nearby == "Talk to Council president", "council demo starts at the president");
                    session.Interact(); yield return new WaitForSecondsRealtime(.9f); session.Choose(0);
                    session.SubmitInvestigationAnswer(1); Check(!session.Progress.deedPresented, "demo booking cannot replace the original");
                    session.SubmitInvestigationAnswer(0); session.SubmitInvestigationAnswer(1); session.SubmitInvestigationAnswer(0); session.SubmitInvestigationAnswer(2);
                    session.RequestCouncilDecision();
                    Check(session.Progress.workSuspended && !session.Progress.libraryProtected && session.worksSuspendedMarker.activeSelf, "demo council suspends work pending later review");
                    yield return Capture("Council-decision");
                    break;
            }
            session.Save(); Check(session.LastSaveSucceeded, "rehearsed demo saves separately");
            session.OpenTitle(); session.ContinueMainVisit();
        }
        IEnumerator Checks()
        {
            yield return null;
            session = FindAnyObjectByType<TeaStallSession>();
            Check(session && session.IsIsolatedTestVisit && !session.IsAutomatedTest && session.Progress != null,
                "requires an initialized isolated visit with no full-story smoke running");
            Check(session.ui.Mode == "main-menu", "current benchmark opens at the title");
            yield return new WaitForSecondsRealtime(.5f);
            yield return VillagePresentationSmoke.CheckSigns();
            session.Progress.brightness = .9f; session.Progress.signRead = true; session.Progress.textScale = 1.3f;
            session.SettingsChanged(); Check(session.LastSaveSucceeded, "isolated main snapshot saves");
            Vector3 mainPosition = session.Progress.position; float mainHeading = session.Progress.heading;
            // The Editor controls Game-view dimensions; record actual dimensions rather than claiming a requested resolution.
            report.width = Screen.width; report.height = Screen.height;
            yield return Capture("00-title");
            foreach (var chapter in PresentationDemo.Chapters)
            {
                session.ui.ShowPresentationDemo(); session.StartPresentationDemo(chapter);
                yield return new WaitForSecondsRealtime(.3f);
                Check(session.IsPresentationDemo && session.PresentationChapter == chapter && session.LastSaveSucceeded, "prepared separate demo: " + chapter);
                Check(!session.Progress.storyCompleted && !session.Progress.libraryProtected, "demo entry does not grant an ending");
                Check(Root.Query<Label>().ToList().Exists(label => label.text == PresentationDemo.Route(chapter)), "literal scene instructions: " + chapter);
                Visible(Root.Q<Button>("demo-enter"), "demo Enter");
                Visible(Root.Q<ScrollView>("demo-arrival-scroll"), "demo instructions scroll area");
                Check((Root.focusController.focusedElement as VisualElement)?.name == "demo-enter", "demo Enter initially focused");
                yield return Capture(chapter + "-guide");
                session.Close(); yield return new WaitForSecondsRealtime(.3f);
                Check(!session.ui.IsModal && !session.player.blocked, "demo entry releases movement");
                if (chapter == ReplayChapter.Arrival)
                {
                    var vendor = Array.Find(session.villagers, npc => npc && npc.role == VillageNpcRole.TeaVendor);
                    Check(vendor && session.Nearby != "Read the bus timetable", "Arrival demo avoids the unrelated timetable prompt");
                    Vector3 face = session.player.view.WorldToViewportPoint(vendor.eyes.position);
                    Check(face.z > 0 && face.x > .15f && face.x < .8f && face.y > .25f && face.y < .85f, "Arrival demo frames the tea vendor in view");
                }
                yield return Capture(chapter + "-world");
                yield return Rehearse(chapter);
                session.ui.ShowPause();
                Check(Root.Q<Button>("settings-demo-guide") != null && Root.Q<Button>("settings-demo-scenes") != null, "Pause offers guide and scene switching");
                session.ui.ShowPresentationArrival(); yield return new WaitForSecondsRealtime(.2f);
                Check(session.ui.Mode == "practice-arrival", "guide reopens from Pause");
                session.Close(); session.ui.ShowPause(); session.ui.ShowPresentationDemo(); session.Close();
                Check(session.ui.Mode == "pause", "closing selector returns to Pause");
                session.ReturnToCompletedStory(); yield return null;
                Check(!session.IsPracticeVisit && !session.IsPresentationDemo && session.Progress.signRead && !session.Progress.greeted &&
                    !session.Progress.riverReportKnown && !session.Progress.deedRetrieved && !session.Progress.fridayFinaleStarted,
                    "main evidence and chapter gates restored: " + chapter);
                Check(Vector3.Distance(session.Progress.position, mainPosition) < .1f && Mathf.Abs(Mathf.DeltaAngle(session.Progress.heading, mainHeading)) < .1f,
                    "main saved position and heading restored");
                Check(session.Progress.textScale == 1.3f && session.Progress.brightness == .9f, "main presentation preferences restored");
                session.OpenTitle();
            }
            Check(SourceDigest() == report.sourceSha256, "source and scene assets remained unchanged throughout the run");
        }
    }
}
#endif
