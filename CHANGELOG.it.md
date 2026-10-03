# Registro delle modifiche

Tutte le modifiche importanti a questo fork. English: [CHANGELOG.md](CHANGELOG.md) · Deutsch: [CHANGELOG.de.md](CHANGELOG.de.md)

## [2.4.0] - 2026-10-03

### Novità
- **Widget**: orologio e calendario, monitor di sistema (CPU, RAM, carico e temperatura GPU, FPS), unità, cestino,
  tempo di gioco e conto alla rovescia. Menu della barra o del recinto → Nuovo widget.
- **Tempo di gioco** per qualsiasi gioco: scegli l'eseguibile, NoFences registra per quanto tempo è in esecuzione
  (oggi, settimana, mese, totale, in corso).
- **Conto alla rovescia** fino a una data, con titolo.
- **Misurazione FPS** (facoltativa, disattivata di default): un piccolo supporto con diritti di amministratore conta i
  fotogrammi del programma in primo piano; Windows lo chiede una volta, le impostazioni spiegano perché.
- Recinto **"File recenti"** e **barra di avvio rapido** (solo icone, nomi come suggerimento).
- **Schede** nei recinti collegamenti.
- **Solo su questo desktop virtuale** per singolo recinto.
- **Esporta/importa** recinti e stili personali, ad es. per un altro PC.
- **Finestra delle impostazioni** (barra → Impostazioni) con tutte le opzioni generali; il menu della barra è molto più corto.
- **Impostazioni del recinto** ridisegnate, con sezioni e anteprima dal vivo.
- **Lingue**: inglese, tedesco e **italiano**; automatica (lingua di Windows, altrimenti inglese) o scelta nelle impostazioni.
- **Finestra Informazioni** con versione, riconoscimenti e link.
- **8 nuovi stili**: Documenti, Multimedia, Musica, Sport, Foto, Viaggi, Cucina, Natura – 24 stili in totale; ogni stile
  ha un colore d'accento per i widget.

### Correzioni
- OK nelle impostazioni di un recinto collegamenti rimuoveva tutti i collegamenti.
- Aprire le impostazioni di un widget causava un arresto anomalo.
- Nel post-it gli elementi sporgevano in basso oltre l'ombra del foglio.
- La scorciatoia "Porta i recinti in primo piano" era attaccata al testo del menu senza spazio.

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

[2.4.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.0
[2.3.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.3.0
[2.2.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.2.0
[2.1.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.1.0
[2.0.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.0.0
