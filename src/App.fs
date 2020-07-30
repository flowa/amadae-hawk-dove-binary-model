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
    }


let init () =
    let setup =
        {
            GameSetup.AgentCount = 100
            PortionOfRed = 50
            PayoffMatrix = FromRewardAndCost (10.0, 20.0)
            SimulationFrames = [
                {
                    SimulationFrame.RoundCount = 10
                    SetPayoffForStage = id
                    StageName = "Stage 1"
                    StrategyFn = SimulationStages.stage1Game
                    MayUseColor = false
                }
                {
                     SimulationFrame.RoundCount = 50
                     SetPayoffForStage = id
                     StageName = "Stage 2"
                     StrategyFn = SimulationStages.stage2Game_v5_withFullIndividualHistory
                     MayUseColor = true

                }
                // {
                //     SimulationFrame.RoundCount = 10
                //     StageName = "Stage 2 v2"
                //     StrategyFn = SimulationStages.stage2GameVersion2
                // }
                // {
                //      SimulationFrame.RoundCount = 10
                //      StageName = "Stage 2 v3"
                //      StrategyFn = SimulationStages.stage2GameVersion3
                // }
                {
                    SimulationFrame.RoundCount = 10
                    SetPayoffForStage = id
                    StageName = "Stage 3"
                    StrategyFn = GameMode.onBasedOfLastEncounterWithOpponentColor
                    MayUseColor = true
                }
            ]

        }
    let initialGameState = setup.ToInitialGameState()
    {
        Setup = setup
        GameState = initialGameState
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
                        PayoffMatrix = (state.Setup.PayoffMatrix.SetV (float value))
                })
        | CostOfLoss value ->
            (setGameSetup {
                    state.Setup with
                        PayoffMatrix = (state.Setup.PayoffMatrix.SetC (float value))
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
            GameState = gameState}, Cmd.none
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