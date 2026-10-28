# Paridhwani

Paridhwani is a story-led Kannada-learning adventure built in Unity. The prototype follows an archive worker investigating Ananya's disappearance and helping protect the village library. Players explore the village, speak with its residents, collect evidence, and practise Kannada through authored dialogue and activities.

The main story is authored from arrival through the council review and epilogue, with a separate practice visit that preserves the completed story save. The game is still a procedural prototype, not a release-ready product; final art, fluent language review, a connected NLP service, and broader player acceptance testing remain outstanding.

## Open and play

1. Open `My project (1)` in Unity Hub using **Unity 6000.6.7f1**.
2. Open `Assets/_Paridhwani/Scenes/TeaStall_Benchmark.unity`.
3. Press **Play**.

If generated assets are missing, use **Paridhwani > Create Tea Stall Benchmark** in the Editor. For controls, story walkthrough, and save details, see [play and build instructions](docs/TEA_STALL_BENCHMARK.md).

## Verification

The latest recorded native Editor regression passed on 28 October 2026, covering the main story through its epilogue and isolated practice visit. Runtime/editor source compilation and 367 standalone assertions for the committed presentation batches also pass. These checks do **not** constitute a fresh standalone Player build or full human acceptance test.

From this repository root, run source and logic checks in PowerShell:

```powershell
.\verification\Verify-Sources.ps1
```

This requires the project to have been imported in Unity and the configured Unity Editor and cached compiler references to be available. In a licensed desktop Unity session, the batch workflow can regenerate, check, build, and optionally smoke-test the Legacy or Additive layout:

```powershell
.\verification\Verify-Unity.ps1 -Layout Legacy -Build -Smoke
```

Use `-Layout Additive` to select the Additive layout. For desktop Editor testing, use **Paridhwani > Play Isolated Test Visit** or **Run Automated Editor Playtest**. To build and test a fresh standalone player from the open Editor, use **Paridhwani > Build and Run Standalone Playtest**.

See [current production status](docs/PRODUCTION_STATUS.md) for verified results and remaining acceptance work. For the project plan, see [Unity game plan](docs/UNITY_GAME_PLAN.md).

## NLP integration

Dialogue currently uses local authored content. The project includes an optional cancellable text-service adapter, but no real NLP endpoint, authentication, or transport is configured. See the [proposed service contract](docs/NLP_SERVICE_CONTRACT_PROPOSAL.md).

Service attachment/detachment uses SetOptionalDialogueService on the Unity thread. Switching providers invalidates old replies; cancellation and shutdown contain transport callback failures while preserving the authored conversation.

Chapter practice returns to its title, Pause or credits origin. Settings and chapter lists keep Back visible while scrolling, and live text scaling updates panel width and height. These changes have source coverage; their new native focus/layout assertions remain pending.

Escape at the welcome screen returns to title until arrival is explicitly acknowledged. Failed save-and-exit attempts offer a retry and preserve the originating title/settings/Pause screen when cancelled. These flows have source checks; their native smoke assertions are pending.

## Presentation demo

The title's **Presentation demo / choose a scene** opens tea, river, trunk/deed or council scenes with prepared prior clues in separate temporary visits. Pause can switch demo scenes or return to the main visit. Demo progress does not unlock main-story chapters. Settings includes Display brightness for the presentation screen. See the [demo rehearsal guide](docs/DEMO_RUNBOOK.md). These additions have source coverage; fresh native layout/brightness/demo acceptance remains pending.

Demo arrivals include a four-step route and the current objective; Pause can reopen the guide. World signs now fit text to their board proportions and share font atlases. Source checks pass; native acceptance of the new sign layout and guide remains pending.
