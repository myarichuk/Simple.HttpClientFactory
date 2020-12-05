#!/bin/sh

apt-get update
apt-get install -y apt-transport-https
apt-get update
apt-get install -y dotnet-sdk-3.1

echo "Executing MSBuild DLL begin command..."
dotnet tools/sonar/SonarScanner.MSBuild.dll begin /o:"myarichuk" /k:"myarichuk_Simple.HttpClientFactory" /d:sonar.cs.vstest.reportsPaths="**/TestResults/*.trx" /d:sonar.cs.opencover.reportsPaths="./Simple.HttpClientFactory.Tests/*.xml" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.verbose=true /d:sonar.login=${SONAR_TOKEN}
echo "Running build..."
dotnet build
echo "Running tests..."
dotnet test --logger:trx /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
echo "Executing MSBuild DLL end command..."
dotnet tools/sonar/SonarScanner.MSBuild.dll end /d:sonar.login=${SONAR_TOKEN}