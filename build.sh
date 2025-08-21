#!/bin/bash

# Build script for Memorizer project with Neo4j integration

echo "Building Memorizer project with Neo4j support..."

# Use Docker to build the project
docker run --rm -v "$(pwd):/src" -w /src/src/Memorizer mcr.microsoft.com/dotnet/sdk:9.0 dotnet build

if [ $? -eq 0 ]; then
    echo "Build completed successfully!"
else
    echo "Build failed. Please check the error messages above."
    exit 1
fi