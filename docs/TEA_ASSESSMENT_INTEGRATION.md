# Production tea-order assessment wiring

The approved initial rules are implemented: accepted mapped tea/water requests
choose their existing authored reply; accepted-with-correction also advances and
keeps a local correction tip visible on the served turn. Input initially uses
Romanised Kannada. The explicitly authored mixed example `tea beku` is permitted;
other English sentences/mixes are not inferred from the broad `a_tea` label.

This is separate from optional generated dialogue and `/v1/dialogue/respond`.
The dialogue payload/response contract is unchanged. Both paths retain authored
replies, English/Roman display toggles and notebook assistance. Tea assessment
adds its own ordering hint/example and cancellation controls.

## Local setup (explicit, not automatically started)

From the language-engine `Paridhwani-NLP` folder, in an environment where normal
loopback use is permitted:

```powershell
python -B src/assessment_host.py --serve --port 8766
```

The host binds only literal `127.0.0.1`, route `/v1/assessment/evaluate`; it does
not change networking/security settings or log learner bodies. In Unity Play mode,
use **Paridhwani > Tea assessment > Attach local assessment (port 8766)**. No scene
or asset is modified by attachment. Detach explicitly or leave Play mode to dispose
the service. Player builds can inject `ITeaAssessmentService` on the Unity thread
using `SetTeaAssessmentService`; no implicit production endpoint/authentication is
configured. Do not start the host or retry network acceptance while an environment
denies loopback; this implementation does not bypass that restriction.

The game pins `TeaAssessmentContract.Revision` to
`3c1a09978760837bee0bf549e4be50edb2d8470ee381615906194ef7a69d3e0f`, the reviewed
assessment data snapshot. A changed snapshot fails closed until deliberately
reviewed and repinned. The host prints its revision on explicit startup.

## Ownership and supported scope

Only the actual tea provider's `order` turn after the authored greeting exposes
assessment input. The request uses curriculum `q1_2/q1_2_a`, a new request ID and a
copied bounded submission. Responses must match version, request, quest, step and
revision. The finite local text mapping revalidates the authored slot's text,
effect and next node before calling **TeaStallSession.Choose**. Existing provider
gates, ApplyState and Save retain authority; the server never sends effect commands.

`a_tea` also covers water and other items pedagogically. Coffee, milk and food
requests therefore never become a tea order. The player gets a scene-availability
message and keeps normal choices. The conductor-thanks lesson is not misused as
the game's greeting. Authored refusal/clarification remain available even when a
typed refusal is rejected by the ordering lesson.

Cancellation, notebook/activity/turn changes, original-line restoration, a newer
submission, provider replacement, session disposal or starting optional generated
text invalidates old assessment results. The existing request coordinator supplies
its eight-second deadline, observes late task faults and rejects stale generations;
transport adds a seven-second limit and bounded content/body parsing. Errors and
offline/unavailable responses leave story/save state and authored choices intact.
HTTP proxies, redirects, cookies, credentials, alternate hosts/routes and content
encodings are unavailable. HTTP failures are fixed diagnostics, not raw server text.

The Python host implementation is in the sibling language-engine working folder
at `Paridhwani-NLP/src/assessment_host.py`. Its required assessment/evaluator
dependencies remain that repository's own work; this game change does not commit
or absorb unrelated language-engine changes. Local hosting is not a remote service
deployment, and cancellation does not promise server-side inference interruption.

## Verification and remaining acceptance

`verification/Verify-TeaAssessment.ps1` generates real Python assessment fixtures,
compiles current runtime/Editor/Player-source branches, reruns the existing 405
chapter and 108 dialogue assertions, and executes 47 production assessment
codec/consumer/mapping/transport/coordinator assertions. Passing transport tests
use an injected in-memory HTTP handler and open **no sockets**. Raw fixture inputs
are synthetic learner examples, not private user data.

The separate Python runner passes 26 original test functions, 12 unittest methods,
all 178 held-out five-field comparisons, 210 authored cases with player context,
and 171 step-specific envelope round-trips. Labels were not changed. The preexisting
single training diagnostic ambiguity remains documented in language-engine.

**Not yet accepted live:** real socket host/client exchange, native Unity submit /
auto-choice / correction display / cancel / notebook / offline behavior, controller
focus/layout of the new controls, a fresh standalone Player build and fluent Kannada
review. The prior loopback AccessDenied was not retried or bypassed. Source and
in-memory success do not constitute a fully playable verified milestone. Use an
isolated test visit before native acceptance to preserve the user's actual save.
