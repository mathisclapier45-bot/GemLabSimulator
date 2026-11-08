# Les leçons de la professeure Thouvenot

Le **panneau des leçons** (dans l'entrée du laboratoire, en face de la boîte aux lettres) liste les leçons et permet de les rejouer. Une leçon s'ouvre quand l'instrument qu'elle enseigne a été présenté par Thouvenot (une ancienne sauvegarde a tout d'ouvert).

Chaque leçon est un fichier JSON dans `GemLabSimulator/Assets/Resources/Lessons/` : on change un texte sans toucher au code.

| Fichier | Leçon | S'ouvre avec |
|---|---|---|
| `gemmes.json` | Qu'est-ce qu'une gemme ? (jouée aussi pendant la première visite du labo) | la loupe |
| `durete.json` | La dureté (Mohs, dureté absolue, crayons de dureté, test destructif) | la loupe |
| `tenacite.json` | La ténacité (clivage, cassure) | la loupe |
| `stabilite.json` | La stabilité (lumière, chaleur, produits chimiques) | la loupe |
| `loupe.json` | La loupe et la chamoisine | la loupe |
| `balance_carats.json` | La balance à carats | la balance |
| `balance_hydrostatique.json` | La balance hydrostatique | la densité |
| `refractometre.json` | Le réfractomètre (animation `RefractionLessonUI`, le JSON sert de fiche) | le réfractomètre |
| `systemes_cristallins.json` | Les systèmes cristallins | le polariscope (chapitre 2, présenté par Thouvenot) |
| `polariscope.json` | Polariscope et conoscope | le polariscope (chapitre 2, présenté par Thouvenot) |

## Format

```json
{
  "id": "loupe",
  "kind": "tool",                 // "tool" (affiche la fiche de l'instrument) ou "concept"
  "title": "La loupe et la chamoisine",
  "unlock": "VisualExamination",  // nom d'un ExaminationTool.MeasurementType, ou "" pour toujours ouverte
  "animation": "",                // "RefractionLessonUI" pour la leçon animée du réfractomètre
  "tool": {                       // la fiche de l'instrument (vide pour un "concept")
    "name": "Loupe 10×", "measures": "...", "where": "...",
    "records": ["Éclat", "Couleur"], "tips": ["..."]
  },
  "steps": [
    { "title": "...", "text": "... <b>gras</b> ...\n...", "figure": "loupe", "focus": 2, "image": "" }
  ]
}
```

- `figure` : le schéma provisoire dessiné par le code (`criteria`, `taxonomy`, `hardness`, `tenacity`, `stability`, `loupe`, `carat`, `hydro`, `systems`, `polariscope`). `focus` choisit ce qu'il montre (le critère, le système cristallin...).
- `image` : le nom (sans extension) d'une image de l'artiste, placée dans `Assets/Resources/Lessons/Images/` et importée comme **Sprite**. Si elle existe, elle **remplace** le schéma de l'étape. Exemple : `gemme_arbre` pour la deuxième étape de `gemmes.json`.
- Pour ajouter une leçon : un nouveau JSON, et son id dans `LessonLibrary.Order`.

## Images attendues de l'artiste

- `gemme_arbre` : l'arbre des gemmes (gemmes → naturelles / non naturelles → cristallines, non cristallines, synthétiques, sans contrepartie, assemblées → minéral, amorphe, matières organiques). Format conseillé : 480×340 px, fond sombre ou transparent.
- Pour les autres étapes, toute image au même format peut être indiquée dans le champ `image` de l'étape.
