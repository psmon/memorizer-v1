# SearXNG with Memorizer's settings baked in — for hosts where settings.yml can't be bind-mounted (Rancher).
# Local compose mounts searxng/settings.yml instead.
#   docker build -t registry.webnori.com/memorizer-searxng:v1.4.7 searxng
FROM searxng/searxng:latest
COPY settings.yml /etc/searxng/settings.yml
