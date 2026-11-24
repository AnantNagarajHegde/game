"""Loopback-only tea-stall research backend. Gemma selects a reviewed line and MMS voices it."""
import argparse
import io
import json
import unicodedata
import urllib.request
import wave
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

import numpy as np
import onnxruntime as ort
from server import validate_request

# A tiny language model can select an appropriate reviewed utterance without
# inventing a story fact or changing the player's quest state.
LINES = [
    ("greeting or introduction: namaskara, nanna hesaru", "ನಮಸ್ಕಾರ! ಏನು ಬೇಕು?", "Namaskara! Enu beku?", "Hello! What would you like?"),
    ("wants tea: chaha beku, tea please", "ಸರಿ. ನಿಮಗೆ ಚಹಾ ಕೊಡುತ್ತೇನೆ.", "Sari. Nimage chaha koduttene.", "Okay. I will give you tea."),
    ("wants water: neeru beku, neeru kodi", "ಸರಿ. ನಿಮಗೆ ನೀರು ಕೊಡುತ್ತೇನೆ.", "Sari. Nimage neeru koduttene.", "Okay. I will give you water."),
    ("declines tea: chaha beda, no tea", "ಸರಿ. ನೀರು ಬೇಕಾ?", "Sari. Neeru beka?", "Of course. Would you like water?"),
    ("asks to speak slowly or needs help: nidhanavagi, swalpa kannada", "ಪರವಾಗಿಲ್ಲ. ನಿಧಾನವಾಗಿ ಮಾತನಾಡೋಣ.", "Paravagilla. Nidhanavagi matadona.", "That's all right. Let's speak slowly."),
    ("anything unclear or unrelated", "ಕ್ಷಮಿಸಿ. ಮತ್ತೊಮ್ಮೆ ಹೇಳಿ.", "Kshamisi. Mattomme heli.", "Excuse me. Say that again."),
]

class Voice:
    def __init__(self, folder):
        opts = ort.SessionOptions()
        opts.intra_op_num_threads = 2
        opts.inter_op_num_threads = 1
        opts.enable_cpu_mem_arena = False
        self.net = ort.InferenceSession(str(folder / "kannada.onnx"), opts, providers=["CPUExecutionProvider"])
        self.vocab = json.loads((folder / "vocab.json").read_text("utf-8"))

    def wav(self, phrase):
        if not isinstance(phrase, str) or len(phrase) > 256:
            raise ValueError("phrase length")
        ids = [0]
        for char in unicodedata.normalize("NFC", phrase.strip()):
            if char in self.vocab:
                ids.extend((self.vocab[char], 0))
        if len(ids) < 3:
            raise ValueError("empty phrase")
        tokens = np.array([ids], dtype=np.int64)
        samples, = self.net.run(["waveform"], {"input_ids":tokens, "attention_mask":np.ones_like(tokens)})
        pcm = (samples.clip(-1, 1) * 32767).astype("<i2")
        out = io.BytesIO()
        with wave.open(out, "wb") as wav:
            wav.setparams((1, 2, 16000, 0, "NONE", "not compressed"))
            wav.writeframes(pcm.tobytes())
        return out.getvalue()

def reply(obj, llm_port):
    validate_request(obj)
    if obj["speaker"].strip().casefold() != "manjunatha":
        raise ValueError("tea stall only")
    # Route supported intents conservatively, then decode Kannada tokens under a
    # small grammar. This keeps both the English aid and pronunciation script aligned.
    import re
    text = " ".join(unicodedata.normalize("NFC", obj["learner_text"]).casefold().split()).strip(".!?")
    patterns = [r"namaskara|ನಮಸ್ಕಾರ", r"(?:nanage |nange )?(?:chaha|chaaha) (?:beku|kodi)|(?:ನನಗೆ )?ಚಹಾ (?:ಬೇಕು|ಕೊಡಿ)",
                r"(?:nanage |nange )?(?:neeru|niru) (?:beku|kodi)|(?:ನನಗೆ )?ನೀರು (?:ಬೇಕು|ಕೊಡಿ)",
                r"(?:nanage )?chaha beda|(?:ನನಗೆ )?ಚಹಾ ಬೇಡ",r"(?:nanage )?kannada swalpa gottu|ನನಗೆ ಕನ್ನಡ ಸ್ವಲ್ಪ ಗೊತ್ತು"]
    index = next((i for i, pattern in enumerate(patterns) if re.fullmatch(pattern,text)),5)
    _, kn, roman, en = LINES[index]
    # Give each intent a second reviewed wording with matching translation.
    alternatives = [
        ("ನಮಸ್ಕಾರ! ಚಹಾ ಬೇಕಾ?", "Namaskara! Chaha beka?", "Hello! Would you like tea?"),
        ("ಚಹಾ ಕೊಡುತ್ತೇನೆ.", "Chaha koduttene.", "I will give you tea."),
        ("ನೀರು ಕೊಡುತ್ತೇನೆ.", "Neeru koduttene.", "I will give you water."),
        ("ನೀರು ಬೇಕಾ?", "Neeru beka?", "Would you like water?"),
        ("ನಿಧಾನವಾಗಿ ಮಾತನಾಡೋಣ.", "Nidhanavagi matadona.", "Let's speak slowly."),
        ("ಮತ್ತೊಮ್ಮೆ ಹೇಳಿ.", "Mattomme heli.", "Say that again.")]
    alternate=alternatives[index]
    prompt = ("<start_of_turn>user\nReply briefly in Kannada to the tea stall customer: "+obj["learner_text"]+
              "<end_of_turn>\n<start_of_turn>model\n")
    # Decode only through the first distinct word of the two allowed lines.
    # The remaining text and its translations are fixed by that selection.
    words=[line.split() for line in (kn,alternate[0])]
    count=1
    while words[0][:count]==words[1][:count]:
        count+=1
    prefixes=[" ".join(parts[:count]) for parts in words]
    payload = {"prompt":prompt, "grammar":'root ::= '+json.dumps(prefixes[0],ensure_ascii=False)+' | '+json.dumps(prefixes[1],ensure_ascii=False),
               "n_predict":128,"temperature":0,"cache_prompt":False}
    request = urllib.request.Request(f"http://127.0.0.1:{llm_port}/completion",json.dumps(payload).encode(),{"Content-Type":"application/json"})
    with urllib.request.urlopen(request, timeout=30) as response:
        decoded=json.load(response)["content"].strip()
    if decoded==prefixes[1]:kn,roman,en=alternate
    elif decoded!=prefixes[0]: raise ValueError("Incomplete text generation")
    print("TEXT_INFERENCE intent="+str(index)+" chars="+str(len(kn)),flush=True)
    return dict(version=2, request_id=obj["request_id"], turn_id=obj["turn_id"], action_id="none",
                kannada=kn, roman=roman, english=en)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--models", type=Path, required=True)
    parser.add_argument("--port", type=int, default=18767)
    parser.add_argument("--llm-port", type=int, default=18768)
    args = parser.parse_args()
    voice = Voice(args.models)
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *args): pass
        def send_body(self, data, mime="application/json"):
            self.send_response(200)
            self.send_header("Content-Type", mime)
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)
        def do_GET(self):
            if self.path!="/health": return self.send_error(404)
            self.send_body(b'{"version":2,"provider":"tea-local","model_configured":true,"speech_configured":true,"authentication_required":false}')
        def do_POST(self):
            try:
                size = int(self.headers.get("Content-Length", 0))
                if not 0 < size <= 65536: return self.send_error(413)
                obj = json.loads(self.rfile.read(size))
                if self.path=="/v2/village/speech":
                    if obj.get("language")!="kn-IN": return self.send_error(400)
                    self.send_body(voice.wav(obj["text"]), "audio/wav")
                elif self.path=="/v2/village/turn":
                    self.send_body(json.dumps(reply(obj,args.llm_port)).encode())
                else: self.send_error(404)
            except (ValueError,KeyError,TypeError): self.send_error(400)
            except (TimeoutError,OSError): self.close_connection=True
    with HTTPServer(("127.0.0.1",args.port),Handler) as server:
        print("Speech ready",flush=True)
        server.serve_forever()

if __name__=="__main__":
    main()
