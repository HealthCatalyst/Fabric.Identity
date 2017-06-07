#!/bin/bash

set -e

host="$1"
shift
cmd="$@"

response=$(curl $host)

until [[ "$response" == "{\"couchdb\":\"Welcome\",\"version\":\"2.0.0\",\"vendor\":{\"name\":\"The Apache Software Foundation\"}}" ]]; do
	>&2 echo "couchdb is unavailable - sleeping"
	sleep 5 
	response=$(curl $host)
done

>&2 echo "couchdb is up - executing command \"$cmd\""
exec $cmd
