# Usa la imagen oficial de .NET SDK para compilar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia el archivo del proyecto y restaura las dependencias
COPY ["E-Dukate.Presentation/E-Dukate.Presentation.csproj", "E-Dukate.Presentation/"]
COPY ["E-Dukate.Application/E-Dukate.Application.csproj", "E-Dukate.Application/"]
COPY ["E-Dukate.Infrastructure/E-Dukate.Infrastructure.csproj", "E-Dukate.Infrastructure/"]
COPY ["E-Dukate.Domain/E-Dukate.Domain.csproj", "E-Dukate.Domain/"]
RUN dotnet restore "E-Dukate.Presentation/E-Dukate.Presentation.csproj"

# Copia todo el código y construye la aplicación
COPY . .
WORKDIR "/src/E-Dukate.Presentation"
RUN dotnet build "E-Dukate.Presentation.csproj" -c Release -o /app/build

# Publica la aplicación
FROM build AS publish
RUN dotnet publish "E-Dukate.Presentation.csproj" -c Release -o /app/publish

# Imagen final y más pequeña, solo con el runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Configura el puerto que usará Railway
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Copia la aplicación publicada desde la etapa 'publish'
COPY --from=publish /app/publish .

# La configuración de entorno se inyectará por Railway
ENTRYPOINT ["dotnet", "E-Dukate.Presentation.dll"]