using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TileEnemies
{
    public partial class Form1 : Form
    {
        Point cursorLocation;//get position of cursor

        //Map Related
        Rectangle[] tiles;//the tiles of map
        bool[] tileCheck;//check if hovering over usable tile      
        int[] tilesMovementCost;//give each tile a movment cost  

        //Map Hazard Related
        Random randomTile = new Random();//select a random tile
        Random block = new Random();//select a random amount of tiles to block
        int[] saveTiles;//save tiles in an int array for later uses

        //Unit Related 
        Rectangle[] units;//units 
        bool right, left, up, down;//lets the units move 
        int unitCheck;//checks which unit is selected 
        int[] unitMovePoints;//each unit has move points for moving

        //Enemy Related
        Rectangle[] enemies;//enemies 
        bool[] enemyDirections = new bool[4];//lets the enemy move
        int[] enemyMovePoints;//each enemy has move points for moving        

        //Test Enemy Related
        int[] closestTarget;//lockon to closest unit
        int[] enemysTargetTile;//sets the closest unit's tile as pathfinding goal 
        double saveLowestDistance = 10000;//tempory fix, big number to start as base, need better solution     
        int[] enemyNeighboringTiles = new int[4];//for looking at all tiles around enemy (Right == 0, Left == 1, Down == 2, Up == 3) (Note: Shouldn't cause problems because only one enemy is moving at a time thus there should never be more than four neighboring tiles at once)
        int[] enemyCurrentTile;//for determining the tile an enemy is currently on
        double cost = 10000; 
        int[] savePath;

        //Movement Related
        int currentTile;//for determining the current tile
        int saveTile;//save a snapshot of selected moveableTile
        bool[] selected;//check if tile is selected
        int[] moveableTile = new int[4];//4 directions to move in up, down, left, right (Right == 0, Left == 1, Down == 2, Up == 3)

        //Graphics Related
        int cursorFlash = 255, tileFlash = 150;//the max opacity for the mouse and movement tiles specifically
        bool cursorTransparent = false, tileTranslucent = false;//used to check if the cursor/tiles are transparent

        public Form1()//loads everything in and sets the labels
        {
            InitializeComponent();

            //by default no unit is initally selected
            lblSelected.Text = "No unit is selected";

            /*!UNITS!*/
            int unitSpace = 110;//used for spacing out units (later, player places units)

            units = new Rectangle[2];//creates x number of units            

            //gives form to each unit
            for (int i = 0; i < units.Length; i++)
            {
                units[i] = new Rectangle(unitSpace, 10, 80, 80);//each unit is in the center of a tile and is 80 x 80 pixels large

                unitSpace = unitSpace + 100;//adds space 
            }

            selected = new bool[units.Length];//create a selected option for each unit

            unitMovePoints = new int[units.Length];//create move points for each unit 

            for (int i = 0; i < units.Length; i++)//goes through each unit
            {
                unitMovePoints[i] = 5;//gives them each 5 unit points, later this will be a stat determined by class and unit bases
            }

            /*!ENEMIES!*/
            unitSpace = 10;//reset unitSpace for enemies to spawn correctly

            enemies = new Rectangle[1];//creates x number of enemies

            enemyMovePoints = new int[enemies.Length];//create each enemy move points

            closestTarget = new int[enemies.Length];//create a target for each enemy

            enemysTargetTile = new int[enemies.Length];//get target tile for pathfinding

            enemyCurrentTile = new int[enemies.Length];//get enemy's current tile for pathfinding
           
            //gives form to each enemy 
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i] = new Rectangle((900 + unitSpace), 10, 80, 80);//each enemy

                unitSpace = unitSpace - 100;//adds space to the left

                enemyMovePoints[i] = 5;//give 5 points to that enemy
            }

            /*!TILES!*/
            //create just enough tiles to fill the form
            tiles = new Rectangle[(ClientRectangle.Width / 100) * (ClientRectangle.Height / 100)];

            //create a tileCheck for every tile
            tileCheck = new bool[tiles.Length];

            //create a movement cost for every tile
            tilesMovementCost = new int[tiles.Length]; 

            //to start a new row (Y of tiles)
            int newRow = 0;

            //to move the tiles right (X of tiles)
            int count = 0;

            for (int i = 0; i < tiles.Length; i++)//goes through each tile
            {
                //create a new tile each round, count is the X and newRow is the Y
                tiles[i] = new Rectangle(count, newRow, 100, 100);

                //adds to count so next tile can appear beside previous 
                count = count + 100;

                //checks if newest tile's right edge passes or is at the edge of the form's right edge 
                if (tiles[i].Right >= ClientRectangle.Right)
                {
                    count = 0;//reset count for new row
                    newRow = newRow + 100;//make sure new tiles appear on new row
                }

                tilesMovementCost[i] = 1;//give each tile a movement cost of 1
            }

            /*!HAZARDS!*/
            //create a random number of block tiles ranging from 0 to 10
            saveTiles = new int[block.Next(1, 20)];

            //goes through each saveTile element
            for (int i = 0; i < saveTiles.Length; i++)
            {
                saveTiles[i] = randomTile.Next(0, tiles.Length);//fills the element with a random tile    

                tilesMovementCost[saveTiles[i]] = 100000;
            }  
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)//when mouse is moving
        {
            cursorLocation = e.Location;//get cursor location 
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)//directs where units go and selects units
        {
            if (tileCheck[moveableTile[0]] == false && tileCheck[moveableTile[1]] == false && tileCheck[moveableTile[2]] == false && tileCheck[moveableTile[3]] == false)//if mouse is not on any of the moveableTiles...
            {
                //by default no unit is initally selected
                lblSelected.Text = "No unit is selected";

                //Goes through each unit... 
                for (int i = 0; i < units.Length; i++)
                {
                    selected[i] = false;//resets every unit for new selected unit

                    //if over a unit...
                    if ((units[i].Left <= cursorLocation.X && cursorLocation.X <= units[i].Right) && (units[i].Top <= cursorLocation.Y && cursorLocation.Y <= units[i].Bottom))
                    {
                        selected[i] = true;//selects unit
                        unitCheck = i;//has the unit check become the selected unit
                        lblSelected.Text = "Unit " + (i + 1) + " is selected";//displays selected unit                            
                    }
                }

                Array.Clear(moveableTile, 0, moveableTile.Length);//used to clear the moveableTile array
            }

            if (tileCheck[moveableTile[0]] == true && left == false && down == false && up == false)//if mouse is on moveableTile[0]...
            {
                //saveTile(a snapshot of moveableTile0 before array resets)
                saveTile = moveableTile[0];

                right = true;//let tmrMove know that unit needs to move right

                tmrMove.Enabled = true;//starts timer so unit can move (sort of redundant but doesn't hurt)     

                Array.Clear(moveableTile, 0, moveableTile.Length);//clear the moveableTile array...

                unitMovePoints[unitCheck] = unitMovePoints[unitCheck] - 1;//subtracts one move point from the selected unit
            }
            else if (tileCheck[moveableTile[1]] == true && right == false && down == false && up == false)//if mouse is on moveableTile[1]...
            {
                //saveTile(a snapshot of moveableTile0 before array resets)
                saveTile = moveableTile[1];

                left = true;//let tmrMove know that unit needs to move left

                tmrMove.Enabled = true;//starts timer so unit can move (sort of redundant but doesn't hurt)

                Array.Clear(moveableTile, 0, moveableTile.Length);//clear the moveableTile array...

                unitMovePoints[unitCheck] = unitMovePoints[unitCheck] - 1;//subtracts one move point from the selected unit
            }
            else if (tileCheck[moveableTile[2]] == true && right == false && left == false && up == false)//if mouse is on moveableTile[2]...
            {
                //saveTile(a snapshot of moveableTile0 before array resets)
                saveTile = moveableTile[2];

                down = true;//let tmrMove know that unit needs to move down

                tmrMove.Enabled = true;//starts timer so unit can move (sort of redundant but doesn't hurt)

                Array.Clear(moveableTile, 0, moveableTile.Length);//clear the moveableTile array...

                unitMovePoints[unitCheck] = unitMovePoints[unitCheck] - 1;//subtracts one move point from the selected unit
            }
            else if (tileCheck[moveableTile[3]] == true && right == false && left == false && down == false)//if mouse is on moveableTile[3]...
            {
                //saveTile(a snapshot of moveableTile0 before array resets)
                saveTile = moveableTile[3];

                up = true;//let tmrMove know that unit needs to move up

                tmrMove.Enabled = true;//starts timer so unit can move (sort of redundant but doesn't hurt)

                Array.Clear(moveableTile, 0, moveableTile.Length);//clear the moveableTile array...

                unitMovePoints[unitCheck] = unitMovePoints[unitCheck] - 1;//subtracts one move point from the selected unit
            }
        }

        private void btnStartEnemyTurn_Click(object sender, EventArgs e)
        {          
            tmrEnemyTurn.Enabled = true;//starts the enemy turn/timer
        }

        private void tmrMove_Tick(object sender, EventArgs e)//timer that moves units
        {
            //if selected units is not on "tile right" and "right" is true...
            if (units[unitCheck].X < tiles[saveTile].X + 10 && right == true)
            {
                units[unitCheck].X = units[unitCheck].X + 5;//unit moves right
            }
            else if (right == true)//but if unit is on "tile right" and only "right is true"...
            {
                right = false;//right becomes false

                tmrMove.Enabled = false;//timer turns off
            }

            //if selected units is not on "tile left" and "left" is true...
            if (units[unitCheck].X > tiles[saveTile].X + 10 && left == true)
            {
                units[unitCheck].X = units[unitCheck].X - 5;//unit moves left
            }
            else if (left == true)//but if unit is on "tile left" and only "left is true"...
            {
                left = false;//left becomes false

                tmrMove.Enabled = false;//timer turns off
            }

            //if selected units is not on "tile down" and "down" is true...
            if (units[unitCheck].Y < tiles[saveTile].Y + 10 && down == true)
            {
                units[unitCheck].Y = units[unitCheck].Y + 5;//unit moves down
            }
            else if (down == true)//but if unit is on "down tile" and only "right is down"...
            {
                down = false;//down becomes false

                tmrMove.Enabled = false;//timer turns off
            }

            //if selected units is not on "tile down" and "down" is true...
            if (units[unitCheck].Y > tiles[saveTile].Y + 10 && up == true)
            {
                units[unitCheck].Y = units[unitCheck].Y - 5;//unit moves up
            }
            else if (up == true)//but if unit is on "up tile" and only "right is up"...
            {
                up = false;//up becomes false

                tmrMove.Enabled = false;//timer turns off
            }
        }

        private void tmrCheck_Tick(object sender, EventArgs e)//Get label information
        {
            lblUnitMovP.Text = "Unit1: " + unitMovePoints[0] + " | Unit2: " + unitMovePoints[1];//used to test the move points of both units

            lblCursorX.Text = cursorLocation.X.ToString();//display cursor's X loaction 
            lblCursorY.Text = cursorLocation.Y.ToString();//display cursor's Y loaction
            lblTile.Text = "No tile is true";//by default no tile is currently true

            //goes through each tileCheck
            for (int i = 0; i < tileCheck.Length; i++)
            {
                if (tileCheck[i] == true)//if there is a true tile...
                {
                    //then label will display that tile as true (there can only be one true tile at a time)
                    lblTile.Text = "Tile " + i + " is true";
                }
            }

            Refresh();//refresh timer
        }

        private void tmrEnemyTurn_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                enemyMovePoints[i] = 5; 
            }

            EnemyMovement();//goes through enemy movement 

            for (int i = 0; i < units.Length; i++)
            {
                unitMovePoints[i] = 5; 
            }

            tmrEnemyTurn.Enabled = false;//at the end of enemy turn, turn ends
        }

        void EnemyMovement()//enemy movement (put in tmrEnemyTurn)
        {
            /*!FIND TARGET UNIT!*/
            for (int i = 0; i < enemies.Length; i++)
            {
                for (int j = 0; j < units.Length; j++)//and goes through each unit...
                {
                    int distanceX = enemies[i].X - units[j].X;//find diffrence of x for each unit vs current enemy (Note: may cause problems based on position of units and enemies)
                    int distanceY = enemies[i].Y - units[j].Y;//find diffrence of y for each unit vs current enemy (Note: may cause problems based on position of units and enemies)

                    double totalDistance = Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2));//Pythagorean theorem to find closest unit to target                    

                    if (Math.Abs(totalDistance) < saveLowestDistance)//if the new distance is shorter... (Note: Math.Abs() gets the true number disragarding negative or positve) 
                    {
                        saveLowestDistance = totalDistance;//it becomes the new saved distance...

                        closestTarget[i] = j;//and the unit of save distance becomes current target
                    }
                }
            }

            for (int i = 0; i < enemies.Length; i++)//goes through each enemy...
            {
                int numberOfSaveTiles = 2;//used to determine the number of saved tiles, which is used to size the array of savePath, the path (tiles) that the enemy will take 
                savePath = new int[numberOfSaveTiles];//creates the number of save tiles the enemy will remember traveling
                for (int j = 0; j < tiles.Length; j++)
                {
                    if (enemies[i].IntersectsWith(tiles[j]))//gets the tile the enemy is currently on... 
                    {
                        savePath[0] = j;//sets the tile as savePath[0], making sure enemy doesn't backtrack
                    }
                }
                
                do//Keep moving until runs out of movement points or in range of enemy
                {
                    bool breakLoop = false;//used to break out of while loop

                    for (int j = 0; j < enemyDirections.Length; j++)//goes through each enemy direction...
                    {
                        enemyDirections[j] = false;//and resets them all to false
                    }

                    /*!FIND ENEMY'S CURRENT TILE!*/
                    for (int j = 0; j < tiles.Length; j++)//goes through each tile
                    {
                        for (int l = 0; l < units.Length; l++)
                        {
                            if (units[closestTarget[i]].IntersectsWith(tiles[j]) == true)//checks for the unit that is closest to the enemy tile (gets the tile the closest unit is on)
                            {
                                enemysTargetTile[i] = j;//has the targeted tile become the enemies goal for pathfinding
                            }

                            if (enemies[i].IntersectsWith(tiles[j]))//gets the tile the enemy is currently on... 
                            {
                                enemyCurrentTile[i] = j;//sets the tile found as enemy's current tile
                            }
                        }                        
                    }

                    /*!FIND THE NEIGHBORING TILES!*/
                    Array.Clear(enemyNeighboringTiles, 0, enemyNeighboringTiles.Length);//Clear enemyNeighboringTiles array (before clear, still has data from last current tile)
                    for (int j = 0; j < tiles.Length; j++)
                    {
                        if (tiles[enemyCurrentTile[i]].Right == tiles[j].Left && tiles[enemyCurrentTile[i]].Top == tiles[j].Top)//finds tile right of enemy's current tile... 
                        {
                            enemyNeighboringTiles[0] = j;//sets that tile as enemyNeighboringTiles[0]
                        }
                        else if (tiles[enemyCurrentTile[i]].Right == tiles[j].Left)//looks at enemy's current tile and goes through every to see if there is a tile on the right of it
                        {
                            enemyDirections[0] = true;//makes enemy have an absurd cost for nonexistent right tile
                        }

                        if (tiles[enemyCurrentTile[i]].Left == tiles[j].Right && tiles[enemyCurrentTile[i]].Top == tiles[j].Top)//finds tile left of enemy's current tile...
                        {
                            enemyNeighboringTiles[1] = j;//sets that tile as enemyNeighboringTiles[1]
                        }
                        else if (tiles[enemyCurrentTile[i]].Left == tiles[j].Right)//looks at enemy's current tile and goes through every to see if there is a tile on the left of it
                        {
                            enemyDirections[1] = true;//makes enemy have an absurd cost for nonexistent left tile 
                        }

                        if (tiles[enemyCurrentTile[i]].Bottom == tiles[j].Top && tiles[enemyCurrentTile[i]].Right == tiles[j].Right)//finds tile below of enemy's current tile... 
                        {
                            enemyNeighboringTiles[2] = j;//sets that tile as enemyNeighboringTiles[2]                            
                        }
                        else if (tiles[enemyCurrentTile[i]].Bottom == tiles[j].Top)//looks at enemy's current tile and goes through every to see if there is a tile below it
                        {
                            enemyDirections[2] = true;//makes enemy have an absurd cost for nonexistent bottem tile
                        }

                        if (tiles[enemyCurrentTile[i]].Top == tiles[j].Bottom && tiles[enemyCurrentTile[i]].Right == tiles[j].Right)//finds tile above of enemy's current tile... 
                        {
                            enemyNeighboringTiles[3] = j;//sets that tile as enemyNeighboringTiles[3]                            
                        }
                        else if (tiles[enemyCurrentTile[i]].Top == tiles[j].Bottom)//looks at enemy's current tile and goes through every to see if there is a tile above it
                        {
                            enemyDirections[3] = true;//makes enemy have an absurd cost for nonexistent top tile
                        }
                    }

                    for (int l = 0; l < units.Length; l++)
                    {
                        if (tiles[enemyNeighboringTiles[0]].IntersectsWith(units[l]) || tiles[enemyNeighboringTiles[1]].IntersectsWith(units[l]) || tiles[enemyNeighboringTiles[2]].IntersectsWith(units[l]) || tiles[enemyNeighboringTiles[3]].IntersectsWith(units[l]))//if the enemy is in range of a unit...
                        {
                            breakLoop = true;//used to break out of while loop
                            break;//breaks this for loop
                        }
                    }

                    if (breakLoop == true)//checks if breakLoop is true...
                    {
                        break;//breaks out of while loop
                    }

                    /*!FIND LOWEST COST!*/
                    cost = 10000;
                    for (int j = 0; j < enemyNeighboringTiles.Length; j++)//goes through each neighboring tile...
                    {
                        int distanceX = tiles[enemyNeighboringTiles[j]].X - tiles[enemysTargetTile[i]].X;//finds diffrence between the distance of X for the current neighboring tile and the enemy's goal (closest unit's tile)
                        int distanceY = tiles[enemyNeighboringTiles[j]].Y - tiles[enemysTargetTile[i]].Y;//finds diffrence between the distance of Y for the current neighboring tile and the enemy's goal (closest unit's tile)
                        double tempCost = Math.Abs(Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2)));//find the hypotenuse between current neighboring tile and enemy's goal, finding the average distance from goal 

                        for (int f = 0; f < enemyDirections.Length; f++)//goes through each enemy direction 
                        {
                            if (enemyNeighboringTiles[j] == 0 && enemyDirections[f] == false)//if the neighboring tile 0 and it's a border tile
                            {
                                tempCost = 10000;//make the tempCost huge so the enemy doesn't go to tile 0, just because a val array defualts unused elements as 0
                            }
                        }

                        bool pathCheck = false;//make a pathCheck set to false
                        for (int f = 0; f < savePath.Length; f++)//goes through every tile the enemy has travelled
                        {
                            if (enemyNeighboringTiles[j] == savePath[f])//if a neighboring tile is a tile the enemy has traveled through before (a saved tile)...
                            {
                                pathCheck = true;//then pathChecks becomes true, telling the enemy it should go to that neighboring tile because it's alredy been there 
                            }
                        }

                        double totalCost = tempCost + tilesMovementCost[enemyNeighboringTiles[j]];
                        if (totalCost < cost && pathCheck == false)//if the calculated tempCost (hypotenuse of current neighboring tile and enemy's goal) is lowest than the sofar lowest cost, and the current neighboring tile hasn't been traveled before... 
                        {
                            cost = tempCost;//the tempCost become the new cost (or the lowest cost tile so far)

                            savePath[numberOfSaveTiles - 1] = enemyNeighboringTiles[j];//the current neighboring tile becomes a save tile (if a lower cost neighboring tile comes along later, the savePath for that step gets overwiten with the new lowest cost neighboring tile)                              
                        }

                        if (j == enemyNeighboringTiles.Length - 1)//if going through the last cycle of the "enemyNeighboringTiles for loop"...
                        {
                            for (int p = 0; p < 50; p++)//every tile is 100x100, and enemy only need to go in one direction to reach next tile (this only works beacuse of how the enemy is programed to move)
                            {
                                if (enemies[i].X < tiles[savePath[numberOfSaveTiles - 1]].X + 10)//if left of where it's supposed to be...
                                {
                                    enemies[i].X = enemies[i].X + 2;//add 1 pixel to X
                                }
                                else if (enemies[i].X > tiles[savePath[numberOfSaveTiles - 1]].X + 10)//if right of where it's supposed to be...
                                {
                                    enemies[i].X = enemies[i].X - 2;//subtract 1 pixel from X
                                }
                                else if (enemies[i].Y > tiles[savePath[numberOfSaveTiles - 1]].Y + 10)//if below of where it's supposed to be...
                                {
                                    enemies[i].Y = enemies[i].Y - 2;//subtract 1 pixel from Y
                                }
                                else if (enemies[i].Y < tiles[savePath[numberOfSaveTiles - 1]].Y + 10)//if above of where it's supposed to be...
                                {
                                    enemies[i].Y = enemies[i].Y + 2;//add 1 pixel to X
                                }

                                Refresh();
                            }
                        }                                                 
                    }

                    numberOfSaveTiles = numberOfSaveTiles + 1;//number of save tiles able to be stored goes up by one

                    Array.Resize(ref savePath, numberOfSaveTiles);//expand array to number of save tiles (Note: unsure if this is proper way)        

                    enemyMovePoints[i] = enemyMovePoints[i] - 1;//every time enemy moves, costs one move point              
                }
                while (enemyMovePoints[i] > 0);
            }                     
        }

        private void tmrAnimation_Tick(object sender, EventArgs e)//flashes tile cursor is currently on and flash movement tiles
        {
            //a counter that checks if cursorFlash is greater than 0 and that cusor isn't tansparent...               
            if (cursorFlash > 0 && cursorTransparent == false)
            {
                cursorFlash = cursorFlash - 5;//if it is, cursorFlash drops 5 opacity every cycle
            }
            //a counter that checks if cursorFlash is less than 255 and cursor is transparent... 
            else if (cursorTransparent == true && cursorFlash < 255)
            {
                cursorFlash = cursorFlash + 5;//if it is, cursorFlash gains 5 opacity every cycle
            }

            if (cursorFlash == 0)//if cursor flash is 0...
            {
                cursorTransparent = true;//than cursorTransparent is true
            }
            else if (cursorFlash == 255)//otherwise if cursor is opaque...
            {
                cursorTransparent = false;//than cursorTransparent is false
            }

            //a counter that checks if tileFlash is greater than 90 and that tile isn't faded(tileTranslucent)...   
            if (tileFlash > 90 && tileTranslucent == false)
            {
                tileFlash = tileFlash - 1;//if it is, tileFlash drops 1 opacity every cycle
            }
            //a counter that checks if tileFlash is less than 255 and tile is faded...
            else if (cursorTransparent == true && cursorFlash < 150)
            {
                tileFlash = tileFlash + 1;//if it is, tileFlash gains 5 opacity every cycle
            }

            if (tileFlash == 90)//if cursor flash is 90...
            {
                tileTranslucent = true;//than cursorTransparent is true
            }
            else if (tileFlash == 150)//otherwise if cursor is translucent...
            {
                tileTranslucent = false;//than tileTranslucent is false
            }
        }

        protected override void OnPaint(PaintEventArgs e)//Graphics
        {
            base.OnPaint(e);

            //have all tile be gray by default 
            for (int i = 0; i < tiles.Length; i++)
            {
                e.Graphics.FillRectangle(Brushes.Gray, tiles[i]);
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                //if over a tile...
                if ((tiles[i].Left <= cursorLocation.X && cursorLocation.X <= tiles[i].Right) && (tiles[i].Top <= cursorLocation.Y && cursorLocation.Y <= tiles[i].Bottom))
                {
                    Brush flashGreen = new SolidBrush(Color.FromArgb(cursorFlash, 34, 139, 34));//a new brush for flashing green (A,R,G,B A: is the opacity and 128 is 50%, 255 is 100% opacity)

                    e.Graphics.FillRectangle(flashGreen, tiles[i]);//tile becomes green...

                    tileCheck[i] = true;//and it becomes a true tile              
                }
                else//otherwise...
                {
                    tileCheck[i] = false;//tile is false (ensure only one tile can be true at a time)
                }
            }

            for (int i = 0; i < selected.Length; i++)//goes through each selected tile...
            {
                for (int j = 0; j < tiles.Length; j++)//and goes through each tile...
                {
                    Brush transBlue = new SolidBrush(Color.FromArgb(tileFlash, 0, 0, 255));//a new brush for translucent blue (A,R,G,B A: is the opacity and 128 is 50%, 256 is 100% opacity)            

                    if (selected[i] == true && units[i].IntersectsWith(tiles[j]))//check if selected unit is on a tile, which it is, this is just ment to single out that tile
                    {
                        e.Graphics.FillRectangle(transBlue, tiles[j]);//change the colour of tile to translucent blue...    

                        currentTile = j;//sets as currentTile or tile unit is currently on                         
                    }

                    //used to find the tile right of current tile or moveableTile[0], this done by checking if the right of "current tile" matches the left of "right tile"
                    else if (selected[i] == true && tiles[currentTile].Right == tiles[j].Left && tiles[currentTile].Top == tiles[j].Top)
                    {
                        e.Graphics.FillRectangle(transBlue, tiles[j]);//change the colour of tile to translucent blue... 

                        moveableTile[0] = j;//and set the tile as a moveableTile                         
                    }

                    //used to find the tile left of current tile or moveableTile[0], this done by checking if the left of "current tile" matches the right of "left tile"
                    else if (selected[i] == true && tiles[currentTile].Left == tiles[j].Right && tiles[currentTile].Top == tiles[j].Top)
                    {
                        e.Graphics.FillRectangle(transBlue, tiles[j]);//change the colour of tile to translucent blue... 

                        moveableTile[1] = j;//and set the tile as a moveableTile  
                    }

                    //used to find the tile bottom of current tile or moveableTile[0], this done by checking if the bottom of "current tile" matches the top of "bottom tile"
                    else if (selected[i] == true && tiles[currentTile].Bottom == tiles[j].Top && tiles[currentTile].Right == tiles[j].Right)
                    {
                        e.Graphics.FillRectangle(transBlue, tiles[j]);//change the colour of tile to translucent blue... 

                        moveableTile[2] = j;//and set the tile as a moveableTile  
                    }

                    //used to find the tile top of current tile or moveableTile[0], this done by checking if the top of "current tile" matches the bottom of "top tile"
                    else if (selected[i] == true && tiles[currentTile].Top == tiles[j].Bottom && tiles[currentTile].Right == tiles[j].Right)
                    {
                        e.Graphics.FillRectangle(transBlue, tiles[j]);//change the colour of tile to translucent blue... 

                        moveableTile[3] = j;//and set the tile as a moveableTile  
                    }
                }
            }

            //goes through each saveTiles 
            for (int i = 0; i < saveTiles.Length; i++)
            {
                //has each saved tile be yellow...
                e.Graphics.FillRectangle(Brushes.Yellow, tiles[saveTiles[i]]);
                //have their tile check be permantly false 
                tileCheck[saveTiles[i]] = false;
                //and have their tile cost be absurd so enemies avoid them                
            }

            //colours in each unit
            for (int i = 0; i < units.Length; i++)
            {
                e.Graphics.FillRectangle(Brushes.HotPink, units[i]);//units are Hot Pink

                if (selected[i] == true)//if unit is selected
                {
                    e.Graphics.FillRectangle(Brushes.DeepPink, units[i]);//unit are Deep Pink
                }
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                e.Graphics.FillRectangle(Brushes.DarkRed, enemies[i]);//units are Dark Red
            }
        }
    }
}
/*Notes
 July 21 2020: 
 - Bug1, when you start the game you cannot move unit1 before unit2
 - Bug2, selected units will teleport to tile1 when tile1 is clicked, and unit can no longer move until another unit is moved first (if both units are on tile1 by this method or unit1 has not been moved yet, game enters a softlock)
 - Bug3, when a unit is on tile1 and tile1 is clicked, unit will lose "move points"
 - Bug4, units can move on top of each other

 July 22 2020: 
 - Bug1 of July 21 2020 has been expanded, to make the first move on unit1, unit2 must be moved first and must be selected before clicking on unit1, is unit2 is deselcted before clicking unit1 or unit2 has not been selected, unit1 will not respond and Bug3 occur on unit1
 - Bug2 of July 21 2020 has been somewhat remedied, unit no longer teleports to tile1 when tile1 is clicked, however if unit is willing moved to tile1 and unit is deselected, unit can no longer move until another unit is moved first 
 - Bug3 of July 21 2020 has been expanded, when a unit is selected and tile1 is clicked, no matter the tile unit is on, unit will lose "move points"
 - Progress, created timer for enemy system
 - Progress, started enemy movement (Just the subprogram, hardly enemy progress)

 August 26 2020:
 - Bugs, all previous Bugs (Bug1, Bug2, Bug3, Bug4) remain
 - Progress, created a targeting system for the enemy (enemy is able to locate the closest unit and set it as its goal)
 - Progress, created neighboring tile system to start enemy movment
 - Note, need to get enemy to pick a direction 
 - Note, implement A* features while working on Dijkstra's Algorithem

 August 27 2020:
 - Bugs, all previous Bugs (Bug1, Bug2, Bug3, Bug4) remain
 - Progress, believe to have finished movement system (have yet to test)
 - Note, somewhat sceptical of saveTile array/system, look into it if fail test
 - Note, somewhat sceptical of while loop that moves the enemies, if needed replace this system first for a more simple version

 August 28 2020
 - Bugs, all previous Bugs (Bug1, Bug2, Bug3, Bug4) remain
 - Progress, troubleshooting enemy movement system
 - Note, when testing remove the 5 move limit on enemy for testing purposes
 - Note, enemy does not seen to move down when closest unit is clearly below it
 - Note, when closest unit is below, it travels the first two tiles normally before jumping to tile 0
 - Note, when closest unit is left, it travels normaly until reaches tile 0, then stops completely
 - Note, removed the enemy's "smooth moving" system in favor of instant teleportation, enemy "smooth moving" was finiky from the start, must revise
 - Note, suspect enemy neighboring tile is causing many of the enemies strange movement choices
 - Note, enemyDirection all end up false on defualt, perphaps change to "else if" is needed 

 August 29 2020
 - Bugs, all previous Bugs (Bug1, Bug2, Bug3, Bug4) remain
 - Bug5, when enemy needs to choose between hazards and backtracking, enemy will choose to go through hazards
 - Progress, troubleshooting enemy movement system
 - Progress, implmented the enemy "smooth moving" system
 - Note, enemyDirection has been fixed
 - Note, enemy will not move if unit is on tile 0, because the enemy thinks it's neighboring tile is intersecting with unit on tile 0 due to neighboring tiles defulting to 0 when no other tiles are around it (ie the border tiles)
 - Note, potential bug for pathCheck, when checking savePath, enemy thinks it has alway travled through tile 0 (should not be a problem unless searched for, also enemy should never end up in the corner of a map)
 */
