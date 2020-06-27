module App
open Model
open Fable.Jester

Jest.describe("can run basic tests", fun () ->
    Jest.test("running a test", fun () ->
        Jest.expect(1+1).toEqual(2)
    )
)