FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Debts.API/Debts.API.csproj", "Debts.API/"]
COPY ["Debts.Application/Debts.Application.csproj", "Debts.Application/"]
COPY ["Debts.Domain/Debts.Domain.csproj", "Debts.Domain/"]
COPY ["Debts.Infrastructure/Debts.Infrastructure.csproj", "Debts.Infrastructure/"]
RUN dotnet restore "Debts.API/Debts.API.csproj"
COPY . .
WORKDIR "/src/Debts.API"
RUN dotnet build "Debts.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Debts.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Debts.API.dll"]