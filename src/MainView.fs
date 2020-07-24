module MainView

open Model
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma

module Common =
    let group title items  =
        fieldset []
            [
                legend [] [str title]
                div [] items
            ]
    let row<'a> = tr []
    let cell<'a> (value: 'a) = td [] [str (sprintf "%O" value)]
    let cellHtml (elem: ReactElement) = td [] [elem]
    let cellHeader className value = th [ClassName className] [str value]
    let emptyCellHeader = th [] []

module RoundSlider =
  open Fulma.Extensions.Wikiki
  let stopIcon = i [ Key "stop";  ClassName "fa fa-stop" ] []
  let playIcon = i [ Key "play"; ClassName "fa fa-play" ] []
  let playOrStopButton (model: State) (dispatch) =
      if model.PlayAnimation then
        Button.button [
            Button.Color IsDanger
            Button.Size IsSmall
            Button.OnClick (fun e -> e.preventDefault(); dispatch StopAnimation)] [
            stopIcon
            // ofString "\u00a0 Stop"
        ]
      else
        Button.button [
            Button.Color IsPrimary
            Button.Size IsSmall
            Button.OnClick (fun e -> e.preventDefault(); dispatch PlayAnimation) ] [
            playIcon
            // str "\u00a0 Play"
        ]

  let slider (model: State) (dispatch) =
    let max = float (model.Setup.RoundsToPlay)
    let onChange (e: Browser.Types.Event) =
        dispatch (ShowRound (int e.Value))
    div [ ClassName "block"]
        [
            div [ClassName "animation-controls columns"] [
                div [ClassName "animation-buttons column is-narrow"] [ playOrStopButton model dispatch ]
                div [ClassName "simulation-frame-slider column"] [
                    Slider.slider
                        [
                          Slider.IsFullWidth
                          Slider.Max max
                          Slider.Min 1.0
                          Slider.Value (float model.CurrentRound)
                          Slider.OnChange onChange
                        ]
                ]
            ]
        ]

module Tables =
    open Common

    let renderPayoffMatrics (payoff: PayoffMatrix)  =
        let format (player1, player2) =
            sprintf "%.0f / %.0f" player1 player2
        table [ClassName "payoff-summary"] [
            thead [] [
                row [
                    emptyCellHeader
                    cellHeader "Hawk"  "Hawk"
                    cellHeader "Dove"  "Dove"
                ]
            ]
            tbody [] [
                row [
                        cellHeader "Hawk" "Hawk"
                        cell (payoff.[Hawk, Hawk] |> format)
                        cell (payoff.[Hawk, Dove] |> format)
                    ]
                row [
                        cellHeader "Dove" "Dove"
                        cell (payoff.[Dove, Hawk] |> format)
                        cell (payoff.[Dove, Dove] |> format)
                    ]
            ]
        ]

    let hawkDoveStatsHeader  =
        table [ClassName "hawk-dove-stats"] [
            tbody []
                [
                    row [cellHeader "Hawk" "Hawk"]
                    row [cellHeader "Dove" "Dove"]
                    row [cellHeader "Sum" ""]
                ]
            ]

    let hawkDoveStats className (hawkN: int, doveN: int) =
        let format (n, portion) =
            sprintf "%i (%.0f%%)" n portion
        let totalN = hawkN + doveN
        let portionFor a =
            match totalN with
            | 0 -> 0.0
            | _ -> ((float a) / (float totalN)) * 100.0

        table [ClassName ("hawk-dove-stats " + className)] [
            tbody []
                [
                    row [ cell ((hawkN, (portionFor hawkN)) |> format) ]
                    row [ cell ((doveN, (portionFor doveN)) |> format) ]
                    row [ td [ClassName "Sum"] [ofInt totalN] ]
                ]
            ]

    let simulatioStatsTable (model: State) =
        let intentTd = td [ClassName "cell-intent"] []
        let subTitle title =
            tr [Class "subtitle-row"] [th [ColSpan 4] [str title]]
        let aggs: Map<ChallengeType * Strategy * Color, int> = model.CurrentRoundChallenges.Aggregates
        let statsTupleFor (challengeType: ChallengeType) (color: Color) =
            let hawkN = Stats.valueOrZero aggs (challengeType, Hawk, color)
            let doveN = Stats.valueOrZero aggs (challengeType, Dove, color)
            (hawkN, doveN)

        let statsTupleColorsCombined (challengeType: ChallengeType) =
            let subAgg = Stats.aggregateBy (fun (challenge, strategy, _) -> (challenge, strategy)) aggs
            let hawkN = Stats.valueOrZero subAgg (challengeType, Hawk)
            let doveN = Stats.valueOrZero subAgg (challengeType, Dove)
            (hawkN, doveN)

        let statsTupleChallengeTypeCombined (color: Color) =
            let subAgg = Stats.aggregateBy (fun (_, strategy, color) -> (strategy, color)) aggs
            let hawkN = Stats.valueOrZero subAgg (Hawk, color)
            let doveN = Stats.valueOrZero subAgg (Dove, color)
            (hawkN, doveN)

        let statsTupleByStrategy =
            let subAgg = Stats.aggregateBy (fun (_, strategy, _) -> strategy) aggs
            let hawkN = Stats.valueOrZero subAgg Hawk
            let doveN = Stats.valueOrZero subAgg Dove
            (hawkN, doveN)

        table [ClassName "hawk-dowe-stats"] [
            thead [] [
                row [
                        emptyCellHeader
                        cellHeader "Red" "Red"
                        cellHeader "Blue" "Blue"
                        cellHeader "AllTotal" "All"

                    ]
            ]
            tbody [] [
                subTitle "Different color"
                row [
                        hawkDoveStatsHeader
                        cellHtml (hawkDoveStats "Red"  (statsTupleFor DifferentColor Red))
                        cellHtml (hawkDoveStats "Blue" (statsTupleFor DifferentColor Blue))
                        cellHtml (hawkDoveStats "All"  (statsTupleColorsCombined DifferentColor))
                    ]
                subTitle "Same color"
                row [
                        hawkDoveStatsHeader
                        cellHtml (hawkDoveStats "Red" (statsTupleFor SameColor Red))
                        cellHtml (hawkDoveStats "Blue" (statsTupleFor SameColor Blue))
                        cellHtml (hawkDoveStats "All" (statsTupleColorsCombined SameColor))
                    ]

                subTitle "All"
                row [
                        hawkDoveStatsHeader
                        cellHtml (hawkDoveStats "Red"  (statsTupleChallengeTypeCombined Red))
                        cellHtml (hawkDoveStats "Blue" (statsTupleChallengeTypeCombined Blue))
                        cellHtml (hawkDoveStats "All"  statsTupleByStrategy)
                    ]
            ]
        ]

    type RenderColotStatsOptions =
        {
            WithTotal: bool
            WithPortions: bool
        }
        static member None with get() = {WithTotal = false; WithPortions = false; }
        static member WithTotalsAndPortions with get()  = {WithTotal = true; WithPortions = true; }

    let renderColotStats (setting: GameSetup) (opt: RenderColotStatsOptions) =
        table [ClassName "agent-summary"] [
            thead [] [
                row [
                        emptyCellHeader
                        cellHeader "Red" "Red"
                        cellHeader "Blue" "Blue"
                        if opt.WithTotal then cellHeader "AllTotal" "Total"
                    ]
            ]
            tbody [] [
                row [
                        cellHeader "attribute" "Count"
                        cell setting.CountOfRed
                        cell setting.CountOfBlue
                        if opt.WithTotal then cell setting.AgentCount
                    ]
            ]
        ]


module Fields =
    type NumberFieldProps =
        {
            Label: string
            Value: int
            OnChange: (int -> FieldValue)
        }
    let numberField
        (dispatch: Msg -> unit)
        (props: NumberFieldProps) =
        let strToInt s =
            match s with
            | "" -> 0
            | s -> int s

        Field.div
            [Field.IsHorizontal]
            [
                div [ClassName "field-label is-small"]
                    [
                        Label.label [Label.CustomClass "is-expanded"] [ str props.Label ]
                    ]
                div [ClassName "field-body"]
                    [
                        Field.div [] [
                            Control.div
                                []
                                [ Input.number [
                                    Input.Props [ClassName "is-small"]
                                    Input.OnChange
                                        (fun e -> dispatch (SetValue (props.OnChange (strToInt e.Value))))
                                    Input.Value (props.Value.ToString())]
                                ]
                        ]
                    ]
            ]

module SettingsForm =
    open Common
    open Tables
    let view (model: State) (dispatch: Msg -> unit) =
        let numberField = Fields.numberField dispatch
        match model.ViewState with
        | InitGame ->
            div [
                    ClassName "column is-one-quarter"
                    Id "settings"
                ]
                [
                    h1 [] [
                        str "Setting"
                    ]

                    group "Duration" [
                            numberField
                                {
                                    Label =    "Rounds"
                                    Value =    model.Setup.RoundsToPlay
                                    OnChange = (fun value -> (TotalRoundsInGame value))
                                }
                        ]
                    group "Agents" [
                            numberField
                                {
                                    Label =    "Agent count"
                                    Value =    model.Setup.AgentCount
                                    OnChange = (fun value -> (AgentCount value))
                                }

                            numberField
                                {
                                    Label =    "Reds agents (%)"
                                    Value =    model.Setup.PortionOfRed
                                    OnChange = (fun value -> (PortionOfRed value))
                                }
                            (renderColotStats model.Setup RenderColotStatsOptions.None)
                        ]

                    group "Payoff" [
                            numberField
                                {
                                    Label =    "Reward (V)"
                                    Value =    (int model.Setup.PayoffMatrixType.VictoryBenefit)
                                    OnChange = (fun value -> (BenefitOnVictory value))
                                }

                            numberField
                                {
                                    Label =    "Cost (C)"
                                    Value =    (int model.Setup.PayoffMatrixType.Cost)
                                    OnChange = (fun value -> (CostOfLoss value))
                                }
                            renderPayoffMatrics (model.Setup.PayoffMatrixType.ToMatrix())
                        ]
                ]

        | ShowResults viewState ->
            div [ ClassName "column is-one-quarter"; Id "settings" ] [
                h1  []
                    [
                        str "Simulation statistics"
                    ]
                group "Round challenges" [
                    simulatioStatsTable model
                ]
                h1  []
                    [
                        str "Setup"
                    ]

                group "Color setup" [
                    renderColotStats model.Setup RenderColotStatsOptions.WithTotalsAndPortions
                ]
                group "Payoff" [
                        renderPayoffMatrics (model.Setup.PayoffMatrixType.ToMatrix())
                ]

                group "Simulation controls" [
                    div [ ClassName "columns" ] [
                        div [ ClassName "column" ] [
                            Button.button
                                [
                                    // Button.IsFullWidth

                                    Button.OnClick (fun _ -> (dispatch RunSimulation))
                                ]
                                [str "Re-run simulation"]
                        ]
                        div [ ClassName "column" ] [
                            Button.button
                                [
                                    // Button.IsFullWidth
                                    Button.Color IsDanger
                                    Button.OnClick (fun _ -> (dispatch ToInitialization))
                                ]
                                [str "Reset setup"]
                        ]
                    ]
                ]
            ]

module ResultTable =
  let agentBox (rounds: int) (model: Agent) =
    let avg =
      match rounds with
      | 0 -> 0.0
      | r -> model.Payoff / (float r)
    div [ ClassName (sprintf "agent-box fade-in %A %A" model.Color model.Strategy)] [
      div [ ClassName "strategy" ] [ str (sprintf "%A" model.Strategy) ]
      div [ ClassName "payoff"]
          [
            div [] [
                span [] [str "Total: "]
                ofFloat model.Payoff
              ]
            div [] [
                span [] [str "Avg: "]
                ofString (sprintf "%.3f" avg)
              ]
          ]
      div [ ClassName "agent-id"] [ str "#"; ofInt model.Id ]
    ]

  let view (model: State) dispatch =
    let currentRound = model.CurrentRound
    let currentRoundAgents = model.CurrentRoundAgents()

    let agents =
        currentRoundAgents
        |> List.map (agentBox currentRound)
    div [ ClassName "agent-listing" ] agents

// Main view
let view (model: State) dispatch =
  let currentRound = model.CurrentRound
  let currentRound = model.CurrentRound
  let roundData = model
  let resultPanel =
        match model.ViewState with
        | InitGame ->
            Button.button
                [
                    Button.Size IsLarge
                    Button.OnClick (fun _ -> dispatch RunSimulation)
                ]
                [ str "Run simulation" ]
        | ShowResults round ->
            div [] [
              h1 [] [
                  str (sprintf "Results (Round %i/%i)" round model.Setup.RoundsToPlay)
              ]
              RoundSlider.slider model dispatch
              ResultTable.view (model: State) dispatch
              pre [] [str (sprintf "%A" model.State.ResolvedRounds)]
            ]

  div [ Id "main-container"; ClassName "columns"; ] [
    SettingsForm.view model dispatch
    div [ Id "results"; ClassName "column" ] [
        resultPanel
    ]
  ]
