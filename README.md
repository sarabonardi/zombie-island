# Sara's Zombie Infested Archipelago

## Island Generation

This scenic zombie archipelago has adapted a Cellular Automata generator to create large islands with small scattered islands along their shorelines. 
It makes a lovely vacation spot for any scientist looking to save the human race or just to enjoy their last few days of consciousness. 
Because these islands are quite remote, I would imagine that any visiting scientist would resort to a boat ride over, but I'll warn that the waters may not be as safe as one would expect during a zombie apocalypse.

### Stage 1
The generation begins by choosing three randomized centerpoints for the islands with a margin around the edges of 0.2 * min(width, height).
Next, for every point in the map, the distance from the current cell to the closest center is calculated. 
If the cell is outside of a predetermined radius from any island, it is immediately designated as non-land (0).
Otherwise, the normalized distance squared is used to affect the likelihood of a cell being land or not.
This creates masses of land that may or may not be connected to each other depending on the randomization.

### Stage 2
After the map is filled with initial tiles, the cellular automata smoothing function is run to make the map feel a bit more natural. 
This is done in several iterations over the whole grid.
While I did not edit the CA smoothing function, I did increase the iterations from 5 to 8 to make prettier islands.

### Stage 3
The bulk of the archipelago's beauty comes from the processing stage because there are a lot of unpredictable things in unity that I decided to brute force solutions for! 
It begins with a call to the function ProcessMap which contains calls to several other functions. 
RemoveSmallWallRegions iterates through the map to remove noisy chunks of land (because I had weird chunks in the corners of every map and this seemed like the simplest way for me to fix it). 
Then I fill in the non-land tiles as water so that I know where I can spawn my Sharkie. 
Finally I add some mini islands so that it feels more archipeligaeic instead of just being three big chunks of land. 
After the map has been processed, I add a border around it so that Sharkie doesn't wander off and get lost and proceed to call the MeshGenerator so that the textures are finalized. 
After that, all that remains is the addition of my hand drawn graphics! 
The laboratory is added first and then the lone shark and my zombie friends.


## Autonomous ZombShark

![sharkie](https://github.com/sarabonardi/zombie-island/blob/main/Assets/Sprites/sharkie.png "Sharkie")

The evil lurking in the waters has been implemented using semi-random steering. 
He chooses a random direction when instantiated/on start and then chooses a random direction +/- 60 degrees from his current direction after the given interval.
This is what gives the appearance of a swiveling shark navigating the waters around the archipelago. 
To prevent him from becoming a landshark, Sharkie's navigation script includes a repulsion function to guide him away from land and borders. 
This samples points within a radius around himself and creates a repel vector to move in the opposite direction of a discovered boundary. 
The direction he is repelled to is added to his randomized direction and normalized to get the true direction for his update function. 
Sharkie enjoys roaming around when he has the chance but he has a soft spot for edges and corners... must be something buried along them that he's investigating.

## Abandoned Laboratory

![laboratory](https://github.com/sarabonardi/zombie-island/blob/main/Assets/Sprites/laboratory.png "Abandoned Laboratory")

The goal of the game is to access the abandoned laboratory to recover the zombie antidote!
So, the level generation would not be complete without the laboratory itself. 
The lab's location is decided by choosing one of the three island centerpoints randomized during the map generation and placing itself just a bit above that point.
This guarantees that the laboratory will be on land and gives me space to place a zombie guard in front of it.
Good luck to any scientist trying to access the lab; maybe there's a secret entrance in the back...

## Patrollers

![zombie](https://github.com/sarabonardi/zombie-island/blob/main/Assets/Sprites/zombie.png "Patrol Zombie")

The final graphical component to the game comes in the form of patrol zombies. 
They are believed to guard the entrance to the laboratory as well as the other two centers to ensure that the player must maintain stealth when navigating the islands.
Maybe in a real game they would be programmed with behavior trees to notice when the player is nearby and investigate or attack. 
In reality, these ones are all just practicing their Thriller dances for a TikTok they plan to film the next time they all hang out together. 

## Play Mode Guidance

I keep the random seed variable set to true, but if you do decide to set a string for deterministic generation, then "sharkluvr" creates one blob of an island in which the laboratory just kind of bounces around.

For settings, I use the following setup:
##### Main Camera:
* Position: X = 0, Y = 60, Z = 0
* Rotation: X = 90, Y = 0, Z = 0
* Field of View = 70
##### MapGenerator
* Width = 124
* Height = 70
* Use Random Seed = true
* Always Keep Edges As Walls = false (Sharkie breaks if true)
* Sharkie Prefab = sharkie
* Laboratory Prefab = laboratory
* Zombie Prefab = zombie
##### Laboratory
* Rotation: X = 90, Y = 0, Z = 0
* Scale: X = 1.2, Y = 1.2, Z = 1.2
* Order in Layer = 4
##### Sharkie
* Rotation: X = 90, Y = 0, Z = 0
* Scale: X = 0.6, Y = 0.6, Z = 0.6
* Order in Layer = 3
* Speed = 5
* Direction Change Interval = 1
* Repulsion Radius = 5
* Density = 0.5
* Repulsion Strength = 5
##### Zombies
* Rotation: X = 90, Y = 0, Z = 0
* Scale: X = 0.5, Y = 0.5, Z = 0.5
* Order in Layer = 5
* Patrol Distance = 7
* Speed = 1

## Acknowledgements
ChatGPT-4o was used to assist in translating my ideas about navigation and land distribution to Unity code. The Stage 2 code is based on "Procedural Cave Generation" by Sebastian Lague and has not been edited from the initial generator.