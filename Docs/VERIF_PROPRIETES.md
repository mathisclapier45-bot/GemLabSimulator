# Vérification des propriétés des 120 espèces (hors fluorescence)

Date : 19 novembre 2026. Données : `site/data/gemlab_data.json` (copie des données du jeu). Complète `Docs/VERIF_FLUORESCENCE.md`.

**Aucune donnée n'a été modifiée.** Ce document signale les écarts ; les corrections se font dans le jeu (`GemLabSimulator/Assets/Data/Gems/*.asset`), pas sur le site.

## Méthode : des plages, pas des valeurs exactes

Le jeu donne **une seule valeur** par propriété (indice de réfraction, densité, dureté, biréfringence). Les tables de gemmologie donnent des **plages** qui varient avec l'origine et la composition. J'ai donc procédé ainsi :
- pour chaque espèce, j'ai pris la **plage la plus large plausible** (minimum et maximum possibles, toutes origines et compositions confondues) ;
- j'ai regardé si la valeur du jeu **tombe dans cette plage**. Une valeur au bord de la plage reste correcte : c'est une valeur typique, pas une erreur ;
- les cas douteux ont été recherchés sur le web (liens en bas).

**Limite** : les plages viennent de ma connaissance des références (Webster, GIA, Gübelin, tables de gemmologie) et de recherches en ligne de qualité inégale, pas d'un accès direct aux livres. Pour les matériaux organiques (cornes, ivoires, écaille) et les agrégats (jades, agates), les plages sont approximatives.

## Résultat en bref

- Sur **480 valeurs** (120 espèces × indice de réfraction, densité, dureté, biréfringence), **477 sont dans la plage** de la littérature. **3 sortent de la plage**, toutes de peu.
- **2 spectres d'absorption à revoir** (painite, spinelle bleu).
- **1 point à confirmer** (pléochroïsme de la calcite).
- Le **filtre Chelsea** et le **pléochroïsme** des autres espèces sont cohérents avec la littérature.

## A. Valeurs hors plage (à corriger)

| Espèce | Propriété | Jeu | Plage de la littérature | Valeur proposée |
|---|---|---|---|---|
| **Painite** | biréfringence | 0,022 | 0,027 – 0,031 (≈ 0,029) | **0,029** |
| **Pezzottaïte** | densité | 3,05 | 3,09 – 3,13 | **3,10** |
| **Rhodocrosite** | densité | 3,75 | 3,40 – 3,71 | **3,65** (au milieu des plages courantes 3,45-3,70) |

Le RI de la painite (1,787 – 1,816, jeu 1,80) est correct. Les sources de painite se contredisent sur le caractère optique (uniaxe ou biaxe) : à ne pas toucher sans meilleure source.

## B. Spectres d'absorption à revoir

- **Painite** (jeu : « raies marquées dans le rouge et le bleu, fer et chrome », lignes à 445 et 635 nm). Une étude minéralogique de 1986 sur la painite type donne des **bandes à 553, 450 et 402 nm, avec un épaulement vers 608 nm**, dues au **chrome et au vanadium** (le spectre ressemble à celui de la tsavorite). **À corriger** : lignes 553 / 450 / 402 (+ 608), « chrome et vanadium ».
- **Spinelle bleu** (jeu : « spectre du cobalt : bandes à 537, 585 et 632 nm »). Ce spectre est celui du **spinelle bleu au cobalt** (surtout synthétique, Verneuil : bandes vers 540, 580, 635 nm selon le GIA). Un spinelle bleu naturel coloré par le fer montre plutôt une **bande vers 457-460 nm** (marqueur d'origine naturelle selon le GIA). **À préciser** : si l'espèce « Spinelle Bleu » est la pierre naturelle, ajouter le fer ; garder le cobalt pour la variante synthétique ou pour un spinelle naturel au cobalt (rare).
- **Pezzottaïte** (jeu : bande à 494 nm et petite bande à 563 nm). Cohérent avec la seule source trouvée (bande à 485-500 nm, faibles raies à 465 et 477 nm, faible bande à 550-580 nm). Source : un résumé Wikipedia, à confirmer.

Les autres spectres (rubis, émeraude, spinelle rouge, alexandrite, chrysobéryl, saphirs, péridot, almandin, jadéite, apatite, zircon, cobalt, verres, béryl maxixe…) sont cohérents avec les valeurs usuelles de la littérature.

## C. Pléochroïsme

- **Calcite** : marquée pléochroïque (« faible, selon la couleur »). Je n'ai trouvé aucune source qui le confirme : elle est très biréfringente (dédoublement de l'image) mais son pléochroïsme est nul à très faible. **À confirmer dans le cours.**
- **Orthose** : marquée pléochroïque : la littérature dit « nul, ou faible pour la variété jaune » (GIA/Cameo). Correct.
- **Diopside chromifère** : décrit « marqué » ; la littérature dit plutôt faible à modéré. Nuance mineure.
- Les autres indicateurs (oui/non et forces décrites) sont cohérents : andalousite, iolite, tanzanite, alexandrite, saphir vert, kunzite, béryls, apatite, saphirs, quartz colorés, topaze, zircon, etc.

## D. Filtre Chelsea

Rien d'incohérent : rubis et rubis de synthèse (rouge vif), alexandrite et démantoïde (rose à rouge vif), émeraude (rouge ou rose, ou verdâtre), spinelle bleu synthétique au cobalt (rouge vif), verre et quartz synthétique au cobalt (rouge), saphir bleu (vert foncé), aigue-marine (bleu-vert), jadéite (vert terne ; teintée : rougeâtre), almandin et pyrope (pas de rouge vif), iolite (parfois rose pâle). Le tableau vient du diplôme.

## E. Tableau complet (valeur du jeu, plage de la littérature, ✓ dans la plage)

| Espèce | IR du jeu (plage) | Densité (plage) | Dureté (plage) | Biréfringence (plage) |
|---|---|---|---|---|
| Agate | 1.535 (1.53 – 1.545) ✓ | 2.62 (2.55 – 2.7) ✓ | 6.5 (6.5 – 7) ✓ | 0.009 (0 – 0.009) ✓ |
| Aigue-marine | 1.577 (1.567 – 1.59) ✓ | 2.72 (2.66 – 2.8) ✓ | 7.5 (7.5 – 8) ✓ | 0.006 (0.004 – 0.009) ✓ |
| Ambre | 1.54 (1.539 – 1.545) ✓ | 1.08 (1 – 1.3) ✓ | 2.5 (2 – 2.5) ✓ | 0 (0 – 0.002) ✓ |
| Améthyste | 1.544 (1.544 – 1.553) ✓ | 2.65 (2.63 – 2.66) ✓ | 7 (7) ✓ | 0.009 (0.009) ✓ |
| Amétrine | 1.544 (1.544 – 1.553) ✓ | 2.65 (2.63 – 2.66) ✓ | 7 (7) ✓ | 0.009 (0.009) ✓ |
| Andalousite | 1.635 (1.629 – 1.65) ✓ | 3.17 (3.13 – 3.22) ✓ | 7.25 (6.5 – 7.5) ✓ | 0.01 (0.007 – 0.013) ✓ |
| Apatite | 1.635 (1.628 – 1.65) ✓ | 3.19 (3.1 – 3.35) ✓ | 5 (5) ✓ | 0.003 (0.002 – 0.008) ✓ |
| Béryl bleu (maxixe) | 1.58 (1.565 – 1.602) ✓ | 2.72 (2.66 – 2.9) ✓ | 7.5 (7.5 – 8) ✓ | 0.007 (0.004 – 0.009) ✓ |
| Béryl Rouge (Bixbite) | 1.57 (1.56 – 1.58) ✓ | 2.68 (2.66 – 2.72) ✓ | 7.5 (7.5 – 8) ✓ | 0.006 (0.004 – 0.008) ✓ |
| CZ | 2.16 (2.15 – 2.18) ✓ | 5.8 (5.6 – 6) ✓ | 8.25 (8 – 8.5) ✓ | 0 (0) ✓ |
| Calcite | 1.55 (1.486 – 1.658) ✓ | 2.71 (2.6 – 2.8) ✓ | 3 (3) ✓ | 0.172 (0.172) ✓ |
| Célestine | 1.625 (1.619 – 1.637) ✓ | 3.96 (3.95 – 3.97) ✓ | 3.25 (3 – 3.5) ✓ | 0.011 (0.009 – 0.012) ✓ |
| Chrysobéryl | 1.745 (1.745 – 1.759) ✓ | 3.73 (3.68 – 3.78) ✓ | 8.5 (8 – 8.5) ✓ | 0.009 (0.008 – 0.011) ✓ |
| Chrysobéryl Alexandrite | 1.745 (1.745 – 1.759) ✓ | 3.73 (3.68 – 3.78) ✓ | 8.5 (8 – 8.5) ✓ | 0.009 (0.008 – 0.011) ✓ |
| Citrine | 1.544 (1.544 – 1.553) ✓ | 2.65 (2.63 – 2.66) ✓ | 7 (7) ✓ | 0.009 (0.009) ✓ |
| Corail | 1.56 (1.486 – 1.658) ✓ | 2.68 (2.6 – 2.7) ✓ | 3.5 (3 – 4) ✓ | 0.17 (0 – 0.172) ✓ |
| Corindon (Rubis) | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Corindon Synthétique (Rubis) | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Corindon Synthétique (Saphir) | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Cornaline | 1.535 (1.53 – 1.545) ✓ | 2.6 (2.55 – 2.7) ✓ | 6.75 (6.5 – 7) ✓ | 0.009 (0 – 0.009) ✓ |
| Corne de Rhinocéros | 1.55 (1.53 – 1.57) ✓ | 1.3 (1.2 – 1.4) ✓ | 2.5 (2 – 3) ✓ | 0 (0 – 0.01) ✓ |
| Défense de Narval | 1.54 (1.535 – 1.541) ✓ | 1.9 (1.7 – 2) ✓ | 2.75 (2 – 3) ✓ | 0.003 (0 – 0.005) ✓ |
| Diamant | 2.417 (2.417 – 2.419) ✓ | 3.52 (3.5 – 3.53) ✓ | 10 (10) ✓ | 0 (0) ✓ |
| Diamant Synthétique (CVD) | 2.417 (2.417 – 2.419) ✓ | 3.52 (3.5 – 3.53) ✓ | 10 (10) ✓ | 0 (0) ✓ |
| Diamant Synthétique (HPHT) | 2.417 (2.417 – 2.419) ✓ | 3.52 (3.5 – 3.53) ✓ | 10 (10) ✓ | 0 (0) ✓ |
| Diopside Chromifère | 1.685 (1.664 – 1.73) ✓ | 3.29 (3.22 – 3.38) ✓ | 5.75 (5.5 – 6.5) ✓ | 0.029 (0.024 – 0.03) ✓ |
| Écaille de Tortue | 1.55 (1.54 – 1.56) ✓ | 1.29 (1.25 – 1.35) ✓ | 2.5 (2 – 3) ✓ | 0.004 (0 – 0.01) ✓ |
| Émeraude | 1.585 (1.565 – 1.602) ✓ | 2.72 (2.67 – 2.78) ✓ | 7.5 (7.5 – 8) ✓ | 0.007 (0.005 – 0.009) ✓ |
| Fluorite | 1.434 (1.433 – 1.435) ✓ | 3.1 (3 – 3.25) ✓ | 4 (4) ✓ | 0 (0) ✓ |
| Fluorite à bandes de couleur | 1.434 (1.433 – 1.435) ✓ | 3.1 (3 – 3.25) ✓ | 4 (4) ✓ | 0 (0) ✓ |
| Fluorite bleue | 1.434 (1.433 – 1.435) ✓ | 3.1 (3 – 3.25) ✓ | 4 (4) ✓ | 0 (0) ✓ |
| Blue John | 1.434 (1.433 – 1.435) ✓ | 3.1 (3 – 3.25) ✓ | 4 (4) ✓ | 0 (0) ✓ |
| Fluorite jaune | 1.434 (1.433 – 1.435) ✓ | 3.1 (3 – 3.25) ✓ | 4 (4) ✓ | 0 (0) ✓ |
| Fluorite verte | 1.434 (1.433 – 1.435) ✓ | 3.1 (3 – 3.25) ✓ | 4 (4) ✓ | 0 (0) ✓ |
| GGG | 1.97 (1.96 – 1.98) ✓ | 7.05 (7.02 – 7.09) ✓ | 6.5 (6 – 7) ✓ | 0 (0) ✓ |
| Goshénite | 1.577 (1.565 – 1.602) ✓ | 2.72 (2.66 – 2.9) ✓ | 7.5 (7.5 – 8) ✓ | 0.006 (0.004 – 0.009) ✓ |
| Grenat Almandin | 1.815 (1.75 – 1.83) ✓ | 4.05 (3.8 – 4.3) ✓ | 7.5 (7 – 7.5) ✓ | 0 (0) ✓ |
| Grenat Démantoïde | 1.885 (1.88 – 1.9) ✓ | 3.85 (3.8 – 3.9) ✓ | 6.75 (6.5 – 7) ✓ | 0 (0) ✓ |
| Grenat Hessonite | 1.74 (1.73 – 1.76) ✓ | 3.68 (3.57 – 3.73) ✓ | 6.75 (6.5 – 7.5) ✓ | 0 (0) ✓ |
| Grenat Hydrogrossulaire | 1.72 (1.7 – 1.73) ✓ | 3.47 (3.2 – 3.6) ✓ | 7 (6.5 – 7) ✓ | 0 (0) ✓ |
| Grenat Malaya | 1.755 (1.73 – 1.79) ✓ | 3.84 (3.65 – 3.9) ✓ | 7.25 (7 – 7.5) ✓ | 0 (0) ✓ |
| Grenat Pyrope | 1.74 (1.714 – 1.76) ✓ | 3.78 (3.65 – 3.8) ✓ | 7.25 (7 – 7.5) ✓ | 0 (0) ✓ |
| Grenat Rhodolite | 1.76 (1.74 – 1.77) ✓ | 3.84 (3.65 – 3.9) ✓ | 7.25 (7 – 7.5) ✓ | 0 (0) ✓ |
| Grenat Spessartite | 1.8 (1.79 – 1.81) ✓ | 4.15 (4.12 – 4.2) ✓ | 7.5 (7 – 7.5) ✓ | 0 (0) ✓ |
| Grenat Tsavorite | 1.74 (1.73 – 1.75) ✓ | 3.61 (3.57 – 3.7) ✓ | 7.25 (7 – 7.5) ✓ | 0 (0) ✓ |
| Uvarovite | 1.86 (1.8 – 1.87) ✓ | 3.77 (3.7 – 3.8) ✓ | 7.25 (6.5 – 7.5) ✓ | 0 (0) ✓ |
| Gypse | 1.525 (1.519 – 1.53) ✓ | 2.31 (2.3 – 2.33) ✓ | 2 (2) ✓ | 0.01 (0.009 – 0.01) ✓ |
| Héliodore | 1.575 (1.565 – 1.602) ✓ | 2.7 (2.66 – 2.9) ✓ | 7.5 (7.5 – 8) ✓ | 0.007 (0.004 – 0.009) ✓ |
| Hématite | 2.94 (2.94 – 3.22) ✓ | 5.15 (4.9 – 5.3) ✓ | 6 (5 – 6.5) ✓ | 0 (0 – 0.28) ✓ |
| Iolite | 1.545 (1.522 – 1.578) ✓ | 2.59 (2.57 – 2.66) ✓ | 7.25 (7 – 7.5) ✓ | 0.01 (0.008 – 0.012) ✓ |
| Iolite bloodshot | 1.545 (1.522 – 1.578) ✓ | 2.59 (2.57 – 2.66) ✓ | 7.25 (7 – 7.5) ✓ | 0.01 (0.008 – 0.012) ✓ |
| Ivoire d'Éléphant | 1.54 (1.535 – 1.541) ✓ | 1.85 (1.7 – 2) ✓ | 2.75 (2 – 3) ✓ | 0.003 (0 – 0.005) ✓ |
| Ivoire d'Hippopotame | 1.54 (1.535 – 1.541) ✓ | 1.9 (1.7 – 2) ✓ | 3 (2 – 3) ✓ | 0.003 (0 – 0.005) ✓ |
| Ivoire de Mammouth | 1.54 (1.535 – 1.541) ✓ | 1.9 (1.7 – 2) ✓ | 2.75 (2 – 3) ✓ | 0.003 (0 – 0.005) ✓ |
| Ivoire de Morse | 1.54 (1.535 – 1.541) ✓ | 1.95 (1.7 – 2) ✓ | 2.9 (2 – 3) ✓ | 0.003 (0 – 0.005) ✓ |
| Ivoire végétal (corozo) | 1.54 (1.53 – 1.54) ✓ | 1.4 (1.38 – 1.5) ✓ | 2.5 (2.5 – 3) ✓ | 0 (0 – 0.005) ✓ |
| Jade Jadéite | 1.665 (1.64 – 1.667) ✓ | 3.34 (3.2 – 3.4) ✓ | 6.75 (6.5 – 7) ✓ | 0.012 (0.009 – 0.013) ✓ |
| Jade Néphrite | 1.615 (1.6 – 1.641) ✓ | 2.95 (2.9 – 3.1) ✓ | 6.25 (6 – 6.5) ✓ | 0.027 (0.02 – 0.027) ✓ |
| Jais | 1.66 (1.64 – 1.68) ✓ | 1.32 (1.3 – 1.35) ✓ | 3 (2.5 – 4) ✓ | 0 (0) ✓ |
| Kunzite | 1.665 (1.66 – 1.676) ✓ | 3.18 (3.16 – 3.23) ✓ | 6.75 (6.5 – 7) ✓ | 0.015 (0.014 – 0.016) ✓ |
| Kyanite | 1.72 (1.71 – 1.735) ✓ | 3.68 (3.53 – 3.69) ✓ | 5.5 (4 – 7.5) ✓ | 0.017 (0.012 – 0.017) ✓ |
| Labradorite | 1.563 (1.559 – 1.573) ✓ | 2.7 (2.65 – 2.75) ✓ | 6.25 (6 – 6.5) ✓ | 0.009 (0.007 – 0.01) ✓ |
| Lapis-lazuli | 1.5 (1.5 – 1.52) ✓ | 2.8 (2.7 – 2.95) ✓ | 5.5 (5 – 5.5) ✓ | 0 (0) ✓ |
| Malachite | 1.88 (1.655 – 1.909) ✓ | 3.8 (3.25 – 4.1) ✓ | 3.75 (3.5 – 4) ✓ | 0.25 (0.2 – 0.254) ✓ |
| Moissanite Synthétique | 2.67 (2.648 – 2.691) ✓ | 3.21 (3.2 – 3.24) ✓ | 9.25 (9.25 – 9.5) ✓ | 0.043 (0.043) ✓ |
| Moldavite | 1.5 (1.48 – 1.54) ✓ | 2.35 (2.32 – 2.38) ✓ | 5.5 (5.5 – 6) ✓ | 0 (0) ✓ |
| Morganite | 1.585 (1.572 – 1.6) ✓ | 2.8 (2.71 – 2.9) ✓ | 7.5 (7.5 – 8) ✓ | 0.008 (0.004 – 0.009) ✓ |
| Obsidienne | 1.49 (1.48 – 1.51) ✓ | 2.45 (2.33 – 2.6) ✓ | 5.25 (5 – 5.5) ✓ | 0 (0) ✓ |
| Obsidienne flocons de neige | 1.5 (1.48 – 1.51) ✓ | 2.4 (2.33 – 2.6) ✓ | 5.25 (5 – 5.5) ✓ | 0 (0) ✓ |
| Œil-de-tigre | 1.544 (1.544 – 1.553) ✓ | 2.65 (2.64 – 2.71) ✓ | 7 (6.5 – 7) ✓ | 0.009 (0 – 0.009) ✓ |
| Opale de Feu | 1.43 (1.37 – 1.47) ✓ | 2 (1.9 – 2.3) ✓ | 6 (5.5 – 6.5) ✓ | 0 (0) ✓ |
| Opale Noble | 1.45 (1.37 – 1.47) ✓ | 2.1 (1.9 – 2.3) ✓ | 6 (5.5 – 6.5) ✓ | 0 (0) ✓ |
| Orthose | 1.522 (1.518 – 1.533) ✓ | 2.56 (2.55 – 2.6) ✓ | 6.25 (6 – 6.5) ✓ | 0.008 (0.005 – 0.008) ✓ |
| Padparadscha | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Painite | 1.8 (1.787 – 1.816) ✓ | 4.01 (3.99 – 4.03) ✓ | 8 (8) ✓ | 0.022 (0.027 – 0.031) **✗** |
| Péridot | 1.67 (1.64 – 1.71) ✓ | 3.34 (3.28 – 3.48) ✓ | 6.5 (6.5 – 7) ✓ | 0.036 (0.033 – 0.038) ✓ |
| Perle de culture | 1.6 (1.52 – 1.69) ✓ | 2.72 (2.6 – 2.85) ✓ | 3 (2.5 – 4.5) ✓ | 0.155 (0 – 0.155) ✓ |
| Perle de Tahiti | 1.6 (1.52 – 1.69) ✓ | 2.75 (2.6 – 2.85) ✓ | 3 (2.5 – 4.5) ✓ | 0.155 (0 – 0.155) ✓ |
| Pezzottaite | 1.615 (1.601 – 1.62) ✓ | 3.05 (3.09 – 3.13) **✗** | 8 (8) ✓ | 0.0095 (0.007 – 0.011) ✓ |
| Pierre de Lune | 1.525 (1.518 – 1.526) ✓ | 2.57 (2.55 – 2.59) ✓ | 6.25 (6 – 6.5) ✓ | 0.006 (0.005 – 0.008) ✓ |
| Prasiolite | 1.544 (1.544 – 1.553) ✓ | 2.65 (2.63 – 2.66) ✓ | 7 (7) ✓ | 0.009 (0.009) ✓ |
| Préhnite | 1.62 (1.611 – 1.665) ✓ | 2.88 (2.8 – 2.95) ✓ | 6.25 (6 – 6.5) ✓ | 0.03 (0.02 – 0.035) ✓ |
| Pyrite | 1.85 (1.75 – 1.95) ✓ | 5 (4.95 – 5.1) ✓ | 6.25 (6 – 6.5) ✓ | 0 (0) ✓ |
| Quartz | 1.544 (1.544 – 1.553) ✓ | 2.65 (2.63 – 2.66) ✓ | 7 (7) ✓ | 0.009 (0.009) ✓ |
| Quartz Fumé | 1.544 (1.544 – 1.553) ✓ | 2.65 (2.63 – 2.66) ✓ | 7 (7) ✓ | 0.009 (0.009) ✓ |
| Quartz Rose | 1.544 (1.544 – 1.553) ✓ | 2.65 (2.63 – 2.66) ✓ | 7 (7) ✓ | 0.009 (0.009) ✓ |
| Rhodocrosite | 1.6 (1.597 – 1.817) ✓ | 3.75 (3.4 – 3.71) **✗** | 3.75 (3.5 – 4) ✓ | 0.22 (0.218 – 0.22) ✓ |
| Rhodonite | 1.735 (1.711 – 1.752) ✓ | 3.55 (3.4 – 3.75) ✓ | 6 (5.5 – 6.5) ✓ | 0.012 (0.01 – 0.014) ✓ |
| Saphir Bleu | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Saphir Incolore | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Saphir Jaune | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Saphir Orange | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Saphir Rose | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Saphir Vert | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Saphir Violet | 1.766 (1.759 – 1.781) ✓ | 4 (3.95 – 4.1) ✓ | 9 (9) ✓ | 0.008 (0.008 – 0.009) ✓ |
| Scapolite | 1.555 (1.54 – 1.579) ✓ | 2.7 (2.6 – 2.74) ✓ | 6.25 (5.5 – 6.5) ✓ | 0.021 (0.004 – 0.037) ✓ |
| Sélénite | 1.52 (1.519 – 1.53) ✓ | 2.32 (2.3 – 2.33) ✓ | 2 (2) ✓ | 0.01 (0.009 – 0.01) ✓ |
| Serpentine Bowénite | 1.555 (1.55 – 1.57) ✓ | 2.6 (2.5 – 2.7) ✓ | 5.5 (2.5 – 6) ✓ | 0.014 (0.005 – 0.015) ✓ |
| Sinhalite | 1.68 (1.668 – 1.707) ✓ | 3.48 (3.46 – 3.5) ✓ | 6.75 (6.5 – 7) ✓ | 0.038 (0.036 – 0.039) ✓ |
| Sodalite | 1.485 (1.483 – 1.487) ✓ | 2.27 (2.27 – 2.33) ✓ | 5.75 (5.5 – 6) ✓ | 0 (0) ✓ |
| Sphalérite | 2.369 (2.369 – 2.371) ✓ | 4.05 (3.9 – 4.1) ✓ | 3.75 (3.5 – 4) ✓ | 0 (0) ✓ |
| Sphène (titanite) | 1.96 (1.885 – 2.05) ✓ | 3.53 (3.4 – 3.55) ✓ | 5.25 (5 – 5.5) ✓ | 0.12 (0.1 – 0.192) ✓ |
| Spinelle | 1.715 (1.712 – 1.762) ✓ | 3.6 (3.54 – 3.63) ✓ | 8 (8) ✓ | 0 (0) ✓ |
| Spinelle Bleu | 1.715 (1.712 – 1.762) ✓ | 3.61 (3.54 – 3.63) ✓ | 8 (8) ✓ | 0 (0) ✓ |
| Stéatite | 1.57 (1.54 – 1.59) ✓ | 2.75 (2.7 – 2.8) ✓ | 1 (1) ✓ | 0.04 (0.03 – 0.05) ✓ |
| Téfféite | 1.72 (1.717 – 1.73) ✓ | 3.61 (3.6 – 3.62) ✓ | 8.25 (8 – 8.5) ✓ | 0.005 (0.004 – 0.005) ✓ |
| Tanzanite | 1.695 (1.691 – 1.7) ✓ | 3.35 (3.3 – 3.4) ✓ | 6.5 (6.5 – 7) ✓ | 0.009 (0.008 – 0.013) ✓ |
| Titanate de strontium (fabulite) | 2.41 (2.4 – 2.41) ✓ | 5.13 (5.12 – 5.14) ✓ | 5.5 (5.5 – 6) ✓ | 0 (0) ✓ |
| Topaze | 1.62 (1.606 – 1.644) ✓ | 3.53 (3.49 – 3.57) ✓ | 8 (8) ✓ | 0.01 (0.008 – 0.011) ✓ |
| Tourmaline indicolite | 1.635 (1.61 – 1.675) ✓ | 3.06 (3 – 3.25) ✓ | 7.25 (7 – 7.5) ✓ | 0.02 (0.014 – 0.04) ✓ |
| Tourmaline melon d'eau | 1.63 (1.61 – 1.675) ✓ | 3.06 (3 – 3.25) ✓ | 7.25 (7 – 7.5) ✓ | 0.018 (0.014 – 0.04) ✓ |
| Tourmaline Paraïba | 1.625 (1.61 – 1.675) ✓ | 3.06 (3 – 3.25) ✓ | 7.25 (7 – 7.5) ✓ | 0.018 (0.014 – 0.04) ✓ |
| Tourmaline Rose | 1.625 (1.61 – 1.675) ✓ | 3.06 (3 – 3.25) ✓ | 7.25 (7 – 7.5) ✓ | 0.018 (0.014 – 0.04) ✓ |
| Tourmaline schorl | 1.655 (1.61 – 1.675) ✓ | 3.15 (3 – 3.25) ✓ | 7.25 (7 – 7.5) ✓ | 0.025 (0.014 – 0.04) ✓ |
| Turquoise | 1.62 (1.61 – 1.65) ✓ | 2.8 (2.6 – 2.9) ✓ | 5.5 (5 – 6) ✓ | 0 (0) ✓ |
| Turquoise Stabilisée | 1.62 (1.61 – 1.65) ✓ | 2.78 (2.4 – 2.9) ✓ | 5.25 (4.5 – 6) ✓ | 0 (0) ✓ |
| Verre | 1.5 (1.45 – 1.7) ✓ | 2.5 (2.2 – 4.5) ✓ | 5.5 (5 – 6) ✓ | 0 (0) ✓ |
| YAG | 1.833 (1.82 – 1.84) ✓ | 4.57 (4.55 – 4.6) ✓ | 8.25 (8 – 8.5) ✓ | 0 (0) ✓ |
| Zircon | 1.95 (1.78 – 1.99) ✓ | 4.65 (3.9 – 4.73) ✓ | 7.5 (6 – 7.5) ✓ | 0.059 (0 – 0.059) ✓ |
| Zoïsite | 1.7 (1.685 – 1.725) ✓ | 3.35 (3.15 – 3.36) ✓ | 6.5 (6 – 7) ✓ | 0.009 (0.008 – 0.013) ✓ |

## Comment appliquer les corrections

Les corrections sont prêtes dans `Docs/corrections_proprietes.json` (avec la raison de chacune). Le script les applique aux données du jeu :

```
python3 tools/apply_property_corrections.py                 # montre ce qui changerait, n'écrit rien
python3 tools/apply_property_corrections.py --write         # applique
python3 tools/apply_property_corrections.py --also D1       # applique aussi la nuance facultative du diopside
```

- **Appliquées par défaut** : P1 (painite, biréfringence 0,029), P2 (pezzottaïte, densité 3,10), P3 (rhodocrosite, densité 3,65), S1 (spectre de la painite : nouvelle entrée dans `Resources/Spectra/spectra.json`).
- **Facultative** : D1 (pléochroïsme du diopside chromifère : « faible à modéré »).
- **Décisions, non appliquées** : S2 (spinelle bleu : fer ou cobalt) et Q1 (pléochroïsme de la calcite). Elles ne changent rien tant que tu ne tranches pas.
- Le script vérifie que la valeur actuelle est bien celle attendue avant de la remplacer : il ne casse rien si une correction a déjà été faite à la main.
- **Après application** : `python3 tools/export_game_data.py`, puis `python3 site/tools/sync_data.py`. Le nouveau texte du spectre de la painite sera à traduire en anglais (`python3 site/tools/species_strings.py` le liste).
- **Dans Unity** : les densités et la biréfringence servent aux mesures de la balance hydrostatique et du réfractomètre ; regarder une pezzottaïte, une rhodocrosite et une painite dans ces deux instruments, et le spectre de la painite au spectroscope.

## Sources

- Painite : [Gem Society](https://www.gemsociety.org/article/painite-jewelry-and-gemstone-information/), [Skyjems](https://skyjems.ca/pages/encyclopedia-burmese-painite), [New data on painite, Mineralogical Magazine 1986](https://rruff.info/doclib/MinMag/Volume_50/50-356-267.htm).
- Rhodocrosite : [Gemdat](https://www.gemdat.org/gem-3406.html), [Wikipedia](https://en.wikipedia.org/wiki/Rhodochrosite), [Gemstones.com](https://gemstones.com/gemopedia/rhodochrosite).
- Pezzottaïte : [Wikipedia](https://en.wikipedia.org/wiki/Pezzottaite), [Skyjems](https://skyjems.ca/pages/encyclopedia-pezzottaite).
- Orthose : [Cameo (MFA)](https://cameo.mfa.org/wiki/Orthoclase).
- Hessonite : [Wikipedia, grossulaire](https://en.wikipedia.org/wiki/Grossular), [Edelsteine.at](https://www.edelsteine.at/en/glossaries/lexicon/hessonite-garnet/).
- Spinelle : [GIA, spinelle bleu cobalt](https://www.gia.edu/doc/Cobalt-Blue-Gem-Spinels.pdf), [GIA, Shigley 1984](https://gia.edu/gems-gemology/spring-1984-spinel-shigley), [GIA 2021](https://gia.edu/gems-gemology/fall-2021-gemnews-spectroscopic-low-temp-heated-spinel).
