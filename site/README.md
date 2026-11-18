# Site web GemLab (première version, en local)

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

## 5. L'anglais plus tard

Aucun texte n'est écrit dans les pages : tout est dans `i18n/fr.json`. Pour l'anglais : copier ce fichier en `i18n/en.json`,
traduire les valeurs, puis ajouter le choix de langue (adresses `/en/...`). Un test refuse tout texte français écrit en dur
dans le code. Le contenu des fiches (descriptions, leçons) vient des fichiers du jeu, qui sont en français : il faudra
une version anglaise de `gemlab_data.json` et des leçons (`Resources/Lessons`).

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
* **Obligations légales des comptes** (RGPD) : mentions légales, politique de confidentialité, case ou texte de conditions
  d'utilisation, moyen de supprimer son compte. Rien de cela n'est écrit : à faire avant d'accepter de vrais inscrits.
* **Pas encore là** : modifier ou supprimer son propre message, supprimer son compte, messages privés, double
  authentification, schémas des leçons (dessinés par le code du jeu), pages sur les instruments et les traitements.
* Les adresses du site sont en français (`/encyclopedie`, `/forum`…) ; elles pourront être préfixées par la langue.
