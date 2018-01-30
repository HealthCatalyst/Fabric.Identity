#!/bin/bash

storageAccountName=$1
storageAccountKey=$2
blobName=$3
fileToUpload=$4


export AZURE_STORAGE_ACCOUNT=$storageAccountName
export AZURE_STORAGE_ACCESS_KEY=$storageAccountKey

export container_name=fabric-release
export blob_name=$blobName
export file_to_upload=$fileToUpload

echo "Creating the container..."
az storage container create --name $container_name

echo "Uploading the file..."
az storage blob upload --container-name $container_name --file $file_to_upload --name $blob_name

echo "Listing the blobs..."
az storage blob list --container-name $container_name --output table

echo "Done"