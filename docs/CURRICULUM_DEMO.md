# Curriculum demo (levels 1 to 3, chapters 1 to 3)

This branch makes the first three story chapters follow the learning-design curriculum.
The learning-design JSON in `language-engine` is the master text.

## How a typed answer works

1. Unity sends `{version, node, text, player_name, wrong_count, history}` to `POST /v2/curriculum/turn`
   on the existing backend (`village_backend/server.py`, port 8767).
2. `curriculum_engine.py` runs the language-engine matcher (`Paridhwani-NLP`). The matcher alone
   decides if the answer is right. Kannada-script input is mapped to Kanglish with the verified
   `translit_pairs.csv` before matching.
3. An accepted answer returns a `choice_key`. Unity selects the authored choice with that key, so the
   story moves exactly as it does with Phrases.
4. A known mistake returns the authored reaction from `feedback.json`, and the quick hint after 2 wrong
   tries and the full example after 3 (from `progression.json`).
5. Only an unanticipated, unclear answer asks Sarvam 105B Conversations for a one or two sentence reply
   (reasoning off, 4.5 s budget). It never advances the story. Without a key, offline or on timeout,
   an authored fallback line from `curriculum/npc.json` is used.

## Files

| File | Purpose |
|---|---|
| `village_backend/curriculum/flow.json` | Node sequence for Manjunatha, Somanna and Shanthakka: lines, Phrases, effects, quest completions |
| `village_backend/curriculum/npc.json` | Fallback lines and NPC cards for live replies |
| `village_backend/curriculum_engine.py` | The turn engine |
| `village_backend/test_curriculum.py` | Checks every Phrase and every authored answer against the real matcher |

## Run the checks (Mac)

```bash
cd game/village_backend
export PARIDHWANI_LANGUAGE_ENGINE="$(cd ../../language-engine && pwd)"
python3 -m pip install pandas
python3 -m unittest test_curriculum test_server test_connection -v
```

## Check the live reply once (uses a little Sarvam credit)

```bash
export SARVAM_API_KEY=...        # never commit this
python3 curriculum_engine.py --check-live --text "Is it going to rain today?"
```

It prints the reply, the elapsed time and `source: live`. `source: fallback` means the call failed or took
longer than 4.5 s.

## Unity side

| File | Purpose |
|---|---|
| `Runtime/CurriculumData.g.cs` | Generated from `flow.json` and the curriculum JSON. Regenerate with `python tools/gen_curriculum_cs.py` |
| `Runtime/CurriculumConversation.cs` | Provider for Manjunatha (level 1), Somanna (level 2) and Shanthakka (level 3): nodes, story effects, quest and level completion, hints, objective |
| `Runtime/CurriculumSession.cs` | Sends typed answers to the engine, shows reactions, tips and notices, plays NPC lines one after another |

The curriculum is active for Manjunatha until the library directions are known, for Somanna until the mess lead is
known, and for Shanthakka until the market lead is known. After that the existing story providers take over.
Set `PARIDHWANI_CURRICULUM=0` before starting Unity to switch it off (for the old automated playtests).
Progress is saved in the visit: player name, the node each NPC is on, completed quests and collected words.

## Play-mode checklist (levels 1 to 3)

Start the backend first (`python server.py --provider ollama --model preview --port 8767`), then press Play.
Use **Paridhwani > Play Isolated Test Visit** so the main save stays clean.

| # | Do | Expect |
|---|---|---|
| 1 | Talk to Manjunatha | Notice "LEVEL 1 · BUS STAND & TEA STALL · Quest: Order tea", then ನಿಮಗೆ ಏನು ಬೇಕು? |
| 2 | Type `Nanage kaapi beku` | ಕಾಫಿ ಇಲ್ಲ. ಚಹಾ ಅಥವಾ ನೀರು? and no progress |
| 3 | Type `asdf` twice | Fallback line twice, then "Hint: Start with Nanage..." |
| 4 | Type `Nanage chaha` | Moves on, "Tip: Add the verb at the end..." |
| 5 | Answer ನೀರು ಬೇಕಾ? with `Haudu, beku` | "QUEST COMPLETE · Order tea", menu board with three word buttons and no text box |
| 6 | Tap ಕಾಫಿ, then ಚಹಾ | "Not that one. Look again.", then the name question |
| 7 | Type `Nanna hesaru` + your name | "QUEST COMPLETE · Tell Manjunatha your name" |
| 8 | Type `Library ellide?` | "LEVEL 1 COMPLETE", the next-level line, directions. Objective at top left changes to Level 2 |
| 9 | Leave, walk to Somanna, talk | Level 2 banner, ಯಾರು? |
| 10 | Type `Bagilu tegi` at the door step | Somanna frowns (no voice), hint after the second wrong try |
| 11 | Finish level 2 (`Ananya`, tap ಕೀಲಿ, `Keeli ellide?`) | "LEVEL 2 COMPLETE", Shanthakka unlocked |
| 12 | Level 3: `Haudu, oota kodi`, `Saaku`, `Eshtu rupayi?`, `Hattu`, `Aivattu`, `Ananya elli?`, tap ಬಸವರಾಜು | Wrong price gets ಇಲ್ಲ, ಐವತ್ತು!; ends with "LEVEL 3 COMPLETE" and the market lead |
| 13 | Stop the backend and type an answer | "The language engine is not running..." and the phrases open |
| 14 | Leave in the middle of a level and talk again | Resumes at the same step |

## Lines that need Kannada review

These are new lines (everything else is copied from the JSON or the existing game).

| Where | What | Kannada | Roman | English |
|---|---|---|---|---|
| m_order | not available | {item_kn} ಇಲ್ಲ. ಚಹಾ ಅಥವಾ ನೀರು? | {item_ro} illa. Chaha athava neeru? | No {item_en} here. Tea or water? |
| s_open | NPC line | ಏನು ಬೇಕು? | Enu beku? | What do you need? |
| s_where | NPC line | ಏನು ಬೇಕು? | Enu beku? | What do you need? |
| s_lead | NPC line | ನನ್ನ ಬಳಿ ಮುಖ್ಯ ಕೀಲಿ ಇದೆ. ಸ್ವಲ್ಪ ಕಾಯಿರಿ. ಅನನ್ಯ ಶಾಂತಕ್ಕನ ಮನೆಗೆ ಹೋಗಿದ್ದರು. ಮುಂದೆ ಹೋಗಿ, ಆಮೇಲೆ ಎಡಕ್ಕೆ. | Nanna bali mukhya keeli ide. Swalpa kaayiri. Ananya Shanthakkana manege hogiddaru. Munde hogi, aamele edakke. | I have the master key. Wait a little. Ananya went to Shanthakka's mess. Go straight, then left. |
| h_meal | not available | {item_kn} ಇಲ್ಲ. ಊಟ ಬೇಕಾ? | {item_ro} illa. Oota beka? | No {item_en} here. Would you like a meal? |
| h_where | NPC line | ಸರಿ, ಧನ್ಯವಾದ. ನಿಮಗೆ ಇನ್ನೇನು ಬೇಕು? | Sari, dhanyavada. Nimage innenu beku? | All right, thank you. What else do you need? |
| h_note | NPC line | ಗೊತ್ತಿಲ್ಲ. ಬೆಳಿಗ್ಗೆ ಎರಡು ಊಟ ತೆಗೆದುಕೊಂಡರು. ಈ ಚೀಟಿ ಇಲ್ಲೇ ಬಿಟ್ಟಿದ್ದರು. | Gottilla. Beligge eradu oota tegedukondaru. Ee cheeti ille bittiddaru. | I don't know. She collected two packed meals yesterday morning and left this shopping note here. |
| manjunatha | fallback 1 | ಕ್ಷಮಿಸಿ, ಅರ್ಥ ಆಗಲಿಲ್ಲ. ಇನ್ನೊಮ್ಮೆ ಹೇಳಿ. | Kshamisi, artha aagalilla. Innomme heli. | Sorry, I didn't understand. Please say it again. |
| manjunatha | fallback 2 | ನಿಧಾನವಾಗಿ ಹೇಳಿ. ಏನು ಬೇಕು? | Nidhanavagi heli. Enu beku? | Say it slowly. What would you like? |
| somanna | fallback 1 | ಕ್ಷಮಿಸಿ, ಅರ್ಥ ಆಗಲಿಲ್ಲ. ಮತ್ತೊಮ್ಮೆ ಹೇಳಿ. | Kshamisi, artha aagalilla. Mattomme heli. | Sorry, I didn't understand. Please say it once more. |
| somanna | fallback 2 | ದಯವಿಟ್ಟು ಕನ್ನಡದಲ್ಲಿ ಹೇಳಿ. | Dayavittu Kannadadalli heli. | Please say it in Kannada. |
| shanthakka | fallback 1 | ಏನು? ಇನ್ನೊಮ್ಮೆ ಹೇಳಿ. | Enu? Innomme heli. | What? Say it again. |
| shanthakka | fallback 2 | ಅರ್ಥ ಆಗಲಿಲ್ಲ. ನಿಧಾನವಾಗಿ ಹೇಳಿ. | Artha aagalilla. Nidhanavagi heli. | I didn't understand. Say it slowly. |