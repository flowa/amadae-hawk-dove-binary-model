module Simulation.Test

open Simulation
open Model
open Fable.Jester

Jest.describe("NashEquilibrioum", fun () ->
    Jest.test("GenerateList", fun () ->
        let matrix = (FromRewardAndCost (10.0, 20.0)).ToMatrix()
        let actual = NashEquilibrium.calculateNashEquilibriumPortionOfHawksFromPayoff matrix
        Jest.expect(actual).toEqual(0.5)
    )
)

module TestData  =
    module Agents =
        let BH = {Agent.Color = Blue; Strategy = Hawk; Id = 0; Payoff = 0.0 }
        let RH = {Agent.Color = Red; Strategy = Hawk; Id = 1; Payoff = 0.0 }
        let BD = {Agent.Color = Blue; Strategy = Dove; Id = 3; Payoff = 0.0 }
        let RD = {Agent.Color = Red; Strategy = Dove; Id = 2; Payoff = 0.0 }

    module Challenges =
        let RedVsBlue =
            [|
                {
                    ResolvedChallenge.Players =  (Agents.RD, Agents.BH)
                    Choices = (Hawk, Hawk)
                }
                {
                    ResolvedChallenge.Players = (Agents.RD, Agents.RH)
                    Choices = (Hawk, Dove)
                }
                {
                    ResolvedChallenge.Players =  (Agents.RD, Agents.BH)
                    Choices = (Dove, Hawk)
                }
                {
                    ResolvedChallenge.Players = (Agents.RD, Agents.BH)
                    Choices = (Dove, Dove)
                }

            |]

open TestData

Jest.describe("Stats", fun () ->
    Jest.test("countOfAgentMatching", fun () ->
        Jest.expect(
                Stats.countOfAgentMatching (Red, Hawk) Challenges.RedVsBlue.[0] // Red plays Hawk, Blue Hawk
            ).toBe(1.0)

        Jest.expect(
            Stats.countOfAgentMatching (Red, Hawk) Challenges.RedVsBlue.[2] // Red plays Dove, Blue Hawk
        ).toBe(0.0)
    )

    Jest.test("calcStatsForChallenge", fun () ->
        Jest.expect(
                Stats.calcStatsForChallenge Challenges.RedVsBlue.[0] // Red plays Hawk, Blue Hawk
            ).toEqual({ColorStatistics.CountOfRedHawks = 1.0; CountOfBlueHawks = 1.0})

        Jest.expect(
                Stats.calcStatsForChallenge Challenges.RedVsBlue.[1] // Red plays Hawk, Blue Dove
            ).toEqual({ColorStatistics.CountOfRedHawks = 1.0; CountOfBlueHawks = 0.0})

        Jest.expect(
                Stats.calcStatsForChallenge Challenges.RedVsBlue.[2] // Red plays Dove, Blue Hawk
            ).toEqual({ColorStatistics.CountOfRedHawks = 0.0; CountOfBlueHawks = 1.0})

        Jest.expect(
            Stats.calcStatsForChallenge Challenges.RedVsBlue.[3] // Red plays Dove, Blue Dove
        ).toEqual({ColorStatistics.CountOfRedHawks = 0.0; CountOfBlueHawks = 0.0})
    )
)