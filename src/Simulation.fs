module Simulation

open Model

module GameModes =
    open Statistics.ModelExtensions

    // Helpers
    // Composite strategy make it easier to test and read strategies
    type CompositeStrategySetup =
        {
            NoHistoryStrategy: GameInformation -> Strategy;
            SameColorStrategy: GameInformation -> Strategy;
            DifferentColorStrategy: GameInformation -> Strategy
        }
    let compositeStrategy (setup: CompositeStrategySetup) (info: GameInformation) =
        match (info.History.HasHistory, info.OpponentColor) with
        | (false, _) ->
            setup.NoHistoryStrategy info
        | (true, opponentColor) when opponentColor = info.Agent.Color ->
            setup.SameColorStrategy info
        | _ ->
            setup.DifferentColorStrategy info


    /// Random Choice game is used in e.g. in stage 2 when euHawk = euDove
    let randomChoiceGame (info: GameInformation): Strategy =
        if info.RandomNumber < 0.5 then // Random number range [0.0, 1.0[
            Dove
        else
            Hawk


    let nashMixedStrategyEquilibriumGameFromPayoff (info: GameInformation) : Strategy =
        let ``change of hawk`` =
            let (hawkMax, doveMin) = info.PayoffMatrix.GetPayoffFor(Hawk, Dove)
            let (doveMax, _)       = info.PayoffMatrix.GetPayoffFor(Dove, Dove)
            let (hawkMin, _)       = info.PayoffMatrix.GetPayoffFor(Hawk, Hawk)
            match (hawkMin - doveMin) with
            | 0.0 -> 1.0
            | _ ->
                let hawksPerDove = (doveMax - hawkMax) / (hawkMin - doveMin)
                let portionOfHawks = hawksPerDove / (1.0 + hawksPerDove)
                if (portionOfHawks > 1.0) then
                    1.0
                else
                    portionOfHawks

        if (``change of hawk`` > info.RandomNumber) then // Random number range [0.0, 1.0[
            Hawk
        else
            Dove


    // let nashMixedStrategyEquilibriumPayoffAndHistory (info: GameInformation) =
    //     let ``change of hawk`` =
    //         let payoff = info.PayoffMatrix
    //         let stats = info.History.LastRoundChallenges.StrategyStatsFor (info.OpponentColor)
    //         // Caclulate expected payoff for playinf hawk and for playing dove
    //         let euHawk = stats.HawkPortion * payoff.GetMyPayoff (Hawk, Hawk) +
    //                      stats.DovePortion * payoff.GetMyPayoff (Hawk, Dove)
    //         let euDove = stats.HawkPortion * payoff.GetMyPayoff (Dove, Hawk) +
    //                      stats.DovePortion * payoff.GetMyPayoff (Dove, Dove)

    //         match euDove with
    //         | 0.0 -> 1.0
    //         | _ ->
    //             // Presuming that everyone play alike in last round
    //             let equilibirium = euHawk / euDove
    //             let portionOfHawks = equilibirium / (1.0 + equilibirium)
    //             if (portionOfHawks > 1.0) then
    //                 1.0
    //             else
    //                 portionOfHawks

    //     if (``change of hawk`` > info.RandomNumber) then // random number range [0.0, 1.0[
    //         Hawk
    //     else
    //         Dove

    let onHawksOnLastRound (info: GameInformation) =
        let lastRoundStats = info.History.LastRoundChallenges.StrategyStats ()
        let hawkPortion = lastRoundStats.HawkPortion
        if (hawkPortion > info.RandomNumber) then // random number range: [0.0, 1.0[
            Hawk
        else
            Dove

    let highestEuOnDifferentColorGame (info: GameInformation): Strategy =
            let payoff = info.PayoffMatrix
            let lastRound = info.History.LastRoundChallenges
            let opposingColorStats = lastRound.StrategyStatsFor(info.OpponentColor)
            let pHawk = opposingColorStats.HawkPortion // = Hawk count / total actors within color segement
            let pDove = opposingColorStats.DovePortion

            // Caclulate expected payoff for playinf hawk and for playing dove
            // In payoff.GetMyPayoff the first param is my move, and the second is opponent move
            // E.g. for V = 10, C = 20 payoff.GetMyPayoff (Hawk, Hawk) return -5 (= (V-C)/2) and payoff.GetMyPayoff(Dove, Hawk) returns 10 (0)
            let euHawk = pHawk * payoff.GetMyPayoff (Hawk, Hawk) +
                         pDove * payoff.GetMyPayoff (Hawk, Dove)
            let euDove = pHawk * payoff.GetMyPayoff (Dove, Hawk) +
                         pDove * payoff.GetMyPayoff (Dove, Dove)

            match (euHawk - euDove) with
            // When you have expected value for playing hawk and playing dove are equal
            // choose randomly
            | 0.0 -> randomChoiceGame info
            // if expected payoff for playing hawk is better, play hawk
            // otherwise play dove
            | diff when diff > 0.0 -> Hawk
            | _  -> Dove

    let stage2Game = compositeStrategy {
            NoHistoryStrategy = nashMixedStrategyEquilibriumGameFromPayoff
            SameColorStrategy = nashMixedStrategyEquilibriumGameFromPayoff
            DifferentColorStrategy = highestEuOnDifferentColorGame
        }

    let stage3Game (gameInformation: GameInformation): Strategy =
        match gameInformation.Agent.Strategy with
        // TODO: Should this be randomm
        | None -> nashMixedStrategyEquilibriumGameFromPayoff gameInformation
        | Some choice -> choice
