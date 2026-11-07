# GemLab Simulator — Guide du graphisme

Ce document explique **ce que le jeu attend comme images**, **dans quel format**, et **la liste de ce qu'il faut créer**. Tout ce qui est dans le jeu aujourd'hui est provisoire : chaque image peut être remplacée.

La liste exacte des fichiers actuels (avec leurs tailles) est dans `INVENTAIRE_GRAPHISMES.md`.

---

## 1. Les règles pour toutes les images

| Sujet | Règle |
|---|---|
| **Format de fichier** | **PNG**, en couleurs avec transparence (RGBA, 8 bits par canal). Pas de JPG, pas de GIF. |
| **Pixel art** | 1 pixel du dessin = 1 pixel du fichier. Pas d'anti-crénelage (pas de bords lissés), pas de flou, pas de dégradés « photo », pas de demi-transparence sur les contours. |
| **Ne jamais agrandir à l'export** | On dessine à la taille réelle (voir les tailles plus bas). Le jeu agrandit tout seul à l'écran. |
| **Échelle du monde** | **32 pixels = 1 case** (1 « mètre » du jeu). Un personnage fait 1 case de large et 2 cases de haut (32 × 64 px). |
| **Fond** | Toujours transparent (jamais un fond blanc ou coloré). |
| **Noms de fichiers** | Pas d'accents, pas d'espaces, pas de majuscules inattendues. Le nom est utilisé par le jeu : **si on garde le nom, l'image est remplacée toute seule.** |
| **Une image = un fichier** | Pas de planche d'animation (« spritesheet ») pour l'instant : chaque image d'une animation est un fichier PNG séparé (voir §2). |
| **Ancrage (le point par lequel l'image est posée dans le monde)** | **Bas, au milieu** de l'image pour les personnages, bâtiments, meubles, arbres, voitures (les pieds / la base touchent le sol). Au milieu pour les icônes d'interface. |
| **Images à bords nets, dimensions paires** | Préférer des tailles paires (ex. 32 × 64, pas 31 × 63), pour que l'image reste centrée sans flou. |

### Fichiers de travail (sources)
Pour travailler, elle peut utiliser le logiciel qu'elle préfère (Aseprite, Photoshop, Krita, Pixquare, Procreate…) et garder les fichiers de travail (`.aseprite`, `.psd`, `.kra`…) **dans un dossier à part** (par exemple `Art_Sources/`, en dehors de `GemLabSimulator/`). Seuls les **PNG exportés** vont dans le jeu.

Conseil : **Aseprite** est le plus pratique pour le pixel art animé (une animation par direction, export en fichiers séparés avec un nom du type `{frame}.png`).

---

## 2. Le personnage joueur

C'est le plus important : c'est ce qu'on voit tout le temps.

- **Taille d'une image : 32 × 64 px**, personnage debout, pieds en bas de l'image, centré.
- **4 directions** : bas (de face), haut (de dos), gauche, droite. **Gauche et droite doivent toutes les deux être dessinées** (le jeu ne retourne pas l'image du joueur).
- **Marche : 6 images par direction**, soit 24 images. Le jeu les enchaîne en boucle (0 → 5 → 0), à environ 8 images par seconde (une image toutes les 0,12 s).
- **Repos : 1 image par direction** (le personnage immobile), soit 4 images.
- **Total par tenue : 28 images.**

### Rangement et noms (obligatoires)
```
Resources/PlayerWalk/Down/0.png … 5.png
Resources/PlayerWalk/Up/0.png … 5.png
Resources/PlayerWalk/Left/0.png … 5.png
Resources/PlayerWalk/Right/0.png … 5.png
Resources/PlayerIdle/Down.png, Up.png, Left.png, Right.png
```
Les images doivent être **numérotées 0, 1, 2, 3, 4, 5** (pas 01, pas 10 : le jeu les trie par ordre alphabétique, donc au-delà de 9 images il faut les nommer 00, 01, 02… et me prévenir).

### Les 3 tenues
Le jeu change l'apparence du joueur selon ce qu'il porte. Il faut donc **3 jeux** de 28 images :

| Tenue | Dossiers | Taille d'une image |
|---|---|---|
| Normal | `PlayerWalk` + `PlayerIdle` | 32 × 64 |
| Avec les baskets (court plus vite) | `PlayerWalkBaskets` + `PlayerIdleBaskets` | 32 × 64 |
| À vélo (en ville seulement) | `PlayerWalkBike` + `PlayerIdleBike` | **64 × 64** (le vélo est plus large) |

→ **84 images pour le joueur** au total.

### « Les déplacements doivent-ils être dans le même fichier ? »
**Non, pas dans l'état actuel du jeu** : une image = un fichier PNG. Elle peut très bien tout dessiner dans **un seul fichier de travail** (par exemple un `.aseprite` avec une animation par direction) puis **exporter en fichiers séparés**.

Si elle préfère livrer une **planche unique** (par exemple 6 colonnes × 4 lignes dans un seul PNG de 192 × 256), c'est possible : **je peux modifier le jeu pour lire une planche**. À décider ensemble avant qu'elle ne finisse les images.

---

## 3. Les autres personnages

### 3.1 Piétons de la ville (qui se promènent)
- **8 apparences différentes** (`W0` à `W7`), chacune avec **exactement la même structure que le joueur** : 4 directions × 6 images de marche, **32 × 64 px**.
- Rangement : `Resources/Walkers/W0/Down/0.png … W7/Right/5.png`.
- Total : 8 × 24 = **192 images**. (Pour limiter le travail, on peut commencer par 2 ou 3 apparences et en réutiliser en changeant les couleurs.)

### 3.2 Personnages fixes (derrière un comptoir, au bureau…)
Une **seule image** par personnage, de face, immobile, pieds en bas :
- Taille actuelle : **32 × 44 px** (certains 32 × 52 ou 32 × 56).
- 16 personnages : directeur, horloger, lapidaire, 4 marchands de pierres, 4 marchands de brut, rapaport, réceptionniste, vieil homme, vieille dame, personnage du tunnel.
- **La professeure Thouvenot** (`Resources/Story/Thouvenot.png`, 32 × 44) : c'est le personnage le plus présent dans le tutoriel, à soigner.
- Si elle veut qu'ils marchent ou bougent, il faudra me le dire (il faudra alors les mêmes 4 × 6 images que le joueur).

### 3.3 Portraits des personnages (facultatif, mais recommandé)
Dans les dialogues, le visage affiché est **découpé automatiquement dans le haut de l'image du personnage**. Si elle veut des visages plus soignés, elle peut dessiner un **portrait par personnage (64 × 64 px, PNG)**. Ça demande une petite modification du jeu de ma part.

### 3.4 Animaux de compagnie
10 espèces : chat, chien, crocodile, grenouille, marabout, singe, serpent, tigre, tortue, varan.
Pour **chaque espèce, 9 images** (tailles variables selon l'animal, environ 24 × 28 à 38 × 38 px) :
- de côté : `nom_0.png`, `nom_1.png`, `nom_2.png` (3 images de marche ; **l'animal regarde vers la droite** : le jeu le retourne tout seul quand il va à gauche) ;
- de face : `nom_down_0.png`, `nom_down_1.png`, `nom_down_2.png` ;
- de dos : `nom_up_0.png`, `nom_up_1.png`, `nom_up_2.png`.

Dossier : `Resources/Pets/`. Total : environ 90 images.

---

## 4. Le décor extérieur (la ville)

### 4.1 Les bâtiments
Une seule image par bâtiment, vue de face (légèrement de dessus), **pieds de la façade en bas de l'image, porte au milieu en bas**. Tailles actuelles très variables (par exemple 224 × 416, 320 × 352, 480 × 420) : **on peut changer les tailles**, mais il faut me le dire (je repositionne le bâtiment et sa porte dans le jeu).

Bâtiments à dessiner (extérieurs) : Maison et Bureau de Gemmologie, Laboratoire de gemmologie, Atelier lapidaire, Musée, Marché aux pierres, Marché aux brutes, Bijouterie, Banque, Quartier des diamantaires, GemStore, Bibliothèque, Animalerie, Boutique de montres, Boutique de vêtements, École, Concessionnaire, Laboratoire de synthèse, Laboratoire de traitements, Manoir, Aéroport (hangar, tour de contrôle, hélicoptère), maisons du joueur et de Vincent, et 3 maisons génériques (cabane, chalet, moderne).

### 4.2 Le sol et la voirie (en tuiles de 32 × 32 px)
Aujourd'hui, une partie du sol de la ville vient d'un **pack d'images acheté** (non redistribuable : il n'est pas dans le dépôt GitHub). **Il faudra le remplacer** si le jeu est un jour distribué.
À créer, en tuiles **32 × 32** qui se raccordent entre elles : herbe (plusieurs variantes), chemin de terre, asphalte (+ marquages au sol horizontaux et verticaux), trottoir, eau peu profonde, eau profonde, bords de l'eau, pont (planches et rambardes), piste d'aéroport (plain, bordures, tirets), haie, toit.

### 4.3 Éléments posés dans la ville
Arbres, buissons, fleurs (9 variantes), réverbères, bancs, voitures vues de face (64 × 128 et 96 × 160 px, 6 modèles), panneaux, etc. Tailles libres, ancrage en bas au milieu.

---

## 5. Les intérieurs

Chaque lieu a son ambiance : maison du joueur, bureau de gemmologie (Home), laboratoires, atelier, boutiques, marché, musée, manoir, tunnel, banque…
- **Sol** : tuile 32 × 32 (une par ambiance : `home`, `lab`, `atelier`, `market`, `museum`, `manoir`, `rough`, `tunnel`).
- **Murs** : 3 tuiles par ambiance (mur de dessus, mur de côté, mur de face), 32 × 32.
- **Meubles et accessoires** (≈ 120 aujourd'hui) : bureaux, étagères, comptoirs, fauteuils, plantes, tapis, lit, cheminée, horloge, ascenseur, cuves de teinture, fours, etc. Tailles libres (en général 32 × 32 à 64 × 64), ancrage en bas au milieu.

La liste complète est dans `INVENTAIRE_GRAPHISMES.md` (fichiers `interior_*`).

---

## 6. Les instruments et les objets

| Élément | Taille | Où | Nombre |
|---|---|---|---|
| Instruments posés sur les bureaux du labo | **48 × 40** (quelques-uns plus grands) | `Art/Sprites/toolhd_*.png` | 25 |
| Icônes d'instruments dans les fenêtres | 48 × 40 ou 32 × 32 | `Resources/ToolIcons/` | 23 |
| Illustrations d'instruments dans les livres | 48 × 40 à 84 × 40 | `Resources/BookIcons/` | 14 |
| Objets du sac (loupes, lampes, chamoisine, baskets, vélo…) | 30 × 18 (baskets 32 × 20, vélo 34 × 22) | `Resources/BagIcons/` | 16 |
| Objets vendus au GemStore | environ 14 × 24 à 30 × 16 | `Art/Sprites/shopitem_*.png` | 4 |
| Meubles « importants » : armoires, coffre, boîte aux lettres | 24 × 44 à 40 × 56 | `Art/Sprites/furniture_*.png` | 4 |

---

## 7. Les icônes et illustrations

| Élément | Taille | Nombre |
|---|---|---|
| **Icônes de la minimap** (une par lieu) | **20 × 20** | 23 |
| **Drapeaux** des pays | **32 × 32** | 20 |
| **Dos de carte** (classeur de cartes) | **100 × 140** | 1 |
| **Paquet de cartes (booster)** | **64 × 96** | 1 |
| **Cinématique d'introduction** (illustrations plein écran) | **480 × 270** | 5 |
| Fonds de cartes / illustrations de cartes | à décider | — |

---

## 8. Les pierres précieuses

Aujourd'hui **le jeu dessine les pierres lui-même par programme** (pierre en 3D pixelisée qui tourne, forme brute selon l'habitus du cristal). Il n'y a donc **rien à dessiner** pour les 90 espèces : elles se règlent par leur couleur.
Il existe aussi 93 icônes de gemmes taillées et 91 de brutes (`gemicon_*.png`, `roughicon_*.png`, 32 × 32) qui ne servent plus que de secours. Si elle veut des icônes dessinées à la main, c'est possible mais ce n'est pas une priorité.

---

## 9. L'interface (fenêtres, boutons, jauges)

L'interface est **construite par programme** (cadres orange, boutons, horloge, bourse, téléphone, jauge d'XP, fenêtres des instruments). Si elle veut la redessiner, voici ce qui serait à faire (je m'occuperai de l'intégrer) :

| Élément | Description | Taille indicative |
|---|---|---|
| Cadre de fenêtre | Cadre « 9 tranches » (coins + bords qui s'étirent) | 24 × 24 |
| Cadre du bandeau (horloge, bourse, jauge) | idem | 24 × 24 |
| Bouton | normal / survolé / désactivé | 24 × 24 (9 tranches) |
| Case en creux (champs de texte, barres) | 9 tranches | 16 × 16 |
| Jauge d'XP | fond + remplissage | 200 × 18 |
| Pièce de monnaie | | 26 × 26 |
| Téléphone | petit téléphone de l'écran | 44 × 70 |
| Flèche orange du guide (qui rebondit) | pointe vers le bas | 15 × 20 |
| Touche « E » (interaction) | | 16 × 16 |
| Onglets de la loupe (6 pictogrammes) | éclat (étoile), couleur, transparence, forme, inclusions, surface | 16 × 16 chacun |
| Menu principal | fond d'écran et titre | 1280 × 720 |

---

## 10. Par où commencer (ordre conseillé)

1. **Décider la taille de l'échelle** (voir §11) et le style général.
2. **Le joueur** : normal (28 images), puis baskets et vélo.
3. **La professeure Thouvenot** et 2-3 piétons.
4. **Le bureau de gemmologie** (Home) : sol, murs, bureaux, instruments de départ (loupe, balance à carats, balance hydrostatique), boîte aux lettres, armoires. C'est le décor du tutoriel.
5. **La ville** : herbe, routes, bâtiments principaux.
6. Les autres intérieurs, les animaux, les icônes et le reste.

---

## 11. Deux décisions à prendre AVANT de dessiner

1. **L'échelle : 32 px par case est-il bon ?** Aujourd'hui le personnage fait 32 × 64 px. Si elle veut dessiner plus détaillé (par exemple 48 ou 64 px par case), **tout le jeu doit être refait à cette échelle** et il faut me le dire *avant* de commencer : on ne peut pas mélanger deux échelles dans le même décor.
2. **Planches ou fichiers séparés ?** (voir §2). Je peux adapter le jeu à l'une ou l'autre méthode.

---

## 12. Comment remplacer et tester ses images

1. **Remplacer = écraser le fichier existant**, avec le même nom, dans le même dossier. **Ne pas le supprimer puis en recréer un** : à côté de chaque PNG, Unity garde un petit fichier du même nom en `.png.meta` qui contient les bons réglages de l'image (sprite, filtre « Point » sans lissage, sans compression, point d'ancrage, 32 pixels par unité). **Il ne faut jamais supprimer ni renommer les fichiers `.meta`.**
2. Ouvrir le projet dans Unity : l'image est réimportée toute seule avec les mêmes réglages.
3. Lancer le jeu et regarder.
4. **Pour un nouveau fichier** (une image qui n'existe pas encore, par exemple un 9ᵉ piéton ou un nouveau meuble), Unity n'applique pas les bons réglages tout seul : il faut me le dire, je configure l'import et le code qui l'utilise.

Si besoin, je peux préparer un **dossier d'échange** avec tous les noms de fichiers déjà créés (images vides à remplacer), pour qu'elle n'ait qu'à dessiner dedans.
