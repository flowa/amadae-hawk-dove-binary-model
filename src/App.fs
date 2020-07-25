module App

open Model
open Elmish
open Browser
open Simulation
// MODEL
let runSimulation (setup: GameSetup)=
    let initialGameState = setup.ToInitialGameState()
    let initialAgents = setup.GenerateAgents()
    //
    let afterSimulatio = initialGameState.SimulateRounds initialAgents
                            // setup.RoundsToPlay
                            // GameModes.nashEqlibiumGame
                            // GameModes.stage2Game
                            // GameModes.simpleGame
    {
        Setup = setup
        State = afterSimulatio
        ViewState = ShowResults (setup.RoundsToPlay)
        PlayAnimation = false
    }


let init() : State =
    let setup =
        {
            GameSetup.RoundsToPlay = 100
            AgentCount = 10
            PortionOfRed = 50
            PayoffMatrixType = FromRewardAndCost (10.0, 20.0)
            SimulationFrames = [
                {
                    SimulationFrame.RoundCount = 10
                    StageName = "Stage 1"
                    StrategyFn = GameModes.nashEqlibiumGame
                }
                {
                    SimulationFrame.RoundCount = 90
                    StageName = "Stage 2"
                    StrategyFn = GameModes.stage2Game
                }
            ]
            // StrategySpecs = [Hawk, 3; Dove, 3]
            // UseNashPortions
            // CustomPortion [Hawk, 9; Dove, 9]

            // In UI
            // number of rounds of certain type of game
            // - stage 1
            // - stage 2
            // - stage 3
            // PayoffMatric cost
            // Show payoff materic
            // Note:
            //    Technically in game stage thtree there are two
            //    Matrices
            // Add possibility to download simulation data as csv
            // Add summary table
            // - row per stage, avg payoff for per color and per strategy
            // Add cability so see historical round and animation


            // PayoffMatrixType = Custom [
            //     (Hawk, Hawk), (0.0, 0.0)
            //     (Hawk, Dove), (4.0, 0.0)
            //     (Dove, Hawk), (0.0, 4.0)
            //     (Dove, Dove), (2.0, 2.0)
            // ]
        }
    runSimulation setup

// UPDATE

let update (msg:Msg) (state: State) =
    let setGameSetup (updatedGameSetup: GameSetup) =
        { state with Setup = updatedGameSetup}
    let setField (field: FieldValue) =
        match field with
        | TotalRoundsInGame value ->
            (setGameSetup { state.Setup with RoundsToPlay = value })
        | AgentCount value ->
            (setGameSetup { state.Setup with AgentCount = value })
        | PortionOfRed value ->
            (setGameSetup { state.Setup with PortionOfRed = value })
        | BenefitOnVictory value ->
            (setGameSetup {
                    state.Setup with
                        PayoffMatrixType = (state.Setup.PayoffMatrixType.SetV (float value))
                })
        | CostOfLoss value ->
            (setGameSetup {
                    state.Setup with
                        PayoffMatrixType = (state.Setup.PayoffMatrixType.SetC (float value))
                })

        // | f ->
        //     eprintfn "Not implemented %A" f;
        //     state

    match msg with
    | SetValue field -> setField field
    | ShowRound round ->
        { state with ViewState = ShowResults round }
    | ToInitialization ->
        { state with
            ViewState = InitGame
            PlayAnimation = false }
    | RunSimulation ->
        runSimulation state.Setup
    | Tick _ ->
        let maxRound = state.Setup.RoundsToPlay
        let currentRound = state.CurrentRound
        match (state.PlayAnimation, state.ViewState) with
        | true, ShowResults round when maxRound > currentRound ->
            { state with ViewState = ShowResults (round + 1)}
        | true, ShowResults _ when maxRound = currentRound ->
            { state with PlayAnimation = false }
        | _ -> state
    | PlayAnimation ->
        { state with
            PlayAnimation = true
            ViewState =
                if state.CurrentRound = state.Setup.RoundsToPlay then
                    ShowResults 1
                else
                    state.ViewState }
    | StopAnimation ->
        { state with PlayAnimation = false }


        // { state with
        //     ViewState = ShowResults { ShowRound = state.Setup.RoundToPlay }}

open MainView
open Elmish.React

let timer initial =
    let sub dispatch =
        window.setInterval
            (fun _ -> dispatch (Tick System.DateTime.Now)
            , 500) |> ignore
    Cmd.ofSub sub

// App
Program.mkSimple init update MainView.view
|> Program.withReactBatched "app"
|> Program.withSubscription timer
// |> Program.withConsoleTrace
|> Program.run