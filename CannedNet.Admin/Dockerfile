FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CannedNet.slnx ./
COPY CannedNet.Shared/CannedNet.Shared.csproj CannedNet.Shared/
COPY CannedNet.Admin/CannedNet.Admin.csproj CannedNet.Admin/
RUN dotnet restore CannedNet.Admin/CannedNet.Admin.csproj
COPY . .
WORKDIR /src/CannedNet.Admin
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
RUN chown -R app:app /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
USER app
ENTRYPOINT ["dotnet", "CannedNet.Admin.dll"]