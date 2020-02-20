# TWASM
Theoistic WebAssembly is a .NET tool to easily create WebAssemblies with .NET

# Gettings Started
Install : dotnet tool install --global twasm

Create New Project : twasm create -name Example

Compile : twasm compile

this will install, create a new default template project and compile the project in the the bin/twasm/ folder,
additionally it will create a bin/twasm/publish folder which is the final output.

twasm also starts a local http after you compile which is http://localhost:8080/

When you create a new project there are 5 files created

index.html, which is the normal entry point for the http server,
app.js, which is an example javascript file which interacts with the .NET runtime,
ScriptAccess.cs, which is a example C# .NET source to guide you in creating your own .NET extensions
Example.twasm, which is the project file, which contains the source definitions, dependencies, content/assets and expose classes.

the Expose classes are classes which you can expose to be consumed in JavaScript.
take a look at the Example.twasm and ScriptAccess.cs and app.js to get a better feel of how they are connected.
