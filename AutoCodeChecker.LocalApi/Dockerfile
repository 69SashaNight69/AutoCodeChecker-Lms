FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AutoCodeChecker.LocalApi/AutoCodeChecker.LocalApi.csproj", "AutoCodeChecker.LocalApi/"]
COPY ["AutoCodeChecker.Core/AutoCodeChecker.Core.csproj", "AutoCodeChecker.Core/"]
COPY ["AutoCodeChecker.Lambda.Assessment/AutoCodeChecker.Lambda.Assessment.csproj", "AutoCodeChecker.Lambda.Assessment/"]

RUN dotnet restore "AutoCodeChecker.LocalApi/AutoCodeChecker.LocalApi.csproj"

COPY . .
WORKDIR "/src/AutoCodeChecker.LocalApi"
RUN dotnet build "AutoCodeChecker.LocalApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AutoCodeChecker.LocalApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 7000
ENV ASPNETCORE_URLS=http://+:7000

ENTRYPOINT ["dotnet", "AutoCodeChecker.LocalApi.dll"]