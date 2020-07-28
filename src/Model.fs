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

type ChallengeType =
    | DifferentColor
    | SameColor

type AgentIdentity =
    {
        Id: int
        Color: Color
    }

type GameInfo =
    {
        Payoff: float
        PreviousChoice: Strategy
        PreviousChallengeType: ChallengeType
    }

type Agent =
    | AgentWithNoGames of AgentIdentity
    | AgentWithGameInfo of AgentIdentity * GameInfo
    member this.Id
        with get() =
            match this with
            | AgentWithNoGames a -> a.Id
            | AgentWithGameInfo (a, _) -> a.Id
    member this.Color
        with get() =
            match this with
            | AgentWithNoGames a -> a.Color
            | AgentWithGameInfo (a, _) -> a.Color
    member this.Payoff
        with get() =
            match this with
            | AgentWithNoGames _ -> 0.0
            | AgentWithGameInfo (_, { Payoff = payoff}) -> payoff
    member this.Strategy
        with get() =
            match this with
            | AgentWithNoGames a -> None
            | AgentWithGameInfo (_, { PreviousChoice = choice}) -> Some choice
    member this.StrategyName
        with get() =
            match this with
            | AgentWithNoGames a -> "None"
            | AgentWithGameInfo (_, { PreviousChoice = choice}) -> sprintf "%A" choice
    member this.LastRoundChallengeType
        with get() =
            match this with
            | AgentWithNoGames a -> None
            | AgentWithGameInfo (_, { PreviousChallengeType = challengType}) -> Some challengType
    member this.UpdateGameInfo(payoff, choice, challengeType) =
        match this with
        | AgentWithNoGames a ->
            AgentWithGameInfo (a, { Payoff = payoff; PreviousChoice = choice; PreviousChallengeType = challengeType})
        | AgentWithGameInfo (a, { Payoff = payoffAccumulator }) ->
            AgentWithGameInfo (a, { Payoff = payoffAccumulator + payoff; PreviousChoice = choice; PreviousChallengeType = challengeType})

type ResolvedChallenge =
    {
        // Players after game was resolved
        Players: Agent * Agent
    }
    member this.ToList () =
        match this.Players with
        | (first, second) -> [first; second]
    member this.ChalengeType
        with get() =
            match this.Players with
            | p2, p1 when p1.Color = p2.Color -> SameColor
            | _ -> DifferentColor

// type PayoffMatrix =
//    Map<(Strategy * Strategy), (float * float)>

// Payoff matrics is type alias hence extension methos is used
// [<System.Runtime.CompilerServices.Extension>]
// type PayoffMatrixExtensions =
//     [<System.Runtime.CompilerServices.Extension>]
//     static member inline GetMyPayoff (this: PayoffMatrixType, myChoise:Strategy, opponentChoise: Strategy) =
//         let (myPayoff, _) = PayoffMatrixType.ToMatrix().[(myChoise, opponentChoise)]
//         myPayoff

type PayoffMatrixType =
    | FromRewardAndCost of reward: float * cost: float
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
    member this.``Revard (V)``
        with get() = match this with | FromRewardAndCost (reward, _) -> reward
    member this.``Cost (C)``
        with get() = match this with | FromRewardAndCost (_, cost) -> cost
    member this.ToMatrix ()  =
        match this with
        | FromRewardAndCost (revard, cost) ->
            Map.ofList [
                (Hawk, Hawk), (((revard - cost) / 2.0), ((revard - cost) / 2.0))
                (Hawk, Dove), (revard, 0.0)
                (Dove, Hawk), (0.0, revard)
                (Dove, Dove), ((revard / 2.0), (revard / 2.0))
            ]
    member this.GetPayoffFor(myChoise:Strategy, opponentChoise: Strategy) =
        this.ToMatrix().[(myChoise, opponentChoise)]
    member this.GetMyPayoff(myChoise:Strategy, opponentChoise: Strategy) =
        let (myPayoff, _) = this.ToMatrix().[(myChoise, opponentChoise)]
        myPayoff


// TODO: refactor
// type ColorStatistics =
//     {
//         CountOfRedHawks: float
//         CountOfBlueHawks: float
//     }
//     member this.HawkCountFor(c: Color) =
//         match c with
//         | Red  -> this.CountOfRedHawks
//         | Blue -> this.CountOfRedHawks

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

type GameHistory =
    | Rounds of GameRound array
    member this.Append(round: GameRound) =
        match this with
        | Rounds previousRounds -> Rounds (Array.append previousRounds [|round|])
    member this.ToList() = this.Unwrap() |> List.ofArray
    member this.Unwrap() =
        match this with
        | Rounds rounds -> rounds
    member this.TotalRounds with get() = this.ToList().Length
    member this.GetRoundByRoundNumber (roundNumber: int) =
        let roundIndex = roundNumber - 1
        this.ToList().[roundIndex]
    member this.GetRoundCount() =
        this.ToList().Length
    member this.HasHistory
        with get() = this.Unwrap() |> Array.isEmpty |> not
    member this.LastRoundChallenges
        with get() = this.Unwrap() |> Array.last

type GameInformation =
    {
        Agent: Agent
        // Should be option
        OpponentColor: Color
        PayoffMatrix: PayoffMatrixType
        Agents: Agent list
        History: GameHistory
        // Random number is passed as a part of game information
        // so that strategies are easier to test
        RandomNumber: float
    }

type StrategyFn = GameInformation -> Strategy
let rand = Random()
type PlannedRound =
    {
        PayoffMatrix: PayoffMatrixType
        StrategyFn: StrategyFn
        StageName: string
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
                    RandomNumber = rand.NextDouble()
                }
            let gameInformationForAgent2: GameInformation =
                { gameInformationForAgent1 with
                    Agent = agent2
                    OpponentColor = agent1.Color
                    RandomNumber = rand.NextDouble()
                }
            let agent1Choise = this.StrategyFn gameInformationForAgent1
            let agent2Choise = this.StrategyFn gameInformationForAgent2
            let challengeType = if agent1.Color = agent2.Color then SameColor else DifferentColor
            let (payoff1, payoff2) = this.PayoffMatrix.GetPayoffFor(agent1Choise, agent2Choise)
            {
                ResolvedChallenge.Players =
                    agent1.UpdateGameInfo(payoff1, agent1Choise, challengeType),
                    agent2.UpdateGameInfo(payoff2, agent2Choise, challengeType)
            }

        gamePairs
        |> List.map play
        |> Round

type GameState =
    {
        PayoffMatrix: PayoffMatrixType
        PlannedRounds: PlannedRound list
        ResolvedRounds:GameHistory
    }
    member this.SimulateRoundsAsync (agents: Agent list) =
        promise {
            let initialValue = Promise.lift (agents, (Rounds [||]))
            let! (_, playedRounds) =
                this.PlannedRounds
                |> List.fold
                    (fun accPromise plannedRound ->
                        Promise.bind
                                (fun (agents: Agent list, history: GameHistory) ->
                                    promise {
                                            // This is needed here to make UI responsive during simulation
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
        AgentCount: int
        PortionOfRed: int
        SimulationFrames: SimulationFrame list
        PayoffMatrix: PayoffMatrixType
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
                AgentWithNoGames {
                    AgentIdentity.Id = agentId
                    Color = color
                }) colors agentIds

    member this.ToInitialGameState () =
        let payoffMatrics = this.PayoffMatrix
        let plannedRounds =
                this.SimulationFrames
                |> List.filter (fun f -> f.RoundCount > 0)
                |> List.collect
                    (fun frame ->
                        let plannedRound: PlannedRound =
                            {
                               PayoffMatrix = payoffMatrics
                               StrategyFn = frame.StrategyFn
                               StageName = frame.StageName
                            }
                        List.replicate frame.RoundCount plannedRound
                    )
        {
            PayoffMatrix   = payoffMatrics
            PlannedRounds  = plannedRounds
            ResolvedRounds = Rounds [||]
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
        GameState: GameState
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
            this.GameState.ResolvedRounds.GetRoundByRoundNumber(this.CurrentRound)
    member this.CurrentRoundAgents
        with get() =
            match this.ViewState with
            | ShowResults round ->
                this.GameState.ResolvedRounds.GetRoundByRoundNumber(round).Agents
            | _ -> []
    member this.CurrentStageName
        with get() =
            match this.ViewState with
            | ShowResults round -> this.GameState.PlannedRounds.[round - 1].StageName
            | _ -> String.Empty

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
