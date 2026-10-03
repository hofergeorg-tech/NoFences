# Guida di NoFences

NoFences mette sul desktop dei riquadri ("recinti") che tengono in ordine le icone, più note e widget.
English: [HELP.md](HELP.md) · Deutsch: [HILFE.md](HILFE.md) · Français : [AIDE.md](AIDE.md) · Español: [AYUDA.md](AYUDA.md)

## Primi passi

- Dopo il primo avvio c'è un recinto vuoto. Trascinaci sopra file o cartelle.
- **Clic destro su un recinto** (titolo o spazio vuoto) per il suo menu: impostazioni, stile, rinomina, nuovo recinto o
  widget, elimina.
- **Clic destro su un elemento** per il normale menu di Esplora file. Maiusc + clic destro mostra invece il menu del recinto.
- L'**icona nella barra** (in basso a destra, forse dietro la freccia ^) crea recinti, li mostra/nasconde, cambia profilo
  e apre le **Impostazioni** generali. La **lingua** si trova nella barra e in ogni menu dei recinti.

## Tipi di recinto

- **Recinto collegamenti** (predefinito): collegamenti a file e cartelle. I file restano dove sono, quindi anche sul
  desktop. Eliminando l'originale sparisce anche dal recinto.
- **Recinto cartella**: mostra il contenuto di una cartella. I file trascinati vengono **spostati** in quella cartella
  (con Ctrl copiati), quindi lasciano davvero il desktop.
- **Nota**: un post-it con testo, vedi sotto.
- **Widget**: contenuto dal vivo – orologio, meteo, giochi, appuntamenti e altro, vedi sotto.
- **File recenti**: gli ultimi 20 file aperti (sola lettura).
- **Barra di avvio rapido**: un recinto collegamenti sottile con sole icone; i nomi appaiono come suggerimento.

Suggerimento: per un desktop ordinato, crea una cartella come `Documenti\Recinti\Lavoro` e usala come recinto cartella.

## Lavorare con gli elementi

- Doppio clic apre un elemento. Trascina gli elementi per riordinarli, su un altro recinto per spostarli o in Esplora file.
- **Ctrl+clic** seleziona più elementi, **Maiusc+clic** un intervallo; trascinando nello spazio vuoto si disegna un rettangolo.
- Un recinto cliccato ascolta la tastiera: **Invio** apre, **F2** rinomina, **Canc** rimuove (recinto collegamenti: solo
  il collegamento; recinto cartella: cestino), **Ctrl+A**, **Ctrl+C**, frecce, **Esc**.
- **Basta digitare** per cercare in quel recinto; il testo appare in alto a destra, Esc termina.
- Menu del recinto → **Ordina per**: manuale, nome, tipo, data di modifica o dimensione.
- **Schede** (recinti collegamenti): menu → Aggiungi scheda. Clic per cambiare, doppio clic per rinominare, trascina gli
  elementi su una scheda per spostarli lì.

## Cercare in tutti i recinti

**Ctrl+Alt+F** (o clic destro sull'icona nella barra o su un recinto → Strumenti ▸ Cerca nei recinti…) apre una casella di ricerca. Trova tutto nei tuoi recinti – collegamenti,
contenuto delle cartelle, schede e testi delle note – anche lettere in ordine ("ffx" trova Firefox) – e anche le app
del menu Start e le pagine delle impostazioni di Windows ("bluetooth", "audio"). Scrivi un calcolo come `12*7` o
`200*15%` e Invio copia il risultato. **Invio** apre il risultato, **↑↓** scegli, **Esc** chiude. La scorciatoia si
cambia in Impostazioni → Desktop.

## Spostare, ridimensionare, rinominare

- Trascina la barra del titolo per spostare, i bordi per ridimensionare. **Doppio clic sul titolo** per rinominare.
- I recinti si **agganciano** ai bordi dello schermo e agli altri recinti; tieni premuto **Alt** per posizionarli liberamente.
- Le posizioni vengono ricordate **per configurazione dei monitor**: scollega e ricollega un monitor e i recinti tornano.
- I recinti **bloccati** non si possono spostare né modificare. **Comprimi quando il mouse è fuori** li riduce alla barra del titolo.
- **Sempre in primo piano** tiene un recinto sopra tutte le finestre (sui giochi solo in modalità "finestra senza bordi").
- **Solo su questo desktop virtuale** mostra un recinto solo sul desktop virtuale attuale (Win+Ctrl+frecce).
- **Ctrl+Alt+D** porta tutti i recinti davanti alle finestre aperte; Esc o un clic altrove li rimanda indietro.

## Note

- **Doppio clic** per scrivere; **Esc** o un clic fuori salva.
- Le righe che iniziano con `[ ]` diventano caselle; un clic le spunta e barra la riga.
- Indirizzi web e percorsi sono sottolineati e si aprono con un clic. Il testo trascinato su una nota viene aggiunto.
- Menu del recinto → **Promemoria…**: a quell'ora NoFences emette un suono e mostra una notifica.
- Stile post-it in giallo, rosa, verde, blu e arancione.

## Widget

Menu della barra o del recinto → **Nuovo widget**. I widget con un elenco scorrono con la rotellina del mouse.

- **Orologio e calendario**.
- **Monitor di sistema**: CPU, RAM, carico e temperatura della GPU (NVIDIA) e **FPS**, se attivati (vedi sotto).
- **Unità**: livello di riempimento e spazio libero; un clic apre l'unità.
- **Cestino**: trascinaci i file per eliminarli, doppio clic lo apre, dal menu lo svuoti.
- **Tempo di gioco**: oggi / questa settimana / questo mese / totale per qualsiasi gioco. Doppio clic e scegli
  l'eseguibile del gioco; NoFences registra per quanto tempo è in esecuzione.
- **Conto alla rovescia**: giorni e ore fino a una data; doppio clic per impostarla.
- **Meteo**: tempo attuale e previsioni per tre giorni per una località cercata (dati: Open-Meteo, senza account).
- **In riproduzione**: titolo, artista e copertina di ciò che Spotify, un browser o un lettore multimediale sta
  riproducendo, con precedente / play-pausa / successivo.
- **Rete**: velocità di download e upload con il grafico dell'ultimo minuto e il ping.
- **Cronologia appunti**: gli ultimi 15 testi copiati; un clic li copia di nuovo. Conservati solo finché NoFences è
  aperto; le password dei gestori di password vengono ignorate.
- **Batteria**: carica, se è in carica, tempo rimanente (portatili).
- **Giochi**: i giochi installati da Steam (con copertina), Epic, GOG e dall'app Xbox; prima quelli giocati di recente.
  Un clic avvia il gioco. Dal menu puoi nascondere giochi, ordinarli per nome o cercare di nuovo.
- **Appuntamenti**: le prossime due settimane dai link dei calendari (.ics). Google: impostazioni del calendario →
  "Indirizzo segreto in formato iCal"; Outlook: Impostazioni → Calendario → Calendari condivisi → Pubblica → ICS; iCloud:
  condividi il calendario pubblicamente. Più calendari: un link per riga. Gli eventi ricorrenti sono supportati.
- **Cornice foto**: presentazione di una cartella di immagini (anche sottocartelle), ogni 10 s fino a 15 min. Un clic
  mostra l'immagine successiva, doppio clic la apre.
- **Timer di concentrazione (Pomodoro)**: 25 minuti di concentrazione, 5 di pausa, una pausa lunga dopo quattro giri
  (oppure 50/10, 15/3). Un suono e una notifica segnalano ogni cambio.
- **Notizie**: titoli da feed RSS o Atom (pulsanti pronti per ANSA, Tagesschau, BBC e altri); un clic apre l'articolo.
- **Quotazioni**: azioni, indici e cripto con la variazione da ieri e il grafico della giornata, con i simboli di Yahoo
  Finance come `AAPL`, `FTSEMIB.MI`, `^GDAXI`, `BTC-EUR`. Aggiornate ogni cinque minuti; solo a scopo informativo.
- **Tempo di utilizzo**: quali programmi hai usato e per quanto oggi o negli ultimi 7 giorni (clic su "oggi ⇄" per
  cambiare). Registrato solo finché il widget esiste e sei al PC; resta su questo PC.
- **Audio**: volume del dispositivo di riproduzione attuale (clic sulla barra o rotellina), muto per altoparlanti e
  microfono, e con un clic passi a un altro dispositivo (cuffie ↔ altoparlanti).
- **Stato dei servizi**: se RSI, Discord, Epic Games, GitHub e altri hanno problemi in questo momento, dalle loro pagine
  di stato pubbliche; un clic su una riga apre la pagina.

Ogni widget ha le sue impostazioni nel menu. Nel menu del timer di concentrazione c'è anche la **modalità
concentrazione**: durante un giro passa a un profilo a tua scelta (ad es. "Concentrazione" con soli recinti di lavoro)
e torna indietro nelle pause.

## Assistente desktop

Menu della barra o del recinto → Strumenti ▸ **Assistente desktop…** (proposto anche al primo avvio) ordina ciò che c'è sul desktop in nuovi recinti –
giochi, programmi, documenti, immagini, musica e video, archivi, cartelle – ognuno con uno stile adatto. Non viene
spostato nulla, i recinti collegano i file. Per nascondere gli originali: clic destro sul desktop → Visualizza →
Mostra icone del desktop.

## Righello sullo schermo

Menu della barra o del recinto → Strumenti ▸ **Righello sullo schermo** mette un righello sopra tutto: trascina per spostarlo, trascina l'estremità per
allungarlo, doppio clic o spazio lo ruota, le frecce lo spostano al pixel (Maiusc: 10 px), U o il menu cambia tra
pixel, centimetri e pollici (dimensione reale, da quella comunicata dal monitor). Una linea rossa segue il mouse e
mostra la distanza. Esc lo chiude.

## Profili

Raggruppa i recinti in profili come "Lavoro" e "Gaming" e passa dall'uno all'altro nella barra (**Profilo ▸**) o in
**Impostazioni → Desktop**. Clic destro su un recinto → **Mostra nel profilo** per assegnarlo; un recinto senza profilo
appare in tutti i profili. I nuovi recinti appartengono al profilo attivo. **Ctrl+Alt+F1…F9** passano al profilo 1…9,
**Ctrl+Alt+F10** mostra tutti i recinti. Con un profilo attivo, barra → Profilo ▸ **Sfondo per «…»** gli dà uno
sfondo proprio; nei profili senza torna quello abituale.

## Automazione (Impostazioni → Automazione)

- **Cambia profilo automaticamente**: "Gaming" mentre è in esecuzione un certo programma, "Lavoro" nei giorni feriali
  dalle 8 alle 17 e così via. Un programma in esecuzione ha la precedenza su una regola oraria; quando nessuna regola
  vale più, torna il profilo precedente. Se cambi a mano, termina ciò che la regola aveva avviato.
- **Schermo intero**: mentre un gioco, un video o una presentazione riempie un monitor, i recinti su quel monitor
  vengono nascosti.
- **Stile chiaro e scuro**: lo stile predefinito cambia con la modalità chiara/scura di Windows o a orari fissi – ad es.
  post-it di giorno e vetro la sera. I recinti con uno stile proprio lo mantengono.
- Lo stile **Colore d'accento di Windows** prende il colore da Impostazioni → Personalizzazione → Colori.

## Più PC

Impostazioni → Dati e stili → **Scegli cartella condivisa…**, ad es. in OneDrive. Recinti, note, tempo di gioco e stili
personali si trovano poi lì, e ogni PC che usa la stessa cartella mostra gli stessi recinti. Le posizioni valgono per
ogni disposizione dei monitor, quindi portatile e PC fisso possono disporli in modo diverso. Quando un altro PC salva,
NoFences ricarica dopo pochi secondi. "Smetti di condividere" copia tutto di nuovo su questo PC.

## Misurazione FPS (facoltativa)

Windows fornisce gli eventi della frequenza dei fotogrammi solo ai programmi con diritti di amministratore. NoFences usa
quindi un piccolo processo di supporto che gira come amministratore – NoFences stesso no. Conta solo i fotogrammi, nessun
contenuto e nessun input. Attivala in **Impostazioni → Misurazione FPS**; Windows lo chiede una volta, poi un'attività
dell'Utilità di pianificazione avvia il supporto senza chiedere. Disattivandola, l'attività viene rimossa.

## Ordina dal desktop

Nelle impostazioni di un recinto, sotto "Ordina dal desktop", inserisci degli schemi, ad es. `*.pdf; *.docx`, o aggiungi
un modello. I nuovi file del desktop che corrispondono finiscono in quel recinto (anche i download completati).
**Riordina il desktop ora** (barra o impostazioni) ordina ciò che c'è già.

## Stili

Lo stile predefinito si sceglie in **Impostazioni → Generale**, per singolo recinto dal menu → Stile o nelle impostazioni
del recinto con anteprima dal vivo. Ci sono 25 stili – vetro, colore d'accento di Windows, HUD Star Citizen, Retro-Arcade,
Hardware, Nerd, Hobby, Lavoro, Famiglia, Gaming, Finanza, Social, Documenti, Multimedia, Musica, Sport, Foto, Viaggi,
Cucina, Natura e post-it in cinque colori.

**Stili personali**: Impostazioni → Dati e stili → Apri cartella degli stili. Copia `beispiel-mocha.json`, cambia i colori
(`#RRGGBB` o `#RRGGBBAA`) e ricarica. Gli stili personali hanno una ★.

## Impostazioni (barra → Impostazioni)

- **Generale**: lingua (automatica, English, Deutsch, Italiano, Français, Español), avvio con Windows, estensioni, stile
  predefinito, animazioni.
- **Desktop**: doppio clic sul desktop nasconde/mostra i recinti; scorciatoia per portarli in primo piano (Ctrl+Alt+D);
  profili; scorciatoia per la ricerca (Ctrl+Alt+F); ordinamento.
- **Automazione**: regole dei profili, schermo intero, stile chiaro e scuro (vedi sopra).
- **Aggiornamenti**: NoFences controlla GitHub e installa le nuove versioni con un clic; donazioni.
- **Misurazione FPS**: vedi sopra.
- **Dati e stili**: esporta/importa recinti, ripristina un backup (ogni 12 ore), cartella condivisa, cartelle.

## Domande frequenti

**Posso eliminare l'originale dopo aver trascinato un'icona in un recinto?**
In un recinto collegamenti no, il recinto si limita a collegarlo. In un recinto cartella il file è stato spostato, quindi
non resta nulla da eliminare.

**Windows mostra un avviso di SmartScreen all'avvio.**
L'eseguibile non è ancora firmato. Fai clic su "Ulteriori informazioni" → "Esegui comunque".

**Cosa va su Internet?**
Solo ciò che imposti tu: il controllo degli aggiornamenti (GitHub), il meteo (Open-Meteo), i link dei tuoi calendari, i
feed di notizie e le quotazioni (Yahoo Finance). Nient'altro viene inviato.

**Dove sono le mie impostazioni?**
In `%LocalAppData%\NoFences\fences.json` (backup accanto) o nella cartella condivisa, se ne hai scelta una. Con un file
vuoto `portable.txt` accanto a `NoFences.exe` vengono invece salvate accanto all'eseguibile.

**Come si disinstalla?**
Impostazioni → Generale: togli "Avvia con Windows"; disattiva la misurazione FPS se usata; barra → Esci; elimina
`NoFences.exe` e la cartella `%LocalAppData%\NoFences`. I file nei recinti cartella restano nelle loro cartelle.
