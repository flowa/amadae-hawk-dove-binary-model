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
    let myPayoff (matrix: PayoffMatrix) pair =
        let (myPayoff, _) = matrix.[pair]
        myPayoff

let rand = System.Random()
module GameModes =
    open Statistics.ModelExtensions
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

    let nashMixedStrategyEquilibriumGame (gameInformation: GameInformation) : Strategy =
        let ``change of hawk`` =
            NashEquilibrium.calculateNashEquilibriumPortionOfHawksFromPayoff gameInformation.PayoffMatrix

        let chance = rand.NextDouble() // range [0.0, 1.0[
        if (``change of hawk`` > chance) then
            Hawk
        else
            Dove


    let nashMixedStrategyEquilibriumPayoffAndHistory (gameInformation: GameInformation) =
        let opposingColorStats = gameInformation.History.LastRoundChallenges.StrategyStatsFor (gameInformation.OpponentColor)
        let hawkPortion = opposingColorStats.HawkPortion

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
        let lastRoundStats = gameInformation.History.LastRoundChallenges.StrategyStats ()
        let hawkPortion = lastRoundStats.HawkPortion
        let chance = rand.NextDouble() // range [0.0, 1.0[
        let choise =
            if (hawkPortion > chance) then
                Hawk
            else
                Dove

        choise


    let highestEuOnDifferentColorGame (onSameColorStragy: GameInformation -> Strategy) (info: GameInformation): Strategy =

        match (info.History.HasHistory, info.OpponentColor) with
        | (false, _) ->
            // if there are not stats play nashEquilibium game
            nashMixedStrategyEquilibriumGame info
        | (true, opponentColor) when opponentColor = info.Agent.Color ->
            onSameColorStragy info
        | (true, opponentColor) ->
            let agent = info.Agent
            let matrix = info.PayoffMatrix
            let getPayoff = Stats.myPayoff matrix
            let lastRound = info.History.LastRoundChallenges
            let opposingColorStats = lastRound.StrategyStatsFor (opponentColor)
            let pHawk = opposingColorStats.HawkPortion
            let pDove = opposingColorStats.DovePortion

            // Caclulate expected payoff for playinf hawk and for playing dove
            let euHawk = pHawk * getPayoff (Hawk, Hawk) +
                         pDove * getPayoff (Hawk, Dove)
            let euDove = pHawk * getPayoff (Dove, Hawk) +
                         pDove * getPayoff (Dove, Dove)

            match (euHawk - euDove) with
            // When you have expected value for playing hawk and playing dove are equal
            // choose randomly
            | 0.0 -> randomChoiseGame info
            // if expected payoff for playing hawk is better, play hawk
            // otherwise play dove
            | diff when diff > 0.0 -> Hawk
            | _  -> Dove

    let stage2Game = highestEuOnDifferentColorGame nashMixedStrategyEquilibriumGame

    let stage3Game (gameInformation: GameInformation): Strategy =
        match gameInformation.Agent.Strategy with
        | None -> nashMixedStrategyEquilibriumGame gameInformation
        | Some choice -> choice
