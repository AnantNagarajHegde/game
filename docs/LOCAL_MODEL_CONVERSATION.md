# Local model conversations

The village's typed NPC conversations can use a local Ollama model. Unity sends
the player's text, current NPC, objective, visible authored line, available
replies, the current story goal and up to six recent exchanges with that NPC to a local Python bridge.
The bridge supplies a short character profile and asks the model for an NPC
reply and, separately, a matching available action. The model is not fine-tuned
on the story and does not receive the entire story or unrevealed clues.

The model may propose an action. Unity checks that it belongs to the current
turn and provider, then applies it automatically through the existing authored
story transition. Authored replies are sent as context to the evaluator, but are
not displayed as conversation buttons. For Manjunatha's initial greeting, the typed objective
asks the player to introduce themselves by name. Other goals are the game's
existing story objectives; they are not yet a full lesson system.

## Run locally on Windows

1. Install and run Ollama and make sure a suitable local model is already
   available. The default is `qwen2.5:3b`; its Kannada ability and task judgments
   must be evaluated before relying on it. You may select a different installed
   model with the command below. No model is bundled or downloaded by the game.
2. From the `game` directory, start Ollama and the bridge using the launcher:

   ```powershell
   .\Start-VillageConversation.ps1
   ```

   The launcher checks that Python, Ollama and the selected model are available.
   For a different installed model, pass `-Model model-name`. It does not download models.

3. Keep the bridge and Ollama running on the same machine as Unity or the Windows
   build. Enter Play mode and speak to a character. Type a message and choose
   **Send**. If the service matches it to an action, the story advances automatically;
   otherwise the NPC replies while the current task remains open. If the service
   is unavailable, the draft stays in the composer so it can be resent after the
   service starts. Escape leaves the conversation.

The bridge binds `127.0.0.1:8767` and calls Ollama at `127.0.0.1:11434`.
It does not launch either service automatically or log player messages. Stop it
with Ctrl+C. The port option exists for development, but Unity currently expects
8767. Conversation history stays in the Unity session; it is not saved.

## Limitations and acceptance

The model's understanding, Kannada, refusal of prompt injection, spoiler
avoidance and action suggestions are probabilistic. An incorrectly suggested action
can advance the wrong authored branch automatically. The local service is required
for typed NPC dialogue and story advancement; there is currently no offline fallback. Current
grounding is the NPC profile, current line, available choices and recent chat;
there is no complete reviewed story knowledge base or per-quest assessment rubric.
No automatic correctness or learning credit is recorded. Check a real model
with fluent Kannada speakers and run native Unity/Player, controller, offline,
timeout and stale-reply tests before presenting this as validated.

Run the bridge's socket-free unit tests from the `village_backend` directory:

```powershell
python -B -m unittest -v test_server
```
