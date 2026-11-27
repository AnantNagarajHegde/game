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