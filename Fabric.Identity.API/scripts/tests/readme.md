# Running CatalystDosIdentity Functional tests

## Run Against Remote Setup
To run against a remote identity server, you will need to point the tests to the identity url and pass in the installer secret. 

Sample call:
```
./CatalystDosIdentity.functional.tests.ps1 -identityUrl "https://server/identity" -installerSecret "secretKey"
```

## Run Against Local Docker Setup
## Setup Docker
Docker needs to be up and running and set to run Linux containers.

Run the start-identity.sh script to start the required docker images.

## Setup Identity Docker Environment
Once the identity image is setup, run the setup-samples.sh script.

You will need the installer secret from the setup-samples.sh script to run the functional tests.

*Note*: You may need to dot source the setup-samples.sh script so the script is able to parse out secrets to successfully complete:
```
. setup-samples.sh
```

## Run CatalystDosIdentity Functional Tests
Run the CatalystDosIdentity.functional.tests.ps1, and pass in the installerSecret parameter from the setup-samples.sh script (or valid installer secret if created outside of the setsup-samples.sh script).

You can also point the CatalystDosIdentity functional tests to another Identity url by passing the identityUrl parameter.


Sample invoke for local docker setup:
```
./CatalystDosIdentity.functional.tests.ps1 -installerSecret "secretKey"
```

