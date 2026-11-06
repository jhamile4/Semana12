FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TallerMecanico/TallerMecanico.csproj TallerMecanico/
RUN dotnet restore TallerMecanico/TallerMecanico.csproj
COPY TallerMecanico/ TallerMecanico/
RUN dotnet publish TallerMecanico/TallerMecanico.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "TallerMecanico.dll"]
