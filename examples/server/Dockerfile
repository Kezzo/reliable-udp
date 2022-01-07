# create build layer
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder

WORKDIR /build

COPY server.csproj ./
COPY /src/ ./src/

# create release binary
RUN dotnet publish -c Release -o out

# create separate layer for running the built app
FROM mcr.microsoft.com/dotnet/sdk:6.0 as runner

WORKDIR /app

COPY --from=builder /build/out .

ENTRYPOINT ["dotnet", "server.dll"]