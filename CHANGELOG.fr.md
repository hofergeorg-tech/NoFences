# Journal des modifications

Toutes les modifications importantes de ce fork.
English: [CHANGELOG.md](CHANGELOG.md) · Deutsch: [CHANGELOG.de.md](CHANGELOG.de.md) ·
Italiano: [CHANGELOG.it.md](CHANGELOG.it.md) · Español: [CHANGELOG.es.md](CHANGELOG.es.md)

## [2.6.0] - non publiée

### Nouveautés
- **Traductions personnelles** : Paramètres → Général → « Traductions personnelles… » ouvre le dossier `lang` avec un modèle anglais. Un fichier comme `nl.json` ajoute une langue, un `fr.json` avec quelques textes ne modifie que ceux-ci. Tous les textes sont désormais dans un fichier JSON par langue.
- **Drapeaux pour chaque langue** : le menu des langues affiche de vrais drapeaux (flag-icons), les langues personnelles ont donc aussi leur drapeau (par le code de langue, une région comme `pt-BR` ou `"_flag": "at"` dans le fichier de langue).
- **Annuler (Ctrl+Z)** pour la suppression, le déplacement, le redimensionnement et le renommage des barrières ainsi que le retrait, le déplacement et le renommage des éléments ; aussi « Annuler : … » dans le menu de la zone de notification et de la barrière.
- **Groupes de barrières** : clic droit → Groupe. Les barrières d'un groupe se déplacent ensemble et se replient ensemble sur leur barre de titre.

### Modifications
- **Toutes les données à côté de NoFences.exe**, rangées en dossiers (`config`, `backups`, `themes`, `media`, `cache`, `logs`, `lang`). Les données des versions précédentes sont copiées une fois ; dans un dossier de programme non accessible en écriture (Program Files, WinGet), elles restent dans `%LocalAppData%\NoFences`, rangées de la même façon.

## [2.5.0] - 2026-10-04

### Nouveautés
- **Jeux** : temps de jeu de chaque jeu détecté, compté automatiquement et affiché sous sa jaquette ; tri par les plus joués.
- Widgets **actus des jeux** (annonces et notes de mise à jour de vos jeux Steam) et **Twitch en direct** (qui est en direct, avec notification).
- **Promos Steam** réglables : promos, meilleures ventes, nouveautés ou seulement votre liste de souhaits ; remise minimale, prix maximal, nombre.
- Widgets **minuteur et réveil** (minuteurs rapides, réveils les jours choisis, sonne même masqué), **habitudes** (cocher les 7 derniers jours, séries) et **progression du temps** (jour, semaine, mois, année).
- **Rappel de pause** après 30 à 120 minutes d'utilisation active (Paramètres → Automatisation).
- Le **calendrier de l'horloge** marque les jours avec des rendez-vous.
- **Barrières** : marques de couleur pour les éléments (Maj+clic droit → Marquer, ou Ctrl+1…6), tri **les plus utilisés d'abord**, **aperçu au survol** (contenu des dossiers, grand aperçu d'images/PDF).
- **Autres barrières** : **modèles** (configuration gaming, bureau, minimal), une **étagère** qui se vide toute seule, **favoris du navigateur** (Chrome, Edge, Brave, Vivaldi, Opera) et **dossiers ouverts récemment**.
- **Raccourci propre à chaque barrière** (Ctrl+Maj+F1…F12) pour l'afficher au premier plan, même depuis un autre profil.
- Les barrières peuvent **s'estomper** quand la souris est loin (Paramètres → Bureau) ; chaque barrière peut être exclue.
- Outils : **afficher/masquer les icônes du bureau** et **déplacer toutes les barrières vers un autre écran**.
- **Profils** : démarrer des programmes avec un profil (et les fermer en le quittant, si souhaité) ; **fond d'écran selon l'heure** (Paramètres → Automatisation).
- **Notes** : protection par mot de passe (chiffrée, se verrouille seule après 2 minutes), coller des **images** avec Ctrl+V, enregistrer des **notes vocales**.
- L'**historique du presse-papiers** garde aussi les images ; **épinglez** des entrées (clic droit) pour qu'elles restent en haut, même après un redémarrage.
- **Moniteur système** avec courbe de deux minutes et **alerte quand la carte graphique chauffe trop** ; **test de débit** dans le widget réseau.
- Le **widget batterie** affiche aussi les **manettes et appareils Bluetooth** ; nouveau widget **Démarrage auto** (activer/désactiver les programmes au démarrage de Windows).
- Outils : **code QR** (texte ou lien du presse-papiers), **loupe** ; "Ranger les dossiers" trouve les **fichiers en double**.
- **Météo** : alerte pluie pour les deux prochaines heures (« Pluie dans env. 20 min »), lever et coucher du soleil, phase de la lune.
- **Créateur de styles** : créez votre propre style en quelques clics (couleurs, polices, bordure, coins) avec aperçu en direct ; nouveau style **Contraste élevé** avec grand texte.
- Nouveau widget **Page web** : une petite page (tableau de bord, page d'état …) directement dans la barrière, actualisée régulièrement.
- **Promos Steam** : une liste de souhaits non publique fonctionne via son **lien de partage**.
- Les **promos Steam** affichent toutes les promotions en cours (pas seulement celles mises en avant ; d'autres se chargent en défilant) et chaque jeu en promo de votre liste de souhaits.

## [2.4.2] - 2026-10-03

### Modifications
- **Nettoyer des dossiers** (avant : Nettoyer les téléchargements) : ajoutez d'autres dossiers que Téléchargements ; tous
  sont parcourus ensemble, une colonne indique où se trouve chaque élément. La liste des dossiers est gardée.

### Corrections
- Les listes défilantes (promos Steam, actualités, rendez-vous, tâches) restaient décalées et bloquées après avoir agrandi la barrière.

## [2.4.1] - 2026-10-03

### Nouveautés
- La **recherche** trouve aussi les applications du menu Démarrer et les pages des paramètres Windows, et calcule
  (`12*7`, `200*15%` ; Entrée copie).
- Widgets **temps d'écran** (programmes utilisés aujourd'hui / 7 jours, reste sur ce PC), **son** (volume, muet, micro,
  changer de périphérique) et **état des services** (RSI, Discord, Epic Games, GitHub… d'après leurs pages d'état).
- **Mode concentration** : le minuteur passe à un profil choisi pendant les tours de concentration.
- **Raccourcis de profil** Ctrl+Alt+F1…F9 (F10 : toutes les barrières) et un **fond d'écran par profil**.
- **Assistant de bureau** : range les icônes du bureau dans de nouvelles barrières par type (proposé au premier démarrage).
- **Règle à l'écran** en pixels, centimètres ou pouces (Outils ▸ Règle à l'écran – dans la zone de notification et le menu de chaque barrière).
- **Pipette de couleur** avec loupe (copie #RRGGBB) et **Nettoyer les téléchargements** (anciens fichiers, les plus gros d'abord, vers la corbeille).
- **Note rapide** de partout avec Ctrl+Alt+N ; **mise en forme des notes** (titres, listes, citations, lignes, gras, italique).
- **Rappels répétés** (chaque jour, en semaine, chaque semaine, chaque mois) et un widget **liste de tâches** avec échéances.
- Widgets **horloge mondiale**, **mode d'alimentation** (aussi par profil) et **promos Steam** (liste de souhaits d'abord).

### Modifications
- **Menus groupés** : Nouveau widget ▸ Temps et planning / Infos et actualités / Système / Jeux et médias ; Style ▸ De base /
  Jeux et technique / Travail et quotidien / Loisirs / Post-it / Styles personnels.
- **Actualités** plus lisibles : titres en gras sur deux lignes au plus, la source dans la couleur d'accent, séparateurs.

### Corrections
- Les widgets actualités et cours affichaient « Pas de connexion au service » quand un flux échouait.
- Une page web saisie comme flux trouve maintenant le flux annoncé par la page, ou indique clairement que ce n'est pas un flux.
- Une mise à jour échouée n'efface plus les titres, cours ou rendez-vous déjà affichés ; nouvel essai après 30 secondes.
- Les problèmes des widgets en ligne sont écrits dans log.txt dans le dossier des données.

## [2.4.0] - 2026-10-03

### Nouveautés
- **Widgets** : horloge et calendrier, moniteur système (CPU, RAM, charge et température du GPU, FPS), lecteurs,
  corbeille, temps de jeu et compte à rebours. Menu de la zone de notification ou d'une barrière → Nouveau widget.
- **Temps de jeu** pour n'importe quel jeu : choisissez son exe, NoFences enregistre combien de temps il tourne
  (aujourd'hui, semaine, mois, total, en cours).
- **Compte à rebours** jusqu'à une date, avec un titre.
- Widgets **météo** (Open-Meteo, sans compte), **en cours de lecture** avec commandes, **réseau** avec graphique et
  ping, **historique du presse-papiers** (en mémoire seulement, respecte les gestionnaires de mots de passe) et **batterie**.
- Widget **jeux** : les jeux installés depuis Steam (avec jaquettes), Epic, GOG et l'application Xbox ; un clic les lance.
- **Rendez-vous** depuis des liens d'agenda (.ics : Google, Outlook, iCloud), y compris récurrents.
- Widgets **cadre photo**, **minuteur de concentration (Pomodoro)**, **actualités** (RSS/Atom avec flux prêts) et
  **cours** (actions, indices, cryptos).
- **Recherche dans toutes les barrières** (Ctrl+Alt+F) : raccourcis, contenu des dossiers, onglets et notes.
- **Profils** comme « Travail » et « Jeux » : changement depuis la zone de notification, attribution par clic droit →
  Afficher dans le profil.
- **Automatisation** : changer de profil pendant qu'un programme tourne ou à heures fixes ; masquer les barrières en
  plein écran ; style par défaut clair/sombre selon Windows ou l'heure.
- **Plusieurs PC** : barrières dans un dossier partagé comme OneDrive.
- Style **Couleur d'accentuation Windows** – 25 styles au total.
- **Français et espagnol** ; menu **Langue** avec drapeaux dans la zone de notification et dans chaque barrière.
- Bouton **Faire un don** (À propos et Paramètres → Mises à jour).
- **Mesure des FPS** (facultative, désactivée par défaut) : un petit assistant avec droits d'administrateur compte les
  images du programme au premier plan ; Windows demande une fois, les paramètres expliquent pourquoi.
- Barrière **« Fichiers récents »** et **barre de lancement rapide** (icônes seules, noms en info-bulle).
- **Onglets** dans les barrières de raccourcis.
- **Seulement sur ce bureau virtuel** par barrière.
- **Export/import** des barrières et des styles personnels, par ex. vers un autre PC.
- **Fenêtre de paramètres** (zone de notification → Paramètres) pour tout ce qui concerne l'application ; le menu est
  beaucoup plus court.
- **Paramètres de barrière** repensés, avec sections et aperçu en direct.
- **Fenêtre À propos** avec version, crédits et liens.
- **8 nouveaux styles** : Documents, Multimédia, Musique, Sport, Photos, Voyages, Cuisine, Nature ; chaque style a une
  couleur d'accent pour les widgets.

### Corrections
- OK dans les paramètres d'une barrière de raccourcis supprimait tous ses raccourcis.
- Ouvrir les paramètres d'un widget provoquait un plantage.
- Dans le post-it, les éléments débordaient en bas sur l'ombre du papier.
- Le raccourci « Afficher les barrières au premier plan » était collé au texte du menu.
- L'entrée sélectionnée dans la barre latérale des paramètres devenait illisible.

## Versions précédentes

Les versions 2.0.0 à 2.3.0 sont décrites dans le [journal en anglais](CHANGELOG.md). NoFences est basé sur
[Twometer/NoFences](https://github.com/Twometer/NoFences) de Twometer et ses contributeurs.

[2.4.1]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.1
[2.4.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.0
