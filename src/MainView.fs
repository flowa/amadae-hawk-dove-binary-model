module MainView

open Model
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props

// VIEW (rendered with React)

let view (model: State) dispatch =
  div []
      [ button [ OnClick (fun _ -> dispatch Decrement) ] [ str "-" ]
        div [] [ ofInt model.Count ]
        button [ OnClick (fun _ -> dispatch Increment) ] [ str "+" ] ]
