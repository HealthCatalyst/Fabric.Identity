#!/bin/bash

git clone https://$GITWIKIACCESSTOKEN@github.com/HealthCatalyst/Fabric.Identity.wiki.git

mv *.md Fabric.Identity.wiki
git add Fabric.Identity.wiki/*.md
git commit -m 'update API documentation'
git push origin master
