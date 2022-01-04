#!/bin/bash

path_to_JsonFolder="./TypeTreeDumps/InfoJson"
path_from_JsonFolder="../.."

if [ $# != 0 ] && [ $# != 1 ]
then
    echo "incorrect number of arguments: $# instead of 0 or 1"
    exit 2
fi
if [ ${#1} = 0 ]
then
    echo "Argument has no length"
    exit 3
fi

cd $path_to_JsonFolder
vers=($(ls *.json | sort -t. -k1,1n -k2,2n -k3,3n))
cd $path_from_JsonFolder

for ((i=0; i<${#vers[@]}; i++)); 
do
    if [ $# != 0 ]
    then
        echo ${vers[i]}
    elif [ ${1:0:2} != "20" ]
    then
        if [ ${vers[i]:0:2} != "20" ]
        then
            echo ${vers[i]}
        fi
    elif [ ${vers[i]:0:4} = $1 ]
    then
        echo ${vers[i]}
    fi
done