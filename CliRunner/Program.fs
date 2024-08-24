// Learn more about F# at http://fsharp.org

open System
open System.IO
open System.Text.Json.Serialization
open FSharp.Control
open FSharp.Json
open Model
open Simulation
open Statistics.ModelExtensions
open Statistics.RoundStats
open ModelExtensions
open Thoth.Json
open System.IO.Compression
let runSimulation (setup: GameSetup) =
    let initialGameState = setup.ToInitialGameState()
    let initialAgents = setup.GenerateAgents()
    let afterSimulation = initialGameState.SimulateRounds initialAgents
    afterSimulation

type SimulationSingleRunSetup =
    {
        Id : string
        Runs: int
        AgentCount: int
        RedAgentPercentage: int
        ExpectedHawkPortion: decimal
        Stage1: (int*string) option
        Stage2: int*string
    }
    member this.Stage1Rounds =
        match this.Stage1 with
        | Some (round, _) -> round
        | None -> 0
    member this.Stage1Mode =
        match this.Stage1 with
        | Some (_, mode) -> mode
        | None -> "None"

    member this.Stage2Rounds =
        match this.Stage2 with
        | (round, _) -> round
    member this.Stage2Mode =
        match this.Stage2 with
        | (_, mode) -> mode

let generateGameSetup (simulationSetup: SimulationSingleRunSetup)  =
    let ``Reward (V)`` = 10.0m
    let ``Cost (C)`` =
        match simulationSetup.ExpectedHawkPortion with
        | p when p >= 1.0m || p <= 0.0m -> raise (ArgumentException("expectedHawkPortion must be in range ]0,1["))
        | _ -> ``Reward (V)`` / simulationSetup.ExpectedHawkPortion
    let modeMapper stageName (roundCount: int, strategyMode: string): SimulationFrame =
        {
            SimulationFrame.RoundCount = roundCount
            StageName = stageName // "Simulation - Stage 2"
            ModeLabel = "Mode"
            StrategyInitFnName = strategyMode
            MayUseColor = true
            SetPayoffForStage = id
        }

    {
        GameParameters = {
            AgentCount = simulationSetup.AgentCount
            PortionOfRed = simulationSetup.RedAgentPercentage
            PayoffMatrix = PayoffMatrixType.FromRewardAndCost (``Reward (V)``, ``Cost (C)``)
        }
        SimulationFrames =
            match simulationSetup.Stage1 with
            | None ->
                [
                    modeMapper "Stage 2" simulationSetup.Stage2
                ]
            | Some stage1 ->
                [
                    modeMapper "Stage 1" stage1
                    modeMapper "Stage 2" simulationSetup.Stage2
                ]
    }

type SimulationRunResultStats =
    {
        Id: string
        Runs: int
        AgentCount: int
        RedAgentPercentage: int
        Stage1Rounds: int
        Stage1Mode: string
        Stage2Rounds: int
        Stage2Mode: string
        HawkPortion: decimal
        PayoffReward: decimal
        PayoffCost: decimal

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

        LastRoundSeparationOfColors_DominatedByRed_Count: int
        LastRoundSeparationOfColors_DominatedByRed_P: float
        LastRoundSeparationOfColors_DominatedByBlue_Count: int
        LastRoundSeparationOfColors_DominatedByBlue_P: float
        LastRoundSeparationOfColors_DominatedByNone_Count: int
        LastRoundSeparationOfColors_DominatedByNone_P: float

//        //
//        First2ConsecutiveSeparationOfColors_Avg: float option
//        First2ConsecutiveSeparationOfColors_Min: int option
//        First2ConsecutiveSeparationOfColors_Max: int option
//        First2ConsecutiveSeparationOfColors_Count: int option
//        First2ConsecutiveSeparationOfColors_P: float option
//        First2ConsecutiveSeparationOfColors_DominatedByRed_Count: int
//        First2ConsecutiveSeparationOfColors_DominatedByRed_P: float
//        First2ConsecutiveSeparationOfColors_DominatedByBlue_Count: int
//        First2ConsecutiveSeparationOfColors_DominatedByBlue_P: float
//        First2ConsecutiveSeparationOfColors_DominatedByNone_Count: int
//        First2ConsecutiveSeparationOfColors_DominatedByNone_P: float
//        // 4 consecutive
//        First4ConsecutiveSeparationOfColors_Avg: float option
//        First4ConsecutiveSeparationOfColors_Min: int option
//        First4ConsecutiveSeparationOfColors_Max: int option
//        First4ConsecutiveSeparationOfColors_Count: int option
//        First4ConsecutiveSeparationOfColors_P: float option
//        First4ConsecutiveSeparationOfColors_DominatedByRed_Count: int
//        First4ConsecutiveSeparationOfColors_DominatedByRed_P: float
//        First4ConsecutiveSeparationOfColors_DominatedByBlue_Count: int
//        First4ConsecutiveSeparationOfColors_DominatedByBlue_P: float
//        First4ConsecutiveSeparationOfColors_DominatedByNone_Count: int
//        First4ConsecutiveSeparationOfColors_DominatedByNone_P: float
//        // 8 consecutive
//        First8ConsecutiveSeparationOfColors_Avg: float option
//        First8ConsecutiveSeparationOfColors_Min: int option
//        First8ConsecutiveSeparationOfColors_Max: int option
//        First8ConsecutiveSeparationOfColors_Count: int option
//        First8ConsecutiveSeparationOfColors_P: float option
//        First8ConsecutiveSeparationOfColors_DominatedByRed_Count: int
//        First8ConsecutiveSeparationOfColors_DominatedByRed_P: float
//        First8ConsecutiveSeparationOfColors_DominatedByBlue_Count: int
//        First8ConsecutiveSeparationOfColors_DominatedByBlue_P: float
//        First8ConsecutiveSeparationOfColors_DominatedByNone_Count: int
//        First8ConsecutiveSeparationOfColors_DominatedByNone_P: float
    }
    member this.saveToFile (simulationHistories: string list list)  =

        let json = Json.serialize this
        let hawks = (int) (this.HawkPortion * 100.0m)
        let fileBase = $"%s{this.Id}_RN%i{this.Runs}.%s{this.Stage2Mode}.s2N%i{this.Stage2Rounds}.agent%i{this.AgentCount}.R%i{this.RedAgentPercentage}.H%i{hawks}"
        let path = $"output/%s{fileBase}.json"
        let historyEntryPath (simulationIndex: int) = $"%s{fileBase}.%i{simulationIndex}.history.csv"
        let historyZipPAth = $"output/%s{fileBase}.history.zip"

        if (not (System.IO.Directory.Exists("output"))) then
             System.IO.Directory.CreateDirectory("output")
             |> (fun d -> printfn $"created folder %s{d.FullName}")
        if (not (System.IO.Directory.Exists("output/data"))) then
             System.IO.Directory.CreateDirectory("output/data")
             |> (fun d -> printfn $"created folder %s{d.FullName}")
        System.IO.File.WriteAllText(path, json)


        use zipToOpen = new FileStream(historyZipPAth, FileMode.Create)
        use archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create)

        let saveHistoryFile (gameIndex: int) (dataRows: string list) =
            let simulationZip = archive.CreateEntry(historyEntryPath gameIndex)
            use writer = new StreamWriter(simulationZip.Open())
            writer.WriteLine "Round index,Me color+id,Other color+id,Choices"
            dataRows |> List.iter writer.WriteLine

        simulationHistories |> List.take 5 |> List.iteri saveHistoryFile

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
        
    let firstRoundsWithNConsecutiveSeparatedRounds (initializationRounds) (n: int) (games: GameState list) =
        let start = DateTime.Now 
        let res =
            games
            |> List.map (fun r -> r.ResolvedRounds.FirstRoundWithNConsecutiveRoundOfSeparatedColors n)
            |> List.filter (fun v -> v.IsSome)
            |> List.map (fun v -> v.Value - initializationRounds)
        printfn $"📊\t firstRoundsWithNConsecutiveSeparatedRounds N=%i{n}; Took={DateTime.Now - start}"
        res
    let compressHistory (games: GameState) : string list =
        let compressResolvedChallenge (roundIndex: int)  (challenge: ResolvedChallenge) : string list =
            let (p1, p2) = challenge.Players
            let compStrategy (s : Strategy option) =
                match s with
                | Some Hawk -> "H"
                | Some Dove -> "D"
                | None -> "N"
            let compColor (c : Color) =
                match c with
                | Blue -> "B"
                | Red -> "R"

            let compEncounter (me: Agent) (other: Agent) = $"{roundIndex},{compColor me.Color}%x{me.Id},{compColor other.Color}%x{other.Id},{compStrategy me.Strategy}{compStrategy other.Strategy}"
            [ (compEncounter p1 p2)
              (compEncounter p2 p1) ]
        let compressRound (roundIndex: int) (round: GameRound) : string list =
            round.ToList()
            |> List.collect (compressResolvedChallenge roundIndex)

        games.ResolvedRounds.Unwrap()
        |> Seq.mapi compressRound
        |> List.concat

let runSimulationsWithOneSetup (simulationRunSetup: SimulationSingleRunSetup)  =
    let start = DateTime.Now
    printfn $"\n▶️\t Starting (R=%i{simulationRunSetup.RedAgentPercentage}; NMSE=%f{simulationRunSetup.ExpectedHawkPortion}) "
    let setup: GameSetup = generateGameSetup simulationRunSetup
    let results = 
        seq { 
            for i in 1 ..  simulationRunSetup.Runs do 
                if (i % 5) = 0 then printf "...%i" i
                yield runSimulation setup
        }  |> Seq.toList
    printfn $"\n⏹️️\t Simulations run completed. Took={DateTime.Now - start}"

    let firstRoundDoveCountAvg =
            results
            |> List.averageBy (fun game ->
                let stat = game.ResolvedRounds.FirstRoundChallenges.StrategyStats ()
                (float) stat.DoveN)

    let firstRoundHawkCountAvg =
        results
        |> List.averageBy (fun game ->
            let stat = game.ResolvedRounds.FirstRoundChallenges.StrategyStats ()
            (float) stat.HawkN)

    let startCalc = DateTime.Now 
    let firstSeparations = SimStats.firstRoundsWithNConsecutiveSeparatedRounds simulationRunSetup.Stage1Rounds 1 results
//    let first2ConsecutiveSeparation = SimStats.firstRoundsWithNConsecutiveSeparatedRounds simulationRunSetup.Stage1Rounds 2 results
//    let first4ConsecutiveSeparation = SimStats.firstRoundsWithNConsecutiveSeparatedRounds simulationRunSetup.Stage1Rounds 4 results
//    let first8ConsecutiveSeparation = SimStats.firstRoundsWithNConsecutiveSeparatedRounds simulationRunSetup.Stage1Rounds 8 results
    printfn $"\n⏹️️\t Separation calculated completed. Took={DateTime.Now - startCalc}"

    let startCalc = DateTime.Now
    let dominance1Con = results |> List.map (fun r -> r.ResolvedRounds.DominatingColorAfterSeparation 1)
    let dominanceLastRound =
        results
        |> List.map (fun r ->
             r.ResolvedRounds.Unwrap()
             |> Array.last
             |> (fun (round: GameRound) -> round.FullyDominatingColor))
//    let dominance2Con = results |> List.map (fun r -> r.ResolvedRounds.DominatingColorAfterSeparation 2)
//    let dominance4Con = results |> List.map (fun r -> r.ResolvedRounds.DominatingColorAfterSeparation 4)
//    let dominance8Con = results |> List.map (fun r -> r.ResolvedRounds.DominatingColorAfterSeparation 8)
    let simHistories = results |> List.map SimStats.compressHistory

    printfn $"\n⏹️️\t Dominance calculated. Took={DateTime.Now - startCalc}"

    let stats: SimulationRunResultStats = {
        SimulationRunResultStats.Id = simulationRunSetup.Id
        HawkPortion = simulationRunSetup.ExpectedHawkPortion
        RedAgentPercentage = simulationRunSetup.RedAgentPercentage
        AgentCount = simulationRunSetup.AgentCount
        Stage1Rounds = simulationRunSetup.Stage1Rounds
        Stage1Mode = simulationRunSetup.Stage1Mode
        Stage2Rounds = simulationRunSetup.Stage2Rounds
        Stage2Mode = simulationRunSetup.Stage2Mode

        PayoffReward = setup.PayoffMatrix.``Revard (V)``
        PayoffCost = setup.PayoffMatrix.``Cost (C)``
        Runs = simulationRunSetup.Runs

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

        LastRoundSeparationOfColors_DominatedByRed_Count  = dominanceLastRound |> (SimStats.colorDominanceCount Red)
        LastRoundSeparationOfColors_DominatedByRed_P      = dominanceLastRound |> (SimStats.colorDominanceP Red simulationRunSetup.Runs)
        LastRoundSeparationOfColors_DominatedByBlue_Count = dominanceLastRound |> (SimStats.colorDominanceCount Blue)
        LastRoundSeparationOfColors_DominatedByBlue_P     = dominanceLastRound |> (SimStats.colorDominanceP Blue simulationRunSetup.Runs)
        LastRoundSeparationOfColors_DominatedByNone_Count = dominanceLastRound |> SimStats.noDominanceCount
        LastRoundSeparationOfColors_DominatedByNone_P     = dominanceLastRound |> (SimStats.noDominanceP simulationRunSetup.Runs)

//        First2ConsecutiveSeparationOfColors_Avg =   first2ConsecutiveSeparation |> SimStats.safeAvg
//        First2ConsecutiveSeparationOfColors_Min =   first2ConsecutiveSeparation |> SimStats.safeMin
//        First2ConsecutiveSeparationOfColors_Max =   first2ConsecutiveSeparation |> SimStats.safeMax
//        First2ConsecutiveSeparationOfColors_Count = first2ConsecutiveSeparation |> SimStats.safeCount
//        First2ConsecutiveSeparationOfColors_P =     first2ConsecutiveSeparation |> (SimStats.safeP simulationRunSetup.Runs)
//        First2ConsecutiveSeparationOfColors_DominatedByRed_Count = dominance2Con |> (SimStats.colorDominanceCount Red)
//        First2ConsecutiveSeparationOfColors_DominatedByRed_P =     dominance2Con |> (SimStats.colorDominanceP Red simulationRunSetup.Runs)
//        First2ConsecutiveSeparationOfColors_DominatedByBlue_Count = dominance2Con |> (SimStats.colorDominanceCount Blue)
//        First2ConsecutiveSeparationOfColors_DominatedByBlue_P =     dominance2Con |> (SimStats.colorDominanceP Blue simulationRunSetup.Runs)
//        First2ConsecutiveSeparationOfColors_DominatedByNone_Count = dominance2Con |> (SimStats.noDominanceCount)
//        First2ConsecutiveSeparationOfColors_DominatedByNone_P =     dominance2Con |> (SimStats.noDominanceP simulationRunSetup.Runs)
//
//        First4ConsecutiveSeparationOfColors_Avg =   first4ConsecutiveSeparation |> SimStats.safeAvg
//        First4ConsecutiveSeparationOfColors_Min =   first4ConsecutiveSeparation |> SimStats.safeMin
//        First4ConsecutiveSeparationOfColors_Max =   first4ConsecutiveSeparation |> SimStats.safeMax
//        First4ConsecutiveSeparationOfColors_Count = first4ConsecutiveSeparation |> SimStats.safeCount
//        First4ConsecutiveSeparationOfColors_P =     first4ConsecutiveSeparation |> (SimStats.safeP simulationRunSetup.Runs)
//        First4ConsecutiveSeparationOfColors_DominatedByRed_Count = dominance4Con |> (SimStats.colorDominanceCount Red)
//        First4ConsecutiveSeparationOfColors_DominatedByRed_P =     dominance4Con |> (SimStats.colorDominanceP Red simulationRunSetup.Runs)
//        First4ConsecutiveSeparationOfColors_DominatedByBlue_Count = dominance4Con |> (SimStats.colorDominanceCount Blue)
//        First4ConsecutiveSeparationOfColors_DominatedByBlue_P =     dominance4Con |> (SimStats.colorDominanceP Blue simulationRunSetup.Runs)
//        First4ConsecutiveSeparationOfColors_DominatedByNone_Count = dominance4Con |> (SimStats.noDominanceCount)
//        First4ConsecutiveSeparationOfColors_DominatedByNone_P =     dominance4Con |> (SimStats.noDominanceP simulationRunSetup.Runs)
//
//        First8ConsecutiveSeparationOfColors_Avg =   first8ConsecutiveSeparation |> SimStats.safeAvg
//        First8ConsecutiveSeparationOfColors_Min =   first8ConsecutiveSeparation |> SimStats.safeMin
//        First8ConsecutiveSeparationOfColors_Max =   first8ConsecutiveSeparation |> SimStats.safeMax
//        First8ConsecutiveSeparationOfColors_Count = first8ConsecutiveSeparation |> SimStats.safeCount
//        First8ConsecutiveSeparationOfColors_P =     first8ConsecutiveSeparation |> (SimStats.safeP simulationRunSetup.Runs)
//        First8ConsecutiveSeparationOfColors_DominatedByRed_Count = dominance8Con |> (SimStats.colorDominanceCount Red)
//        First8ConsecutiveSeparationOfColors_DominatedByRed_P =     dominance8Con |> (SimStats.colorDominanceP Red simulationRunSetup.Runs)
//        First8ConsecutiveSeparationOfColors_DominatedByBlue_Count = dominance8Con |> (SimStats.colorDominanceCount Blue)
//        First8ConsecutiveSeparationOfColors_DominatedByBlue_P =     dominance8Con |> (SimStats.colorDominanceP Blue simulationRunSetup.Runs)
//        First8ConsecutiveSeparationOfColors_DominatedByNone_Count = dominance8Con |> (SimStats.noDominanceCount)
//        First8ConsecutiveSeparationOfColors_DominatedByNone_P =     dominance8Con |> (SimStats.noDominanceP simulationRunSetup.Runs)
     }

    let startSave = DateTime.Now
    stats.saveToFile(simHistories)
    printfn $"\n⏹️️\t Data persisted. Took={DateTime.Now - startSave}"
    printfn $"\n🏁\t Completed (R=%i{simulationRunSetup.RedAgentPercentage},NMSE=%f{simulationRunSetup.ExpectedHawkPortion}) took={DateTime.Now - start}"
    stats




let runAllSimulationsForARedSetupAndHaws runId runs agentCount stage1 stage2 redPercents hawkPortions =
    let setups = seq {
        for redPercent in redPercents do
            for hawkPortion in hawkPortions do
                yield {
                    SimulationSingleRunSetup.Id = runId
                    Runs = runs
                    AgentCount = agentCount
                    RedAgentPercentage = redPercent
                    ExpectedHawkPortion = hawkPortion
                    Stage1 = stage1
                    Stage2 = stage2
                }
    }
    setups
    |> Seq.map runSimulationsWithOneSetup
    |> List.ofSeq

type CliParams =
    {
        RunId: string
        Runs: int option
        AgentCount: int option
        Stage1: (int * string) option
        Stage2: (int * string) option
        RedSetup: int list option
        HawkSetup: decimal list option
    }
    static member Empty =
        {
            RunId = Guid.NewGuid().ToString()
            Runs = None
            AgentCount = None
            Stage1 = None
            Stage2 = None
            RedSetup = None
            HawkSetup = None
        }
    member this.Call() =
        match this with
        | {
            RunId = runId
            Runs = Some runsTyped
            AgentCount = Some agentsTyped
            Stage1 = maybeStage1
            Stage2 = Some stage2
            RedSetup = Some redSetup
            HawkSetup = Some hawkSetup
           } -> printfn "Run simulation with setup: \n\n %O \n\n" this
                let start = DateTime.Now
                runAllSimulationsForARedSetupAndHaws runId runsTyped agentsTyped maybeStage1 stage2 redSetup hawkSetup
                |> ignore
                printfn "Simulation completed. Took %O" (DateTime.Now - start)
                0
        | _ -> printfn "Invalid or missing params"
               printfn $"Parsed: \n\n {this} \n\n
                    Usage
                    =====
                    dotnet run id=<id:string> runs=<runsCount: int> agents=<agents: int> stage1=<stage1Round: int>-<mode:string> stage2=<stage2Round: int>-<mode:string> reds=<redAgentSetup: int list> hawks=<expectedHawkPercents: int list>

                    id and stage1 are optional. Other parameters are mandatory

                    Example
                    =======
                    dotnet run id=test runs=100 agents=200 stage2=250-stage2_nmse red=10,50,90 hawk=10,30,50,70,90

                    For more examples, see README.md
                    "
               1

[<EntryPoint>]
let main argv =
    let start = DateTime.Now
    printfn "Hawk-Dove simulation runner"
    printfn "==========================="
    let accParse (this: CliParams) (str: string): CliParams =
        let parseIntList (listStr: string) =
            listStr.Split ','
            |> Seq.map Int32.Parse
            |> List.ofSeq
        let parseHawkSetup (listStr: string)=
            listStr.Split ','
            |> Seq.map (Int32.Parse >> fun percents -> decimal percents / 100.0m)
            |> List.ofSeq
        let parseMode (str: string) =
            (SimulationStageOptions.AllOptions
            |> List.tryFind (fun (o: StageStrategyFnOptions) -> o.Name = str)
            |> Option.map (fun o -> o.Name)
            )

        match str.Split('=','-') with
        | [|"id"; id |] -> { this with RunId = id}
        | [|"runs"; runCount |] -> { this with Runs = Some (Int32.Parse runCount) }
        | [|"agents"; agentCount |] -> { this with AgentCount = Some (Int32.Parse agentCount) }
        | [|"stage1"; roundCount; mode |] when (parseMode mode) <> None ->
            { this with Stage1 = Some ((Int32.Parse roundCount), (parseMode mode).Value) }
        | [|"stage2"; roundCount; mode |] when (parseMode mode) <> None
         -> { this with Stage2 = Some ((Int32.Parse roundCount), (parseMode mode).Value) }
        | [|"red"; setup |] -> { this with RedSetup = Some (parseIntList setup) }
        | [|"hawk"; setup |] -> { this with HawkSetup = Some (parseHawkSetup setup) }
        | _ -> failwith $"Unexpected cli parameter: %s{str}"

    let setup = argv |> Array.fold accParse CliParams.Empty
    setup.Call()