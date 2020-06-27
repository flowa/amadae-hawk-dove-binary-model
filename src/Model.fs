module Model
open Helpers

type Color =
    | Blue
    | Red
    static member GenerateList
        (specs: (Color * int) list) : Color list =
        specs
        |> List.collect (fun (value, count) ->
                List.init count (fun _ -> value))


type Strategy =
    | Dowe
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
        PayOff: int
        Strategy: Strategy
    }

type PayoffTable = Map<(Strategy * Strategy), (int * int)>
type GameSetup =
    {
        RoundToPlay: int
        ColorSpecs:  (Color * int) list
        StrategySpecs:  (Strategy * int) list
        PayOffTable: PayoffTable
    }

type AgentRef = int
type ResolvedChallenge =
    {
        // Players after game was resolved
        Players: Agent * Agent
        Choices: Strategy * Strategy
    }

type GameRound = ResolvedChallenge list
type GameHistory = GameRound list
type GameState =
    {
        PayoffTable: PayoffTable
        Agents: Agent list
        ResolvedRounds: ResolvedChallenge list list
    }
    static member FromSetup (setup: GameSetup)=
        let agents =
            let colors = Color.GenerateList setup.ColorSpecs |> ListHelpers.shuffle
            let strategies = Strategy.GenerateList setup.StrategySpecs |> ListHelpers.shuffle
            let agentIds = List.init colors.Length id
            List.map3
                (fun color startegy agentId ->
                    {
                        Agent.Id = agentId
                        Color = color
                        Strategy = startegy
                        PayOff = 0
                    }) colors strategies agentIds
        {
            GameState.Agents = agents
            PayoffTable = setup.PayOffTable
            ResolvedRounds = []
        }

    member this.GetAgent (id: AgentRef) =
            this.Agents.[id]
    member this.SetAgent (id: AgentRef) (newValue: Agent) =
            let replace index oldValue =
                if index = id then newValue else oldValue
            {
                this with
                    Agents =
                        this.Agents
                        |> List.mapi replace
            }
    // Should this be here?
    member this.Simulate (playFn: (Agent -> Color -> GameHistory -> Strategy)) =
        let gamePairs =
            this.Agents
            |> ListHelpers.shuffle
            |> ListHelpers.toPairs
        let play (agent1: Agent, agent2: Agent): ResolvedChallenge =
            let agent1Choise = playFn agent1 agent2.Color this.ResolvedRounds
            let agent2Choise = playFn agent1 agent2.Color this.ResolvedRounds
            let payOffPair = this.PayoffTable.Item (agent1Choise, agent2Choise)
            let updatedAgents =
                match payOffPair with
                (p1, p2) -> { agent1 with PayOff = agent1.PayOff + p1},
                            { agent2 with PayOff = agent2.PayOff + p2}
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
