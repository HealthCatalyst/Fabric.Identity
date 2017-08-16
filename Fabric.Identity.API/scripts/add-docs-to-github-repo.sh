#!/bin/bash

echo "Cloning Git Wiki repo..."
git clone https://$GITWIKIACCESSTOKEN@github.com/HealthCatalyst/Fabric.Identity.wiki.git

echo "Moving MD files to Fabric.Identity.wiki..."
mv *.md Fabric.Identity.wiki

echo "Changing directory..."
cd Fabric.Identity.wiki

echo "Present directory = $(pwd)"

git config user.name "Fabric Identity System User"
git config user.email "brian.smith@healthcatalyst.com"
git add *.md
git commit -m 'update API documentation'
git push origin master
