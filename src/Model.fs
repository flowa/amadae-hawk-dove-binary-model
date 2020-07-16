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

type PayoffMatrix =Map<(Strategy * Strategy), (float * float)>

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

type AgentRef = int
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


type GameRound = ResolvedChallenge list
type GameHistory = GameRound list
type GameState =
    {
        PayoffMatrix: PayoffMatrix
        Agents: Agent list
        ResolvedRounds: ResolvedChallenge list list
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
            ResolvedRounds = []
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
            ResolvedRounds = List.append this.ResolvedRounds [round]
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

type State =
    {
        Setup: GameSetup
        State: GameState
    }

type FieldValue =
    | MaxRoundsField of int
    | CountOfRed of int
    | CountOfHawks of int
    | BenefitOnVictory of int
    | CostOfLoss of int

type Msg =
| Set of FieldValue
