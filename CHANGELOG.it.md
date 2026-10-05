# Registro delle modifiche

Tutte le modifiche importanti a questo fork.
English: [CHANGELOG.md](CHANGELOG.md) · Deutsch: [CHANGELOG.de.md](CHANGELOG.de.md) ·
Français : [CHANGELOG.fr.md](CHANGELOG.fr.md) · Español: [CHANGELOG.es.md](CHANGELOG.es.md)

## [2.10.0] - non pubblicata

### Novità
- **Note**: avanzamento della lista nel titolo, voci fatte in fondo o nascoste, liste che si azzerano ogni giorno/settimana/mese, sottovoci con frecce, contatori cliccabili `[3/8]`, calcoli (`650 + 80 =` → 730), `#tag` colorati (un clic cerca), tabelle, dimensione del testo con Ctrl+rotella, **modelli** (anche propri), le ultime 20 **versioni** da ripristinare, **esportazione** in Markdown o PDF e stampa.

## [2.9.0] - 2026-10-05

### Novità
- **Formattazione delle note**: sottolineato, barrato, evidenziazione a colori e bandierine di priorità (!!! / !! / !) oltre a grassetto e corsivo – con una barra di formattazione sopra l'editor e Ctrl+B / I / U / H.

### Modifiche
- Lo stile **Tagliere** ora usa foto vere (speck, formaggio, pane, salamino, cetrioli, ravanelli sul legno) invece di forme disegnate.

### Correzioni
- Un recinto che si espande (comprimi quando il mouse è fuori) ora si apre sempre sopra i recinti vicini.

## [2.8.0] - 2026-10-04

### Novità
- Nuovo stile **Tagliere (speck e formaggio)**: un tagliere di legno con speck, formaggio, pane e ravanelli. Le barre laterali possono avere uno **stile di sfondo** (Gruppo → Aggancia al bordo dello schermo → Sfondo della barra), ad es. un tavolo di legno con tovaglia a quadri e cibo tra i recinti.

## [2.7.0] - 2026-10-04

### Novità
- **Barra laterale**: aggancia un gruppo di recinti al bordo sinistro, destro, superiore o inferiore di un monitor (clic destro → Gruppo → Aggancia al bordo dello schermo). Entra quando il mouse tocca il bordo oppure riserva il suo spazio come la barra delle applicazioni; mai sopra i giochi a schermo intero.

## [2.6.0] - 2026-10-04

### Novità
- **Traduzioni proprie**: Impostazioni → Generale → "Traduzioni proprie…" apre la cartella `lang` con un modello inglese. Un file come `nl.json` aggiunge una lingua, un `it.json` con singoli testi cambia solo quelli. Tutti i testi ora sono in un file JSON per lingua.
- **Bandiere per ogni lingua**: il menu delle lingue mostra vere bandiere (flag-icons), così anche le lingue proprie hanno la loro bandiera (dal codice della lingua, da una regione come `pt-BR` o da `"_flag": "at"` nel file di lingua).
- **Annulla (Ctrl+Z)** per eliminare, spostare, ridimensionare e rinominare recinti e per rimuovere, spostare e rinominare elementi; anche "Annulla: …" nel menu della barra e del recinto.
- **Gruppi di recinti**: clic destro → Gruppo. I recinti di un gruppo si spostano insieme e si possono comprimere insieme alla barra del titolo.
- **Recinti**: aprire le cartelle nel recinto (freccia nell'angolo, contenuto rientrato sotto), **note sugli elementi** (tooltip), **utilizzo e riordino** (elementi mai aperti come suggerimento), **immagine di sfondo** o motivo per recinto e **apri i file con** un programma scelto.
- Le **note** riconoscono gli appuntamenti ("lun 14:00 Dentista", "domani 9:30 …", "12.10. 15:00 …") e propongono un promemoria 15 minuti prima; timer e sveglie che suonano si possono **posticipare** di 5 o 10 minuti. Nuovi strumenti: **generatore di password** (copia senza cronologia degli appunti) e **informazioni di rete** (indirizzi, Wi-Fi, router; un clic copia).

### Modifiche
- **Tutti i dati accanto a NoFences.exe**, ordinati in cartelle (`config`, `backups`, `themes`, `media`, `cache`, `logs`, `lang`). I dati delle versioni precedenti vengono copiati una volta; in una cartella del programma non scrivibile (Programmi, WinGet) restano in `%LocalAppData%\NoFences`, ordinati allo stesso modo.

## [2.5.0] - 2026-10-04

### Novità
- **Giochi**: tempo di gioco di ogni gioco trovato, contato automaticamente e mostrato sotto la copertina; ordinamento per i più giocati.
- Widget **notizie sui giochi** (annunci e note di patch dei tuoi giochi Steam) e **Twitch in diretta** (chi è in diretta, con avviso).
- **Offerte Steam** configurabili: offerte, più venduti, nuove uscite o solo la lista dei desideri; sconto minimo, prezzo massimo, numero.
- Widget **timer e sveglia** (timer rapidi, sveglie nei giorni scelti, suona anche se nascosto), **abitudini** (spunta gli ultimi 7 giorni, serie) e **avanzamento del tempo** (giorno, settimana, mese, anno).
- **Promemoria pausa** dopo 30–120 minuti di uso attivo del PC (Impostazioni → Automazione).
- Il **calendario dell'orologio** segna i giorni con appuntamenti.
- **Recinti**: contrassegni colorati per gli elementi (Maiusc+clic destro → Contrassegna, o Ctrl+1…6), ordinamento **più usati prima**, **anteprima al passaggio del mouse** (contenuto delle cartelle, anteprima grande di immagini/PDF).
- **Altri recinti**: **modelli** (postazione gaming, ufficio, minimale), un **ripiano** che si svuota da solo, **segnalibri del browser** (Chrome, Edge, Brave, Vivaldi, Opera) e **cartelle aperte di recente**.
- **Scorciatoia per ogni recinto** (Ctrl+Maiusc+F1…F12) lo porta in primo piano, anche da un altro profilo.
- I recinti possono **sfumare** quando il mouse è lontano (Impostazioni → Desktop); si possono escludere singoli recinti.
- Strumenti: **mostra/nascondi icone del desktop** e **sposta tutti i recinti su un altro monitor**.
- **Profili**: avvia programmi con un profilo (e, se vuoi, chiudili quando lo lasci); **sfondo in base all'ora** (Impostazioni → Automazione).
- **Note**: proteggi con password (cifrata, si blocca da sola dopo 2 minuti), incolla **immagini** con Ctrl+V, registra **note vocali**.
- La **cronologia degli appunti** conserva anche le immagini; **fissa** le voci (clic destro) perché restino in alto, anche dopo un riavvio.
- **Monitor di sistema** con grafico di due minuti e **avviso se la scheda grafica è troppo calda**; **speed test** nel widget di rete.
- Il **widget batteria** mostra anche **controller e dispositivi Bluetooth**; nuovo widget **Avvio automatico** (attiva/disattiva i programmi all'avvio di Windows).
- Strumenti: **codice QR** (testo o link dagli appunti), **lente d'ingrandimento**; "Riordina cartelle" trova i **file duplicati**.
- **Meteo**: avviso di pioggia per le prossime due ore («Pioggia tra circa 20 min»), alba e tramonto, fase lunare.
- **Designer di stili**: crea il tuo stile con pochi clic (colori, caratteri, bordo, angoli) con anteprima dal vivo; nuovo stile **Alto contrasto** con testo grande.
- Nuovo widget **Pagina web**: una piccola pagina (dashboard, pagina di stato …) direttamente nel recinto, aggiornata regolarmente.
- **Offerte Steam**: una lista dei desideri non pubblica funziona tramite il suo **link di condivisione**.
- Le **offerte Steam** mostrano tutte le offerte attuali (non solo quelle in evidenza; scorrendo se ne caricano altre) e ogni gioco scontato della tua lista dei desideri.

## [2.4.2] - 2026-10-03

### Modifiche
- **Pulisci cartelle** (prima: Pulisci Download): aggiungi altre cartelle oltre a Download; vengono cercate tutte
  insieme, una colonna mostra dove si trova ogni elemento. L'elenco delle cartelle viene salvato.

### Correzioni
- Gli elenchi scorrevoli (offerte Steam, notizie, appuntamenti, attività) restavano spostati e bloccati dopo aver ingrandito il recinto.

## [2.4.1] - 2026-10-03

### Novità
- La **ricerca** trova anche le app del menu Start e le pagine delle impostazioni di Windows, e calcola (`12*7`,
  `200*15%`; Invio copia).
- Widget **tempo di utilizzo** (programmi usati oggi / 7 giorni, resta su questo PC), **audio** (volume, muto, microfono,
  cambio dispositivo) e **stato dei servizi** (RSI, Discord, Epic Games, GitHub … dalle loro pagine di stato).
- **Modalità concentrazione**: il timer passa a un profilo scelto durante i giri di concentrazione.
- **Scorciatoie dei profili** Ctrl+Alt+F1…F9 (F10: tutti i recinti) e uno **sfondo per profilo**.
- **Assistente desktop**: ordina le icone del desktop in nuovi recinti per tipo (proposto al primo avvio).
- **Righello sullo schermo** in pixel, centimetri o pollici (Strumenti ▸ Righello sullo schermo – nella barra e in ogni menu dei recinti).
- **Contagocce** con lente (copia #RRGGBB) e **Pulisci Download** (file vecchi, i più grandi prima, nel cestino).
- **Nota rapida** da ovunque con Ctrl+Alt+N; **formattazione nelle note** (titoli, elenchi, citazioni, linee, grassetto, corsivo).
- **Promemoria ripetuti** (ogni giorno, feriali, settimanali, mensili) e un widget **Cose da fare** con scadenze.
- Widget **orologio mondiale**, **risparmio energia** (anche per profilo) e **offerte Steam** (prima la lista dei desideri).

### Modifiche
- **Menu raggruppati**: Nuovo widget ▸ Tempo e pianificazione / Info e notizie / Sistema / Giochi e media; Stile ▸ Base /
  Gaming e tecnologia / Lavoro e quotidiano / Tempo libero / Post-it / Stili personali.
- **Notizie** più leggibili: titoli in grassetto su un massimo di due righe, la fonte nel colore d'accento, linee di separazione.

### Correzioni
- I widget notizie e quotazioni dicevano "Nessuna connessione al servizio meteo" quando un feed non funzionava.
- Una pagina web inserita come feed ora trova il feed indicato dalla pagina, oppure dice chiaramente che non è un feed.
- Un aggiornamento non riuscito non cancella più titoli, quotazioni o appuntamenti già mostrati; nuovo tentativo dopo 30 secondi.
- I problemi dei widget online vengono scritti in log.txt nella cartella dei dati.

## [2.4.0] - 2026-10-03

### Novità
- **Widget**: orologio e calendario, monitor di sistema (CPU, RAM, carico e temperatura GPU, FPS), unità, cestino,
  tempo di gioco e conto alla rovescia. Menu della barra o del recinto → Nuovo widget.
- **Tempo di gioco** per qualsiasi gioco: scegli l'eseguibile, NoFences registra per quanto tempo è in esecuzione
  (oggi, settimana, mese, totale, in corso).
- **Conto alla rovescia** fino a una data, con titolo.
- Widget **meteo** (Open-Meteo, senza account), **in riproduzione** con controlli, **rete** con grafico e ping,
  **cronologia appunti** (solo in memoria, rispetta i gestori di password) e **batteria**.
- Widget **giochi**: i giochi installati da Steam (con copertina), Epic, GOG e dall'app Xbox; un clic li avvia.
- **Appuntamenti** dai link dei calendari (.ics: Google, Outlook, iCloud), anche ricorrenti.
- Widget **cornice foto**, **timer di concentrazione (Pomodoro)**, **notizie** (RSS/Atom con feed pronti) e
  **quotazioni** (azioni, indici, cripto).
- **Ricerca in tutti i recinti** (Ctrl+Alt+F): collegamenti, contenuto delle cartelle, schede e note.
- **Profili** come "Lavoro" e "Gaming": si cambiano dalla barra, i recinti si assegnano con clic destro → Mostra nel profilo.
- **Automazione**: cambio di profilo mentre è in esecuzione un programma o a orari fissi; recinti nascosti mentre
  qualcosa è a schermo intero; stile predefinito chiaro/scuro secondo Windows o l'ora.
- **Più PC**: i recinti in una cartella condivisa come OneDrive.
- Stile **Colore d'accento di Windows** – 25 stili in totale.
- **Francese e spagnolo**; menu **Lingua** con bandiere nella barra e in ogni menu dei recinti.
- Pulsante **Dona** (Informazioni e Impostazioni → Aggiornamenti).
- **Misurazione FPS** (facoltativa, disattivata di default): un piccolo supporto con diritti di amministratore conta i
  fotogrammi del programma in primo piano; Windows lo chiede una volta, le impostazioni spiegano perché.
- Recinto **"File recenti"** e **barra di avvio rapido** (solo icone, nomi come suggerimento).
- **Schede** nei recinti collegamenti.
- **Solo su questo desktop virtuale** per singolo recinto.
- **Esporta/importa** recinti e stili personali, ad es. per un altro PC.
- **Finestra delle impostazioni** (barra → Impostazioni) con tutte le opzioni generali; il menu della barra è molto più corto.
- **Impostazioni del recinto** ridisegnate, con sezioni e anteprima dal vivo.
- **Lingue**: inglese, tedesco, **italiano**, **francese** e **spagnolo**; automatica (lingua di Windows, altrimenti
  inglese) o scelta nelle impostazioni.
- **Finestra Informazioni** con versione, riconoscimenti e link.
- **8 nuovi stili**: Documenti, Multimedia, Musica, Sport, Foto, Viaggi, Cucina, Natura; ogni stile ha un colore
  d'accento per i widget.

### Correzioni
- OK nelle impostazioni di un recinto collegamenti rimuoveva tutti i collegamenti.
- Aprire le impostazioni di un widget causava un arresto anomalo.
- Nel post-it gli elementi sporgevano in basso oltre l'ombra del foglio.
- La scorciatoia "Porta i recinti in primo piano" era attaccata al testo del menu senza spazio.
- La voce selezionata nella barra laterale delle impostazioni diventava illeggibile.

## [2.3.0] - 2026-10-02

Selezione multipla, tastiera e ricerca; aggancio ai bordi; posizioni per configurazione dei monitor; backup della
configurazione; promemoria e link nelle note; post-it in cinque colori; animazioni; stili personali in JSON;
rinomina con doppio clic sul titolo; test automatici.

## [2.2.0] - 2026-10-01

Note (post-it) con caselle di controllo, stile Post-it attaccato al desktop, "Sempre in primo piano", nuovi recinti
vicino al mouse.

## [2.1.0] - 2026-10-01

Aggiornamenti automatici con un clic, scorciatoia per portare i recinti in primo piano, ordinamento per recinto.

## [2.0.0] - 2026-10-01

Prima versione di questo fork: riscritto su .NET 10, recinti cartella, ordinamento automatico dal desktop, stili,
icona nella barra, guida e registro delle modifiche nell'app. Basato su
[Twometer/NoFences](https://github.com/Twometer/NoFences) di Twometer e collaboratori.

[2.4.1]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.1
[2.4.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.0
[2.3.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.3.0
[2.2.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.2.0
[2.1.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.1.0
[2.0.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.0.0
