#!/usr/bin/env python3
"""Exporte les données du jeu GemLab Simulator dans un seul fichier JSON (Docs/gemlab_data.json).

Sources lues (rien n'est modifié) :
  - GemLabSimulator/Assets/Data/Gems/*.asset         les pierres (ScriptableObjects Unity, YAML)
  - Assets/Scripts/Simulation/CrystalHabits.cs       les habitus cristallins et leur répartition par espèce
  - Assets/Scripts/Simulation/ToolInfo.cs            la description des instruments du laboratoire
  - Assets/Scripts/Simulation/BagItemCatalog.cs      l'équipement du sac
  - Assets/Editor/SceneBootstrap.cs                  les articles du GemStore (prix) et les postes du laboratoire
  - les catalogues de traitements (Bleaching, Dyeing, Coating, Filling, Diffusion, Irradiation, Laser)
  - BookCatalog.cs, OpeningHours.cs, StoryState.cs   les livres, les horaires, les règles d'ouverture des lieux

Utilisation :  python3 tools/export_game_data.py
"""
import json
import os
import re
import sys
from datetime import date

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "GemLabSimulator", "Assets")
SCRIPTS = os.path.join(ASSETS, "Scripts")
SIM = os.path.join(SCRIPTS, "Simulation")
OUT = os.path.join(ROOT, "Docs", "gemlab_data.json")

# ---------------------------------------------------------------------------------------------------------- lecture

def read(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


def unescape_double(raw):
    """Contenu d'une chaîne YAML entre guillemets (\\xE9, \\n, \\" ...)."""
    out = []
    i = 0
    while i < len(raw):
        c = raw[i]
        if c != "\\" or i + 1 >= len(raw):
            out.append(c)
            i += 1
            continue
        n = raw[i + 1]
        if n == "x":
            out.append(chr(int(raw[i + 2:i + 4], 16)))
            i += 4
        elif n == "u":
            out.append(chr(int(raw[i + 2:i + 6], 16)))
            i += 6
        elif n == "U":
            out.append(chr(int(raw[i + 2:i + 10], 16)))
            i += 10
        else:
            out.append({"n": "\n", "t": "\t", '"': '"', "\\": "\\", "/": "/", "0": "\0", " ": " ", "_": " "}.get(n, n))
            i += 2
    return "".join(out)


def scalar(text):
    text = text.strip()
    if text == "":
        return ""
    if text.startswith("{"):
        return parse_flow(text)
    if re.fullmatch(r"-?\d+", text):
        return int(text)
    if re.fullmatch(r"-?\d*\.\d+(e[-+]?\d+)?|-?\d+e[-+]?\d+", text, re.I):
        return float(text)
    return text


def parse_flow(text):
    """{r: 0.92, g: 0.94} -> dict (les références {fileID: ...} sont ignorées plus haut)."""
    inner = text.strip()[1:-1]
    result = {}
    for part in inner.split(","):
        if ":" not in part:
            continue
        key, value = part.split(":", 1)
        result[key.strip()] = scalar(value)
    return result


def parse_unity_asset(path):
    """Lit un ScriptableObject Unity sérialisé en YAML : renvoie un dict des champs de l'objet."""
    lines = read(path).splitlines()
    start = next(i for i, l in enumerate(lines) if l.startswith("MonoBehaviour:")) + 1
    data = {}
    key = None
    list_key = None
    item = None
    i = start
    while i < len(lines):
        line = lines[i]
        i += 1
        if not line.strip():
            continue
        top = re.match(r"^  ([A-Za-z_][\w]*):(?: (.*))?$", line)
        listed = re.match(r"^  - ([A-Za-z_][\w]*):(?: (.*))?$", line)
        sub = re.match(r"^    ([A-Za-z_][\w]*):(?: (.*))?$", line)
        if listed and list_key is not None:
            item = {}
            data[list_key].append(item)
            item[listed.group(1)] = read_value(listed.group(2) or "", lines, i)[0]
            i = read_value(listed.group(2) or "", lines, i)[1]
            continue
        if sub and item is not None and list_key is not None:
            value, i = read_value(sub.group(2) or "", lines, i)
            item[sub.group(1)] = value
            continue
        if top:
            key = top.group(1)
            raw = top.group(2) or ""
            if raw == "" and i < len(lines) and lines[i].startswith("  - "):
                list_key = key
                data[key] = []
                item = None
                continue
            list_key = None
            item = None
            value, i = read_value(raw, lines, i)
            data[key] = value
    return data


def read_value(raw, lines, i):
    """Valeur d'un champ : chaîne (éventuellement sur plusieurs lignes), nombre, couleur... Renvoie (valeur, ligne suivante)."""
    raw = raw.rstrip()
    if raw.startswith('"'):
        text = raw
        while not closes_double(text) and i < len(lines):
            text += " " + lines[i].strip()
            i += 1
        return unescape_double(text[1:text.rindex('"')]), i
    if raw.startswith("'"):
        text = raw
        while not (len(text) > 1 and text.endswith("'") and not text.endswith("''")) and i < len(lines):
            text += " " + lines[i].strip()
            i += 1
        return text[1:-1].replace("''", "'"), i
    # une chaîne simple peut continuer sur les lignes suivantes, plus indentées (4 espaces, sans "clé:" ni "- ")
    if raw and not raw.startswith("{"):
        while i < len(lines) and re.match(r"^    \S", lines[i]) and not re.match(r"^    [A-Za-z_]\w*:( |$)", lines[i]):
            raw += " " + lines[i].strip()
            i += 1
    return scalar(raw), i


def closes_double(text):
    if len(text) < 2 or not text.endswith('"'):
        return False
    backslashes = 0
    j = len(text) - 2
    while j >= 0 and text[j] == "\\":
        backslashes += 1
        j -= 1
    return backslashes % 2 == 0


# ---------------------------------------------------------------------------------------------------------- C#

CS_STRING = r'"((?:[^"\\]|\\.)*)"'


def cs_unescape(s):
    return s.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")


def cs_concat(text):
    """Une expression de chaînes C# collées avec + (ou un seul littéral)."""
    parts = re.findall(CS_STRING, text)
    return "".join(cs_unescape(p) for p in parts)


def block_end(text, start):
    """Index de l'accolade fermante qui correspond à l'accolade ouvrante en start (en ignorant les chaînes)."""
    depth = 0
    i = start
    in_string = False
    while i < len(text):
        c = text[i]
        if in_string:
            if c == "\\":
                i += 1
            elif c == '"':
                in_string = False
        else:
            if c == '"':
                in_string = True
            elif c == "{":
                depth += 1
            elif c == "}":
                depth -= 1
                if depth == 0:
                    return i
        i += 1
    return len(text) - 1


def parse_initializers(text, type_name):
    """Tous les `new Type { Cle = valeur, ... }` d'un texte : liste de dicts (chaînes, nombres et booléens seulement)."""
    results = []
    for match in re.finditer(r"new\s+" + re.escape(type_name) + r"\s*\{", text):
        open_index = text.index("{", match.start())
        close_index = block_end(text, open_index)
        body = text[open_index + 1:close_index]
        entry = {}
        # clé = "chaîne" (éventuellement concaténée), nombre, booléen
        for pair in re.finditer(r"(\w+)\s*=\s*((?:\"(?:[^\"\\]|\\.)*\"(?:\s*\+\s*\"(?:[^\"\\]|\\.)*\")*)|-?\d+(?:\.\d+)?f?|true|false)(?=\s*[,}\n]|\s*$)", body):
            key, raw = pair.group(1), pair.group(2)
            if raw.startswith('"'):
                entry[key] = cs_concat(raw)
            elif raw in ("true", "false"):
                entry[key] = raw == "true"
            else:
                number = raw.rstrip("f")
                entry[key] = float(number) if "." in number else int(number)
        results.append(entry)
    return results


# ---------------------------------------------------------------------------------------------------------- les sections

def export_species():
    folder = os.path.join(ASSETS, "Data", "Gems")
    bools = {"pleochroic", "isQuartzFamily", "isPolycrystalline", "isThermallyConductiveLikeDiamond", "isElectricallyConductive",
             "forcedSynthetic", "forcedTreatment", "isTunnelExclusive"}
    optical = {0: "Isotrope", 1: "Uniaxe", 2: "Biaxe", 3: "Opaque"}
    habits, definitions = export_habits()
    species = []
    for name in sorted(os.listdir(folder)):
        if not name.endswith(".asset"):
            continue
        raw = parse_unity_asset(os.path.join(folder, name))
        entry = {"id": name[:-6]}
        for key, value in raw.items():
            if key.startswith("m_") or key in ("icon", "roughIcon"):
                continue
            if key in bools:
                value = bool(value)
            if key == "opticalCharacterClass":
                entry["opticalCharacterClassName"] = optical.get(value, str(value))
            if key in ("iconColor", "iconFacetColor") and isinstance(value, dict):
                value = "#%02X%02X%02X" % tuple(int(round(float(value.get(c, 0)) * 255)) for c in "rgb")
            entry[key] = value
        entry["habits"] = habits.get(entry["id"], habits_for(entry, habits))
        species.append(entry)
    return species, definitions


def export_habits():
    text = read(os.path.join(SIM, "CrystalHabits.cs"))
    definitions = [
        {"id": h["Id"], "short": h.get("Short", ""), "name": h["Name"], "description": h.get("Description", "")}
        for h in parse_initializers(text, "CrystalHabit")
    ]
    # le tableau de répartition : case "X": ... return Chance(("id", poids), ...);
    mapping = {}
    pending = []
    for line in text.splitlines():
        case = re.match(r'\s*case "([^"]+)":\s*(.*)$', line)
        if case:
            pending.append(case.group(1))
            rest = case.group(2)
        else:
            rest = line
        chance = re.search(r"return Chance\((.*)\);", rest)
        if chance and pending:
            tuples = [{"habit": h, "weight": int(w)} for h, w in re.findall(r'\("([^"]+)",\s*(\d+)\)', chance.group(1))]
            for name in pending:
                mapping[name] = tuples
            pending = []
    # les règles par préfixe
    prefixes = []
    for match in re.finditer(r'name\.StartsWith\("([^"]+)"\)\)\s*return Chance\((.*?)\);', text):
        tuples = [{"habit": h, "weight": int(w)} for h, w in re.findall(r'\("([^"]+)",\s*(\d+)\)', match.group(2))]
        prefixes.append((match.group(1), tuples))
    quartz = re.search(r'var quartz = (.*?);', text)
    quartz_names = re.findall(r'name == "([^"]+)"', quartz.group(1)) if quartz else []
    exclusions = re.search(r'if \(quartz && (.*?)\) return', text)
    excluded = re.findall(r'name != "([^"]+)"', exclusions.group(1)) if exclusions else []
    mapping["__prefixes__"] = prefixes
    mapping["__quartz__"] = ([n for n in quartz_names if n not in excluded], excluded)
    return mapping, definitions


def habits_for(entry, mapping):
    for prefix, tuples in mapping["__prefixes__"]:
        if entry["id"].startswith(prefix):
            return tuples
    names, excluded = mapping["__quartz__"]
    if entry["id"] in names or (entry.get("isQuartzFamily") and entry["id"] not in excluded):
        return [{"habit": "quartz_prism", "weight": 100}]
    return []


def export_tools():
    info = read(os.path.join(SIM, "ToolInfo.cs"))
    instruments = [{"id": k, "description": cs_unescape(v)} for k, v in re.findall(r'\{\s*"(\w+)",\s*"((?:[^"\\]|\\.)*)"\s*\}', info)]

    bag = read(os.path.join(SIM, "BagItemCatalog.cs"))
    bag_items = [{"id": i, "name": cs_unescape(n), "description": cs_unescape(d)}
                 for i, n, d in re.findall(r'\{\s*"(\w+)",\s*\("((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)"\)\s*\}', bag)]

    boot = read(os.path.join(ASSETS, "Editor", "SceneBootstrap.cs"))
    shop = []
    for match in re.finditer(r'CreateShopItem\([^,]+,\s*"[^"]*",\s*"(\w+)",\s*"((?:[^"\\]|\\.)*)",\s*(\d+),\s*(' + CS_STRING + r')'
                             r'(?:,\s*(' + CS_STRING + r'))?(?:,\s*(true|false))?\)', boot):
        shop.append({
            "id": match.group(1), "name": cs_unescape(match.group(2)), "price": int(match.group(3)),
            "description": cs_unescape(match.group(5)),
            "purchase_message": cs_unescape(match.group(7)) if match.group(7) else "",
            "goes_in_bag": match.group(8) == "true",
        })

    # les postes d'examen : CreateDeskStation("Loupe", ExaminationTool.MeasurementType.VisualExamination, ...)
    scene_starts = [(m.start(), m.group(1)) for m in re.finditer(r"public static void (Create\w+Scene)\(", boot)]
    stations = []
    for match in re.finditer(r'CreateDeskStation\("(\w+)",\s*ExaminationTool\.MeasurementType\.(\w+)', boot):
        owner = "?"
        for position, name in scene_starts:
            if position < match.start():
                owner = name
        stations.append({"tool": match.group(1), "measures": match.group(2), "built_in": owner})
    owned = read(os.path.join(SCRIPTS, "Player", "PlayerOwnedTools.cs"))
    ids = dict(re.findall(r'public const string (\w+) = "([^"]+)";', owned))
    dealership = [{"id": ids.get(c, c), "name": cs_unescape(n), "price": int(price)}
                  for c, n, price in re.findall(r'CreateDealerItem\(.*?PlayerOwnedTools\.(\w+),\s*"((?:[^"\\]|\\.)*)",\s*(\d+)', boot)]
    return {"instruments": instruments, "bag_items": bag_items, "gemstore_shop": shop, "dealership": dealership, "lab_stations": stations}


def export_treatments():
    sources = [
        ("bleaching", "BleachingCatalog.cs", "BleachBath", "Baths"),
        ("dyeing", "DyeingCatalog.cs", "DyeBath", "Baths"),
        ("coating", "CoatingCatalog.cs", "CoatTarget", "Targets"),
        ("filling", "FillingCatalog.cs", "FillProduct", "Products"),
        ("diffusion", "DiffusionCatalog.cs", "Dopant", "Dopants"),
        ("irradiation", "IrradiationCatalog.cs", "IrradiationSourceInfo", "Sources"),
        ("laser", "LaserCatalog.cs", "LaserMethodInfo", "Methods"),
    ]
    result = {}
    for key, file, type_name, array in sources:
        path = os.path.join(SIM, file)
        if not os.path.exists(path):
            continue
        text = read(path)
        start = re.search(r"public static readonly " + re.escape(type_name) + r"\[\]\s+" + array + r"\s*=", text)
        if not start:
            continue
        open_index = text.index("{", start.end())
        body = text[open_index:block_end(text, open_index) + 1]
        result[key] = parse_initializers(body, type_name)
    result["note"] = ("Les pierres concernées par chaque traitement et les recettes de chauffage sont calculées dans le code (TreatmentReference, "
                      "HeatingCatalog) ; voir aussi le tableau du cours : Blanchiment, Teinture, Revêtement, Imprégnation/remplissage, Chauffage, "
                      "Diffusion, Irradiation, Laser, HPHT.")
    return result


def export_books():
    text = read(os.path.join(SIM, "BookCatalog.cs"))
    consts = dict(re.findall(r'public const string (\w+) = "([^"]+)";', text))
    ids = re.findall(r"Id = (\w+),", text)
    books = parse_initializers(text, "BookInfo")
    return [{"id": consts.get(ids[i], ids[i]) if i < len(ids) else None, "title": b.get("Title"), "subtitle": b.get("Subtitle"),
             "price": b.get("Price"), "blurb": b.get("Blurb")} for i, b in enumerate(books)]


def export_places():
    hours_text = read(os.path.join(SCRIPTS, "Systems", "OpeningHours.cs"))
    hours = [{"scene": s, "open": float(o), "close": float(c)}
             for s, o, c in re.findall(r'\{\s*"(\w+)",\s*new Hours\s*\{\s*Open = ([\d.]+)f,\s*Close = ([\d.]+)f\s*\}\s*\}', hours_text)]
    story = read(os.path.join(SCRIPTS, "Systems", "StoryState.cs"))
    gate = [{"scene": r.get("Scene"), "reputation_level": r.get("Level"), "message": r.get("Message", "")} for r in parse_initializers(story, "Rule")]
    return {"opening_hours": hours, "story_gate": gate,
            "note": "Une porte du jeu s'ouvre selon les horaires, puis selon la réputation du joueur (règle provisoire en attendant les chapitres)."}


def constants(path, names):
    text = read(path)
    found = {}
    for name in names:
        match = re.search(r"const\s+(?:float|int|string)\s+" + name + r"\s*=\s*([^;]+);", text)
        if match:
            raw = match.group(1).strip()
            if raw.startswith('"'):
                found[name] = cs_unescape(raw.strip('"'))
            else:
                number = raw.rstrip("f")
                found[name] = float(number) if "." in number else int(number)
    return found


def export_economy():
    systems = os.path.join(SCRIPTS, "Systems")
    jewel = read(os.path.join(SIM, "JewelCatalog.cs"))
    auction = read(os.path.join(SIM, "AuctionCatalog.cs"))
    return {
        "markets": constants(os.path.join(systems, "MarketState.cs"), ["PriceMarkup", "BuybackMin", "BuybackMax", "HomeCountryBonus", "RoughPenalty", "StoneQuirkSpread"]),
        "disclosure": constants(os.path.join(SIM, "TreatmentDisclosure.cs"), ["FinePercent", "ReputationLoss"]),
        "auctions": {
            **constants(os.path.join(SIM, "AuctionCatalog.cs"), ["BuyerPremium", "SellerCommission"]),
            **constants(os.path.join(systems, "AuctionState.cs"), ["MaxConsignments"]),
            "themes": [t for t in parse_initializers(auction, "AuctionTheme")],
            "note": "Une vente chaque samedi, à la banque ; le catalogue peut taire ou nier un traitement.",
        },
        "jeweler": {
            **constants(os.path.join(SIM, "JewelCatalog.cs"), ["UnsetFee", "JewelerExpertise", "JewelerShare"]),
            "kinds": parse_initializers(jewel, "JewelKindInfo"),
            "metals": parse_initializers(jewel, "JewelMetalInfo"),
            "settings": parse_initializers(jewel, "JewelSettingInfo"),
        },
        "bank": constants(os.path.join(systems, "BankState.cs"), ["Ceiling"]),
        "rapaport": constants(os.path.join(systems, "RapaportState.cs"), ["MonthlyPrice"]),
        "reputation": {"rule": "Le niveau n demande 2^(n-1) points cumulés (niveau 2 : 2 points, niveau 3 : 4, niveau 4 : 8...). Un rapport réussi au directeur donne 1 point."},
    }


# ---------------------------------------------------------------------------------------------------------- main

def main():
    species, habit_definitions = export_species()
    data = {
        "meta": {
            "game": "GemLab Simulator",
            "generated": date.today().isoformat(),
            "generator": "tools/export_game_data.py",
            "language": "fr",
            "note": "Données lues dans le projet Unity (pierres, outils, traitements, livres, lieux). À régénérer après un changement : python3 tools/export_game_data.py",
        },
        "species": species,
        "crystal_habits": habit_definitions,
        "tools": export_tools(),
        "treatments": export_treatments(),
        "books": export_books(),
        "places": export_places(),
        "economy": export_economy(),
    }
    data["meta"]["counts"] = {
        "species": len(species),
        "origins": len({o["name"] for s in species for o in s.get("origins", [])}),
        "crystal_habits": len(habit_definitions),
        "instruments": len(data["tools"]["instruments"]),
        "bag_items": len(data["tools"]["bag_items"]),
        "shop_items": len(data["tools"]["gemstore_shop"]),
        "books": len(data["books"]),
        "dealership_items": len(data["tools"]["dealership"]),
        "auction_themes": len(data["economy"]["auctions"]["themes"]),
    }
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print("écrit", os.path.relpath(OUT, ROOT), os.path.getsize(OUT) // 1024, "Ko")
    print(json.dumps(data["meta"]["counts"], ensure_ascii=False))


if __name__ == "__main__":
    sys.exit(main())
