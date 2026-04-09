FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY GymManagementBackend/GymManagementBackend.csproj GymManagementBackend/
RUN dotnet restore GymManagementBackend/GymManagementBackend.csproj

COPY . .
RUN dotnet publish GymManagementBackend/GymManagementBackend.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Render uses port 10000 by default for Docker services.
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "GymManagementBackend.dll"]
