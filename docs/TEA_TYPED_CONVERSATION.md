# Typed tea-stall conversation

All NPC conversations use typed messages through the local model bridge described
in [Local model conversations](LOCAL_MODEL_CONVERSATION.md). The visible authored
reply buttons have been removed. The model proposes a story action from the current
turn, and a valid current result advances the existing authored story automatically.
Investigation activities retain their own controls. Roman letters, English meaning
and the field notebook remain available. The prior authored tea-topic router and
separate assessment code are not the active typed-chat path.

## Keyboard and controller

Villager conversations open in a right-side chat panel. The transcript retains
typed messages and NPC responses for the current conversation,
including while visiting and returning from the notebook. The message composer sits
below the scrollable transcript, with language toggles and notebook controls underneath.
The transcript is session-only and starts fresh when you begin a new conversation.
The right stick scrolls it even while the controller keyboard is open. That keyboard
spans the bottom of the screen, and the chat panel becomes shorter above it; the
current draft remains visible in the chat composer.

With a physical keyboard, type a message and press **Enter** or choose **Send**.
With a controller, the QWERTY keyboard opens automatically at the bottom when a
villager conversation begins. Use the **left stick** to move through the character
grid and **A / Cross** to type. **RB / R1** inserts a space and **LB / L1** deletes
the previous character. **D-pad up** toggles capitals and **D-pad down** switches
between letters and digits/punctuation; D-pad left/right moves across a row. **X /
Square** sends, **B / Circle** hides the keyboard while keeping the draft, and X /
Square opens it again when hidden. The grid also includes Space, Backspace, Send and
Hide. The right stick scrolls the conversation while typing. Switching to a
physical keyboard hides the controller keyboard and returns focus to the text field.

The keyboard occupies the bottom of the screen and keeps the conversation visible above it.
While it is open, its own controls handle navigation; hide it to reach conversation controls.
Conversation/modal input suppresses world movement. Navigation is bounded at grid
edges and held-direction repeat is delayed. Hardware behavior and native focus
still require the acceptance checks below.

## Safe preview and fresh build

In Unity, after compilation finishes and while Play is stopped, use
**Paridhwani → Preview isolated tea practice**. It opens the drink question in a
temporary test visit, without sending a service request or loading the regular save.
Stop Play and repeat the preview for a fresh drink test. For the greeting and
before-ordering cases, use **Paridhwani → Play Isolated Test Visit**, then approach
Manjunatha normally.

To build the currently saved benchmark scene without regenerating world/art, stop
Play and use **Paridhwani → Build current tea assessment snapshot**. A successful
build reports its exact output in the Unity Console:

```text
My project (1)/Builds/TeaAssessment-<UTC yyyyMMdd-HHmmss>/Paridhwani.exe
```

Use that newly reported build for standalone testing. The old `Builds/TeaStall`
executable is not updated by this command. A regular standalone launch uses the
usual save; use the isolated Editor preview for save-safe acceptance.

## Manual acceptance

1. Start the bridge and model, then introduce yourself to Manjunatha by name.
   Check that the model replies and the greeting step advances automatically.
2. Send a tea or water request, then ask for the library. Verify the matching
   story effects, and check that unrelated or negated requests do not advance it.
3. Stop the bridge and send a message. Check that the draft remains available and
   an unavailable-service message appears rather than an invented NPC reply.
4. Enter tea conversation with a controller and confirm the keyboard appears automatically.
   Move the stick one column at a time, exercise LB/RB, caps, symbols and X to send.
   Check the reply, hide a new draft with B, then reopen it with X and confirm it
   survives. Check stick edge/repeat behavior, right-stick scrolling, focus restoration,
   keyboard/mouse takeover and no character movement.
5. Toggle Roman/English, use the notebook, and return. Check that feedback remains
   readable and a cancelled request cannot overwrite a newer reply.
6. Try Romanised Kannada phrases such as `Nanage chaha beku` and `Nanage neeru beku`
   with the actual model. Record its results rather than assuming a language score or
   deterministic classification; also check that a coffee request does not serve tea.

The existing loopback access denial has not been bypassed. Real socket exchange,
native visible submission, controller hardware/focus, and a fresh standalone
playthrough remain acceptance items until recorded as verified.

## Repeatable source checks

From the `game` directory:

```powershell
& .\verification\Verify-TeaConversation.ps1
```

This checks the applied runtime, Editor and Player source branches, the existing
game/dialogue suites, and `TeaTopicChecks`, `TeaKeyboardChecks` and
`TeaAssessmentChecks`. It regenerates synthetic fixtures through the real local
evaluator and uses an in-memory HTTP handler. It opens no sockets, launches no
game, and sends no native controller input. Passing these checks does not establish
native or live-network acceptance.
