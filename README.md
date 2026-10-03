# Olomove

## Přihlášení

API používá ASP.NET Core Identity a ukládá účty do PostgreSQL. Nuxt UI formuláře jsou na `/login` a `/signup`.

| Akce | Endpoint |
| --- | --- |
| Registrace e-mailem a heslem | `POST /api/auth/register` |
| Přihlášení e-mailem a heslem | `POST /api/auth/login` |
| Odhlášení | `POST /api/auth/logout` |
| Aktuální uživatel | `GET /api/auth/me` |
Pro nasazení upravte `Cors:AllowedOrigins` a `NUXT_PUBLIC_API_BASE` na produkční adresy.
