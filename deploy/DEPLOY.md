# Deploy TaskManager to a VPS

Stack: **PostgreSQL** + **.NET API** + **React (nginx)** via Docker Compose.

## VPS requirements

- Ubuntu 22.04+ (or similar Linux)
- Docker Engine + Docker Compose plugin
- Ports **80** (and **443** later if you add HTTPS) open in firewall
- At least **1 GB RAM**, **10 GB disk**

Install Docker (Ubuntu):

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# log out and back in
```

## Folder layout on the server

Clone both repos as **siblings**:

```text
/home/you/
  TaskManager/        # API + deploy/
  TaskManagerReact/   # frontend
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
| `PUBLIC_ORIGIN` | `http://YOUR_VPS_IP` or `https://tasks.yourdomain.com` |
| `HTTP_PORT` | `80` (on **chat-prod**, port 80 is used by the chat app — use e.g. `8080` instead) |

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
# set PUBLIC_ORIGIN=http://localhost
docker compose --env-file .env up --build
```

Open http://localhost

## Troubleshooting

| Problem | Check |
|---------|--------|
| Blank page | `docker compose logs web` |
| API errors | `docker compose logs api` |
| DB connection failed | `docker compose logs db`, verify `.env` passwords |
| CORS errors | `PUBLIC_ORIGIN` must match the URL in the browser exactly |
| Build fails on web path | Ensure `TaskManagerReact` is sibling of `TaskManager` |

## Security notes

- Never commit `.env`
- Use a long random `JWT_KEY` (32+ chars)
- Change default Postgres password
- Prefer HTTPS on a real domain before sharing the app
