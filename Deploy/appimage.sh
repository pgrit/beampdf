#!/bin/bash

mkdir -p staging
mkdir -p staging/bin

dotnet publish beampdf --self-contained -r linux-x64 -c Release -o staging/bin

cp Deploy/beampdf.desktop staging/
cp beampdf/Assets/256x256.png staging/

cd staging
ln -s 256x256.png .DirIcon
ln -s bin/beampdf AppRun
cd ..

curl -L https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage --output appimagetool.AppImage
chmod +x ./appimagetool.AppImage

./appimagetool.AppImage ./staging

rm appimagetool.AppImage
