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


[<EntryPoint>]
let main argv =
    printfn "Running simulations"
    let runs = 200
    let setup = {
            GameParameters = {
                AgentCount = 100
                PortionOfRed = 50
                PayoffMatrix = PayoffMatrixType.FromRewardAndCost (10.0, 20.0)
            }
            SimulationFrames = [
                {
                    SimulationFrame.RoundCount = 100
                    StageName = "Stage 2"
                    StrategyInitFn = SimulationStages.stage2Game_withNashAdjustedFirstRound
                    MayUseColor = true
                    SetPayoffForStage = id
                }
            ]
    }
    let results = seq { for i in 1 .. runs do yield runSimulation setup }
    // printfn "result %O" results
    let states =
        results
        |> Seq.map (fun r -> {
            State.Setup = setup
            GameState = r
            ViewState = ShowResults 100
            PlayAnimation = false
        })
        |> List.ofSeq
        
    let stats = simulationStatsTable (Seq.last states)
    let firstSeparations = states |> Seq.map (fun s -> s.GameState.ResolvedRounds.FirstSeparationOfColorsRound)
    let avgSeparationWhenOccured = firstSeparations
                                   |> Seq.filter (fun v -> v.IsSome)
                                   |> Seq.map (fun v -> (float) v.Value)
                                   |> Seq.average
    let chanceOfSeparation =
        (firstSeparations
        |> Seq.filter (fun v -> v.IsSome)
        |> Seq.length
        |> float) / (float) runs
        
    printfn "stats %O \n firstSeparation %f (chance %f)" stats avgSeparationWhenOccured chanceOfSeparation
    0 // return an integer exit code
