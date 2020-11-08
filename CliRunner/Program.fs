// Learn more about F# at http://fsharp.org

open System
open Model
open Simulation
open Statistics.ModelExtensions
open Statistics.RoundStats

let runSimulation (setup: GameSetup) =
    let initialGameState = setup.ToInitialGameState()
    let initialAgents = setup.GenerateAgents()
    let afterSimulation = initialGameState.SimulateRounds initialAgents
    afterSimulation


type StatsTableRow = {
    EncounterType: string
    Red: StrategyStats
    Blue: StrategyStats
    All: StrategyStats
}

let simulationStatsTable (model: State): StatsTableRow list =
    let challenges = model.CurrentRoundChallenges
    [
        {
            StatsTableRow.EncounterType = "Different color"
            Red = (challenges.StrategyStatsFor (DifferentColor, Red))
            Blue = (challenges.StrategyStatsFor (DifferentColor, Blue))
            All = (challenges.StrategyStatsFor DifferentColor)
        }
        {
            StatsTableRow.EncounterType = "SameColor color"
            Red = (challenges.StrategyStatsFor (SameColor, Red))
            Blue = (challenges.StrategyStatsFor (SameColor, Blue))
            All = (challenges.StrategyStatsFor SameColor)
        }
        {
            StatsTableRow.EncounterType = "All"
            Red = (challenges.StrategyStatsFor Red)
            Blue = (challenges.StrategyStatsFor Blue)
            All = (challenges.StrategyStats ())
        }
    ]

let initializationRounds = 1
let secondStageRounds = 150

let generateSetup (agentCount: int) (portionOfRed: int) (expectedHawkPortion: float) =
    let ``Reward (V)`` = 10.0 
    let ``Cost (C)`` =
        match expectedHawkPortion with
        | p when p >= 1.0 || p <= 0.0 -> raise (new ArgumentException("expectedHawkPortion must be in range ]0,1["))
        | _ -> ``Reward (V)`` / expectedHawkPortion
    {        
        GameParameters = {
            AgentCount = agentCount
            PortionOfRed = portionOfRed
            PayoffMatrix = PayoffMatrixType.FromRewardAndCost (``Reward (V)``, ``Cost (C)``)
        }
        
        SimulationFrames = [
            {
                SimulationFrame.RoundCount = initializationRounds
                StageName = "Stage 1 - Ideal Nash Distribution"
                StrategyInitFn = SimulationStages.stage1Game_withIdealNMSEDistribution
                MayUseColor = true
                SetPayoffForStage = id
            }
            {
                SimulationFrame.RoundCount = secondStageRounds
                StageName = "Stage 2"
                StrategyInitFn = SimulationStages.stage2Game_v5_withFullIndividualHistory
                MayUseColor = true
                SetPayoffForStage = id
            }
        ]
    }

[<EntryPoint>]
let main argv =
    printfn "Running simulations"
    let runs = 50
    let initializationRounds = 1
    let setup = generateSetup 500 50 0.50
    printfn "Setup = %O" setup
    let results = seq { for i in 1 .. runs do yield runSimulation setup }
    // printfn "result %O" results
    let states =
        results
        |> Seq.map (fun r -> {
            State.Setup = setup
            GameState = r
            ViewState = ShowResults (initializationRounds + secondStageRounds)
            PlayAnimation = false
        })
        |> List.ofSeq
        
    let firstSeparations = states |> List.map (fun s -> s.GameState.ResolvedRounds.FirstSeparationOfColorsRound)
    let dominance = states |> List.map (fun s -> s.GameState.ResolvedRounds.DominationColorAfterSeparation)
    
    let roundNumberOfSeparationWhenOccured =
        (firstSeparations
        |> Seq.filter (fun v -> v.IsSome)
        |> Seq.map (fun v -> (float) v.Value - (float) initializationRounds)
        |> List.ofSeq)

    let chanceOfSeparation =
        (float) roundNumberOfSeparationWhenOccured.Length / (float) runs
        
    let avgSeparationWhenOccured =
        roundNumberOfSeparationWhenOccured
       |> Seq.average
    
    let distributionOfSeparation =
        [
            roundNumberOfSeparationWhenOccured |> Seq.min
            roundNumberOfSeparationWhenOccured |> Seq.max
        ]
    let chanceOfRedDominanceChance =
        let redDominated = dominance |> List.filter (fun d -> d = Some Red) |> List.length |> float
        redDominated / (float) runs
        
        
    printfn "firstSeparation %f %O (chance %f); chance of red dominance %f"
        avgSeparationWhenOccured
        distributionOfSeparation
        chanceOfSeparation
        chanceOfRedDominanceChance
    0 // return an integer exit code
