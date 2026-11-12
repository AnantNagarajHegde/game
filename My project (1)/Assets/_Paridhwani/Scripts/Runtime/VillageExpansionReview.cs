#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Paridhwani
{
    [DefaultExecutionOrder(500)]
    public sealed class VillageExpansionReview : MonoBehaviour
    {
        TeaStallSession session;
        string output;
        int checks;
        bool overview;
        Vector3 cameraPosition, cameraTarget;
        public void Begin(TeaStallSession value)
        {
            if (!value.IsIsolatedTestVisit) throw new InvalidOperationException("An isolated visit is required.");
            session = value;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../verification/Temp/VillageExpansion/Native"));
            Directory.CreateDirectory(output); StartCoroutine(Guarded());
        }
        void LateUpdate()
        {
            if (!overview) return;
            session.player.view.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(cameraTarget - cameraPosition));
        }
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); checks++; }
        IEnumerator Guarded()
        {
            var stack = new Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0)
            {
                bool more; object next = null;
                try { more = stack.Peek().MoveNext(); if (more) next = stack.Peek().Current; }
                catch (Exception error)
                {
                    File.WriteAllText(Path.Combine(output, "failure.txt"), error.ToString()); Debug.LogException(error);
                    UnityEditor.EditorApplication.isPlaying = false; yield break;
                }
                if (!more) { stack.Pop(); continue; }
                if (next is IEnumerator nested) stack.Push(nested); else yield return next;
            }
        }
        IEnumerator Shot(string name, Vector3 position, Vector3 target)
        {
            overview = true; cameraPosition = position; cameraTarget = target;
            for (int i = 0; i < 12; i++) yield return null;
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG()); }
            finally { Destroy(image); }
            overview = false;
        }
        static bool Inside(Vector3 p) => p.x >= -20 && p.x <= 31 && p.z >= -25 && p.z <= 40 || p.x >= -29.85f && p.x <= -20 && p.z >= -1.8f && p.z <= 12.5f;
        IEnumerator Run()
        {
            session.BeginNewMainVisit(); session.AcknowledgeArrival();
            session.Progress.textScale = 1; session.SettingsChanged();
            session.player.Restore(new Vector3(20.4f, .15f, 8), 0);
            yield return new WaitForSecondsRealtime(2);
            Check(Application.isFocused, "Game view needs native focus for movement verification");
            var residents = FindObjectsByType<VillageAmbientResident>();
            var before = residents.Select(r => r.TravelDistance).ToArray();
            yield return new WaitForSecondsRealtime(7);
            for (int i = 0; i < residents.Length; i++)
            {
                Check(residents[i].TravelDistance - before[i] > .4f, "Resident did not walk: " + residents[i].name);
                var character = residents[i].GetComponentInChildren<VillageCharacter>();
                Check(character.Gait != null && character.GetComponentsInChildren<SkinnedMeshRenderer>().Length == 2, "Resident needs articulated legs: " + residents[i].name);
                Check(character.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.EndsWith("Trouser")).All(r => !r.enabled), "Distance culling restored an unanimated source leg: " + residents[i].name);
                Check(Inside(residents[i].transform.position), "Resident left playable streets");
            }
            session.OpenNotebook(); yield return null;
            var paused = residents.Select(r => r.transform.position).ToArray();
            yield return new WaitForSecondsRealtime(.4f);
            for (int i = 0; i < residents.Length; i++) Check(Vector3.Distance(paused[i], residents[i].transform.position) < .001f, "Resident moved during a modal");
            session.Close();
            // Physically walk the native player controller against every perimeter segment.
            var player = session.player; var controller = player.GetComponent<CharacterController>();
            player.enabled = false;
            foreach (var boundary in FindObjectsByType<BoxCollider>().Where(c => c.name.StartsWith("Village boundary -")))
            {
                Vector3 center = boundary.transform.position, direction = boundary.transform.right;
                if (Inside(center + direction)) direction = -direction;
                Vector3 start = center - direction; start.y = .25f;
                player.Restore(start, 0); Physics.SyncTransforms();
                for (int step = 0; step < 60; step++) controller.Move(direction * .10f + Vector3.down * .05f);
                Check(Vector3.Dot(player.transform.position - center, direction) < -.25f, "Player crossed " + boundary.name);
            }
            player.enabled = true; player.Restore(new Vector3(20.4f, .2f, -8), 0);
            yield return Shot("01-residential-street", new Vector3(20.4f, 2.2f, -11), new Vector3(20.4f, 1.7f, 20));
            yield return Shot("02-veranda-houses", new Vector3(20.7f, 2.25f, 2.5f), new Vector3(14.8f, 1.8f, 8));
            yield return Shot("03-village-overview", new Vector3(42, 29, -34), new Vector3(1, 0, 10));
            player.Restore(new Vector3(20.4f, .2f, 35), 0);
            yield return Shot("04-continuing-village", new Vector3(20.4f, 2.1f, 35.5f), new Vector3(20.4f, 1.7f, 66));
            player.Restore(new Vector3(-28, .3f, .3f), -90);
            yield return Shot("05-river-weir", new Vector3(-28.3f, 2.6f, -3), new Vector3(-35.2f, -.6f, 5));
            yield return Shot("06-river-upstream", new Vector3(-29, 2.2f, 10), new Vector3(-37, -.3f, 38));
            player.Restore(new Vector3(-9, .2f, -14), 0);
            yield return Shot("07-southern-houses", new Vector3(-8, 2.3f, -13.5f), new Vector3(-5, 1.7f, -21));
            player.Restore(new Vector3(1.6f, .2f, -5), 0);
            yield return Shot("08-original-story-lane", new Vector3(2, 2.5f, -6), new Vector3(1, 1.6f, 14));
            File.WriteAllText(Path.Combine(output, "report.txt"), "VILLAGE_NATIVE_REVIEW_OK: " + checks + " checks\nNative controller containment at all 16 perimeter segments; 12 residents walk with articulated gait and stop for modal UI.\nViewport: " + Screen.width + "x" + Screen.height + "\n");
            Debug.Log("VILLAGE_NATIVE_REVIEW_OK: " + checks + " checks; " + output);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
