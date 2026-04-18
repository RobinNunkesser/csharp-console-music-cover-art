# csharp-console-music-cover-art

Minimaler CLI-Consumer fuer die iTunes Search API mit Bezug auf die veroeffentlichten
Pakete aus `nuget-music` und `nuget-foundation`.

NuGet-Bezug:

- `Italbytz.Music.ITunes.Client` `1.0.2`
- Restore erfolgt direkt ueber `dotnet restore` gegen NuGet.org, kein lokaler Checkout von `nuget-music` ist mehr noetig.

Aktuell implementiert:

- Suche nach Songs ueber die iTunes Search API
- Ausgabe der Treffer im Terminal
- optionaler Download des Covers des ersten Treffers
- optionaler Download eines beliebigen Treffers per Index
- iTunes-Integration ausgelagert in das NuGet-Paket `Italbytz.Music.ITunes.Client` `1.0.2`

Beispiel:

```bash
dotnet restore
dotnet run -- search "Daft Punk" --limit 5
dotnet run -- search "Daft Punk" --download-first --output ./covers --size large
dotnet run -- search "Daft Punk" --download-index 1 --output ./covers --size medium
```