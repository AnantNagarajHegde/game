# Unity / separate language-engine boundary

The full story runs through authored local conversation providers. Unity owns turn progression, permitted choices, notebook evidence, scene state and save consequences. The separate language engine remains external.

## Optional text adapter

`TeaStallSession.OptionalDialogueText` accepts an injected delegate taking a `DialoguePrompt`, cancellation token and returning a `Task<TeaLine>`. It is a Unity-side adapter surface, not an HTTP/service protocol or a new language engine. No endpoint, backend model, key or transport is configured or shipped.

A prompt contains copied current-turn text, copied available reply text, speaker/turn identity and bounded learner input. It exposes no quest-effect commands or full hidden story state. A reply supplies text only. Authored reply selection remains responsible for all consequences. Recognised intents and any future backend API must be agreed with the engine team before replacing that flow.

When no adapter is installed, the free-text controls are hidden and the entire authored story remains playable. When configured, the UI offers free text, a thinking state, cancellation and retry/fallback through the supported replies. Requests time out, observe late task faults and ignore replies after exit, notebook/activity changes or a newer turn/request. Input/reply lengths are bounded. A displayed reply never invokes a quest command.

## Checks

Standalone tests cover missing/failing services, null/oversized replies, timeout with a non-cooperative task, stale replies after cancellation, and copied authored choices. They simulate transport behavior without network calls. Unity UI/lifecycle tests and the actual service integration remain pending.

The integration team must still supply the real contract, allowed-fact policy, service availability, transport implementation and reviewed language behavior. This adapter does not claim NLP integration is complete.
