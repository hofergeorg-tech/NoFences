# Aide de NoFences

NoFences place sur votre bureau des cadres (« barrières ») qui gardent vos icônes en ordre, ainsi que des notes et des widgets.
English: [HELP.md](HELP.md) · Deutsch: [HILFE.md](HILFE.md) · Italiano: [AIUTO.md](AIUTO.md) · Español: [AYUDA.md](AYUDA.md)

## Premiers pas

- Après le premier démarrage, il y a une barrière vide. Faites-y glisser des fichiers ou des dossiers.
- **Clic droit sur une barrière** (titre ou espace vide) pour son menu : paramètres, style, renommer, nouvelle barrière
  ou nouveau widget, supprimer.
- **Clic droit sur un élément** pour le menu habituel de l'Explorateur. Maj + clic droit affiche plutôt le menu de la barrière.
- L'**icône de la zone de notification** (en bas à droite, parfois derrière la flèche ^) crée des barrières, les
  affiche/masque, change de profil et ouvre les **Paramètres** généraux. La **langue** se trouve dans la zone de
  notification et dans le menu de chaque barrière.

## Types de barrières

- **Barrière de raccourcis** (par défaut) : des liens vers des fichiers et dossiers. Les fichiers restent où ils sont,
  donc aussi sur le bureau. Supprimer l'original le retire aussi de la barrière.
- **Barrière de dossier** : affiche le contenu d'un dossier. Les fichiers déposés y sont **déplacés** (Ctrl pour
  copier), ils quittent donc vraiment le bureau.
- **Note** : un post-it avec du texte, voir plus bas.
- **Widget** : du contenu en direct – horloge, météo, jeux, rendez-vous et plus, voir plus bas.
- **Fichiers récents** : les 20 derniers fichiers ouverts (lecture seule).
- **Barre de lancement rapide** : une barrière de raccourcis étroite, icônes seules ; les noms s'affichent en info-bulle.

Astuce : pour un bureau rangé, créez un dossier comme `Documents\Barrières\Travail` et utilisez-le comme barrière de dossier.

## Travailler avec les éléments

- Double-clic ouvre un élément. Faites glisser les éléments pour les réordonner, sur une autre barrière pour les y
  déplacer, ou dans l'Explorateur.
- **Ctrl+clic** sélectionne plusieurs éléments, **Maj+clic** une plage ; glisser dans un espace vide trace un rectangle de sélection.
- Une barrière cliquée écoute le clavier : **Entrée** ouvre, **F2** renomme, **Suppr** retire (barrière de raccourcis :
  seulement le lien ; barrière de dossier : corbeille), **Ctrl+A**, **Ctrl+C**, flèches, **Échap**.
- **Tapez simplement** pour chercher dans cette barrière ; le texte s'affiche en haut à droite, Échap arrête.
- Menu de la barrière → **Trier par** : manuel, nom, type, date de modification ou taille.
- **Onglets** (barrières de raccourcis) : menu → Ajouter un onglet. Clic pour changer, double-clic pour renommer, faites
  glisser des éléments sur un onglet pour les y déplacer.

## Rechercher dans toutes les barrières

**Ctrl+Alt+F** (ou clic droit sur l'icône de notification ou une barrière → Outils ▸ Rechercher dans les barrières…) ouvre une zone de recherche. Elle trouve tout
dans vos barrières – raccourcis, contenu des dossiers, onglets et textes des notes – même des lettres dans l'ordre
(« ffx » trouve Firefox) – ainsi que les applications du menu Démarrer et les pages des paramètres Windows
(« bluetooth », « son »). Tapez un calcul comme `12*7` ou `200*15%` : Entrée copie le résultat. **Entrée** ouvre le
résultat, **↑↓** pour choisir, **Échap** ferme. Le raccourci se change dans Paramètres → Bureau.

## Déplacer, redimensionner, renommer

- Faites glisser la barre de titre pour déplacer, les bords pour redimensionner. **Double-clic sur le titre** pour renommer.
- Les barrières **s'aimantent** aux bords de l'écran et aux autres barrières ; maintenez **Alt** pour les placer librement.
- Les positions sont mémorisées **par configuration d'écrans** : débranchez puis rebranchez un écran, les barrières reviennent.
- Les barrières **verrouillées** ne peuvent pas être déplacées ni modifiées. **Replier quand la souris s'éloigne** les
  réduit à leur barre de titre.
- **Toujours au premier plan** garde une barrière au-dessus de toutes les fenêtres (au-dessus des jeux seulement en mode
  « fenêtre sans bordure »).
- **Seulement sur ce bureau virtuel** n'affiche une barrière que sur le bureau virtuel actuel (Win+Ctrl+flèches).
- **Ctrl+Alt+D** fait passer toutes les barrières devant les fenêtres ouvertes ; Échap ou un clic ailleurs les renvoie.

## Notes

- **Double-clic** pour écrire ; **Échap** ou un clic à côté enregistre.
- Les lignes commençant par `[ ]` deviennent des cases à cocher ; un clic les coche et barre la ligne.
- Les adresses web et chemins sont soulignés et s'ouvrent d'un clic. Le texte déposé sur une note y est ajouté.
- **Mise en forme** : `# Titre` (aussi `##`, `###`), `- élément` ou `* élément` pour les listes, `> citation`, `---`
  pour une ligne, `**gras**` et `*italique*`.
- **Ctrl+Alt+N** (modifiable dans Paramètres → Bureau) crée de n'importe où une note près de la souris, prête à écrire.
- Menu de la barrière → **Rappel…** : à l'heure choisie, NoFences joue un son et affiche une notification – une fois,
  chaque jour, en semaine, chaque semaine ou chaque mois.
- Style post-it en jaune, rose, vert, bleu et orange.

## Widgets

Menu de la zone de notification ou d'une barrière → **Nouveau widget**. Les widgets avec une liste défilent à la molette.

- **Horloge et calendrier**.
- **Moniteur système** : CPU, RAM, charge et température du GPU (NVIDIA), et **FPS** si activé (voir plus bas).
- **Lecteurs** : niveau de remplissage et espace libre ; un clic ouvre le lecteur.
- **Corbeille** : déposez-y des fichiers pour les supprimer, double-clic l'ouvre, le menu la vide.
- **Temps de jeu** : aujourd'hui / cette semaine / ce mois-ci / total pour n'importe quel jeu. Double-cliquez et
  choisissez l'exe du jeu ; NoFences enregistre combien de temps il tourne.
- **Compte à rebours** : jours et heures jusqu'à une date ; double-clic pour la définir.
- **Météo** : temps actuel et prévisions sur trois jours pour un lieu recherché (données : Open-Meteo, sans compte).
- **En cours de lecture** : titre, artiste et pochette de ce que joue Spotify, un navigateur ou un lecteur multimédia,
  avec précédent / lecture-pause / suivant.
- **Réseau** : débit de téléchargement et d'envoi avec un graphique de la dernière minute, et le ping.
- **Historique du presse-papiers** : les 15 derniers textes copiés ; un clic les copie à nouveau. Conservés seulement
  tant que NoFences tourne ; les mots de passe des gestionnaires de mots de passe sont ignorés.
- **Batterie** : charge, en charge ou non, autonomie restante (portables).
- **Jeux** : vos jeux installés depuis Steam (avec jaquettes), Epic, GOG et l'application Xbox ; les plus récemment
  joués d'abord. Un clic lance le jeu. Le menu permet de masquer des jeux, trier par nom ou relancer la recherche.
- **Rendez-vous** : les deux prochaines semaines depuis des liens d'agenda (.ics). Google : paramètres de l'agenda →
  « Adresse secrète au format iCal » ; Outlook : Paramètres → Calendrier → Calendriers partagés → Publier → ICS ;
  iCloud : partager l'agenda publiquement. Plusieurs agendas : un lien par ligne. Les événements récurrents sont pris en charge.
- **Cadre photo** : diaporama d'un dossier d'images (sous-dossiers compris), toutes les 10 s à 15 min. Un clic affiche
  l'image suivante, double-clic l'ouvre.
- **Minuteur de concentration (Pomodoro)** : 25 minutes de concentration, 5 minutes de pause, une longue pause après
  quatre tours (ou 50/10, 15/3). Un son et une notification marquent chaque changement.
- **Actualités** : titres de flux RSS ou Atom (boutons prêts pour Le Monde, BBC, heise et d'autres) ; un clic ouvre l'article.
- **Cours** : actions, indices et cryptos avec la variation depuis la veille et un graphique de la journée, avec les
  symboles de Yahoo Finance comme `AAPL`, `^FCHI` (CAC 40), `^GDAXI`, `BTC-EUR`. Mis à jour toutes les cinq minutes ;
  à titre indicatif seulement.
- **Temps d'écran** : quels programmes vous avez utilisés et combien de temps aujourd'hui ou sur les 7 derniers jours
  (cliquez sur « aujourd'hui ⇄ » pour changer). Enregistré seulement tant que le widget existe et que vous êtes au PC ;
  reste sur ce PC.
- **Son** : volume du périphérique de lecture actuel (cliquez sur la barre ou utilisez la molette), couper le son des
  haut-parleurs et du micro, et passer d'un clic à un autre périphérique (casque ↔ haut-parleurs).
- **État des services** : si RSI, Discord, Epic Games, GitHub et d'autres ont des problèmes en ce moment, d'après leurs
  pages d'état publiques ; un clic sur une ligne ouvre la page.
- **Liste de tâches** : double-cliquez pour ajouter une tâche, avec échéance et répétition si vous voulez ; un clic sur le
  cercle la coche (les tâches répétées passent à leur prochaine date). Les tâches arrivées à échéance sont annoncées même
  si le widget est masqué.
- **Horloge mondiale** : l'heure ailleurs avec le décalage par rapport à la vôtre ; double-cliquez pour choisir les
  fuseaux horaires.
- **Mode d'alimentation** : passez d'un clic entre Utilisation normale, Performances élevées et les autres.
- **Promos Steam** : les promotions actuelles sur Steam ; les jeux de votre liste de souhaits passent en premier si elle
  est publique (le compte Steam connecté sur ce PC est utilisé). Un clic ouvre la page du magasin dans Steam.

Chaque widget a ses propres réglages dans son menu. Le menu du minuteur de concentration propose aussi le **mode
concentration** : pendant un tour, il passe à un profil de votre choix (par ex. « Concentration » avec seulement les
barrières de travail) et revient pendant les pauses.

## Assistant de bureau

Menu de la zone de notification ou d'une barrière → Outils ▸ **Assistant de bureau…** (proposé aussi au premier démarrage) range ce qui se trouve sur votre
bureau dans de nouvelles barrières – jeux, programmes, documents, images, musique et vidéos, archives, dossiers –,
chacune dans un style adapté. Rien n'est déplacé : les barrières pointent vers les fichiers. Pour masquer les
originaux : clic droit sur le bureau → Affichage → Afficher les icônes du bureau.

## Règle à l'écran

Menu de la zone de notification ou d'une barrière → Outils ▸ **Règle à l'écran** place une règle au-dessus de tout : glisser pour la déplacer, glisser
l'extrémité pour l'allonger, double-clic ou espace pour la tourner, flèches pour l'ajuster au pixel (Maj : 10 px), U ou
le menu passe entre pixels, centimètres et pouces (taille réelle, d'après la taille indiquée par l'écran). Une ligne
rouge suit la souris et affiche la distance. Échap la ferme.

## Pipette et nettoyage

- Outils ▸ **Pipette de couleur** : l'écran se fige et une loupe suit la souris ; un clic copie la couleur en `#RRGGBB`
  (Maj+clic : `rgb(…)`), Échap annule.
- Outils ▸ **Nettoyer des dossiers…** : liste ce qui n'a pas bougé depuis une semaine, un mois, trois mois ou un an, les
  plus gros d'abord ; les éléments choisis vont à la corbeille (restaurables). Au départ, c'est le dossier
  Téléchargements ; **Ajouter un dossier…** en ajoute d'autres (bureau, vidéos, un dossier de jeux…), la liste est gardée.

## Profils

Regroupez les barrières en profils comme « Travail » et « Jeux » et passez de l'un à l'autre depuis la zone de
notification (**Profil ▸**) ou dans **Paramètres → Bureau**. Clic droit sur une barrière → **Afficher dans le profil**
pour l'attribuer ; une barrière sans profil apparaît dans tous les profils. Les barrières créées pendant qu'un profil
est actif lui appartiennent. **Ctrl+Alt+F1…F9** passent au profil 1…9, **Ctrl+Alt+F10** affiche toutes les barrières.
Avec un profil actif, zone de notification → Profil ▸ **Fond d'écran pour « … »** lui donne son propre fond d'écran ;
votre fond habituel revient dans les profils qui n'en ont pas. **Alimentation pour « … » ▸** dans le même menu change
aussi le mode d'alimentation avec le profil (par ex. Performances élevées pour les jeux).

## Automatisation (Paramètres → Automatisation)

- **Changer de profil automatiquement** : « Jeux » tant qu'un certain programme tourne, « Travail » en semaine de 8 h à
  17 h, etc. Un programme en cours l'emporte sur une règle horaire ; quand plus aucune règle ne s'applique, le profil
  précédent revient. Changer à la main met fin à ce qu'une règle a lancé.
- **Plein écran** : tant qu'un jeu, une vidéo ou une présentation occupe un écran, les barrières de cet écran sont masquées.
- **Style clair et sombre** : le style par défaut suit le mode clair/sombre de Windows ou change à heures fixes – par
  exemple post-it le jour et verre le soir. Les barrières avec leur propre style le gardent.
- Le style **Couleur d'accentuation Windows** prend sa couleur dans Paramètres → Personnalisation → Couleurs.

## Plusieurs PC

Paramètres → Données et styles → **Choisir le dossier partagé…**, par ex. dans OneDrive. Barrières, notes, temps de jeu
et styles personnels y sont alors stockés, et chaque PC qui utilise le même dossier affiche les mêmes barrières. Les
positions sont gardées par configuration d'écrans : un portable et un PC fixe peuvent donc les disposer différemment.
Quand un autre PC enregistre, NoFences recharge au bout de quelques secondes. « Arrêter le partage » recopie tout sur ce PC.

## Mesure des FPS (facultative)

Windows ne fournit les événements de fréquence d'images qu'aux programmes disposant de droits d'administrateur. NoFences
utilise donc un petit processus assistant qui s'exécute en administrateur – pas NoFences lui-même. Il compte seulement
les images, aucun contenu, aucune saisie. Activez-la dans **Paramètres → Mesure des FPS** ; Windows demande une fois,
ensuite une tâche du Planificateur de tâches démarre l'assistant sans demander. La désactiver supprime la tâche.

## Ranger depuis le bureau

Dans les paramètres d'une barrière, saisissez des modèles sous « Ranger depuis le bureau », par ex. `*.pdf; *.docx`, ou
ajoutez un modèle prédéfini. Les nouveaux fichiers du bureau correspondants vont dans cette barrière (les téléchargements
terminés aussi). **Ranger le bureau maintenant** (zone de notification ou paramètres) range ce qui s'y trouve déjà.

## Styles

Choisissez le style par défaut dans **Paramètres → Général**, ou un par barrière (menu → Style, ou paramètres de la
barrière avec aperçu en direct). Il y a 25 styles – verre, couleur d'accentuation Windows, HUD Star Citizen,
Retro-Arcade, Matériel, Geek, Loisirs, Travail, Famille, Gaming, Finance, Réseaux sociaux, Documents, Multimédia,
Musique, Sport, Photos, Voyages, Cuisine, Nature et post-it en cinq couleurs.

**Styles personnels** : Paramètres → Données et styles → Ouvrir le dossier des styles. Copiez `beispiel-mocha.json`,
changez les couleurs (`#RRGGBB` ou `#RRGGBBAA`) et rechargez. Les styles personnels portent une ★.

## Paramètres (zone de notification → Paramètres)

- **Général** : langue (automatique, English, Deutsch, Italiano, Français, Español), démarrer avec Windows, extensions,
  style par défaut, animations.
- **Bureau** : double-clic sur le bureau pour masquer/afficher les barrières ; raccourci pour les mettre au premier plan
  (Ctrl+Alt+D) ; profils ; raccourci de recherche (Ctrl+Alt+F) ; rangement.
- **Automatisation** : règles de profil, plein écran, style clair et sombre (voir plus haut).
- **Mises à jour** : NoFences vérifie GitHub et installe les nouvelles versions en un clic ; dons.
- **Mesure des FPS** : voir plus haut.
- **Données et styles** : exporter/importer des barrières, restaurer une sauvegarde (toutes les 12 heures), dossier
  partagé, dossiers.

## Questions fréquentes

**Puis-je supprimer l'original après avoir glissé une icône dans une barrière ?**
Dans une barrière de raccourcis, non : elle ne fait que pointer vers lui. Dans une barrière de dossier, le fichier a été
déplacé, il n'y a donc plus rien à supprimer.

**Windows affiche un avertissement SmartScreen au démarrage.**
L'exe n'est pas encore signé. Cliquez sur « Informations complémentaires » → « Exécuter quand même ».

**Qu'est-ce qui passe par Internet ?**
Seulement ce que vous configurez : la vérification des mises à jour (GitHub), la météo (Open-Meteo), vos liens d'agenda,
les flux d'actualités et les cours (Yahoo Finance). Rien d'autre n'est envoyé.

**Où sont mes réglages ?**
Dans `%LocalAppData%\NoFences\fences.json` (sauvegardes à côté), ou dans le dossier partagé si vous en avez choisi un.
Avec un fichier vide `portable.txt` à côté de `NoFences.exe`, ils sont enregistrés à côté de l'exe.

**Comment désinstaller ?**
Paramètres → Général : décochez « Démarrer avec Windows » ; désactivez la mesure des FPS si utilisée ; zone de
notification → Quitter ; supprimez `NoFences.exe` et le dossier `%LocalAppData%\NoFences`. Les fichiers des barrières de
dossier restent dans leurs dossiers.
