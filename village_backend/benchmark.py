"""Optional real-model smoke benchmark. Starts no server or model downloads."""
import argparse
import time

from server import evaluate, ollama_generate, openrouter_generate, groq_generate, provider_api_key


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--provider", choices=("ollama", "openrouter", "groq"), default="ollama")
    parser.add_argument("--model", default="")
    args = parser.parse_args()
    model = args.model or {"ollama": "gemma3:4b", "groq": "qwen/qwen3.8-27b"}.get(args.provider, "")
    key = provider_api_key(args.provider)
    if args.provider == "openrouter" and (not model or not key):
        parser.error("OpenRouter requires --model and OPENROUTER_API_KEY")
    if args.provider == "groq" and (not model or not key):
        parser.error("Groq requires a model and GROQ_API_KEY")
    cases = [
        ("chaha beku", "none"),
        ("neeru beku", "none"),
        ("Namaskara nanna hesaru Amogh", "a0"),
        ("Hello, my name is Meera", "none"),
        ("Hello, nanna hesaru Meera", "a0"),
        ("namaskara", "none"),
        ("I know only a little Kannada", "none"),
        ("please speak slowly", "none"),
        ("where is the library?", "none"),
    ]
    failures = 0
    for i, (message, expected) in enumerate(cases):
        data = {"version": 2, "request_id": f"{i:032x}", "event": "message", "turn_id": "hello",
                "speaker": "Manjunatha", "role": "Tea vendor", "learner_text": message,
                "objective": "Greet Manjunatha and introduce yourself by name.",
                "scene_fact": "Welcome the visitor and ask them to introduce themselves.", "history": [],
                "actions": [{"id": "a0", "description": "Greet Manjunatha and introduce yourself by providing your name.", "next_fact": "Offer tea or water."},
                            {"id": "a1", "description": "Explicitly ask Manjunatha to speak slowly or say you know only a little Kannada.", "next_fact": "Speak slowly."}]}
        started = time.monotonic()
        try:
            if args.provider == "openrouter":
                generate = lambda system, scene, schema: openrouter_generate(model, key, system, scene, schema)
            elif args.provider == "groq":
                generate = lambda system, scene, schema: groq_generate(model, key, system, scene, schema)
            else:
                generate = lambda system, scene, schema: ollama_generate(model, system, scene, schema)
            result = evaluate(data, generate)
            actual = result["action_id"]
            passed = actual == expected
            print(f"{'PASS' if passed else 'FAIL'} {message!r}: expected={expected}, actual={actual}, {time.monotonic()-started:.1f}s")
            print(f"  NPC (Romanised): {result['roman']}")
            print(f"  NPC (English): {result['english']}")
            if not passed:
                failures += 1
        except Exception as exc:
            failures += 1
            print(f"ERROR {message!r}: {type(exc).__name__}")
    print(f"{len(cases)-failures}/{len(cases)} action checks passed; review the actual Kannada replies separately.")
    raise SystemExit(1 if failures else 0)


if __name__ == "__main__":
    main()
