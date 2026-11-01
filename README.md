# Behaviour-Tree-System-for-TOFM

**This repository is not a finished product intended to be used in other projects**
**It is my showcase of a system I am currently working on**

## What are behavior trees

A behaviour tree is a way of structuring AI decision-making in games as a tree of small, reusable tasks. Each frame (or tick), the game starts at the root and walks down the tree, and each node reports back one of three results: success, failure, or running (still in progress).

The main node types are:

- **Leaf nodes** do the actual work: either checking a condition (“Is the player visible?”) or performing an action (“Move to cover”, “Attack”).
- **Sequence nodes** run their children in order and stop at the first failure, like an “AND”. Example: see player → move closer → attack.
- **Selector** (fallback) nodes try their children in order and stop at the first success, like an “OR”. Example: try to attack, otherwise take cover, otherwise patrol.
- **Decorators** modify a single child, for example by inverting its result, repeating it, or adding a cooldown.

Compared to finite state machines, behaviour trees are easier to scale and maintain. Behaviours are modular, so you can reuse the same “find cover” branch across many enemy types, and you can add new logic without rewiring a tangle of state transitions. They’re widely used in games like the Halo series and in engines such as Unreal and Unity (via plugins or in my case implemented from scratch).

## What does my system consist of

Along side the basic nodes and strategies(actions that leaf nodes can perform)
I also use:
  - **Blackboard** system which is used to store data about the AI agent communicate data to its nodes or other agents and systems
  - **Perception** system that is used as a way for the agent to gather data about its surrounding acting like its eyes and ears
  - **Emotion** system that is deciding the internal state of the agent and modifying its behaviour in reaction to players actions (not yet implemented)
  - **Tree viewer** allows me to see what the agent is doing at runtime with a visual representation of the tree
  - **Tree editor** allows in editor way to make and configure behaviour trees (not implemented yet)

## what is TOFM

**TOFM = The forest of the missing(wip)**

It is an ambitious project me and my friends are working on

It will be a big 3D horror game where you are hunted by a demon whilst uncovering uncomfortable truths about the world and trying to escape a terrible fate

It is the main antagonist the demon for which this system is meant


### Huge thanks to git-amend Youtube channel on whose this system is based on
https://linktr.ee/gitamend
