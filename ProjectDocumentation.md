# Amadae Hawk-Dove Binary model of systemic discrimination

## HOW TO INSTALL THE MODEL
See [README.md](README.md)

INTRODUCTION
The Amadae Hawk-Dove Binary (HDB) model of discrimination uses game theoretic agent-based modeling to study how localized individual choices result in overarching social patterns.  This model provides a minimalist account of the sufficient conditions to yield a systemic pattern of discrimination.
In this agent-based HDB model, after the introduction of the binary tag throughout the population, even though proportions of the two group sizes do not change, still the symmetry of the mixed strategy is broken, and one of the two pure Nash equilibria arise. HDB shows that in contexts represented by a Hawk Dove, in which competition over scarce resources involves costly conflict, that introducing a binary tag is sufficient to create an overarching pattern of hierarchy between members of the two groups.  See also Cailin O’Connor (2019).


## Simple simulation operating instructions

HDB is set up to run on default setting, which can be changed by the user. The payoff matrix is calculated according to the user’s input.  Either using the default settings, or entering new values, the user can then run the simulation. 
The user can enter values for:
Number of rounds for Stage 1
Number of rounds for Stage 2
Agent count
Red agents (%)
Reward V (greater than 0)
Cost C (0 or higher)

## Brief description of the model

This computer simulation is of a multi-agent Hawk-Dove game played for a given number of rounds in which agents are randomly paired during each round.  The model has two stages, Stage 1 and Stage 2, each of a number of rounds R specified by the user.  In both stages the population number is input by the user, and a number of 100-150 is recommended for results given the need to have a sufficiently large population, but not to over-burden computational efficiency.  The Hawk-Dove payoff matrix is specified above with the variables V for reward and C for cost.  In the model V has a default value of 10, and C is variable according to user input with a default of 5.  The purpose of the model is to demonstrate the impact of introducing a binary marker, here Red and Blue, tagging every player, on the progression of play.  The proportion of Red and Blue players is input by the user.
The variables running this model are:
N:  Number of players, each labeled with a unique number ranging from 1 to N.
I :  Every individual is each labeled I1, I2, I3…to IN.
R’ and R’’:  Number of rounds, here designated as R’ for Stage 1 and R’’ for Stage 2.  Each round is numbered from R’1, R’2, R’3…to R’R’ in Stage 1, and R’’1, R’’2, R’’3,…to R’’R’’ in Stage 2. The total number of rounds for Stage 1 and Stage 2 is input by the user.  Each round is visible by manipulating the sliding bar on the top of the simulation data outcome panel.  
V:  Reward, input by user, value must be greater than 0, default value is 10
C:  Costs, specified by user, value must be 0 or greater, default is 20
PR and PB: Proportion of agents; PR is the proportion of Red agents, input by user as a defined number of actors; (1-PR) is the proportion of Blue agents, PB.  Each individual in the population I1 through IN  is randomly assigned a Red or Blue designation to uphold the designated proportion.
H:  Accumulated history of Hawk and Dove play by other-type actors; HH is the % of Hawk play  and HD = (1-HH) is the % of Dove play experienced in all previous rounds when encountering other actors. HD and HH are unique for every player Ix in every round R’’1 to R’’R’’.
Stage 1 of the model anticipates that each player is tagged as either Red or Blue, but no agent acts on this information; all actors are colorblind.  Stage 1 simulates the multi-agent N-player Hawk Dove game in which in every round of play, players encounter each other in randomized pairs.  The program tabulates the cumulative and average score for each player.  It anticipates their Red and Blue labels, and provides and average and cumulative score for Red-type and Blue-type actors.  Each player is programmed to play the NMSE equilibrium of the game in accordance with user’s input of V and C: each plays Hawk (V/C) % of the time and Dove (1-V/C)% of the time.  The frequency of play is governed by shuffling a deck preassigned the appropriate ratio of Hawk versus Dove play, and correlating the values randomly to each player.
Stage 2 of the model displays the color labels in the proportion of Red and Blue input by the user.  Stage 2 plays for the number of rounds (R’’) input by the user.  Stage 1 or Stage 2 can be played by using a value of 0 for the other stage.  As in Stage 1, agents play successive rounds in randomized pairs.  In the initial round of play (R’’ = 1), players play NMSE with like-players, and play 50% Hawk-50% Dove with unalike-players.  All players play 50% Hawk-50% Dove in their first encounter with unalike players, regardless of which round in Stage 2 this occurs.   This initialization of the strategy for the first unalike encounter is justified by a lack of experience with other-colored agents.  Throughout the play progresses, in every round agents play NMSE when they encounter alike players.  
When agents encounter unalike players, after their first such encounter, they use an expected utility (EU) calculation based on their history of encounters with the other type actor, HH and HD.  The EU calculation is completed for each round R’’1 to R’’R’’ in Stage 2, with a unique calculation in each round for each player.
Calculation of EU for player Ix, round R’’ = y when playing unalike player for each strategy choice of Dove and Hawk is a function of each player’s history of other-colored encounters specified for each round of play:
EU (Ix, R’’y) (Dove) = (HD * V/2 ) + (HH  * 0)
EU (Ix, R’’y)  (Hawk) = (HD * V)  +  (HH * [{V-C}/2])
In subsequent rounds of encounter paired with an unlike agent (after the first such round), in each round R’’2…R, every agent Ix plays Hawk or Dove according to whether EU Ix  (Hawk) or EU Ix (Dove) is higher, and randomizes between 50% Hawk and 50% Dove if they are equal.


## Analysis

The Hawk-Dove Binary (HDB) model builds on a research using evolutionary games to provide the basis of this agent-based approach (Maynard Smith 1974).  Evolutionary game theory demonstrates that given multi-agent Hawk Dove game played indefinitely among randomized pairs in a population of actors is in equilibrium when all actors play the Nash Mixed Strategy Equilibrium (NMSE).  When an arbitrary binary marker is introduced throughout the population, one of the pure strategy Nash equilibriums results, with members of one group always playing Hawk and members of the other group always playing Dove, or vice versa.  This model has been used by the evolutionary game theorist John Maynard Smith to provide a potential mechanism for the evolution of property rights (Smith 1982; Gintis 2007).  
The HDB model of systemic discrimination is simplified and abstract.  Here the Hawk Dove game is applied to agent-based modeling rather than to evolutionary replicator dynamics.  Its target is to explain a possible mechanism for the emergence and persistence of discriminatory conventions.  This explanatory tactic resembles the Checkerboard model of segregation developed by James Sakoda and Thomas Schelling (Sakoda 1978; Rainer Hegselmann 2017; Schelling 1969; Schelling 1971; Aydinonat 2008)  It also is similar to Robert Axelrod’s analysis of the evolution of cooperation using the Prisoner’s Dilemma (Robert Axelrod 1984).  Social scientists use models to represent the empirical world in a simplified form (Sugden 2000; Ylikoski and Aydinonat 2014).  Models can provide “how possibly” representations of how outcomes arise from specified initial conditions and variables determining a system’s evolution.  Models are also useful for analyzing possible causal mechanisms determining outcomes.  

HDB does not assume discriminatory attitudes, as for example racist or sexist inclinations.  Hawk-Dove shows that in a population wherein all members are indistinguishable, relatively egalitarian distributional outcomes will result.  However, in these repeating bargaining scenarios in which agents resort costly conflict when they spar over scarce resources, once a binary tag is introduced an asymmetric equilibrium evolves.  Resources can range from money and status to tangible goods and territory.  Varying forms of HDB with differing population ratios and costs of conflict can be tested in empirical studies to see under what conditions the abstract model makes accurate predictions of agents’ actions (see e.g. Hargreaves Heap and Varoufakis 2002).



## Licence

Source code MIT


## References

Axelrod, Robert (1984), The evolution of cooperation, New York:  Basic Books
Aydinonat, N.E. (2008), “The invisible hand in economics: How economists explain unintended social consequences,” Abington:  Routledge
Gallo, E. (2014), Communication networks in markets, Work. Pap. Econ. 1431, University of Cambridge
Gintis, Herbert (2007), “The Evolution of Private Property.” Journal of Economic Behavior and Organization, Vol. 64, 1-16
Hegselmann, Rainer (2017), “Thomas C. Schelling and James M. Sakoda: The intellectual, technical, and social history of a model,” Journal of artificial societies and social simulation 20(3), http://jasss.soc.surrey.ac.uk/20/3/15.html
Maynard Smith, John (1982), Evolution and the theory of games, Cambridge:  Cambridge University Press
Hargreaves Heap, Shaun and Yanis Varoufakis (2002),  "Some experimental evidence on the evolution of discrimination, co‐operation and perceptions of fairness," The economic Journal 112:481, 679-703
Maynard Smith, John (1974) “The Theory of Games and the Evolution of Animal Conflicts,” Journal of Theoretical Biology, 47:1, 209-211.
O’Connor, Cailin (2019), The origins of unfairness: Social categories and cultural evolution, Oxford:  Oxford University Press
Sakoda, James M. (1978), “CHEBO: The checkboard model of social interaction,” in D. E. Bailey, ed., Computer Science in Social and Behavioral Science Education Englewood Cliffs, NJ:  Educational Technologies Publications, chapter 28, 357-373
Schelling, Thomas C., (1969), “Models of Segregation,” American economic review, 59(2):499-493
Schelling, Thomas C., (1971), “Dynamic models of segregation,” Journal of mathematical sociology, 1(2):143-186
Sugden, Robert (2000), “Credible worlds: the status of theoretical models in economics,” Journal of economic methodology, 7:1, 1-31
Ylikoski, Petri and N. Emrah Aydinonat (2014), “Understanding with theoretical models,” Journal of economic methodology, 21:1, 19-36
Young, H. Peyton (2017), “The evolution of Norms,” Annual Review of Economics, 7:359-87]

