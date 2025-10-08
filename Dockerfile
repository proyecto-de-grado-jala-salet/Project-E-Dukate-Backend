# ============================
# 1️⃣ Etapa de compilación
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar los archivos de proyecto y restaurar dependencias
COPY E-Dukate.Domain/E-Dukate.Domain.csproj E-Dukate.Domain/
COPY E-Dukate.Infrastructure/E-Dukate.Infrastructure.csproj E-Dukate.Infrastructure/
COPY E-Dukate.Application/E-Dukate.Application.csproj E-Dukate.Application/
COPY E-Dukate.Presentation/E-Dukate.Presentation.csproj E-Dukate.Presentation/

RUN dotnet restore E-Dukate.Presentation/E-Dukate.Presentation.csproj

# Copiar el resto del código y compilar
COPY . .
WORKDIR /app/E-Dukate.Presentation
RUN dotnet publish -c Release -o /out /p:UseAppHost=false

# ============================
# 2️⃣ Etapa de ejecución
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar publicación desde la etapa anterior
COPY --from=build /out .

# Variables de entorno requeridas por Railway
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Exponer el puerto
EXPOSE 8080

# Comando de arranque
ENTRYPOINT ["dotnet", "E-Dukate.Presentation.dll"]