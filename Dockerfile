# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Copy csproj and restore dependencies
COPY YeeMotion.csproj .
RUN dotnet restore -a $TARGETARCH

# Copy source and build
COPY *.cs ./
RUN dotnet publish -a $TARGETARCH -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build /app .

ENV GPIO_PIN=23
ENV BULB_ADDRESS=192.168.178.25

ENTRYPOINT ["dotnet", "YeeMotion.dll"]
