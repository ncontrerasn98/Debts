FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Notification.Service/Notification.Service.csproj", "Notification.Service/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
RUN dotnet restore "Notification.Service/Notification.Service.csproj"
COPY . .
WORKDIR "/src/Notification.Service"
RUN dotnet build "Notification.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Notification.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notification.Service.dll"]