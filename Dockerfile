FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Libreria.Client/Libreria.Client.csproj", "src/Libreria.Client/"]
COPY ["src/Libreria.Application/Libreria.Application.csproj", "src/Libreria.Application/"]
COPY ["src/Libreria.Infrastructure/Libreria.Infrastructure.csproj", "src/Libreria.Infrastructure/"]
COPY ["src/Libreria.Domain/Libreria.Domain.csproj", "src/Libreria.Domain/"]
RUN dotnet restore "src/Libreria.Client/Libreria.Client.csproj"

COPY . .
WORKDIR "/src/src/Libreria.Client"
RUN dotnet publish "Libreria.Client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Libreria.Client.dll"]
