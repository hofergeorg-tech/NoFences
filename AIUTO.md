# Guida di NoFences

NoFences mette sul desktop dei riquadri ("recinti") che tengono in ordine le icone, oltre a note e widget.
English: [HELP.md](HELP.md) · Deutsch: [HILFE.md](HILFE.md)

## Primi passi

- Al primo avvio c'è un recinto vuoto. Trascinaci sopra file o cartelle.
- **Clic destro su un recinto** (titolo o spazio vuoto) apre il suo menu: impostazioni, stile, rinomina, nuovo recinto
  o widget, elimina.
- **Clic destro su un elemento** mostra il normale menu di Esplora file. Maiusc + clic destro mostra invece il menu del recinto.
- L'**icona nella barra delle applicazioni** (in basso a destra, forse dietro la freccia ^) crea recinti, li mostra o
  nasconde e apre le **Impostazioni** generali.

## Tipi di recinto

- **Recinto collegamenti** (predefinito): collegamenti a file e cartelle. I file restano dove sono, quindi anche sul
  desktop. Se elimini l'originale, sparisce anche dal recinto.
- **Recinto cartella**: mostra il contenuto di una cartella. I file trascinati vengono **spostati** lì (tieni premuto
  Ctrl per copiarli), quindi lasciano davvero il desktop.
- **Nota**: un post-it con testo, vedi sotto.
- **Widget**: contenuto dal vivo – orologio, monitor di sistema, unità, cestino, tempo di gioco, conto alla rovescia.
- **File recenti**: gli ultimi 20 file aperti (sola lettura).
- **Barra di avvio rapido**: un recinto collegamenti sottile con sole icone; i nomi appaiono come suggerimento.

Consiglio: per un desktop in ordine, crea una cartella come `Documenti\Recinti\Lavoro` e usala come recinto cartella.

## Lavorare con gli elementi

- Doppio clic apre un elemento. Trascina gli elementi per riordinarli, su un altro recinto per spostarli o in Esplora file.
- **Ctrl+clic** seleziona più elementi, **Maiusc+clic** un intervallo; trascinando sullo spazio vuoto disegni un rettangolo di selezione.
- Un recinto cliccato risponde alla tastiera: **Invio** apre, **F2** rinomina, **Canc** rimuove (recinto collegamenti:
  solo il collegamento; recinto cartella: cestino), **Ctrl+A**, **Ctrl+C**, frecce, **Esc**.
- **Digita** per cercare; il testo appare in alto a destra, Esc termina la ricerca.
- Menu del recinto → **Ordina per**: manuale, nome, tipo, data di modifica o dimensione.
- **Schede** (recinti collegamenti): menu del recinto → Aggiungi scheda. Clic per cambiare, doppio clic per rinominare,
  trascina gli elementi su una scheda per spostarli lì.

## Spostare, ridimensionare, rinominare

- Trascina la barra del titolo per spostare, i bordi per ridimensionare. **Doppio clic sul titolo** per rinominare.
- I recinti si **agganciano** ai bordi dello schermo e agli altri recinti; tieni premuto **Alt** per posizionarli liberamente.
- Le posizioni vengono ricordate **per configurazione dei monitor**: scollega e ricollega un monitor e i recinti tornano al loro posto.
- I recinti **bloccati** non si possono spostare né modificare. **Comprimi quando il mouse è fuori** riduce il recinto alla barra del titolo.
- **Sempre in primo piano** tiene un recinto sopra tutte le finestre (sopra i giochi solo in modalità "finestra senza bordi").
- **Solo su questo desktop virtuale** mostra un recinto solo sul desktop virtuale attuale (Win+Ctrl+frecce).

## Note

- **Doppio clic** per scrivere; **Esc** o un clic fuori salva.
- Le righe che iniziano con `[ ]` diventano caselle; un clic le spunta e barra la riga.
- Indirizzi web e percorsi sono sottolineati e si aprono con un clic. Il testo trascinato su una nota viene aggiunto in fondo.
- Menu del recinto → **Promemoria…**: all'ora scelta NoFences emette un suono e mostra una notifica.
- Stile post-it in giallo, rosa, verde, blu e arancione.

## Widget

Menu della barra o del recinto → **Nuovo widget**:

- **Orologio e calendario**.
- **Monitor di sistema**: CPU, RAM, carico e temperatura della GPU (NVIDIA) e **FPS**, se attivati (vedi sotto).
- **Unità**: livello di riempimento e spazio libero; un clic apre l'unità.
- **Cestino**: trascinaci i file per eliminarli, doppio clic lo apre, dal menu lo svuoti.
- **Tempo di gioco**: oggi / questa settimana / questo mese / totale per un gioco, letto dallo strumento gratuito
  SC Playtime, che può registrare qualsiasi gioco. Scegli il gioco nel menu del widget.
- **Conto alla rovescia**: giorni e ore fino a una data; doppio clic per impostarla.

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
del recinto con anteprima dal vivo. Ci sono 24 stili – vetro, HUD Star Citizen, Retro-Arcade, Hardware, Nerd, Hobby,
Lavoro, Famiglia, Gaming, Finanza, Social, Documenti, Multimedia, Musica, Sport, Foto, Viaggi, Cucina, Natura e post-it
in cinque colori.

**Stili personali**: Impostazioni → Dati e stili → Apri cartella degli stili. Copia `beispiel-mocha.json`, cambia i colori
(`#RRGGBB` o `#RRGGBBAA`) e ricarica. Gli stili personali hanno una ★.

## Impostazioni (barra → Impostazioni)

- **Generale**: lingua (automatica, English, Deutsch, Italiano), avvio con Windows, estensioni, stile predefinito, animazioni.
- **Desktop**: doppio clic sul desktop nasconde/mostra i recinti; scorciatoia per portarli in primo piano (Ctrl+Alt+D); ordinamento.
- **Aggiornamenti**: NoFences controlla GitHub e installa le nuove versioni con un clic.
- **Misurazione FPS**: vedi sopra.
- **Dati e stili**: esporta/importa recinti (ad es. per un altro PC), ripristina un backup (ogni 12 ore), cartelle.

## Domande frequenti

**Posso eliminare l'originale dopo averlo trascinato in un recinto?**
In un recinto collegamenti no, il recinto vi rimanda soltanto. In un recinto cartella il file è stato spostato, quindi non resta nulla da eliminare.

**Windows mostra un avviso di SmartScreen all'avvio.**
L'eseguibile non è ancora firmato. Clicca su "Ulteriori informazioni" → "Esegui comunque".

**Dove sono le mie impostazioni?**
In `%LocalAppData%\NoFences\fences.json` (i backup accanto). Con un file vuoto `portable.txt` accanto a `NoFences.exe`
vengono invece salvate accanto all'eseguibile.

**Come si disinstalla?**
Impostazioni → Generale: togli "Avvia con Windows"; disattiva la misurazione FPS se usata; barra → Esci; elimina
`NoFences.exe` e la cartella `%LocalAppData%\NoFences`. I file nei recinti cartella restano nelle loro cartelle.
