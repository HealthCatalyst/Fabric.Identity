FROM microsoft/aspnetcore:1.1.2

# Install prereqs
#RUN apt-get update -y -qq && apt-get install -y --no-install-recommends \
    #build-essential \
	#pkg-config erlang \
    #libicu-dev \
	#libmozjs185-dev \
	#libcurl4-openssl-dev \
	#runit

# Build couchdb
#RUN useradd -m couchdb
#
#ENV COUCHDB_VERSION 2.0.0

#RUN cd /usr/src && mkdir couchdb \
  #&& curl -fSL https://dist.apache.org/repos/dist/release/couchdb/source/$COUCHDB_VERSION/apache-couchdb-$COUCHDB_VERSION.tar.gz -o couchdb.tar.gz \
  #&& tar -xzf couchdb.tar.gz -C couchdb --strip-components=1

  
#RUN cd /usr/src/couchdb \
  #&& ./configure \
  #&& make release

#COPY local.ini /home/couchdb/couchdb/etc/local.d/
#COPY runcouch.sh /etc/service/couchdb/run
#COPY rundotnet.sh /etc/service/identity/run
#COPY entrypoint.sh /usr/local/bin/entrypoint.sh

#RUN cp -R /usr/src/couchdb/rel/couchdb /home/couchdb \
  #&& rm -rf /usr/src/couchdb* 

ARG source
WORKDIR /app
#EXPOSE 5001 5984
EXPOSE 5001
COPY ${source:-obj/Docker/publish} .

COPY wait-for-couchdb.sh wait-for-couchdb.sh

ADD https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh wait-for-it.sh
RUN chmod +x wait-for-it.sh \	
  && chmod +x wait-for-couchdb.sh

CMD ["dotnet", "Fabric.Identity.API.dll"]
