#!/bin/bash

shopt -s globstar

java_files=( **/*.java )

if [ ${#java_files[@]} -eq 0 ] || [ ! -e "${java_files[0]}" ]; then
    echo "No .java files found."
    exit 0
fi

echo "Start compiling Java files..."

failed=0

for file in "${java_files[@]}"; do
    if [[ "$file" == *Java\ 8* ]]; then
        echo "Compiling (Java 8): $file"
        javac --release 8 "$file"
    else
        echo "Compiling (Default): $file"
        javac "$file"
    fi
    
    if [ $? -ne 0 ]; then
        echo "[-] Failed to compile: $file"
        failed=1
    fi
done

if [ $failed -eq 0 ]; then
    echo "All Java files compiled successfully!"
else
    echo "Compilation completed with errors. Please check the logs above."
    exit 1
fi