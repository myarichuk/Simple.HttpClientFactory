#!/bin/sh
echo "Changing to /src directory..."
cd src
echo "Executing MSBuild DLL begin command..."
dotnet tools/sonar/SonarScanner.MSBuild.dll begin /o:"myarichuk" /k:"myarichuk_Simple.HttpClientFactory" /d:sonar.cs.vstest.reportsPaths="**/TestResults/*.trx" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.verbose=true /d:sonar.login=${SONAR_TOKEN}
echo "Running build..."
dotnet build
echo "Running tests..."
dotnet test --logger:trx
echo "Executing MSBuild DLL end command..."
dotnet tools/sonar/SonarScanner.MSBuild.dll end /d:sonar.login=${SONAR_TOKEN}