# Manjunatha’s joint Kannada dialogue model

From this checkout, run the PowerShell launcher to open an isolated visit at Manjunatha’s tea stall.

~~~powershell
.\Start-ManjunathaJoint.ps1
~~~

Type **ಎರಡು ಚಹಾ ಕೊಡಿ. ಒಂದಕ್ಕೆ ಸಕ್ಕರೆ ಬೇಡ.** (“Two teas, please. No sugar in one.”) and press Enter. Manjunatha composes a reply in Kannada. Each part appears as its audio starts. You can also try **three hot coffees please with less sugar**. The experiment accepts short Kannada, Romanized Kannada and English requests. Its training menu covers tea, coffee, water, four quantities, sugar preferences and temperature. Authored phrases continue to control story progress. The generated answers have Kannada captions and one voice.

The existing Romanized and English controls show optional reading aids after a reply finishes. These aids translate the words the model actually emitted. Repeating the same prompt produces the same reply.

## Model

The trainable checkpoint contains 341,504 parameters and occupies 1.27 MB on disk. A prompt encoder combines learned word and adjacent-word embeddings. A 96-unit GRU predicts the next Kannada word from the prompt and the words already emitted. An acoustic head takes that same recurrent state, the emitted word embedding, neighboring characters and utterance position. It predicts a duration and a 192-channel acoustic distribution for each character or blank. During final fine-tuning, both the text and acoustic losses update shared parameters.

A frozen 85.8 MB decoder from Meta’s Kannada MMS turns these acoustic features into 16 kHz mono audio. Its ONNX graph has two inputs, acoustic latents and a frame mask. The running game loads the custom checkpoint and this decoder. The full MMS text encoder and duration predictor are used offline to produce training targets. The original checkpoint is distributed under CC BY-NC 4.0. [Meta’s Kannada model card](https://huggingface.co/facebook/mms-tts-kan) · [Pinned ONNX export](https://huggingface.co/onnx-community/mms-tts-kan-ONNX/tree/cac3976902fe64861618432d00337d19e8c2ec61)

One loopback request streams successive groups of up to three words with their generated audio. The client checks the turn ID and sequence number, plays each group, and reveals its text after the Unity audio cursor advances. The acoustic head can also read Manjunatha’s existing scripted opening. Every typed answer comes from autoregressive word prediction. The training sentence generator and benchmark answers have no role in serving replies.

## Training and reproduction

The offline corpus contains 1,115 prompt–reply pairs and 92 distinct training replies. Six complete combinations of drink, quantity, sugar and temperature are held out across all input languages. Their expected reply strings are absent from training. The acoustic targets come from synthetic Kannada speech features aligned to the words of training sentences and the tea stall’s authored lines. The language and acoustic heads share learned word embeddings. This experiment targets the tea-stall vocabulary. Each player message is evaluated independently.

On a fresh checkout with Unity 6000.6.7f1 and Python installed, prepare the decoder and build the player once. The launcher downloads the pinned model and verifies its source hashes. It creates a project-local Python environment. The trained custom weights are included in the branch.

~~~powershell
.\Start-ManjunathaJoint.ps1 -Prepare -Build
~~~

To reproduce training on CPU, use the command below. Training runs before the player starts. Its memory usage is outside the gameplay measurement. Pass an existing prepared MMS directory with `-Teacher` to reuse local assets.

~~~powershell
.\Start-ManjunathaJoint.ps1 -Prepare -Train -Build
~~~

## Laptop test

~~~powershell
.\Start-ManjunathaJoint.ps1 -Benchmark
.\Start-ManjunathaJoint.ps1 -Benchmark -Width 1920 -Height 1080 -TextScale 1.3
~~~

The launcher starts an isolated Unity Editor, the standalone game and the actual Paridhwani assessment engine. The game warms the tea stall with speech off. After the Editor settles for 30 seconds, the harness measures the three process trees for three seconds. It then starts the joint model cold. Unity sends all six unseen orders twice through the regular chat input path and checks the complete replies, the rendered caption at each playback cursor and the original quest turn. Each run saves its JSON measurements, logs and a screenshot. For a differently located engine checkout, pass `-Engine` with the path to `src/assessment_host.py`.

The RAM limit is 1,000,000,000 bytes. The sampled measurement adds each baseline process tree’s positive growth and the model process’s physical working set. The conservative figure adds the largest baseline-tree growths, the model process’s Windows peak working set and launcher peaks, even when these happened at different times. The renderer runs on CPU. The game’s dedicated GPU memory is outside these RAM figures.

On the Intel Core 5 210H laptop with 15.64 GiB of usable system RAM, both playtests passed. Each run used the same custom checkpoint and frozen decoder. MB here means one million bytes.

| Player and text scale | Peak sampled extra RAM | Conservative extra RAM including the Python launchers | Median time to first audible caption |
| --- | ---: | ---: | ---: |
| 960 × 540 at 100% | 273.0 MB | 337.9 MB | 0.98 s |
| 1920 × 1080 at 130% | 278.7 MB | 348.4 MB | 1.01 s |

In each run, all 12 typed turns returned the expected Kannada. Unity verified that all 42 generated caption groups were inside the chat viewport while their corresponding audio clip was playing. The model process tree peaked at 207.2 MB and 207.7 MB respectively. [Compact measurement](../verification/manjunatha-joint/compact.json) · [1080p measurement](../verification/manjunatha-joint/1080p.json)

The compact capture shows the first three generated words. The large-text capture shows the complete reply about leaving one of the two teas unsweetened. [First spoken caption](../verification/manjunatha-joint/streaming-960.png) · [Completed Kannada reply](../verification/manjunatha-joint/reply-1080.png)

## Speech check

A separate offline Kannada recognizer transcribed the six generated WAVs without access to the prompts or expected replies. The same recognizer also transcribed the original MMS model reading the same sentences. After normalizing punctuation and equivalent written numerals, the generated audio had one word substitution in 51 reference words. In the four-coffee order, the recognizer wrote **ಕಾಸಿ** for **ಕಾಫಿ**. All six generated Kannada captions matched their held-out targets exactly. A fluent listener can assess the single voice’s prosody from the sample. [Listen to the two-tea order](../verification/manjunatha-joint/two-teas.wav) · [Transcripts and controls](../verification/manjunatha-joint/pronunciation.json) · [Held-out inputs and generated replies](../verification/manjunatha-joint/held-out.json)

The independent verifier used the INT8 Koyal Kannada ONNX checkpoint at revision `6ec535ac40b50f7046f17fa1f5eea32360acdefd` and `onnx-asr==0.12.0`. To reproduce the speech check after training, run `village_backend/joint_tea/evaluate.py`, then `verification/CheckJointPronunciation.py --cache verification/Temp/JointTea --asr <local Koyal directory> --teacher <prepared MMS directory>` in a verification environment with that package. Recognition runs separately from the game RAM test. [Kannada verifier model](https://huggingface.co/adalat-ai/koyal-kn-120m-1.0-onnx)
