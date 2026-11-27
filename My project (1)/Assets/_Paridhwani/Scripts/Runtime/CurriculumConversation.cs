using System;
using System.Collections.Generic;

namespace Paridhwani
{
    public sealed class CurriculumLine
    {
        public readonly string Kn, Ro, En;
        public CurriculumLine(string kn, string ro, string en) { Kn = kn ?? ""; Ro = ro ?? ""; En = en ?? ""; }
        public TeaLine ToTeaLine(bool silent = false)
        {
            var line = new TeaLine(Kn, Ro, En);
            line.Silent = silent || Kn.Length == 0;
            return line;
        }
        public string Display() { return Ro.Length > 0 ? Ro + (En.Length > 0 ? " (" + En + ")" : "") : (Kn.Length > 0 ? Kn : En); }
    }

    public sealed class CurriculumChoice
    {
        public readonly string Key, Next;
        public readonly CurriculumLine Line, Reaction;
        public readonly string[] Effects, Completes;
        public readonly bool Wrong;
        public CurriculumChoice(string key, CurriculumLine line, string next, string[] effects, string[] completes, bool wrong, CurriculumLine reaction)
        { Key = key; Line = line; Next = next; Effects = effects; Completes = completes; Wrong = wrong; Reaction = reaction; }
    }

    public sealed class CurriculumNode
    {
        public readonly string Id, Npc, Kind, Quest, Step, Task, QuickHint;
        public readonly CurriculumLine Line, FullExample;
        public readonly bool Silent, Optional, EnglishHidden;
        public readonly CurriculumChoice[] Choices;
        public CurriculumNode(string id, string npc, string kind, string quest, string step, CurriculumLine line, string task, string quickHint,
            CurriculumLine fullExample, bool silent, bool optional, bool englishHidden, CurriculumChoice[] choices)
        {
            Id = id; Npc = npc; Kind = kind; Quest = quest ?? ""; Step = step ?? ""; Line = line; Task = task ?? ""; QuickHint = quickHint ?? "";
            FullExample = fullExample; Silent = silent; Optional = optional; EnglishHidden = englishHidden; Choices = choices;
        }
        public bool Typing { get { return Kind == "respond"; } }
    }

    public sealed class CurriculumQuest
    {
        public readonly string Id, Title, Objective;
        public readonly int Level;
        public CurriculumQuest(string id, int level, string title, string objective) { Id = id; Level = level; Title = title; Objective = objective; }
    }

    public sealed class CurriculumLevel
    {
        public readonly int Number;
        public readonly string Name, Next;
        public readonly string[] Quests;
        public CurriculumLevel(int number, string name, string[] quests, string next) { Number = number; Name = name; Quests = quests; Next = next; }
    }

    public sealed class CurriculumNpcInfo
    {
        public readonly string Key, Display, Start;
        public readonly VillageNpcRole Role;
        public readonly int Level;
        public CurriculumNpcInfo(string key, VillageNpcRole role, string display, string start, int level) { Key = key; Role = role; Display = display; Start = start; Level = level; }
    }

    public static partial class CurriculumData
    {
        public static CurriculumNode Node(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            int mark = id.IndexOf('#');
            if (mark >= 0) id = id.Substring(0, mark);
            foreach (var node in Nodes) if (node.Id == id) return node;
            return null;
        }
        public static CurriculumQuest Quest(string id) { foreach (var q in Quests) if (q.Id == id) return q; return null; }
        public static CurriculumLevel Level(int number) { foreach (var l in Levels) if (l.Number == number) return l; return null; }
        public static CurriculumNpcInfo Npc(VillageNpcRole role) { foreach (var n in Npcs) if (n.Role == role) return n; return null; }
    }

    // Chapters 1 to 3 follow the learning-design curriculum (levels 1 to 3).
    // The language engine decides typed answers; this provider only moves through authored nodes.
    public sealed class CurriculumConversation : ITeaConversationProvider
    {
        public readonly CurriculumNpcInfo Info;
        public readonly List<string> Notices = new List<string>();
        string triesNode = "";
        int tries;

        public CurriculumConversation(VillageNpcRole role)
        {
            Info = CurriculumData.Npc(role);
            if (Info == null) throw new ArgumentException("No curriculum for " + role);
        }

        public static bool Enabled { get { return Environment.GetEnvironmentVariable("PARIDHWANI_CURRICULUM") != "0"; } }

        public static bool Handles(VillageNpcRole role, TeaProgress p)
        {
            if (p == null || !Enabled) return false;
            switch (role)
            {
                case VillageNpcRole.TeaVendor: return !p.directionsKnown;
                case VillageNpcRole.Librarian: return !p.messLeadKnown && !p.guddaFamilyLeadKnown;
                case VillageNpcRole.MessOwner: return p.messLeadKnown && !p.marketLeadKnown;
                default: return false;
            }
        }

        public static void EnsureLists(TeaProgress p)
        {
            if (p.questsDone == null) p.questsDone = new List<string>();
            if (p.curriculumWords == null) p.curriculumWords = new List<string>();
            if (p.playerName == null) p.playerName = "";
        }

        static string Saved(TeaProgress p, string npc)
        {
            switch (npc) { case "manjunatha": return p.teaNode; case "somanna": return p.libraryNode; default: return p.messNode; }
        }
        static void Store(TeaProgress p, string npc, string node)
        {
            switch (npc) { case "manjunatha": p.teaNode = node; break; case "somanna": p.libraryNode = node; break; default: p.messNode = node; break; }
        }

        public TeaTurn Start(TeaProgress p)
        {
            EnsureLists(p);
            Notices.Clear();
            var node = CurriculumData.Node(Saved(p, Info.Key));
            if (node == null || node.Npc != Info.Key || node.Kind == "story") node = CurriculumData.Node(Info.Start);
            Store(p, Info.Key, node.Id);
            ResetTries(node.Id);
            var level = CurriculumData.Level(Info.Level);
            var quest = CurriculumData.Quest(node.Quest);
            if (level != null && quest != null)
                Notices.Add("LEVEL " + level.Number + " · " + level.Name.ToUpperInvariant() + " · Quest: " + quest.Title);
            return Build(node, null);
        }

        public TeaTurn Reply(TeaTurn turn, int index, TeaProgress p)
        {
            EnsureLists(p);
            Notices.Clear();
            var node = CurriculumData.Node(turn.Id);
            if (node == null || index < 0 || index >= node.Choices.Length) throw new ArgumentOutOfRangeException("index");
            var choice = node.Choices[index];
            if (choice.Wrong)
            {
                RegisterWrong(turn);
                return Build(node, choice.Reaction);
            }
            foreach (var effect in choice.Effects) Apply(effect, p);
            foreach (var quest in choice.Completes) Complete(quest, p);
            if (choice.Next == "close") { Store(p, Info.Key, ""); return null; }
            var next = CurriculumData.Node(choice.Next);
            if (next == null) throw new InvalidOperationException("Missing curriculum node " + choice.Next);
            if (next.Quest.Length > 0 && next.Quest != node.Quest && !p.questsDone.Contains(next.Quest))
            {
                var quest = CurriculumData.Quest(next.Quest);
                if (quest != null) Notices.Add("Next quest: " + quest.Title);
            }
            Store(p, Info.Key, next.Id);
            ResetTries(next.Id);
            return Build(next, null);
        }

        TeaTurn Build(CurriculumNode node, CurriculumLine retryLine)
        {
            var source = retryLine ?? node.Line;
            var turn = new TeaTurn(retryLine == null ? node.Id : node.Id + "#retry", source.Kn, source.Ro, source.En);
            turn.Speaker = Info.Display;
            turn.Line.Silent = (retryLine == null && node.Silent) || source.Kn.Length == 0;
            foreach (var choice in node.Choices)
            {
                var item = new TeaChoice(choice.Line.Kn, choice.Line.Ro, choice.Line.En, choice.Next, string.Join(",", choice.Effects));
                item.Line.Silent = true;
                turn.Choices.Add(item);
            }
            return turn;
        }

        static void Apply(string effect, TeaProgress p)
        {
            string name = effect, arg = "";
            int colon = effect.IndexOf(':');
            if (colon >= 0) { name = effect.Substring(0, colon); arg = effect.Substring(colon + 1); }
            switch (name)
            {
                case "greet": p.greeted = true; break;
                case "drink": p.drinkOrdered = true; p.drink = arg; break;
                case "name": if (string.IsNullOrEmpty(p.playerName)) p.playerName = arg; break;
                case "directions":
                    if (!p.drinkOrdered) throw new InvalidOperationException("Order a drink before asking the way.");
                    p.directionsKnown = true; break;
                case "routine": p.ananyaRoutineKnown = true; break;
                case "library_greet": p.metSomanna = true; break;
                case "mess_lead": p.messLeadKnown = true; break;
                case "meal": p.mealOrdered = true; break;
                case "enough": p.servingComplete = true; break;
                case "note":
                    if (!p.messLeadKnown || !p.servingComplete) throw new InvalidOperationException("Finish the meal before the note.");
                    p.shoppingNoteCollected = true; break;
                case "market_lead": p.marketLeadKnown = true; break;
                case "word": case "read": if (!p.curriculumWords.Contains(arg)) p.curriculumWords.Add(arg); break;
                default: throw new ArgumentException("Unknown curriculum effect " + effect);
            }
        }

        void Complete(string questId, TeaProgress p)
        {
            if (p.questsDone.Contains(questId)) return;
            p.questsDone.Add(questId);
            var quest = CurriculumData.Quest(questId);
            if (quest == null) return;
            Notices.Add("QUEST COMPLETE · " + quest.Title);
            var level = CurriculumData.Level(quest.Level);
            if (level == null) return;
            foreach (var q in level.Quests) if (!p.questsDone.Contains(q)) return;
            Notices.Add("LEVEL " + level.Number + " COMPLETE · " + level.Name);
            Notices.Add(level.Next);
        }

        void ResetTries(string node) { triesNode = node; tries = 0; }
        public string NodeId(TeaTurn turn) { var n = CurriculumData.Node(turn.Id); return n == null ? "" : n.Id; }
        public bool Typing(TeaTurn turn) { var n = CurriculumData.Node(turn.Id); return n != null && n.Typing; }
        public int Tries(TeaTurn turn) { return NodeId(turn) == triesNode ? tries : 0; }
        public void RegisterWrong(TeaTurn turn)
        {
            string id = NodeId(turn);
            if (id != triesNode) ResetTries(id);
            tries++;
        }

        public int ChoiceIndex(TeaTurn turn, string key)
        {
            var node = CurriculumData.Node(turn.Id);
            if (node == null) return -1;
            for (int i = 0; i < node.Choices.Length; i++) if (node.Choices[i].Key == key && !node.Choices[i].Wrong) return i;
            return -1;
        }

        public string TaskFor(TeaTurn turn)
        {
            var node = CurriculumData.Node(turn.Id);
            return node == null || node.Task.Length == 0 ? "" : "Task: " + node.Task;
        }

        // Progression rules: quick hint after 2 wrong tries, full example offered after 3.
        public string HelpFor(TeaTurn turn)
        {
            var node = CurriculumData.Node(turn.Id);
            int count = Tries(turn);
            if (node == null || count < 2) return "";
            string help = node.QuickHint.Length > 0 ? "Hint: " + node.QuickHint : "";
            if (count >= 3 && node.FullExample != null && node.Typing)
                help += (help.Length > 0 ? "  ·  " : "") + "Example: " + node.FullExample.Display();
            return help;
        }

        public static string Objective(TeaProgress p)
        {
            if (p == null || !Enabled || !p.arrivalAcknowledged) return null;
            string npc, node;
            if (!p.directionsKnown) { npc = "manjunatha"; node = p.teaNode; }
            else if (!p.messLeadKnown && !p.guddaFamilyLeadKnown) { npc = "somanna"; node = p.libraryNode; }
            else if (p.messLeadKnown && !p.marketLeadKnown) { npc = "shanthakka"; node = p.messNode; }
            else return null;
            CurriculumNpcInfo info = null;
            foreach (var n in CurriculumData.Npcs) if (n.Key == npc) info = n;
            if (info == null) return null;
            var current = CurriculumData.Node(node);
            if (current == null || current.Npc != npc || current.Quest.Length == 0) current = CurriculumData.Node(info.Start);
            var quest = CurriculumData.Quest(current.Quest);
            if (quest == null) return null;
            return "Level " + quest.Level + " · " + quest.Title + ": " + quest.Objective + " (" + info.Display + ")";
        }
    }
}
