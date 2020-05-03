#!/bin/sh
echo "Starting install..."
wget -O sonar.zip https://github.com/SonarSource/sonar-scanner-msbuild/releases/download/4.8.0.12008/sonar-scanner-msbuild-4.8.0.12008-netcoreapp3.0.zip
echo "Unzipping..."
unzip -qq sonar.zip -d tools/sonar
echo "Displaying file structure..."
find .
ls -l tools/sonar
echo "Changing permissions..."
chmod +x tools/sonar/sonar-scanner-4.2.0.1873/bin/sonar-scanner