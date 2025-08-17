
# 디버그 컨테이너를 사용자 지정하는 방법과 Visual Studio 이 Dockerfile을 사용하여 더 빠른 디버깅을 위해 이미지를 빌드하는 방법을 알아보려면 https://aka.ms/customizecontainer를 참조하세요.

# 이 스테이지는 VS에서 빠른 모드로 실행할 때 사용됩니다(디버그 구성의 기본값).
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080


# 이 스테이지는 서비스 프로젝트를 빌드하는 데 사용됩니다.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution file and package management files
COPY ["Memorizer.slnx", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["NuGet.Config", "./"]

COPY ["src/Memorizer/Memorizer.csproj", "src/Memorizer/"]
COPY ["src/Memorizer.IntegrationTests/Memorizer.IntegrationTests.csproj", "src/Memorizer.IntegrationTests/"]

RUN dotnet restore "./Memorizer.slnx"
COPY . .
WORKDIR "/src/src/Memorizer"
RUN dotnet build "./Memorizer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 이 스테이지는 최종 스테이지에 복사할 서비스 프로젝트를 게시하는 데 사용됩니다.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Memorizer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 이 스테이지는 프로덕션에서 사용되거나 VS에서 일반 모드로 실행할 때 사용됩니다(디버그 구성을 사용하지 않는 경우 기본값).
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Memorizer.dll"]

