# GameFramework
Generic game features/libraries

Beginnings of a framework for a tile based base builder type game.

Built using the Unity Engine. C#. Visual Studios.

Note: Files in the Utility folder are 3rd party code and libraries.

Currently implemented:

1. A* pathfinding

2. Job queue

3. Basic UI - buttons to queue "build wall" jobs. Couple debug buttons.

4. Two types of objects - built objects (walls/tables/furniture) and loose objects (swords, clothes, ore.)

5. Save/Load Map/characters/objects to XML


In Progress:

1. Adding a room system to enhance pathfinding as well as for features like room tempature.
2. Flood fill algorithm to map out rooms.


TODO:

1. Map generation.

2. Character stats - hunger / sleep / etc.

3. Additional objects + Expand on the loose object stuff to match complexity of built objects.

4. Add priorities to job system - Add intelligent job picking ai - prefer closer jobs, jobs that character has higher skills in. Sleep/Eat over "work" jobs.

5. Put objects and other stuff into external files to increase modability.

6. Expand UI along the way.
