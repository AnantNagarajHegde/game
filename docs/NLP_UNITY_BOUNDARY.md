# Unity / separate language-engine boundary

The full story runs through authored local conversation providers. Unity owns turn progression, permitted choices, notebook evidence, scene state and save consequences. The separate language engine remains external.

## Optional text adapter

`TeaStallSession.OptionalDialogueText` accepts an injected delegate taking a `DialoguePrompt`, cancellation token and returning a `Task<TeaLine>`. It is a Unity-side adapter surface, not an HTTP/service protocol or a new language engine. No endpoint, backend model, key or transport is configured or shipped.

A prompt contains copied current-turn text, copied available reply text, speaker/turn identity and bounded learner input. It exposes no quest-effect commands or full hidden story state. A reply supplies text only. Authored reply selection remains responsible for all consequences. Recognised intents and any future backend API must be agreed with the engine team before replacing that flow.

When no adapter is installed, the free-text controls are hidden and the entire authored story remains playable. When configured, the UI offers free text, a thinking state, cancellation and retry/fallback through the supported replies. Requests time out, observe late task faults and ignore replies after exit, notebook/activity changes or a newer turn/request. Input/reply lengths are bounded. A displayed reply never invokes a quest command. Optional text is rendered literally with rich-text interpretation disabled. The Original line action restores the authored line without choosing a reply or changing story facts. Draft input clears on a new turn; same-turn refreshes retain the current input and reading position. Long speech and choices scroll beneath fixed leave/notebook controls.

## Typed service boundary

Configure an IDialogueTextService on the Unity thread:

```csharp
session.SetOptionalDialogueService(service);
// Disconnect and return to the authored conversation:
session.SetOptionalDialogueService(null);
```

DialogueServiceAdapter.Bind(service) also remains available for direct delegate integration. Setting or replacing OptionalDialogueText invalidates pending work and clears the previous optional reply. These are configuration operations, not per-frame calls. The service owner retains responsibility for disposing its connection/client; the session only cancels its own requests.

The service implementation is supplied by the integration team. No implementation, endpoint or credential is installed by this change. DialogueServiceRequest and DialogueServiceReply are immutable in-process C# types, not JsonUtility DTOs or an assertion about the external engine's actual schema. A transport must map the agreed wire contract to them.

Each request has version 1, a unique request ID, turn ID, speaker display name, bounded learner input and immutable copies of the current authored line/replies. The collection cannot be modified by a service. Responses must echo the exact request ID, turn ID and version, and provide nonempty bounded text. Invalid responses become the existing unavailable/fallback state. Cancellation is checked before calling the service and after its response; the same token reaches the transport. Existing request management still applies its deadline and discards superseded work.

Validated legacy delegate responses are now copied before presentation too, so a provider cannot mutate its returned TeaLine after validation. Neither service interface has quest commands, intent application, save mutation, or hidden story state.

See [NLP service contract proposal](NLP_SERVICE_CONTRACT_PROPOSAL.md) for the recommended HTTP/JSON mapping and open integration decisions. It is a proposal, not a deployed API.

## Cancellation and replacement lifecycle

Request state is updated before invoking transport cancellation callbacks, and callbacks execute outside the coordinator state lock. A throwing callback is contained and counted by CancellationCallbackFailures; it cannot escape as an exception into the gameplay UI. Reentrant callbacks may start another request without old cleanup clearing its busy state.

The local deadline has its own cancellation source. Successful completion stops that timer without signalling cancellation to the completed transport. Timeout, explicit cancel, incomplete setup and disposal still cancel transport work. A separate cancellation wake-up lets the game stop waiting even when a service ignores its token. Late task faults remain observed.

Every result carries its coordinator generation. The session checks that generation immediately before presentation, including already-completed responses queued for a later Unity continuation. Changing/detaching a provider, leaving a turn or destroying the session invalidates those results. Disposed coordinators cannot start new service calls. Attaching/detaching restores the authored line and refreshes free-text controls without choosing a reply or changing evidence.

## Checks

Standalone tests cover missing/failing services, null/oversized replies, timeout with a non-cooperative task, stale replies after cancellation, copied authored choices, immutable service context, unique IDs, response correlation/version validation, forwarding cancellation, callback failures, reentrant replacement, stale completed results, setup failure cleanup and shutdown. All runtime/editor sources compile and 287 assertions pass. They simulate transport behavior without network calls. The preceding native Editor playthrough exercised optional text presentation, literal text and large-response scrolling through the legacy delegate; the new typed service boundary and real transport still need in-engine/network acceptance.

The integration team must still supply the real contract, allowed-fact policy, service availability, transport implementation and reviewed language behavior. This adapter does not claim NLP integration is complete.

The cancellation-callback regression was reproduced against the previous implementation before the fix. Native smoke assertions now cover provider replacement/detachment with a delayed old reply and a throwing cancellation callback. They compile but have not been executed in a new Unity playthrough during this pass.
