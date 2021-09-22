path_to_OutputFolder="./Output"
path_to_JsonFolder="./TypeTreeDumps/InfoJson"
path_to_AssemblyDumper="./AssemblyDumper/bin/Release/net6/AssemblyDumper"
path_to_RuntimeLibrary="./AssemblyDumper/Libraries/System.Runtime.dll"
path_to_CollectionsLibrary="./AssemblyDumper/Libraries/System.Collections.dll"

mkdir "$(path_to_OutputFolder)"

generate() {
	i=$1
	echo Generating $i...
	"$(path_to_AssemblyDumper)" --output "$(path_to_OutputFolder)" --Runtime "$(path_to_RuntimeLibrary)" --Collections "$(path_to_CollectionsLibrary)" $i
}

cd "$(path_to_JsonFolder)"
vers=($(ls *.json | sort -t. -k1,1n -k2,2n -k3,3n))
cd ../..
echo Generating assemblies for ${#vers[@]} Unity versions
for ((i=0; i<${#vers[@]}; i++)); 
do
    generate ${vers[i]}
done