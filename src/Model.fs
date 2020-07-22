module Model
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
    static member GenerateList
        (specs: (Strategy * int) list) : Strategy list =
        specs
        |> List.collect (fun (value, count) ->
                List.init count (fun _ -> value))

type Agent =
    {
        // For testing and for UI
        Id: int
        // Only these values should be used in simulation
        Color: Color
        Payoff: float
        // TODO Should be memory
        Strategy: Strategy
    }

type PayoffMatrix =
    Map<(Strategy * Strategy), (float * float)>

type PayOffMatrixType =
    | Custom of ((Strategy * Strategy) * (float * float)) list
    | FromRewardAndCost of revard: float * cost: float
    member this.ToMatrix () =
        match this with
        | Custom m -> Map.ofList m
        | FromRewardAndCost (revard, cost) ->
            Map.ofList [
                (Hawk, Hawk), (((revard - cost) / 2.0), ((revard - cost) / 2.0))
                (Hawk, Dove), (revard, 0.0)
                (Dove, Hawk), (0.0, revard)
                (Dove, Dove), ((revard / 2.0), (revard / 2.0))
            ]


type GameSetup =
    {
        RoundToPlay: int
        ColorSpecs:  (Color * int) list
        StrategySpecs:  (Strategy * int) list
        PayoffMatrixType: PayOffMatrixType
    }

type ColorStatistics =
    {
        CountOfRedHawks: float
        CountOfBlueHawks: float
    }
    member this.HawkCountFor(c: Color) =
        match c with
        | Red  -> this.CountOfRedHawks
        | Blue -> this.CountOfRedHawks

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
    member this.GetColorStatistics() =
        {
            CountOfRedHawks = this.CountOf (Red, Hawk)
            CountOfBlueHawks = this.CountOf (Blue, Hawk)
        }


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

type GameState =
    {
        PayoffMatrix: PayoffMatrix
        // TODO: this is agents on before game is pley
        // -> probably it should not be here at all because it's confusing
        Agents: Agent list
        ResolvedRounds:GameHistory
    }
    static member FromSetup (setup: GameSetup)=
        let agents =
            let colors = Color.GenerateList setup.ColorSpecs |> ListHelpers.shuffle
            let strategies = Strategy.GenerateList setup.StrategySpecs |> ListHelpers.shuffle
            let agentIds = List.init colors.Length id
            List.map3
                (fun color strategy agentId ->
                    {
                        Agent.Id = agentId
                        Color = color
                        Strategy = strategy
                        Payoff = 0.0
                    }) colors strategies agentIds
        {
            GameState.Agents = agents
            PayoffMatrix = setup.PayoffMatrixType.ToMatrix()
            ResolvedRounds = Rounds []
        }
    member this.Simulate (playFn: (Agent -> Color -> GameState -> Strategy)) =
        let gamePairs =
            this.Agents
            |> ListHelpers.shuffle
            |> ListHelpers.toPairs

        let play (agent1: Agent, agent2: Agent): ResolvedChallenge =
            let agent1Choise = playFn agent1 agent2.Color this
            let agent2Choise = playFn agent2 agent1.Color this
            let payOffPair = this.PayoffMatrix.Item (agent1Choise, agent2Choise)
            let updatedAgents =
                match payOffPair with
                (p1, p2) -> { agent1 with
                                Payoff = agent1.Payoff + p1
                                Strategy = agent1Choise
                            },
                            { agent2 with
                                Payoff = agent2.Payoff + p2
                                Strategy = agent2Choise
                            }
            {
                ResolvedChallenge.Players = updatedAgents
                Choices = agent1Choise, agent2Choise
            }

        let round = gamePairs |> List.map play
        { this with
            ResolvedRounds = this.ResolvedRounds.Append (Round round)
            Agents =
                round
                |> List.collect (fun {Players = (p1, p2)} -> [p1; p2])
                |> List.sortBy (fun a -> a.Id)
        }
    member this.SimulateRounds (rounds: int) playFn =
        let rec simulateRec (currentRound: int) (stateAcc: GameState) =
            let updatedState = stateAcc.Simulate playFn
            if currentRound = rounds then
                updatedState
            else
                simulateRec (currentRound + 1) updatedState
        simulateRec 1 this

type ShowResultsViewState =
    {
        ShowRound: int
    }
type ResultViewState =
    | InitGame
    | ShowResults of ShowResultsViewState

type State =
    {
        Setup: GameSetup
        State: GameState
        ViewState: ResultViewState
    }
    member this.CurrentRound
        with get() =
            match this.ViewState with
            | ShowResults { ShowRound = round } -> round
            | _ -> 0
    member this.CurrentRoundAgents() =
        match this.ViewState with
        | ShowResults { ShowRound = round } ->
            this.State.ResolvedRounds.GetRoundByRoundNumber(round).Agents
        | _ -> []

type FieldValue =
    | MaxRoundsField of int
    | CountOfRed of int
    | CountOfHawks of int
    | BenefitOnVictory of int
    | CostOfLoss of int

type Msg =
    | Set of FieldValue
    | ShowRound of int
    | RunSimulation