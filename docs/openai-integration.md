# OpenAI Integration Guide

Memorizer now supports OpenAI API for both LLM and embedding services alongside the existing Ollama support.

## Configuration

The service automatically detects whether to use OpenAI or Ollama based on the API URL in your configuration:
- If the URL contains `api.openai.com`, OpenAI services will be used
- Any other URL will use Ollama services

### OpenAI Configuration Example

Create or modify your `appsettings.json` with the following configuration:

```json
{
  "Embeddings": {
    "ApiUrl": "https://api.openai.com/v1",
    "Model": "text-embedding-3-small",
    "ApiKey": "sk-your-openai-api-key-here",
    "Timeout": "00:00:30"
  },
  "LLM": {
    "ApiUrl": "https://api.openai.com/v1",
    "Model": "gpt-4o-mini",
    "Timeout": "00:02:00",
    "ApiKey": "sk-your-openai-api-key-here"
  }
}
```

### Ollama Configuration (Default)

For Ollama, use the standard configuration:

```json
{
  "Embeddings": {
    "ApiUrl": "http://localhost:11434",
    "Model": "all-minilm:33m-l12-v2-fp16",
    "ApiKey": ""
  },
  "LLM": {
    "ApiUrl": "http://localhost:11434",
    "Model": "qwen2:0.5b",
    "Timeout": "00:02:00",
    "ApiKey": ""
  }
}
```

## Supported Models

### OpenAI Embedding Models
- `text-embedding-3-small` (recommended, 1536 dimensions)
- `text-embedding-3-large` (3072 dimensions)
- `text-embedding-ada-002` (legacy, 1536 dimensions)

### OpenAI LLM Models
- `gpt-4o-mini` (recommended for cost-effectiveness)
- `gpt-4o`
- `gpt-4-turbo`
- `gpt-3.5-turbo`

## Environment Variables

You can also configure the API key using environment variables:

```bash
export MEMORIZER_Embeddings__ApiKey="sk-your-openai-api-key-here"
export MEMORIZER_LLM__ApiKey="sk-your-openai-api-key-here"
```

## Migration from Ollama to OpenAI

1. Update your `appsettings.json` with OpenAI configuration
2. Set your OpenAI API key
3. Restart the service

The service will automatically use the OpenAI implementation when it detects the OpenAI API URL.

## Cost Considerations

When using OpenAI:
- **Embeddings**: text-embedding-3-small costs approximately $0.02 per 1M tokens
- **LLM**: gpt-4o-mini costs approximately $0.15 per 1M input tokens and $0.60 per 1M output tokens

For cost optimization:
- Use `text-embedding-3-small` for embeddings
- Use `gpt-4o-mini` for title generation
- Consider caching embeddings to reduce API calls

## Troubleshooting

### Authentication Errors
Ensure your API key is correctly set and has the necessary permissions.

### Rate Limiting
OpenAI has rate limits. If you encounter rate limit errors, consider:
- Implementing retry logic with exponential backoff
- Upgrading your OpenAI tier for higher limits
- Using batch processing where possible

### Dimension Mismatch
If switching from Ollama to OpenAI or vice versa, note that embedding dimensions may differ:
- Ollama models typically use 384 dimensions
- OpenAI text-embedding-3-small uses 1536 dimensions
- You may need to re-generate embeddings when switching providers