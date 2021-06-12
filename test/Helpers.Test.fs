module Helpers.Test

open Helpers
open Fable.Jester

Jest.describe("Helpers test", fun () ->
    Jest.test("toPairs", fun () ->
        let expected = [|(1, 2); (3, 4)|]
        let actual = ArrayHelpers.toPairs [|1; 2; 3; 4|]

        Jest.expect(actual)
            .toEqual(expected)
    )

    Jest.test("shuffle (this may fail in unlikely cases)", fun () ->
        let input = Array.init 1000 id;

        let result = ArrayHelpers.shuffle input

        Jest.expect(input)
            .toHaveLength(input.Length)
        Jest.expect(input)
            .not.toEqual(result);

    )
)