# Docker Compose Configuration Guide

## Overview

One compose file runs every local/dev scenario. Nothing environment-specific is hard-coded in it —
the differences (LLM provider, ports, image, secrets) are **injected**:

```
docker-compose.yml          ← single service definition (local / dev / self-hosted)
docker-compose.rancher.yml  ← Rancher 1.6 server deployment only (compose v2, self-contained)
.env.example                ← template → copy to .env (git-ignored): scenario switches + secrets
env/
├── llm-ollama.env          ← LLM preset: local Ollama
├── llm-custom.env          ← LLM preset: LM Studio / OpenAI-compatible server on the LAN
├── llm-openai.env          ← LLM preset: OpenAI cloud
├── psmon.stack.env         ← ready-made scenario (copy to .env)
└── local.override.env      ← optional, git-ignored: per-variable overrides
```

Injection order for the `memorizer` service (later wins):

1. `env/llm-${LLM_PRESET}.env` — provider preset
2. `env/local.override.env` — your overrides (optional)
3. `environment:` in docker-compose.yml — common settings (DB, Neo4j, Server, OAuth, WebSearch) whose values come from `.env`

> Keep provider variables (`MEMORIZER_Embeddings__*`, `MEMORIZER_LLM__*`, `MEMORIZER_LLM-EX__*`, `MEMORIZER_MultiModal__*`) in the env files only.
> Anything set under `environment:` overrides the env files.

## Quick Start

```bash
cp .env.example .env        # default: Ollama all-in-one
docker compose up -d
```

| Task | Command |
|------|---------|
| Start (use image from registry) | `docker compose up -d` |
| Build from source and start | `docker compose up -d --build` |
| Rebuild only the app, keep DBs running | `docker compose up -d --build --no-deps memorizer` |
| Use a scenario file without copying | `docker compose --env-file env/psmon.stack.env up -d` |
| Show the resolved configuration | `docker compose config` |

## Scenarios

| Scenario | `.env` settings | Replaces old file |
|----------|----------------|-------------------|
| Ollama all-in-one (default) | `LLM_PRESET=ollama`, `COMPOSE_PROFILES=ollama` | `docker-compose.yml`, `docker-compose.local.yml` |
| Custom LLM server on the LAN | `LLM_PRESET=custom`, `COMPOSE_PROFILES=`, `LLM_A_URL` + `LLM_A_API_KEY` | `docker-compose.custom-sample.yml` |
| psmon local dev (Custom LLM A/B, 768-dim, SearXNG web search, port 5012) | `cp env/psmon.stack.env .env` | `docker-compose.local-psmon.yml` |
| OpenAI cloud | `LLM_PRESET=openai`, `COMPOSE_PROFILES=`, `OPENAI_API_KEY=sk-...` | `docker-compose.openai-sample.yml` |
| Rancher 1.6 server | `docker-compose.rancher.yml` | `docker-compose.server.yml`, `docker-compose.ollama.yml` |

### Mixing providers per feature

Presets set all four features to one provider. To mix them (e.g. Ollama for LLM but a large Custom model for LLM-EX),
create `env/local.override.env`:

```dotenv
MEMORIZER_LLM-EX__Type=Custom
MEMORIZER_LLM-EX__ApiUrl=http://192.168.0.64:1234
MEMORIZER_LLM-EX__Model=openai/gpt-oss-120b
```

## Profiles

| Profile | Services | Enable with |
|---------|----------|-------------|
| (always) | postgres, neo4j, memorizer | — |
| `ollama` | ollama, ollama-init (pulls `OLLAMA_EMBEDDING_MODEL` and `OLLAMA_LLM_MODEL`) | `COMPOSE_PROFILES=ollama` |
| `searxng` | searxng — web search for AskBot (localhost:8888 for manual checks) | `COMPOSE_PROFILES=ollama,searxng` |
| `tools` | pgadmin (port 5051) | `COMPOSE_PROFILES=ollama,searxng,tools` |

## Web search (SearXNG)

AskBot falls back to web search when no memory matches. It tries `WEBSEARCH_PROVIDERS` in order and uses the
first provider that returns results:

| Entry | Meaning |
|-------|---------|
| `SearXng` | Self-hosted SearXNG container (`searxng` profile). Queries Google, Naver, Bing, DuckDuckGo, Brave… at once; engines that are rate-limited or show a CAPTCHA are covered by the others. No browser, no API key. |
| `Naver:Fetch`, `Google:Fetch` | Plain HTTP fetch of the search page (fragile: layout changes / bot checks). |
| `Naver:Api` | Naver Open API — set `MEMORIZER_WebSearch__Naver__ClientId` / `ClientSecret` in `env/local.override.env`. |

Default: `SearXng` only. Example with a fallback: `WEBSEARCH_PROVIDERS=SearXng,Naver:Api`.

The image contains **no browser** (Playwright was removed in v1.4.7). A legacy `Headless` access mode is accepted and treated as `Fetch`.
Engines are configured in [`searxng/settings.yml`](searxng/settings.yml) (Naver and Bing are enabled there; Korean queries use `ko-KR`).
Check SearXNG directly: `http://localhost:8888/search?q=akka&format=json`.

> The Bing Search API was retired in Aug 2025 and Google Custom Search JSON API is closed to new customers
> (shutdown Jan 2027), so the `Api` mode for Google/Bing is not a long-term option.

## ⚠️ Embedding dimensions

The vector columns are created as **384 dims** by default (`migrations/001_init.sql`; the 768 migration `009` is commented out).
The embedding model must match the database:

| Model | Dims | Preset |
|-------|------|--------|
| `all-minilm:33m-l12-v2-fp16` (Ollama) | 384 | llm-ollama (default) |
| `text-embedding-all-minilm-l12-v2` (Custom) | 384 | llm-custom (default) |
| `text-embedding-nomic-embed-text-v1.5` (Custom) | 768 | psmon.stack — DB migrated manually to 768 |
| `text-embedding-3-small` (OpenAI) | 1536 | llm-openai — requires migrating the vector columns first |

Switching the embedding model on an existing database breaks search until the columns and data are migrated
(see `src/Memorizer/migrations/embedding_dimension.md`).

## `.env` Variables

### Scenario switches
| Variable | Description | Default |
|----------|-------------|---------|
| `LLM_PRESET` | `ollama` \| `custom` \| `openai` → `env/llm-<preset>.env` | `ollama` |
| `COMPOSE_PROFILES` | Optional services: `ollama`, `tools` | `ollama` |
| `MEMORIZER_IMAGE` | App image name (also the tag used by `--build`) | `psmon/mcp-memorizer:latest` |
| `MEMORIZER_PORT` | Host port for the app | `5050` |
| `MEMORIZER_RUN_AS` | Container user: `app` or `0:0` | `app` |
| `CANONICAL_URL` | Public URL of the app | `http://localhost:5050` |
| `WEBSEARCH_PROVIDERS` | AskBot web search chain (see Web search) | `SearXng` |
| `WEBSEARCH_MODE` | Default access mode for page reading / manual API calls: `Fetch` \| `Api` | `Fetch` |
| `SEARXNG_PORT` / `SEARXNG_SECRET` | SearXNG localhost port / secret key | `8888` / sample |
| `SEARXNG_URL` | SearXNG URL used by the app | `http://searxng:8080` |

### Provider endpoints / models
| Variable | Preset | Default |
|----------|--------|---------|
| `OLLAMA_URL` | ollama | `http://ollama:11434` |
| `OLLAMA_EMBEDDING_MODEL` / `OLLAMA_LLM_MODEL` / `OLLAMA_LLM_EX_MODEL` | ollama | `all-minilm:33m-l12-v2-fp16` / `qwen2:0.5b` / = LLM model |
| `LLM_A_URL` / `LLM_A_API_KEY` | custom | `https://a1.webnori.com` / — (device A, default for every feature) |
| `LLM_B_URL` / `LLM_B_API_KEY` | custom | — (device B, used only when a feature is routed to it) |
| `EMBEDDING_URL`/`_API_KEY`, `LLM_URL`/`_API_KEY`, `LLM_EX_URL`/`_API_KEY`, `MULTIMODAL_URL`/`_API_KEY` | custom | device A — route a feature with e.g. `MULTIMODAL_URL=${LLM_B_URL}` + `MULTIMODAL_API_KEY=${LLM_B_API_KEY}` |
| `CUSTOM_EMBEDDING_MODEL` / `CUSTOM_LLM_MODEL` / `CUSTOM_LLM_EX_MODEL` / `CUSTOM_MULTIMODAL_MODEL` | custom | `text-embedding-all-minilm-l12-v2` / `google/gemma-4-e4b` / `google/gemma-4-26b-a4b` / `qwen/qwen3-vl-8b` |
| `OPENAI_API_KEY` | openai | — (required) |
| `OPENAI_EMBEDDING_MODEL` / `OPENAI_LLM_MODEL` / `OPENAI_LLM_EX_MODEL` / `OPENAI_MULTIMODAL_MODEL` | openai | `text-embedding-3-small` / `gpt-4o` / `gpt-4o` / `gpt-4o` |

### Secrets (dev defaults — change for anything shared)
| Variable | Maps to | Default |
|----------|---------|---------|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | postgres + `ConnectionStrings__Storage` | `postgres` / `postgres` / `postgmem` |
| `NEO4J_PASSWORD` | neo4j + `Neo4j__Password` | `password` |
| `ADMIN_USERNAME` / `ADMIN_PASSWORD` | `MEMORIZER_Server__UserName` / `Password` | `admin` / `admin123` |
| `MEMORIZER_API_KEY` | `MEMORIZER_Server__ApiKey` | `your-api-key-here` |
| `OAUTH_ENABLED` / `OAUTH_SECRET_KEY` | `MEMORIZER_OAuth__Enabled` / `SecretKey` | `true` / sample key |
| `OAUTH_CLIENT_ID` / `OAUTH_CLIENT_SECRET` | `MEMORIZER_OAuth__Clients__0__*` | `chatgpt-mcp` / sample secret |

### Ports
`POSTGRES_PORT` (5432), `NEO4J_HTTP_PORT` (7474), `NEO4J_BOLT_PORT` (7687), `OLLAMA_PORT` (11434), `PGADMIN_PORT` (5051). Override `POSTGRES_DATA` with a host path to bind-mount the database directory.

## Application Settings Reference

Provider variables used inside the presets (`MEMORIZER_` prefix maps to appsettings sections):

| Section | Variables |
|---------|-----------|
| Embeddings | `Type` (Custom/Ollama/OpenAI), `ApiUrl`, `Model`, `ApiKey` |
| LLM | `Type`, `ApiUrl`, `Model`, `Timeout`, `ApiKey`, `Chunking__MinCharactersForChunking`, `Chunking__TargetChunkSize` |
| LLM-EX (deep analysis: PRD Maker, ShapeUp) | `Type`, `ApiUrl`, `Model`, `Timeout` (5 min), `ApiKey` |
| MultiModal (vision) | `Type` (Custom/OpenAI — no Ollama), `ApiUrl`, `Model`, `Temperature`, `MaxTokens`, `Timeout`, `ApiKey` |

## Rancher 1.6

`docker-compose.rancher.yml` keeps compose v2 format because Rancher 1.6 does not support profiles or
`env_file` interpolation. It uses the Ollama provider; a commented OpenAI block shows the alternative.
Modern `docker compose` prints warnings for this file (`version` obsolete, `$i` not set) — that is expected
and does not affect Rancher.
