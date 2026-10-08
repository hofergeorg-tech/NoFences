# Änderungsprotokoll

Alle wichtigen Änderungen an diesem Fork.
English: [CHANGELOG.md](CHANGELOG.md) · Italiano: [CHANGELOG.it.md](CHANGELOG.it.md) ·
Français : [CHANGELOG.fr.md](CHANGELOG.fr.md) · Español: [CHANGELOG.es.md](CHANGELOG.es.md)

## [2.11.0] - 2026-10-08

### Behoben
- **Kurze Hänger**: Doppelklick-auf-Desktop lässt nicht mehr die ganze Maus ruckeln, wenn NoFences gerade beschäftigt ist (der Maus-Hook hat jetzt einen eigenen Thread; vorher konnte Windows ihn auch still abschalten). Zwischenablage lesen, auf einen Fence gezogene Dateien kopieren/verschieben und Einträge öffnen warten nicht mehr auf die Fences – große Kopien frieren weder NoFences noch das Explorer-Fenster ein, aus dem sie kommen.

### Neu
- NoFences merkt, wenn es hängt, und schreibt in logs\log.txt, was gerade lief (Zeile Freeze: …), Abstürze ebenso.

### Entfernt
- **FPS-Messung**: Sie brauchte einen Helfer mit Administratorrechten. NoFences braucht jetzt nie Adminrechte. War sie an, bietet NoFences einmal an, die übrig gebliebene Aufgabe in der Aufgabenplanung zu entfernen.

## [2.10.0] - 2026-10-05

### Neu
- **Notizen**: Checklisten-Fortschritt im Titel, Erledigte ans Ende oder ausblenden, Checklisten, die sich täglich/wöchentlich/monatlich zurücksetzen, Unterpunkte mit Klapp-Pfeilen, Klick-Zähler `[3/8]`, Rechnen (`650 + 80 =` → 730), farbige `#Tags` (Klick sucht), Tabellen, Schriftgröße mit Strg+Mausrad, **Vorlagen** (auch eigene), die letzten 20 **Versionen** zum Wiederherstellen, **Export** als Markdown oder PDF und Drucken.

## [2.9.0] - 2026-10-05

### Neu
- **Notizen formatieren**: unterstrichen, durchgestrichen, farbig markiert und Prioritäts-Fähnchen (!!! / !! / !) zusätzlich zu fett und kursiv – mit Formatierungsleiste über dem Editor und Strg+B / I / U / H.

### Geändert
- Der Style **Brettljause** nutzt jetzt echte Fotos (Speck, Käse, Brot, Kaminwurz, Gurkerl, Radieschen auf Holz) statt gezeichneter Formen.

### Behoben
- Ein Fence, der ausklappt (Einklappen wenn Maus weg), öffnet sich jetzt immer über benachbarten Fences.

## [2.8.0] - 2026-10-04

### Neu
- Neuer Style **Brettljause (Speck & Käse)**: ein Holzbrett mit Speck, Käse, Brot und Radieschen. Seitenleisten können einen **Hintergrund-Style** bekommen (Gruppe → An Bildschirmrand andocken → Leisten-Hintergrund), z. B. einen Holztisch mit Karo-Tuch und Jause zwischen den Fences.

## [2.7.0] - 2026-10-04

### Neu
- **Seitenleiste**: eine Fence-Gruppe am linken, rechten, oberen oder unteren Bildschirmrand andocken (Rechtsklick → Gruppe → An Bildschirmrand andocken). Sie fährt herein, wenn die Maus den Rand berührt, oder reserviert ihren Platz wie die Taskleiste; nie über Spielen im Vollbild.

## [2.6.0] - 2026-10-04

### Neu
- **Eigene Übersetzungen**: Einstellungen → Allgemein → „Eigene Übersetzungen…“ öffnet den Ordner `lang` mit einer englischen Vorlage. Eine Datei wie `nl.json` fügt eine Sprache hinzu, eine `de.json` mit einzelnen Texten ändert genau diese. Alle Texte liegen jetzt in einer JSON-Datei pro Sprache.
- **Flaggen für jede Sprache**: Das Sprachmenü zeigt echte Länderflaggen (flag-icons), damit auch eigene Sprachen ihre Flagge bekommen (über den Sprachcode, eine Region wie `pt-BR` oder `"_flag": "at"` in der Sprachdatei).
- **Rückgängig (Strg+Z)** für Löschen, Verschieben, Größe ändern und Umbenennen von Fences sowie Entfernen, Verschieben und Umbenennen von Einträgen; auch „Rückgängig: …“ im Tray- und Fence-Menü.
- **Fence-Gruppen**: Rechtsklick → Gruppe. Fences einer Gruppe bewegen sich gemeinsam und lassen sich zusammen auf ihre Titelleisten einklappen.
- **Fences**: Ordner im Fence aufklappen (Pfeil in der Ordner-Ecke, Inhalt eingerückt darunter), **Notizen an Einträgen** (Tooltip), **Nutzung und Aufräumen** (nie geöffnete Einträge als Vorschlag), **Hintergrundbild** oder Muster pro Fence und **Dateien öffnen mit** einem gewählten Programm.
- **Notizen** erkennen Termine („Mo 14:00 Zahnarzt“, „morgen 9:30 …“, „12.10. 15:00 …“) und bieten eine Erinnerung 15 Minuten vorher an; klingelnde Timer und Wecker lassen sich um 5 oder 10 Minuten **verschieben (Schlummern)**. Neue Werkzeuge: **Passwort-Generator** (kopiert ohne Zwischenablage-Verlauf) und **Netzwerk-Infos** (Adressen, WLAN, Router; Klick kopiert).

### Geändert
- **Alle Daten neben der NoFences.exe**, sortiert in Ordner (`config`, `backups`, `themes`, `media`, `cache`, `logs`, `lang`). Daten älterer Versionen werden einmalig übernommen; in einem nicht beschreibbaren Programmordner (Programme, WinGet) bleiben sie in `%LocalAppData%\NoFences`, genauso sortiert.

## [2.5.0] - 2026-10-04

### Neu
- **Spiele**: Spielzeit jedes erkannten Spiels, automatisch gezählt und unter dem Cover angezeigt; Sortierung nach meistgespielt.
- Widgets **Spiele-News** (Ankündigungen und Patchnotes deiner Steam-Spiele) und **Twitch live** (wer live ist, mit Benachrichtigung).
- **Steam-Angebote** einstellbar: Angebote, Topseller, Neuerscheinungen oder nur deine Wunschliste; Mindestrabatt, Höchstpreis, Anzahl.
- Widgets **Timer & Wecker** (Schnell-Timer, Wecker an gewählten Tagen, klingelt auch ausgeblendet), **Gewohnheiten** (letzte 7 Tage abhaken, Serien) und **Zeit-Fortschritt** (Tag, Woche, Monat, Jahr).
- **Pausen-Erinnerung** nach 30–120 Minuten aktiver PC-Nutzung (Einstellungen → Automatik).
- Der **Kalender im Uhr-Widget** markiert Tage mit Terminen.
- **Fences**: Farbmarkierungen für Einträge (Shift+Rechtsklick → Markieren, oder Strg+1…6), Sortierung **meistgenutzt zuerst**, **Vorschau beim Darüberfahren** (Ordnerinhalt, große Bild-/PDF-Vorschau).
- **Weitere Fences**: **Vorlagen** (Gaming-Setup, Büro, Minimal), eine **Ablage**, die sich selbst leert, **Browser-Lesezeichen** (Chrome, Edge, Brave, Vivaldi, Opera) und **zuletzt geöffnete Ordner**.
- Eigenes **Tastenkürzel pro Fence** (Strg+Shift+F1…F12) holt ihn nach vorne, auch aus einem anderen Profil.
- Fences können sich **ausblenden**, wenn die Maus weit weg ist (Einstellungen → Desktop); einzelne Fences lassen sich ausnehmen.
- Werkzeuge: **Desktop-Symbole ein/aus** und **alle Fences auf einen anderen Monitor**.
- **Profile**: Programme mit dem Profil starten (und beim Verlassen auf Wunsch wieder schließen); **Hintergrundbild nach Tageszeit** (Einstellungen → Automatik).
- **Notizen**: mit Passwort schützen (verschlüsselt, sperrt sich nach 2 Minuten selbst), **Bilder** mit Strg+V einfügen, **Sprachnotizen** aufnehmen.
- Der **Zwischenablage-Verlauf** merkt sich auch Bilder; Einträge **anheften** (Rechtsklick), damit sie oben bleiben – auch nach einem Neustart.
- **Systemmonitor** mit Zwei-Minuten-Kurve und **Warnung bei zu heißer Grafikkarte**; **Speedtest** im Netzwerk-Widget.
- Das **Akku-Widget** zeigt auch **Controller und Bluetooth-Geräte**; neues Widget **Autostart** (Windows-Autostart-Programme ein/aus).
- Werkzeuge: **QR-Code** (Text oder Link aus der Zwischenablage), **Bildschirmlupe**; "Ordner aufräumen" findet **doppelte Dateien**.
- **Wetter**: Regen-Hinweis für die nächsten zwei Stunden („Regen in ca. 20 Min.“), Sonnenauf- und -untergang, Mondphase.
- **Style-Designer**: eigenen Style per Klick gestalten (Farben, Schrift, Rahmen, Ecken) mit Live-Vorschau; neuer Style **Hochkontrast** mit großer Schrift.
- Neues Widget **Webseite**: eine kleine Seite (Dashboard, Statusseite …) direkt im Fence, regelmäßig aktualisiert.
- **Steam-Angebote**: eine nicht öffentliche Wunschliste funktioniert über ihren **Freigabelink**.
- **Steam-Angebote** zeigen alle aktuellen Angebote (nicht nur die hervorgehobenen; beim Scrollen werden weitere geladen) und jedes reduzierte Spiel deiner Wunschliste.

## [2.4.2] - 2026-10-03

### Geändert
- **Ordner aufräumen** (vorher: Downloads aufräumen): weitere Ordner neben Downloads hinzufügen; alle werden zusammen
  durchsucht, eine Spalte zeigt, wo ein Eintrag liegt. Die Ordnerliste bleibt gespeichert.

### Behoben
- Scrollbare Listen (Steam-Angebote, News, Termine, To-dos) blieben nach dem Vergrößern des Fences verschoben und ließen sich nicht mehr scrollen.

## [2.4.1] - 2026-10-03

### Neu
- Die **Suche** findet auch Startmenü-Apps und Windows-Einstellungsseiten und rechnet (`12*7`, `200*15%`; Enter kopiert).
- Widgets **Bildschirmzeit** (benutzte Programme heute / 7 Tage, bleibt auf diesem PC), **Sound** (Lautstärke, Stumm,
  Mikrofon, Wiedergabegerät wechseln) und **Dienst-Status** (RSI, Discord, Epic Games, GitHub … aus ihren Statusseiten).
- **Fokus-Modus**: Der Fokus-Timer wechselt während der Fokus-Runden zu einem gewählten Profil.
- **Profil-Tastenkürzel** Strg+Alt+F1…F9 (F10: alle Fences) und ein **Hintergrundbild pro Profil**.
- **Desktop-Assistent**: sortiert Desktop-Symbole nach Art in neue Fences (wird beim ersten Start angeboten).
- **Bildschirm-Lineal** in Pixel, Zentimeter oder Zoll (Werkzeuge ▸ Bildschirm-Lineal – im Tray und in jedem Fence-Menü).
- **Farbpipette** mit Lupe (kopiert #RRGGBB) und **Downloads aufräumen** (alte Dateien, größte zuerst, in den Papierkorb).
- **Schnellnotiz** von überall mit Strg+Alt+N; **Formatierung in Notizen** (Überschriften, Aufzählungen, Zitate, Linien, fett, kursiv).
- **Wiederkehrende Erinnerungen** (täglich, werktags, wöchentlich, monatlich) und ein Widget **To-do-Liste** mit Fälligkeiten.
- Widgets **Weltzeituhr**, **Energiesparplan** (auch pro Profil) und **Steam-Angebote** (Wunschliste zuerst).

### Geändert
- **Gruppierte Menüs**: Neues Widget ▸ Zeit & Planung / Info & News / System / Spiele & Medien; Style ▸ Basis /
  Gaming & Technik / Arbeit & Alltag / Freizeit / Post-it / Eigene Styles.
- **News** übersichtlicher: halbfette Überschriften auf bis zu zwei Zeilen, die Quelle in der Akzentfarbe des Styles, Trennlinien.

### Behoben
- News- und Kurs-Widget meldeten „Keine Verbindung zum Wetterdienst“, wenn ein Feed nicht ging.
- Eine Webseite als News-Feed findet jetzt den Feed, den die Seite angibt, oder sagt klar, dass es kein Feed ist.
- Ein fehlgeschlagenes Update löscht keine schon angezeigten Schlagzeilen, Kurse oder Termine mehr; neuer Versuch nach 30 Sekunden.
- Probleme der Online-Widgets landen in log.txt im Datenordner.

## [2.4.0] - 2026-10-03

### Neu
- **Widgets**: Uhr & Kalender, System-Monitor (CPU, RAM, GPU-Last und -Temperatur, FPS), Laufwerke, Papierkorb,
  Spielzeit und Countdown. Tray- oder Fence-Menü → Neues Widget.
- **Spielzeit** für beliebige Spiele: EXE auswählen, NoFences zeichnet auf, wie lange es läuft (heute, Woche, Monat, gesamt, läuft gerade).
- **Countdown** bis zu einem Datum mit Titel.
- Widgets **Wetter** (Open-Meteo, ohne Konto), **Medien** mit Steuerung, **Netzwerk** mit Verlauf und Ping,
  **Zwischenablage-Verlauf** (nur im Speicher, Passwort-Manager werden respektiert) und **Akku**.
- Widget **Spiele**: installierte Spiele aus Steam (mit Cover), Epic, GOG und der Xbox-App; Klick startet.
- **Termine** aus Kalender-Links (.ics: Google, Outlook, iCloud), auch wiederkehrende.
- Widgets **Fotorahmen**, **Fokus-Timer (Pomodoro)**, **News** (RSS/Atom mit fertigen Feeds) und **Kurse** (Aktien,
  Indizes, Krypto).
- **Suche in allen Fences** (Strg+Alt+F): Verknüpfungen, Ordnerinhalte, Reiter und Notizen.
- **Profile** wie „Arbeit“ und „Gaming“: im Tray umschalten, Fences per Rechtsklick → In Profil zeigen zuordnen.
- **Automatik**: Profil wechseln, solange ein Programm läuft oder zu festen Zeiten; Fences ausblenden, solange etwas im
  Vollbild läuft; heller/dunkler Standard-Style nach Windows oder Uhrzeit.
- **Mehrere PCs**: Fences in einem gemeinsamen Ordner wie OneDrive ablegen.
- Style **Windows-Akzentfarbe** – insgesamt 25 Styles.
- **Französisch und Spanisch**; Menü **Sprache** mit Flaggen im Tray und in jedem Fence-Menü.
- Knopf **Spenden** (Info-Fenster und Einstellungen → Updates).
- **FPS-Messung** (optional, standardmäßig aus): Ein kleiner Helfer mit Administratorrechten zählt die Bilder des Programms
  im Vordergrund; Windows fragt einmal, die Einstellungen erklären, warum.
- Fence **„Zuletzt verwendet“** und **Schnellstart-Leiste** (nur Icons, Namen als Tooltip).
- **Reiter** in Verknüpfungs-Fences.
- **Nur auf diesem virtuellen Desktop** pro Fence.
- **Export/Import** von Fences und eigenen Styles, z. B. für einen zweiten PC.
- **Einstellungsfenster** (Tray → Einstellungen) mit allem, was die ganze App betrifft; das Tray-Menü ist viel kürzer.
- **Fence-Einstellungen** neu gestaltet, mit Bereichen und Live-Vorschau.
- **Sprachen**: Englisch, Deutsch, **Italienisch**, **Französisch** und **Spanisch**; automatisch (Windows-Sprache,
  sonst Englisch) oder in den Einstellungen gewählt.
- **Info-Fenster** mit Version, Credits und Links.
- **8 neue Styles**: Dokumente, Multimedia, Musik, Sport, Fotos, Reisen, Kochen, Natur – insgesamt 24 Styles; jeder Style hat
  eine Akzentfarbe für Widgets.

### Behoben
- OK in den Einstellungen eines Verknüpfungs-Fences hat alle Verknüpfungen entfernt.
- Die Einstellungen eines Widgets zu öffnen führte zum Absturz.
- Beim Post-it ragten Einträge unten über den Schatten des Papiers hinaus.
- Das Tastenkürzel „Fences nach vorne holen“ klebte ohne Abstand am Menütext.
- Der ausgewählte Eintrag in der Seitenleiste der Einstellungen wurde unlesbar.

## [2.3.0] - 2026-10-02

### Neu
- **Mehrfachauswahl**: Strg-/Shift-Klick und Auswahlrechteck; Ziehen, Umsortieren und das Explorer-Menü
  funktionieren mit mehreren Einträgen.
- **Tastatur**: Enter öffnet, Entf entfernt Verknüpfungen (Ordner-Fences: Papierkorb), F2 benennt um, Strg+A,
  Strg+C, Pfeiltasten, Esc. Angeklickte Fences bekommen den Tastatur-Fokus, bleiben aber hinter anderen Fenstern.
- **Suche**: einfach tippen, wenn ein Fence aktiv ist; die Suche steht oben rechts, Esc löscht sie.
- **Einrasten** an Bildschirmrändern und anderen Fences beim Verschieben und Größe ändern; mit Alt frei platzieren.
- **Layouts pro Monitor-Setup**: Fences kehren an ihren Platz für die aktuellen Bildschirme zurück (z. B. Laptop an/ab Dock).
- **Sicherungen** der Konfiguration alle 12 Stunden (die letzten 10 bleiben); Tray → „Sicherung wiederherstellen“.
- **Erinnerungen für Notizen** (Fence-Menü → Erinnerung…) als Benachrichtigung mit Ton; die Uhrzeit steht im Titel.
- **Links in Notizen** (Webadressen, Pfade) sind unterstrichen und öffnen sich per Klick.
- **Post-it-Farben**: Gelb, Rosa, Grün, Blau und Orange.
- **Animationen**: sanftes Ein-/Ausklappen; Hover-Effekte bei Star Citizen, Gaming und Retro-Arcade. Tray → Animationen.
- **Eigene Styles** als JSON-Dateien im Ordner `themes` (ein Beispiel wird angelegt); Tray → Eigene Styles.
- Automatische Tests (45) laufen bei jedem Push; winget-Paketbeschreibung vorbereitet in `packaging/winget`.

- **Umbenennen direkt im Titel**: Doppelklick auf die Titelleiste (oder Fence-Menü → Umbenennen).

### Behoben
- Verschieben ruckelte: Die Position wird erst beim Loslassen gespeichert, Hover-Animationen pausieren während
  des Ziehens, und das Einrasten ist weniger klebrig.
- Tabs in Notizen werden mit denselben Tab-Stopps gezeichnet wie im Editor.
- Ein eingeklapptes Post-it zeigte nur einen Strich statt des Titels.
- Post-its ließen sich nicht in der Größe ändern (die Ziehzonen lagen im durchsichtigen Rand, der keine Klicks bekommt).

## [2.2.0] - 2026-10-01

### Neu
- **Notizen (Post-its)**: neuer Fence-Typ „Notiz“ (Tray- oder Fence-Menü → „Neue Notiz“). Doppelklick zum Schreiben,
  Esc oder ein Klick daneben speichert. Zeilen mit `[ ]` am Anfang werden zu Kästchen, die man per Klick abhakt.
  Auf eine Notiz gezogener Text wird angehängt.
- **Post-it-Style**: ein gelber Zettel, aufgeklebt auf den Desktop: durchsichtiger Hintergrund um das Papier,
  zwei halbdurchsichtige Klebestreifen, weicher Schatten mit leicht abstehenden Ecken, Eselsohr und Handschrift.
  Neue Notizen bekommen ihn automatisch. Styles können jetzt auf das Milchglas verzichten und freie Formen zeichnen.
- **Immer im Vordergrund** pro Fence (Fence-Menü), z. B. für eine Notiz neben einem Spiel im randlosen Fenstermodus.

### Geändert
- Neue Fences und Notizen erscheinen neben der Maus, auf dem Monitor, auf dem du gerade arbeitest, statt immer auf
  dem Hauptmonitor (wo sie z. B. ein Spiel verdecken kann).

## [2.1.0] - 2026-10-01

### Neu
- **Updates**: NoFences sucht auf GitHub nach neuen Versionen (kurz nach dem Start, dann alle 6 Stunden) und
  installiert sie mit einem Klick auf die Benachrichtigung oder Tray → „Update installieren“. Abschaltbar;
  „Jetzt nach Updates suchen“ im Tray.
- **Fences nach vorne holen** per Tastenkürzel (Standard Strg+Alt+D; Strg+Alt+Leertaste, Strg+Shift+D oder aus im Tray).
  Die Fences bleiben vorne, bis du das Kürzel nochmal drückst, Esc drückst, daneben klickst oder einen Eintrag öffnest.
- **Sortierung pro Fence**: manuell (Drag & Drop), Name, Typ, Änderungsdatum oder Größe. Fence-Menü → „Sortieren nach“
  oder in den Fence-Einstellungen. Ordner stehen immer zuerst.

## [2.0.0] - 2026-10-01

Erste Version dieses Forks. Neu aufgebaut auf .NET 10, basierend auf
[Twometer/NoFences](https://github.com/Twometer/NoFences) 1.x von Twometer und Mitwirkenden.

### Neu
- **Ordner-Fences**: zeigen den Inhalt eines Ordners live an. Reingezogene Dateien werden dorthin verschoben
  (Strg = kopieren), die Icons verschwinden also wirklich vom Desktop.
- **Auto-Sortieren**: Muster pro Fence (z. B. `*.pdf; *.docx`) mit Vorlagen für Bilder, Dokumente, Archive,
  Installer, Videos, Musik und Verknüpfungen. Neue Desktop-Dateien und fertige Browser-Downloads werden
  automatisch einsortiert; „Desktop jetzt aufräumen“ im Tray sortiert vorhandene Dateien.
- **Doppelklick auf leeren Desktop** blendet alle Fences aus und wieder ein.
- **11 Styles**, global oder pro Fence: Standard (Glas), Star Citizen (HUD), Retro-Arcade,
  Hardware (Platine), Nerd (Terminal), Hobby (Pinnwand), Arbeit (Business), Familie, Gaming (RGB),
  Finanzen (Börse) und Social.
- `--preview <ordner>` rendert alle Styles mit Beispiel-Einträgen als PNG.
- **Tray-Icon** mit neuer Fence, Fences ein-/ausblenden, Mit Windows starten, Dateiendungen,
  Konfigurationsordner öffnen, Beenden.
- „Mit Windows starten“ zusätzlich im Rechtsklick-Menü jedes Fences.
- **Hilfe** und **Was ist neu?** im Tray- und Fence-Menü, auf Deutsch und Englisch (folgt der Windows-Sprache).
  „Was ist neu?“ öffnet sich nach einem Update einmal von selbst.
- Drag & Drop: Einträge umsortieren, zwischen Fences verschieben, in den Explorer ziehen.
- Vorschaubilder für Bilder, Videos, PDFs usw.; Icongröße 24–96 px.
- Ein Einstellungsdialog pro Fence: Name, Typ, Ordner, Style, Titelhöhe, Icongröße, Farbe, Deckkraft,
  Auto-Sortieren, Sperren, Einklappen.
- Portable-Modus (`portable.txt` neben der EXE).
- App-Icon, eigenständige Einzel-EXE, GitHub-Release-Workflow mit Herkunftsnachweis
  (SignPath-Signatur vorbereitet).

### Geändert
- Von .NET Framework 4.8 auf .NET 10 portiert.
- Konfiguration in einer einzigen `fences.json`, sicher gespeichert. Fences aus 1.x werden automatisch übernommen.
- DPI-Unterstützung pro Monitor; Fences außerhalb des sichtbaren Bereichs werden zurückgeholt.
- Oberfläche auf Deutsch und Englisch (folgt der Windows-Sprache).

### Behoben
- Ruckeln beim Scrollen und Zeichnen mit vielen Einträgen (Icons werden jetzt zwischengespeichert und im Hintergrund geladen).
- Speicherlecks (GDI) beim Zeichnen.
- Vorschaubilder luden nach einigen defekten Bildern nicht mehr.
- Eine fehlende oder kaputte Fence-Datei verhinderte den Programmstart.
- Position/Größe ging verloren, wenn man kurz nach dem Verschieben beendet hat.
- Fences tauchten manchmal vor anderen Fenstern auf.
- Dokumente ließen sich nach der .NET-Portierung nicht öffnen („UseShellExecute“).
- Das Programm ließ sich nur beenden, indem man alle Fences löschte.
- Lange Namen ohne Leerzeichen wurden mitten im Wort umgebrochen; jetzt werden sie mit „…“ gekürzt.
- Electron-Apps (z. B. RSI Launcher, Discord) starteten nicht aus einem Fence, wenn NoFences selbst aus VS Code
  oder einer anderen Electron-App gestartet wurde (geerbtes `ELECTRON_RUN_AS_NODE`).

### Entfernt
- Chinesische und tschechische Übersetzung der 1.x-Dialoge (die Dialoge wurden ersetzt).

## [1.x]

Original-NoFences von Twometer, siehe [Twometer/NoFences](https://github.com/Twometer/NoFences).

[2.4.1]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.1
[2.4.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.0
[2.3.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.3.0
[2.2.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.2.0
[2.1.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.1.0
[2.0.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.0.0
[1.x]: https://github.com/Twometer/NoFences
