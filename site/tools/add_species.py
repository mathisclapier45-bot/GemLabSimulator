#!/usr/bin/env python3
"""Writes the new species of tools/new_species_spec.py into the game code (they cannot be compiled here: Unity does that).

  python3 site/tools/add_species.py            writes the code
  python3 site/tools/add_species.py --check    only says what it would change

What is written (each piece sits between two marker comments, so running the tool again replaces it instead of adding it twice):
  Assets/Editor/SceneBootstrap.cs                      one BuildGem(...) block per species
  Assets/Scripts/Simulation/GemColorSystem.cs          the colour window of each species
  Assets/Scripts/Simulation/{RamanSpectrum,EdxrfSpectrum,MebImaging}.cs   the reading of the instruments
  Assets/Scripts/Simulation/CrystalHabits.cs           the shapes of the rough crystals
  Assets/Scripts/Simulation/RefractoGuide.cs           the sign of the birefringence
  Assets/Scripts/Simulation/GemLook.cs                 the pattern of the stones, the fire of the benitoite, the color change of the zultanite
  Assets/Resources/Fluorescence/fluorescence.json      the fluorescence of the benitoite

Then, in Unity: menu GemLab > Rebuild Gem Data (creates the .asset files and the icons). The site does not need that: see the README, section 15."""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from new_species_spec import SPECIES as ALL_SPECIES, ZIRCON_HIGH, PATCHES, SPECTRA_META, SPECTRA_NEW  # noqa: E402

SPECIES = [s for s in ALL_SPECIES if not s.get("site_only")]       # the species marked site_only are on the site, not (yet) in the game code

REPO = Path(__file__).resolve().parent.parent.parent
ASSETS = REPO / "GemLabSimulator" / "Assets"
SIM = ASSETS / "Scripts" / "Simulation"
TAG = "new-species-2026-11-20"
TAG_MODES = "new-species-modes-2026-11-20"          # the colour modes of the zircons (a second region in GemColorSystem.cs)
TAG_REPORT = "new-species-report-2026-11-20"        # the names of the species in the answers of the report
OPTIC_ENUM = {"Uniaxial": "Uniaxial", "Biaxial": "Biaxial", "Opaque": "Opaque", "Isotropic": "Isotropic"}


def cs(text):
    """A string for C# code."""
    return text.replace("\\", "\\\\").replace('"', '\\"')


def num(x):
    return "{:g}f".format(float(x))


def pair(fr_en):
    return fr_en[0]


def gem_block(sp):
    c1, c2 = sp["icon"]
    colors = lambda c: "new Color({}f, {}f, {}f)".format(*[("%g" % v) for v in c])
    key = sp["id"].lower().replace("_", "")
    pleo = sp.get("pleo")
    origins = ""
    for o in sp["origins"]:
        override = ',\n                        inclusionOverride = "{}"'.format(cs(o["over"][0])) if o.get("over") else ""
        origins += '                    new GemOrigin {{ name = "{}", rarityWeight = {}, valueMultiplier = {}f{} }},\n'.format(cs(o["fr"]), o["weight"], "%g" % o["mult"], override)
    lines = "".join("                    new SpectrumLine {{ wavelengthNm = {}f, widthNm = {}f }},\n".format(a, b) for a, b in sp["lines"])
    spectrum = "new List<SpectrumLine>\n                {\n" + lines + "                }" if lines else "new List<SpectrumLine>()"
    extra = ""
    if sp.get("poly"):
        extra += "                g.isPolycrystalline = true;\n"
    return '''            BuildGem("{id}", g =>
            {{
                g.displayName = "{name}";
                g.description = "{desc}";
                g.icon = GetGemIconSprite("{key}", {c1}, {c2}, "{shape}", "{cut}");
                g.roughIcon = GetRoughGemIconSprite("{key}", {c1}, "{shape}");
                g.refractiveIndex = {ri};
                g.density = {sg};
                g.hardness = {h};
                g.lengthMm = {l};
                g.widthMm = {w};
                g.depthMm = {d};
                g.weightCarats = {ct};
                g.luster = "{luster}";
                g.color = "{color}";
                g.shape = "{shape}";
                g.cutStyle = "{cut}";
                g.transparency = "{transp}";
                g.pleochroic = {pleo_b};
                g.pleochroicDescription = "{pleo_t}";
                g.fluorescenceLongWave = "{flL}";
                g.fluorescenceShortWave = "{flS}";
                g.opticalCharacter = "{opt}";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.{optic};
                g.birefringence = {biref};
{extra}                g.absorptionSpectrum = "{spec}";
                g.spectrumLines = {spectrum};
                g.inclusions = "{incl}";
                g.interferenceFigure = "{fig}";
                g.chelseaFilterReaction = "{chelsea}";
                g.magnetism = "{magn}";
                g.baseValue = {value};
                g.origins = new List<GemOrigin>
                {{
{origins}                }};
            }}),
'''.format(id=sp["id"], name=cs(sp["name"][0]), desc=cs(sp["desc"][0]), key=key, c1=colors(c1), c2=colors(c2), shape=sp["shape"], cut=sp["cut"],
           ri=num(sp["ri"]), sg=num(sp["sg"]), h=num(sp["h"]), l=num(sp["dims"][0]), w=num(sp["dims"][1]), d=num(sp["dims"][2]), ct=num(sp["ct"]),
           luster=cs(sp["luster"][0]), color=cs(sp["color"][0]), transp=sp["transp"], pleo_b="true" if pleo else "false", pleo_t=cs(pleo[0]) if pleo else "",
           flL=cs(sp["fluL"][0]), flS=cs(sp["fluS"][0]), opt=cs(sp["opt"][0]), optic=OPTIC_ENUM[sp["optic"]], biref=num(sp["biref"]), extra=extra,
           spec=cs(sp["spec"][0]), spectrum=spectrum, incl=cs(sp["incl"][0]), fig=cs(sp["fig"][0]), chelsea=cs(sp["chelsea"][0]), magn=cs(sp["magn"][0]),
           value=sp["value"], origins=origins)


def region(path, anchor, body, comment="//", tag=None, text=None):
    """Puts `body` before the line that contains `anchor`, between two marker lines; replaces the region when it is already there."""
    tag = tag or TAG
    text = text if text is not None else path.read_text(encoding="utf-8")
    begin, end = "{} <{}>".format(comment, tag), "{} </{}>".format(comment, tag)
    if begin in text:
        a = text.index(begin)
        a = text.rfind("\n", 0, a) + 1                      # start of the line of the begin marker
        b = text.index(end, a)
        b = text.index("\n", b) + 1
        new = text[:a] + indent_of(text, a) + begin + "\n" + body + indent_of(text, a) + end + "\n" + text[b:]
    else:
        at = text.index(anchor)
        assert text.count(anchor) == 1, (path.name, anchor)
        at = text.rfind("\n", 0, at) + 1
        ind = text[at:at + len(text[at:]) - len(text[at:].lstrip(" "))]
        new = text[:at] + ind + begin + "\n" + body + ind + end + "\n" + text[at:]
    return new


def indent_of(text, pos):
    line_end = text.index("\n", pos)
    line = text[pos:line_end]
    return line[:len(line) - len(line.lstrip(" "))]


def replace_once(path, old, new, check):
    text = path.read_text(encoding="utf-8")
    if check in text:
        return text
    assert text.count(old) == 1, (path.name, old)
    return text.replace(old, new, 1)


def main():
    check = "--check" in sys.argv
    out = {}
    ind = "            "
    # ---- SceneBootstrap: the species
    out[ASSETS / "Editor" / "SceneBootstrap.cs"] = region(ASSETS / "Editor" / "SceneBootstrap.cs", '            BuildGem("Grenat_Rhodolite", g =>', "".join(gem_block(s) for s in SPECIES))
    # ---- colour windows
    body = "".join('        {{ "{}", new[] {{ {}f, {}f, {}f, {}f, {}f, {}f }} }},\n'.format(s["id"], *[("%g" % v) for v in s["limits"]]) for s in SPECIES)
    body += "".join('        {{ "{}", new[] {{ {}f, {}f, {}f, {}f, {}f, {}f }} }},\n'.format(name, *[("%g" % v) for v in patch["limits"]]) for name, patch in PATCHES.items() if patch.get("limits"))
    out[SIM / "GemColorSystem.cs"] = region(SIM / "GemColorSystem.cs", "        // Quartz and other transparent stones.", body)
    # the characteristic colours of the zircons (high, low, metamict): modes of the colour system, before the fluorite's
    modes = [(name, patch["modes"]) for name, patch in PATCHES.items() if patch.get("modes")] + [(sp["id"], sp["modes"]) for sp in SPECIES if sp.get("modes")]
    body = ""
    for name, rows in modes:
        body += '            case "{}":\n                return new[]\n                {{\n'.format(name)
        body += "".join("                    M({}f, {}f, {}f, {}f, {}f, {}f, {}f),\n".format(*[("%g" % v) for v in row]) for row in rows)
        body += "                };\n"
    out[SIM / "GemColorSystem.cs"] = region(SIM / "GemColorSystem.cs", '            case "Fluorite":', body, tag=TAG_MODES, text=out[SIM / "GemColorSystem.cs"])
    # ---- Raman: which reference spectrum a species is read against
    by_ref = {}
    for s in SPECIES:
        if s.get("raman"):
            by_ref.setdefault(s["raman"], []).append(s["id"])
    body = "".join('            {} return "{}";\n'.format(" ".join('case "{}":'.format(i) for i in ids), ref) for ref, ids in by_ref.items())
    # the tourmalines join the existing tourmaline reference
    out[SIM / "RamanSpectrum.cs"] = region(SIM / "RamanSpectrum.cs", '            case "Spinelle": case "Spinelle_Bleu": return "Spinelle";', body)
    # ---- X-ray fluorescence and electron microscope
    body = "".join('            case "{}": {} break;\n'.format(s["id"], " ".join('add("{}", {}f);'.format(e, "%g" % w) for e, w in s["edxrf"])) for s in SPECIES)
    out[SIM / "EdxrfSpectrum.cs"] = region(SIM / "EdxrfSpectrum.cs", '            case "Topaze": add("Fe", 0.06f);', body)
    body = "".join('            case "{}": {} break;\n'.format(s["id"], " ".join('e("{}", {}f);'.format(e, "%g" % w) for e, w in s["meb"])) for s in SPECIES)
    out[SIM / "MebImaging.cs"] = region(SIM / "MebImaging.cs", '            case "Spinelle": e("O", 0.6f); e("Mg", 0.8f);', body)
    # ---- crystal habits
    body = "".join('            case "{}": return Chance({});\n'.format(s["id"], ", ".join('("{}", {})'.format(h, w) for h, w in s["habits"])) for s in SPECIES if s["habits"])
    out[SIM / "CrystalHabits.cs"] = region(SIM / "CrystalHabits.cs", '            case "Taaffeite": return Chance(("hex_prism", 60), ("hex_bipyramid", 40));', body)
    # ---- the sign of the birefringence (the default is negative)
    body = "".join('        {{ "{}", \'{}\' }},\n'.format(s["id"], s["sign"]) for s in SPECIES if s.get("sign"))
    out[SIM / "RefractoGuide.cs"] = region(SIM / "RefractoGuide.cs", "        // Uniaxial negative: corundum", body)
    # ---- look of the stones
    path = SIM / "GemLook.cs"
    pats = ""
    for s in SPECIES:
        if s.get("pattern"):
            name, strength, (r, g, b) = s["pattern"]
            pats += '        {{ "{}", (GemPattern.{}, {}f, new Color({}f, {}f, {}f)) }},\n'.format(s["id"], name, "%g" % strength, "%g" % r, "%g" % g, "%g" % b)
    out[path] = region(path, '        { "Padparadscha", (GemPattern.Gradient', pats)
    text = out[path]
    for old, new, chk in (('"Grenat_Mali", "Sphene"', '"Grenat_Mali", "Benitoite", "Sphene"', '"Benitoite", "Sphene"'),
                          ('            case "Grenat_Mali": return 0.028f;\n', '            case "Grenat_Mali": return 0.028f;\n            case "Benitoite": return 0.023f;\n', 'case "Benitoite"'),
                          ('def.name == "Grenat_Change_Couleur");', 'def.name == "Grenat_Change_Couleur" || def.name == "Diaspore_Zultanite");', 'def.name == "Diaspore_Zultanite"')):
        if chk not in text:
            assert text.count(old) == 1, old
            text = text.replace(old, new, 1)
    out[path] = text
    # ---- the fluorescence table of the game
    path = ASSETS / "Resources" / "Fluorescence" / "fluorescence.json"
    table = json.loads(path.read_text(encoding="utf-8"))
    keys = {e["key"] for e in table["entries"]}
    for s in SPECIES:
        if s.get("fluo_entry") and s["id"] not in keys:
            table["entries"].append(dict(key=s["id"], source="Ajout 2026 : fluorescence de {} (valeurs à faire relire)".format(s["name"][0]), **s["fluo_entry"]))
    out[path] = json.dumps(table, indent=1, ensure_ascii=False)
    # spectra and Chelsea filter of the new species (the diploma course tables)
    path = ASSETS / "Resources" / "Spectra" / "spectra.json"
    table = json.loads(path.read_text(encoding="utf-8"))
    keys = {e["key"] for e in table["entries"]}
    for s in SPECIES:
        if s.get("spectra_entry") and s["id"] not in keys:
            e = s["spectra_entry"]
            table["entries"].append({"key": s["id"], "source": "Ajout 2026 (valeurs à faire relire)", "description": e["description"],
                                     "lines": [{"wavelengthNm": w, "widthNm": wd, "depth": dp, "edgeNm": 0.0} for w, wd, dp in e["lines"]]})
        for e in s.get("spectra_extra", []):                       # the other spectra a species can show
            if e["key"] not in keys:
                table["entries"].append({"key": e["key"], "source": "Ajout 2026 (valeurs à faire relire)", "description": e["description"],
                                         "lines": [{"wavelengthNm": w, "widthNm": wd, "depth": dp, "edgeNm": 0.0} for w, wd, dp in e["lines"]]})
    for e in SPECTRA_NEW:                                          # the new spectra of species that already exist
        if e["key"] not in {x["key"] for x in table["entries"]}:
            table["entries"].append({"key": e["key"], "source": "Ajout 2026 (valeurs à faire relire)", "description": e["description"],
                                     "lines": [{"wavelengthNm": w, "widthNm": wd, "depth": dp, "edgeNm": 0.0} for w, wd, dp in e["lines"]]})
    for e in table["entries"]:                                     # which species a spectrum belongs to, and what makes it different
        if e["key"] in SPECTRA_META:
            species, (label, _) = SPECTRA_META[e["key"]]
            e.update({"of": species, "label": label})
    out[path] = json.dumps(table, indent=1, ensure_ascii=False)
    path = ASSETS / "Resources" / "Chelsea" / "chelsea.json"
    table = json.loads(path.read_text(encoding="utf-8"))
    keys = {e["key"] for e in table["entries"]}
    for s in SPECIES:
        if s.get("chelsea_entry") and s["id"] not in keys:
            table["entries"].append(dict(key=s["id"], source="Ajout 2026 (valeurs à faire relire)", **s["chelsea_entry"]))
    out[path] = json.dumps(table, indent=1, ensure_ascii=False)
    # the names of the species among the answers of the report (the player names the species of a stone)
    path = ASSETS / "Editor" / "SceneBootstrap.cs"
    names = ["Zircon (haut)"] + [sp["name"][0] for sp in SPECIES]
    body = "".join('        "{}",\n'.format(cs(n)) for n in names)
    out[path] = region(path, '        "YAG", "GGG", "Grenat Malaya", "Grenat Rhodolite",', body, tag=TAG_REPORT, text=out[path])
    changed = 0
    for path, new in out.items():
        old = path.read_text(encoding="utf-8")
        if old != new:
            changed += 1
            print("{} {}".format("changerait" if check else "écrit", path.relative_to(REPO)))
            if not check:
                path.write_text(new, encoding="utf-8")
    print("{} fichier(s) {}.".format(changed, "à changer" if check else "changé(s)"))


if __name__ == "__main__":
    main()
