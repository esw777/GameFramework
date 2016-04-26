# GameFramework
Generic game features/libraries

Beginnings of a framework for a tile based base builder type game.

Built using the Unity Engine. C#. Visual Studios.

Currently implemented:

1. A* pathfinding
2. Job queue
3. Basic UI - Buttons to build walls/doors. Basic tool tips and mouseover stats shown. Graphical representations of objects.
4. Two types of objects - built objects (walls/tables/furniture) and loose objects (swords, clothes, ore.)
5. Save/Load Map/characters/objects to XML
6. Rooms - floodfill algorithm to detect rooms.
7. Hauling, job material requirements. 
8. Furniture wih automatic job creation - stockpiles creating haul jobs for example.

In Progress:

1. Clean up the new stockpile/hauling code. Minor bugs still there. 
2. Character/World fluff - relationships, history, names, places, significant events.
3. Charcter skills - gain xp for complete a job.
4. Refactoring character to have a state machine related to current job/action. (Will significantly reduce if/else trees and allow for more complex logic.)

TODO:

1. Map generation.
2. Character stats - hunger / sleep / etc. 
3. Add priorities to job system - Add intelligent job picking ai - prefer closer jobs, jobs that character has higher skills in. Sleep/Eat over "work" jobs.
4. Put objects and other stuff into external files (XML / LUA.)
