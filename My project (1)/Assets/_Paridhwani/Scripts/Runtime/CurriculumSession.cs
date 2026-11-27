using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Paridhwani
{
    // Reply from POST /v2/curriculum/turn (village_backend/curriculum_engine.py).
    public sealed class CurriculumReply
    {
        public string Outcome = "", ChoiceKey = "", PlayerName = "", Tip = "", Source = "";
        public bool CountsAsWrong;
        public TeaLine Reaction;

        public static async Task<CurriculumReply> Request(string node, string text, string playerName, int wrongCount,
            VillageConversationExchange[] history, CancellationToken token)
        {
            byte[] body;
            using (var stream = new MemoryStream())
            {
                using (var json = new Utf8JsonWriter(stream))
                {
                    json.WriteStartObject();
                    json.WriteNumber("version", 1);
                    json.WriteString("node", node);
                    json.WriteString("text", text);
                    json.WriteString("player_name", playerName ?? "");
                    json.WriteNumber("wrong_count", Math.Min(99, Math.Max(0, wrongCount)));
                    json.WriteStartArray("history");
                    int start = Math.Max(0, history.Length - 2);
                    for (int i = start; i < history.Length; i++)
                    {
                        json.WriteStartObject();
                        json.WriteString("player", history[i].Player ?? "");
                        json.WriteString("npc", history[i].Npc ?? "");
                        json.WriteEndObject();
                    }
                    json.WriteEndArray();
                    json.WriteEndObject();
                }
                body = stream.ToArray();
            }
            var bytes = await VillageConversationClient.CurriculumTurn(body, token);
            return Parse(bytes);
        }

        static string Text(JsonElement e, string name)
        {
            JsonElement v;
            return e.TryGetProperty(name, out v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";
        }

        public static CurriculumReply Parse(byte[] bytes)
        {
            using (var doc = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 }))
            {
                var root = doc.RootElement;
                var reply = new CurriculumReply
                {
                    Outcome = Text(root, "outcome"), ChoiceKey = Text(root, "choice_key"), PlayerName = Text(root, "player_name"),
                    Tip = Text(root, "tip"), Source = Text(root, "source")
                };
                JsonElement value;
                reply.CountsAsWrong = root.TryGetProperty("counts_as_wrong", out value) && value.ValueKind == JsonValueKind.True;
                if (root.TryGetProperty("reaction", out value) && value.ValueKind == JsonValueKind.Object)
                {
                    string kn = Text(value, "kn"), ro = Text(value, "ro"), en = Text(value, "en"), action = Text(value, "action");
                    if (kn.Length == 0 && action.Length > 0) en = "(" + action + ")" + (en.Length > 0 ? " " + en : "");
                    if (kn.Length > 0 || ro.Length > 0 || en.Length > 0)
                    {
                        reply.Reaction = new TeaLine(kn, ro, en);
                        reply.Reaction.Silent = kn.Length == 0;
                    }
                }
                return reply;
            }
        }
    }

    public sealed partial class TeaStallSession
    {
        bool curriculumBusy, curriculumSpeechRunning;
        readonly Queue<KeyValuePair<TeaLine, string>> curriculumSpeech = new Queue<KeyValuePair<TeaLine, string>>();

        CurriculumConversation Curriculum => Provider as CurriculumConversation;
        bool CurriculumTyping => Curriculum == null || Turn == null || Curriculum.Typing(Turn);
        bool CurriculumAutoPhrases => Curriculum != null && Turn != null && !Curriculum.Typing(Turn);

        static string JoinFeedback(params string[] parts)
        {
            var kept = new List<string>();
            foreach (var part in parts) if (!string.IsNullOrWhiteSpace(part)) kept.Add(part.Trim());
            return string.Join("\n", kept);
        }

        // After Start or a choice: show quest/level notices, help and the task line.
        void CurriculumAfterTurn()
        {
            var c = Curriculum;
            if (c == null || !ui) return;
            foreach (var notice in c.Notices) ui.RecordSystemNotice(notice);
            c.Notices.Clear();
            conversation.SetFeedback(Turn == null ? "" : JoinFeedback(c.HelpFor(Turn), c.TaskFor(Turn)));
        }

        async void AskCurriculum()
        {
            var c = Curriculum; var turn = Turn; var npc = ActiveNpc;
            if (c == null || turn == null || !ui || ui.Mode != "conversation" || curriculumBusy || !c.Typing(turn)) return;
            string input = (TeaAssessmentInput ?? "").Trim();
            if (input.Length == 0 || input.Length > 512) return;
            string speaker = npc ? npc.displayName : turn.Speaker;
            ui.CloseTeaKeyboard(); curriculumSpeech.Clear(); StopVillageSpeech();
            ui.RecordChatMessage(input);
            TeaAssessmentInput = "";
            curriculumBusy = true;
            conversation.SetFeedback(speaker + " is thinking…");
            ui.ShowConversation();
            CurriculumReply reply = null;
            using (var cancel = new CancellationTokenSource(8000))
            {
                try { reply = await CurriculumReply.Request(c.NodeId(turn), input, Progress.playerName, c.Tries(turn), conversation.History(speaker), cancel.Token); }
                catch (Exception error) { Debug.LogWarning("PARIDHWANI_CURRICULUM_FAILURE " + error.GetType().Name); }
            }
            curriculumBusy = false;
            if (!this || !ui || Turn != turn || ActiveNpc != npc || !ReferenceEquals(c, Provider) || ui.Mode != "conversation") return;
            if (reply == null)
            {
                TeaAssessmentInput = input;
                conversation.SetFeedback("The language engine is not running. Start it (./start-demo.sh), then send again, or choose a phrase.");
                conversation.ShowPhrases();
                ui.ShowConversation();
                return;
            }
            if (!string.IsNullOrEmpty(reply.PlayerName)) Progress.playerName = reply.PlayerName;
            if (reply.Reaction != null) ui.RecordCurrentChatReply(reply.Reaction);
            conversation.Remember(speaker, input, reply.Reaction != null ? reply.Reaction.English : "");
            if (!string.IsNullOrEmpty(reply.ChoiceKey))
            {
                int index = c.ChoiceIndex(turn, reply.ChoiceKey);
                if (index < 0) { Debug.LogWarning("PARIDHWANI_CURRICULUM_UNKNOWN_CHOICE " + reply.ChoiceKey); ui.ShowConversation(); return; }
                Choose(index, false);
                if (ui.Mode == "conversation" && !string.IsNullOrEmpty(reply.Tip))
                {
                    conversation.SetFeedback(JoinFeedback("Tip: " + reply.Tip, conversation.Feedback));
                    ui.ShowConversation();
                }
                return;
            }
            if (reply.CountsAsWrong) c.RegisterWrong(turn);
            conversation.SetFeedback(JoinFeedback(c.HelpFor(turn), c.TaskFor(turn)));
            ui.ShowConversation();
        }

        // NPC lines in curriculum chats play one after another instead of cutting each other off.
        void QueueCurriculumSpeech(TeaLine line, string speaker)
        {
            if (line == null) return;
            while (curriculumSpeech.Count >= 4) curriculumSpeech.Dequeue();
            curriculumSpeech.Enqueue(new KeyValuePair<TeaLine, string>(line, speaker));
            if (!curriculumSpeechRunning) StartCoroutine(PumpCurriculumSpeech());
        }

        IEnumerator PumpCurriculumSpeech()
        {
            curriculumSpeechRunning = true;
            while (curriculumSpeech.Count > 0)
            {
                if (!ui || ui.Mode != "conversation") { curriculumSpeech.Clear(); break; }
                var item = curriculumSpeech.Dequeue();
                PlayVillageSpeech(item.Key, item.Value);
                yield return null;
                float started = Time.unscaledTime;
                while (villageSpeechCancel != null && Time.unscaledTime - started < 12f) yield return null;
                while (villageSpeechSource && villageSpeechSource.isPlaying) yield return null;
            }
            curriculumSpeechRunning = false;
        }
    }
}
