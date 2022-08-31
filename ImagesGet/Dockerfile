FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ImagesGet/ImagesGet.csproj", "ImagesGet/"]
RUN dotnet restore "ImagesGet/ImagesGet.csproj"
COPY . .
WORKDIR "/src/ImagesGet"
RUN dotnet build "ImagesGet.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ImagesGet.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImagesGet.dll"]
