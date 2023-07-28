FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src
COPY ["src", ""]
COPY ["*.sln", ""]

COPY . .
WORKDIR /src/src/FrigateSender
RUN dotnet restore
RUN dotnet build "FrigateSender.csproj" -c Release -o /app/build

#WORKDIR /src
#RUN dotnet test "FrigateSender.sln" "--logger:trx"
#WORKDIR /src/src/FrigateSender

FROM build AS publish
RUN dotnet publish "FrigateSender.csproj" -c Release -o /app/publish
FROM base AS final

RUN apt-get update && apt-get install -y ffmpeg

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "FrigateSender.dll"]

# go to directory above docker file in it in PS, and write: 
# docker build FrigateSender -t frigatesender --progress=plain
# docker run -d -p 80:80 frigatesender:latest -e environment=Development -e ASPNETCORE_ENVIRONMENT=Development