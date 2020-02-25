# TWASM

Theoistic WebAssembly is a .NET tool to easily create WebAssemblies with .NET

It was created to be easily implemented in existing solutions, that requires more raw power on the clientside than what JavaScript can handle. without the heavy pipepline of blazor.
When one just needs an option to compile some source code down that one already has on the server side that could be run on the clientside to lesser the server load. without implementing an entire server side framework.

Install:
``` bash
dotnet tool install --global twasm
```
Create Project:
``` bash
twasm create -name Example
```
Compile:
``` bash
twasm compile
```
Optionally, you can also compile a csproj directly
if you cd. into a C# .NETStandard Library directory and just call:
``` bash
twasm compile
```

it will convert the local *.csproj to a *.twasm project file and compile it. Additionally also starts a local http after you compile which is http://localhost:8080/

When you create a new project there are 5 files created

index.html, which is the normal entry point for the http server,
app.js, which is an example javascript file which interacts with the .NET runtime,
ScriptAccess.cs, which is a example C# .NET source to guide you in creating your own .NET extensions
Example.twasm, which is the project file, which contains the source definitions, dependencies, content/assets and expose classes.

the Expose classes are classes which you can expose to be consumed in JavaScript.
take a look at the Example.twasm and ScriptAccess.cs and app.js to get a better feel of how they are connected.

# Exposed Classes
Currently, you have the option to list static classes in the twasm project file under "Expose", this will trigger a
build procedure when compiling, to parse the C# classes and explose its methods to be exposed within the twasm.js startup initalization.
Classes are named as they are declared within C#, Example below contains a ScriptAccess.cs class which has a simple Hello method.
when compiled with twasm, you can call it directly from javascript.
``` csharp
public static class ScriptAccess
{
    public static string Hello(string str)
    {
        return $"Hello {str} from TWASM at {DateTime.Now}";
    }
}
```
Within JavaScript you can then call:
``` javascript
document.addEventListener("TWASMReady", function (e) {

    // this will be called once .NET has loaded.
    var elem = document.querySelector('.container h1').innerHTML = ScriptAccess.Hello("TWASM Developer");

});
```
The TWASMReady is triggered once all required system files have been loaded into the local dotnet runtime.

# Hosted Testing Environment
if you need to test out your previous compilation. you can call:
``` bash
twasm serve
```
Which will start a local http listener, which will start up chrome to http://localhost:8080/
where you can use the developer control to test out the JavaScript endpoints.
