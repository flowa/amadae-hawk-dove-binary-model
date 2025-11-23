module Helpers.Test

open Helpers
open Fable.Mocha

let tests = testList "Helpers test" [
    testCase "toPairs" <| fun () ->
        let expected = [|(1, 2); (3, 4)|]
        let actual = ArrayHelpers.toPairs [|1; 2; 3; 4|]

        Expect.equal actual expected "toPairs should convert array to pairs"

    testCase "suffle (this may faile in unlikely cases)" <| fun () ->
        let input = Array.init 1000 id;

        let result = ArrayHelpers.shuffle input

        Expect.equal input.Length result.Length "shuffled array should have same length"
        Expect.notEqual input result "shuffled array should be different from original"
]

Mocha.runTests tests |> ignore