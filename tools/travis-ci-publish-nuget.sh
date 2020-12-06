#!/bin/sh

echo "Generating NuGet package"
dotnet pack --configuration Release -o nupkg -p:Version="$GitVersion_FullSemVer" ./Simple.HttpClientFactory/Simple.HttpClientFactory.csproj 

echo "Publishing to 'api.nuget.org'"
find . -name *.nupkg -type f -print0 | xargs -0 -I pkg dotnet nuget push pkg -k ${NUGET_API_KEY} -s "https://api.nuget.org/v3/index.json" --skip-duplicate