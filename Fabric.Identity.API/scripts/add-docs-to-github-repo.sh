#!/bin/bash

git clone https://$GITWIKIACCESSTOKEN@github.com/HealthCatalyst/Fabric.Identity.wiki.git
git add $Build.ArtifactStagingDirectory/*.md
git commit -m 'update API documentation'
git push origin master
