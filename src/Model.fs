module Model
open Fable
open Helpers
open System

type Color =
    | Blue
    | Red
    static member GenerateList
        (specs: (Color * int) list) : Color list =
        specs
        |> List.collect (fun (value, count) ->
                List.init count (fun _ -> value))

type Strategy =
    | Dove
    | Hawk

type Agent =
    {
        // For testing and for UI
        Id: int
        // Only these values should be used in simulation
        Color: Color
        Payoff: float
        // TODO Should be memory?
        Strategy: Strategy option
    }

type PayoffMatrix =
    Map<(Strategy * Strategy), (float * float)>

type PayOffMatrixType =
    //    | Custom of ((Strategy * Strategy) * (float * float)) list
    | FromRewardAndCost of revard: float * cost: float
    member this.Cost with get() =
        match this with
        | FromRewardAndCost (_, cost) -> cost
    member this.VictoryBenefit with get() =
        match this with
        | FromRewardAndCost (victory, _) -> victory
    member this.SetV(newValue) =
        match this with
        | FromRewardAndCost (victory, cost) -> FromRewardAndCost (newValue, cost)
    member this.SetC(newValue)  =
        match this with
        | FromRewardAndCost (victory, cost) -> FromRewardAndCost (victory, newValue)
    member this.ToMatrix ()  =
        match this with
    //    | Custom m -> Map.ofList m
        | FromRewardAndCost (revard, cost) ->
            Map.ofList [
                (Hawk, Hawk), (((revard - cost) / 2.0), ((revard - cost) / 2.0))
                (Hawk, Dove), (revard, 0.0)
                (Dove, Hawk), (0.0, revard)
                (Dove, Dove), ((revard / 2.0), (revard / 2.0))
            ]

// TODO: refactor
type ColorStatistics =
    {
        CountOfRedHawks: float
        CountOfBlueHawks: float
    }
    member this.HawkCountFor(c: Color) =
        match c with
        | Red  -> this.CountOfRedHawks
        | Blue -> this.CountOfRedHawks


type ChallengeType =
    | DifferentColor
    | SameColor

type ResolvedChallenge =
    {
        // Players after game was resolved
        Players: Agent * Agent
        Choices: Strategy * Strategy
    }
    member this.ToPlayersList () =
        match this.Players with
        | (first, second) -> [first; second]
    member this.ToColorChoiseList () =
        match this.Players, this.Choices with
        | (a1, a2), (c1, c2) -> [(a1.Color, c1); (a2.Color, c2)]
    member this.CountOf(pairToMatch: Color * Strategy) =
        this.ToColorChoiseList()
        |> List.filter ((=) pairToMatch)
        |> List.length
        |> float
    // TODO: refactor/remove
    member this.GetColorStatistics() =
        {
            CountOfRedHawks = this.CountOf (Red, Hawk)
            CountOfBlueHawks = this.CountOf (Blue, Hawk)
        }
    member this.ChalengeType
        with get() =
            match this.Players with
            | {Color = color1}, {Color = color2} when color1 = color2 -> SameColor
            | _ -> DifferentColor

type GameRound =
    | Round of (ResolvedChallenge list)
    member this.ToList() =
        match this with
        | Round round -> round
    member this.Agents
        with get() =
          match this with
          | Round challenges ->
            challenges
            |> List.collect (fun {Players = (p1, p2)} -> [p1; p2])
            |> List.sortBy (fun a -> a.Id)
    // TODO: refactor/remove
    member this.ColorStats () =
        let initialValue = { CountOfBlueHawks = 0.0; CountOfRedHawks = 0.0 }
        let updateStats (accumulatedStats: ColorStatistics) (currentChallenge: ResolvedChallenge) =
            let currentChallengeStats = currentChallenge.GetColorStatistics()
            {
                CountOfRedHawks = accumulatedStats.CountOfRedHawks + currentChallengeStats.CountOfRedHawks
                CountOfBlueHawks = accumulatedStats.CountOfBlueHawks + currentChallengeStats.CountOfBlueHawks
            }
        match this with
        | Round round ->
            round
            |> List.fold (updateStats) initialValue

type GameHistory =
    | Rounds of GameRound list
    member this.Append(round: GameRound) =
        match this with
        | Rounds previousRounds -> Rounds (List.append previousRounds [round])
    member this.ToList() =
        match this with
        | Rounds rounds -> rounds
    member this.TotalRounds with get() = this.ToList().Length
    member this.GetRoundByRoundNumber (roundNumber: int) =
        let roundIndex = roundNumber - 1
        this.ToList().[roundIndex]
    member this.GetRoundCount() =
        this.ToList().Length

type GameInformation =
    {
        Agent: Agent
        OpponentColor: Color
        PayoffMatrix: PayoffMatrix
        Agents: Agent list
        History: GameHistory
    }

type StrategyFn = GameInformation -> Strategy
type PlannedRound =
    {
        PayoffMatrix: PayoffMatrix
        StrategyFn: StrategyFn
    }
    member this.PlayRound (agents: Agent list) (history: GameHistory) =
        let gamePairs =
            agents
            |> ListHelpers.shuffle
            |> ListHelpers.toPairs

        let play (agent1: Agent, agent2: Agent): ResolvedChallenge =
            let gameInformationForAgent1: GameInformation =
                {
                    Agent = agent1
                    OpponentColor = agent2.Color
                    Agents = agents
                    History = history
                    PayoffMatrix = this.PayoffMatrix
                }
            let gameInformationForAgent2: GameInformation =
                { gameInformationForAgent1 with
                    Agent = agent2
                    OpponentColor = agent1.Color
                }
            let agent1Choise = this.StrategyFn gameInformationForAgent1
            let agent2Choise = this.StrategyFn gameInformationForAgent2
            let payOffPair = this.PayoffMatrix.Item (agent1Choise, agent2Choise)
            let updatedAgents =
                match payOffPair with
                (p1, p2) -> { agent1 with
                                Payoff = agent1.Payoff + p1
                                Strategy = Some agent1Choise
                            },
                            { agent2 with
                                Payoff = agent2.Payoff + p2
                                Strategy = Some agent2Choise
                            }
            {
                ResolvedChallenge.Players = updatedAgents
                Choices = agent1Choise, agent2Choise
            }

        gamePairs
        |> List.map play
        |> Round

type GameState =
    {
        PayoffMatrix: PayoffMatrix
        PlannedRounds: PlannedRound list
        ResolvedRounds:GameHistory
    }
    member this.SimulateRoundsAsync (agents: Agent list) =
        promise {
            // let! (_, playedRounds: GameHistory), err =
            let initialValue = Promise.lift (agents, (Rounds []))
            let! (_, playedRounds) =
                this.PlannedRounds
                |> List.fold
                    (fun accPromise plannedRound ->
                        // (agents: Agent list, history: GameHistory)
                        Promise.bind
                                (fun (agents: Agent list, history: GameHistory) ->
                                    promise {
                                            do! Promise.sleep 100
                                            let roundResult = plannedRound.PlayRound agents history
                                            let updatedHistory = history.Append roundResult
                                            let agentsAfterRound = roundResult.Agents
                                            return (agentsAfterRound, updatedHistory)
                                    })
                                accPromise
                    )
                    initialValue

            return {
                this with ResolvedRounds = playedRounds
            }
        }

type SimulationFrame =
    {
        RoundCount: int
        StageName: string
        StrategyFn: StrategyFn
    }

type GameSetup =
    {
        // RoundsToPlay: int
        AgentCount: int
        PortionOfRed: int
        SimulationFrames: SimulationFrame list
        PayoffMatrixType: PayOffMatrixType
    }
    member this.RoundsToPlay
        with get() = this.SimulationFrames |> List.sumBy (fun f -> f.RoundCount)
    member this.CountOfRed
        with get() =
            (float this.AgentCount) * ((float this.PortionOfRed) / 100.0)
            |> round
            |> int
    member this.CountOfBlue
        with get() = this.AgentCount - this.CountOfRed
    member this.ColorSpecs
        with get() =
            [
                Red, this.CountOfRed
                Blue, this.CountOfBlue
            ]
    member this.GenerateAgents() =
        let colors =
            Color.GenerateList this.ColorSpecs
            |> ListHelpers.shuffle
        let agentIds = List.init colors.Length id
        List.map2
            (fun color agentId ->
                {
                    Agent.Id = agentId
                    Color = color
                    Strategy = None
                    Payoff = 0.0
                }) colors agentIds
    member this.ToInitialGameState () =
        let payoffMatrics = this.PayoffMatrixType.ToMatrix()
        let plannedRounds =
                this.SimulationFrames
                |> List.filter (fun f -> f.RoundCount > 0)
                |> List.collect
                    (fun frame ->
                        let plannedRound: PlannedRound =
                            {
                               PayoffMatrix = payoffMatrics
                               StrategyFn = frame.StrategyFn
                            }
                        List.replicate frame.RoundCount plannedRound
                    )
        {
            PayoffMatrix   = payoffMatrics
            PlannedRounds  = plannedRounds
            ResolvedRounds = Rounds []
        }

type ShowResultsViewState =
    {
        ShowRound: int
    }
type ResultViewState =
    | InitGame
    | Loading
    | ShowResults of roundNumber: int

type State =
    {
        Setup: GameSetup
        State: GameState
        ViewState: ResultViewState
        PlayAnimation: Boolean
    }
    member this.CurrentRound
        with get() =
            match this.ViewState with
            | ShowResults round -> round
            | _ -> 0
    member this.CurrentRoundChallenges
        with get() =
            this.State.ResolvedRounds.GetRoundByRoundNumber(this.CurrentRound)

    member this.CurrentRoundAgents() =
        match this.ViewState with
        | ShowResults round ->
            this.State.ResolvedRounds.GetRoundByRoundNumber(round).Agents
        | _ -> []

type FieldValue =
    | RoundCountOfStage of stageName: string * roundCount: int
    | AgentCount of agentCount: int
    | PortionOfRed of percentsOfRed: int
    | BenefitOnVictory of v: int
    | CostOfLoss of c: int

type Msg =
    | SetValue of FieldValue
    | ShowRound of int
    | RunSimulation
    | OnSimulationComplated of GameState
    | ToInitialization
    | Tick of DateTime
    | PlayAnimation
    | StopAnimation


module RoundStats =
    let calcRoundAggregates (round: GameRound): Map<ChallengeType * Strategy * Color, int> =
        let items = round.ToList()
        items
        |>  List.collect (
            fun challenge ->
                let (a1, a2) = challenge.Players
                let challengeType = challenge.ChalengeType
                [
                    (challengeType, a1.Strategy.Value, a1.Color)
                    (challengeType, a2.Strategy.Value, a2.Color)
                ]
            )
        |> List.groupBy id
        |> List.map (fun (key, items) -> (key, items.Length))
        |> Map.ofList

    let aggregateBy<'a when 'a : equality and 'a : comparison>
        (keyFn: ChallengeType * Strategy * Color -> 'a)
        (aggs: Map<ChallengeType * Strategy * Color, int>): Map<'a, int> =
        aggs
        |> Map.toList
        |> List.map (fun (key, total) -> (keyFn key), total)
        |> List.groupBy (fun (key, _) -> key)
        |> List.map (fun (key, subAggs) -> key, subAggs |> List.sumBy (fun (_, value) -> value))
        |> Map.ofList

    let valueOrZero<'a when 'a : equality and 'a : comparison> (map: Map<'a, int>) (key: 'a) =
        match map.TryFind(key) with
        | None -> 0
        | Some v -> v

type GameRound with
    member this.Aggregates with get() = RoundStats.calcRoundAggregates(this)