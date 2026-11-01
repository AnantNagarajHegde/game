"""Prepare locally downloaded licensed recordings. Dependencies: numpy, scipy, soundfile.
Source URLs/credits and source hashes are recorded in docs/AUDIO_ASSETS.json.
Raw downloads live in ignored verification/Temp/AudioSources; no runtime networking.
"""
from pathlib import Path
import hashlib, json, sys, uuid, subprocess
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT / 'verification/Temp/AudioSources'
sys.path.insert(0, str(RAW / 'pythonlibs'))
import soundfile as sf
import imageio_ffmpeg
from scipy.signal import butter, sosfilt, resample_poly
from math import gcd

OUT = ROOT / 'My project (1)/Assets/_Paridhwani/Resources/VillageAudio'
OUT.mkdir(parents=True, exist_ok=True)
rows = []

def asset(name, source, author, page, license='CC0-1.0', loop=False, mono=False, start=0, length=None, rms=-22):
    source = Path(source)
    x, rate = sf.read(source, dtype='float32', always_2d=True)
    original = {'rate': rate, 'channels': x.shape[1], 'seconds': len(x)/rate}
    x = x[int(start*rate):int((start+length)*rate) if length else None]
    if mono: x = x.mean(axis=1, keepdims=True)
    x = sosfilt(butter(2, 45, 'highpass', fs=rate, output='sos'), x, axis=0)
    if rate != 44100:
        divisor = gcd(rate, 44100)
        x = resample_poly(x, 44100//divisor, rate//divisor, axis=0)
    rate = 44100
    if not loop:
        # Remove excess lead/trailing silence while retaining the natural decay.
        active = np.flatnonzero(np.max(np.abs(x), axis=1) > max(.0002, float(np.max(np.abs(x)))*.002))
        if len(active): x = x[max(0,active[0]-int(rate*.008)):min(len(x),active[-1]+int(rate*.06))]
    target = 10**(rms/20)
    level = float(np.sqrt(np.mean(x*x)))
    x *= min(target/max(level,1e-9), .84/max(float(np.max(np.abs(x))),1e-9))
    # Tiny edge ramps avoid start/stop clicks. Long loops overlap at runtime.
    n = min(int(rate*(.02 if loop else .003)), len(x)//4)
    x[:n] *= np.linspace(0,1,n)[:,None]
    x[-n:] *= np.linspace(1,0,n)[:,None]
    path = OUT / (name+'.ogg')
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-v', 'error', '-y', '-f', 'f32le',
                    '-ar', str(rate), '-ac', str(x.shape[1]), '-i', 'pipe:0',
                    '-c:a', 'libvorbis', '-q:a', '6', str(path)],
                   input=np.asarray(x,dtype='<f4').tobytes(), check=True)
    # Validate decoded shipping file, including lossy-codec overshoot.
    decoded, actual_rate = sf.read(path, always_2d=True)
    peak = float(np.max(np.abs(decoded)))
    assert np.isfinite(decoded).all() and 0 < peak < 1, name
    assert len(decoded) > 100 and actual_rate == rate, name
    meta = Path(str(path)+'.meta')
    if not meta.exists():
        meta.write_text(f'''fileFormatVersion: 2
guid: {uuid.uuid4().hex}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: {1 if loop else 0}
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.85
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: {1 if mono else 0}
  normalize: 0
  loadInBackground: 1
  ambisonic: 0
  3D: 1
  userData:
  assetBundleName:
  assetBundleVariant:
''')
    rows.append(dict(asset=name, file=str(path.relative_to(ROOT)).replace('\\','/'), author=author,
                     page=page, license=license, source=source.name,
                     source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(), original=original,
                     processing=f'45 Hz high-pass, DC removal, crop {start}s/{length}s, level match, edge ramps; runtime overlap for loops',
                     loop=loop, rate=rate, channels=decoded.shape[1], seconds=round(len(decoded)/rate,3),
                     peak_dbfs=round(20*np.log10(peak),2), rms_dbfs=round(20*np.log10(np.sqrt(np.mean(decoded**2))),2),
                     sha256=hashlib.sha256(path.read_bytes()).hexdigest()))

asset('Rain', RAW/'rain-field.mp3', 'barkenov', 'https://freesound.org/s/640655/', loop=True, start=5, length=50, rms=-20)
asset('RoofRunoff', RAW/'eaves.mp3', 'Ogrebane', 'https://opengameart.org/content/rain-gutter-loop', loop=True, start=3, length=48, rms=-21)
asset('River', RAW/'river-field.mp3', 'saeedamirabadizadeh', 'https://freesound.org/s/749871/', loop=True, mono=True, start=5, length=60, rms=-20)
asset('Pour', RAW/'pour.mp3', 'Moonlight Dancer', 'https://freesound.org/s/213737/', mono=True, start=1, length=4, rms=-20)
asset('Bus', RAW/'bus.mp3', 'Akseli1236', 'https://freesound.org/s/768261/', loop=True, mono=True, start=.3, length=4.4, rms=-24)
asset('Auto', RAW/'auto.mp3', 'ambar das', 'https://freesound.org/s/203177/', loop=True, mono=True, start=15, length=30, rms=-25)
asset('Boiler', RAW/'boil.mp3', 'xenognosis', 'https://freesound.org/s/137229/', loop=True, mono=True, start=1, length=10, rms=-25)

impact = RAW/'impact/Audio'
if not impact.exists(): impact = next((RAW/'impact').rglob('footstep_grass_000.ogg')).parent
for surface, original in [('Earth','grass'),('Stone','concrete'),('Timber','wood')]:
    for i in range(5):
        asset(f'{surface}{i}', impact/f'footstep_{original}_{i:03}.ogg', 'Kenney', 'https://kenney.nl/assets/impact-sounds', mono=True, rms=-23)
asset('TempleBell', impact/'impactBell_heavy_001.ogg', 'Kenney', 'https://kenney.nl/assets/impact-sounds', mono=True, rms=-25)
asset('Cup', impact/'impactGlass_light_000.ogg', 'Kenney', 'https://kenney.nl/assets/impact-sounds', mono=True, rms=-25)
rpg = RAW/'rpg/Audio'
for name, file in [('NotebookOpen','bookOpen'),('NotebookClose','bookClose'),('Paper','bookFlip2'),('Door','doorOpen_1'),('Latch','metalLatch'),('Utensil','metalPot1')]:
    asset(name,rpg/(file+'.ogg'),'Kenney','https://kenney.nl/assets/rpg-audio',mono=True,rms=-25)
wildlife = RAW/'wildlife/amb_bird_cricket_frog'
for name, original in [('Bird','amb_bird_1'),('Cricket','amb_cricket_1'),('Frog','amb_frog_1')]:
    asset(name,wildlife/(original+'.flac'),'Blender Foundation / Yo Frankie!','https://opengameart.org/content/ambient-bird-cricket-and-frog','CC-BY-3.0',mono=True,rms=-25)

manifest = ROOT/'docs/AUDIO_ASSETS.json'
manifest.write_text(json.dumps({'note':'Recorded replacements and licensed foley. Freesound assets use publicly available HQ MP3 derivatives, not original lossless WAV downloads. No regional wildlife authenticity or native mix acceptance is claimed.', 'assets':rows},indent=2), encoding='utf-8')
print(f'AUDIO_ASSETS_OK: {len(rows)} decoded shipping clips, finite samples, no sample clipping')
print(f'Shipping OGG size: {sum(p.stat().st_size for p in OUT.glob("*.ogg"))/1024/1024:.2f} MiB')
