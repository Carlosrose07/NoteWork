# ETAPA 1: construir la Api (usa el SDK completo de .NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se copian primero solo los .csproj para que Docker guarde en cache la restauracion de paquetes
COPY NoteWork.Shared/NoteWork.Shared.csproj NoteWork.Shared/
COPY NoteWork.Api/NoteWork.Api.csproj NoteWork.Api/
RUN dotnet restore NoteWork.Api/NoteWork.Api.csproj

# Ahora si se copia el codigo y se publica en modo Release
COPY NoteWork.Shared/ NoteWork.Shared/
COPY NoteWork.Api/ NoteWork.Api/
RUN dotnet publish NoteWork.Api/NoteWork.Api.csproj -c Release -o /app/publish --no-restore

# ETAPA 2: imagen final liviana (solo el runtime, sin SDK)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render espera que la Api escuche en el puerto 8080 por HTTP (Render pone el HTTPS por fuera)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NoteWork.Api.dll"]