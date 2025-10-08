# Etapa de construcción
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["E-Dukate.Presentation/E-Dukate.Presentation.csproj", "E-Dukate.Presentation/"]
COPY ["E-Dukate.Application/E-Dukate.Application.csproj", "E-Dukate.Application/"]
COPY ["E-Dukate.Infrastructure/E-Dukate.Infrastructure.csproj", "E-Dukate.Infrastructure/"]
COPY ["E-Dukate.Domain/E-Dukate.Domain.csproj", "E-Dukate.Domain/"]
RUN dotnet restore "E-Dukate.Presentation/E-Dukate.Presentation.csproj"

COPY . .
WORKDIR "/src/E-Dukate.Presentation"
RUN dotnet build "E-Dukate.Presentation.csproj" -c Release -o /app/build

# Etapa de publicación
FROM build AS publish
RUN dotnet publish "E-Dukate.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configurar puerto para Railway
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "E-Dukate.Presentation.dll"]