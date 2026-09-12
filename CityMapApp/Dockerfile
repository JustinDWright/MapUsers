FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5156

ENV ASPNETCORE_URLS=http://+:5156

RUN mkdir /data && chown app:app /data
USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["CityMapApp/CityMapApp.csproj", "CityMapApp/"]
COPY ["CityMapApp.ServiceDefaults/CityMapApp.ServiceDefaults.csproj", "CityMapApp.ServiceDefaults/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "CityMapApp/CityMapApp.csproj"
COPY . .
WORKDIR "/src/CityMapApp"
RUN dotnet build "CityMapApp.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "CityMapApp.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish --chown=app:app /app/publish .
ENTRYPOINT ["dotnet", "CityMapApp.dll"]
