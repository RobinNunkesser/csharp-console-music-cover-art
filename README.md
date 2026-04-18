# csharp-console-music-cover-art

Minimaler CLI-Consumer fuer die iTunes Search API mit Bezug auf die lokale Paketgrenze
zwischen `nuget-music` und `nuget-foundation`.

Aktuell implementiert:

- Suche nach Songs ueber die iTunes Search API
- Ausgabe der Treffer im Terminal
- optionaler Download des Covers des ersten Treffers
- optionaler Download eines beliebigen Treffers per Index
- iTunes-Integration ausgelagert in das lokale Paket `Italbytz.Music.ITunes.Client` aus `nuget-music`

Beispiel:

```bash
dotnet run -- search "Daft Punk" --limit 5
dotnet run -- search "Daft Punk" --download-first --output ./covers --size large
dotnet run -- search "Daft Punk" --download-index 1 --output ./covers --size medium
```