"""Optional reading aids for the words already emitted by Manjunatha."""
import re

ROMAN = {
    "ಇನ್ನೊಮ್ಮೆ":"innomme", "ಎರಡು":"eradu", "ಏನು":"enu", "ಒಂದಕ್ಕೆ":"ondakke", "ಒಂದು":"ondu",
    "ಕಡಿಮೆ":"kadime", "ಕಪ್":"kap", "ಕಾಫಿ":"kaafi", "ಕೊಡುತ್ತೇನೆ":"koduttene", "ಕ್ಷಮಿಸಿ":"kshamisi",
    "ಚಹಾ":"chahaa", "ತರುತ್ತೇನೆ":"taruttene", "ನನ್ನ":"nanna", "ನಮಸ್ಕಾರ":"namaskaara",
    "ನಾಲ್ಕು":"naalku", "ನಿಧಾನವಾಗಿ":"nidhaanavaagi", "ನೀರು":"neeru", "ಪರವಾಗಿಲ್ಲ":"paravaagilla",
    "ಬನ್ನಿ":"banni", "ಬಿಸಿಯಾಗಿ":"bisiyaagi", "ಬೆಚ್ಚಗೆ":"bechchage", "ಬೇಕಾ":"bekaa", "ಬೇಕು":"beku",
    "ಮಂಜುನಾಥ":"Manjunaatha", "ಮತ್ತು":"mattu", "ಮತ್ತೆ":"matte", "ಮಾತನಾಡುತ್ತೇನೆ":"maatanaaduttene",
    "ಮೂರು":"mooru", "ಸಕ್ಕರೆ":"sakkare", "ಸರಿ":"sari", "ಸಿಗುತ್ತವೆ":"siguttave", "ಹಾಕುತ್ತೇನೆ":"haakuttene",
    "ಹಾಕುವುದಿಲ್ಲ":"haakuvudilla", "ಹೆಸರು":"hesaru", "ಹೇಳಿ":"heli",
}
NUMBER = {"ಒಂದು":"one","ಎರಡು":"two","ಮೂರು":"three","ನಾಲ್ಕು":"four"}
DRINK = {"ಚಹಾ":"tea","ಕಾಫಿ":"coffee"}
CLAUSES = {
    "ನಮಸ್ಕಾರ":"Hello!", "ಏನು ಬೇಕು":"What would you like?", "ನನ್ನ ಹೆಸರು ಮಂಜುನಾಥ":"My name is Manjunatha.",
    "ಪರವಾಗಿಲ್ಲ":"You're welcome.", "ಮತ್ತೆ ಬನ್ನಿ":"Come again.", "ನೀರು ಕೊಡುತ್ತೇನೆ":"I'll bring you water.",
    "ನಿಧಾನವಾಗಿ ಮಾತನಾಡುತ್ತೇನೆ":"I'll speak slowly.", "ನೀರು ಬೇಕಾ":"Would you like water?",
    "ಚಹಾ ಮತ್ತು ಕಾಫಿ ಸಿಗುತ್ತವೆ":"Tea and coffee are available.", "ಕ್ಷಮಿಸಿ, ಇನ್ನೊಮ್ಮೆ ಹೇಳಿ":"Excuse me, please say that again.",
    "ಸಕ್ಕರೆ ಹಾಕುವುದಿಲ್ಲ":"I'll leave out the sugar.", "ಸಕ್ಕರೆ ಕಡಿಮೆ ಹಾಕುತ್ತೇನೆ":"I'll use less sugar.",
    "ಒಂದಕ್ಕೆ ಸಕ್ಕರೆ ಹಾಕುವುದಿಲ್ಲ":"I'll leave the sugar out of one cup.", "ಬಿಸಿಯಾಗಿ ತರುತ್ತೇನೆ":"I'll bring it hot.",
    "ಬೆಚ್ಚಗೆ ತರುತ್ತೇನೆ":"I'll bring it warm.",
}


def reading_aids(text):
    roman_parts, english = [], []
    for sentence in re.findall(r"[^.!?]+[.!?]?",text):
        sentence=sentence.strip()
        syllables=[]
        for word in sentence.split():
            stem=word.rstrip(",.!?")
            if stem not in ROMAN: return "",""
            syllables.append(ROMAN[stem]+word[len(stem):])
        roman=" ".join(syllables)
        roman_parts.append(roman[:1].upper()+roman[1:])
        clause=sentence.rstrip(".!?")
        ok=clause.startswith("ಸರಿ, ")
        if ok: clause=clause[len("ಸರಿ, "):]
        parts=clause.split()
        if len(parts)==4 and parts[0] in NUMBER and parts[1]=="ಕಪ್" and parts[2] in DRINK and parts[3]=="ಕೊಡುತ್ತೇನೆ":
            cup="cup" if NUMBER[parts[0]]=="one" else "cups"
            meaning=f"I'll bring {NUMBER[parts[0]]} {cup} of {DRINK[parts[2]]}."
        else:
            meaning=CLAUSES.get(clause,"")
        english.append(("Okay. " if ok else "")+meaning if meaning else "")
    return " ".join(roman_parts)," ".join(english) if all(english) else ""
