"""Encode the native Unity frames, Kannada voice, existing ambience and titles.

The game receives a movie with a Kannada soundtrack and a separate subtitle
timeline. A subtitled MP4 is also exported for viewing outside Unity.
"""
import argparse
import array
import json
from pathlib import Path
import shutil
import subprocess
import textwrap
import wave

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / "verification/Temp/ArrivalMovie"


def ass_time(seconds):
    cs = round(seconds * 100)
    return f"{cs // 360000}:{cs // 6000 % 60:02}:{cs // 100 % 60:02}.{cs % 100:02}"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    ffmpeg = str(Path(args.ffmpeg).resolve())
    timeline = json.loads((ROOT / "My project (1)/Assets/_Paridhwani/Resources/ArrivalIntro/MovieTimeline.json").read_text(encoding="utf-8-sig"))
    duration = timeline["duration"]
    samples = array.array("h", [0]) * round(duration * 24000)
    for cue in timeline["cues"]:
        with wave.open(str(CACHE / (cue["id"] + "-kn.wav")), "rb") as source:
            assert source.getframerate() == 24000 and source.getnchannels() == 1
            segment = array.array("h", source.readframes(source.getnframes()))
        start = round((cue["start"] + .35) * 24000)
        assert start + len(segment) <= round(cue["end"] * 24000)
        samples[start:start + len(segment)] = segment
    with wave.open(str(CACHE / "narration.wav"), "wb") as output:
        output.setnchannels(1); output.setsampwidth(2); output.setframerate(24000); output.writeframes(samples.tobytes())

    fonts = CACHE / "fonts"; fonts.mkdir(exist_ok=True)
    for name in ("NotoSans-Regular.ttf", "NotoSansKannada-Regular.ttf"):
        shutil.copy2(ROOT / "My project (1)/Assets/_Paridhwani/Art/Fonts" / name, fonts / name)
    header = """[Script Info]
ScriptType: v4.00+
PlayResX: 1920
PlayResY: 1080
WrapStyle: 0
ScaledBorderAndShadow: yes
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Caption,Noto Sans,38,&H00DFEDF2,&H00DFEDF2,&H00131819,&H9A131819,0,0,0,0,100,100,0,0,3,12,0,2,170,170,120,1
Style: Title,Noto Sans,66,&H00DFEDF2,&H00DFEDF2,&H00131819,&H00131819,0,0,0,0,100,100,9,0,1,1,1,5,100,100,0,1
Style: Small,Noto Sans,24,&H0079B5D5,&H0079B5D5,&H00131819,&H00131819,0,0,0,0,100,100,3,0,1,1,1,5,100,100,0,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""
    def line(start, end, style, text):
        return f"Dialogue: 0,{ass_time(start)},{ass_time(end)},{style},,0,0,0,,{text}\n"
    titles = header
    titles += line(.6, 4.6, "Small", r"{\pos(960,235)\fad(600,600)}KALYANAPURA / WEDNESDAY MORNING")
    titles += line(.6, 4.6, "Title", r"{\pos(960,305)\fad(600,600)}PARIDHWANI")
    fifth = timeline["cues"][4]
    titles += line(fifth["start"]+1, fifth["end"]-1, "Small", r"{\pos(960,180)\fad(400,400)}NAMASKARA / HELLO")
    titles += line(duration-2.8, duration, "Title", r"{\pos(960,470)\fad(600,500)}PARIDHWANI")
    titles += line(duration-2.8, duration, "Small", r"{\pos(960,555)\fad(600,500)}YOUR STORY BEGINS WITH A CONVERSATION")
    (CACHE / "titles.ass").write_text(titles, encoding="utf-8")
    subtitles = header
    for cue in timeline["cues"]:
        wrapped = r"\N".join(textwrap.wrap(cue["caption"], width=74))
        subtitles += line(cue["start"]+.3, cue["end"]-.2, "Caption", r"{\fad(160,160)}"+wrapped)
    (CACHE / "captions.ass").write_text(subtitles, encoding="utf-8")

    def run(*command):
        subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "warning", "-y", *map(str, command)], cwd=CACHE, check=True)
    rain = ROOT / "My project (1)/Assets/_Paridhwani/Resources/VillageAudio/Rain.ogg"
    bus = rain.with_name("Bus.ogg")
    boiler = rain.with_name("Boiler.ogg")
    bus_end = timeline["cues"][1]["end"]
    tea_start = timeline["cues"][5]["start"]
    run("-i", "narration.wav", "-stream_loop", "-1", "-i", rain, "-stream_loop", "-1", "-i", bus, "-stream_loop", "-1", "-i", boiler,
        "-filter_complex", f"[0:a]highpass=f=75,loudnorm=I=-18:TP=-2:LRA=9,aformat=sample_rates=48000:channel_layouts=stereo[voice];[1:a]volume=0.14,afade=t=in:d=1,afade=t=out:st={duration-3}:d=3[rain];[2:a]volume='if(lt(t,{bus_end}),0.12,0)':eval=frame,afade=t=out:st={bus_end-2}:d=2[bus];[3:a]volume='if(gte(t,{tea_start}),0.09,0)':eval=frame[boiler];[voice][rain][bus][boiler]amix=inputs=4:duration=first:normalize=0,alimiter=limit=0.89[mix]",
        "-map", "[mix]", "-t", duration, "-c:a", "aac", "-b:a", "192k", "soundtrack.m4a")
    print("KANNADA_SOUNDTRACK_ENCODED", flush=True)
    # Baseline/no B frames gives Windows Media Foundation monotonic timestamps.
    # Convert the full-range JPEG frames and declare an explicit HD colour space.
    movie_filter = f"scale=in_range=pc:out_range=tv:out_color_matrix=bt709,format=yuv420p,drawbox=x=0:y=0:w=iw:h=60:color=black:t=fill,drawbox=x=0:y=ih-60:w=iw:h=60:color=black:t=fill,fade=t=in:d=0.7,fade=t=out:st={duration-3}:d=3,ass=titles.ass:fontsdir=fonts,setparams=range=limited:color_primaries=bt709:color_trc=bt709:colorspace=bt709"
    video_options = ("-c:v", "libx264", "-preset", "fast", "-crf", "19", "-pix_fmt", "yuv420p", "-profile:v", "baseline", "-level:v", "4.1", "-bf", "0", "-color_range", "tv", "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709")
    run("-framerate", timeline["fps"], "-i", "Frames/frame-%05d.jpg", "-i", "soundtrack.m4a", "-vf", movie_filter, "-t", duration, *video_options, "-c:a", "copy", "-movflags", "+faststart", "arrival.mp4")
    destination = ROOT / "My project (1)/Assets/StreamingAssets/ArrivalMovie"
    destination.mkdir(parents=True, exist_ok=True)
    shutil.copy2(CACHE / "arrival.mp4", destination / "arrival.mp4")
    preview = ROOT / "docs/intro-movie"
    preview.mkdir(parents=True, exist_ok=True)
    run("-i", "arrival.mp4", "-vf", "ass=captions.ass:fontsdir=fonts", *video_options, "-c:a", "copy", "-movflags", "+faststart", preview / "Paridhwani-introduction.mp4")
    print(f"ARRIVAL_MOVIE_ENCODED: {duration:.2f}s, {timeline['width']}x{timeline['height']}, Kannada audio, English subtitle preview", flush=True)


if __name__ == "__main__":
    main()
