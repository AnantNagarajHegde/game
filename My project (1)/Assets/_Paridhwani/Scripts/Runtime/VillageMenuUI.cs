using UnityEngine;
using UnityEngine.UIElements;

namespace Paridhwani
{
    public sealed partial class TeaStallUI
    {
        string pauseSection = "visit";
        string notebookSection = "story";

        public void ShowPauseSection(string section)
        {
            pauseSection = section;
            BuildPauseMenu();
            FocusLater(root.Q<Button>("pause-nav-" + section));
        }

        public bool ClosePauseSection()
        {
            if (Mode != "pause" || session.TitleSettings || pauseSection == "visit") return false;
            ShowPauseSection("visit");
            return true;
        }

        void BuildPauseMenu()
        {
            var shell = Card("pause", "paper");
            shell.name = "pause-card";
            Class(shell, "menu-shell");
            shell.style.height = Length.Percent(85);
            var header = new VisualElement(); Class(header, "menu-header"); shell.Add(header);
            var heading = new VisualElement(); header.Add(heading);
            Add(heading, "PARIDHWANI", "eyebrow");
            Add(heading, session.TitleSettings ? "Settings" : "Paused", "title");
            bool resume = !session.TitleSettings && pauseSection == "visit";
            var back = Button(header, session.TitleSettings ? "Back to title" : resume ? "Resume" : "Back", session.Close, resume ? "button" : "quiet-button");
            back.name = "pause-back";
            if (resume) Class(back, "primary-button");

            var body = new VisualElement(); Class(body, "menu-body"); shell.Add(body);
            var navigation = new VisualElement(); Class(navigation, "menu-navigation"); body.Add(navigation);
            var content = new VisualElement(); Class(content, "menu-content"); body.Add(content);
            var scroll = MenuScroll(content, "pause-scroll");
            string[] sections = session.TitleSettings
                ? new[] { "display", "audio", "controls", "accessibility" }
                : new[] { "visit", "display", "audio", "controls", "accessibility" };
            var tabs = new Button[sections.Length];
            for (int i = 0; i < sections.Length; i++)
            {
                string section = sections[i]; int index = i;
                string label = section == "visit" ? "Your visit" : char.ToUpperInvariant(section[0]) + section.Substring(1);
                var tab = Button(navigation, label, () => ShowPauseSection(section), "menu-tab");
                tab.name = "pause-nav-" + section; tabs[i] = tab;
                tab.EnableInClassList("menu-tab-selected", section == pauseSection);
                tab.RegisterCallback<NavigationMoveEvent>(e =>
                {
                    int step = e.direction == NavigationMoveEvent.Direction.Down ? 1 : e.direction == NavigationMoveEvent.Direction.Up ? -1 : 0;
                    if (step != 0) { e.StopImmediatePropagation(); tabs[Mathf.Clamp(index + step, 0, tabs.Length - 1)].Focus(); }
                    else if (e.direction == NavigationMoveEvent.Direction.Right)
                    {
                        e.StopImmediatePropagation();
                        var target = scroll.Query<VisualElement>().ToList().Find(item => AuthoredControl(item) && CanFocus(item));
                        target?.Focus();
                    }
                });
            }
            content.RegisterCallback<NavigationMoveEvent>(e =>
            {
                if (e.direction != NavigationMoveEvent.Direction.Left) return;
                // Sliders and popup fields keep their own left/right editing behaviour.
                for (var item = e.target as VisualElement; item != null && item != content; item = item.parent)
                    if (item is Slider || item is DropdownField || item is TextField) return;
                e.StopImmediatePropagation(); root.Q<Button>("pause-nav-" + pauseSection)?.Focus();
            });
            switch (pauseSection)
            {
                case "display": BuildDisplayMenu(scroll); break;
                case "audio": BuildAudioMenu(scroll); break;
                case "controls": BuildControlsMenu(scroll); break;
                case "accessibility": BuildAccessibilityMenu(scroll, shell); break;
                default: BuildVisitMenu(scroll); break;
            }
            Add(shell, session.TitleSettings ? "Changes save automatically" : session.SaveMessage, "menu-status");
            FocusLater(session.TitleSettings ? root.Q<Button>("pause-nav-" + pauseSection) : back);
        }

        void MenuHeading(VisualElement parent, string title, string description)
        {
            Add(parent, title, "menu-title");
            Add(parent, description, "menu-description");
        }

        void BuildVisitMenu(VisualElement card)
        {
            MenuHeading(card, "Your visit", "Pick up where you left off.");
            var objectiveCard = new VisualElement(); Class(objectiveCard, "menu-objective"); card.Add(objectiveCard);
            Add(objectiveCard, "NEXT STEP", "eyebrow");
            Add(objectiveCard, session.Progress.Objective, "body");
            Button(card, "Field notebook", session.OpenNotebook).name = "pause-notebook";
            if (session.IsPracticeVisit) Button(card, "Return to the main visit", session.ReturnToCompletedStory);
            else Button(card, "Chapter practice", ShowChapterReplay).name = "pause-practice";
            AddPresentationActions(card);
            if (session.Progress.storyCompleted) Button(card, "Credits & phrase recap", ShowCredits);
            var saves = new VisualElement(); Class(saves, "menu-action-row"); card.Add(saves);
            Button(saves, "Save visit", () => { session.Save(); ShowPause(); }, "quiet-button").name = "pause-save";
            Button(saves, "Restore save", session.Load, "quiet-button").name = "pause-restore";
            var exits = new VisualElement(); Class(exits, "menu-action-row"); card.Add(exits);
            Button(exits, "Title screen", session.OpenTitle, "quiet-button");
            Button(exits, "Save & quit", session.RequestQuit, "quiet-button");
        }

        void BuildDisplayMenu(VisualElement card)
        {
            MenuHeading(card, "Display", "Tune the village to your screen.");
            AddDisplaySettings(card);
            var quality = new DropdownField("Rendering quality", new System.Collections.Generic.List<string> { "Balanced", "Low", "High" }, (int)PresentationPolicy.ValidQuality(session.Progress.quality));
            quality.name = "settings-quality";
            quality.RegisterValueChangedCallback(e => { session.Progress.quality = (VillageQuality)quality.index; session.SettingsChanged(); }); card.Add(quality);
            Add(card, "Balanced is the default. Low favours performance; High adds finer shadows.", "small");
            AddDeveloperDisplaySettings(card);
            card.Q<Foldout>("developer-display").value = false;
        }

        void BuildAudioMenu(VisualElement card)
        {
            Class(card, "audio-settings");
            MenuHeading(card, "Audio", "Set a comfortable listening level.");
            var description = card.Q<Label>(className: "menu-description");
            var audio = new VillageSwitch("Game audio") { name = "settings-mute", value = !session.Progress.muted };
            card.Add(audio);
            var volume = new VillageSlider("Master volume", 0, 1) { name = "settings-master-volume", value = session.Progress.masterVolume };
            volume.RegisterValueChangedCallback(e => { session.Progress.masterVolume = e.newValue; session.SettingsChanged(); }); card.Add(volume);
            var environment = new VillageSlider("Rain & river", 0, 1) { name = "settings-ambience-volume", value = session.Progress.ambienceVolume };
            environment.RegisterValueChangedCallback(e => { session.Progress.ambienceVolume = e.newValue; session.SettingsChanged(); }); card.Add(environment);
            var steps = new VillageSlider("Footsteps", 0, 1) { name = "settings-footstep-volume", value = session.Progress.footstepVolume };
            steps.RegisterValueChangedCallback(e => { session.Progress.footstepVolume = e.newValue; session.SettingsChanged(); }); card.Add(steps);
            void RefreshAudioControls()
            {
                volume.SetEnabled(audio.value); environment.SetEnabled(audio.value); steps.SetEnabled(audio.value);
                description.text = audio.value ? "Set a comfortable listening level." : "Audio is off. Turn it on to adjust your saved levels.";
            }
            audio.RegisterValueChangedCallback(e => { session.Progress.muted = !e.newValue; RefreshAudioControls(); session.SettingsChanged(); });
            RefreshAudioControls();
        }

        void BuildControlsMenu(VisualElement card)
        {
            MenuHeading(card, "Controls", "Adjust how movement and looking feel.");
            var sensitivity = new VillageSlider("Look sensitivity", .05f, .3f, "Slower", "Faster") { value = session.Progress.sensitivity };
            sensitivity.RegisterValueChangedCallback(e => { session.Progress.sensitivity = e.newValue; session.SettingsChanged(); }); card.Add(sensitivity);
            var invert = new VillageSwitch("Invert vertical look") { value = session.Progress.invertY };
            invert.RegisterValueChangedCallback(e => { session.Progress.invertY = e.newValue; session.SettingsChanged(); }); card.Add(invert);
            var vibration = new VillageSlider("Controller vibration", 0, 1, "Off", "Strong") { name = "settings-controller-vibration", value = session.Progress.controllerVibration };
            vibration.RegisterValueChangedCallback(e => session.SetControllerVibration(e.newValue)); card.Add(vibration);
            Add(card, "Set vibration to 0 to turn it off.", "small");
            Add(card, "IN THE VILLAGE", "section-title");
            Add(card, session.UsingController ? "Left stick · Walk     Right stick · Look\nA / Cross · Interact     View / Share · Notebook\nMenu / Options · Pause" : "WASD · Walk     Mouse · Look     Shift · Jog\nE · Interact     Tab · Notebook     Esc · Pause", "body");
        }

        void BuildAccessibilityMenu(VisualElement card, VisualElement shell)
        {
            MenuHeading(card, "Accessibility", "Read and explore at your own pace.");
            var size = new VillageSlider("Text size", .85f, 1.3f, "85%", "130%", number => Mathf.RoundToInt(number * 100) + "%") { name = "settings-text-size", value = session.Progress.textScale };
            size.RegisterValueChangedCallback(e => { session.Progress.textScale = e.newValue; ApplyCardScale(shell, "pause"); session.SettingsChanged(); }); card.Add(size);
            var motion = new VillageSwitch("Reduced camera motion") { value = session.Progress.reducedMotion };
            motion.RegisterValueChangedCallback(e => { session.Progress.reducedMotion = e.newValue; session.SettingsChanged(); }); card.Add(motion);
            Add(card, "CONVERSATION AIDS", "section-title");
            var english = new VillageSwitch("English meanings") { value = session.Progress.english };
            english.RegisterValueChangedCallback(e => { session.Progress.english = e.newValue; session.SettingsChanged(); }); card.Add(english);
            var roman = new VillageSwitch("Romanized Kannada") { value = session.Progress.roman };
            roman.RegisterValueChangedCallback(e => { session.Progress.roman = e.newValue; session.SettingsChanged(); }); card.Add(roman);
            Add(card, "You can also change these aids below any conversation.", "small");
        }

        void OrganizeNotebook(VisualElement card, ScrollView story)
        {
            var words = new ScrollView(ScrollViewMode.Vertical) { name = "notebook-words" };
            Class(words, "notebook-scroll"); words.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            card.Insert(card.IndexOf(story) + 1, words);
            // Collect first so reparenting never changes a live child enumeration.
            var vocabulary = new System.Collections.Generic.List<VisualElement>();
            foreach (var child in story.Children()) if (child.ClassListContains("word-row")) vocabulary.Add(child);
            foreach (var word in vocabulary) words.Add(word);
            if (vocabulary.Count == 0) Add(words, "Useful Kannada phrases will appear here as you meet people and explore.", "body");
            var tabs = new VisualElement(); Class(tabs, "notebook-tabs"); card.Insert(card.IndexOf(story), tabs);
            var notesTab = Button(tabs, "Story notes", () => SelectNotebookSection("story"), "menu-tab"); notesTab.name = "notebook-tab-story";
            var wordsTab = Button(tabs, "Words & phrases", () => SelectNotebookSection("words"), "menu-tab"); wordsTab.name = "notebook-tab-words";
            SelectNotebookSection(notebookSection, false);
        }

        void SelectNotebookSection(string section, bool focus = true)
        {
            notebookSection = section;
            root.Q("notebook-story").style.display = section == "story" ? DisplayStyle.Flex : DisplayStyle.None;
            root.Q("notebook-words").style.display = section == "words" ? DisplayStyle.Flex : DisplayStyle.None;
            root.Q("notebook-tab-story").EnableInClassList("menu-tab-selected", section == "story");
            root.Q("notebook-tab-words").EnableInClassList("menu-tab-selected", section == "words");
            if (focus) FocusLater(root.Q("notebook-tab-" + section));
        }
    }
}
