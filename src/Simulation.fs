module Simulation

open Model
type StragyPropability =
    {
        Hawk: float
        Dove: float
    }

type PropabilitiesForColor = Color * StragyPropability
type PropabilityMap = Map<Color, StragyPropability>

module NashEquilibrium =
    let calculateNashEquilibriumPortionOfHawksFromPayoff(payOff: PayoffMatrix) : float =
            let (hawkMax, doveMin) = payOff.[(Hawk, Dove)]
            let (doveMax, _)       = payOff.[(Dove, Dove)]
            let (hawkMin, _)       = payOff.[(Hawk, Hawk)]
            match (hawkMin - doveMin) with
            | 0.0 -> 1.0
            | _ ->
                let hawksPerDove = (doveMax - hawkMax) / (hawkMin - doveMin)
                let portionOfHawks = hawksPerDove / (1.0 + hawksPerDove)
                if (portionOfHawks > 1.0) then
                    1.0
                else
                    portionOfHawks

    let calculateNashEquilibriumPortionOfHawksFromPayoffAndPortionOfHawks
            (payOff: PayoffMatrix)
            (probabilityOfHawk: float)
            : float =
            let expectedValueForPayingHawk =
                match payOff.[(Hawk, Hawk)],  payOff.[(Hawk, Dove)] with
                | (whenOtherPlayedHawk, _), (whenOtherPlayedDove, _) ->
                    probabilityOfHawk * whenOtherPlayedHawk +
                    (1.0 - probabilityOfHawk) * whenOtherPlayedDove

            let expectedValueForPayingDove =
                match payOff.[(Dove, Hawk)],  payOff.[(Dove, Dove)] with
                | (whenOtherPlayedHawk, _), (whenOtherPlayedDove, _) ->
                    probabilityOfHawk * whenOtherPlayedHawk +
                    (1.0 - probabilityOfHawk)  * whenOtherPlayedDove

            match expectedValueForPayingDove with
            // If other is expected to play how I should play?
            //
            | 0.0 -> 1.0
            | _ ->
                // Peruming that everyone play alike in last round
                let expectedRatioOfHawks = expectedValueForPayingHawk / expectedValueForPayingDove
                let portionOfHawks = expectedRatioOfHawks / (1.0 + expectedRatioOfHawks)
                if (portionOfHawks > 1.0) then
                    1.0
                else
                    portionOfHawks


module Stats =
    let calcStatsForLastRound (rounds: GameHistory): ColorStatistics =
        let lastRound = rounds.Unwrap() |> Array.last
        lastRound.ColorStats()

    let calcStrategyPropability
        (color: Color, choises: Strategy list)
        : PropabilitiesForColor =
        let total = (float choises.Length)
        let countFor choice =
            choises
            |> List.filter (fun item -> item = choice)
            |> List.length
            |> float

        color, {
           Hawk = (countFor Hawk) / total
           Dove = (countFor Dove) / total
        }

    let mapToColorChoicePairs
        (challenge: ResolvedChallenge):
        (Color * Strategy) list =
        match challenge with
        | { ResolvedChallenge.Players = (p1, p2)
            Choices = (c1, c2)} ->
                [
                    (p1.Color, c1)
                    (p2.Color, c2)
                ]

    let selectStartegyFromPair
        (pairs: (Color * Strategy) list )
        : Strategy list =
        pairs
        |> List.map (fun (c, s) -> s)

    let mapToColorStrategyListPair
        (color: Color, pairs) =
        (color, selectStartegyFromPair pairs)

    let calcProbablities
        (gameState: GameInformation)
        : PropabilityMap option =
                match gameState.History with
                | Rounds [||] -> None
                | Rounds rounds ->
                    let lastRound = rounds |> Array.last
                    lastRound.ToList()
                    |> List.collect mapToColorChoicePairs
                    |> List.groupBy (fun (color, _) -> color)
                    |> List.map (mapToColorStrategyListPair
                                >> calcStrategyPropability)
                    |> Map.ofList
                    |> Some

    let myPayoff (matrix: PayoffMatrix) pair =
        let (myPayoff, _) = matrix.[pair]
        myPayoff

let rand = System.Random()
module GameModes =
    // let simpleGame (agent: Agent)
    //                (opponentColor: Color)
    //                (gameState: GameState)
    //                : Strategy =
    //    agent.Strategy

    let randomChoiseGame (gameInformation: GameInformation): Strategy =
        let rand = System.Random()
        let change = rand.NextDouble() // Range [0.0, 1.0]
        if change < 0.5 then
            Dove
        else
            Hawk

    let nashEqlibiumGame (gameInformation: GameInformation) : Strategy =
        let ``change of hawk`` =
            NashEquilibrium.calculateNashEquilibriumPortionOfHawksFromPayoff gameInformation.PayoffMatrix

        let chance = rand.NextDouble() // range [0.0, 1.0[
        if (``change of hawk`` > chance) then
            Hawk
        else
            Dove


    let nashEqlibiumOnBasedOfPayoffAndHistory (gameInformation: GameInformation) =
        let total =
            gameInformation.Agents
            |> List.filter (fun a -> a.Color = gameInformation.OpponentColor)
            |> List.length
            |> float

        let hawkPortion =
            let stats = Stats.calcStatsForLastRound gameInformation.History
            let countOfHawks = stats.HawkCountFor gameInformation.OpponentColor
            countOfHawks / total

        let ``change of hawk`` =
            NashEquilibrium.calculateNashEquilibriumPortionOfHawksFromPayoffAndPortionOfHawks
               gameInformation.PayoffMatrix
               hawkPortion
        let chance = rand.NextDouble() // range [0.0, 1.0[
        printfn "hawkP %f change %f" ``change of hawk`` chance
        if (``change of hawk`` > chance) then
            Hawk
        else
            Dove

    let onHawksOnLastRound (gameInformation: GameInformation) =

        let total =
            gameInformation.Agents
            |> List.filter (fun a -> a.Color = gameInformation.OpponentColor)
            |> List.length
            |> float
        let roundCount = gameInformation.History.GetRoundCount()

        let hawkPortion =
            let stats = Stats.calcStatsForLastRound gameInformation.History
            printfn "Round: %i stats: %O" roundCount stats
            let countOfHawks = stats.HawkCountFor gameInformation.OpponentColor
            countOfHawks / total

        let chance = rand.NextDouble() // range [0.0, 1.0[
        let choise =
            if (hawkPortion > chance) then
                Hawk
            else
                Dove

        printfn "Round: %i Total: %f; hawks (%%): %f (random %f), opponent color %O (vs. %O) => choise: %O"
                (gameInformation.History.GetRoundCount()) total hawkPortion chance gameInformation.OpponentColor gameInformation.Agent.Color choise
        choise


    let highestEuOnDifferentColorGame (onSameColorStragy: GameInformation -> Strategy) (gameInformation: GameInformation): Strategy =
        let probablities: PropabilityMap option =
            Stats.calcProbablities gameInformation

        match (probablities, gameInformation.OpponentColor) with
        | (None, _) ->
            // if there are not stats play nashEquilibium game
            nashEqlibiumGame gameInformation
        | (Some p, opponentColor) when opponentColor = gameInformation.Agent.Color -> onSameColorStragy gameInformation
        | (Some p, opponentColor) ->
            let agent = gameInformation.Agent
            let matrix = gameInformation.PayoffMatrix
            let getPayoff = Stats.myPayoff matrix
            let pHawk = (p.Item opponentColor).Hawk
            let pDove = (p.Item opponentColor).Dove

            // Caclulate expected payoff for playinf hawk and for playing dove
            let euHawk = pHawk * getPayoff (Hawk, Hawk) +
                         pDove * getPayoff (Hawk, Dove)
            let euDove = pHawk * getPayoff (Dove, Hawk) +
                         pDove * getPayoff (Dove, Dove)

            match (euHawk - euDove) with
            // When you have expected value for playing hawk and playing dove are equal
            // choose randomly
            | 0.0 -> randomChoiseGame gameInformation
            // if expected payoff for playing hawk is better, play hawk
            // otherwise play dove
            | diff when diff >= 0.0 -> Hawk
            | _  -> Dove

    let stage2Game = highestEuOnDifferentColorGame nashEqlibiumGame

    let stage3Game (gameInformation: GameInformation): Strategy =
        match gameInformation.Agent.Strategy with
        | None -> nashEqlibiumGame gameInformation
        | Some choice -> choice
