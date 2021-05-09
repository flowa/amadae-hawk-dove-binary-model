module Simulation

open System
open Helpers
open Model
open Statistics

module Composition =
    type CompositeStrategySetup =
        {
            SameColorNoHistoryStrategy: GameInformation -> Strategy;
            DifferentColorNoHistoryStrategy: GameInformation -> Strategy;
            SameColorStrategy: GameInformation -> Strategy;
            DifferentColorStrategy: GameInformation -> Strategy
        }

    let compositeStrategy (setup: CompositeStrategySetup) (info: GameInformation) =
        match (info.HistoryView.History.HasHistory, info.OpponentColor) with
        | (false, opponentColor) when opponentColor <> info.Agent.Color ->
            setup.DifferentColorNoHistoryStrategy info
        | (false, opponentColor) when opponentColor = info.Agent.Color ->
            setup.SameColorNoHistoryStrategy info
        | (true, opponentColor) when opponentColor = info.Agent.Color ->
            setup.SameColorStrategy info
        | _ ->
            setup.DifferentColorStrategy info

module GameMode =
    open Statistics.ModelExtensions

    /// Random Choice game is used in e.g. in stage 2 when euHawk = euDove
    let randomChoiceGame (info: GameInformation): Strategy =
        let changeOfHawk = 0.5
        if info.RandomNumber < changeOfHawk then // Random number range [0.0, 1.0[
            Hawk
        else
            Dove

    let fixedGame (fixedStrategy: Strategy) (info: GameInformation): Strategy = fixedStrategy

    let cardDeckGame (deck: Strategy list) (info: GameInformation): Strategy =
        let cardIndex = info.Agent.Id
        deck.[cardIndex]

    let nashMixedStrategyEquilibriumGameFromPayoffParameters (info: GameInformation) : Strategy =
        let ``change of hawk`` =
            let C = info.PayoffMatrix.``Cost (C)``
            let V = info.PayoffMatrix.``Revard (V)``
            match C with
            | 0.0 -> 1.0
            | _ ->
                let portionOfHawks = V / C
                if (portionOfHawks > 1.0) then
                    1.0
                else
                    portionOfHawks

        if (info.RandomNumber < ``change of hawk``) then // Random number range [0.0, 1.0[
            Hawk
        else
            Dove

    let keepSameStrategy (info: GameInformation): Strategy  =
        match info.Agent.Strategy with
        // TODO: Should this be randomm
        | None -> nashMixedStrategyEquilibriumGameFromPayoffParameters info
        | Some choice -> choice

    let onBasedOfLastEncounterWithOpponentColor(info: GameInformation): Strategy  =
        let myColor = info.Agent.Color

        let lastRound = info.HistoryView.History.LastRoundChallenges.StrategyStatsFor(DifferentColor, myColor)

        match lastRound.DoveN, lastRound.HawkN  with
        | (0, _) -> Hawk
        | (_, 0) -> Dove
        | _ -> nashMixedStrategyEquilibriumGameFromPayoffParameters info

    let highestEuOnDifferentColorGameForInvidualAgent (challengeTypeFilter: ChallengeType option) (info: GameInformation): Strategy =
            let payoff = info.PayoffMatrix
            let history = info.HistoryView
            let opposingColorStats =
                match challengeTypeFilter with
                | None -> history.StrategyStatsFor(info.Agent, info.OpponentColor)
                | Some challengeType -> history.StrategyStatsFor(info.Agent, challengeType, info.OpponentColor)

            let pHawk = opposingColorStats.HawkPortion // = Hawk count / total actors within color segment
            let pDove = opposingColorStats.DovePortion

            // Caclulate expected payoff for playinf hawk and for playing dove
            // In payoff.GetMyPayoff the first param is my move, and the second is opponent move
            // E.g. for V = 10, C = 20 payoff.GetMyPayoff (Hawk, Hawk) return -5 (= (V-C)/2) and payoff.GetMyPayoff(Dove, Hawk) returns 10 (0)
            let evHawk = pHawk * payoff.GetMyPayoff (Hawk, Hawk) +
                         pDove * payoff.GetMyPayoff (Hawk, Dove)
            let evDove = pHawk * payoff.GetMyPayoff (Dove, Hawk) +
                         pDove * payoff.GetMyPayoff (Dove, Dove)

            match (evHawk - evDove) with
            // When you have expected value for playing
            // hawk and playing dove are equal
            // choose randomly
            | 0.0 -> randomChoiceGame info
            // if expected payoff for playing hawk is better, play hawk
            // otherwise play dove
            | diff when diff > 0.0 -> Hawk
            | _  -> Dove

    let highestEuOnDifferentColorGameForInvidualAgentNonCached (challengeTypeFilter: ChallengeType option) (info: GameInformation): Strategy =
            let payoff = info.PayoffMatrix
            let history = info.HistoryView.History
            let opposingColorStats =
                match challengeTypeFilter with
                | None -> history.StrategyStatsFor(info.Agent, info.OpponentColor)
                | Some challengeType -> history.StrategyStatsFor(info.Agent, challengeType, info.OpponentColor)

            let pHawk = opposingColorStats.HawkPortion // = Hawk count / total actors within color segement
            let pDove = opposingColorStats.DovePortion

            // Calculate expected payoff for playing hawk and for playing dove
            // In payoff.GetMyPayoff the first param is my move, and the second is opponent move
            // E.g. for V = 10, C = 20 payoff.GetMyPayoff (Hawk, Hawk) return -5 (= (V-C)/2) and payoff.GetMyPayoff(Dove, Hawk) returns 10 (0)
            let evHawk = pHawk * payoff.GetMyPayoff (Hawk, Hawk) +
                         pDove * payoff.GetMyPayoff (Hawk, Dove)
            let evDove = pHawk * payoff.GetMyPayoff (Dove, Hawk) +
                         pDove * payoff.GetMyPayoff (Dove, Dove)

            match (evHawk - evDove) with
            // When you have expected value for playing
            // hawk and playing dove are equal
            // choose randomly
            | 0.0 -> randomChoiceGame info
            // if expected payoff for playing hawk is better, play hawk
            // otherwise play dove
            | diff when diff > 0.0 -> Hawk
            | _  -> Dove

    let highestEuOnDifferentColorGameWithFilter (challengeTypeFilter: ChallengeType option) (info: GameInformation): Strategy =
            let payoff = info.PayoffMatrix
            let lastRound = info.HistoryView.History.LastRoundChallenges
            let opposingColorStats =
                match challengeTypeFilter with
                | None -> lastRound.StrategyStatsFor(info.OpponentColor)
                | Some challengeType -> lastRound.StrategyStatsFor(challengeType, info.OpponentColor)

            let pHawk = opposingColorStats.HawkPortion // = Hawk count / total actors within color segement
            let pDove = opposingColorStats.DovePortion

            // Caclulate expected payoff for playinf hawk and for playing dove
            // In payoff.GetMyPayoff the first param is my move, and the second is opponent move
            // E.g. for V = 10, C = 20 payoff.GetMyPayoff (Hawk, Hawk) return -5 (= (V-C)/2) and payoff.GetMyPayoff(Dove, Hawk) returns 10 (0)
            let evHawk = pHawk * payoff.GetMyPayoff (Hawk, Hawk) +
                         pDove * payoff.GetMyPayoff (Hawk, Dove)
            let evDove = pHawk * payoff.GetMyPayoff (Dove, Hawk) +
                         pDove * payoff.GetMyPayoff (Dove, Dove)

            match (evHawk - evDove) with
            // When you have expected value for playing
            // hawk and playing dove are equal
            // choose randomly
            | 0.0 -> randomChoiceGame info
            // if expected payoff for playing hawk is better, play hawk
            // otherwise play dove
            | diff when diff > 0.0 -> Hawk
            | _  -> Dove

    let highestExpectedValueOnDifferentColorGame = highestEuOnDifferentColorGameWithFilter None
    let highestExpectedValueOnDifferentColorGameUsingOnlyDifferentColorStats = highestEuOnDifferentColorGameWithFilter (Some DifferentColor)

// These modes are included in this file temporarily and are not used in the simulation
module ExtraGameModes =
    open Statistics.ModelExtensions

    let nashMixedStrategyEquilibriumGameFromPayoffMatrix (info: GameInformation) : Strategy =
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

    let dependingHawksWithinColorSegment (info: GameInformation) =
        let lastRoundStats = info.HistoryView.History.LastRoundChallenges.StrategyStatsFor (info.OpponentColor)
        let hawkPortion = lastRoundStats.HawkPortion
        if (hawkPortion > info.RandomNumber) then // random number range: [0.0, 1.0[
            Hawk
        else
            Dove

    let onHawksOnLastRound (info: GameInformation) =
        let lastRoundStats = info.HistoryView.History.LastRoundChallenges.StrategyStats ()
        let hawkPortion = lastRoundStats.HawkPortion
        if (hawkPortion > info.RandomNumber) then // random number range: [0.0, 1.0[
            Hawk
        else
            Dove

module IdealNashMixedDistribution =
    
    // Generates "deck" of Strategy Choice so that
    // portion of Hawks is exactly V / C and Portion of Doves is 1 - (V / C)
    let generate (setup: GameParameters) =
        let hawkCount =
            let playerCount = (float) setup.AgentCount
            match setup.PayoffMatrix.``Cost (C)`` with
            | 0.0 -> setup.AgentCount
            | _ ->
                let hawkCountFloat = (setup.PayoffMatrix.``Revard (V)`` / setup.PayoffMatrix.``Cost (C)``) * playerCount
                // Uncomment to get warning (Note: Some errors are false positive due to float operation inaccuracy)
                // if (hawkCountFloat <> Math.Floor(hawkCountFloat)) then
                //    printfn "WARNING: Could not setup NSME accurately for setup %O. Will use floor(%f)" setup hawkCountFloat
                Math.Floor(hawkCountFloat) |> int
                
        let doveCount =  setup.AgentCount - hawkCount
    
        Strategy.GenerateList [(Hawk, hawkCount); (Dove, doveCount)]
        |> ListHelpers.shuffle

module IdealFiftyFiftyDistribution =

    // Generates "deck" of Strategy Choice so that
    // portion of Hawks is exactly V / C and Portion of Doves is 1 - (V / C)
    let generate (setup: GameParameters) =
        let doveCount = setup.AgentCount / 2
        let hawkCount = setup.AgentCount - doveCount
        Strategy.GenerateList [(Hawk, hawkCount); (Dove, doveCount)]
        |> ListHelpers.shuffle

module SimulationStages =

    let allPlay (fixedStrategy: Strategy) (_setup: GameParameters) = GameMode.fixedGame fixedStrategy

    let stage1Game (_setup: GameParameters) = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters

    let simulation_setup_random (_setup: GameParameters) = Composition.compositeStrategy {
            SameColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorNoHistoryStrategy = GameMode.randomChoiceGame            
            SameColorStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorStrategy = GameMode.randomChoiceGame 
        }

    let stage2Game (_setup: GameParameters) = Composition.compositeStrategy {
            SameColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            SameColorStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorStrategy = GameMode.highestExpectedValueOnDifferentColorGameUsingOnlyDifferentColorStats
        }

    let stage1Game_withIdealNMSEDistribution (setup: GameParameters) =
        let deck = IdealNashMixedDistribution.generate(setup)
        (GameMode.cardDeckGame deck)

    let stage1Game_withIdealFiftyFiftyDistribution (setup: GameParameters) =
        let deck = IdealFiftyFiftyDistribution.generate(setup)
        (GameMode.cardDeckGame deck)

    let stage2Game_v2_AllEncounter (_setup: GameParameters) = Composition.compositeStrategy {
            SameColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            SameColorStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorStrategy = GameMode.highestExpectedValueOnDifferentColorGame
        }

    let stage2Game_v3_keepSameAsSameColorStrategy (_setup: GameParameters) = Composition.compositeStrategy {
            SameColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            SameColorStrategy = GameMode.keepSameStrategy
            DifferentColorStrategy = GameMode.highestExpectedValueOnDifferentColorGame
        }

    let stage2Game_v4_dependingHawksWithinColorSegmentAsSameColorStrategy (_setup: GameParameters) =
            Composition.compositeStrategy {
                SameColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
                DifferentColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
                SameColorStrategy = ExtraGameModes.dependingHawksWithinColorSegment
                DifferentColorStrategy = GameMode.highestExpectedValueOnDifferentColorGame
            }

    let stage2Game_v5_withFullIndividualHistory (_setup: GameParameters) =
            Composition.compositeStrategy {
                SameColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
                DifferentColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
                
                SameColorStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
                DifferentColorStrategy = GameMode.highestEuOnDifferentColorGameForInvidualAgent None
            }

    let stage2Game_v5_withFullIndividualHistory_NonCached (setup: GameParameters) =
        Composition.compositeStrategy {
            SameColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            SameColorStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorStrategy = GameMode.highestEuOnDifferentColorGameForInvidualAgentNonCached None
        }
        
    let stage3Game (setup: GameParameters) =
        Composition.compositeStrategy {
            SameColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            SameColorStrategy = GameMode.keepSameStrategy
            DifferentColorStrategy = GameMode.highestExpectedValueOnDifferentColorGame
        }
    
    let stage3Game_onBasedOfLastEncounterWithOpponentColor (setup: GameParameters) =
        Composition.compositeStrategy {
            SameColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorNoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            
            SameColorStrategy = GameMode.onBasedOfLastEncounterWithOpponentColor
            DifferentColorStrategy = GameMode.onBasedOfLastEncounterWithOpponentColor
        }

module SimulationStageNames =
    let AllPlayDove = "AllPlayDove"
    let AllPlayHawk = "AllPlayHawk"
    let Random = "RandomFiftyFifty"
    let GuaranteedFiftyFifty = "IdealFiftyFifty"
    let ProbabilisticNSME = "ProbabilisticNSME"
    let GuaranteedNSME = "GuaranteedNSME"
    let HighestExpectedValueOnBasedOfHistory = "HighestExpectedValueOnBaseOfHistory"

    // let GuaranteedNSME = "HighestExpectedValueOnBaseOfHistory"


module SimulationStageOptions =
    let AllOptions =
        [
            {
                StageStrategyFnOptions.Name = SimulationStageNames.AllPlayDove
                DisplayName = "All play Dove"
                StrategyInitFn = SimulationStages.allPlay Strategy.Dove
            }
            {
                StageStrategyFnOptions.Name = SimulationStageNames.AllPlayHawk
                DisplayName = "All play Hawk"
                StrategyInitFn = SimulationStages.allPlay Strategy.Hawk
            }
            {
                StageStrategyFnOptions.Name = SimulationStageNames.Random
                DisplayName = "Random 50/50"
                // (50% change to play Hawk for each agent)
                StrategyInitFn = SimulationStages.simulation_setup_random
            }
            {
                StageStrategyFnOptions.Name = SimulationStageNames.GuaranteedFiftyFifty
                DisplayName = "Ideal 50%/50% distribution"
                // (50% will play Hawk and rest Dove)"
                StrategyInitFn = SimulationStages.simulation_setup_random
            }
            {
                StageStrategyFnOptions.Name = SimulationStageNames.ProbabilisticNSME
                DisplayName = "NMSE strategy"
                // (Reward / Cost change to play Hawk for each agent)"
                StrategyInitFn = SimulationStages.stage1Game
            }
            {
                StageStrategyFnOptions.Name = SimulationStageNames.GuaranteedNSME
                DisplayName = "Ideal NMSE distribution"
                StrategyInitFn = SimulationStages.stage1Game
            }
            {
                StageStrategyFnOptions.Name = SimulationStageNames.HighestExpectedValueOnBasedOfHistory
                DisplayName = "Highest expected value"
                StrategyInitFn = SimulationStages.stage1Game
            }
        ]

    let getFn (name: string) =
        (AllOptions
        |> List.find (fun o -> o.Name = name)).StrategyInitFn

module ModelExtensions =
    open Model
    type SimulationFrame with
        member this.StrategyInitFn
            with get() = SimulationStageOptions.getFn this.StrategyInitFnName

    type GameSetup with

        member this.ToInitialGameState () =
            let payoffMatrix = this.PayoffMatrix
            let plannedRounds =
                    this.SimulationFrames
                    |> List.filter (fun f -> f.RoundCount > 0)
                    |> List.collect
                        (fun frame ->
                            let plannedRound: PlannedRound =
                                {
                                   PayoffMatrix = frame.SetPayoffForStage payoffMatrix
                                   StrategyFn = frame.StrategyInitFn this.GameParameters
                                   StageName = frame.StageName
                                   MayUseColor = frame.MayUseColor
                                }
                            List.replicate frame.RoundCount plannedRound
                        )
            {
                PayoffMatrix   = payoffMatrix
                PlannedRounds  = plannedRounds
                ResolvedRounds = Rounds [||]
            }