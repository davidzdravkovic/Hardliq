# Hardliq

A task is a concrete unit you can execute. It exists to serve a goal — and goals shift. They roll up into larger aims that change more slowly. Hardliq keeps the big picture steady while you break it into smaller pieces you can change, finish, and see progress on.

**Live app → [https://taskspace.duckdns.org](https://taskspace.duckdns.org)**

**Demo video → [YouTube](https://www.youtube.com/watch?v=WN8hsU93xkU)**

Register to use the app. AI Ask is available for premium users.

---

## Screenshot


`docs/screenshots/dashboard.png`

---

## Why this project exists

Hardliq started as a tool I needed: a flexible way to organize my work in a clear structure, with the features I cared about not another generic tasks tracking app.

---

## Features

**Accounts**
- Registration and login (JWT)

**Organize**
- Folder tree for your work
- Create, update, move, and delete items
- Status tracking: pending, completed, canceled
- Search and folder stats

**Ask**
- Natural-language questions about your work
- Answers grounded in your saved items, with linked sources
- Premium access and daily ask limits

---

## Tech stack

| Layer | Technologies |
|--------|----------------|
| Frontend | React · TypeScript · TanStack Query |
| API | .NET 8 · ASP.NET Core · EF Core · JWT |
| Assistant | Python · FastAPI · fastembed · Gemini (production) / Ollama (local) |
| Data | PostgreSQL · pgvector |
| Deploy | Docker Compose · Linux VPS · nginx |

---

## Architecture (overview)

### Deployment shape (VPS)

```text
Browser
    │
    ▼
nginx (Infra)
  ├── /        → Hardliq SPA (React)
  └── /api/    → .NET API (:8080)
                    │
                    ├── postgres (pgvector)
                    └── http://ai:8000 → assistant service (internal)
```

Local all-in-one Docker uses `deploy/docker-compose.yml` (`db`, `api`, `ai`, `web`).

**Repositories** — separate repos, not a monorepo:

| Repo | Role |
|------|------|
| [Hardliq](https://github.com/davidzdravkovic/Hardliq) | .NET API · **this repo** · `deploy/` |
| [HardliqClient](https://github.com/davidzdravkovic/HardliqClient) | React frontend |
| [HardliqAi](https://github.com/davidzdravkovic/HardliqAi) | Assistant service (`/internal/embed`, `/internal/ask`) |
| [Infra](https://github.com/davidzdravkovic/Infra) (optional) | Shared nginx + Postgres on VPS |

For Docker or full local dev, clone **HardliqClient** and **HardliqAi** next to this repo (sibling folders). Details: [deploy/DEPLOY.md](deploy/DEPLOY.md).

### How Ask works

1. When you create or update an item, the API triggers background embedding in the assistant service.
2. The service stores readable text and a vector embedding in PostgreSQL.
3. When you ask a question, it finds relevant items, builds context, and calls the LLM.
4. Hardliq returns the answer and source links to the chat UI.

Deleting an item removes its embedding automatically (database cascade).

---

## Security

- **Auth:** JWT for API routes; passwords hashed with BCrypt.
- **Internal assistant routes:** shared secret (`RAG_INTERNAL_KEY`) between API and Python service.
- **Secrets:** environment variables and `.env` — not committed to git.
- **Ask access:** premium usernames and daily limits enforced in the API.
- **Assistant errors:** service failures return HTTP 503 with a user-friendly message — not infrastructure details in the chat.

---

## Quick start (API only — this repo)

**Need:** .NET 8 SDK, PostgreSQL with pgvector, database `taskmanager`.

```bash
dotnet run
```

Set the connection string and `Rag__BaseUrl` in `appsettings.Development.json` (or user secrets). Swagger: `/swagger` in Development.

---

## Quick start (full stack, no Docker)

Three separate repos. Clone each into its own folder, then open **three terminals**:

```bash
git clone https://github.com/davidzdravkovic/Hardliq.git
git clone https://github.com/davidzdravkovic/HardliqClient.git
git clone https://github.com/davidzdravkovic/HardliqAi.git
```

**Hardliq** (this repo):

```bash
dotnet run
```

**[HardliqAi](https://github.com/davidzdravkovic/HardliqAi)** — set `DB_*` env vars or `Configuration/database.json`:

```bash
uvicorn main:app --reload --port 8000
```

**[HardliqClient](https://github.com/davidzdravkovic/HardliqClient)**:

```bash
npm install
npm run dev
```

Configure CORS and `Rag__BaseUrl` in the API to point at the assistant service.

---

## Quick start (Docker — all services)

**Need:** Docker Engine + Compose, plus sibling clones of **HardliqClient** and **HardliqAi** next to this repo. For compose, clone the frontend as `TaskManagerReact` (same repo as **HardliqClient**):

```bash
git clone https://github.com/davidzdravkovic/HardliqClient.git TaskManagerReact
git clone https://github.com/davidzdravkovic/HardliqAi.git
```

From this repo:

```bash
cd deploy
cp .env.example .env
# Set POSTGRES_PASSWORD, JWT_KEY, RAG_INTERNAL_KEY, PUBLIC_ORIGIN=http://localhost:8080
docker compose --env-file .env up --build
```

Open [http://localhost:8080](http://localhost:8080).

For local LLM via Ollama, set `LLM_PROVIDER=ollama` in `.env` and run Ollama on the host. For Gemini, set `LLM_PROVIDER=gemini` and `GEMINI_API_KEY`.

Migrations run on API startup (`Database__AutoMigrate=true`).

Full VPS deploy: [deploy/DEPLOY.md](deploy/DEPLOY.md).

---

## Configuration

Copy [deploy/.env.example](deploy/.env.example) and set at minimum:

| Variable | Purpose |
|----------|---------|
| `POSTGRES_PASSWORD` | Database password |
| `JWT_KEY` | API JWT signing key |
| `RAG_INTERNAL_KEY` | Shared secret for API → assistant `/internal/*` |
| `PUBLIC_ORIGIN` | Public app URL (CORS) |
| `GEMINI_API_KEY` | LLM for production (`LLM_PROVIDER=gemini`) |
| `RAG_PREMIUM_USERNAME` | Username allowed to use Ask |

Full deploy and VPS notes: [deploy/DEPLOY.md](deploy/DEPLOY.md)

---

## API

Swagger is available in Development at `/swagger`.

Main routes:

| Route | Description |
|-------|-------------|
| `POST /api/auth/register` | Register |
| `POST /api/auth/login` | Login |
| `GET /api/topics` | Folder tree |
| `POST /api/tasks` | Create item |
| `PATCH /api/tasks/{id}` | Update item |
| `POST /api/ask` | Ask the assistant (premium) |

---

## License

Personal portfolio project. Ask before reusing commercially.
