module Simulation.Test

open Simulation
open Model
open Fable.Jester
module TestData  =
    module Agents =
        let Blue1 = AgentWithNoGames {Color = Blue;  Id = 0;  }
        let Red1 = AgentWithNoGames {Color = Red;   Id = 1;  }
        let Blue2 = AgentWithNoGames {Color = Blue;  Id = 2;  }
        let Red2 = AgentWithNoGames {Color = Red;   Id = 3;  }
        let Blue3 = AgentWithNoGames {Color = Blue;  Id = 4;  }
        let Red3 = AgentWithNoGames {Color = Red;   Id = 5;  }

    module Challenges =
        let round1 =
            [||]

type SimpleTestCase<'input, 'output> =
    {
        Name: string
        FunctionToTest: 'input -> 'output
        Input: 'input
        ExpectedOutput: 'output
    }

type WithInitizerTestCase<'input, 'fnInput, 'output> =
    {
        Name: string
        FunctionName: string
        FunctionToTest: 'fnInput -> 'output
        Input: 'input
        MapInput: 'input -> 'fnInput
        ExpectedOutput: 'output
    }


Jest.describe("Test game modes: nashMixedStrategyEquilibriumGameFromPayoffParameters", fun () ->
    let rand = System.Random()
    let info: GameInformation =
        {
            Agent = TestData.Agents.Blue1
            OpponentColor = Blue
            History = Rounds ([||])
            PayoffMatrix = FromRewardAndCost (10.0, 20.0)
            RandomNumber = rand.NextDouble ()
        }
    [
       {
           Name = "Hawk 1: It should yield HAWK when V/C = 0.0"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.0 }
           ExpectedOutput = Hawk
       }
       {
           Name = "Hawk 2: It should yield HAWK when V/C < RandomNumber"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.49 }
           ExpectedOutput = Hawk
       }
       {
           Name = "Hawk 3: It should yield HAWK when V/C = 1"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.9999999; PayoffMatrix = (FromRewardAndCost (1.0, 1.0)) }
           ExpectedOutput = Hawk
       }
       {
           Name = "Hawk 4: It should yield HAWK when C = 0"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.999999; PayoffMatrix = (FromRewardAndCost (1.0, 0.0)) }
           ExpectedOutput = Hawk
       }
       {
           Name = "Dove 1: It should yield HAWK when V/C = RandomNumber"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.5 }
           ExpectedOutput = Dove
       }
       {
           Name = "Dove 2: It should yield HAWK when V/C > RandomNumber"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.51 }
           ExpectedOutput = Dove
       }
       {
           Name = "Dove 3: It should yield HAWK when V/C = 0"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.0; PayoffMatrix = (FromRewardAndCost (0.0, 1.0)) }
           ExpectedOutput = Dove
       }
    ]
    |> List.iter (fun testData ->
        Jest.test(testData.Name, (fun () ->
            let actual = testData.FunctionToTest testData.Input
            Jest.expect(actual).toEqual(testData.ExpectedOutput)
        )))
)


Jest.describe("Test game modes: randomChoiceGame", fun () ->
    let rand = System.Random()
    let info: GameInformation =
        {
            Agent = TestData.Agents.Blue1
            OpponentColor = Blue
            History = Rounds ([||])
            PayoffMatrix = FromRewardAndCost (10.0, 20.0)
            RandomNumber = rand.NextDouble ()
        }
    [
        {
           Name = "It should yield HAWK when RandomNumber = 0.0"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.0 }
           ExpectedOutput = Hawk
        }
        {
           Name = "It should yield HAWK when RandomNumber < 0.5"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.49999 }
           ExpectedOutput = Hawk
        }
        {
           Name = "It should yield Dove when RandomNumber = 0.5"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.5 }
           ExpectedOutput = Dove
        }
        {
           Name = "It should yield Dove when RandomNumber > 0.5"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.999999 }
           ExpectedOutput = Dove
        }
    ]
    |> List.iteri (fun index testData ->
        let name = sprintf "Case %i: %s" (index + 1) testData.Name
        Jest.test(name,
            (fun () ->
                let actual = testData.FunctionToTest testData.Input
                Jest.expect(actual).toEqual(testData.ExpectedOutput)
            )))
)

Jest.describe("Test game modes: keepSameStrategy", fun () ->
    let agent1 = TestData.Agents.Blue1.UpdateGameInfo(10.0, Hawk,  DifferentColor)
    let agent2 = TestData.Agents.Red1.UpdateGameInfo(0.0, Dove,  DifferentColor)
    let agent3 = TestData.Agents.Blue1
    let agent4 = TestData.Agents.Red1

    let matrix = FromRewardAndCost (10.0, 40.0)
    let history = Rounds [| GameRound.Empty.AppendWith(agent1, agent2) |]
    let agent1Info, agent2Info =
        GameInformation.InitGameInformationForAgents matrix history agent1 agent2

    let agent3Info, agent4Info =
        GameInformation.InitGameInformationForAgents matrix history agent3 agent4

    [
        {
           Name = "It should yield HAWK because agent1 played Hawk on previous round"
           FunctionToTest = GameMode.keepSameStrategy
           Input = agent1Info
           ExpectedOutput = Hawk
        }
        {
           Name = "It should yield Dove because agent2 played Dove on previous round"
           FunctionToTest = GameMode.keepSameStrategy
           Input = agent2Info
           ExpectedOutput = Dove
        }
        {
           Name = "It should use NMSE -strategy when agent3 have not played earlier, hence when RandomNumber is 0.25 DOVE should be played because 10 / 40 = 0.25"
           FunctionToTest = GameMode.keepSameStrategy
           Input = { agent3Info with RandomNumber = 0.25 }
           ExpectedOutput = Dove
        }
        {
           Name = "It should use NMSE -strategy when agent4 have not played earlier, hence when RandomNumber is 0.24 HAWK should be played because 10 / 40 = 0.25"
           FunctionToTest = GameMode.keepSameStrategy
           Input = { agent4Info with RandomNumber = 0.24 }
           ExpectedOutput = Hawk
        }
    ]
    |> List.iteri
        (fun index testData ->
            let name = sprintf "Case %i: %s" (index + 1) testData.Name
            Jest.test(name,
                (fun () ->
                    let actual = testData.FunctionToTest testData.Input
                    Jest.expect(actual).toEqual(testData.ExpectedOutput)
                )))
)

type HighestEuTestInput =
    {
        History: GameHistory
        Agent: Agent
        Other: Agent
        RandomNumber: float
    }

Jest.describe("Test game modes: highestEuOnDifferentColorGameUsingAllEncounters and highestEuOnDifferentColorGame", fun () ->
    let blueHawk = TestData.Agents.Blue1.UpdateGameInfo(0.0, Hawk,  DifferentColor)
    let blueDove = TestData.Agents.Blue2.UpdateGameInfo(0.0, Dove,  DifferentColor)
    let redHawk  = TestData.Agents.Red1.UpdateGameInfo(0.0, Hawk,  DifferentColor)
    let redDove  = TestData.Agents.Red2.UpdateGameInfo(0.0, Dove,  DifferentColor)
    let sameColorBlueHawk  = TestData.Agents.Blue1.UpdateGameInfo(0.0, Hawk,  SameColor)


    let matrix = FromRewardAndCost (10.0, 20.0)
    let ``history Red 1H 1D, Blue 2H 0D`` = Rounds [|
        GameRound.Empty
            .AppendWith(blueHawk, redDove)
            .AppendWith(blueHawk, redHawk)
         |]
    let ``history Red 3H 1D, Blue 1H 3D`` = Rounds [|
        GameRound.Empty
            .AppendWith(blueDove, redDove)
            .AppendWith(blueDove, redHawk)
            .AppendWith(blueDove, redHawk)
            .AppendWith(blueHawk, redHawk)
         |]

    let ``history Red 3H 1D, Blue 1H 3D with same color Blue 4H 0D`` = Rounds [|
        GameRound.Empty
            .AppendWith(blueDove, redDove)
            .AppendWith(blueDove, redHawk)
            .AppendWith(blueDove, redHawk)
            .AppendWith(blueHawk, redHawk)
            .AppendWith(sameColorBlueHawk, sameColorBlueHawk)
            .AppendWith(sameColorBlueHawk, sameColorBlueHawk)
         |]

    let mapInput (testParams: HighestEuTestInput): GameInformation =
        let (info, _) = GameInformation.InitGameInformationForAgents matrix testParams.History testParams.Agent testParams.Other
        { info with RandomNumber = testParams.RandomNumber }

    let generateExplainingText hawkN doveN opponentColor =
        let total = doveN + hawkN
        let hawkP = (float hawkN) / (float total)
        let euHawk = hawkP * -5.0 + (1.0 - hawkP) * 10.0
        let euDove = hawkP *  0.0 + (1.0 - hawkP) * 5.0
        sprintf "For opposing color %O, there was %i Hawks and %i Doves, hence EU Hawk is %f and EU Dove is %f" opponentColor hawkN doveN euHawk euDove

    let testCasesForBothFunctions fnName fnToTest = [
        {
           // opponent = Red
           // euHawk = 0.5 * -5 + 0.5 * 10 = 2.5
           // euDove = 0.5 * 0  + 0.5 * 5  = 2.5
           Name = "It should choose randomly when euHawk = euDove, because RandomNumber was 0.4 it should return Hawk: " + (generateExplainingText 1 1 Red)
           FunctionToTest = fnToTest
           Input = {
               History = ``history Red 1H 1D, Blue 2H 0D``
               Agent = blueHawk
               Other = redDove
               RandomNumber = 0.4 // In randomStrategy 0.4 -> Hawk
           }
           ExpectedOutput = Hawk
           FunctionName = fnName
           MapInput = mapInput
        }
        {
           // opponent = Red
           // euHawk = 0.5 * -5 + 0.5 * 10 = 2.5
           // euDove = 0.5 * 0  + 0.5 * 5  = 2.5
           Name = "It should choose randomly when euHawk = euDove, because RandomNumber was 0.6 it should return Dove: " + (generateExplainingText 1 1 Red)
           FunctionToTest = fnToTest
           Input = {
               History = ``history Red 1H 1D, Blue 2H 0D``
               Agent = blueHawk
               Other = redDove
               RandomNumber = 0.6 // In randomStrategy 0.6 -> Dove
           }
           ExpectedOutput = Dove
           FunctionName = fnName
           MapInput = mapInput
        }
        {
           // opponent = Blue
           // euHawk = 1 * -5 + 0 * 10 = -5
           // euDove = 1 * 0  + 0 * 5  = 0
           Name = "It should return Dove because euHawk < euDove: " + (generateExplainingText 2 0 Blue)
           FunctionToTest = fnToTest
           Input = {
               History = ``history Red 1H 1D, Blue 2H 0D``
               Agent = redDove
               Other = blueHawk
               RandomNumber = 0.0 // If used => Hawk; should not use
           }
           ExpectedOutput = Dove
           FunctionName = fnName
           MapInput = mapInput
        }
        {
           // opponent = Red
           // euHawk = 0.75 * -5 + 0.25 * 10 = -1.25
           // euDove = 0.75 * 0  + 0.25 * 5  = 1.25
           Name = "It should return Dove because euHawk < euDove: " + (generateExplainingText 3 1 Red)
           FunctionToTest = fnToTest
           Input = {
               History = ``history Red 3H 1D, Blue 1H 3D``
               Agent = blueHawk
               Other = redDove
               RandomNumber = 0.0 // If used => Hawk; should not use
           }
           ExpectedOutput = Dove
           FunctionName = fnName
           MapInput = mapInput
        }
        {
           // opponent = Blue
           // euHawk = 0.25 * -5 + 0.75 * 10 = 6.25
           // euDove = 0.25 * 0  + 0.75 * 5  = 3.75
           Name = "It should return Hawk because euHawk > euDove: " + (generateExplainingText 1 3 Blue)
           FunctionToTest = fnToTest
           Input = {
               History = ``history Red 3H 1D, Blue 1H 3D``
               Agent = redDove
               Other = blueHawk
               RandomNumber = 0.0 // If used => Hawk; should not use
           }
           ExpectedOutput = Hawk
           FunctionName = fnName
           MapInput = mapInput
        }
    ]

    let alsoSameColorEncountersTestCases = [
        {
           // opponent = Blue
           // euHawk = 0.25 * -5 + 0.75 * 10 = 6.25
           // euDove = 0.25 * 0  + 0.75 * 5  = 3.75
           Name = "It should return Hawk because euHawk > euDove: " + (generateExplainingText 1 3 Blue)
           FunctionToTest = GameMode.highestEuOnDifferentColorGameUsingOnlyDifferentColorStats
           Input = {
               History = ``history Red 3H 1D, Blue 1H 3D with same color Blue 4H 0D``
               Agent = redDove
               Other = blueHawk
               RandomNumber = 0.0 // If used => Hawk; should not use
           }
           ExpectedOutput = Hawk
           FunctionName = "Different color encounter only"
           MapInput = mapInput
        }
        {
           // opponent = Red
           // euHawk = 0.75 * -5 + 0.25 * 10 = -1.25
           // euDove = 0.75 * 0  + 0.25 * 5  = 1.25
           Name = "It should return Dove because euHawk < euDove: " + (generateExplainingText 3 1 Red)
           FunctionToTest = GameMode.highestEuOnDifferentColorGameUsingOnlyDifferentColorStats
           Input = {
               History = ``history Red 3H 1D, Blue 1H 3D with same color Blue 4H 0D``
               Agent = blueHawk
               Other = redDove
               RandomNumber = 0.0 // If used => Hawk; should not use
           }
           ExpectedOutput = Dove
           FunctionName = "Different color encounter only"
           MapInput = mapInput
        }
        {
           // opponent = Blue (total 8 7 )
           // euHawk = 0.625 * -5 + 0.375 * 10 = 0.625
           // euDove = 0.625 * 0  + 0.375 * 5  = 1.875
           Name = "It should return Dove because euHawk < euDove: " + (generateExplainingText 5 3 Blue)
           FunctionToTest = GameMode.highestEuOnDifferentColorGame
           Input = {
               History = ``history Red 3H 1D, Blue 1H 3D with same color Blue 4H 0D``
               Agent = redDove
               Other = blueHawk
               RandomNumber = 0.0 // If used => Hawk; should not use
           }
           ExpectedOutput = Dove
           FunctionName = "All encounters"
           MapInput = mapInput
        }
    ]

    List.concat [
        testCasesForBothFunctions "All encounters" GameMode.highestEuOnDifferentColorGame
        testCasesForBothFunctions "Different color encounter only" GameMode.highestEuOnDifferentColorGameUsingOnlyDifferentColorStats
        alsoSameColorEncountersTestCases
    ]
    |> List.iteri
        (fun index testData ->
            let name = sprintf "Case %i (%s): %s" (index + 1) testData.FunctionName testData.Name
            Jest.test(name,
                (fun () ->
                    let fnInput = testData.MapInput testData.Input
                    let actual = testData.FunctionToTest fnInput
                    Jest.expect(actual).toEqual(testData.ExpectedOutput)
                )))
)