# Les 22 nouvelles espèces : mode d'emploi et points à relire

Source unique : `site/tools/new_species_spec.py`. Code du jeu : `python3 site/tools/add_species.py` (entre marqueurs `new-species-2026-11-20`).

## À faire dans Unity
1. Ouvrir le projet, menu **GemLab > Rebuild Gem Data** (crée les `.asset` et les icônes des 22 espèces et des 4 grenats).
2. Lire la console : **le code C# n'a pas pu être compilé** pendant l'écriture (seules des vérifications de structure ont été faites).
3. Ensuite : `python3 site/tools/sync_data.py --export`, puis `export_palettes.py`, `build_species_extra.py`, `add_new_species.py` et les tests.

## Fichiers du jeu touchés (contenaient déjà d'autres modifications non commitées)
SceneBootstrap.cs, GemColorSystem.cs, RamanSpectrum.cs, EdxrfSpectrum.cs (+ baryum), MebImaging.cs (+ Be, B, Ba), CrystalHabits.cs, RefractoGuide.cs, GemLook.cs, Resources/Fluorescence/fluorescence.json.

## Données à faire relire par un gemmologue
Valeurs de base (`baseValue`), poids des provenances, fenêtres de couleur, fluorescences, pléochroïsme, et les spectres : les textes ont été écrits sans chiffres de raies non vérifiés pour l'hiddénite, l'enstatite, la sugilite et la verdélite. Pierres polycristallines (aventurine, jaspe, onyx, larimar, charoïte, sugilite, chrysocolle, variscite, lépidolite) : lecture de l'indice « à la tache ».
