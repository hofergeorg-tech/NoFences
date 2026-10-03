# NoFences Hilfe

NoFences legt Boxen („Fences“) auf deinen Desktop, die deine Icons ordnen – dazu Notizen und Widgets.
English: [HELP.md](HELP.md) · Italiano: [AIUTO.md](AIUTO.md)

## Erste Schritte

- Nach dem ersten Start gibt es einen leeren Fence. Zieh Dateien oder Ordner darauf.
- **Rechtsklick auf einen Fence** (Titel oder leere Fläche) öffnet sein Menü: Einstellungen, Style, Umbenennen,
  neuer Fence oder neues Widget, Löschen.
- **Rechtsklick auf einen Eintrag** zeigt das normale Explorer-Menü. Mit Shift + Rechtsklick kommt stattdessen das Fence-Menü.
- Das **Tray-Icon** (unten rechts, evtl. hinter dem ^-Pfeil) legt Fences an, blendet sie ein und aus und öffnet die
  **Einstellungen** für alles, was die ganze App betrifft.

## Arten von Fences

- **Verknüpfungs-Fence** (Standard): Verweise auf Dateien und Ordner. Die Dateien bleiben, wo sie sind, also auch auf
  dem Desktop. Löschst du das Original, verschwindet es auch aus dem Fence.
- **Ordner-Fence**: zeigt den Inhalt eines Ordners. Reingezogene Dateien werden dorthin **verschoben** (mit Strg kopiert),
  sie verschwinden also wirklich vom Desktop.
- **Notiz**: ein Post-it mit Text, siehe unten.
- **Widget**: Live-Inhalt – Uhr, System-Monitor, Laufwerke, Papierkorb, Spielzeit, Countdown, siehe unten.
- **Zuletzt verwendet**: die 20 zuletzt geöffneten Dateien (nur lesen).
- **Schnellstart-Leiste**: ein schmaler Verknüpfungs-Fence nur mit Icons; die Namen erscheinen als Tooltip.

Tipp: Für einen aufgeräumten Desktop einen Ordner wie `Dokumente\Fences\Arbeit` anlegen und als Ordner-Fence verwenden.

## Mit Einträgen arbeiten

- Doppelklick öffnet einen Eintrag. Einträge ziehen zum Umsortieren, auf einen anderen Fence zum Verschieben oder in den Explorer.
- **Strg+Klick** wählt mehrere Einträge, **Shift+Klick** einen Bereich; Ziehen auf freier Fläche zeichnet ein Auswahlrechteck.
- Ein angeklickter Fence reagiert auf die Tastatur: **Enter** öffnet, **F2** benennt um, **Entf** entfernt
  (Verknüpfungs-Fence: nur den Verweis; Ordner-Fence: Papierkorb), **Strg+A**, **Strg+C**, Pfeiltasten, **Esc**.
- **Lostippen** sucht; das Suchwort steht oben rechts, Esc beendet die Suche.
- Fence-Menü → **Sortieren nach**: manuell, Name, Typ, Änderungsdatum oder Größe.
- **Reiter** (Verknüpfungs-Fences): Fence-Menü → Reiter hinzufügen. Klick wechselt, Doppelklick benennt um, Einträge auf einen
  Reiter ziehen verschiebt sie dorthin.

## Verschieben, Größe, Umbenennen

- Titelleiste ziehen zum Verschieben, Ränder ziehen für die Größe. **Doppelklick auf den Titel** benennt um.
- Fences **rasten** an Bildschirmrändern und anderen Fences ein; mit **Alt** platzierst du frei.
- Positionen werden **pro Monitor-Setup** gemerkt: Monitor ab- und wieder anstecken, und die Fences kehren zurück.
- **Gesperrte** Fences lassen sich nicht verschieben oder ändern. **Einklappen wenn Maus weg** verkleinert auf die Titelleiste.
- **Immer im Vordergrund** hält einen Fence über allen Fenstern (über Spielen nur im Modus „Randloses Fenster“).
- **Nur auf diesem virtuellen Desktop** zeigt einen Fence nur auf dem aktuellen virtuellen Desktop (Win+Strg+Pfeiltasten).

## Notizen

- **Doppelklick** zum Schreiben; **Esc** oder ein Klick daneben speichert.
- Zeilen mit `[ ]` am Anfang werden zu Kästchen; ein Klick hakt sie ab und streicht die Zeile durch.
- Webadressen und Pfade sind unterstrichen und öffnen sich per Klick. Auf die Notiz gezogener Text wird angehängt.
- Fence-Menü → **Erinnerung…**: Zur gewählten Zeit meldet sich NoFences mit Ton und Benachrichtigung.
- Post-it-Style in Gelb, Rosa, Grün, Blau und Orange.

## Widgets

Tray- oder Fence-Menü → **Neues Widget**:

- **Uhr & Kalender**.
- **System-Monitor**: CPU, RAM, GPU-Last und -Temperatur (NVIDIA) und **FPS**, wenn aktiviert (siehe unten).
- **Laufwerke**: Füllstand und freier Platz; Klick öffnet das Laufwerk.
- **Papierkorb**: Dateien darauf ziehen löscht sie, Doppelklick öffnet ihn, im Menü leeren.
- **Spielzeit**: heute / diese Woche / diesen Monat / gesamt für ein Spiel, gelesen aus dem kostenlosen Tool SC Playtime,
  das beliebige Spiele aufzeichnen kann. Das Spiel wählst du im Menü des Widgets.
- **Countdown**: Tage und Stunden bis zu einem Datum; Doppelklick zum Festlegen.

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
mit Live-Vorschau. Es gibt 24 Styles – Glas, Star Citizen HUD, Retro-Arcade, Hardware, Nerd, Hobby, Arbeit, Familie, Gaming,
Finanzen, Social, Dokumente, Multimedia, Musik, Sport, Fotos, Reisen, Kochen, Natur und Post-it in fünf Farben.

**Eigene Styles**: Einstellungen → Daten & Styles → Styles-Ordner öffnen. `beispiel-mocha.json` kopieren, Farben ändern
(`#RRGGBB` oder `#RRGGBBAA`) und neu laden. Eigene Styles tragen einen ★.

## Einstellungen (Tray → Einstellungen)

- **Allgemein**: Sprache (automatisch, English, Deutsch, Italiano), mit Windows starten, Dateiendungen, Standard-Style, Animationen.
- **Desktop**: Doppelklick auf den Desktop blendet Fences aus/ein; Tastenkürzel, um Fences nach vorne zu holen (Strg+Alt+D); Einsortieren.
- **Updates**: NoFences prüft GitHub und installiert neue Versionen mit einem Klick.
- **FPS-Messung**: siehe oben.
- **Daten & Styles**: Fences exportieren/importieren (z. B. für einen zweiten PC), Sicherung wiederherstellen (alle 12 Stunden), Ordner.

## Häufige Fragen

**Kann ich das Original löschen, nachdem ich ein Icon in einen Fence gezogen habe?**
Bei einem Verknüpfungs-Fence nicht, er verweist nur darauf. Bei einem Ordner-Fence wurde die Datei verschoben, es gibt also nichts mehr zu löschen.

**Windows zeigt beim Start eine SmartScreen-Warnung.**
Die EXE ist noch nicht signiert. Auf „Weitere Informationen“ → „Trotzdem ausführen“ klicken.

**Wo liegen meine Einstellungen?**
In `%LocalAppData%\NoFences\fences.json` (Sicherungen daneben). Liegt eine leere `portable.txt` neben der `NoFences.exe`,
werden sie stattdessen neben der EXE gespeichert.

**Wie deinstalliere ich NoFences?**
Einstellungen → Allgemein: „Mit Windows starten“ abhaken; FPS-Messung ausschalten, falls genutzt; Tray → Beenden;
`NoFences.exe` und den Ordner `%LocalAppData%\NoFences` löschen. Dateien in Ordner-Fences bleiben in ihren Ordnern.
