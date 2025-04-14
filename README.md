# Sara's Zombie Archipelago

## Island Generation

The zombie archipelago has adapted a Cellular Automata generator to create large islands with small scattered islands along their shorelines. 
It begins by choosing three randomized centerpoints for the islands with a margin around the edges of 0.2 * min(width, height).
Next, for every point in the map, the distance from the current cell to the closest center is calculated. 
If the cell is outside of a predetermined radius from any island, it is immediately designated as water rather than land.
Otherwise, the normalized distance squared is used to affect the likelihood of a cell being water or land.

## Autonomous ZombShark

![alt text](https://github.com/sarabonardi/zombie-island/blob/main/Assets/Sprites/sharkie.png "Sharkie")

The evil lurking in the waters has been implemented using semi-random steering. 
He chooses a random direction when instantiated/on start and then chooses a random direction +/- 45 degrees from his current direction after the given interval.
This is what gives the appearance of a swiveling shark navigating the waters around the archipelago.