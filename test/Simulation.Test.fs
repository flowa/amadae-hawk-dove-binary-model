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
    module GameInfo =
        let getDefaultTestInfo () =
            let cache = new AgentViewCache()
            let matrix = FromRewardAndCost(10.0, 20.0)
            let history = Rounds ([||])
            let info, _ =
                GameInformation.InitGameInformationForAgents
                    cache
                    matrix
                    history
                    Agents.Blue1
                    Agents.Red1
            info

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
    let info: GameInformation = TestData.GameInfo.getDefaultTestInfo()

    [
       {
           Name = "It should return Hawk when RandomNumber = 0.0 and V/C < 0"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.0 }
           ExpectedOutput = Hawk
       }
       {
           Name = "It should return Hawk when RandomNumber < V/C"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.49 }
           ExpectedOutput = Hawk
       }
       {
           Name = "It should return Hawk when V/C = 1"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.9999999; PayoffMatrix = (FromRewardAndCost (1.0, 1.0)) }
           ExpectedOutput = Hawk
       }
       {
           Name = "It should return Hawk when C = 0"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.999999; PayoffMatrix = (FromRewardAndCost (1.0, 0.0)) }
           ExpectedOutput = Hawk
       }
       {
           Name = "It should return Hawk  when V/C = RandomNumber"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.5 }
           ExpectedOutput = Dove
       }
       {
           Name = "It should return Dove when RandomNumber > V/C"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.51 }
           ExpectedOutput = Dove
       }
       {
           Name = "It should return Dove when V/C = 0"
           FunctionToTest = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
           Input = { info with RandomNumber = 0.0; PayoffMatrix = (FromRewardAndCost (0.0, 1.0)) }
           ExpectedOutput = Dove
       }
    ]
    |> List.iteri (fun index testData ->
        let name = sprintf "Case %i: %s" (index + 1) testData.Name
        Jest.test(name, (fun () ->
            let actual = testData.FunctionToTest testData.Input
            Jest.expect(actual).toEqual(testData.ExpectedOutput)
        )))
)


Jest.describe("Test game modes: randomChoiceGame", fun () ->
    let info: GameInformation = TestData.GameInfo.getDefaultTestInfo()

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
        let cache = new AgentViewCache()
        GameInformation.InitGameInformationForAgents cache matrix history agent1 agent2

    let agent3Info, agent4Info =
        let cache = new AgentViewCache()
        GameInformation.InitGameInformationForAgents cache matrix history agent3 agent4

    [
        {
           Name = "It should return Hawk because agent1 played Hawk on previous round"
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
           Name = "It should use NMSE -strategy when agent3 have not played earlier, and thereby when RandomNumber is 0.25 Dove should be played because 10 / 40 = 0.25"
           FunctionToTest = GameMode.keepSameStrategy
           Input = { agent3Info with RandomNumber = 0.25 }
           ExpectedOutput = Dove
        }
        {
           Name = "It should use NMSE -strategy when agent4 have not played earlier, and thereby when RandomNumber is 0.24 dove should be played because 10 / 40 = 0.25"
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
                ))))

type HighestEuTestInput =
    {
        History: GameHistory
        Agent: Agent
        Other: Agent
        RandomNumber: float
    }

Jest.describe(
    sprintf "Test game modes: highestEuOnDifferentColorGameUsingAllEncounters and highestEuOnDifferentColorGame \n\t - PayoffMatrix for all tests is: %A" ((FromRewardAndCost (10.0, 20.0)).ToMatrix()),
    fun () ->
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
            let cache = new AgentViewCache()
            let (info, _) = GameInformation.InitGameInformationForAgents cache matrix testParams.History testParams.Agent testParams.Other
            { info with RandomNumber = testParams.RandomNumber }

        let generateExplainingText hawkN doveN opponentColor =
            let total = doveN + hawkN
            let hawkP = (float hawkN) / (float total)
            let evHawk = hawkP * -5.0 + (1.0 - hawkP) * 10.0
            let evDove = hawkP *  0.0 + (1.0 - hawkP) * 5.0
            (sprintf "\n\t - For opposing color %O, there was %i Hawks and %i Doves" opponentColor hawkN doveN
                + (sprintf "\n\t - Probablity for Hawk was %f " hawkP)
                + (sprintf "\n\t - EV for playing Hawk is %f \t (= %f * -5.0 + (1.0 - %f) * 10.0) " evHawk hawkP hawkP)
                + (sprintf "\n\t - EV for playing Dove is %f \t (= %f *  0.0 + (1.0 - %f) * 5.0)" evDove hawkP hawkP))

        let testCasesForBothFunctions fnName fnToTest = [
            {
               // opponent = Red
               // evHawk = 0.5 * -5 + 0.5 * 10 = 2.5
               // evDove = 0.5 * 0  + 0.5 * 5  = 2.5
               Name = "It should choose randomly when evHawk = evDove, because RandomNumber was 0.4 it should return Hawk: " + (generateExplainingText 1 1 Red)
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
               // evHawk = 0.5 * -5 + 0.5 * 10 = 2.5
               // evDove = 0.5 * 0  + 0.5 * 5  = 2.5
               Name = "It should choose randomly when evHawk = evDove, because RandomNumber was 0.6 it should return Dove: " + (generateExplainingText 1 1 Red)
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
               // evHawk = 1 * -5 + 0 * 10 = -5
               // evDove = 1 * 0  + 0 * 5  = 0
               Name = "It should return Dove because evHawk < evDove: " + (generateExplainingText 2 0 Blue)
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
               // evHawk = 0.75 * -5 + 0.25 * 10 = -1.25
               // evDove = 0.75 * 0  + 0.25 * 5  = 1.25
               Name = "It should return Dove because evHawk < evDove: " + (generateExplainingText 3 1 Red)
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
               // evHawk = 0.25 * -5 + 0.75 * 10 = 6.25
               // evDove = 0.25 * 0  + 0.75 * 5  = 3.75
               Name = "It should return Hawk because evHawk > evDove: " + (generateExplainingText 1 3 Blue)
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
               // opponent = Blue, Only different color encounters
               // evHawk = 0.25 * -5 + 0.75 * 10 = 6.25
               // evDove = 0.25 * 0  + 0.75 * 5  = 3.75
               Name = "It should return Hawk because evHawk > evDove: " + (generateExplainingText 1 3 Blue)
               FunctionToTest = GameMode.highestExpectedValueOnDifferentColorGameUsingOnlyDifferentColorStats
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
               // opponent = Red, Only different color encounters
               // evHawk = 0.75 * -5 + 0.25 * 10 = -1.25
               // evDove = 0.75 * 0  + 0.25 * 5  = 1.25
               Name = "It should return Dove because evHawk < evDove: " + (generateExplainingText 3 1 Red)
               FunctionToTest = GameMode.highestExpectedValueOnDifferentColorGameUsingOnlyDifferentColorStats
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
               // opponent = Blue
               // evHawk = 0.625 * -5 + 0.375 * 10 = 0.625
               // evDove = 0.625 * 0  + 0.375 * 5  = 1.875
               Name = "It should return Dove because evHawk < evDove: " + (generateExplainingText 5 3 Blue)
               FunctionToTest = GameMode.highestExpectedValueOnDifferentColorGame
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
            testCasesForBothFunctions "All encounters" GameMode.highestExpectedValueOnDifferentColorGame
            testCasesForBothFunctions "Different color encounter only" GameMode.highestExpectedValueOnDifferentColorGameUsingOnlyDifferentColorStats
            alsoSameColorEncountersTestCases
//            testCasesForBothFunctions "Stage 2 Game" SimulationStages.stage2Game { GameParameters.AgentCount = 4; PortionOfRed = 50; PayoffMatrix =   }
        ]
        |> List.iteri
            (fun index testData ->
                let name = sprintf "Case %i (%s): %s" (index + 1) testData.FunctionName testData.Name
                Jest.test(name,
                    (fun () ->
                        let fnInput = testData.MapInput testData.Input
                        let actual = testData.FunctionToTest fnInput
                        Jest.expect(actual).toEqual(testData.ExpectedOutput)
                    ))))