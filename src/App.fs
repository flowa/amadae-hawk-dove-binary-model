module App
open Model
open Elmish

// MODEL

let init() : State = { Count = 0 }

// UPDATE

let update (msg:Msg) (model: State) =
    match msg with
    | Increment -> { model with Count = model.Count +  1}
    | Decrement -> { model with Count = model.Count - 1 }

open MainView
open Elmish.React

// App
Program.mkSimple init update MainView.view
|> Program.withReactBatched "app"
|> Program.withConsoleTrace
|> Program.run