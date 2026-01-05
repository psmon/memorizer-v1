# Docker Compose Configuration Guide

## Overview

This project provides multiple docker-compose configuration files for different deployment scenarios. Each configuration uses different LLM providers (Custom, Ollama, OpenAI) for various features.

## Configuration Files

| File | LLM Provider | LLM-EX Provider | Use Case |
|------|-------------|-----------------|----------|
| `docker-compose.yml` | Ollama | Ollama | Default configuration for local development |
| `docker-compose.custom-sample.yml` | Custom | Custom | Internal network with LM Studio compatible API |
| `docker-compose.local-psmon.yml` | Custom | Custom | Local development with custom LLM server |
| `docker-compose.openai-sample.yml` | OpenAI | OpenAI | Cloud-based OpenAI API |
| `docker-compose.ollama.yml` | N/A | N/A | Ollama service only (for server deployment) |
| `docker-compose.server.yml` | Ollama | Ollama | Server deployment (Rancher 1.6) |

## LLM Provider Types

### Custom
- Uses LM Studio compatible OpenAI-style API
- Best for internal network deployments
- No API key required
- Configuration:
  ```yaml
  MEMORIZER_LLM__Type: Custom
  MEMORIZER_LLM__ApiUrl: http://192.168.0.50:1234
  MEMORIZER_LLM__Model: openai/gpt-oss-20b
  ```

### Ollama
- Uses local Ollama instance
- Best for lightweight local development
- No API key required
- Configuration:
  ```yaml
  MEMORIZER_LLM__Type: Ollama
  MEMORIZER_LLM__ApiUrl: http://ollama:11434
  MEMORIZER_LLM__Model: qwen2:0.5b
  ```

### OpenAI
- Uses OpenAI cloud API
- Requires API key
- Configuration:
  ```yaml
  MEMORIZER_LLM__Type: OpenAI
  MEMORIZER_LLM__ApiUrl: api.openai.com
  MEMORIZER_LLM__Model: gpt-4o
  MEMORIZER_LLM__ApiKey: sk-...
  ```

## LLM-EX Configuration

LLM-EX (Extended) is used for deep analysis features like PRD analysis and Event Storming. It supports the same three provider types: Custom, Ollama, and OpenAI.

### Custom LLM-EX
```yaml
MEMORIZER_LLM-EX__Type: Custom
MEMORIZER_LLM-EX__ApiUrl: http://192.168.0.50:1234
MEMORIZER_LLM-EX__Model: openai/gpt-oss-120b
MEMORIZER_LLM-EX__Timeout: 00:05:00
```

### Ollama LLM-EX
```yaml
MEMORIZER_LLM-EX__Type: Ollama
MEMORIZER_LLM-EX__ApiUrl: http://ollama:11434
MEMORIZER_LLM-EX__Model: qwen2:0.5b
MEMORIZER_LLM-EX__Timeout: 00:05:00
```

### OpenAI LLM-EX
```yaml
MEMORIZER_LLM-EX__Type: OpenAI
MEMORIZER_LLM-EX__Model: gpt-4o
MEMORIZER_LLM-EX__ApiKey: sk-...
MEMORIZER_LLM-EX__Timeout: 00:05:00
```

## Environment Variables Reference

### Core Settings
| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__Storage` | PostgreSQL connection string | Required |
| `ASPNETCORE_ENVIRONMENT` | Environment mode | Development |

### Embeddings Settings
| Variable | Description | Default |
|----------|-------------|---------|
| `MEMORIZER_Embeddings__Type` | Provider type (Custom/Ollama/OpenAI) | Ollama |
| `MEMORIZER_Embeddings__ApiUrl` | API endpoint URL | http://localhost:11434 |
| `MEMORIZER_Embeddings__Model` | Embedding model name | all-minilm:33m-l12-v2-fp16 |
| `MEMORIZER_Embeddings__ApiKey` | API key (OpenAI only) | - |

### LLM Settings
| Variable | Description | Default |
|----------|-------------|---------|
| `MEMORIZER_LLM__Type` | Provider type (Custom/Ollama/OpenAI) | Ollama |
| `MEMORIZER_LLM__ApiUrl` | API endpoint URL | http://localhost:11434 |
| `MEMORIZER_LLM__Model` | LLM model name | llama3 |
| `MEMORIZER_LLM__Timeout` | Request timeout | 00:02:00 |
| `MEMORIZER_LLM__ApiKey` | API key (OpenAI only) | - |

### LLM-EX Settings (Deep Analysis)
| Variable | Description | Default |
|----------|-------------|---------|
| `MEMORIZER_LLM-EX__Type` | Provider type (Custom/Ollama/OpenAI) | Custom |
| `MEMORIZER_LLM-EX__ApiUrl` | API endpoint URL | http://localhost:1234 |
| `MEMORIZER_LLM-EX__Model` | LLM model name | openai/gpt-oss-120b |
| `MEMORIZER_LLM-EX__Timeout` | Request timeout (longer for complex analysis) | 00:05:00 |
| `MEMORIZER_LLM-EX__ApiKey` | API key (OpenAI only) | - |

### MultiModal Settings
| Variable | Description | Default |
|----------|-------------|---------|
| `MEMORIZER_MultiModal__Type` | Provider type (Custom/OpenAI) | Custom |
| `MEMORIZER_MultiModal__ApiUrl` | API endpoint URL | http://localhost:1234 |
| `MEMORIZER_MultiModal__Model` | Vision model name | qwen/qwen3-vl-8b |
| `MEMORIZER_MultiModal__ApiKey` | API key (OpenAI only) | - |

### Neo4j Settings
| Variable | Description | Default |
|----------|-------------|---------|
| `Neo4j__Uri` | Neo4j Bolt URI | bolt://localhost:7687 |
| `Neo4j__User` | Neo4j username | neo4j |
| `Neo4j__Password` | Neo4j password | password |
| `Neo4j__Database` | Neo4j database name | neo4j |

### Server Settings
| Variable | Description | Default |
|----------|-------------|---------|
| `MEMORIZER_Server__CanonicalUrl` | Public URL for the application | http://localhost:5000 |
| `MEMORIZER_Server__UserName` | Admin username | - |
| `MEMORIZER_Server__Password` | Admin password | - |
| `MEMORIZER_Server__ApiKey` | API authentication key | - |

### OAuth 2.0 Settings (for ChatGPT MCP integration)
| Variable | Description | Default |
|----------|-------------|---------|
| `MEMORIZER_OAuth__Enabled` | Enable OAuth 2.0 authentication | false |
| `MEMORIZER_OAuth__Issuer` | Token issuer name | memorizer |
| `MEMORIZER_OAuth__Audience` | Token audience | memorizer-api |
| `MEMORIZER_OAuth__SecretKey` | Secret key for token signing (min 32 chars) | Required |
| `MEMORIZER_OAuth__TokenExpirationHours` | Token expiration time in hours | 24 |

### OAuth Client Configuration
| Variable | Description | Example |
|----------|-------------|---------|
| `MEMORIZER_OAuth__Clients__0__ClientId` | Client identifier | chatgpt-mcp |
| `MEMORIZER_OAuth__Clients__0__ClientSecret` | Client secret | chatgpt-secret-key |
| `MEMORIZER_OAuth__Clients__0__AllowedScopes__0` | First allowed scope | mcp:read |
| `MEMORIZER_OAuth__Clients__0__AllowedScopes__1` | Second allowed scope | mcp:write |
| `MEMORIZER_OAuth__Clients__0__AllowedRedirectUris__0` | Allowed redirect URI | https://chatgpt.com/aip/g-callback |

## OAuth Configuration Example

For ChatGPT MCP integration:
```yaml
# OAuth 2.0 Settings
MEMORIZER_OAuth__Enabled: "true"
MEMORIZER_OAuth__Issuer: memorizer
MEMORIZER_OAuth__Audience: memorizer-api
MEMORIZER_OAuth__SecretKey: your-super-secret-key-at-least-32-characters-long
MEMORIZER_OAuth__TokenExpirationHours: "24"
# OAuth Client (for ChatGPT MCP integration)
MEMORIZER_OAuth__Clients__0__ClientId: chatgpt-mcp
MEMORIZER_OAuth__Clients__0__ClientSecret: chatgpt-secret-key
MEMORIZER_OAuth__Clients__0__AllowedScopes__0: "mcp:read"
MEMORIZER_OAuth__Clients__0__AllowedScopes__1: "mcp:write"
MEMORIZER_OAuth__Clients__0__AllowedRedirectUris__0: "https://chatgpt.com/aip/g-callback"
```

## Usage Examples

### Local Development with Ollama (Default)
```bash
docker-compose up -d
```

### Local Development with Custom LLM Server
```bash
docker-compose -f docker-compose.local-psmon.yml up -d
```

### Production with OpenAI
```bash
docker-compose -f docker-compose.openai-sample.yml up -d
```

### Server Deployment
```bash
docker-compose -f docker-compose.server.yml up -d
```

## Notes

- LLM-EX is designed for deep analysis tasks and uses higher capacity models
- The default timeout for LLM-EX is 5 minutes to accommodate complex analysis
- When using OpenAI, ensure API keys are properly configured for all services (LLM, LLM-EX, Embeddings, MultiModal)
- Custom provider is compatible with LM Studio and similar OpenAI-compatible API servers
