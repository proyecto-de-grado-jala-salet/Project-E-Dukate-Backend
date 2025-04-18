FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["E-Dukate.Presentation/E-Dukate.Presentation.csproj", "E-Dukate.Presentation/"]
COPY ["E-Dukate.Application/E-Dukate.Application.csproj", "E-Dukate.Application/"]
COPY ["E-Dukate.Domain/E-Dukate.Domain.csproj", "E-Dukate.Domain/"]
COPY ["E-Dukate.Infrastructure/E-Dukate.Infrastructure.csproj", "E-Dukate.Infrastructure/"]
RUN dotnet restore "E-Dukate.Presentation/E-Dukate.Presentation.csproj"
COPY . .
WORKDIR "/src/E-Dukate.Presentation"
RUN dotnet build "E-Dukate.Presentation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "E-Dukate.Presentation.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "E-Dukate.Presentation.dll"]