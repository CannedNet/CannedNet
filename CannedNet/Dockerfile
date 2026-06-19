FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CannedNet.slnx ./
COPY CannedNet.Shared/CannedNet.Shared.csproj CannedNet.Shared/
COPY CannedNet/CannedNet.csproj CannedNet/
RUN dotnet restore CannedNet/CannedNet.csproj
COPY . .
WORKDIR /src/CannedNet
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
COPY --from=build /src/CannedNet/JSON JSON/
RUN mkdir -p Data Images && chown -R app:app /app && chmod -R u+w /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
VOLUME ["/app/Data", "/app/Images"]
USER app
ENTRYPOINT ["dotnet", "CannedNet.dll"]