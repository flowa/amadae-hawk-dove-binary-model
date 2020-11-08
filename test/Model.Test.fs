module Model.Test

open Model
open Fable.Jester

Jest.describe("Color", fun () ->
    Jest.test("GenerateList", fun () ->
        let specs = [(Red, 2); (Blue, 3)]
        let colorList = Color.GenerateList specs
        Jest.expect(colorList).toHaveLength(5);
        Jest.expect(colorList.[0]).toEqual(Red)
        Jest.expect(colorList.[4]).toEqual(Blue)
    )
)

Jest.describe("Strategy", fun () ->
    Jest.test("GenerateList", fun () ->
        let specs = [(Hawk, 2); (Dove, 3)]
        let colorList = Strategy.GenerateList specs
        Jest.expect(colorList).toHaveLength(5);
        Jest.expect(colorList.[0]).toEqual(Hawk)
        Jest.expect(colorList.[1]).toEqual(Hawk)
        Jest.expect(colorList.[2]).toEqual(Dove)
        Jest.expect(colorList.[3]).toEqual(Dove)
        Jest.expect(colorList.[4]).toEqual(Dove)
    )
)