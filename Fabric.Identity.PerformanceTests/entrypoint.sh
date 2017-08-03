#!/bin/bash

./create-userproperties.sh $AUTH_SERVER $AUTH_PORT $IDENTITY_SERVER $IDENTITY_PORT
mv user.properties /jmeter/apache-jmeter-3.2/bin/user.properties
mkdir results
mkdir results/output
/jmeter/apache-jmeter-3.2/bin/jmeter -n -t /Fabric.Identity.Perf.jmx -l /results/results.txt -e -o /results/output
cd /apdexcalc
dotnet /apdexcalc/Fabric.Identity.ApdexCalculator.dll /results/results.txt
