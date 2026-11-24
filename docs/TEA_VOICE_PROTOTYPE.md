# Tea-stall text and voice prototype

This branch runs a local CPU dialogue experiment in the Paridhwani tea stall. The launcher opens an isolated visit with Manjunatha. Type `Nanage chaha beku`, `ಚಹಾ ಬೇಕು`, `Nanage neeru beku`, or `Chaha beda` and press Send. Phrases also lets you continue the authored story. Close the player to stop its model services.

On the test laptop, run this from the prototype checkout in PowerShell.

```powershell
.\Start-TeaVoicePrototype.ps1
```

On a fresh Windows checkout with Python 3.14 and Unity 6000.6.7f1, the following command prepares an isolated Python environment, downloads checksum-pinned CPU models, builds the player and opens the demo. The models, generated builds and per-run logs live under `verification/Temp/TeaVoice`. Allow several GB of disk space for Unity's Library and build caches. Supply `-Editor` or `-Python` for a custom installation.

```powershell
.\Start-TeaVoicePrototype.ps1 -Prepare -Build
```

## Dialogue and playback

A small Kannada/Kanglish phrase matcher identifies greetings, orders, refusals and requests for help. For each supported intent, Gemma 3 270M (Q8, llama.cpp, CPU) decodes a Kannada prefix under a two-response grammar. Once the prefix uniquely determines the line, the engine expands it and returns matching Kannada, Romanization and English. Unknown inputs use a request to repeat. Typed prototype replies are conversational. The game's authored Phrases handle quest choices. This design measures a bounded tea-stall task. Open conversation and a jointly trained speech decoder would require further model work.

For each Kannada phrase, the CPU speech service applies MMS-VITS in ONNX Runtime. The player requests the next phrase during playback, limits buffering to the current and next clips, and updates the existing Kannada chat label once the current clip's playback cursor advances. Closing a conversation or starting a new turn cancels playback. On a speech failure the whole Kannada line becomes readable. The English and Romanization aids retain their existing display behavior.

The pinned third-party ONNX conversion has incorrect right padding in twelve encoder convolutions. The preparation script verifies the source SHA-256 and restores the symmetric padding specified for the two kernel-3 feed-forward convolutions in each of the six layers. It writes a separate graph in the local cache. The original weights and vocabulary come from Meta's Kannada MMS model, licensed CC BY-NC 4.0. Gemma is distributed under Google's Gemma terms. [Kannada checkpoint](https://huggingface.co/facebook/mms-tts-kan) · [ONNX export](https://huggingface.co/onnx-community/mms-tts-kan-ONNX) · [Gemma weights](https://huggingface.co/ggml-org/gemma-3-270m-it-GGUF).

## Laptop benchmark

Run this from the same checkout. Point `-Engine` at the existing language-engine assessment host if the main project is in another directory.

```powershell
.\Start-TeaVoicePrototype.ps1 -Benchmark
```

The harness starts the Unity Editor, the actual Paridhwani assessment service and the standalone game. It warms the tea-stall view with the same dialogue UI and speech disabled, then samples three seconds for the baseline. The current harness allows 30 additional seconds for Editor startup before that baseline. It starts both model services from cold and exercises a greeting, a typed request and another authored turn in the player. The assessment service stays resident alongside these turns. The prototype dialogue backend handles the typed tea request. The harness closes its own processes after the test.

The memory target is 1,000,000,000 additional bytes of resident RAM. Each sample sums positive growth from each baseline process tree and all resident memory of the new model processes. Sampling starts before the models launch. A second estimate adds the model processes' Windows peak working sets and each baseline tree's largest sampled increase, even when their peaks occur at different times. Summing process working sets can count shared pages twice. These numbers describe a short CPU run and exclude dedicated graphics memory.

Each run saves a JSON measurement, game and engine logs and a native player screenshot. The game checks the visible Kannada label against each expected prefix after the audio cursor advances and records frame times and typed-turn latency. A fluent Kannada listener still needs to assess pronunciation. See [laptop measurements](TEA_VOICE_RESULTS.md).
