FROM microsoft/aspnetcore:1.1.2

ARG source
WORKDIR /app
EXPOSE 5001
COPY ${source:-obj/Docker/publish} .

COPY ${source:-}/wait-for-couchdb.sh wait-for-couchdb.sh

ADD https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh wait-for-it.sh
RUN chmod +x wait-for-it.sh \	
  && chmod +x wait-for-couchdb.sh

CMD ["dotnet", "Fabric.Identity.API.dll"]
