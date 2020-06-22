class Foo {
    constructor() {
        console.log(`FOO CTOR`)
        this.number = 8888;
    }

    hello() {
        console.log(`FOO HELLO: ${this}`)
    }

    throwError() {
        var myError = new MyError()
        console.log(`THROWING HERE`) // @@BREAKPOINT: FooThrowError
        throw myError
    }

    get errorProperty() {
        var myError = new MyError()
        console.log(`THROWING HERE`) // @@BREAKPOINT: FooErrorProperty
        throw myError
    }
}

class MyError extends Error {
    constructor() {
        super(`MY ERROR`)
        this.hello = "World"
        this.foo = 999
    }
}

// @@BEGIN-FUNCTION: jsVariables
function jsVariables() {
    var myError = new MyError ()
    var foo = new Foo()
    console.log(myError) // @@BREAKPOINT: JsVariables
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
