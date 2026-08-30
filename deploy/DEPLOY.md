# Deploy TaskManager to a VPS

Stack: **PostgreSQL (pgvector)** + **.NET API** + **Python (RAG)** + **React (nginx)** via Docker Compose.

## VPS requirements

- Ubuntu 22.04+ (or similar Linux)
- Docker Engine + Docker Compose plugin
- Ports **80** (and **443** later if you add HTTPS) open in firewall
- At least **1 GB RAM**, **10 GB disk**
- Infra **PostgreSQL must have pgvector** installed (API migration runs `CREATE EXTENSION vector`)

Install Docker (Ubuntu):

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# log out and back in
```

## Folder layout on the server

Clone repos as **siblings**:

```text
/home/you/
  TaskManager/        # API + deploy/
  TaskManagerReact/   # frontend (local compose only)
  HardliqAi/          # Python RAG service
```

The compose file expects this layout.

## First deploy

1. **Copy env file**

```bash
cd ~/TaskManager/deploy
cp .env.example .env
nano .env
```

Set:

| Variable | Example |
|----------|---------|
| `POSTGRES_PASSWORD` | strong random password |
| `JWT_KEY` | `openssl rand -base64 48` |
| `RAG_INTERNAL_KEY` | shared secret for .NET → Python (`/internal/*`) |
| `PUBLIC_ORIGIN` | `http://YOUR_VPS_IP` or `https://tasks.yourdomain.com` |
| `HTTP_PORT` | `80` (local compose only; on **chat-prod**, use e.g. `8080`) |

2. **Build and start**

```bash
chmod +x deploy.sh
./deploy.sh
```

Or manually:

```bash
docker compose --env-file .env up -d --build
```

3. **Open the app**

Visit `PUBLIC_ORIGIN` in your browser and register a user.

Migrations run automatically on API startup (`Database__AutoMigrate=true` in compose).

## Useful commands

```bash
cd ~/TaskManager/deploy

# Status
docker compose --env-file .env ps

# Logs
docker compose --env-file .env logs -f api
docker compose --env-file .env logs -f ai
docker compose --env-file .env logs -f web

# Restart after code update
docker compose --env-file .env up -d --build

# Stop
docker compose --env-file .env down

# Stop and delete database volume (destructive)
docker compose --env-file .env down -v
```

## Update after code changes

On your PC, push to git. On the VPS:

```bash
cd ~/TaskManager && git pull
cd ~/TaskManagerReact && git pull
cd ~/TaskManager/deploy && ./deploy.sh
```

## HTTPS (optional, recommended)

After DNS points to your VPS:

1. Install certbot on the host
2. Put certificates in nginx or use a reverse proxy like Caddy/Traefik
3. Set `PUBLIC_ORIGIN=https://yourdomain.com` in `.env` and rebuild web

Same-origin `/api` proxy means you do not need to change frontend API URLs when adding HTTPS — update `PUBLIC_ORIGIN` and terminate TLS at nginx.

## Local production test

From `TaskManager/deploy` with Docker running:

```bash
cp .env.example .env
# set PUBLIC_ORIGIN=http://localhost, POSTGRES_PASSWORD, JWT_KEY, RAG_INTERNAL_KEY
docker compose --env-file .env up --build
```

Services: **db** (pgvector), **ai** (Python :8000), **api**, **web** (nginx :80).

Open http://localhost

## VPS deploy (infra nginx + postgres)

Use `compose.vps.yaml` when **nginx** and **postgres** already run on external Docker networks (`edge`, `database`):

```bash
cd ~/TaskManager/deploy
cp .env.example .env
# POSTGRES_* = infra postgres admin; PUBLIC_ORIGIN = your public URL
docker compose -f compose.vps.yaml --env-file .env up -d --build
```

- **api** on `edge` + `database` (nginx proxies to it)
- **ai** on `database` only (internal; API calls `http://ai:8000`)
- **no web/db** in this compose — infra serves React `dist/` and Postgres
- Infra Postgres must support **pgvector**

## Troubleshooting

| Problem | Check |
|---------|--------|
| Blank page | `docker compose logs web` |
| API errors | `docker compose logs api` |
| RAG / ask errors | `docker compose logs ai` |
| DB connection failed | `docker compose logs db`, verify `.env` passwords |
| pgvector migration failed | DB image must support pgvector (`pgvector/pgvector:pg16` locally) |
| CORS errors | `PUBLIC_ORIGIN` must match the URL in the browser exactly |
| Build fails on web path | Ensure `TaskManagerReact` is sibling of `TaskManager` |
| Build fails on ai path | Ensure `HardliqAi` is sibling of `TaskManager` |

## Security notes

- Never commit `.env`
- Use a long random `JWT_KEY` (32+ chars)
- Change default Postgres password
- Prefer HTTPS on a real domain before sharing the app
