#!/bin/sh

echo "Travis Branch variable is set to $TRAVIS_BRANCH"
if [ $TRAVIS_BRANCH == "master"]; then
  echo "Generating NuGet package";
  dotnet pack --configuration Release -o nupkg -p:Version="$GitVersion_FullSemVer" ./Simple.HttpClientFactory/Simple.HttpClientFactory.csproj;

  echo "Publishing to 'api.nuget.org'";
  find . -name *.nupkg -type f -print0 | xargs -0 -I pkg dotnet nuget push pkg -k ${NUGET_API_KEY} -s "https://api.nuget.org/v3/index.json" --skip-duplicate;
else 
  echo "Skip nuget publish because we are not on 'master' branch!"; 
fi
