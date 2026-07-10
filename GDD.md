# GemLab Simulator — Game Design Document

> Document vivant. Mis à jour au fil du développement — ne pas le considérer comme figé.

## 1. Pitch

GemLab Simulator est un jeu de simulation/RPG en top-down 2D pixel art, dans l'esprit visuel d'EarthBound et la boucle de vie quotidienne de Stardew Valley. Le joueur incarne un gemmologue installé dans une petite ville. Il reçoit des gemmes par la poste, les étudie avec des instruments de gemmologie réels (loupe, réfractomètre, polariscope, balance hydrostatique, spectroscope, dichroscope, microscope, puis des instruments de labo avancés : FTIR, Raman, UV-Vis-NIR, MEB...), rédige un rapport de conclusion, et le présente au directeur du laboratoire pour validation. Une identification validée rapporte de l'argent, de la réputation, et fait progresser le joueur.

## 2. Plateforme & moteur

- **Plateforme cible** : PC, distribution via **Steam**.
- **Moteur** : Unity (C#).
- **Résolution de référence** : à fixer avant tout sprite (proposition : 384x216 ou 320x180, upscalé en plein écran). Utiliser le package **2D Pixel Perfect** dès la mise en place du projet pour éviter le tremblement de pixels en mouvement.
- **Import sprites** : Filter Mode = Point (no filter), Compression = None, Pixels Per Unit cohérent avec la taille de tuile choisie (16px ou 32px).
- **Input** : clavier/souris en priorité (jeu PC), support manette en option plus tard.

## 3. Boucle de gameplay (game loop)

1. Le joueur reçoit une gemme par courrier (boîte aux lettres à la maison ou au bureau).
2. Il l'examine avec les outils disponibles selon son niveau/lieu (bureau = tests simples, laboratoire = instruments avancés).
3. Chaque outil renvoie une **mesure** (indice de réfraction, densité, pléochroïsme, spectre d'absorption, inclusions visibles, etc.), avec une marge d'erreur selon la compétence du joueur et l'état de l'instrument.
4. Le joueur croise les mesures pour déterminer l'espèce/variété/traitement éventuel de la gemme, et rédige un rapport (choix parmi une liste, pas de texte libre).
5. Il présente le rapport au directeur du laboratoire, qui valide ou rejette selon l'exactitude et la qualité de la démarche.
6. Validation → argent + réputation + XP. Rejet → pénalité légère (temps perdu, réputation).
7. Avec l'argent : achat de meilleurs outils, accès à de nouveaux lieux, achat/vente de gemmes au marché ou à la bourse du diamant, taille de pierres à l'atelier lapidaire, exposition au musée.

## 4. Scope — Vertical Slice (v1) vs Vision complète

Le risque principal de ce projet est le scope (9 lieux, ~15 instruments, économie, PNJ multiples). Approche : construire une **tranche verticale** jouable de bout en bout avant d'étendre.

### Vertical Slice (objectif court terme)
- Lieux : Maison + Bureau de gemmologie uniquement.
- Outils : Loupe, réfractomètre, balance hydrostatique (3 suffisent pour une boucle complète).
- Gemmes : 5 espèces simples et bien différenciables (ex: quartz, corindon/rubis, spinelle, grenat, verre).
- PNJ : le joueur + le directeur du laboratoire.
- Boucle complète : réception → examen → rapport → validation → argent.
- Sauvegarde basique.

### Vision complète (roadmap post-slice, voir §11)
Tous les lieux et systèmes ci-dessous.

## 5. Systèmes de jeu

### 5.1 Base de données de gemmes (data-driven)
Chaque gemme est définie par des données (ScriptableObject en Unity), pas en dur dans le code :
- Espèce, variété, propriétés physiques réelles (indice de réfraction, biréfringence, densité, dureté, pléochroïsme, fluorescence UV, inclusions typiques, spectre d'absorption).
- Traitements éventuels (chauffage, irradiation, remplissage) et synthèses/imitations à distinguer des naturelles.
- Rareté, valeur de base, origine géographique.

### 5.2 Outils de gemmologie
Chaque outil = un mini-jeu ou une interaction courte qui produit une mesure exploitable dans le rapport. Prévoir une marge d'erreur/risque de mauvaise lecture pour garder de la difficulty et de la place à la progression du joueur (compétence qui réduit l'erreur).

**Bureau de gemmologie (accessible tôt)** : loupe, réfractomètre, polariscope, balance hydrostatique, spectroscope, dichroscope, microscope.

**Laboratoire de gemmologie (débloqué plus tard)** : spectromètre FTIR, Raman, UV-Vis-NIR, MEB, et autres instruments avancés pour détecter traitements/synthèses indétectables aux tests classiques.

### 5.3 Rapport & validation
Le joueur compile ses mesures dans un rapport structuré (choix guidés : espèce, variété, traitement, niveau de confiance). Le directeur du laboratoire évalue selon l'exactitude réelle et peut donner un feedback (indice) en cas d'erreur, pour rester pédagogique plutôt que punitif.

### 5.4 Économie
- Revenus : validation de rapports (payée par le client qui a envoyé la gemme), vente de gemmes/pierres taillées.
- Dépenses : achat/entretien d'outils, taxe/droit d'entrée bourse du diamant, achat de gemmes au marché ou au tunnel, upgrades de la maison/atelier.
- Le marché aux pierres propose des marchands aléatoires (stock et prix variables) — bonne source de rejouabilité.
- Le tunnel : marchand mystérieux, gemmes rares/interdites (probablement synthèses non déclarées, pierres volées ou d'origine douteuse) — risque/récompense, peut-être lié à une mécanique de réputation ou de moralité.

### 5.5 Progression du joueur
- Rang de gemmologue (débloque instruments, lieux, types de gemmes plus rares/complexes).
- Réputation (influence les prix, l'accès au manoir, à la bourse du diamant).
- Compétence par instrument (réduit la marge d'erreur des mesures).

### 5.6 Atelier lapidaire
Taille des pierres brutes en pierres facettées/cabochons — augmente leur valeur, débloque des ventes au manoir ou au musée. Mini-jeu de précision probable.

### 5.7 Musée
Le joueur peut exposer ses plus belles/rares découvertes. Sert de vitrine de progression (collection) plutôt que de source de revenu directe — bonus de réputation possible.

## 6. Lieux de la ville

| Lieu | Fonction |
|---|---|
| Maison du joueur | Repos, réception du courrier, sauvegarde |
| Bureau de gemmologie | Tests simples et instruments de base |
| Laboratoire de gemmologie | Instruments avancés (FTIR, Raman, UV-Vis-NIR, MEB) |
| Atelier lapidaire | Taille des pierres |
| Musée | Exposition des spécimens |
| Quartier des diamantaires / Bourse du diamant | Achat/échange de diamants |
| Marché aux pierres | Marchands aléatoires internationaux |
| Le Manoir | Client fortuné, achète les plus belles pierres |
| Le Tunnel | Marchand mystérieux, gemmes rares/interdites |

## 7. Personnages clés

- **Le joueur** : gemmologue jouable, personnalisable (à définir).
- **Le directeur du laboratoire** : valide/rejette les rapports, PNJ central de la boucle de jeu.
- **L'homme du Manoir** : client fortuné et collectionneur.
- **Marchands du marché** : PNJ aléatoires, origines variées, stocks/prix dynamiques.
- **Le marchand du tunnel** : PNJ mystérieux, apparitions aléatoires, gemmes rares/interdites.

## 8. Direction artistique

- Style pixel art top-down, inspiration EarthBound (chaleur, couleurs, PNJ expressifs) + Stardew Valley (lisibilité, routine de vie).
- Palette de couleurs limitée et cohérente à fixer avant production des assets.
- Grille de tuiles identique dans tous les lieux.
- Design des instruments de gemmologie basé sur du matériel réel (dossier de références visuelles déjà collecté dans le projet — à ranger dans `Assets/References/`).

## 9. Architecture technique (Unity)

- **Séparation simulation / présentation** : la logique d'identification (gemmes, outils, mesures) doit être indépendante du rendu et de l'UI, pour rester testable.
- **ScriptableObjects** pour : définitions de gemmes, définitions d'outils, PNJ, objets du marché, quêtes/commandes.
- **Système de dialogue** et **système de quêtes** génériques et data-driven, réutilisables pour tous les PNJ.
- **Système de sauvegarde** pensé tôt : inventaire de gemmes, argent, réputation, rang, gemmes découvertes (musée), outils possédés.

## 10. Structure de projet proposée

```
Assets/
  Art/
    Sprites/
    Tilesets/
    UI/
  References/          (images de matériel réel déjà collectées)
  Data/
    Gems/               (ScriptableObjects gemmes)
    Tools/              (ScriptableObjects outils)
    NPCs/
    Quests/
  Scripts/
    Simulation/         (logique d'identification, indépendante de l'UI)
    Systems/            (économie, sauvegarde, progression)
    UI/
    Player/
    World/
  Scenes/
    Home.unity
    GemologyOffice.unity
    Lab.unity
    ...
  Audio/
```

## 11. Roadmap / Milestones

1. **M0 — Setup** : projet Unity, config pixel perfect, structure de dossiers, GDD (ce document).
2. **M1 — Vertical Slice** : Maison + Bureau, 3 outils, 5 gemmes, boucle complète jouable.
3. **M2 — Extension du cœur de jeu** : plus de gemmes/outils, système de progression, sauvegarde complète.
4. **M3 — Laboratoire avancé** : instruments FTIR/Raman/UV-Vis/MEB, détection de traitements/synthèses.
5. **M4 — Économie & lieux sociaux** : marché aux pierres, bourse du diamant, manoir.
6. **M5 — Atelier lapidaire & musée**.
7. **M6 — Le Tunnel** : contenu rare/interdit, éventuelle mécanique de risque/moralité.
8. **M7 — Polish & sortie Steam** : intégration Steamworks (achievements, cloud save), optimisation, tests.

## 12. Questions ouvertes (à trancher au fil du dev)

- Personnalisation du personnage : oui/non, à quel niveau ?
- Le tunnel implique-t-il un risque légal/moral (réputation, police) ou reste-t-il purement transactionnel ?
- Multijoueur : hors scope pour l'instant.
- Durée d'une "journée" en jeu, cycle jour/nuit, saisons (façon Stardew) ?
