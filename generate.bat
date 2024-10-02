:: Build Solution

dotnet build -c Debug


:: Generate Dll

cd ./AssetRipper.AssemblyDumper/bin/Debug/net9.0/

AssetRipper.AssemblyDumper.exe

cd ../../../../


:: Decompile Dll

cd ./AssetRipper.AssemblyDumper.Recompiler/bin/Debug/net9.0/

AssetRipper.AssemblyDumper.Recompiler.exe ../../../../AssetRipper.AssemblyDumper/bin/Debug/net9.0/AssetRipper.SourceGenerated.dll ./Output/ %1

cd ../../../../


:: Recompile into a NuGet package

cd ./AssetRipper.AssemblyDumper.Recompiler/bin/Debug/net9.0/Output/

dotnet build -c Release

cd ../../../../../


:: Remove the dependency references

cd ./AssetRipper.AssemblyDumper.NuGetFixer/bin/Debug/net9.0/

AssetRipper.AssemblyDumper.NuGetFixer.exe ../../../../AssetRipper.AssemblyDumper.Recompiler/bin/Debug/net9.0/Output/bin/Release/ %1