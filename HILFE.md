# NoFences Hilfe

NoFences legt Boxen („Fences“) auf deinen Desktop, die deine Icons ordnen – dazu Notizen und Widgets.
English: [HELP.md](HELP.md) · Italiano: [AIUTO.md](AIUTO.md) · Français : [AIDE.md](AIDE.md) · Español: [AYUDA.md](AYUDA.md)

## Erste Schritte

- Nach dem ersten Start gibt es einen leeren Fence. Zieh Dateien oder Ordner darauf.
- **Rechtsklick auf einen Fence** (Titel oder leere Fläche) öffnet sein Menü: Einstellungen, Style, Umbenennen,
  neuer Fence oder neues Widget, Löschen.
- **Rechtsklick auf einen Eintrag** zeigt das normale Explorer-Menü. Mit Shift + Rechtsklick kommt stattdessen das Fence-Menü.
- Das **Tray-Icon** (unten rechts, evtl. hinter dem ^-Pfeil) legt Fences an, blendet sie ein und aus, wechselt Profile
  und öffnet die **Einstellungen** für alles, was die ganze App betrifft. Die **Sprache** findest du im Tray und in
  jedem Fence-Menü.

## Arten von Fences

- **Verknüpfungs-Fence** (Standard): Verweise auf Dateien und Ordner. Die Dateien bleiben, wo sie sind, also auch auf
  dem Desktop. Löschst du das Original, verschwindet es auch aus dem Fence.
- **Ordner-Fence**: zeigt den Inhalt eines Ordners. Reingezogene Dateien werden dorthin **verschoben** (mit Strg kopiert),
  sie verschwinden also wirklich vom Desktop.
- **Notiz**: ein Post-it mit Text, siehe unten.
- **Widget**: Live-Inhalt – Uhr, Wetter, Spiele, Termine und mehr, siehe unten.
- **Zuletzt verwendet**: die 20 zuletzt geöffneten Dateien (nur lesen).
- **Schnellstart-Leiste**: ein schmaler Verknüpfungs-Fence nur mit Icons; die Namen erscheinen als Tooltip.

Tipp: Für einen aufgeräumten Desktop einen Ordner wie `Dokumente\Fences\Arbeit` anlegen und als Ordner-Fence verwenden.

## Mit Einträgen arbeiten

- Doppelklick öffnet einen Eintrag. Einträge ziehen zum Umsortieren, auf einen anderen Fence zum Verschieben oder in den Explorer.
- **Strg+Klick** wählt mehrere Einträge, **Shift+Klick** einen Bereich; Ziehen auf freier Fläche zeichnet ein Auswahlrechteck.
- Ein angeklickter Fence reagiert auf die Tastatur: **Enter** öffnet, **F2** benennt um, **Entf** entfernt
  (Verknüpfungs-Fence: nur den Verweis; Ordner-Fence: Papierkorb), **Strg+A**, **Strg+C**, Pfeiltasten, **Esc**.
- **Lostippen** sucht in diesem Fence; das Suchwort steht oben rechts, Esc beendet die Suche.
- Fence-Menü → **Sortieren nach**: manuell, Name, Typ, Änderungsdatum oder Größe.
- **Reiter** (Verknüpfungs-Fences): Fence-Menü → Reiter hinzufügen. Klick wechselt, Doppelklick benennt um, Einträge auf einen
  Reiter ziehen verschiebt sie dorthin.

## In allen Fences suchen

**Strg+Alt+F** (oder Tray → Fences durchsuchen…) öffnet ein Suchfeld. Es findet alles in deinen Fences – Verknüpfungen,
Ordnerinhalte, Reiter und Notiztexte – sogar Buchstaben in Reihenfolge („ffx“ findet Firefox) – und außerdem
Startmenü-Apps und Windows-Einstellungsseiten („bluetooth“, „sound“). Tipp eine Rechnung wie `12*7` oder `200*15%`,
Enter kopiert das Ergebnis. **Enter** öffnet den Treffer, **↑↓** wählen, **Esc** schließt. Das Tastenkürzel änderst
du unter Einstellungen → Desktop.

## Verschieben, Größe, Umbenennen

- Titelleiste ziehen zum Verschieben, Ränder ziehen für die Größe. **Doppelklick auf den Titel** benennt um.
- Fences **rasten** an Bildschirmrändern und anderen Fences ein; mit **Alt** platzierst du frei.
- Positionen werden **pro Monitor-Setup** gemerkt: Monitor ab- und wieder anstecken, und die Fences kehren zurück.
- **Gesperrte** Fences lassen sich nicht verschieben oder ändern. **Einklappen wenn Maus weg** verkleinert auf die Titelleiste.
- **Immer im Vordergrund** hält einen Fence über allen Fenstern (über Spielen nur im Modus „Randloses Fenster“).
- **Nur auf diesem virtuellen Desktop** zeigt einen Fence nur auf dem aktuellen virtuellen Desktop (Win+Strg+Pfeiltasten).
- **Strg+Alt+D** holt alle Fences vor die offenen Fenster; Esc oder ein Klick daneben schickt sie zurück.

## Notizen

- **Doppelklick** zum Schreiben; **Esc** oder ein Klick daneben speichert.
- Zeilen mit `[ ]` am Anfang werden zu Kästchen; ein Klick hakt sie ab und streicht die Zeile durch.
- Webadressen und Pfade sind unterstrichen und öffnen sich per Klick. Auf die Notiz gezogener Text wird angehängt.
- Fence-Menü → **Erinnerung…**: Zur gewählten Zeit meldet sich NoFences mit Ton und Benachrichtigung.
- Post-it-Style in Gelb, Rosa, Grün, Blau und Orange.

## Widgets

Tray- oder Fence-Menü → **Neues Widget**. Widgets mit Listen scrollen mit dem Mausrad.

- **Uhr & Kalender**.
- **System-Monitor**: CPU, RAM, GPU-Last und -Temperatur (NVIDIA) und **FPS**, wenn aktiviert (siehe unten).
- **Laufwerke**: Füllstand und freier Platz; Klick öffnet das Laufwerk.
- **Papierkorb**: Dateien darauf ziehen löscht sie, Doppelklick öffnet ihn, im Menü leeren.
- **Spielzeit**: heute / diese Woche / diesen Monat / gesamt für ein beliebiges Spiel. Doppelklick und die EXE des Spiels
  auswählen; NoFences zeichnet auf, wie lange es läuft.
- **Countdown**: Tage und Stunden bis zu einem Datum; Doppelklick zum Festlegen.
- **Wetter**: aktuelles Wetter und drei Tage Vorschau für einen gesuchten Ort (Daten: Open-Meteo, ohne Konto).
- **Medien**: Titel, Interpret und Cover von dem, was Spotify, ein Browser oder ein Mediaplayer gerade abspielt, mit
  Zurück / Play-Pause / Weiter.
- **Netzwerk**: Download- und Upload-Rate mit Verlauf der letzten Minute und Ping.
- **Zwischenablage-Verlauf**: die letzten 15 kopierten Texte; anklicken kopiert sie erneut. Nur solange NoFences läuft,
  Passwörter aus Passwort-Managern werden übersprungen.
- **Akku**: Ladestand, ob geladen wird, Restzeit (Laptops).
- **Spiele**: deine installierten Spiele aus Steam (mit Cover), Epic, GOG und der Xbox-App; zuletzt gespielte zuerst.
  Klick startet ein Spiel. Im Menü Spiele ausblenden, nach Name sortieren oder erneut suchen.
- **Termine**: die nächsten zwei Wochen aus Kalender-Links (.ics). Google: Kalendereinstellungen → „Privatadresse im
  iCal-Format“; Outlook: Einstellungen → Kalender → Freigegebene Kalender → Veröffentlichen → ICS; iCloud: Kalender
  öffentlich freigeben. Mehrere Kalender: ein Link pro Zeile. Wiederkehrende Termine werden unterstützt.
- **Fotorahmen**: Diashow eines Bilderordners (auch Unterordner), alle 10 s bis 15 min. Klick zeigt das nächste Bild,
  Doppelklick öffnet es.
- **Fokus-Timer (Pomodoro)**: 25 Minuten Fokus, 5 Minuten Pause, nach vier Runden eine lange Pause (oder 50/10, 15/3).
  Ton und Benachrichtigung bei jedem Wechsel.
- **News**: Schlagzeilen aus RSS- oder Atom-Feeds (fertige Knöpfe für Tagesschau, ORF, heise, BBC und mehr); Klick
  öffnet den Artikel.
- **Kurse**: Aktien, Indizes und Krypto mit der Veränderung seit gestern und einem Tagesverlauf, mit Symbolen wie bei
  Yahoo Finance: `AAPL`, `^GDAXI` (DAX), `^ATX`, `BTC-EUR`. Alle fünf Minuten aktualisiert; nur zur Information.
- **Bildschirmzeit**: welche Programme du heute oder in den letzten 7 Tagen wie lange benutzt hast (Klick auf
  „heute ⇄“ wechselt). Aufgezeichnet nur, solange das Widget existiert und du am PC bist; bleibt auf diesem PC.
- **Sound**: Lautstärke des aktuellen Wiedergabegeräts (Balken anklicken oder Mausrad), Stummschalten für Lautsprecher
  und Mikrofon, und mit einem Klick zu einem anderen Gerät wechseln (Headset ↔ Lautsprecher).
- **Dienst-Status**: ob RSI, Discord, Epic Games, GitHub und andere gerade Störungen haben, aus ihren öffentlichen
  Statusseiten; Klick auf eine Zeile öffnet die Seite.

Jedes Widget hat im Menü eigene Einstellungen. Im Menü des Fokus-Timers gibt es außerdem den **Fokus-Modus**: Er
wechselt während einer Fokus-Runde zu einem Profil deiner Wahl (z. B. „Fokus“ nur mit Arbeits-Fences) und in den
Pausen zurück.

## Desktop-Assistent

Tray → **Desktop-Assistent…** (wird auch beim ersten Start angeboten) sortiert, was auf deinem Desktop liegt, in neue
Fences – Spiele, Programme, Dokumente, Bilder, Musik & Videos, Archive, Ordner –, jeweils mit passendem Style. Es wird
nichts verschoben, die Fences verweisen auf die Dateien. Um die Originale auszublenden: Rechtsklick auf den Desktop →
Ansicht → Desktopsymbole anzeigen.

## Bildschirm-Lineal

Tray → **Bildschirm-Lineal** legt ein Lineal über alles: ziehen verschiebt, das Ende ziehen ändert die Länge,
Doppelklick oder Leertaste dreht es, Pfeiltasten verschieben pixelgenau (Shift: 10 px), U oder das Menü wechselt
zwischen Pixel, Zentimeter und Zoll (echte Größe, aus der Größe, die dein Monitor meldet). Eine rote Linie folgt der
Maus und zeigt den Abstand. Esc schließt es.

## Profile

Fences in Profile wie „Arbeit“ und „Gaming“ gruppieren und im Tray (**Profil ▸**) oder unter **Einstellungen → Desktop**
umschalten. Rechtsklick auf ein Fence → **In Profil zeigen** ordnet es zu; ein Fence ohne Profil erscheint in allen
Profilen. Neue Fences gehören zum gerade aktiven Profil. **Strg+Alt+F1…F9** wechseln zu Profil 1…9, **Strg+Alt+F10**
zeigt alle Fences. Ist ein Profil aktiv, gibt Tray → Profil ▸ **Hintergrundbild für „…“** ihm ein eigenes
Hintergrundbild; in Profilen ohne eigenes kommt dein gewohntes zurück.

## Automatik (Einstellungen → Automatik)

- **Profile automatisch wechseln**: „Gaming“, solange ein bestimmtes Programm läuft, „Arbeit“ werktags von 8 bis 17 Uhr
  usw. Ein laufendes Programm hat Vorrang vor einer Zeitregel; gilt keine Regel mehr, kommt das vorherige Profil zurück.
  Wechselst du selbst, endet, was die Regel begonnen hat.
- **Vollbild**: Solange ein Spiel, Video oder eine Präsentation einen Monitor füllt, werden die Fences auf diesem
  Monitor ausgeblendet.
- **Heller und dunkler Style**: Der Standard-Style wechselt mit dem hellen/dunklen Modus von Windows oder zu festen
  Uhrzeiten – z. B. tagsüber Post-it, abends Glas. Fences mit eigenem Style behalten ihn.
- Der Style **Windows-Akzentfarbe** übernimmt die Farbe aus Einstellungen → Personalisierung → Farben.

## Mehrere PCs

Einstellungen → Daten & Styles → **Gemeinsamen Ordner wählen…**, z. B. in OneDrive. Fences, Notizen, Spielzeit und
eigene Styles liegen dann dort, und jeder PC, der auf denselben Ordner zeigt, hat dieselben Fences. Positionen gelten
pro Monitor-Anordnung, Laptop und Desktop-PC können sie also verschieden anordnen. Speichert ein anderer PC, lädt
NoFences nach ein paar Sekunden neu. „Nicht mehr teilen“ kopiert alles zurück auf diesen PC.

## FPS-Messung (optional)

Windows gibt die Ereignisse für die Bildrate nur an Programme mit Administratorrechten. NoFences nutzt dafür einen kleinen
Hilfsprozess, der als Administrator läuft – NoFences selbst nicht. Er zählt nur Bilder, keine Bildinhalte, keine Eingaben.
Einschalten unter **Einstellungen → FPS-Messung**; Windows fragt einmal, danach startet eine Aufgabe in der Aufgabenplanung
den Helfer ohne Nachfrage. Beim Ausschalten wird die Aufgabe wieder entfernt.

## Vom Desktop einsortieren

In den Fence-Einstellungen unter „Vom Desktop einsortieren“ Muster eintragen, z. B. `*.pdf; *.docx`, oder eine Vorlage
hinzufügen. Neue Desktop-Dateien, die passen, wandern in diesen Fence (auch fertige Downloads). **Desktop jetzt aufräumen**
(Tray oder Einstellungen) sortiert, was schon da liegt.

## Styles

Den Standard wählst du unter **Einstellungen → Allgemein**, pro Fence im Fence-Menü → Style oder in den Fence-Einstellungen
mit Live-Vorschau. Es gibt 25 Styles – Glas, Windows-Akzentfarbe, Star Citizen HUD, Retro-Arcade, Hardware, Nerd, Hobby,
Arbeit, Familie, Gaming, Finanzen, Social, Dokumente, Multimedia, Musik, Sport, Fotos, Reisen, Kochen, Natur und Post-it
in fünf Farben.

**Eigene Styles**: Einstellungen → Daten & Styles → Styles-Ordner öffnen. `beispiel-mocha.json` kopieren, Farben ändern
(`#RRGGBB` oder `#RRGGBBAA`) und neu laden. Eigene Styles tragen einen ★.

## Einstellungen (Tray → Einstellungen)

- **Allgemein**: Sprache (automatisch, English, Deutsch, Italiano, Français, Español), mit Windows starten, Dateiendungen,
  Standard-Style, Animationen.
- **Desktop**: Doppelklick auf den Desktop blendet Fences aus/ein; Tastenkürzel, um Fences nach vorne zu holen
  (Strg+Alt+D); Profile; Tastenkürzel für die Suche (Strg+Alt+F); Einsortieren.
- **Automatik**: Profilregeln, Vollbild, heller und dunkler Style (siehe oben).
- **Updates**: NoFences prüft GitHub und installiert neue Versionen mit einem Klick; Spenden.
- **FPS-Messung**: siehe oben.
- **Daten & Styles**: Fences exportieren/importieren, Sicherung wiederherstellen (alle 12 Stunden), gemeinsamer Ordner, Ordner.

## Häufige Fragen

**Kann ich das Original löschen, nachdem ich ein Icon in einen Fence gezogen habe?**
Bei einem Verknüpfungs-Fence nicht, er verweist nur darauf. Bei einem Ordner-Fence wurde die Datei verschoben, es gibt also nichts mehr zu löschen.

**Windows zeigt beim Start eine SmartScreen-Warnung.**
Die EXE ist noch nicht signiert. Auf „Weitere Informationen“ → „Trotzdem ausführen“ klicken.

**Was geht ins Internet?**
Nur, was du einrichtest: die Update-Prüfung (GitHub), Wetter (Open-Meteo), deine Kalender-Links, News-Feeds und Kurse
(Yahoo Finance). Sonst wird nichts gesendet.

**Wo liegen meine Einstellungen?**
In `%LocalAppData%\NoFences\fences.json` (Sicherungen daneben) oder im gemeinsamen Ordner, wenn du einen gewählt hast.
Liegt eine leere `portable.txt` neben der `NoFences.exe`, werden sie stattdessen neben der EXE gespeichert.

**Wie deinstalliere ich NoFences?**
Einstellungen → Allgemein: „Mit Windows starten“ abhaken; FPS-Messung ausschalten, falls genutzt; Tray → Beenden;
`NoFences.exe` und den Ordner `%LocalAppData%\NoFences` löschen. Dateien in Ordner-Fences bleiben in ihren Ordnern.
