# Quatre nouveaux grenats

Ajoutés d'après le schéma de la famille des grenats (variétés qui manquaient) :

| Espèce (nom dans le code) | Ce que c'est | Valeurs principales |
|---|---|---|
| **Grenat du Mali** (`Grenat_Mali`) | mélange grossulaire-andradite, jaune-vert à vert olive, découvert au Mali en 1994 | IR 1,80 · densité 3,75 · dureté 6,75 · fer 440 nm |
| **Grenat Topazolite** (`Grenat_Topazolite`) | andradite jaune à jaune-vert | IR 1,887 · densité 3,85 · dureté 6,75 · fort feu |
| **Grenat Pyrope chromifère** (`Grenat_Pyrope_Chromifere`) | pyrope rouge vif coloré par le chrome (indicateur de kimberlite) | IR 1,745 · densité 3,75 · dureté 7,25 · spectre du chrome |
| **Grenat à changement de couleur** (`Grenat_Change_Couleur`) | pyrope-spessartite, bleu-vert le jour, violet à rouge sous incandescence | IR 1,765 · densité 3,85 · dureté 7,25 · vanadium et chrome |

Les valeurs sont dans les plages de la littérature (voir `Docs/VERIF_PROPRIETES.md` pour la méthode). Les textes, les gisements et les prix de base (70, 90, 55 et 200) sont de moi : à relire.

## Ce qui a été écrit dans le jeu (rien n'a pu être compilé ni lancé ici)

- `Assets/Editor/SceneBootstrap.cs` : quatre blocs `BuildGem(...)`, copiés sur ceux des grenats existants, juste avant la rhodolite.
- `Scripts/Simulation/GemColorSystem.cs` : la plage de couleurs de chaque espèce (le grenat à changement de couleur est dans les bleu-vert, 165-205°).
- `RamanSpectrum.cs`, `EdxrfSpectrum.cs`, `MebImaging.cs`, `UvVisNirSpectrum.cs` : le spectre de chaque espèce (Raman, fluorescence X, microscope électronique, UV-visible).
- `GemLook.cs` : le feu et l'éclat de la topazolite et du grenat du Mali (comme le démantoïde), et le **changement de couleur** : le grenat à changement de couleur change de couleur sous la lampe à incandescence de la loupe, comme l'alexandrite (la couleur « incandescente » est la même que pour l'alexandrite : un rose-rouge).
- Les habitus cristallins n'ont pas besoin d'être touchés : tout nom qui commence par `Grenat_` reçoit le dodécaèdre rhombique.

## À faire dans l'ordre

1. **Unity** : ouvrir le projet (regarder la console : une erreur de compilation serait dans les fichiers ci-dessus), puis menu **GemLab > Rebuild Gem Data**. Cela crée les quatre `.asset` et leurs icônes dans `Assets/Data/Gems/`.
2. **Dans le jeu** : regarder les quatre pierres à la loupe, au réfractomètre, au spectroscope et sous la lampe à incandescence (le grenat à changement de couleur).
3. **Le site** :
   ```
   python3 site/tools/sync_data.py --export
   python3 site/tools/export_palettes.py
   python3 site/tools/build_species_extra.py
   python3 site/tools/add_new_garnets.py
   python3 -m unittest discover -s site/tests
   ```
   `add_new_garnets.py` range les quatre espèces dans la classe des silicates et dans la famille des grenats, et ajoute leurs textes anglais. Sur le schéma des grenats, les variétés deviennent des liens dès que l'espèce existe.

## Prudence

- Les fichiers du jeu ci-dessus contiennent aussi des modifications non commitées qui ne sont pas de moi : un commit de ces fichiers les prendrait avec.
- Je n'ai pas pu compiler le code C#. Si Unity signale une erreur, envoie-moi le message.
