# NoFences Hilfe

NoFences legt Boxen („Fences“) auf deinen Desktop, die deine Icons ordnen. English version: [HELP.md](HELP.md).

## Erste Schritte

- Nach dem ersten Start gibt es einen leeren Fence. Zieh Dateien oder Ordner darauf.
- **Rechtsklick auf einen Fence** (Titel oder leere Fläche) öffnet sein Menü: Einstellungen, Style, Sperren,
  neuer Fence, löschen.
- **Rechtsklick auf einen Eintrag** zeigt das normale Explorer-Menü. Mit Shift + Rechtsklick kommt stattdessen das Fence-Menü.
- Das **Tray-Icon** (unten rechts, evtl. hinter dem ^-Pfeil) hat die App-Optionen und „Beenden“.

## Zwei Arten von Fences

- **Verknüpfungs-Fence** (Standard): zeigt Verweise auf Dateien und Ordner. Die Dateien bleiben, wo sie sind,
  also auch auf dem Desktop. Löschst du das Original, verschwindet es auch aus dem Fence.
- **Ordner-Fence**: zeigt den Inhalt eines Ordners. Reingezogene Dateien werden in diesen Ordner **verschoben**
  (mit Strg kopiert), sie verschwinden also wirklich vom Desktop. Anlegen über „Neuer Ordner-Fence…“.

- **Notiz**: ein Post-it mit Text statt Dateien. Anlegen über „Neue Notiz“.

Tipp: Für einen aufgeräumten Desktop einen Ordner wie `Dokumente\Fences\Arbeit` anlegen und als Ordner-Fence verwenden.

## Mit Einträgen arbeiten

- Doppelklick öffnet einen Eintrag.
- Einträge ziehen zum Umsortieren, auf einen anderen Fence zum Verschieben oder in den Explorer.
- Mit dem Mausrad scrollen, wenn ein Fence voll ist.
- „Aus Fence entfernen“ (Verknüpfungs-Fences) entfernt nur den Verweis, nie die Datei.

## Auswahl, Tastatur und Suche

- **Strg+Klick** wählt mehrere Einträge, **Shift+Klick** einen Bereich; auf freier Fläche ziehen zeichnet ein
  **Auswahlrechteck**. Ausgewählte Einträge lassen sich gemeinsam ziehen und per Rechtsklick bearbeiten.
- Ein angeklickter Fence reagiert auf die Tastatur:
  - **Enter** öffnet, **F2** benennt um, **Strg+C** kopiert, **Strg+A** wählt alles, Pfeiltasten bewegen die Auswahl.
  - **Entf**: im Verknüpfungs-Fence wird nur der Verweis entfernt, im Ordner-Fence kommt die Datei in den Papierkorb.
  - **Lostippen** sucht: Der Fence zeigt nur noch passende Einträge, das Suchwort steht oben rechts. **Esc** beendet die Suche.

## Notizen

- **Doppelklick** auf die Notiz zum Schreiben; **Esc** oder ein Klick daneben speichert.
- Zeilen, die mit `[ ]` beginnen, werden zu Kästchen. Ein Klick hakt sie ab (`[x]`) und streicht die Zeile durch.
- Text, den du auf eine Notiz ziehst (z. B. aus dem Browser), wird unten angehängt.
- Webadressen und Pfade (z. B. `www.example.com`, `C:\Ordner\Datei.pdf`) werden unterstrichen und öffnen sich per Klick.
- Fence-Menü → **Erinnerung…**: Zu der Zeit meldet sich NoFences mit Ton und Benachrichtigung; ein Klick darauf
  holt die Notiz nach vorne. Solange eine Erinnerung gesetzt ist, steht die Uhrzeit oben rechts.
- Neue Notizen haben den Post-it-Style (gelb); Rosa, Grün, Blau und Orange gibt es unter Fence-Menü → **Style**.
- Neue Notizen und Fences erscheinen neben der Maus.
- Fence-Menü → **Immer im Vordergrund** hält eine Notiz über allen Fenstern. Über Spielen klappt das nur im
  Modus „Randloses Fenster“, nicht im exklusiven Vollbild.

## Verschieben und Größe ändern

- Titelleiste ziehen zum Verschieben, Ränder ziehen für die Größe.
- **Gesperrte** Fences lassen sich nicht verschieben, nicht in der Größe ändern und nehmen nichts an.
- **Einklappen wenn Maus weg** verkleinert den Fence auf die Titelleiste, bis du darauf zeigst.
- Beim Verschieben und Größe ändern **rasten** Fences an Bildschirmrändern und an anderen Fences ein. Mit
  gedrückter **Alt**-Taste platzierst du frei.
- NoFences merkt sich die Positionen **pro Monitor-Setup**: Steckst du einen Monitor ab und wieder an, wandern
  die Fences an ihren jeweiligen Platz zurück.

## Automatisch einsortieren

In den Fence-Einstellungen unter „Vom Desktop einsortieren“ Muster eintragen, z. B. `*.pdf; *.docx`, oder eine
Vorlage wählen (Bilder, Dokumente, Archive, Installer, Videos, Musik, Verknüpfungen).

- Neue Dateien auf dem Desktop, die passen, werden in diesen Ordner-Fence verschoben (bzw. im Verknüpfungs-Fence verlinkt).
- Downloads werden einsortiert, sobald sie fertig sind.
- Tray → **Desktop jetzt aufräumen** sortiert die Dateien, die schon auf dem Desktop liegen.
- Tray → **Neue Desktop-Dateien automatisch einsortieren** schaltet es aus und ein.
- Überwacht wird nur dein eigener Desktop, nicht der gemeinsame „Öffentliche“ Desktop.

## Fences ausblenden

- **Doppelklick auf eine leere Stelle des Desktops** blendet alle Fences aus, noch ein Doppelklick wieder ein.
  Abschaltbar im Tray-Menü.
- Tray → **Fences anzeigen** macht dasselbe, ebenso ein Doppelklick auf das Tray-Icon.

## Fences nach vorne holen

- **Strg+Alt+D** holt alle Fences vor die offenen Fenster, ohne etwas zu minimieren.
- Sie bleiben vorne, bis du das Kürzel nochmal drückst, **Esc** drückst, daneben klickst oder einen Eintrag öffnest.
- Tray → **Tastenkürzel**: Strg+Alt+Leertaste, Strg+Shift+D oder aus. Ist ein Kürzel schon von einem anderen
  Programm belegt, meldet sich NoFences.

## Sortieren

Fence-Menü → **Sortieren nach**: Manuell (Drag & Drop), Name, Typ, Änderungsdatum (neueste zuerst) oder
Größe (größte zuerst). Ordner stehen immer zuerst. Umsortieren per Ziehen geht nur bei „Manuell“.

## Updates

- NoFences schaut kurz nach dem Start und dann alle 6 Stunden auf GitHub nach einer neuen Version.
- Gibt es eine, erscheint eine Benachrichtigung. Ein Klick darauf (oder Tray → **Update installieren**) lädt sie,
  tauscht die EXE aus und startet NoFences neu. Danach zeigt „Was ist neu?“ die Änderungen.
- Tray → **Jetzt nach Updates suchen** prüft sofort; **Automatisch nach Updates suchen** schaltet die Prüfung ab.
- Dabei wird nur die öffentliche GitHub-Seite des Projekts abgefragt, es werden keine Daten von dir gesendet.

## Styles

16 Styles: Standard (Glas), Star Citizen (HUD), Retro-Arcade, Hardware, Nerd, Hobby, Arbeit, Familie, Gaming,
Finanzen, Social und Post-it in fünf Farben.

- Tray → **Standard-Style** gilt für alle Fences.
- Fence-Menü → **Style** überschreibt ihn für einen Fence.
- Farbe und Deckkraft stellst du in den Fence-Einstellungen ein (Farbe nur beim Standard-Style).
- Tray → **Animationen** schaltet sanftes Einklappen und die Hover-Effekte (Star Citizen, Gaming, Retro-Arcade) ein und aus.

### Eigene Styles

Tray → **Eigene Styles → Styles-Ordner öffnen**. Dort liegt `beispiel-mocha.json`: kopieren, umbenennen, Farben
ändern (`#RRGGBB` oder `#RRGGBBAA`, wobei AA die Deckkraft ist), dann **Styles neu laden**. Eigene Styles stehen mit ★
in der Style-Liste. Fehlerhafte Dateien meldet NoFences mit dem Grund.

## Einstellungen pro Fence

Rechtsklick → **Fence-Einstellungen…**: Name, Typ (Verknüpfungen/Ordner), Ordner, Style, Sortierung, Titelhöhe, Icongröße,
Hintergrundfarbe, Deckkraft, Einsortier-Muster, Gesperrt, Einklappen.

## App-Optionen (Tray-Menü)

- **Mit Windows starten** (auch in jedem Fence-Menü).
- **Dateiendungen anzeigen**: wie im Explorer, immer oder nie.
- **Konfigurationsordner öffnen**: dort liegen die Einstellungen.
- **Sicherung wiederherstellen**: NoFences sichert die Einstellungen alle 12 Stunden (die letzten 10 bleiben).
  Ein Klick auf eine Sicherung setzt alle Fences auf diesen Stand zurück und startet NoFences neu.
- **Beenden**.

## Häufige Fragen

**Kann ich das Original löschen, nachdem ich ein Icon in einen Fence gezogen habe?**
Bei einem Verknüpfungs-Fence: nein, der Fence verweist nur darauf. Bei einem Ordner-Fence wurde die Datei verschoben,
es gibt also nichts mehr zu löschen.

**Windows zeigt beim Start eine SmartScreen-Warnung.**
Die EXE ist noch nicht signiert. Auf „Weitere Informationen“ → „Trotzdem ausführen“ klicken. Eine Signatur ist geplant.

**Wo liegen meine Einstellungen?**
In `%LocalAppData%\NoFences\fences.json`. Liegt eine leere `portable.txt` neben der `NoFences.exe`, werden sie stattdessen neben der EXE gespeichert.

**Ein Fence ist außerhalb des Bildschirms verschwunden.**
NoFences neu starten; Fences außerhalb aller Bildschirme werden automatisch zurückgeholt.

**Wie deinstalliere ich NoFences?**
Tray → „Mit Windows starten“ abhaken, Tray → Beenden, dann `NoFences.exe` und den Ordner `%LocalAppData%\NoFences` löschen.
Dateien in Ordner-Fences bleiben in ihren Ordnern.
