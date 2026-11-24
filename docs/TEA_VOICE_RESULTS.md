# Tea-stall laptop measurements

Measured on 24 November 2026 on the development laptop with an Intel Core 5 210H, 15.64 GiB usable RAM and an RTX 4050 Laptop GPU rendering the Unity player. Both inference models ran on the CPU. Each test had the Unity 6000.6.7f1 Editor, Windows player and Paridhwani assessment host running before the model services started.

| Run | Captured player size / text scale | Peak additional resident RAM | Sum of separate peak increases¹ | Typed order to first spoken phrase |
| --- | --- | ---: | ---: | ---: |
| First build, newly imported Editor project | 960 × 540 / 1.0 | 1,128 MB | 1,213 MB | Unmeasured |
| Final dialogue implementation, 30 s settle | 960 × 540 / 1.0 | 633 MB | 691 MB | 1.48 s |
| Final dialogue implementation, 30 s settle | 1920 × 1171 / 1.3 | 664 MB | 744 MB | 2.40 s |
| Final dialogue implementation, cached project, no extra settle | 1920 × 1080 / 1.3 | 626 MB | 672 MB | 1.50 s |

MB is decimal. The target was 1,000 MB above the baseline. The first build exceeded it while the Editor process tree grew by 544 MB from its baseline. The three final runs met it. The window manager maximized the first large window, producing a taller capture than requested. The final borderless repeat produced the exact 1080p capture.

¹ This estimate adds Windows lifetime peak working sets for each new model service to the greatest observed increase for each baseline process tree. Individual peaks can occur at different times. The primary column instead uses increases observed in the same sample. Sampling can miss brief changes. See the [measurement method](TEA_VOICE_PROTOTYPE.md) and [raw results](../verification/tea-voice).

Each successful run exercised the opening greeting, a typed tea order and an authored follow-up. The player confirmed three completed NPC lines and six on-screen Kannada prefixes while the audio playback cursor had advanced. The 95th-percentile frame interval during dialogue was 11.70 ms in the compact run and 13.89 ms in both large-window runs. These are short stationary exchanges. They do not establish a long-session ceiling, general Kannada comprehension or pronunciation quality.

During development, one intermediate trial failed when the text generator reached its token limit before completing a permitted reply. The final decoder only generates through the first distinguishing word. Six additional live model-and-speech checks covered tea, water, refusal, greeting, help and unknown input. The Windows player built successfully and the repository source check passed all 776 chapter assertions.

[Compact capture](../verification/tea-voice/caption-960.png) · [1080p capture](../verification/tea-voice/caption-1080.png) · [Kannada synthesis sample of “ಚಹಾ ಬೇಕಾ?”](../verification/tea-voice/tea-question.wav)
