# Nouveau sprite du personnage (mis de côté)

Quatre vues à l'arrêt, 32×64 px : `Down` (face), `Up` (dos), `Left` et `Right` (profils). Il n'y a pas encore d'animation de marche. Pour l'essayer en jeu :

1. Copier chaque image dans `Assets/Resources/PlayerIdle/` (même nom) et dans les six images de `Assets/Resources/PlayerWalk/<Direction>/` (0.png à 5.png) : tant qu'il n'y a pas de marche, la même image sert partout.
2. Pieds au sol : dans les `.meta`, mettre `spritePivot: {x: 0.5, y: 0.15625}` (Up, Left, Right) et `{x: 0.5, y: 0.140625}` (Down), car l'image a 9 à 10 pixels vides sous les chaussures.
3. Plus grand sur la carte : dans `PlayerController.Awake`, `transform.localScale = (1.25, 1.25, 1)` et diviser `size` et `offset` des BoxCollider2D par 1.25 (la collision garde sa taille).

Les baskets et le vélo (`PlayerIdleBaskets`, `PlayerWalkBike`...) restent à redessiner.
