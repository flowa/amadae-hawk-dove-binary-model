// Learn more about F# at http://fsharp.org

open System
open FSharp.Control
open FSharp.Json
open Model
open Simulation
open Statistics.ModelExtensions
open Statistics.RoundStats

let runSimulation (setup: GameSetup) =
    let initialGameState = setup.ToInitialGameState()
    let initialAgents = setup.GenerateAgents()
    let afterSimulation = initialGameState.SimulateRounds initialAgents
    afterSimulation

let initializationRounds = 1
type SimulationSingleRunSetup = {
    Runs: int
    AgentCount: int
    RedAgentPercentage: int
    ExpectedHawkPortion: float
    State2Rounds: int
}


let generateGameSetup (simulationSetup: SimulationSingleRunSetup)  =
    let ``Reward (V)`` = 10.0 
    let ``Cost (C)`` =
        match simulationSetup.ExpectedHawkPortion with
        | p when p >= 1.0 || p <= 0.0 -> raise (ArgumentException("expectedHawkPortion must be in range ]0,1["))
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
                StageName = "Stage 1"
                // StrategyInitFn = SimulationStages.stage1Game_withIdealNMSEDistribution
                StrategyInitFn =
                    SimulationStages.stage1Game_withIdealFiftyFiftyDistribution
                    // SimulationStages.simulation_setup_random
                    // SimulationStages.stage1Game_withIdealNMSEDistribution
                    // SimulationStages.simulation_setup_random
                    // (SimulationStages.allPlay Strategy.Hawk)
                MayUseColor = true
                SetPayoffForStage = id
            }
            {
                SimulationFrame.RoundCount = simulationSetup.State2Rounds
                StageName = "Stage 2"
                StrategyInitFn = SimulationStages.stage2Game_v5_withFullIndividualHistory
                MayUseColor = true
                SetPayoffForStage = id
            }
        ]
    }
    
type SimulationRunResultStats =
    {
        Runs: int
        AgentCount: int
        RedAgentPercentage: int
        HawkPortion: float
        PayoffReward: float
        PayoffCost: float
        State2Rounds: int
        FirstRoundHawkCountAvg: float
        FirstRoundDoveCountAvg: float
        //
        FirstSeparationOfColors_Avg:   float option
        FirstSeparationOfColors_Min:   int option
        FirstSeparationOfColors_Max:   int option
        FirstSeparationOfColors_Count: int option
        FirstSeparationOfColors_P:     float option
        FirstSeparationOfColors_DominatedByRed_Count: int
        FirstSeparationOfColors_DominatedByRed_P: float
        FirstSeparationOfColors_DominatedByBlue_Count: int
        FirstSeparationOfColors_DominatedByBlue_P: float
        FirstSeparationOfColors_DominatedByNone_Count: int
        FirstSeparationOfColors_DominatedByNone_P: float
        
        // 
        First2ConsecutiveSeparationOfColors_Avg: float option
        First2ConsecutiveSeparationOfColors_Min: int option
        First2ConsecutiveSeparationOfColors_Max: int option
        First2ConsecutiveSeparationOfColors_Count: int option
        First2ConsecutiveSeparationOfColors_P: float option
        First2ConsecutiveSeparationOfColors_DominatedByRed_Count: int
        First2ConsecutiveSeparationOfColors_DominatedByRed_P: float
        First2ConsecutiveSeparationOfColors_DominatedByBlue_Count: int
        First2ConsecutiveSeparationOfColors_DominatedByBlue_P: float
        First2ConsecutiveSeparationOfColors_DominatedByNone_Count: int
        First2ConsecutiveSeparationOfColors_DominatedByNone_P: float
        // 4 consecutive 
        First4ConsecutiveSeparationOfColors_Avg: float option
        First4ConsecutiveSeparationOfColors_Min: int option
        First4ConsecutiveSeparationOfColors_Max: int option
        First4ConsecutiveSeparationOfColors_Count: int option
        First4ConsecutiveSeparationOfColors_P: float option
        First4ConsecutiveSeparationOfColors_DominatedByRed_Count: int
        First4ConsecutiveSeparationOfColors_DominatedByRed_P: float
        First4ConsecutiveSeparationOfColors_DominatedByBlue_Count: int
        First4ConsecutiveSeparationOfColors_DominatedByBlue_P: float
        First4ConsecutiveSeparationOfColors_DominatedByNone_Count: int
        First4ConsecutiveSeparationOfColors_DominatedByNone_P: float
        // 8 consecutive 
        First8ConsecutiveSeparationOfColors_Avg: float option
        First8ConsecutiveSeparationOfColors_Min: int option
        First8ConsecutiveSeparationOfColors_Max: int option
        First8ConsecutiveSeparationOfColors_Count: int option
        First8ConsecutiveSeparationOfColors_P: float option
        First8ConsecutiveSeparationOfColors_DominatedByRed_Count: int
        First8ConsecutiveSeparationOfColors_DominatedByRed_P: float
        First8ConsecutiveSeparationOfColors_DominatedByBlue_Count: int
        First8ConsecutiveSeparationOfColors_DominatedByBlue_P: float
        First8ConsecutiveSeparationOfColors_DominatedByNone_Count: int
        First8ConsecutiveSeparationOfColors_DominatedByNone_P: float
    
    }
    member this.saveToFile ()  =
        let json = Json.serialize this
        let path = (sprintf "output/runs_%i-s2rounds_%i-agents_%i-redPercent_%i.NMSE_%f.json"
                         this.Runs
                         this.State2Rounds
                         this.AgentCount
                         this.RedAgentPercentage
                         this.HawkPortion)
        if (not (System.IO.Directory.Exists("output"))) then
             System.IO.Directory.CreateDirectory("output")
             |> (fun d -> printfn $"created folder %s{d.FullName}")
        System.IO.File.WriteAllText(path, json)

module SimStats =
    let safeAvg (data: int list) =
       match data with
       | [] -> None
       | _ -> data |> List.map float |> Seq.average |> Some
    
    let safeMin (data: int list) =
       match data with
       | [] -> None
       | _ -> data |> List.min |> int |> Some

    let safeMax (data: int list) =
       match data with
       | [] -> None
       | _ -> data |> List.max |> int |> Some

    let safeCount (data: int list) =
       match data with
       | [] -> None
       | _ -> data |> List.length |> Some

    let safeP (runs: int) (data: int list) =
       match data with
       | [] -> None
       | _ ->
           (float) data.Length / (float) runs
           |> Some
       
    let colorDominanceCount (c: Color) (data: Color option list) =
        data |> List.filter (fun d -> d = Some c) |> List.length

    let noDominanceCount (data: Color option list) =
        data |> List.filter (fun d -> d = None) |> List.length

    let colorDominanceP (c: Color) (runs: int) (data: Color option list)  =
        (float) (colorDominanceCount c data) / (float) runs

    let noDominanceP (runs: int) (data: Color option list)  =
        (float) (noDominanceCount data) / (float) runs
        
    let firstRoundsWithNConsecutiveSeparatedRounds (n: int) (games: GameState list) =
        let start = DateTime.Now 
        let res =
            games
            |> List.map (fun r -> r.ResolvedRounds.FirstRoundWithNConsecutiveRoundOfSeparatedColors n)
            |> List.filter (fun v -> v.IsSome)
            |> List.map (fun v -> v.Value - initializationRounds)
        printfn "📊\t firstRoundsWithNConsecutiveSeparatedRounds N=%i; Took=%O" n (DateTime.Now - start)
        res

let runSimulationsWithOneSetup (simulationRunSetup: SimulationSingleRunSetup)  =
    let start = DateTime.Now
    printfn "\n▶️\t Starting (R=%i; NMSE=%f) " simulationRunSetup.RedAgentPercentage simulationRunSetup.ExpectedHawkPortion
    let setup: GameSetup = generateGameSetup simulationRunSetup
    let results = 
        seq { 
            for i in 1 ..  simulationRunSetup.Runs do 
                if (i % 5) = 0 then printf "...%i" i
                yield runSimulation setup
        }  |> Seq.toList
    printfn "\n⏹️️\t Simulations run completed. Took=%O" (DateTime.Now - start)
//    printfn "\nPayoff: %O" setup.PayoffMatrix
//    printfn "\nLast round: %O"
//        (results
//         |> List.last
//         |> (fun (r: GameState) -> r.ResolvedRounds)
//         |> (fun (Rounds c) -> Array.last c))

    let firstRoundDoveCountAvg =
            results
            |> List.map (fun game ->
                let stat = game.ResolvedRounds.FirstRoundChallenges.StrategyStats ()
                (float) stat.DoveN)
            |> List.average
    let firstRoundHawkCountAvg =
        results
        |> List.map (fun game ->
            let stat = game.ResolvedRounds.FirstRoundChallenges.StrategyStats ()
            (float) stat.HawkN)
        |> List.average

    let startCalc = DateTime.Now 
    let firstSeparations = SimStats.firstRoundsWithNConsecutiveSeparatedRounds 1 results        
    let first2ConsecutiveSeparation = SimStats.firstRoundsWithNConsecutiveSeparatedRounds 2 results
    let first4ConsecutiveSeparation = SimStats.firstRoundsWithNConsecutiveSeparatedRounds 4 results  
    let first8ConsecutiveSeparation = SimStats.firstRoundsWithNConsecutiveSeparatedRounds 8 results  
    printfn "\n⏹️️\t Separation calculated completed. Took=%O" (DateTime.Now - startCalc)

    let startCalc = DateTime.Now
    let dominance1Con = results |> List.map (fun r -> r.ResolvedRounds.DominatingColorAfterSeparation 1)
    let dominance2Con = results |> List.map (fun r -> r.ResolvedRounds.DominatingColorAfterSeparation 2)
    let dominance4Con = results |> List.map (fun r -> r.ResolvedRounds.DominatingColorAfterSeparation 4)
    let dominance8Con = results |> List.map (fun r -> r.ResolvedRounds.DominatingColorAfterSeparation 8)
    printfn "\n⏹️️\t Dominance calculated. Took=%O" (DateTime.Now - startCalc)

    let stats: SimulationRunResultStats = {
        SimulationRunResultStats.HawkPortion = simulationRunSetup.ExpectedHawkPortion
        RedAgentPercentage = simulationRunSetup.RedAgentPercentage
        AgentCount = simulationRunSetup.AgentCount
        PayoffReward = setup.PayoffMatrix.``Revard (V)``
        PayoffCost = setup.PayoffMatrix.``Cost (C)``
        Runs = simulationRunSetup.Runs
        State2Rounds = simulationRunSetup.State2Rounds

        FirstRoundDoveCountAvg = firstRoundDoveCountAvg
        FirstRoundHawkCountAvg = firstRoundHawkCountAvg
        FirstSeparationOfColors_Avg =   firstSeparations |> SimStats.safeAvg
        FirstSeparationOfColors_Min =   firstSeparations |> SimStats.safeMin
        FirstSeparationOfColors_Max =   firstSeparations |> SimStats.safeMax
        FirstSeparationOfColors_Count = firstSeparations |> SimStats.safeCount
        FirstSeparationOfColors_P =     firstSeparations |> (SimStats.safeP simulationRunSetup.Runs)
        FirstSeparationOfColors_DominatedByRed_Count =  dominance1Con |> (SimStats.colorDominanceCount Red)
        FirstSeparationOfColors_DominatedByRed_P =      dominance1Con |> (SimStats.colorDominanceP Red simulationRunSetup.Runs)
        FirstSeparationOfColors_DominatedByBlue_Count = dominance1Con |> (SimStats.colorDominanceCount Blue)
        FirstSeparationOfColors_DominatedByBlue_P =     dominance1Con |> (SimStats.colorDominanceP Blue simulationRunSetup.Runs)
        FirstSeparationOfColors_DominatedByNone_Count = dominance1Con |> SimStats.noDominanceCount
        FirstSeparationOfColors_DominatedByNone_P =     dominance1Con |> (SimStats.noDominanceP simulationRunSetup.Runs)
        
        First2ConsecutiveSeparationOfColors_Avg =   first2ConsecutiveSeparation |> SimStats.safeAvg 
        First2ConsecutiveSeparationOfColors_Min =   first2ConsecutiveSeparation |> SimStats.safeMin
        First2ConsecutiveSeparationOfColors_Max =   first2ConsecutiveSeparation |> SimStats.safeMax        
        First2ConsecutiveSeparationOfColors_Count = first2ConsecutiveSeparation |> SimStats.safeCount
        First2ConsecutiveSeparationOfColors_P =     first2ConsecutiveSeparation |> (SimStats.safeP simulationRunSetup.Runs)
        First2ConsecutiveSeparationOfColors_DominatedByRed_Count = dominance2Con |> (SimStats.colorDominanceCount Red)
        First2ConsecutiveSeparationOfColors_DominatedByRed_P =     dominance2Con |> (SimStats.colorDominanceP Red simulationRunSetup.Runs)
        First2ConsecutiveSeparationOfColors_DominatedByBlue_Count = dominance2Con |> (SimStats.colorDominanceCount Blue)
        First2ConsecutiveSeparationOfColors_DominatedByBlue_P =     dominance2Con |> (SimStats.colorDominanceP Blue simulationRunSetup.Runs)
        First2ConsecutiveSeparationOfColors_DominatedByNone_Count = dominance2Con |> (SimStats.noDominanceCount)
        First2ConsecutiveSeparationOfColors_DominatedByNone_P =     dominance2Con |> (SimStats.noDominanceP simulationRunSetup.Runs)
        
        First4ConsecutiveSeparationOfColors_Avg =   first4ConsecutiveSeparation |> SimStats.safeAvg 
        First4ConsecutiveSeparationOfColors_Min =   first4ConsecutiveSeparation |> SimStats.safeMin
        First4ConsecutiveSeparationOfColors_Max =   first4ConsecutiveSeparation |> SimStats.safeMax        
        First4ConsecutiveSeparationOfColors_Count = first4ConsecutiveSeparation |> SimStats.safeCount
        First4ConsecutiveSeparationOfColors_P =     first4ConsecutiveSeparation |> (SimStats.safeP simulationRunSetup.Runs)
        First4ConsecutiveSeparationOfColors_DominatedByRed_Count = dominance4Con |> (SimStats.colorDominanceCount Red)
        First4ConsecutiveSeparationOfColors_DominatedByRed_P =     dominance4Con |> (SimStats.colorDominanceP Red simulationRunSetup.Runs)
        First4ConsecutiveSeparationOfColors_DominatedByBlue_Count = dominance4Con |> (SimStats.colorDominanceCount Blue)
        First4ConsecutiveSeparationOfColors_DominatedByBlue_P =     dominance4Con |> (SimStats.colorDominanceP Blue simulationRunSetup.Runs)
        First4ConsecutiveSeparationOfColors_DominatedByNone_Count = dominance4Con |> (SimStats.noDominanceCount)
        First4ConsecutiveSeparationOfColors_DominatedByNone_P =     dominance4Con |> (SimStats.noDominanceP simulationRunSetup.Runs)
        
        First8ConsecutiveSeparationOfColors_Avg =   first8ConsecutiveSeparation |> SimStats.safeAvg 
        First8ConsecutiveSeparationOfColors_Min =   first8ConsecutiveSeparation |> SimStats.safeMin
        First8ConsecutiveSeparationOfColors_Max =   first8ConsecutiveSeparation |> SimStats.safeMax        
        First8ConsecutiveSeparationOfColors_Count = first8ConsecutiveSeparation |> SimStats.safeCount
        First8ConsecutiveSeparationOfColors_P =     first8ConsecutiveSeparation |> (SimStats.safeP simulationRunSetup.Runs)
        First8ConsecutiveSeparationOfColors_DominatedByRed_Count = dominance8Con |> (SimStats.colorDominanceCount Red)
        First8ConsecutiveSeparationOfColors_DominatedByRed_P =     dominance8Con |> (SimStats.colorDominanceP Red simulationRunSetup.Runs)
        First8ConsecutiveSeparationOfColors_DominatedByBlue_Count = dominance8Con |> (SimStats.colorDominanceCount Blue)
        First8ConsecutiveSeparationOfColors_DominatedByBlue_P =     dominance8Con |> (SimStats.colorDominanceP Blue simulationRunSetup.Runs)
        First8ConsecutiveSeparationOfColors_DominatedByNone_Count = dominance8Con |> (SimStats.noDominanceCount)
        First8ConsecutiveSeparationOfColors_DominatedByNone_P =     dominance8Con |> (SimStats.noDominanceP simulationRunSetup.Runs)
     }
    
    stats.saveToFile()
    printfn "\n🏁\t Completed (R=%i,NMSE=%f) took=%O" simulationRunSetup.RedAgentPercentage simulationRunSetup.ExpectedHawkPortion (DateTime.Now - start)
    stats
    

let runAllSimulationsForARedSetup runs agentCount stage2Rounds redPercent =
    let setups = seq {
        for hawkPortion in (0.1)..(0.1)..(0.9) do
            yield {
                SimulationSingleRunSetup.Runs = runs
                AgentCount = agentCount
                RedAgentPercentage = redPercent
                ExpectedHawkPortion = hawkPortion
                State2Rounds = stage2Rounds }
    }
    
    setups
    |> Seq.map runSimulationsWithOneSetup
    |> List.ofSeq

let runAllSimulationsForARedSetupAndHaws runs agentCount stage2Rounds redPercents hawkPortions =
    let setups = seq {
        for redPercent in redPercents do
            for hawkPortion in hawkPortions do
                yield {
                    SimulationSingleRunSetup.Runs = runs
                    AgentCount = agentCount
                    RedAgentPercentage = redPercent
                    ExpectedHawkPortion = hawkPortion
                    State2Rounds = stage2Rounds }
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
    | [|  "once"; runs; agents; stage2Rounds; redAgentPercent; hawkPercent |]
        when isIntRe.IsMatch(runs) &&
             isIntRe.IsMatch(agents) &&
             isIntRe.IsMatch(stage2Rounds) &&
             isIntRe.IsMatch(redAgentPercent) &&
             isIntRe.IsMatch(hawkPercent) ->
        let runsTyped = Int32.Parse(runs)
        let agentsTyped = Int32.Parse(agents)
        let redsTyped = Int32.Parse(redAgentPercent)
        let stage2RoundsTyped = Int32.Parse(stage2Rounds)
        let redAgentPercentTyped = Double.Parse(hawkPercent) / 100.0
        let res = runSimulationsWithOneSetup {
            SimulationSingleRunSetup.Runs = runsTyped
            AgentCount = agentsTyped
            RedAgentPercentage = redsTyped
            ExpectedHawkPortion = redAgentPercentTyped
            State2Rounds = stage2RoundsTyped
        }
        printfn "Result: %O" res
        0
    
    | [|  runs; agents; stage2Rounds; redAgentSetup; expectedHawkPercents |]

        when isIntRe.IsMatch(runs) &&
             isIntRe.IsMatch(agents) &&
             isIntRe.IsMatch(stage2Rounds) &&
             isIntListRe.IsMatch(redAgentSetup) && 
             isIntListRe.IsMatch(expectedHawkPercents) ->
        let runsTyped = Int32.Parse(runs)
        let agentsTyped = Int32.Parse(agents)
        let stage2Rounds = Int32.Parse(stage2Rounds)
        let redAgents = redAgentSetup.Split(";")
                        |> Seq.map Int32.Parse
                        |> List.ofSeq
        let expectedHawkPercents = expectedHawkPercents.Split(";")
                                   |> Seq.map Int32.Parse
                                   |> Seq.map (fun percents -> (float) percents / 100.0)
                                   |> List.ofSeq
        printfn "Start simulation runs = %i; agents = %i; redAgentSetup = %O" runsTyped agentsTyped redAgents
        runAllSimulationsForARedSetupAndHaws runsTyped agentsTyped stage2Rounds redAgents expectedHawkPercents  
        |> ignore
        printfn "Simulation completed. Took %O" (DateTime.Now - start)
        0
    | _ ->
        printfn "Invalid command parameters.
        
        Usage
        =====
        dotnet run <runsCount: int> <agents: int> <stage2Round: int> <redAgentSetup: int list> <expectedHawkPercents: int list>
        
        Example
        =======
        dotnet run 100 200 250 \"10;50;90\" \"10;30;50;70;90\"
        "
        1