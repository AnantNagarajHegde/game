# Paridhwani

Paridhwani is a story-led Kannada-learning adventure built in Unity. The prototype follows an archive worker investigating Ananya's disappearance and helping protect the village library. Players explore the village, speak with its residents, collect evidence, and practise Kannada through authored dialogue and activities.

The main story is authored from arrival through the council review and epilogue, with a separate practice visit that preserves the completed story save. The game is still a procedural prototype, not a release-ready product; final art, fluent language review, a connected NLP service, and broader player acceptance testing remain outstanding.

## Open and play

1. Open `My project (1)` in Unity Hub using **Unity 6000.6.7f1**.
2. Open `Assets/_Paridhwani/Scenes/TeaStall_Benchmark.unity`.
3. Press **Play**.

If generated assets are missing, use **Paridhwani > Create Tea Stall Benchmark** in the Editor. For controls, story walkthrough, and save details, see [play and build instructions](docs/TEA_STALL_BENCHMARK.md).

## Verification

The latest recorded native Editor regression passed on 28 October 2026, covering the main story through its epilogue and isolated practice visit. Runtime/editor source compilation and 287 standalone assertions also pass. These checks do **not** constitute a fresh standalone Player build or full human acceptance test.

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
