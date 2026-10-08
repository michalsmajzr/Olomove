# Olomove

## Spuštění vývojového prostředí v Dockeru

Je potřeba mít spuštěný Docker Desktop nebo termínál a z kořenové složky projektu spustit:

```powershell
docker compose up -d
```

Docker spustí container s PostgreSQL databází.

| Služba | Účel |
| --- | --- |
| `postgres` | PostgreSQL databáze |

Pokud používáte linux můžete odkomentovat zbytek kódu v docker compose a spustit vše v containerech, ale pro vývoj na Windows doporučuji spustit .NET a NuxtUI zvlášť v terminálech.

Dotnet pro vývoj spusťte v termínálu ve složce Olomove:
```powershell
dotnet watch run
```

Při prvním spuštění:
```powershell
dotnet clean
dotnet build
```

Migrace pro EF:
```powershell
dotnet tool install --global dotnet-ef    # jen první spuštění, instalace
dotnet ef migrations add nazev-migrace    # migrace
dotnet ef database update    # aktualizace
```

NuxtUI pro vývoj spusťte v termínálu ve složce NuxtUI (nejdříve je nutné mít nainstalovaný Node.js):
```powershell
npm install -g pnpm    # jen první spuštění
pnpm dev
```

Aplikace je potom dostupná na [http://localhost:3000](http://localhost:3000).

Při prvním startu se automaticky aplikují databázové migrace, vytvoří role a výchozí vývojový administrátor:

```text
E-mail: admin@olomove.local
Heslo: Admin123!
```

Heslo k databázi pro lokální vývoj je nastavené na: `admin`

Volitelně lze upravit `.env` podle `.env.example` a změnit heslo k databázi nebo údaje výchozího administrátora.

Pro úplné smazání lokální databáze a nové vytvoření dat použijte:

```powershell
docker compose down -v
docker compose up
```
