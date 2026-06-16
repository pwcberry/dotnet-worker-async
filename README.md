# .NET Worker Asynchrony

An example .NET application that demonstrates how worker services can interact with file systems asynchronously.

## What it does

From the specified input directory, it receives a file that contains a list of pairs of numbers. It converts this
list into an XML document, including the result of multiplying each pair.

The purpose is to watch the directory, and avoid another thread attempting to access the same input file while 
it's being processed.

