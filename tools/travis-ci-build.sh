#!/bin/sh

echo "Executing SonarScanner begin command..."
dotnet tool run dotnet-sonarscanner begin /o:"myarichuk" /k:"myarichuk_Simple.HttpClientFactory" /d:sonar.cs.vstest.reportsPaths="**/TestResults/*.trx" /d:sonar.cs.opencover.reportsPaths="./Simple.HttpClientFactory.Tests/*.xml" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.verbose=true /d:sonar.login=${SONAR_TOKEN}
echo "Running build..."
dotnet build
echo "Running tests..."
dotnet test --logger:trx /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
echo "Executing SonarScanner end command..."
dotnet tool run dotnet-sonarscanner end /d:sonar.login=${SONAR_TOKEN}