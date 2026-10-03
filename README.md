# Olomove

## Spuštění vývojového prostředí v Dockeru

Je potřeba mít spuštěný Docker Desktop. Z kořenové složky projektu spusťte:

```powershell
docker compose up
```

Docker spustí tři kontejnery:

| Služba | Účel |
| --- | --- |
| `postgres` | PostgreSQL databáze |
| `aspnetapi` | ASP.NET Core API s automatickým restartem při změně C# souborů |
| `nuxt` | Nuxt UI s automatickým obnovením při změně frontendových souborů |

Aplikace je potom dostupná na [http://localhost:3000](http://localhost:3000).

Při prvním startu se automaticky aplikují databázové migrace, vytvoří role a výchozí vývojový administrátor:

```text
E-mail: admin@olomove.local
Heslo: Admin123!
```

Volitelně lze vytvořit vlastní `.env` podle `.env.example` a změnit heslo k databázi nebo údaje výchozího administrátora. Soubor `.env` se neukládá do Gitu.

Pro spuštění na pozadí použijte:

```powershell
docker compose up -d
```

Pro úplné smazání lokální databáze a nové vytvoření dat použijte:

```powershell
docker compose down -v
docker compose up
```

Příkaz `down -v` smaže všechna lokální data v databázi včetně vytvořených uživatelských účtů.

## Přihlášení

API používá ASP.NET Core Identity a ukládá účty do PostgreSQL. Nuxt UI formuláře jsou na `/login` a `/signup`.

| Akce | Endpoint |
| --- | --- |
| Registrace e-mailem a heslem | `POST /api/auth/register` |
| Přihlášení e-mailem a heslem | `POST /api/auth/login` |
| Odhlášení | `POST /api/auth/logout` |
| Aktuální uživatel | `GET /api/auth/me` |
Pro nasazení upravte `Cors:AllowedOrigins` a `NUXT_PUBLIC_API_BASE` na produkční adresy.
