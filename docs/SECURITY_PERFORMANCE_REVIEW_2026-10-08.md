# Security- & Performance-Review — NoFences (Fork)

**Datum:** 2026-10-08
**Reviewer:** Claude (Opus 5.5)
**Stand:** v2.10.0 (`071a48e`), laufende Instanz `D:\Fences\app\NoFences.exe`
**Umfang:** Statische Durchsicht von `NoFences/` mit Fokus auf (a) Ursachen für
gelegentliche Hänger und (b) Sicherheit einer Desktop-App (keine Server-Seite,
kein Login — Angriffsfläche sind Dateien, Netzwerkdaten, Prozessstarts, Rechte).

> **Status (2026-10-09, v2.13.0): alle Befunde erledigt.** P1–P8 behoben bzw. die
> betroffenen Widgets entfernt, S1 durch **Entfernen der FPS-Messung** („kein Admin ist
> besser“), S2–S4 behoben, S5 bewusst so belassen. Offen bleibt nur die Code-Signatur
> (SignPath-Freigabe ausstehend).
>
> | Befund | Status |
> |---|---|
> | P1 Maus-Hook | ✅ eigener Thread mit eigener Nachrichtenschleife (`Win32/DesktopDoubleClickHook.cs`) |
> | P2 Drop kopiert synchron | ✅ `ShellFileOps.InBackground` (STA-Thread, ohne Owner); Quelle bekommt „Copy" gemeldet, damit sie die Originale nicht selbst löscht |
> | P3 Zwischenablage | ✅ 150 ms entprellt, Lesen + Bildumwandlung auf eigenem STA-Thread, hängende Quelle blockiert max. 10 s neue Lesevorgänge |
> | P4 Öffnen | ✅ `Util/Launcher` (STA-Thread) für Fence-Einträge und Notiz-Links |
> | Diagnose | ✅ `Util/UiWatchdog`: Log-Zeile `Freeze: UI blocked N ms during: …`, alle Dauer-Timer, Paint, Drop, Widgets markiert; Abstürze → `Crash:` im Log |
> | S1 FPS-Helfer | ✅ entfernt (inkl. TraceEvent-Paket); wer ihn an hatte, bekommt einmal das Angebot, die Admin-Aufgabe zu löschen; `--fps-helper` aus einer alten Aufgabe tut nichts mehr |
> | S3 Import mit Netzwerkpfaden | ✅ 2.12.0: Import listet Netzwerkpfade (alle Felder, auch Notizen/Widget-Optionen) und übernimmt standardmäßig ohne sie; nur `.json` als Stil; Netzwerk-Einträge werden erst beim Öffnen angefasst (Typ-Symbol, keine Vorschau, kein `File.Exists`) |
> | P5, P7 | ✅ 2.13.0: betroffene Widgets **entfernt** statt repariert (Nutzerentscheid „zu viel ist auch nicht gut“): System-Monitor, Laufwerke, Papierkorb, Spiele, Spielzeit, dazu Steam-Angebote, Twitch, Webseite (WebView2-Paket raus). Fences dieser Typen werden beim Start herausgenommen + Hinweis |
> | P6 Auto-Sortieren | ✅ 2.13.0: `File.Move` im Hintergrund |
> | P8 | ✅ 2.13.0: Widget-Fehler ins Log (nur bei neuem Fehler), IconCache verwirft Bilder mit altem Zeitstempel |
> | S2 Update-Prüfsumme | ✅ 2.13.0: SHA-256 aus dem GitHub-`digest` wird geprüft (Signatur weiter offen, SignPath) |
> | S4 Zwischenablage | ✅ 2.13.0: passwortartige Einträge maskiert, ohne Tooltip, nicht anheftbar (also nie im Klartext in fences.json) |
> | S5 | ✔ bewusst so belassen (Nutzerentscheid 2026-10-09): Einzelplatz-PC, ohne FPS-Helfer kein Risiko |

---

## Befund der laufenden Instanz (Messung)

| Messwert | Wert | Bewertung |
|---|---|---|
| Laufzeit / CPU | 3 h 20 min / 151 s (~1,3 % eines Kerns) | ok, leicht hoch für Leerlauf |
| Arbeitsspeicher | 177 MB WS / 113 MB privat | ok |
| GDI / USER-Objekte | 125 / 112 (Peak 139) | **kein Handle-Leck** |
| Handles / Threads | 899 / 14 | ok |
| Windows-Ereignisse (30 Tage) | keine `AppHang` (1002); einziger Absturz 01.10. in einer Test-Kopie (UpdateChecker-Static-Init, längst behoben) | Hänger sind **kurz** (Sekunden), kein „Keine Rückmeldung"-Abbruch |
| `logs\log.txt` | nur News-Feed-Fehler vom 03.10. | **Hänger werden nirgends protokolliert** |

Aktive Funktionen bei dir: 7 Ordner-Fences, 1 Notiz, Zwischenablage-Widget,
Doppelklick-auf-Desktop, Hover-Vorschau, Vollbild-Ausblenden, Auto-Stil nach Uhrzeit.
Auto-Sortieren ist an, aber ohne Muster (also inaktiv). FPS-Helfer aus.

---

## Teil A — Performance / Hänger

Grundproblem: Fast alles läuft auf **dem einen UI-Thread** (WinForms-Timer,
FileSystemWatcher mit `SynchronizingObject`, Widget-`Refresh()`). Jede blockierende
Operation friert **alle** Fences gleichzeitig ein.

### P1 — Maus-Hook auf dem UI-Thread verstärkt jeden Hänger systemweit  ⚠️ Hoch

**Ort:** `Win32/DesktopDoubleClickHook.cs:80` (`SetWindowsHookEx(WH_MOUSE_LL, …)` im UI-Thread)

Ein Low-Level-Maus-Hook wird von Windows **für jede Mausbewegung im ganzen System**
synchron im Thread aufgerufen, der ihn installiert hat. Ist der NoFences-UI-Thread
gerade 200 ms beschäftigt, wartet **jede Mausnachricht aller Programme** (auch Star
Citizen) bis zum `LowLevelHooksTimeout`. Ergebnis: Die *ganze* Maus ruckelt/hängt,
nicht nur NoFences — das passt zum Symptom „hin und wieder mal so Hänger".
Zusätzlich entfernt Windows einen Hook nach wiederholten Timeouts **still** →
Doppelklick-auf-Desktop funktioniert danach nicht mehr bis zum Neustart.

**Behebung:** Hook in einen eigenen Thread mit eigener Nachrichtenschleife
(`Application.Run()` auf separatem Thread) verlegen; der Hook-Callback postet wie
bisher per `ui.Post` an den UI-Thread. Kleine, isolierte Änderung, größte Wirkung.

### P2 — Drag & Drop in einen Fence kopiert synchron (blockiert NoFences **und** Explorer)  ⚠️ Hoch

**Ort:** `FenceWindow.cs:1376-1377` (`ShellFileOps.Move/Copy` → `SHFileOperation` im `OnDragDrop`)

Während `OnDragDrop` läuft, wartet auch die Quelle (Explorer/Desktop) auf das Ende
des Drops. Große Dateien von C:\ (Desktop) nach D:\Fences\… = echtes Kopieren →
NoFences **und** das Explorer-Fenster hängen bis zum Ende; über P1 zusätzlich die Maus.

**Behebung:** Pfadliste im Drop nur übernehmen, Handler sofort verlassen, die
Shell-Operation per `BeginInvoke` bzw. auf einem eigenen STA-Thread ausführen
(`IFileOperation` mit Fortschrittsdialog, Owner = Fence).

### P3 — Zwischenablage wird synchron auf dem UI-Thread gelesen  Mittel

**Ort:** `Widgets/ClipboardWidget.cs:49-56` (`Clipboard.GetDataObject`, `GetData`, `Clipboard.GetImage`)

- Viele Programme (Office, Browser, VS Code, Bildbearbeitung) nutzen *verzögertes
  Rendern*: Die Daten werden erst beim Abruf erzeugt. NoFences wartet dann auf das
  andere Programm — ist das gerade beschäftigt/hängt, hängt NoFences mit.
- `Clipboard.GetImage()` wandelt einen 4K-Screenshot auf dem UI-Thread in ein
  Bitmap um (nur das PNG-Kodieren danach läuft im Hintergrund).
- Es werden 4–6 Formate nacheinander abgefragt (`GetFormats`, `CanIncludeInClipboardHistory`, Text, Bitmap) — jede Abfrage öffnet die Zwischenablage erneut.

Du hast genau dieses Widget aktiv → **wahrscheinlichster Auslöser im Alltag**
(Hänger direkt nach Strg+C/Screenshot), verstärkt durch P1.

**Behebung:** Bei `WM_CLIPBOARDUPDATE` nur einen kurzen Timer (≈150 ms) starten
(entprellt Mehrfach-Updates), dann auf einem eigenen STA-Thread lesen und das
Ergebnis per `Post` übergeben. Bilder dort direkt als DIB → PNG verarbeiten.

### P4 — Programme/Dateien werden auf dem UI-Thread gestartet  Mittel

**Ort:** `Model/FenceEntry.cs:83` u. a. (`Process.Start(... UseShellExecute = true)`)

`ShellExecute` kann Sekunden brauchen (Kontextmenü-/Shell-Erweiterungen,
Virenscanner, Netzwerk-Verknüpfungen, langsam startende Launcher wie der RSI
Launcher). Solange friert alles ein.

**Behebung:** `Task.Run(() => Process.Start(...))` (Fehler-MessageBox per `Post` zurück).

### P5 — Prozessliste alle 15 s auf dem UI-Thread  Niedrig (nur mit Spiele-Widget)

**Ort:** `NoFencesApp.Playtime.cs:41` (`Process.GetProcesses()` + Bildpfad + `StartTime` je Prozess),
`NoFencesApp.Automation.cs:118` (nur bei Profilregeln „Programm läuft").

Je nach Prozessanzahl 20–150 ms; `StartTime` öffnet jeden Prozess einzeln. Bei
dir aktuell **inaktiv** (kein Spiele-/Spielzeit-Widget, keine Programmregel).

**Behebung:** Auf einen Hintergrund-Task verlagern, Ergebnis posten.

### P6 — Auto-Sortieren verschiebt große Dateien synchron  Niedrig (bei dir inaktiv)

**Ort:** `Model/AutoSorter.cs:137` (`File.Move` Desktop C:\ → Fence-Ordner D:\ = Kopieren)

Ein 2-GB-Download auf dem Desktop würde den UI-Thread für die gesamte Kopierzeit
blockieren. Bei dir sind keine Muster gesetzt → aktuell kein Effekt.

**Behebung:** Verschieben in `Task.Run`, `sorted(fence)` per `Post`.

### P7 — Widgets laufen auf dem UI-Thread  Niedrig/Info

**Ort:** `FenceWindow.Widget.cs:49` (`widget.Refresh()` im WinForms-Timer)

Netzwerk-Widgets sind async (ok). Synchron und potenziell langsam:
`SystemStats` erzeugt alle 10 s **alle** „GPU Engine"-PerformanceCounter neu
(`Widgets/SystemStats.cs:72`, nur ohne NVIDIA-Karte; kann 0,5–2 s dauern), Laufwerke-
und Papierkorb-Widget (schlafende HDD / getrenntes Netzlaufwerk). Bei dir nicht aktiv.

### P8 — Kleinere Punkte  Info

- `Util/IconCache.cs:60`: `MakeKey` ruft bei **jedem** `Get` (pro Eintrag pro
  Neuzeichnen) `File.GetLastWriteTimeUtc` auf dem UI-Thread auf. Lokal billig, bei
  Einträgen auf Netz-/USB-Laufwerken, die gerade weg sind, blockierend. Außerdem
  wird der Cache nie geleert (alte Zeitstempel-Schlüssel bleiben).
- Kein globaler Fehler-Handler (`Application.ThreadException`,
  `AppDomain.UnhandledException`) → eine Ausnahme in einem Timer zeigt den
  WinForms-Fehlerdialog bzw. beendet den Prozess ohne Logeintrag. Widget-Fehler
  gehen nur nach `Debug.WriteLine` (im Release unsichtbar).

### Diagnose-Vorschlag (damit wir die Hänger *finden* statt raten)

1. **UI-Watchdog** (klein, im Release eingebaut): Hintergrund-Thread postet alle
   250 ms einen Ping an den UI-Thread; kommt er nicht innerhalb von 500 ms zurück,
   wird `UI blockiert 1830 ms während: <Aktion>` in `logs\log.txt` geschrieben. Die
   „Aktion" setzen zentrale Stellen (Timer-Ticks, Widget-Refresh, Drop, Clipboard,
   Öffnen) per `using (Activity.Of("Clipboard"))`. Nach ein paar Tagen Log ist die
   Ursache eindeutig.
2. Für einen konkreten Hänger: `procdump -h -ma NoFences.exe` (Sysinternals)
   schreibt bei „Keine Rückmeldung" einen Dump mit Stack des UI-Threads.

---

## Teil B — Sicherheit

Positiv: Notizverschlüsselung sauber (AES-256-GCM, PBKDF2-SHA256 mit 200 000
Iterationen, zufälliges Salt/Nonce, `Model/NoteCrypto.cs`); RSS/XML mit
`DtdProcessing.Ignore` + `XmlResolver = null` (kein XXE, `NewsWidget.cs:223`);
News-Links nur `http/https` (`NewsWidget.cs:369`); Zwischenablage respektiert
„nicht aufzeichnen"-Markierungen von Passwortmanagern; Passwort-Generator umgeht
den Verlauf; Kalender-Log schreibt nur den Host (keine privaten ICS-Tokens);
Update-Check über HTTPS + Größenprüfung; WebView2 ohne Pop-ups/Host-Objekte;
Export-Import schützt Stil-Dateinamen per `Path.GetFileName` (kein Path-Traversal).

### S1 — FPS-Helfer: Aufgabe mit Admin-Rechten zeigt auf eine für Benutzer beschreibbare EXE  Mittel (latent)

**Ort:** `FpsHelper.cs:164` (`schtasks /Create … /RL HIGHEST` → `Environment.ProcessPath`)

Wird der FPS-Helfer eingeschaltet, legt NoFences eine geplante Aufgabe an, die
`D:\Fences\app\NoFences.exe` **mit höchsten Rechten ohne UAC-Abfrage** startet und
von jedem Benutzerprozess per `schtasks /Run` ausgelöst werden kann. Der Ordner ist
für „Authentifizierte Benutzer" änderbar (`icacls`: `(M)`). Jede Schadsoftware, die
mit normalen Rechten läuft, kann also die EXE ersetzen und die Aufgabe starten →
**stille Rechteausweitung auf Admin** (UAC-Umgehung). Zusätzlich löscht/schreibt der
erhöhte Prozess Dateien in einem benutzerbeschreibbaren Ordner (`fps.stop`,
`fps.json` → Junction-/Symlink-Tricks möglich).

Bei dir **aktuell nicht aktiv** (Aufgabe existiert nicht, FPS-Helfer aus).

**Behebung:** Für die Aufgabe eine Kopie des Helfers an einen nur für Admins
beschreibbaren Ort legen (z. B. `%ProgramFiles%\NoFences\`), bei jedem Start Hash
prüfen; Kommunikation über einen Ordner mit eingeschränkter ACL. Alternativ ganz auf
die Aufgabe verzichten (UAC-Abfrage beim Einschalten in Kauf nehmen).

### S2 — Selbst-Update ohne Signatur-/Hash-Prüfung  Niedrig/Mittel

**Ort:** `Util/UpdateChecker.cs:78-84`

Die neue EXE wird nur auf die Größe geprüft und dann gestartet. Schutz hängt allein
an TLS + dem GitHub-Konto. Ein kompromittiertes GitHub-Token/-Konto oder eine
manipulierte Release-Datei verteilt sich automatisch an alle Nutzer.

**Behebung:** (a) Authenticode-Signatur (wie bei sc-playtime über SignPath geplant)
und vor dem Tausch `WinVerifyTrust` + Herausgeber prüfen; (b) mindestens den
`digest` (SHA-256), den die GitHub-API pro Asset liefert, vergleichen — schützt vor
beschädigten/abgefangenen Downloads, nicht vor Kontoübernahme.

### S3 — Import fremder Fence-Dateien kann auf Netzwerkpfade/Programme zeigen  Niedrig

**Ort:** `NoFencesApp.Transfer.cs:38`, `Model/FenceExport.cs` (übernimmt `FolderPath`, `Files`, `OpenWith`, Hintergrundbilder …)

Eine geteilte „Vorlage" (`nofences-export`) kann Einträge auf `\\server\share\…`
enthalten. Schon das Anzeigen (Symbole/Vorschau laden) löst eine SMB-Anmeldung aus →
**NTLM-Hash geht an fremden Server**. Harmlos aussehende Einträge können auf EXEs zeigen.
`WriteThemes` schreibt beliebige Dateinamen (nicht nur `*.json`) in den Stile-Ordner.

**Behebung:** Beim Import UNC-/`http`-Pfade und `OpenWith` anzeigen und bestätigen
lassen oder entfernen; Stil-Dateien auf `.json` beschränken.

### S4 — Zwischenablage-Verlauf: angeheftete Einträge im Klartext  Info

Angeheftete Texte stehen im Klartext in `fences.json` (und damit in Backups und im
Sync-Ordner). Für Passwörter, die *nicht* von einem Passwortmanager markiert sind
(z. B. aus einer Textdatei kopiert), landet der Text im RAM-Verlauf (15 Einträge).
Akzeptabel, aber im Hilfetext erwähnenswert; optional „Verlauf bei Sperre leeren".

### S5 — Daten-/Programmordner für alle Benutzer änderbar  Info

`D:\Fences\app` erbt `Authentifizierte Benutzer: Ändern`. Auf einem Einzelplatz-PC
ohne weitere Konten unkritisch; relevant nur in Verbindung mit S1.

---

## Empfohlene Reihenfolge

1. **P1** Maus-Hook in eigenen Thread — kleine Änderung, beseitigt den systemweiten Ruckel-Effekt.
2. **UI-Watchdog** einbauen — liefert die echte Ursache aus dem Alltag.
3. **P3** Zwischenablage asynchron, **P2** Drop asynchron, **P4** Starten asynchron.
4. **S1** FPS-Helfer absichern (bevor jemand ihn einschaltet / bevor diese Datei öffentlich wird).
5. **S2** Hash-Prüfung jetzt, Signatur sobald SignPath steht.
6. Rest (P5–P8, S3–S5) nach Bedarf.
