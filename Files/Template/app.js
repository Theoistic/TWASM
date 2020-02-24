
document.addEventListener("TWASMReady", function (e) {

    // this will be called once .NET has loaded.
    var elem = document.querySelector('.container h1').innerHTML = ScriptAccess.Hello("TWASM Developer");

});