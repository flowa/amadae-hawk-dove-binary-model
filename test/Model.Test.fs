module Model.Test

open Model
open Fable.Mocha

let colorTests = testList "Color" [
    testCase "GenerateList" <| fun () ->
        let specs = [(Red, 2); (Blue, 3)]
        let colorList = Color.GenerateList specs
        Expect.equal colorList.Length 5 "color list should have length 5"
        Expect.equal colorList.[0] Red "first element should be Red"
        Expect.equal colorList.[4] Blue "last element should be Blue"
]

let strategyTests = testList "Strategy" [
    testCase "GenerateList" <| fun () ->
        let specs = [(Hawk, 2); (Dove, 3)]
        let colorList = Strategy.GenerateList specs
        Expect.equal colorList.Length 5 "strategy list should have length 5"
        Expect.equal colorList.[0] Hawk "first element should be Hawk"
        Expect.equal colorList.[1] Hawk "second element should be Hawk"
        Expect.equal colorList.[2] Dove "third element should be Dove"
        Expect.equal colorList.[3] Dove "fourth element should be Dove"
        Expect.equal colorList.[4] Dove "fifth element should be Dove"
]

let tests = testList "Model tests" [
    colorTests
    strategyTests
]

Mocha.runTests tests |> ignore