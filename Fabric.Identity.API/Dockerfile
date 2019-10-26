FROM mcr.microsoft.com/dotnet/core/aspnet:2.2

ARG source
WORKDIR /app
EXPOSE 5001
COPY ${source:-obj/Docker/publish} .

CMD ["dotnet", "Fabric.Identity.API.dll"]
