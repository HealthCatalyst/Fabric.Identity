# Running CatalystDosIdentity Functional tests

## Setup Docker
Docker needs to be up and running and set to run Linux containers.  

The script run-functional-tests.sh starts up the docker enviroment but also runs functional tests which will pollute the enviroment for CatalystDosIdentity testing as well as shut down the identity container once finished testing. 

To get around this, run the first half of the script before the functional test start (up to line 34 in run-functional-tests.sh).

## Setup Identity Docker Enviroment
Once the identity container is setup, run the setup-samples.sh script.  You will need the installer secret from the setup-samples.sh script.

## Run CatalystDosIdentity Functional Tests
Run the CatalystDosIdentity.tests.ps1, and pass in the installerSecret parameter from the setup-samples.sh script (or valid installer secret if created outside of the setsup-samples.sh script).

You can also point the CatalystDosIdentity functional tests to another Identity url by passing the identityUrl parameter.