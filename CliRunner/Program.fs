// Learn more about F# at http://fsharp.org

open System
open FSharp.Collections.ParallelSeq
open FSharp.Control
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
type SimulationSingleRunSetup = {
    Runs: int
    AgentCount: int
    RedAgentPercentage: int
    ExpectedHawkPortion: float
}


let generateGameSetup (simulationSetup: SimulationSingleRunSetup)  =
    let ``Reward (V)`` = 10.0 
    let ``Cost (C)`` =
        match simulationSetup.ExpectedHawkPortion with
        | p when p >= 1.0 || p <= 0.0 -> raise (new ArgumentException("expectedHawkPortion must be in range ]0,1["))
        | _ -> ``Reward (V)`` / simulationSetup.ExpectedHawkPortion
    {        
        GameParameters = {
            AgentCount = simulationSetup.AgentCount
            PortionOfRed = simulationSetup.RedAgentPercentage
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
    
type SimulationRunResultStats =
    {
        AgentCount: int
        RedAgentPercentage: int
        HawkPortion: float
        PayoffReward: float
        PayoffCost: float
        AvgSeparationWhenOccured: float
        EarliestRoundSeparation: int
        LatestRoundOfSeparation: int
        RunWhereSeparationOccured: int
        ChanceOfSeparation: float
        RunsWhereRedDominated: int
        ChanceOfRedDominance: float
    }
    member this.saveToFile ()  =
        let json = System.Text.Json.JsonSerializer.Serialize(this)
        let path = (sprintf "output/output.A%i.R%i.NMSE%f.json"
                         this.AgentCount
                         this.RedAgentPercentage
                         this.HawkPortion)
        if (not (System.IO.Directory.Exists("output"))) then
             System.IO.Directory.CreateDirectory("output")
             |> ignore        
        System.IO.File.WriteAllText(path, json)


let runSimulationsWithOneSetup (simulationRunSetup: SimulationSingleRunSetup)  =
    let start = DateTime.Now
    printfn "Starting (R=%i; NMSE=%f) " simulationRunSetup.RedAgentPercentage simulationRunSetup.ExpectedHawkPortion
    let setup: GameSetup = generateGameSetup simulationRunSetup
    let results = seq { for i in 1 ..  simulationRunSetup.Runs do yield runSimulation setup }  |> Seq.toList
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
        (float) roundNumberOfSeparationWhenOccured.Length / (float) simulationRunSetup.Runs
        
    let avgSeparationWhenOccured =
       match roundNumberOfSeparationWhenOccured with
       | [] -> -1.0
       | _ -> roundNumberOfSeparationWhenOccured |> Seq.average
    
    let minSeparationWhenOccured =
       match roundNumberOfSeparationWhenOccured with
       | [] -> -1.0
       | _ -> roundNumberOfSeparationWhenOccured |> Seq.min
           
    let maxSeparationWhenOccured =
        match roundNumberOfSeparationWhenOccured with
        | [] -> -1.0
        | _ -> roundNumberOfSeparationWhenOccured |> Seq.max 
    
    let runWhereRedDominated = dominance |> List.filter (fun d -> d = Some Red) |> List.length 
    let chanceOfRedDominanceChance = (float) runWhereRedDominated / (float) simulationRunSetup.Runs
    let stats: SimulationRunResultStats = {
        SimulationRunResultStats.HawkPortion = simulationRunSetup.ExpectedHawkPortion
        RedAgentPercentage = simulationRunSetup.RedAgentPercentage
        AgentCount = simulationRunSetup.AgentCount
        PayoffReward = setup.PayoffMatrix.``Revard (V)``
        PayoffCost = setup.PayoffMatrix.``Cost (C)``
        AvgSeparationWhenOccured = avgSeparationWhenOccured
        EarliestRoundSeparation = minSeparationWhenOccured |> int
        LatestRoundOfSeparation = maxSeparationWhenOccured |> int
        RunWhereSeparationOccured = roundNumberOfSeparationWhenOccured.Length
        ChanceOfSeparation = chanceOfSeparation
        RunsWhereRedDominated = runWhereRedDominated
        ChanceOfRedDominance = chanceOfRedDominanceChance }
    
    stats.saveToFile()
    printfn "Completed (R=%i,NMSE=%f) took=%O" simulationRunSetup.RedAgentPercentage simulationRunSetup.ExpectedHawkPortion (DateTime.Now - start)
    stats
    

let runAllSimulationsForARedSetup runs agentCount redPercent =
    let setups = seq {
        for hawkPortion in (0.1)..(0.1)..(0.9) do
            yield {
                SimulationSingleRunSetup.Runs = runs
                AgentCount = agentCount
                RedAgentPercentage = redPercent
                ExpectedHawkPortion = hawkPortion }
    }
    
    setups
    |> Seq.map runSimulationsWithOneSetup
    |> List.ofSeq
    
[<EntryPoint>]
let main argv =
    let start = DateTime.Now
    printfn "Hawk-Dove simulation runner"
    printfn "==========================="
    
    let isIntRe = System.Text.RegularExpressions.Regex("^[0-9]+$")
    let isIntListRe = System.Text.RegularExpressions.Regex("^[0-9;]+$")
    match argv with
    | [|  runs; agents; redAgentSetup |]
        when isIntRe.IsMatch(runs) &&
             isIntRe.IsMatch(agents) &&
             isIntListRe.IsMatch(redAgentSetup) ->
        let runsTyped = Int32.Parse(runs)
        let agentsTyped = Int32.Parse(agents)
        let redAgents = redAgentSetup.Split(";") |> Seq.map Int32.Parse |> List.ofSeq
        printfn "Start simulation runs = %i; agents = %i; redAgentSetup = %O" runsTyped agentsTyped redAgents
        redAgents
        |> List.map (runAllSimulationsForARedSetup runsTyped agentsTyped)
        |> ignore
        printfn "Simulation completed. Took %O" (DateTime.Now - start)
        0
    | _ ->
        printfn "Invalid command.
        
        Usage
        =====
        dotnet run <runsCount: int> <agents: int> <redAgentSetup: int list>
        
        Example
        =======
        dotnet run 100 200 10;30;50
        "
        1
