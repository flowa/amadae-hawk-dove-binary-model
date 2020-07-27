module App

open Model
open Elmish
open Browser
open Simulation
// MODEL
let runSimulationAsync (setup: GameSetup) =
    promise {
        let initialGameState = setup.ToInitialGameState()
        let initialAgents = setup.GenerateAgents()

        let! afterSimulation = initialGameState.SimulateRoundsAsync initialAgents
        return afterSimulation
        // {
        //     Setup = setup
        //     State = afterSimulation
        //     ViewState = ShowResults (setup.RoundsToPlay)
        //     PlayAnimation = false
        // }
    }


let init () =
    let setup =
        {
            GameSetup.AgentCount = 100
            PortionOfRed = 50
            PayoffMatrixType = FromRewardAndCost (10.0, 20.0)
            SimulationFrames = [
                {
                    SimulationFrame.RoundCount = 10
                    StageName = "Stage 1"
                    StrategyFn = GameModes.nashEqlibiumGame
                }
                {
                    SimulationFrame.RoundCount = 10
                    StageName = "Stage 2"
                    StrategyFn = GameModes.stage2Game
                }
                {
                    SimulationFrame.RoundCount = 10
                    StageName = "Stage 3"
                    StrategyFn = GameModes.stage3Game
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
    let initialGameState = setup.ToInitialGameState()
    {
        Setup = setup
        State = initialGameState
        ViewState = InitGame
        PlayAnimation = false
    }, Cmd.none

// UPDATE

let update (msg:Msg) (state: State) =
    let setGameSetup (updatedGameSetup: GameSetup) =
        { state with Setup = updatedGameSetup}
    let setField (field: FieldValue) =
        match field with
        | RoundCountOfStage (stage, value) ->
            (setGameSetup
                {
                    state.Setup with
                        SimulationFrames =
                            state.Setup.SimulationFrames
                            |> List.map
                                (fun frame ->
                                    if frame.StageName = stage then
                                        { frame with RoundCount = value}
                                    else frame)
                })
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
    | SetValue field -> (setField field), Cmd.none
    | ShowRound round ->
        { state with ViewState = ShowResults round }, Cmd.none
    | ToInitialization ->
        { state with
            ViewState = InitGame
            PlayAnimation = false }, Cmd.none
    | RunSimulation ->
        let runSimulation () =
                promise {
                    printfn "starting"
                    do! Promise.sleep 200
                    let! results = runSimulationAsync state.Setup
                    printfn "beforeDispatch"
                    return (OnSimulationComplated results)
                }
        {state with ViewState = Loading},
        Cmd.OfPromise.perform runSimulation () id

    | OnSimulationComplated gameState ->
        { state with
            ViewState = ShowResults state.Setup.RoundsToPlay
            State = gameState}, Cmd.none
    | Tick _ ->
        let maxRound = state.Setup.RoundsToPlay
        let currentRound = state.CurrentRound
        let updatedState =
            match (state.PlayAnimation, state.ViewState) with
            | true, ShowResults round when maxRound > currentRound ->
                { state with ViewState = ShowResults (round + 1)}
            | true, ShowResults _ when maxRound = currentRound ->
                { state with PlayAnimation = false }
            | _ -> state
        updatedState, Cmd.none
    | PlayAnimation ->
        {
            state with
                PlayAnimation = true
                ViewState =
                    if state.CurrentRound = state.Setup.RoundsToPlay then
                        ShowResults 1
                    else
                        state.ViewState
        }, Cmd.none

    | StopAnimation ->
        { state with PlayAnimation = false }, Cmd.none

open MainView
open Elmish.React

let timer initial =
    let sub dispatch =
        window.setInterval
            (fun _ -> dispatch (Tick System.DateTime.Now)
            , 500) |> ignore
    Cmd.ofSub sub

// App
Program.mkProgram init update MainView.view
|> Program.withReactBatched "app"
|> Program.withSubscription timer
// |> Program.withConsoleTrace
|> Program.run