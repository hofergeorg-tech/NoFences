# Änderungsprotokoll

Alle wichtigen Änderungen an diesem Fork. English version: [CHANGELOG.md](CHANGELOG.md).

## [2.4.0] - 2026-10-03

### Neu
- **Widgets**: Uhr & Kalender, System-Monitor (CPU, RAM, GPU-Last und -Temperatur, FPS), Laufwerke, Papierkorb,
  Spielzeit und Countdown. Tray- oder Fence-Menü → Neues Widget.
- **Spielzeit** für beliebige Spiele: EXE auswählen, NoFences zeichnet auf, wie lange es läuft (heute, Woche, Monat, gesamt, läuft gerade).
- **Countdown** bis zu einem Datum mit Titel.
- **FPS-Messung** (optional, standardmäßig aus): Ein kleiner Helfer mit Administratorrechten zählt die Bilder des Programms
  im Vordergrund; Windows fragt einmal, die Einstellungen erklären, warum.
- Fence **„Zuletzt verwendet“** und **Schnellstart-Leiste** (nur Icons, Namen als Tooltip).
- **Reiter** in Verknüpfungs-Fences.
- **Nur auf diesem virtuellen Desktop** pro Fence.
- **Export/Import** von Fences und eigenen Styles, z. B. für einen zweiten PC.
- **Einstellungsfenster** (Tray → Einstellungen) mit allem, was die ganze App betrifft; das Tray-Menü ist viel kürzer.
- **Fence-Einstellungen** neu gestaltet, mit Bereichen und Live-Vorschau.
- **Sprachen**: Englisch, Deutsch und **Italienisch**; automatisch (Windows-Sprache, sonst Englisch) oder in den Einstellungen gewählt.
- **Info-Fenster** mit Version, Credits und Links.
- **8 neue Styles**: Dokumente, Multimedia, Musik, Sport, Fotos, Reisen, Kochen, Natur – insgesamt 24 Styles; jeder Style hat
  eine Akzentfarbe für Widgets.

### Behoben
- OK in den Einstellungen eines Verknüpfungs-Fences hat alle Verknüpfungen entfernt.
- Die Einstellungen eines Widgets zu öffnen führte zum Absturz.
- Beim Post-it ragten Einträge unten über den Schatten des Papiers hinaus.
- Das Tastenkürzel „Fences nach vorne holen“ klebte ohne Abstand am Menütext.

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

[2.4.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.0
[2.3.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.3.0
[2.2.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.2.0
[2.1.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.1.0
[2.0.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.0.0
[1.x]: https://github.com/Twometer/NoFences
