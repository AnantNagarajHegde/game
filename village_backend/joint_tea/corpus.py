"""Offline supervision for Manjunatha. The serving process never imports this file."""
import itertools

NUM = [("ಒಂದು","ondu","one"),("ಎರಡು","eradu","two"),("ಮೂರು","mooru","three"),("ನಾಲ್ಕು","naalku","four")]
DRINK = [("ಚಹಾ","chaha","tea"),("ಕಾಫಿ","kaafi","coffee")]
SUGARS = [
    ("","","",""),
    ("ಸಕ್ಕರೆ ಬೇಡ","sakkare beda","without sugar","ಸಕ್ಕರೆ ಹಾಕುವುದಿಲ್ಲ."),
    ("ಸಕ್ಕರೆ ಕಡಿಮೆ","sakkare kadime","less sugar","ಸಕ್ಕರೆ ಕಡಿಮೆ ಹಾಕುತ್ತೇನೆ."),
    ("ಒಂದಕ್ಕೆ ಸಕ್ಕರೆ ಬೇಡ","ondakke sakkare beda","one without sugar","ಒಂದಕ್ಕೆ ಸಕ್ಕರೆ ಹಾಕುವುದಿಲ್ಲ."),
]
TEMPS = [("","","",""),("ಬಿಸಿ","bisi","hot","ಬಿಸಿಯಾಗಿ ತರುತ್ತೇನೆ."),("ಉಗುರುಬೆಚ್ಚಗೆ","ugurubechchage","warm","ಬೆಚ್ಚಗೆ ತರುತ್ತೇನೆ.")]
# Every wording and complete reply for these semantic combinations stays out of training.
HELD = {(2,"tea",3,0),(3,"coffee",2,1),(4,"tea",1,2),(1,"coffee",0,1),(4,"coffee",3,2),(3,"tea",0,2)}
CASES = [
    ("ಎರಡು ಚಹಾ ಕೊಡಿ. ಒಂದಕ್ಕೆ ಸಕ್ಕರೆ ಬೇಡ.",(2,"tea",3,0)),
    ("three hot coffees please with less sugar",(3,"coffee",2,1)),
    ("naalku cup chaha ugurubechchage sakkare beda",(4,"tea",1,2)),
    ("ಒಂದು ಬಿಸಿ ಕಾಫಿ ಬೇಕು",(1,"coffee",0,1)),
    ("four warm coffees one without sugar",(4,"coffee",3,2)),
    ("ಮೂರು ಕಪ್ ಚಹಾ ಉಗುರುಬೆಚ್ಚಗೆ ಕೊಡಿ",(3,"tea",0,2)),
]
SMALL = [
    (("hello","namaskara","ನಮಸ್ಕಾರ","hi manjunatha"),"ನಮಸ್ಕಾರ! ಏನು ಬೇಕು?"),
    (("your name","nimma hesaru enu","ನಿಮ್ಮ ಹೆಸರು ಏನು","who are you"),"ನನ್ನ ಹೆಸರು ಮಂಜುನಾಥ."),
    (("thank you","thanks","dhanyavadagalu","ಧನ್ಯವಾದಗಳು"),"ಪರವಾಗಿಲ್ಲ. ಮತ್ತೆ ಬನ್ನಿ."),
    (("water please","neeru kodi","ನನಗೆ ನೀರು ಬೇಕು","ನೀರು ಕೊಡಿ"),"ಸರಿ, ನೀರು ಕೊಡುತ್ತೇನೆ."),
    (("please repeat","matte heli","ಮತ್ತೆ ಹೇಳಿ","please speak slowly","ನಿಧಾನವಾಗಿ ಹೇಳಿ"),"ಸರಿ, ನಿಧಾನವಾಗಿ ಮಾತನಾಡುತ್ತೇನೆ."),
    (("no tea","chaha beda","ಚಹಾ ಬೇಡ","ನನಗೆ ಚಹಾ ಬೇಡ"),"ಸರಿ, ನೀರು ಬೇಕಾ?"),
    (("what is available","enu sigutte","ಏನು ಸಿಗುತ್ತದೆ","menu"),"ಚಹಾ ಮತ್ತು ಕಾಫಿ ಸಿಗುತ್ತವೆ."),
    (("unknown","weather forecast","bus timetable","how far is the moon","ಬಸ್ಸು ಯಾವಾಗ ಬರುತ್ತದೆ","ನಾಳೆ ಮಳೆ ಬರುತ್ತದೆಯೇ"),"ಕ್ಷಮಿಸಿ, ಇನ್ನೊಮ್ಮೆ ಹೇಳಿ."),
]


def examples():
    train, testing = [], {}
    for count,drink,sugar,temp in itertools.product(range(1,5),DRINK,range(4),range(3)):
        if count == 1 and sugar == 3: continue
        k,r,e = NUM[count-1]
        d,rd,ed = drink
        s,sr,se,sa = SUGARS[sugar]
        t,tr,te,ta = TEMPS[temp]
        answer = " ".join(x for x in (f"ಸರಿ, {k} ಕಪ್ {d} ಕೊಡುತ್ತೇನೆ.",sa,ta) if x)
        key = (count,ed,sugar,temp)
        if key in HELD:
            testing[key] = answer
            continue
        plural = ed if count == 1 else ed+"s"
        prompts = [
            f"{k} {t} {d} ಕೊಡಿ {s}", f"ನನಗೆ {k} ಕಪ್ {d} ಬೇಕು {s} {t}", f"{s} {k} {d} {t} ಮಾಡಿ",
            f"{r} {tr} {rd} kodi {sr}", f"nanage {r} cup {rd} beku {sr} {tr}", f"{r} {rd} {sr} {tr}",
            f"{e} {te} {plural} {se}", f"please give me {e} cups of {ed} {se} {te}", f"{ed} {e} {se} {te} please",
            f"{count} {te} {ed} {se}", f"{count} {d} {s} {t}",
        ]
        if count==1:
            prompts += [f"ನನಗೆ {d} ಬೇಕು {s} {t}",f"nanage {rd} beku {sr} {tr}",f"{te} {ed} please {se}"]
        train.extend((" ".join(x.split()),answer) for x in prompts)
    for prompts,answer in SMALL:
        for prompt in prompts: train.extend([(prompt,answer)]*4)
    return train, [(prompt,testing[key]) for prompt,key in CASES]
