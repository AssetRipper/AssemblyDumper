dotnet build -c Debug


cd ./AssetRipper.AssemblyDumper/bin/Debug/net6.0/

AssetRipper.AssemblyDumper.exe

cd ../../../../


cd ./AssetRipper.AssemblyDumper.Recompiler/bin/Debug/net6.0/

AssetRipper.AssemblyDumper.Recompiler.exe ../../../../AssetRipper.AssemblyDumper/bin/Debug/net6.0/AssetRipper.SourceGenerated.dll ./Output/

cd ../../../../


cd ./AssetRipper.AssemblyDumper.Recompiler/bin/Debug/net6.0/Output/

dotnet build -c Release

cd ../../../../../