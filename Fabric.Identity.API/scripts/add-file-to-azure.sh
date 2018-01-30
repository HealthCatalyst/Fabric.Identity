#!/bin/bash

storageAccountName=$1
blobName=$2
fileToUpload=$3
storageAccountKey=$4


export AZURE_STORAGE_ACCOUNT=$storageAccountName
export AZURE_STORAGE_ACCESS_KEY=$storageAccountKey

container_name=fabric-release
blob_name=$blobName
file_to_upload=$fileToUpload

echo $storageAccountName
echo $file_to_upload
echo $blob_name

echo "Creating the container..."
az storage container create --name $container_name

echo "Uploading the file..."
az storage blob upload --container-name $container_name --file $file_to_upload --name $blob_name

echo "Listing the blobs..."
az storage blob list --container-name $container_name --output table

echo "Done"