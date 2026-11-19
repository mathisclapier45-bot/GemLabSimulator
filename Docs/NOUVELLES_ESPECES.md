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

## Saphir et fluorite : une seule espèce chacun (2026-11-20)
Le jeu n'a plus que `Saphir` (au lieu de Saphir_Bleu, _Jaune, _Rose, _Orange, _Vert, _Violet, _Incolore) et `Fluorite` (au lieu de Fluorite_Verte, _Bleue, _Jaune). Détails dans `site/README.md`, section 16.
À vérifier dans Unity après « Rebuild Gem Data » : la console (code non compilé), un saphir de chaque couleur au chauffage (bleu, jaune, orange) et à l'irradiation (incolore/jaune), l'échantillon incolore du technicien, la pierre de référence bleue de l'histoire (cabine UV), la pierre de septembre du calendrier.
Limites connues : les pages du livre « couleurs du corindon » gardent l'icône bleue pour toutes les couleurs ; le spectroscope donne la même raie du fer à tous les saphirs sauf les verts (trois bandes) ; le filtre Chelsea n'a qu'une réaction pour tout le saphir.
