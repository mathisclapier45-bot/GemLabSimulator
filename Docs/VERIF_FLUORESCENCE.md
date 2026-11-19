# Vérification des fluorescences aux UV des 120 espèces

Date : 19 novembre 2026. Données vérifiées : `site/data/gemlab_data.json` (copie des données du jeu), champs `fluorescenceLongWave` / `fluorescenceShortWave` de chaque espèce, et les 33 entrées du bloc `fluorescence` (texte, couleur, intensité 0-1, phosphorescence).

**Aucune donnée n'a été modifiée.** Ce document ne fait que signaler les écarts. Les corrections se font dans le jeu (`GemLabSimulator/`), pas sur le site : la copie du site est écrasée à chaque `sync_data.py`.

## Comment j'ai vérifié

- Chaque espèce a été relue une par une et comparée à ce que je connais des références de gemmologie (GIA, Webster, Gübelin, tables de gemmologie).
- Les points douteux ont été recherchés sur le web (articles GIA *Gems & Gemology*, bases de minéraux fluorescents, sites de gemmologie). Les liens sont en bas.
- Les textes des entrées viennent du **diplôme de gemmologie 2019** (p. 48-49, p. 121). Un écart avec la littérature n'est donc pas forcément une erreur : le cours peut simplifier. Je signale, tu décides.
- **Limites** : je n'ai pas eu accès aux livres de référence (Webster, Nassau, Hughes), seulement à des articles et à des bases en ligne, de qualité inégale. La fluorescence varie d'un échantillon à l'autre (origine, fer, traitement) : « variable » est souvent la bonne réponse.

## Résultat en bref

- **2 erreurs nettes** (diamants synthétiques HPHT et CVD : le sens SW/LW est inversé).
- **7 points à corriger ou à nuancer** (zircon, kunzite, spinelle rouge, rubis, alexandrite, émeraude, saphir bleu de synthèse).
- **4 nuances mineures**.
- **Les ~90 autres espèces** (pierres inertes : grenats, tourmalines, quartz, béryls, jades, etc.) sont cohérentes avec la littérature.
- Les textes des champs de l'espèce et ceux des entrées sont identiques quand les deux existent : aucune contradiction interne.

## A. Erreurs probables à corriger

### 1. Diamant synthétique HPHT (`Diamant_Synthetique_HPHT`)
- **Dans le jeu** : LW « fluorescence variable ; phosphorescence prolongée… », SW « fluorescence plus faible sous UV courte onde ».
- **Littérature** : les HPHT réagissent **plus fort en onde courte qu'en onde longue**. Exemple GIA : aucune réaction en LW, vert-jaune moyen à fort en SW, avec phosphorescence jaune-vert de plus de 20 secondes. C'est même un indice de synthèse, à l'inverse d'un diamant naturel (bleu en LW, plus faible en SW).
- **À corriger** : LW « inerte à faible », SW « vert-jaune à jaune-vert, plus forte qu'en LW », phosphorescence souvent présente.

### 2. Diamant synthétique CVD (`Diamant_Synthetique_CVD`)
- **Dans le jeu** : LW « orangée faible parfois », SW « plus faible ».
- **Littérature** : pour les CVD aussi, la réaction **SW est plus forte que la LW** (orange-rouge dû au centre NV0, ou vert-bleu après traitement HPHT). Phosphorescence très faible.
- **À corriger** : SW au moins aussi forte que LW (orangé à orange-rouge).

## B. À corriger ou à nuancer

### 3. Zircon (`Zircon`, `Zircon_Vert`)
- **Jeu** : LW « jaune-brunâtre », SW « aucune réaction ».
- **Littérature** : jaune moutarde / jaune-orangé **typique en SW**, jaune terne en LW, parfois phosphorescent ; ou inerte, selon la pierre. « Aucune réaction » en SW est trop absolu.
- **Proposition** : « Variable, de nulle à jaune moutarde/orangé ; souvent plus visible en onde courte ».
- **Bonus pour les leçons** : le zircon bleu chauffé **brunit sous UV long** en moins de 6 minutes et redevient bleu à la lumière d'une lampe à incandescence (étude GIA 2016).

### 4. Kunzite (`Kunzite`)
- **Jeu** : LW « orange », SW « aucune réaction ».
- **Littérature** : orange (rose à orange) en LW, **plus faible mais présente en SW** (orange ou violet selon l'échantillon) ; phosphorescence fréquente de quelques secondes à quelques minutes.
- **Proposition** : SW « plus faible, orangée ou violacée » ; ajouter phosphorescente = vrai.

### 5. Spinelle rouge (`Spinelle`)
- **Jeu** : rouge (chrome), intensité identique en LW et en SW (0,75).
- **Littérature** : la réaction est **la plus forte en LW, faible à nulle en SW**.
- **Proposition** : SW autour de 0,25-0,3.

### 6. Rubis naturel / synthétique (`Corindon_Rubis`, `Corindon_Synthetique_Rubis`)
- **Jeu** : rouge vif, 0,9 en LW **et** en SW, « naturel ou synthétique : la réaction est la même ».
- **Littérature** : le rubis naturel réagit plus en LW qu'en SW ; les rubis **riches en fer** (Thaïlande, Cambodge) sont faibles à inertes, les rubis pauvres en fer (Mogok) très forts ; les Verneuil sont souvent d'un rouge plus vif que les naturels.
- **Proposition** : SW plus faible que LW pour le naturel (0,6), et mention « faible à inerte si riche en fer ». Le message « la fluorescence ne distingue pas naturel et synthétique » reste juste.

### 7. Alexandrite (`Chrysoberyl_Alexandrite`)
- **Jeu** : rouge (chrome), 0,7 en LW **et** en SW.
- **Littérature** : rouge en LW (confirmé) ; je n'ai pas trouvé de source fiable pour la SW. De mémoire (Webster), la SW est inerte à faible. **À vérifier avant de modifier.**

### 8. Émeraude (`Emeraude`)
- **Jeu** : « Rouge ou vert, naturelle ou synthétique », 0,5.
- **Littérature** : la plupart des émeraudes naturelles sont **inertes ou faibles** (le fer éteint la fluorescence) ; les synthétiques (hydrothermales, au flux) sont plus rouges. Une émeraude naturelle fine peut aussi réagir en orange-rouge (GIA). Je n'ai trouvé aucune source pour « vert ».
- **Proposition** : naturelle « inerte à faible rouge », synthétique « rouge plus vive » ; retirer « vert » sauf si le cours le dit.

### 9. Saphir bleu de synthèse (`Corindon_Synthetique_Saphir`)
- **Jeu** : SW « vert », intensité 0,7.
- **Littérature** : SW « chalky » (crayeux) bleu à bleu-vert ou blanchâtre, **inerte à faible** selon les cas.
- **Proposition** : « crayeux bleu-vert, faible à moyen » et intensité ~0,45. Le saphir naturel « parfois un vert faible » en SW est confirmé (saphirs de Madagascar surtout).

## C. Nuances mineures

- **Scapolite** : le jeu donne LW « jaunâtre », SW « rougeâtre ». Les sources montrent que ça **dépend de l'origine** : jaune fort en LW et faible en SW (Afghanistan), rose/magenta en LW et rouge-orange en SW (Inde, Tanzanie). À écrire comme « variable selon l'origine ».
- **Saphir jaune naturel** : SW « aucune réaction » ; la littérature donne orange faible à moyen en SW. Acceptable si le cours le dit.
- **Opale noble** : « blanc à vert » à 0,5 pour LW et SW. La LW est souvent plus forte que la SW (GIA : fort en LW, faible en SW), et la phosphorescence verte est possible. Le blanc à bleu-blanc existe aussi.
- **Turquoise** : LW « possible faible », SW « non applicable (minéral opaque) ». Incohérent : une pierre opaque peut réagir en surface en LW comme en SW. Même remarque pour la turquoise stabilisée.

## D. À confirmer dans le cours (aucune source trouvée)

- **GGG** et **YAG** (« variable »), **pezzottaïte** (« aucune »), **painite** (« inerte »), **titanate de strontium** : je n'ai pas trouvé de source fiable en ligne ; les textes du jeu sont prudents (« variable ou absente »), donc peu risqués.

## Ce qui est confirmé

Diamant naturel (bleu LW, plus faible SW) · spinelle bleu synthétique au cobalt (rouge LW, blanc bleuté SW, signe de synthèse selon le GIA) · spinelle bleu naturel (faible, le fer éteint le cobalt) · ambre · ivoires · perles · fluorite (LW > SW) · calcite (SW plus vive, manganèse) · sodalite/hackmanite (orange, SW plus vive) · sphalérite · lapis (calcite rose-orangé) · corail · verre · CZ · apatite · topaze · padparadscha · kunzite en LW · saphir rose · et toutes les pierres inertes (grenats, tourmalines, quartz et variétés, béryls, jades, iolite, péridot, tanzanite, sphène, etc.).

## Sources consultées

- GIA, [caractéristiques des diamants synthétiques (été 2018)](https://www.gia.edu/gems-gemology/summer-2018-features-of-synthetic-diamonds) ; [HPHT de grande taille (printemps 2015)](https://www.gia.edu/gems-gemology/spring-2015-labnotes-large-hpht-grown-synthetic-diamonds) ; [phosphorescence HPHT (printemps 2023)](https://www.gia.edu/gems-gemology/spring-2023-gemnews-hpht-diamond-phosphorescence) ; [CVD orange (hiver 2014)](https://www.gia.edu/gems-gemology/winter-2014-labnotes-cvd-synthetic-diamond-fancy-vivid-orange-color) ; [dépistage des synthétiques](https://www.jewellerybusiness.com/features/a-closer-look-screening-for-synthetic-diamonds-using-standard-gem-instruments/4/).
- Kunzite : [Gemdat](https://www.gemdat.org/gem-2289.html), [Fluorescent Mineral Database](https://uvminerals.org/fmdb/specimen/1327/), [Gem-A, focus sur la fluorescence](https://gem-a.com/gem-hub/focus-on-fluorescence/).
- Scapolite : [GIA 2014](https://gia.edu/gems-gemology/spring-2014-gemnews-tenebrescent-scapolite), [Fluorescent Mineral Database](https://uvminerals.org/fmdb/specimen/1535/).
- Spinelle : [GIA, spinelle bleu synthétique (hiver 2014)](https://www.gia.edu/gems-gemology/winter-2014-gemnews-short-wave-uv-reaction-synthetic-blue-spinel), [GIA, spinelle bleu craquelé (printemps 2016)](https://www.gia.edu/gems-gemology/spring-2016-labnotes-quench-crackled-blue-synthetic-spinel), [Ganoksin](https://www.ganoksin.com/lesson/gemstone-fluorescence/).
- Émeraude et alexandrite : [Ganoksin](https://www.ganoksin.com/lesson/gemstone-fluorescence/), [Gemmology](https://70073cc6.gemmology-dev.pages.dev/learn/phenomena/alexandrite-effect).
- Corindon : [Ruby-Sapphire.com, fluorescence UV](https://ruby-sapphire.com/articles/796-heat-seeker-uv-fluorescence), [Gemology Project, rubis](https://www.gemologyproject.com/wiki/index.php?title=Ruby), [Lotus Gemology, saphir jaune de synthèse](https://lotusgemology.com/resources/articles/333-a-sapphire-s-secret-synthetic-yellow-sapphire), [Gemstones.com, saphir](https://www.gemstones.com/gemopedia/sapphire).
- Zircon : [GIA, zircon bleu sous UV long (automne 2016)](https://www.gia.edu/gems-gemology/fall-2016-reversible-color-modification-blue-zircon-long-wave-ultraviolet-radiation), [Gem Society](https://www.gemsociety.org/article/zircon/).
- Opale : [GIA, opale réactive aux UV (printemps 2014)](https://gia.edu/gems-gemology/spring-2014-labnotes-uv-reactive-opal), [Fluorescent Mineral Database](https://uvminerals.org/glownotes/hobby/collecting-and-displaying-fluorescent-minerals-with-an-opal-emphasis/).

## Comment appliquer les corrections

Les corrections sont prêtes dans `Docs/corrections_fluorescence.json` (texte, couleur, intensité et phosphorescence de chaque entrée, avec la raison). Elles ne touchent que la table du jeu `Resources/Fluorescence/fluorescence.json`, que le jeu lit en priorité (les textes des `.asset` ne servent que pour les espèces sans entrée).

```
python3 tools/apply_fluorescence_corrections.py              # montre ce qui changerait, n'écrit rien
python3 tools/apply_fluorescence_corrections.py --write      # applique
python3 tools/apply_fluorescence_corrections.py --also C1 C3 # applique aussi des nuances facultatives
```

- **Appliquées par défaut** : A1, A2 (diamants synthétiques), B1 à B6 (zircon, kunzite, spinelle rouge, rubis, émeraude naturelle et synthétique, saphir bleu de synthèse).
- **Non appliquées par défaut** : B7 (alexandrite en onde courte, source manquante) et C1 à C4 (nuances facultatives). Elles s'ajoutent avec `--also`.
- **Deux créations** : `Diamant_Synthetique_HPHT`, `Diamant_Synthetique_CVD` (ces espèces n'avaient pas d'entrée) et `Emeraude_Synthetique` (une émeraude de synthèse du jeu prend cette entrée, comme pour les autres espèces synthétiques).
- **Après application** : `python3 tools/export_game_data.py`, puis pour le site `python3 site/tools/sync_data.py`. Les nouveaux textes français n'auront pas d'anglais tant qu'on ne les a pas traduits : `python3 site/tools/species_strings.py` les liste, et un test du site échoue jusqu'à ce que `site/data/en/species.json` les contienne.
- **Dans Unity** : rouvrir le jeu pour que la table soit relue, et regarder au moins le diamant synthétique et le zircon sous la lampe UV.
