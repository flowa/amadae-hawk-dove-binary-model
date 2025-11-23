module App.Test
open Model
open Fable.Mocha

let tests = testList "can run basic tests" [
    testCase "running a test" <| fun () ->
        Expect.equal (1+1) 2 "1+1 should equal 2"
]

Mocha.runTests tests |> ignore