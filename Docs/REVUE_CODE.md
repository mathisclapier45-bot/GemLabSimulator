# Revue du code, 15 novembre 2026

Huit relecteurs ont lu en entier : Scripts/UI (A-D, E-H, I-M, O-W), Scripts/Simulation (A-F, G-L, M-U), Scripts/Player, Scripts/World, Scripts/Systems, Assets/Editor et les shaders. Rien n'a été compilé ni lancé : chaque point vient de la lecture du code. Aucune erreur de compilation ni clé de dictionnaire en double n'a été trouvée. Non couvert : `tools/export_game_data.py` et, en grande partie, `SceneBootstrap.cs` (parcouru par script).

## À corriger d'abord

1. **Couleurs perdues au chargement (élevé)** : `GemInstance.cs` `FromSaveData` (l. 790-808). Le `else if (definition != null)` est rattaché à `if (data.hasReport)` au lieu de `if (data.hasColor)`. Toute pierre sans rapport imprimé est donc recolorée au hasard à chaque chargement : couleurs traitées, irradiées, teintées ou blanchies perdues, lettre de couleur des diamants, nom commercial désaccordé. Correction : `else if (!data.hasColor && definition != null)` et un `if` propre pour le rapport. (Vérifié dans le code.)
2. **Libellé de transparence mal lu (moyen)** : `GemDiaphaneity.cs:15`. « Transparent à translucide » est lu comme semi-opaque. Huit espèces concernées : fluorite (4), calcite, sélénite, célestine, sphalérite. Aspect laiteux, éclat réduit, libellé faux dans la loupe.
3. **Quête bloquée possible aux chapitres 3, 4 et 5 (élevé à moyen)** : `StoryDirector.cs` (l. 1824, 2018, 2436). Les pierres sont retirées de l'inventaire avant que la phase suivante soit enregistrée : un changement de scène ou une sauvegarde à ce moment bloque la quête. Fréquence non confirmée.
4. **Récompenses payées deux fois (moyen à bas)** : `StoryDirector.cs` (l. 1203-1233, 1049-1054). Le crédit de Léa (60 €) et les 50 € d'Anselme sont versés avant l'avancement de la phase.
5. **État non remis à zéro entre deux parties (moyen)** : boîte aux lettres, `QuestState`, `ManoirRequestState`, indicateurs de `StoryDirector`, annonce du jour (`GameCalendar`), cotations des diamants (`GradingUI.Sessions`, qui survit aussi à une annulation de certificat par HPHT).
6. **Fenêtres qui rouvrent le menu pause avec Échap (moyen)** : carte de la ville, montre, choix de l'histoire, boîte aux lettres, leçons, encyclopédie (nom du panneau non reconnu), poster, confirmation des bains, de la teinture, du laser, de l'irradiation, du four, de la presse, de l'autoclave, des enchères. Cause commune : `ReturnToMenuController` ne reconnaît que les panneaux nommés `*Panel`, `*Page` ou `*Window` et ne sait pas qu'une confirmation consomme déjà Échap. Les enchères continuent aussi pendant le menu pause.
7. **Encyclopédie sans verrou de déplacement (moyen)** : on peut marcher, parler ou ouvrir d'autres fenêtres pendant qu'elle est ouverte depuis le menu pause.
8. **Cache de diagrammes et de résultats de laboratoire (moyen)** : `LabFindings.cs` (cache jamais vidé, clé sans l'état traité de la pierre) et `GemDiagrams.cs` (256 Ko par pierre gardés pour toujours, clé sans l'espèce).
9. **Rapport de pierre (moyen)** : `GemReportUI.cs:330`. « OTL (hors limite du prisme) » s'affiche chaque fois que l'indice n'a pas été mesuré.
10. **Spectres FTIR des diamants (moyen)** : un type IIa traité en HPHT est déclaré « HPHT probable » sans ses bandes ; un diamant irradié aux électrons (teinte 178°) ou revêtu de cobalt (225°) est pris pour un type IIb.

## Autres points

**Rendu 3D**
- `RealGemRenderer` : une allocation de 256 Ko par image dans `readback.GetPixels32()` ; cache de maillages jamais libéré (clé sur le hash d'identité).
- Un diamant de couleur de degré Faint sur une teinte à faible plafond de saturation peut tomber sous 0,04 : nommé « Incolore » mais payé 34 fois son prix (`GemColorSystem.cs:111` avec `GiaDiamondColors.cs:145`).
- Un rouge de coloration ratée peut valoir plus qu'avant (revêtement raté, `CoatingCatalog.cs:329`).

**Mes ajouts récents**
- Cornaline marquée « famille du quartz » : brut en cristal de quartz et revêtements métalliques acceptés.
- Raman de la calcite défini deux fois (la seconde définition écrase la première).
- Analyses aux rayons X (MEB) de la fabulite et de la sphalérite sans Sr, Zn ni S.
- 27 origines sans carte dans l'Atlas (dont celles des nouvelles espèces).
- La page « Gris » du nuancier cite un degré inexistant.
- Le marché aux pierres taillées peut proposer des espèces « brut seulement ».
- Stéatite et cornaline : biréfringence 0 avec un caractère biaxe ou uniaxe.

**Interface et fuites**
- Textures jamais détruites : carte de la ville (24 par ouverture), certificat, spectres des livres et du grimoire, bulles et flammes des fours.
- La loupe dessine la pierre avec les dimensions brutes (taille ronde ovale) et ses inclusions ignorent le changement de couleur de l'alexandrite et le pléochroïsme.
- Cinématique : Entrée ou Espace sur la première ligne peut lancer deux fois la création de partie.
- Boutons « Retour au sac » qui apparaissent à tort, texte du sac périmé après un clic sur un équipement.
- `ComputerUI.Close()` libère le verrou de déplacement même quand ce n'est pas lui qui l'a posé.

**Divers**
- Compagnons : on peut avoir 11 animaux (un invisible).
- Numéros de certificat non sauvegardés (doublons après redémarrage).
- Allocations par image (monnaie, horloge, aimant, minicarte).
- Détails dans les rapports des agents : fichiers et lignes cités ci-dessus.
