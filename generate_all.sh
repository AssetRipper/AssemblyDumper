path_to_OutputFolder="./Output"
path_to_JsonFolder="./TypeTreeDumps/InfoJson"
path_to_AssemblyDumper="./AssemblyDumper/bin/Release/net6/AssemblyDumper"
path_to_RuntimeLibrary="./AssemblyDumper/Libraries/System.Runtime.dll"
path_to_CollectionsLibrary="./AssemblyDumper/Libraries/System.Collections.dll"

mkdir "./Output"

generate() {
	i=$1
	echo Generating $i...
	./AssemblyDumper/bin/Release/net6/AssemblyDumper --output ./Output --Runtime ./AssemblyDumper/Libraries/System.Runtime.dll --Collections ./AssemblyDumper/Libraries/System.Collections.dll ./TypeTreeDumps/InfoJson/$i
}

cd ./TypeTreeDumps/InfoJson
vers=($(ls *.json | sort -t. -k1,1n -k2,2n -k3,3n))
cd ../..
echo Generating assemblies for ${#vers[@]} Unity versions
for ((i=0; i<${#vers[@]}; i++)); 
do
    generate ${vers[i]}
done