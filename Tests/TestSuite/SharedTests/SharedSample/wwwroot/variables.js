class Foo {
    constructor() {
        console.log(`FOO CTOR`)
    }

    hello() {
        console.log(`FOO HELLO: ${this}`)
    }
}

class MyError extends Error {
    constructor() {
        super(`MY ERROR`)
    }

    get foo() {
        return 999
    }
}

// @@BEGIN-FUNCTION: jsVariables
function jsVariables() {
    var myError = new MyError ()
    var foo = new Foo() // @@BREAKPOINT: JsVariables
    console.log(myError)
    foo.hello()
}
// @@END-FUNCTION

// @@BEGIN-FUNCTION: jsException
function jsException() {
    var myError = new MyError ()
    console.log(myError) // @@BREAKPOINT: JsException
    throw myError
}
// @@END-FUNCTION
