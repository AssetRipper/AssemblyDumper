dotnet build -c Debug


cd ./AssetRipper.AssemblyDumper/bin/Debug/net8.0/

AssetRipper.AssemblyDumper.exe --include-player-settings

cd ../../../../


cd ./AssetRipper.AssemblyDumper.Recompiler/bin/Debug/net8.0/

AssetRipper.AssemblyDumper.Recompiler.exe ../../../../AssetRipper.AssemblyDumper/bin/Debug/net8.0/AssetRipper.SourceGenerated.dll ./Output/

cd ../../../../


cd ./AssetRipper.AssemblyDumper.Recompiler/bin/Debug/net8.0/Output/

dotnet build -c Release

cd ../../../../../