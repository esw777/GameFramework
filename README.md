# GameFramework
Generic game features/libraries

Beginnings of a framework for a tile based base builder type game.

Built using the Unity Engine. C#. Visual Studios.

Currently implemented:

1. A* pathfinding

2. Job queue

3. Basic UI - Buttons to build walls/doors. Basic tool tips and mouseover stats shown.

4. Two types of objects - built objects (walls/tables/furniture) and loose objects (swords, clothes, ore.)

5. Save/Load Map/characters/objects to XML

6. "Rooms" - floodfill algorithm to detect rooms.


In Progress:

1. Character ai in selecting a job. Hauling materials to a job site.
2. Minor refactoring to make eventual LUA/XML extraction easier.

TODO:

1. Map generation.

2. Character stats - hunger / sleep / etc.

4. Add priorities to job system - Add intelligent job picking ai - prefer closer jobs, jobs that character has higher skills in. Sleep/Eat over "work" jobs.

5. Put objects and other stuff into external files to increase modability.
