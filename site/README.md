# Site web GemXP (première version, en local)

Un site en français autour du simulateur de gemmologie :

| Partie | Qui peut la voir |
|---|---|
| **Encyclopédie** : une page par espèce (120) avec composition chimique, système cristallin et axes, figure d'interférence, couleurs de pléochroïsme, lueur aux UV, spectre, gamme de couleurs ; recherche et filtres | tout le monde |
| **Leçons** : les 14 leçons du jeu et 8 leçons écrites pour le site (microscope, micromètre, aimant, traitements, testeurs de diamant, infrarouge et Raman, origine géographique, diamant au laboratoire), avec la leçon animée du réfractomètre et les schémas animés du simulateur | tout le monde |
| **Comptes** : inscription, vérification de l'e-mail, connexion, mot de passe oublié | tout le monde |
| **Forum** : catégories, sujets, réponses, signalement, modération | membres connectés |
| **Simulateur** (le build Unity) | membres connectés |

Le site **n'est pas référencé** par les moteurs de recherche (voir plus bas).
Style : moderne, clair ou sombre selon le réglage de l'ordinateur (`static/css/theme.css`, couleurs en variables en tête de fichier) ; l'accueil montre un saphir 3D qui tourne ; seule la fenêtre du simulateur a l'allure « vieux Windows XP ».

## 1. Lancer le site sur ton Mac

Il n'y a **rien à installer** : le site n'utilise que Python 3, déjà présent sur ton Mac.
Depuis le dossier du dépôt :

```bash
python3 site/tools/sync_simulator.py
```

```bash
python3 site/run.py
```

Puis ouvre <http://127.0.0.1:8000>. Pour arrêter : `Ctrl+C` dans le terminal.
(La première commande copie le simulateur dans le site ; elle n'est à refaire que si tu le reconstruis dans Unity.)

### Essayer les comptes sans vrai e-mail

En local, le site **n'envoie aucun vrai e-mail**. Les messages (vérification, mot de passe oublié) s'affichent sur
la page <http://127.0.0.1:8000/dev/courriels> : clique sur le lien dans le message, comme dans une vraie boîte aux lettres.
Cette page n'existe pas en mode « production ».

1. Clique sur « Créer un compte », remplis le formulaire.
2. Ouvre `/dev/courriels`, clique sur le lien de vérification.
3. Connecte-toi : tu peux écrire sur le forum et ouvrir le simulateur.

### Devenir administrateur

Crée d'abord ton compte sur le site, puis :

```bash
python3 site/manage.py make-admin ton-adresse@exemple.fr
```

Un administrateur peut épingler, verrouiller et supprimer des sujets, supprimer des messages, traiter les signalements
(page « Administration ») et bannir un membre. Autres commandes : `list-users`, `verify-email`.

## 2. Mettre à jour les données et le simulateur

**Encyclopédie et leçons** (après un changement dans le jeu) :

```bash
python3 site/tools/sync_data.py --export
```

Cela relance `tools/export_game_data.py` (qui relit les fichiers du jeu), puis copie le résultat et les 14 leçons du jeu dans
`site/data/`. Sans `--export`, on copie simplement les fichiers actuels. Le site relit les fichiers tout seul :
pas besoin de le redémarrer. Les 8 leçons propres au site sont dans `site/data/lessons_extra/` : la synchronisation
ne les touche pas (on les modifie à la main, comme les autres : un fichier JSON par leçon).

**Quiz, glossaire, comparaison.**
- *Quiz de 20 questions* : `/quiz` (présentation et choix du thème : tout le cours, notions de base, instruments), `/quiz/jouer` (le quiz) : 20 questions tirées au hasard dans toutes les banques
  de questions des leçons, au plus 2 par leçon, dans un ordre aléatoire et avec les réponses mélangées. La correction montre la bonne réponse et l'explication de chaque
  question, et renvoie vers les leçons à revoir. Les membres gardent l'historique (table `mixed_quiz`, visible dans `/progression`). Réglages : `MIXED_LENGTH`,
  `MIXED_PER_LESSON` dans `app/routes_learn.py`.
- *Recherche* : `/recherche?q=…` cherche dans les espèces, les leçons (titres et textes, avec un extrait surligné) et le glossaire ; une loupe dans l'en-tête y mène.
  La page n'est pas indexée par Google.
- *Quiz* : un fichier par leçon dans `site/data/quizzes/` (`{"lesson": "aimant", "questions": [{"q", "choices" (4), "answer" (0 à 3), "why"}]}`).
  À chaque essai, 5 questions sont tirées au hasard dans la banque de la leçon (6 à 9 questions), dans un ordre aléatoire et avec les réponses
  mélangées (`QUIZ_LENGTH` dans `app/routes_learn.py`) : on peut ajouter des questions au fichier JSON sans rien changer d'autre. Les bonnes réponses ne sont jamais
  envoyées au navigateur : le serveur corrige. Un visiteur peut faire le quiz, un membre garde son
  meilleur score (table `quiz_results`) et voit sa progression sur `/progression`. Un quiz est réussi à partir de 70 % (`PASS_RATIO`
  dans `app/routes_learn.py`).
- *Glossaire* : `site/data/glossary.json` (un mot, ses autres formes, sa définition, les leçons liées). La première mention de chaque mot
  est soulignée en pointillé dans les leçons et dans les fiches d'espèces, et renvoie à `/glossaire`.
- *Comparaison* : `/comparer?a=...&b=...` met deux espèces côte à côte, colore les lignes qui diffèrent et liste les instruments qui les
  départagent (seuils dans `_verdicts`, `app/routes_learn.py`).

**Schémas animés des leçons** : ce sont les dessins du jeu (`LessonFigures.cs`), refaits en JavaScript dans
`static/js/lessonfig*.js`. Chaque étape d'une leçon nomme sa figure et son « focus » dans le JSON, comme dans le jeu ; si l'artiste
fait de vraies images, elles remplaceront ces dessins. Ils sont en accès libre, sans compte. Les textes des dessins sont écrits
dans le JavaScript (en français) : ils seront à traduire avec le reste le jour de la version anglaise.

**Simulateur** (après « BuildCourseWeb » dans Unity) :

```bash
python3 site/tools/sync_simulator.py
```

Les quatre fichiers (`WebCours.loader.js`, `.data.gz`, `.framework.js.gz`, `.wasm.gz`) sont copiés dans
`site/private/simulateur/`. Les anciens sont remplacés d'un coup. Ce dossier est **privé** (voir plus bas) et ignoré par git
(12 Mo, refabriqué souvent). Le dossier du jeu n'est jamais modifié.

## 2 ter. Recherche par données

L'onglet « Recherche par données » (`/identification`) retrouve une pierre à partir de ce qu'on a mesuré ou observé : indice de réfraction,
densité et biréfringence (avec une tolérance au choix), dureté (de … à …), caractère optique, pléochroïsme, couleur, transparence, éclat,
fluorescence aux UV, filtre Chelsea, spectre, conductivité thermique, aimant, naturelle ou synthétique. On laisse vide ce qu'on ne sait pas ;
la liste se réduit aux pierres compatibles avec tous les critères remplis, les plus proches d'abord. Les valeurs sont celles du jeu (une seule
valeur par pierre) : en vrai elles varient un peu, d'où la tolérance.

## 2 bis. Les images des fiches (et les vraies photos)

Aucune image du jeu n'est utilisée. Les dessins sont **générés par le site** à partir des données :

* **Système cristallin et axes** : cellule et axes a, b, c (ou a₁ a₂ a₃ + c) de chacun des 7 systèmes.
* **Figure d'interférence** : croix noire (uniaxe) ou deux branches (biaxe) ; rien pour les pierres isotropes.
* **Pléochroïsme** : pastilles de couleur déduites du texte de la fiche (approximation).
* **Lueur aux UV** : la couleur et la force de la réaction de la fiche.
* **Spectre** : spectre dessiné à partir des raies mesurées (longueur d'onde, largeur, profondeur).
* **Couleurs possibles** (comme dans l'encyclopédie du jeu) : toutes les couleurs que la pierre peut avoir, en grille (du clair au sombre).
  On clique sur une couleur, on choisit une forme (13) et une taille (les 8 du jeu : brillant, mixte, à degrés, Ascher, princesse,
  ciseaux, radiant, cabochon ; celles qu'une forme n'accepte pas sont grisées, comme dans le jeu). La pierre est un **visualiseur 3D**
  qu'on tourne à la souris : même géométrie de facettes que le jeu (`tools/export_meshes.py` relit `GemMesh.cs`) et même rendu par
  lancer de rayons (réfraction, réflexion totale, dispersion, studio de lampes : port de `GemRealistic.shader` dans `static/js/gem3d.js`).
  Ce n'est pas pixel pour pixel le rendu d'Unity (motifs, inclusions et polariscope ne sont pas repris). Sans WebGL2, un dessin simple le remplace. Les pierres de synthèse « toutes couleurs » ont un bouton Naturelle / Synthétique. Pour les diamants :
  l'échelle D à Z seulement (les 27 teintes de la grille GIA ne sont pas reprises, voir l'inventaire des droits).
  Les palettes viennent du jeu : `python3 site/tools/export_palettes.py` relit `GemColorSystem.cs` (sans le modifier) et
  réécrit `data/palettes.json`. À relancer après un changement des couleurs dans le jeu.
* **Composition chimique et système** : écrits à la main dans `tools/build_species_extra.py` (minéralogie courante, à faire relire
  par ton enseignante) ; `python3 site/tools/build_species_extra.py` régénère `data/species_extra.json`.

**Photos** : aucune photo pour le moment (les photos trouvées automatiquement sur Wikimedia Commons ne montraient pas toujours la bonne
pierre, elles ont été supprimées). Le site est prêt à en afficher : déposer `static/photos/<nom-de-la-page>.jpg` et ajouter une ligne
(fichier, auteur, licence, adresse) dans `data/photo_credits.json`. Les outils `tools/find_photo_candidates.py` et `tools/fetch_photos.py`
restent disponibles.

Ce ne sont pas des photos (dessins ci-dessus). De vraies photos viennent de Wikimedia Commons (licences libres, crédit obligatoire) :

1. `python3 site/tools/find_photo_candidates.py` cherche des candidates (rien n'est téléchargé) ;
2. ouvre <http://127.0.0.1:8000/dev/photos> et note le numéro de la bonne photo pour chaque pierre ;
3. `python3 site/tools/fetch_photos.py corindon-rubis=1 emeraude=2 …` télécharge **seulement** les photos choisies dans
   `static/photos/` et enregistre l'auteur et la licence, affichés sous la photo. Tu peux aussi déposer tes propres photos
   (`static/photos/<nom-de-la-page>.jpg`) en ajoutant une ligne dans `data/photo_credits.json`.

## 3. Comment c'est protégé

* **Le simulateur** est réservé aux membres connectés (l'e-mail n'a pas besoin d'être vérifié). Ce n'est pas un simple fichier caché. Les 4 fichiers ne sont pas dans le dossier public : le site ne les
  envoie qu'à un visiteur dont la connexion est valide, à chaque demande. Déconnecté, on reçoit « 403 ». Les fichiers
  compressés `.gz` sont envoyés avec l'en-tête `Content-Encoding: gzip`, comme le demande Unity.
* **Le forum** : tout passe par un seul fichier de règles (`app/forum.py`) qui vérifie qui demande quoi (équivalent des
  règles de sécurité de la base : le navigateur ne peut jamais lire ni écrire dans la base directement). Il faut un e-mail vérifié pour écrire
  ou signaler ; seuls les administrateurs modèrent ; le texte des messages est « nettoyé » (le HTML est refusé,
  seuls `**gras**`, `*italique*`, listes, citations et liens sont permis, liens en `nofollow`).
* **Anti-spam** : 1 message toutes les 15 s, 10 par heure, 3 nouveaux sujets par heure, pas de message identique répété,
  1 seul lien pendant les 3 premiers jours d'un compte (4 ensuite), champ piège invisible contre les robots,
  limites sur les inscriptions, connexions et envois d'e-mails.
* **Comptes** : mots de passe stockés brouillés (PBKDF2, jamais en clair), liens e-mail à usage unique, session fermée
  partout après un changement de mot de passe, même réponse que l'adresse existe ou non, protection CSRF sur tous les
  formulaires, en-têtes de sécurité (CSP, X-Frame-Options…).
* **Pas de référencement** : balise `<meta name="robots" content="noindex, nofollow">` sur chaque page, en-tête
  `X-Robots-Tag: noindex, nofollow` sur chaque réponse, `robots.txt` qui interdit tout, aucun sitemap.

Vérifier toi-même (le site doit tourner) :

```bash
curl -si http://127.0.0.1:8000/ | grep -i x-robots-tag
```

```bash
curl -s http://127.0.0.1:8000/robots.txt
```

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:8000/simulateur/fichiers/WebCours.wasm.gz
```

(Réponses attendues : `X-Robots-Tag: noindex, nofollow` ; `Disallow: /` ; `403`.)

Attention : « non référencé » ne veut pas dire « secret ». Une fois en ligne, n'importe qui qui connaît l'adresse peut
l'ouvrir, et d'autres moteurs peuvent ignorer ces consignes. Le simulateur et le forum, eux, sont vraiment fermés.

## 4. Où sont les choses

```
site/
  run.py                  lance le site en local
  manage.py               commandes d'administration (administrateur, liste des membres)
  wsgi.py                 point d'entrée pour un vrai serveur
  app/                    le code (pages, comptes, forum, règles, sécurité)
  i18n/fr.json            TOUS les textes visibles du site (voir « anglais » plus bas)
  static/                 styles et script de chargement du simulateur
  data/                   copie des données du jeu (encyclopédie, leçons)
  private/simulateur/     copie du build Unity (privée, ignorée par git)
  var/                    base de données gemlab.db et journaux (ignoré par git, à sauvegarder en ligne)
  tools/                  sync_data.py, sync_simulator.py
  deploy/                 exemples pour un serveur (Caddy, service)
  tests/                  vérifications automatiques
```

Lancer les vérifications automatiques (49 tests : comptes, forum, droits, simulateur fermé, noindex, textes) :

```bash
python3 -m unittest discover -s site/tests
```

## 5. L'anglais

Le site existe en français (adresses sans préfixe) et en anglais (adresses qui commencent par `/en`, par exemple `/en/lecons/dichroscope`).
Un bouton **FR / EN** dans l'en-tête passe d'une langue à l'autre sur la même page ; chaque page annonce son équivalent aux moteurs de recherche
(balises `hreflang`, plan du site dans les deux langues). Les liens, les redirections et les liens des courriels restent dans la langue de la page.

**Ce qui est traduit** (étape 1) : toute l'interface (`i18n/en.json`, mêmes clés que `fr.json`), les 22 leçons (`data/en/lessons/`), les 169 questions de quiz
(`data/en/quizzes/`), le glossaire (`data/en/glossary.json`) et les textes des schémas animés (`static/i18n/figures.en.json`).
Les **fiches des espèces** sont aussi traduites (étape 2) : noms, descriptions, propriétés, spectres, fluorescence, formes cristallines, gisements et couleurs possibles
(`data/en/species.json`, 1154 textes). C'est un dictionnaire « texte français → texte anglais » : un même texte n'est traduit qu'une fois. En anglais on cherche « ruby » ou
« garnet » (le nom français marche encore). Les adresses gardent leurs mots français (`/en/encyclopedie/grenat-spessartite`). Quand le jeu change un texte
(`sync_data.py`), `python3 site/tools/species_strings.py` liste les textes sans traduction ; un test échoue tant qu'il en reste. Le code qui lit des mots français dans les fiches
(familles de couleurs, pléochroïsme, magnétisme, spectres) utilise `catalog.fr(espece, champ)`, le texte d'origine, donc l'identification et la comparaison donnent les mêmes
résultats dans les deux langues.

**Comment ça marche** : un fichier anglais ne change que les textes ; la structure (étapes, figures, bonne réponse d'un quiz, nombre de choix) vient du fichier français,
donc ajouter une question ou une étape en français sans la traduire ne casse rien (le texte français s'affiche en attendant). Un test vérifie que `en.json` a les mêmes clés
et les mêmes `{trous}` que `fr.json`, et que chaque leçon, quiz et schéma a sa traduction. Après un changement du jeu (`sync_data.py`), relire `data/en/` pour les leçons modifiées.
Les textes des schémas : `python3 site/tools/extract_figure_texts.py` refait la liste (`figures.template.json`) ; mettre la traduction dans `figures.en.json`.
**Les traductions sont de moi, sans relecture par un anglophone** : le vocabulaire de gemmologie est à faire vérifier avant la mise en ligne.
Pour une troisième langue : copier `fr.json` en `<langue>.json`, ajouter la langue dans `Request` (`app/web.py`) et dans `catalog.set_lang`.

## 6. Mettre le site en ligne (plus tard)

Il te faudra :

| Besoin | À quoi ça sert | Coût approximatif (à vérifier) |
|---|---|---|
| **Un petit serveur** (VPS : Hetzner, OVH, Scaleway…) ou un hébergeur d'applications Python avec disque durable | fait tourner le site, garde la base de comptes et le simulateur | 4 à 6 € / mois |
| **Un nom de domaine** (`.fr`, `.com`…) | l'adresse du site | 8 à 15 € / an |
| **Un service d'envoi d'e-mails** (Brevo, Resend, Mailjet… : gratuits pour de petits volumes) | envoyer les liens de vérification et de mot de passe oublié, sans finir en courrier indésirable (il faut régler quelques lignes de configuration sur le domaine) | 0 € au début |
| Certificat https | cadenas, obligatoire pour les comptes | gratuit (Caddy / Let's Encrypt) |
| Sauvegardes (option fortement conseillée) | copie quotidienne de `var/gemlab.db` | 0 à 1 € / mois |

Total : environ **5 à 8 € par mois** au départ, plus le nom de domaine. Pourquoi pas l'hébergement gratuit ? Ce site
a une vraie base de comptes et un simulateur à protéger : il lui faut un petit serveur qui reste allumé. C'est ce qui
a permis de tout tester ici sans aucun compte chez un service externe.

Étapes (un technicien ou moi pouvons les faire en une heure) : installer Python et `pip install -r requirements.txt` ;
copier le dossier `site/` ; copier le build avec `sync_simulator.py` ; régler les variables (`GEMLAB_ENV=prod`,
`GEMLAB_BASE_URL`, `GEMLAB_SMTP_*`, `GEMLAB_TRUST_PROXY=1`, voir `deploy/gemlab.service.example`) ; lancer avec `gunicorn`
derrière Caddy (`deploy/Caddyfile.example`). Ne jamais mettre en ligne avec `python3 site/run.py` : c'est seulement pour
essayer.

Variantes possibles plus tard : Supabase (comptes et base hébergés, gratuit au début mais envois de fichiers de 12 Mo
limités à environ 400 chargements par mois sur l'offre gratuite) avec un site statique (Cloudflare Pages…). Cela demanderait
de réécrire les comptes et le forum ; ce n'est pas utile tant qu'un petit serveur suffit.

## 7. À régler avant d'ouvrir au public

* **Droits** (`Docs/INVENTAIRE_DROITS.md`) : le site publie les textes des fiches et des leçons, et le simulateur contient
  les sprites du jeu. L'inventaire note des points « Moyen » (leçons, spectres, fluorescence, traitements tirés du cours :
  accord de l'enseignant) et « Élevé » (origine des sprites à confirmer). Le site n'utilise aucune image du jeu : les pierres
  sont de petits dessins générés avec les couleurs de la fiche. Les mentions « source : diplôme… » des données ne sont
  **pas** affichées.
* **Obligations légales des comptes** (RGPD) : les pages (mentions légales, confidentialité, contact, à propos) et la suppression de compte
  existent, voir la section 8. Il reste à renseigner les informations de l'éditeur, à faire relire les textes, et à décider si
  des conditions d'utilisation (case à cocher à l'inscription) sont nécessaires.
* **Pas encore là** : modifier ou supprimer son propre message, messages privés, double authentification, pages sur les
  traitements.
* Les adresses du site sont en français (`/encyclopedie`, `/forum`…) ; elles pourront être préfixées par la langue.

## 8. Avant de publier : pages légales et référencement

**Pages légales.** Le site a quatre pages : `/mentions-legales`, `/confidentialite` (politique de confidentialité et cookies), `/contact` et `/a-propos`, liées dans le
pied de page. Les textes sont dans `i18n/fr.json` (clés `legal.*`, `privacy.*`, `contact.*`, `about.*`) ; ce sont des modèles rédigés pour ce site tel qu'il fonctionne
aujourd'hui (comptes, forum, quiz, aucun traceur) : **à faire relire par quelqu'un de compétent avant la mise en ligne**, et à mettre à jour si le site change
(publicité, statistiques, paiement…). La date « dernière mise à jour » est `LEGAL_UPDATED` dans `app/routes_legal.py`.

Les informations de l'éditeur ne sont pas dans le code : on les donne par des variables d'environnement. Tant qu'elles manquent, les pages affichent
« à compléter » en surbrillance jaune.

| Variable | Contenu |
|---|---|
| `GEMLAB_PUBLISHER_NAME` | Nom de la personne ou de la société qui édite le site |
| `GEMLAB_PUBLISHER_STATUS` | Statut (micro-entreprise, association, particulier…) |
| `GEMLAB_PUBLISHER_ADDRESS` | Adresse de l'éditeur |
| `GEMLAB_PUBLISHER_DIRECTOR` | Directeur de la publication |
| `GEMLAB_CONTACT_EMAIL` | Adresse de contact (affichée publiquement) |
| `GEMLAB_HOST_NAME`, `GEMLAB_HOST_ADDRESS`, `GEMLAB_HOST_PHONE` | Hébergeur (obligatoire dans les mentions légales) |

`python3 site/manage.py check-publication` liste ce qui manque encore (variables, courriels réels, adresse publique…).

**Suppression de compte.** Un membre peut supprimer son compte depuis « Mon compte » (droit à l'effacement) : adresse, nom, mot de passe, sessions et scores de quiz
sont effacés ; ses messages de forum restent, sous le nom « membre-supprime-N ». Un compte administrateur ne se supprime pas depuis cette page.

**Référencement.** Par défaut le site est fermé aux moteurs de recherche (`noindex` partout, `robots.txt` interdit tout, pas de plan du site). Pour l'ouvrir,
c'est une seule variable : `GEMLAB_INDEXABLE=1` (avec `GEMLAB_BASE_URL` = l'adresse publique en https). Alors :
- les pages publiques (accueil, encyclopédie et ses 120 fiches, leçons, glossaire, comparateur, identification, pages légales) deviennent `index, follow`, avec une adresse
  canonique ; les pages privées (compte, forum, simulateur, progression, connexion…) restent toujours `noindex` ;
- `robots.txt` autorise le site, interdit les pages privées et indique le plan du site ; `/sitemap.xml` liste les pages publiques (120 fiches + leçons comprises).
Chaque page porte aussi un titre, une description et les balises d'aperçu de partage (Facebook, Messenger, Discord, X) avec l'image `static/img/og-image.png`.
Les icônes (`favicon.svg`, `apple-touch-icon.png`, `og-image.png`) se refont avec `python3 site/tools/make_icons.py "NomDuSite" "Phrase d'accroche"` (nécessite Pillow,
seulement pour cet outil) : à relancer quand le nom du site sera choisi.

Une fois ouvert, déclarer le plan du site dans la Search Console de Google (`https://votre-site/sitemap.xml`) et vérifier quelques pages avec l'outil « Inspection d'URL ».


## 9. Accessibilité et relecture

- **Contrastes** : `python3 site/tools/check_contrast.py` mesure les paires de couleurs du thème (clair et sombre) selon le niveau AA des règles WCAG ; un test le lance.
- **Animations** : un bouton « Mettre les animations en pause » (accueil et pied de page de chaque page) arrête le saphir, le bandeau de pierres, les étoiles et les schémas
  des leçons ; le choix est gardé dans le navigateur, et le site démarre déjà sans animation si l'appareil demande moins de mouvement. Le bandeau de pierres s'arrête aussi quand
  on navigue au clavier dedans.
- **Schémas des leçons** : chacun a un texte alternatif (`aria-label`) et un bloc « Décrire ce schéma » (clés `fig.desc.*` de `i18n/fr.json`).
- **Structure des pages** : un test vérifie sur une vingtaine de pages la langue, un seul titre h1, les textes alternatifs des images, les libellés des champs de formulaire,
  les noms des boutons, le lien d'évitement. Ce n'est pas un audit complet : il reste à essayer avec un lecteur d'écran et au clavier seul.
- **Relecture par un spécialiste** : `python3 site/tools/export_review.py` écrit `Docs/RELECTURE_CONTENU.md` : toutes les questions de quiz (réponse juste marquée), le
  glossaire et les couleurs de pléochroïsme dessinées, avec une case d'avis pour chaque point. À envoyer à un gemmologue ; à refaire après chaque changement de contenu.

## 10. L'onglet Prix (estimation du prix d'une gemme)

Page publique `/prix` : on choisit une espèce naturelle, un poids et, au choix, la couleur, la pureté, le traitement et le type de prix (demandé ou payé). Le site donne une **fourchette** (quart bas, valeur centrale, quart haut du prix par carat, puis pour la pierre entière), un graphique des prix relevés et le tableau par tranche de poids. Il ne cite jamais de vendeur ni de site.

**Rien n'est inventé.** L'estimation ne vient que de la table `price_reports` (prix « approuvés », des 24 derniers mois). Avec moins de 5 prix proches du poids demandé, le site répond « pas assez de données » ; entre les deux, une tendance poids/prix (moins fiable, marquée « confiance faible ») n'est utilisée que s'il y a au moins 8 prix sur au moins 3 tranches de poids, et jamais très loin des poids connus. Les valeurs extrêmes sont écartées avant le calcul.

D'où viennent les prix :
- **Les membres** (compte vérifié) : `/prix/ajouter`, 10 prix par jour au plus, case « je confirme que ce prix est réel », champ anti-robot, refus des doublons. Le prix attend la vérification de l'administrateur (`/admin/prix`, avec un signalement « très éloigné des autres » pour les montants suspects). Un prix de l'administrateur est publié tout de suite.
- **Des fichiers** importés par l'administrateur (par exemple un accord avec un négociant, ou un relevé fait à la main) :
  ```
  python3 site/manage.py import-prices fichier.csv      # colonnes : voir data/prices_template.csv
  python3 site/manage.py export-prices sauvegarde.csv   # sans aucune donnée de membre
  ```
  Les lignes passent les mêmes contrôles que celles des membres ; les lignes refusées sont listées avec leur numéro.

Ce qu'il ne faut pas faire (c'est la partie risquée) : copier automatiquement les prix d'autres sites, ou réutiliser les données d'une interface de programmation sans que sa licence le permette. Mieux vaut des prix saisis, des accords écrits ou des relevés personnels.

Confidentialité : un prix ajouté n'est pas lié à son auteur après la suppression du compte (l'identifiant est effacé, le prix reste, anonyme) ; la politique de confidentialité le dit. Le code est dans `app/prices.py` (calcul) et `app/routes_prices.py` (pages) ; les tests dans `TestPrices`.

## 11. Le nom du site

Le nom (« GemXP ») vit à un seul endroit : `SITE_NAME` dans `app/config.py`, ou la variable `GEMLAB_SITE_NAME`. Il apparaît dans l'en-tête, les titres, le pied de page, les pages légales,
les courriels (sujet et expéditeur) et les aperçus de partage. Les textes l'écrivent `{site_name}`. Pour changer de nom :
1. changer la valeur (ou `GEMLAB_SITE_NAME=NouveauNom` au lancement) ;
2. refaire l'icône et l'image de partage : `python3 site/tools/make_icons.py` (elles reprennent le nom) ;
3. mettre à jour `GEMLAB_BASE_URL` et `GEMLAB_MAIL_FROM` avec le nouveau domaine.
Le jeu garde son nom (« GemLab Simulator »), c'est un autre produit : le texte du simulateur (`sim.window`) ne change pas.

## 12. Les familles de gemmes

Page `/encyclopedie/familles` (menu « Familles » de l'encyclopédie), en deux parties :
1. **Les classes de minéraux** : les 10 classes de la classification de Strunz (éléments natifs, sulfures, halogénures, oxydes et hydroxydes, carbonates, borates, sulfates, phosphates, silicates, composés organiques) et une 11e, « synthèses et imitations ».
   Chacune a sa page `/encyclopedie/classes/<classe>` : introduction, ce que ses espèces ont en commun (système cristallin, indice, densité, dureté, caractère optique, calculés à partir des données du jeu), points à retenir, liste des espèces. Chaque espèce est dans **une seule** classe.
   Choix de Strunz à connaître : le quartz et l'opale (la silice SiO₂) sont rangés avec les **oxydes**, comme dans la classification officielle, alors que beaucoup d'ouvrages les mettent avec les silicates ; la page des oxydes l'explique.
2. **Les grandes familles de gemmes** : pour l'instant seulement les **béryls**, les **grenats** et les **corindons** (`/encyclopedie/familles/<famille>`, même présentation que les classes).

Chaque classe a son icône (`app/class_icons.py` : un atome, un cube de pyrite, l'octaèdre de la fluorite, le tonneau hexagonal du corindon, le rhomboèdre de la calcite, le triangle du groupe BO₃, des lames de célestine, le prisme de l'apatite, le tétraèdre SiO₄, une feuille, un flacon de laboratoire).

Chaque fiche d'espèce indique sa classe, et sa famille quand elle est montrée.

- **Les données** : `data/classes.json` et `data/families.json` (français : nom, composition, introduction, points, liste des espèces), et `data/en/classes.json`, `data/en/families.json` (anglais, même structure).
- **Ajouter une famille au site** : 30 familles sont déjà écrites (quartz, tourmalines, feldspaths, olivines, jades, ivoires, opales, perles, corail, diamants…) mais cachées : dans `families.json`, mettre `"visible": true` pour celle qu'on veut montrer. Un test vérifie que chaque espèce est dans une classe et une seule, que les identifiants existent et que tout est traduit.
- **Quand le jeu ajoute une espèce** : le test échoue tant qu'on ne l'a pas rangée dans une classe de `classes.json`.
- Les textes sont de moi : à faire relire par un gemmologue avec le reste du contenu (`Docs/RELECTURE_CONTENU.md`).

## 13. Espèces propres au site

`data/extra_species.json` contient des espèces que l'encyclopédie montre mais que les données du jeu n'ont pas (encore) : les quatre nouveaux grenats écrits dans le code du jeu (mais pas encore générés dans Unity), et le grossulaire et l'andradite purs (deux pôles du schéma des grenats).
Le fichier est fabriqué par `python3 site/tools/build_extra_species.py` (les quatre variétés sont lues dans `SceneBootstrap.cs`, les deux pôles sont écrits dans le script). Le catalogue les ajoute aux espèces du jeu, sauf si le jeu a déjà une espèce du même nom : la version du jeu gagne.
Leur fiche porte un bandeau qui dit que ce n'est pas (encore) une pierre du jeu. Pour en ajouter une : l'écrire dans `build_extra_species.py`, puis lancer `build_extra_species.py`, `export_palettes.py`, `build_species_extra.py` (formule), puis `add_new_garnets.py` (classe, famille, anglais : on adapte la liste `NEW` et le dictionnaire `EN`).

## 14. Les pierres qui changent de couleur : deux grilles

Pour une pierre qui change de couleur (alexandrite, grenat à changement de couleur, saphir à changement de couleur), le nuancier « Couleurs possibles » de la fiche a **deux grilles, une pour chaque état** : à la lumière du jour, et sous une lampe à incandescence (plus chaude). Les deux grilles ont les mêmes cases aux mêmes places : choisir une couleur de jour montre, dans la case au même endroit de l'autre grille, ce qu'elle devient sous la lampe, et la fiche montre **deux pierres** côte à côte (en 3D, comme d'habitude).

- **D'où viennent les couleurs de la lampe** : `tools/export_palettes.py` ajoute à chaque case de ces espèces (liste `COLOR_CHANGE`) sa couleur sous lampe : la teinte de jour est transformée par une table propre à l'espèce (alexandrite : du vert jaunâtre à l'orangé-rouge et du vert bleuté au pourpre ; grenat : du bleu-vert au rose-rouge et du bleu au violet ; saphir : du bleu au violet et du violet au rose-rouge), la saturation reste, la brillance baisse un peu. Le jeu, lui, donne aujourd'hui un même rose à toutes ces pierres (`GemLook.Incandescent`) : les tables du site gardent l'ordre des couleurs, ce qui donne une grille variée.
- **Ajouter une pierre qui change de couleur** : l'ajouter à `COLOR_CHANGE` (avec sa table de teintes) dans `export_palettes.py`, relancer `python3 site/tools/export_palettes.py`. La fiche passe seule en deux grilles.
- Le **saphir à changement de couleur** est une espèce du site (`extra_species.json`, voir la section 13), classée dans les oxydes et les corindons.

## 15. Les 22 nouvelles espèces (jeu et site)

Vingt-deux espèces ont été ajoutées d'un coup, pour le jeu **et** pour le site, en trois groupes : les incontournables du commerce (amazonite, pierre de soleil, hiddénite, tourmalines verdélite et dravite, aventurine, jaspe, onyx, enstatite), les pierres d'ornement (larimar, charoïte, sugilite, azurite, chrysocolle, variscite, lépidolite) et les raretés de collection (bénitoïte, grandidiérite, zultanite, euclase, danburite, kornérupine).

- **Une seule source** : `site/tools/new_species_spec.py` (une fiche par espèce, avec les textes français et anglais).
- **Côté jeu** : `python3 site/tools/add_species.py` écrit le code C# entre deux marqueurs `// <new-species-2026-11-20>` (une espèce par bloc dans `SceneBootstrap.cs`, les fenêtres de couleur, les instruments, les habitus, le signe de la biréfringence, l'aspect des pierres, la fluorescence de la bénitoïte). On peut le relancer : il remplace le bloc au lieu de l'ajouter deux fois (`--check` montre ce qu'il changerait). **Le code n'a pas été compilé** (pas d'Unity ici) : il faut ouvrir Unity, lancer **GemLab > Rebuild Gem Data** et lire la console.
- **Côté site** : `build_extra_species.py` (les espèces, ajoutées à `extra_species.json`), `export_palettes.py` (nuancier ; la zultanite a deux grilles, voir la section 14), `build_species_extra.py` (système cristallin et formule), puis `add_new_species.py` (classes, familles, textes anglais). Les fiches portent le bandeau « pas (encore) une pierre du jeu » jusqu'à ce que Unity ait généré les pierres (la version du jeu prend alors le dessus).
- **Classes** : tout en silicates, sauf larimar/charoïte/sugilite/chrysocolle ; l'azurite est un carbonate, la variscite un phosphate ; l'aventurine, le jaspe, l'onyx et la zultanite sont dans les oxydes (le quartz y est classé).
- **Familles** (écrites mais masquées, comme les autres) : feldspaths, pyroxènes, tourmalines, quartz, calcédoines, carbonates, phosphates.
- Les instruments qui lisent des éléments (fluorescence X, microscope électronique) ont reçu le baryum, le béryllium et le bore, pour la bénitoïte, l'euclase et la danburite.

## 16. Une espèce, plusieurs couleurs : le saphir et la fluorite

Une pierre qui change seulement de couleur reste **la même espèce** (demande du 2026-11-20). Il y a donc une seule espèce **Saphir** (tout sauf le rouge, qui est le rubis : bleu, jaune, rose, orange, vert, violet, incolore) à la place de sept, et une seule **Fluorite** (violette, bleue, verte, jaune, orange, rose, rougeâtre, incolore) à la place de quatre. Restent à part : le padparadscha, le saphir à changement de couleur, les saphirs de synthèse, et la fluorite à bandes et le Blue John (un autre aspect, pas une couleur).

- **Dans le jeu** (code C#, non compilé ici) : `GemColorSystem.cs` (la fenêtre de couleur « Saphir » est la réunion de celles des six couleurs, chacune pondérée par sa part du commerce ; la fluorite a ses huit modes de couleur), nouvelle classe `GemVariants.cs` (le groupe de couleur d'un saphir, le facteur de prix par couleur, les échantillons de couleur voulue), et les catalogues qui pensaient « une espèce par couleur » (chauffage, irradiation, diffusion, teinture, remplissage, fluorescence, spectres, EDXRF, MEB, photoluminescence, UV-Vis) lisent maintenant la couleur de la pierre. Les dix anciens fichiers `.asset` sont supprimés ; **Unity doit être relancé** (GemLab > Rebuild Gem Data) pour créer `Saphir.asset`.
- **Sur le site** : `python3 site/tools/merge_species.py` fait la même fusion dans la copie des données du jeu (mêmes textes que `SceneBootstrap.cs`), les classes et les familles, les photos candidates et les textes anglais ; il ne fait rien quand les données du jeu ont déjà l'espèce fusionnée, et `sync_data.py` le relance après avoir copié les données. `export_palettes.py` porte la même fusion des couleurs (`Palettes.sapphire`).
- Le prix : la valeur de base du saphir est celle du bleu (140) ; `GemVariants.ColorValueFactor` donne le facteur des autres couleurs (rose 0,79, orange 0,64, violet 0,46, jaune 0,39, vert 0,29, incolore 0,16 : les valeurs qu'elles avaient comme espèces).
