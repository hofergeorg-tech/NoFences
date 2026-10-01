# Änderungsprotokoll

Alle wichtigen Änderungen an diesem Fork. English version: [CHANGELOG.md](CHANGELOG.md).

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

[2.1.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.1.0
[2.0.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.0.0
[1.x]: https://github.com/Twometer/NoFences
