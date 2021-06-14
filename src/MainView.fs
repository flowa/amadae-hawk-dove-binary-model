module MainView

open Fulma
open Model
open Simulation
open Statistics
open Statistics.ModelExtensions
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
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
    let playOrStopButton (model: State) (dispatch) =
        div [ClassName "animation-buttons column is-narrow"] [
            if model.PlayAnimation then
                button [
                    Key "stop"
                    ClassName "button is-small is-danger"
                    OnClick (fun e -> e.preventDefault(); dispatch StopAnimation)] [
                    i [ Key "stop"; ClassName "fa fa-stop" ] []
                ]
            else
                button [
                    Key "play"
                    ClassName "button is-small is-primary"
                    OnClick (fun e -> e.preventDefault(); dispatch PlayAnimation) ] [
                    i [ Key "play"; ClassName "fa fa-play" ] []
                ]
        ]

    let slider (model: State) (dispatch) =
        let max = float (model.Setup.RoundsToPlay)
        let onChange (e: Browser.Types.Event) =
            dispatch (ShowRound (int e.Value))
        div [ ClassName "block"]
            [
                div [ClassName "animation-controls columns"] [
                    playOrStopButton model dispatch
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

    let renderPayoffMatrics (payoff: PayoffMatrixType)  =
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
                        cell (payoff.GetPayoffFor(Hawk, Hawk) |> format)
                        cell (payoff.GetPayoffFor(Hawk, Dove) |> format)
                    ]
                row [
                        cellHeader "Dove" "Dove"
                        cell (payoff.GetPayoffFor(Dove, Hawk) |> format)
                        cell (payoff.GetPayoffFor(Dove, Dove) |> format)
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

    let hawkDoveStats className (stats: RoundStats.StrategyStats) = // (hawkN: int, doveN: int) =
        let format (n, portion) =
            sprintf "%i (%.0f%%)" n ((float) portion * 100.0)


        table [ClassName ("hawk-dove-stats " + className)] [
            tbody []
                [
                    row [ cell ((stats.HawkN, stats.HawkPortion) |> format) ]
                    row [ cell ((stats.DoveN, stats.DovePortion) |> format) ]
                    row [ td [ClassName "Sum"] [ofInt stats.TotalN] ]
                ]
            ]

    let simulatioStatsTable (model: State) =
        let intentTd = td [ClassName "cell-intent"] []
        let subTitle title =
            tr [Class "subtitle-row"] [th [ColSpan 4] [str title]]

        let challenges = model.CurrentRoundChallenges
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
                        cellHtml hawkDoveStatsHeader
                        cellHtml (hawkDoveStats "Red"  (challenges.StrategyStatsFor (DifferentColor, Red)))
                        cellHtml (hawkDoveStats "Blue" (challenges.StrategyStatsFor (DifferentColor, Blue)))
                        cellHtml (hawkDoveStats "All"  (challenges.StrategyStatsFor DifferentColor))
                    ]
                subTitle "Same color"
                row [
                        cellHtml hawkDoveStatsHeader
                        cellHtml (hawkDoveStats "Red"  (challenges.StrategyStatsFor(SameColor, Red)))
                        cellHtml (hawkDoveStats "Blue" (challenges.StrategyStatsFor(SameColor, Blue)))
                        cellHtml (hawkDoveStats "All"  (challenges.StrategyStatsFor(SameColor)))
                    ]

                subTitle "All"
                row [
                        cellHtml hawkDoveStatsHeader
                        cellHtml (hawkDoveStats "Red"  (challenges.StrategyStatsFor Red))
                        cellHtml (hawkDoveStats "Blue" (challenges.StrategyStatsFor Blue))
                        cellHtml (hawkDoveStats "All"  (challenges.StrategyStats ()))
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

    let renderColotStats (model: State) (opt: RenderColotStatsOptions) =
        let setting = model.Setup
        table [ClassName "agent-summary"] [
            thead [] [
                row [
                        emptyCellHeader
                        cellHeader "Red" "Red"
                        cellHeader "Blue" "Blue"
                        if opt.WithTotal then cellHeader "AllTotal" "All"
                    ]
            ]
            tbody [] [
                row [
                        cellHeader "attribute" "Count"
                        cell setting.CountOfRed
                        cell setting.CountOfBlue
                        if opt.WithTotal then cell setting.AgentCount
                    ]

                if opt.WithPortions then
                    let redAvg, blueAvg, allAvg = model.CurrentRoundChallenges.PayoffAccumulativeAvgForRedAndBlue
                    let rounds = model.CurrentRound
                    let format = sprintf "%.1f"
                    ofList [
                        row [
                            cellHeader "attribute" "Avg. total"
                            cell (format redAvg)
                            cell (format blueAvg)
                            cell (format allAvg)
                        ]
                        row [
                            cellHeader "attribute" "Avg. per round"
                            cell (format (redAvg / (decimal rounds)))
                            cell (format (blueAvg / (decimal rounds)))
                            cell (format (allAvg / (decimal rounds)))
                        ]
                    ]
            ]
        ]

module Fields =
    type NumberFieldProps =
        {
            Disabled:bool
            Label: string
            Value: int
            OnChange: (int -> FieldValue)
        }
    type DropdownFieldProps =
        {
            Disabled:bool
            Label: string
            SelectedValue: string
            Options: (string * string) list
            OnChange: (string -> FieldValue)
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
                                    Input.Disabled props.Disabled
                                    Input.Props [ClassName "is-small"]
                                    Input.OnChange
                                        (fun e -> dispatch (SetValue (props.OnChange (strToInt e.Value))))
                                    Input.Value (props.Value.ToString())]
                                ]
                        ]
                    ]
            ]
    let dropdownField
        (dispatch: Msg -> unit)
        (props: DropdownFieldProps) =
        let selectedDisplayName =
            props.Options
            |> List.filter (fun (name, displayName) -> name = props.SelectedValue)
            |> List.map (fun (name, displayName) -> displayName)
            |> List.head

        let isActive (name) = name = props.SelectedValue
        Field.div
            []
            [
                div [ClassName "field-label is-small left"]
                    [
                        Label.label [Label.CustomClass "is-expanded"; Label.Props [Style [TextAlign TextAlignOptions.Left ]]] [ str props.Label ]
                    ]
                div [ClassName "field-body is-expanded" ]
                    [
                        Dropdown.dropdown [ Dropdown.IsHoverable; Dropdown.Props [Style [Width "100%"]]]
                            [
                              Dropdown.trigger [ Props [Style [Width "100%"]]]
                                [
                                  Button.button [ Button.Size IsSmall; Button.Props [Style [Width "100%"] ]]
                                      [
                                          span [ ClassName "is-small"] [ str selectedDisplayName ]
                                          Icon.icon [ Icon.Size IsSmall ] [ Fa.i [ Fa.Solid.AngleDown ] [] ]
                                      ]
                                ]
                              Dropdown.menu []
                                  [
                                      Dropdown.content []
                                         (props.Options
                                         |> List.map (fun (name, displayName) ->
                                             Dropdown.Item.a [
                                                 Dropdown.Item.IsActive (isActive name);
                                                 Dropdown.Item.Props [
                                                    OnClick (fun _ -> dispatch (SetValue (props.OnChange name)))
                                                 ]
                                             ] [ str displayName ]))
                                  ]
                            ]
                    ]
            ]

module Authors = 
    let render () =
        div [ ClassName "column authors" ] [
            p [] [(str "Amadae Hawk-Dove Binary Model 1.0 by S.M. Amadae")]
            p [] [(str "Programmed by Ari-Pekka Lappi (Flowa Oy)")]
        ]

module SettingsForm =
    open Common
    open Tables
    let view (model: State) (dispatch: Msg -> unit) =
        let numberField = Fields.numberField dispatch
        let dropdownField = Fields.dropdownField dispatch
        let renderStageRoundCountFields (isDisabled: bool) =
            model.Setup.SimulationFrames
            |> List.map<SimulationFrame, ReactElement>
                (fun f ->
                    div []
                        [
                            h3 [] [str f.StageName]
                            dropdownField
                                {
                                   Disabled = isDisabled
                                   Label = "Mode"
                                   SelectedValue = f.StrategyInitFnName
                                   Options = SimulationStageOptions.AllOptions |> List.map (fun o -> (o.Name, o.DisplayName))
                                   OnChange = (fun value -> (ModeOfStage (f.StageName, value)))
                                }
                            numberField
                                {
                                    Disabled = isDisabled
                                    Label =    "Rounds"
                                    Value =    f.RoundCount
                                    OnChange = (fun value -> (RoundCountOfStage (f.StageName, value)))
                                }
                        ])

        let renderSetupForm (isDisabled: bool) =
            div [
                    ClassName "column is-one-quarter"
                    Id "settings"
                ]
                [
                    h1 [] [
                        str "Settings"
                    ]

                    group "Duration" (renderStageRoundCountFields isDisabled)

                    group "Agents" [
                            numberField
                                {
                                    Disabled =  isDisabled
                                    Label =    "Agent count"
                                    Value =    model.Setup.AgentCount
                                    OnChange = (fun value -> (AgentCount value))
                                }

                            numberField
                                {
                                    Disabled =  isDisabled
                                    Label =    "Red agents (%)"
                                    Value =    model.Setup.PortionOfRed
                                    OnChange = (fun value -> (PortionOfRed value))
                                }
                            (renderColotStats model RenderColotStatsOptions.None)
                        ]

                    group "Payoff" [
                            numberField
                                {
                                    Disabled =  isDisabled
                                    Label =    "Reward (V)"
                                    Value =    (int model.Setup.PayoffMatrix.VictoryBenefit)
                                    OnChange = (fun value -> (BenefitOnVictory value))
                                }

                            numberField
                                {
                                    Disabled =  isDisabled
                                    Label =    "Cost (C)"
                                    Value =    (int model.Setup.PayoffMatrix.Cost)
                                    OnChange = (fun value -> (CostOfLoss value))
                                }
                            renderPayoffMatrics (model.Setup.PayoffMatrix)
                        ]
                    
                    group "Authors" [
                        Authors.render ()
                    ]
                ]

        let colorSeparation =
            match model.GameState.ResolvedRounds.FirstRoundWithNConsecutiveRoundOfSeparatedColors 1 with
            | None -> "None"
            | Some i -> i.ToString()

        match model.ViewState with
        | InitGame -> renderSetupForm false
        | Loading -> renderSetupForm true
        | ShowResults viewState ->
            div [ ClassName "column is-one-quarter"; Id "settings" ] [
                h1  []
                    [
                        str "Simulation statistics"
                    ]
                group "Round challenges" [
                    simulatioStatsTable model
                    div [] [
                        str (sprintf "1st round of color separation in Different color encounters: %s" colorSeparation)
                    ]
                ]

                group "Color statistics" [
                    renderColotStats model RenderColotStatsOptions.WithTotalsAndPortions
                ]

                h1  []
                    [
                        str "Setup"
                    ]

                group "Payoff" [
                        renderPayoffMatrics (model.CurrentStagePayoffMatrix)
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
  let agentBox (usedColor: bool) (rounds: int) (model: Agent) =
    let colorTypeColor =
        if usedColor then model.Color.ToString() else "UnknownColor"
    let avg =
      match rounds with
      | 0 -> 0.0m
      | r -> model.Payoff / (decimal r)
    div [
            Key (model.Id.ToString())
            ClassName (sprintf "agent-box fade-in %s %A" colorTypeColor model.Strategy.Value)
        ] [
          div [ ClassName "strategy" ] [ str (sprintf "%A" model.Strategy.Value) ]
          div [ ClassName "payoff"]
              [
                div [] [
                    span [] [str "Total: "]
                    ofFloat (float model.Payoff)
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
    let currentRoundAgents = model.CurrentRoundAgents
    let mayUseColor = model.CurrentStageMayUseColor;

    let agents =
        currentRoundAgents
        |> List.groupBy (fun a -> a.LastRoundChallengeType)
        |> List.map (fun (group, agents) ->
                        group,
                        agents |> List.map (agentBox mayUseColor currentRound))
        |> Map.ofList

    div [ ClassName "agent-group" ] [
        h1 [] [str "Different color"]
        div [ ClassName "agent-listing" ]  agents.[(Some DifferentColor)]
        h1 [] [str "Same color"]
        div [ ClassName "agent-listing" ]  agents.[(Some SameColor)]
    ]

// Main view
let view (model: State) dispatch =
  let resultPanel =
        match model.ViewState with
        | InitGame ->
            div []
                [
                    h1 [] [
                        str (sprintf "Results")
                    ]
                    br []
                    Button.button
                        [
                            Button.Color IsPrimary
                            Button.Size IsLarge
                            Button.OnClick (fun _ -> dispatch RunSimulation)
                        ]
                        [ str "Run simulation" ]
                ]
        | Loading ->
            div []
                [
                    h1 [] [
                        str (sprintf "Results")
                    ]
                    br []
                    Progress.progress [Progress.Size IsLarge] []
                    str "Running simulation..."
                ]
        | ShowResults round ->
            div [] [
              h1 [] [
                  str (sprintf "Results (Round %i/%i - %s)" round model.Setup.RoundsToPlay model.CurrentStageName)
              ]
              RoundSlider.slider model dispatch
              ResultTable.view (model: State) dispatch
              // pre [] [str (sprintf "%A" model.GameState.ResolvedRounds)]
            ]

  div [ Id "main-container"; ClassName "columns"; ] [
    SettingsForm.view model dispatch
    div [ Id "results"; ClassName "column" ] [
        resultPanel
    ]
  ]