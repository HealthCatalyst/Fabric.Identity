#!/bin/bash
echo "Cloning Git Wiki repo..."
REPO="https://$GITWIKIACCESSTOKEN@github.com/HealthCatalyst/Fabric.Identity.wiki.git"
echo $REPO
git clone $REPO

echo "Moving MD files to Fabric.Identity.wiki..."
mv overview.md API-Reference-Overview.md
mv paths.md API-Reference-Resources.md
mv definitions.md API-Reference-Models.md
mv security.md API-Reference-Security.md 

mv *.md Fabric.Identity.wiki

echo "Changing directory..."
cd Fabric.Identity.wiki

echo "-----Present directory = $(pwd)-----"

git config user.name "VSTS System User"
git config user.email "brian.smith@healthcatalyst.com"
git add *.md
git commit -m 'update API documentation'
git push origin master
