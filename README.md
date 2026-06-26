# .NET Worker Asynchrony

An example .NET application that demonstrates how worker services can interact with file systems asynchronously.

## What it does

From the specified input directory, it receives a file that contains a list of pairs of numbers. It converts this
list into an XML document, including the result of multiplying each pair.

The purpose is to watch the directory, and avoid another thread attempting to access the same input file while 
it's being processed.

## Generating files

The JavaScript file [scripts/index.js](./scripots/index.js) is used to generate the input files of paired numbers. You need 
at least Node version 24 to run the script. 

A configuration file [scripts/config.json](./scripts/config.json) has these settings:

Settings:

* `maxFilesToGenerate`: The number of files to generate 
* `minPairs`: The minimum number of pairs of numbers to generate for each file
* `maxPairs`: The maximum number of pairs of numbers to generate for each file
* `maxInteger`: The highest integer to randomly generate to form a pair
* `maxDelay`: The maximum delay, in milliseconds, between file generation
* `fileExtension`: The extension of the generated files

Directories:

* `input`: The target directory for the generated files


