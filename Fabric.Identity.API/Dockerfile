FROM microsoft/dotnet:2.2-aspnetcore-runtime

ARG source
WORKDIR /app
EXPOSE 5001
COPY ${source:-obj/Docker/publish} .

CMD ["dotnet", "Fabric.Identity.API.dll"]
