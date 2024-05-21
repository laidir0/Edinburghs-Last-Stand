using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

// Zachary Thomson 2024
// Graded Unit
// The project is to create a game a group from planning through evaluation
// We are making a top down defense style game
// The player character will be at the top of the screen and the enemies will spawn at the bottom
// The player will have to defend the castle from the enemies
// The player can move side to side and shoot the enemies
// The enemies will move up the screen towards the castle
// If the enemies reach the castle they will deal damage to it
// The game ends when the castle is destroyed

namespace GradedUnit
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics; // TODO: not sure if needed, made readonly for time being
        private SpriteBatch _spriteBatch;

        // Lane logic - TODO: Move to own class/function
        private static int _lane = 2; // The lane the player is in
        public static int Lane
        {
            get => _lane;
            set
            {
                if (value < 0) _lane = 0;
                else if (value > 4) _lane = 4;
                else _lane = value;
            }
        }

        private const int tileSize = 128;                     // Size of the tiles in pixels *at current settings this gives 15 tiles wide and 8.5 tall? (1920*1088 - just off bottom of screen)
        // Background space is including tile 4 through to the 15 for the X-axis (12 tiles wide = 1536 pixels) and 8.5 tiles tall for the Y-axis (8.5 tiles tall = 1088 pixels)
        private int range = 0;                               // 0 = 1 tile, 1 = 1 range around the tile, 2 = 2 range around the tile....
        public Vector2 mouseTilePosition;                    // The mouse position in tiles

        private Texture2D tileTexture;                       // The texture for the tiles

        // 2D structure for the background
        struct BackgroundStruct
        {
            public Texture2D backgroundTexture;             // The texture for the background
            public Rectangle backgroundRectangle;           // The rectangle for the background

            // Constructor for the background
            public BackgroundStruct(ContentManager content, string filename, int backgroundWidth, int backgroundHeight)
            {
                backgroundTexture = content.Load<Texture2D>(filename);                          // Load the texture
                float ratio = ((float)backgroundWidth / (float)backgroundTexture.Width);        // Get the ratio of the texture
                backgroundRectangle.Width = backgroundWidth;                                    // Set the width of the rectangle
                backgroundRectangle.Height = (int)(backgroundTexture.Height * ratio);           // Set the height of the rectangle
                backgroundRectangle.X = 0;                                                      // Set the x position of the rectangle
                backgroundRectangle.Y = (backgroundHeight - backgroundRectangle.Height) / 2;    // Set the y position of the rectangle
            } // End of backgroundStruct constructor
        } // End of backgroundStruct

        // 2D structure for the player sprite
        struct PlayerSprite
        {
                public Texture2D playerTexture;                 // The texture for the player
                public Rectangle playerRectangle;               // The rectangle for the player
                public Vector2 playerOrigin;                    // The origin of the player
                public Vector2 playerPosition;                  // The position of the player
                public int playerHealth;                        // The health of the player
                public bool playerAlive;                        // If the player is alive
                public int score;                               // The score of the player

                // Constructor for the player
                public PlayerSprite(ContentManager content, string filename)
                {
                    playerTexture = content.Load<Texture2D>(filename);                                      // Load the texture
                    playerRectangle = new Rectangle(0, 0, playerTexture.Width, playerTexture.Height);       // Set the rectangle
                    playerOrigin.X = (float)playerRectangle.Width / 2;                                      // Set the x origin of the player
                    playerOrigin.Y = (float)playerRectangle.Height / 2;                                     // Set the y origin of the player

                    // General
                    playerHealth = 100;                             // Set the health of the player
                    score = 0;                                      // Set the score of the player
                    playerAlive = true;                             // Set the player to alive
                    playerPosition = new Vector2();                 // Set the player position
                }
            }

            // Structure for the players basic attack - Arrows
            struct ArrowStruct
            {
                public Texture2D arrowTexture;                  // The texture for the arrow
                public Rectangle arrowRectangle;                // The rectangle for the arrow
                public Vector3 arrowPosition;                   // The position of the arrow
                public Vector2 arrowOrigin;                      // The origin of the arrow
                public BoundingSphere arrowBoundingSphere;      // The bounding sphere of the arrow
                public bool arrowAlive;                         // If the arrow is alive
            }

            // Structure for the enemies
            struct EnemyStruct
            {
                public Texture2D enemyTexture;                  // The texture for the enemy
                public Rectangle enemyRectangle;                // The rectangle for the enemy
                public Vector2 enemyOrigin;                     // The origin of the enemy
                public Vector2 enemyPosition;                   // The position of the enemy
                public int enemySpeed;                          // The speed of the enemy
                public int enemyHealth;                         // The health of the enemy
                public bool enemyAlive;                         // If the enemy is alive
                public BoundingSphere enemyBoundingSphere;     // The bounding sphere of the enemy
                public float spawnTime;                           // The time the last enemy was spawned

                // Constructor for the enemy
                public EnemyStruct(ContentManager content, string filename)
                {
                    enemyTexture = content.Load<Texture2D>(filename);                                      // Load the texture
                    enemyRectangle = new Rectangle(0, 0, enemyTexture.Width, enemyTexture.Height);         // Set the rectangle
                    enemyOrigin.X = (float)enemyRectangle.Width / 2;                                        // Set the x origin of the enemy
                    enemyOrigin.Y = (float)enemyRectangle.Height / 2;                                       // Set the y origin of the enemy
                    enemyBoundingSphere = new BoundingSphere();                                             // Set the bounding sphere of the enemy

                    // General
                    enemyHealth = 30;                             // Set the health of the enemy
                    enemySpeed = 1;                               // Set the speed of the enemy
                    enemyAlive = false;                             // Set the enemy to alive
                    enemyPosition = new Vector2();                 // Set the enemy position
                    spawnTime = 0;                              // Set the spawn time
                }
            }

            // VARIABLES
            BackgroundStruct gameBackground, gameOverBackground;                // Background and game over screen
            SpriteFont mainFont;                                                // Main font
            bool gameOver;                                                      // If the game is over
            PlayerSprite player;                                                // Player sprite
            ArrowStruct[] arrows = new ArrowStruct[100];                        // Array of arrows
            EnemyStruct[] enemies = new EnemyStruct[20];                        // Array of enemies

            // Assuming will need later, there for now
            Random rand = new Random();                                         // Random number generator
            // A timer for the game equal to gametime
            


            public Game1()                                      // Machine things - Set the w   indow size, fullscreen, mouse visibility and content directory
            {
                _graphics = new GraphicsDeviceManager(this)     // Set the window size
                {
                    PreferredBackBufferWidth = 1920,            // 1920x1080
                    PreferredBackBufferHeight = 1080,
                    IsFullScreen = true
                };
                Content.RootDirectory = "Content";              // Set the content directory
                IsMouseVisible = true;                          // Show the mouse
            } // End of Game1 constructor

            protected override void Initialize()    // Initialize the game
            {
                // Create a texture for the tiles
                tileTexture = new Texture2D(GraphicsDevice, 1, 1);  // 1x1 pixel
                tileTexture.SetData(new Color[] { Color.White });   // White pixel

                base.Initialize();
            } // End of Initialize

        protected override void LoadContent()   // Load the content for the game
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);


            // Load font
            mainFont = Content.Load<SpriteFont>("quartz4");                                                                                     // Load the main font

            // Load background textures
            // gameBackground = new BackgroundStruct(Content, "background", GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);     // Load the game background TODO: Add background texture
            gameOverBackground = new BackgroundStruct(Content, "gameover", GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);      // Load the game over background

            // Load sounds
            // SoundEffect playerMoveSFX = Content.Load<SoundEffect>("playerMove");                                                               // Load the player move sound effect TODO: Add sound effect

            // Load the player sprite
            player = new PlayerSprite(Content, "Archer1");                                                                                      // Load the player sprite
            
            // Arrow setup
            for (int i = 0; i < arrows.Length; i++)                                                                                             // Loop through the arrows
            {
                arrows[i].arrowTexture = Content.Load<Texture2D>("ArrowOutline");                                                              // Load the arrow texture
                arrows[i].arrowRectangle = new Rectangle(0, 0, arrows[i].arrowTexture.Width, arrows[i].arrowTexture.Height);                   // Set the arrow rectangle
                arrows[i].arrowOrigin.X = (float)arrows[i].arrowRectangle.Width / 2;                                                           // Set the x origin of the arrow
                arrows[i].arrowOrigin.Y = (float)arrows[i].arrowRectangle.Height / 2;                                                          // Set the y origin of the arrow
                arrows[i].arrowAlive = false;                                                                                                  // Set the arrow to not alive
            } // End of arrow setup

            // Enemy setup
            for (int i = 0; i < enemies.Length; i++)                                                                                           // Loop through the enemies
            {
                enemies[i].enemyTexture = Content.Load<Texture2D>("E1");                                                                      // Load the enemy texture
                enemies[i].enemyRectangle = new Rectangle(0, 0, enemies[i].enemyTexture.Width, enemies[i].enemyTexture.Height);                // Set the enemy rectangle
                enemies[i].enemyOrigin.X = (float)enemies[i].enemyRectangle.Width / 2;                                                          // Set the x origin of the enemy
                enemies[i].enemyOrigin.Y = (float)enemies[i].enemyRectangle.Height / 2;                                                         // Set the y origin of the enemy
                enemies[i].enemyAlive = false;                                                                                                  // Set the enemy to not alive
            } // End of enemy setup
        } // End of LoadContent

        KeyboardState previousKeyboardState = Keyboard.GetState();  // The previous keyboard state - Set once outside of update

        protected override void Update(GameTime gameTime) // Update the game - ran every frame
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit(); // Exit the game if the back button is pressed or the escape key is pressed

            // Get keyboard and mouse state
            KeyboardState currentKeyboardState = Keyboard.GetState();   // Get the current keyboard state - set every frame
            // MouseState currentMouseState = Mouse.GetState();            // Get the current mouse state - set every frame

            if (!gameOver)
            {
                // Get the mouse position in tiles
                mouseTilePosition = new Vector2(Mouse.GetState().Position.X / tileSize, Mouse.GetState().Position.Y / tileSize);    // Divide the mouse position by the tile size to get the tile position

                // Move the player character between lanes
                if (currentKeyboardState.IsKeyDown(Keys.Left) && !previousKeyboardState.IsKeyDown(Keys.Left) || currentKeyboardState.IsKeyDown(Keys.A) && !previousKeyboardState.IsKeyDown(Keys.A))
                {
                    Game1.Lane--; // Move to the left lane
                    // playerMoveSFX.Play();   // Play the player move sound effect TODO: Add sound effect
                }
                else if (currentKeyboardState.IsKeyDown(Keys.Right) && !previousKeyboardState.IsKeyDown(Keys.Right) || currentKeyboardState.IsKeyDown(Keys.D) && !previousKeyboardState.IsKeyDown(Keys.D))
                {
                    Game1.Lane++; // Move to the right lane
                    // playerMoveSFX.Play();   // Play the player move sound effect TODO: Add sound effect
                }

                // Set player positions based on lane - TODO: Move to own function, maybe properly define lanes...
                if (Game1.Lane == 0)
                    player.playerPosition = new Vector2(GraphicsDevice.Viewport.Width - tileSize * 10, tileSize / 2);       // Set the player position to the left most lane - 0
                else if (Game1.Lane == 1)
                    player.playerPosition = new Vector2(GraphicsDevice.Viewport.Width - tileSize * 8, tileSize / 2);        // Set player position to the next lane to the right - 1
                else if (Game1.Lane == 2)
                    player.playerPosition = new Vector2(GraphicsDevice.Viewport.Width - tileSize * 6, tileSize / 2);        // Set player position to the next lane to the right - 2
                else if (Game1.Lane == 3)
                    player.playerPosition = new Vector2(GraphicsDevice.Viewport.Width - tileSize * 4, tileSize / 2);        // Set player position to the next lane to the right - 3
                else if (Game1.Lane == 4)
                    player.playerPosition = new Vector2(GraphicsDevice.Viewport.Width - tileSize * 2, tileSize / 2);        // Set player position to the right most lane - 4

                // Shooting arrows
                if (currentKeyboardState.IsKeyDown(Keys.Space) && !previousKeyboardState.IsKeyDown(Keys.Space)) // If the space key is pressed
                {
                    for (int i = 0; i < arrows.Length; i++) // Loop through the arrows
                        if (!arrows[i].arrowAlive) // If the arrow is not alive
                        {
                            arrows[i].arrowAlive = true; // Set the arrow to alive
                            arrows[i].arrowPosition = new Vector3(player.playerPosition.X, player.playerPosition.Y, 0); // Set the arrow position
                            break; // Break the loop
                        }
                }

                // Arrow movement
                for (int i = 0; i < arrows.Length; i++) // Loop through the arrows
                    if (arrows[i].arrowAlive) // If the arrow is alive
                    {
                        // Move arrow down the screen
                        arrows[i].arrowPosition.Y += 10; // Move the arrow down the screen
                        arrows[i].arrowBoundingSphere = new BoundingSphere(arrows[i].arrowPosition, arrows[i].arrowRectangle.Width / 2); // Set the bounding sphere of the arrow

                        // Check if the arrow is off bottom of the screen
                        if (arrows[i].arrowPosition.Y > _graphics.PreferredBackBufferHeight) // If the arrow is off the bottom of the screen
                            arrows[i].arrowAlive = false; // Set the arrow to not alive
                    }

                  // Enemy spawning attempt 2 - spawns one enemy at a time with a second delay
                    for (int i = 0; i < enemies.Length; i++) // Loop through the enemies
                        if (!enemies[i].enemyAlive) // If the enemy is not alive
                        {
                            enemies[i].spawnTime -= (float)gameTime.ElapsedGameTime.TotalSeconds; // Decrease the spawn time
                            if (enemies[i].spawnTime <= 0) // If the spawn time is less than or equal to 0
                            {
                                enemies[i].enemyAlive = true; // Set the enemy to alive
                                enemies[i].enemyHealth = 30; // Set the enemy health

                                // Set enemy position to spawn at the bottom of the screen in one of the 5 lanes
                                if (rand.Next(0, 5) == 0) // If the random number is 0
                                    enemies[i].enemyPosition = new Vector2(GraphicsDevice.Viewport.Width - tileSize * 10, 1088); // Set the enemy position to the left most lane
                                else if (rand.Next(0, 5) == 1) // If the random number is 1
                                    enemies[i].enemyPosition = new Vector2(GraphicsDevice.Viewport.Width - tileSize * 8, 1088); // Set the enemy position to the next lane to the right
                                else if (rand.Next(0, 5) == 2) // If the random number is 2
                                    enemies[i].enemyPosition = new Vector2(GraphicsDevice.Viewport.Width - tileSize * 6, 1088); // Set the enemy position to the next lane to the right
                                else if (rand.Next(0, 5) == 3) // If the random number is 3
                                    enemies[i].enemyPosition = new Vector2(GraphicsDevice.Viewport.Width - tileSize * 4, 1088); // Set the enemy position to the next lane to the right
                                else if (rand.Next(0, 5) == 4) // If the random number is 4
                                    enemies[i].enemyPosition = new Vector2(GraphicsDevice.Viewport.Width - tileSize * 2, 1088); // Set the enemy position to the right most lane
                                enemies[i].spawnTime = 5;
                                break; // Break the loop
                            }
                        }

                // Enemy movement
                for (int i = 0; i < enemies.Length; i++) // Loop through the enemies
                    if (enemies[i].enemyAlive) // If the enemy is alive
                    {
                        enemies[i].enemyPosition.Y -= 1; // Move the enemy up the screen - TODO: Change to enemy speed - currently does not work
                        enemies[i].enemyRectangle.X = (int)enemies[i].enemyPosition.X; // Set the x position of the enemy rectangle
                        enemies[i].enemyRectangle.Y = (int)enemies[i].enemyPosition.Y; // Set the y position of the enemy rectangle
                        enemies[i].enemyBoundingSphere = new BoundingSphere(new Vector3(enemies[i].enemyPosition.X, enemies[i].enemyPosition.Y, 0), enemies[i].enemyRectangle.Width / 2); // Set the bounding sphere of the enemy

                        // Check if the enemy is off the top of the screen
                        if (enemies[i].enemyPosition.Y < 2 * tileSize) // If the enemy is off the top of the screen
                        {
                            enemies[i].enemyAlive = false; // Set the enemy to not alive
                            player.playerHealth -= 10; // Decrease the player health
                        }
                    }

                    // Arrow collision with enemies
                    for (int i = 0; i < arrows.Length; i++) // Loop through the arrows
                        if (arrows[i].arrowAlive) // If the arrow is alive
                            for (int j = 0; j < enemies.Length; j++) // Loop through the enemies
                                if (enemies[j].enemyAlive) // If the enemy is alive
                                    if (arrows[i].arrowBoundingSphere.Intersects(enemies[j].enemyBoundingSphere)) // If the arrow intersects the enemy
                                    {
                                        arrows[i].arrowAlive = false; // Set the arrow to not alive
                                        enemies[j].enemyHealth -= 10; // Decrease the enemy health
                                        if (enemies[j].enemyHealth <= 0) // If the enemy health is less than or equal to 0
                                        {
                                            enemies[j].enemyAlive = false; // Set the enemy to not alive
                                            enemies[j].enemyHealth = 30; // Reset the enemy health
                                            enemies[j].enemyPosition = new Vector2(); // Reset the enemy position
                                            player.score += 10; // Increase the player score
                                        }
                                    }

                    // Game over
                    if (player.playerHealth <= 0) // If the player health is less than or equal to 0
                        gameOver = true; // Set the game to over
            }   // End of !gameOver if
            else // If the game is over -- Do I need this? think it can be removed but idk yet tbh
            {
                gameOver = true; // Set the game to over
            } // End of gameOver else
            previousKeyboardState = currentKeyboardState; // Set the previous keyboard state - Must be at the end of the update method
            base.Update(gameTime); // Update the game - Stays at the end of the update method   Down
        } // End of Update method                                                                ^^

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.LightYellow);

            _spriteBatch.Begin();

            if (gameOver)   // If the game is over
            {
                _spriteBatch.Draw(gameOverBackground.backgroundTexture, gameOverBackground.backgroundRectangle, Color.White);       // Draw the game over screen
                _spriteBatch.DrawString(mainFont, "Score: " + player.score, new Vector2((GraphicsDevice.Viewport.Width / 2) - 50, GraphicsDevice.Viewport.Height - 150), Color.White); // Draw the score
            }
            else            // If the game is not over
            {
                // Draw the background
                // _spriteBatch.Draw(gameBackground.backgroundTexture, gameBackground.backgroundRectangle, Color.White);    // Draw the background TODO: Add background texture
                // Draw grey as placeholder till background is added
                _spriteBatch.Draw(tileTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, tileSize), Color.LightGray); // Draw the light grey box for under player feet
                _spriteBatch.Draw(tileTexture, new Rectangle(0, tileSize, GraphicsDevice.Viewport.Width, tileSize), Color.DarkGray); // Draw the dark grey box to represent the castle
                _spriteBatch.Draw(tileTexture, new Rectangle((3 * tileSize), (2*tileSize), tileSize, GraphicsDevice.Viewport.Height), Color.DarkGray); // Draw the dark grey box for the lane
                _spriteBatch.Draw(tileTexture, new Rectangle((14 * tileSize), (2 * tileSize), tileSize, GraphicsDevice.Viewport.Height), Color.DarkGray); // Draw the dark grey box for the lane
                // ^^Temp stuff for background


                _spriteBatch.Draw(tileTexture, new Rectangle(0, 0, tileSize * 3, GraphicsDevice.Viewport.Height), Color.Black);    // Draw the black box for under text

                // If the mouse has the same x axis as one of the lanes highlight the tile the mouse is over
                if (mouseTilePosition.X > 3 && mouseTilePosition.X < 14 && mouseTilePosition.Y > 1) // If the mouse is over the lanes
                    _spriteBatch.Draw(tileTexture, new Rectangle((int)mouseTilePosition.X * tileSize, (int)mouseTilePosition.Y * tileSize, tileSize, tileSize), Color.Red); // Draw the tile in red so it is clear to the player
                    
                // Draw the tiles
                for (int x = 0; x < GraphicsDevice.Viewport.Width; x += tileSize)                                                   // Loop through the screen width
                    _spriteBatch.Draw(tileTexture, new Rectangle(x, 0, 1, GraphicsDevice.Viewport.Height), Color.Black);            // Draw a vertical line

                for (int y = 0; y < GraphicsDevice.Viewport.Height; y += tileSize)                                                  // Loop through the screen height
                    _spriteBatch.Draw(tileTexture, new Rectangle(0, y, GraphicsDevice.Viewport.Width, 1), Color.Black);             // Draw a horizontal line

                // Draw on screen text
                _spriteBatch.DrawString(mainFont, "Score: " + player.score, new Vector2(10, 10), Color.White);                  // Draw the score
                _spriteBatch.DrawString(mainFont, "Enemies: " + 0, new Vector2(10, 40), Color.White);                           // Draw the enemies remaining.... when possible TODO: Add enemies remaining
                _spriteBatch.DrawString(mainFont, "Wave: " + 1, new Vector2(10, GraphicsDevice.Viewport.Height - 40), Color.White); // Draw the wave number.... when possible TODO: Add enemies remaining
                _spriteBatch.DrawString(mainFont, "HP: ", new Vector2(10, GraphicsDevice.Viewport.Height - 80), Color.White); // Draw the player health
                _spriteBatch.Draw(tileTexture, new Rectangle(65, GraphicsDevice.Viewport.Height - 75, 184, 24), Color.White); // Draw the player health bar outline
                _spriteBatch.Draw(tileTexture, new Rectangle(67, GraphicsDevice.Viewport.Height - 73, player.playerHealth * 2, 20), Color.Red); // Draw the player health bar
                // ^^That is quite lazy, will need to change healthbar to be a set size and diminish based on a percentage of health lost


                /// ENEMIES ///
                for (int i = 0; i < enemies.Length; i++) // Loop through the enemies
                    if (enemies[i].enemyAlive) // If the enemy is alive
                        _spriteBatch.Draw(enemies[i].enemyTexture, enemies[i].enemyPosition, null, Color.White, 0, enemies[i].enemyOrigin, 0.175f, SpriteEffects.None, 0); // Draw the enemy

                // Draw the player
                _spriteBatch.Draw(player.playerTexture, player.playerPosition, null, Color.White, 0, player.playerOrigin, 0.175f, SpriteEffects.None, 0); // Draw the player

                // Draw the arrows
                for (int i = 0; i < arrows.Length; i++) // Loop through the arrows
                    if (arrows[i].arrowAlive) // If the arrow is alive
                        _spriteBatch.Draw(arrows[i].arrowTexture, new Vector2(arrows[i].arrowPosition.X, arrows[i].arrowPosition.Y), null, Color.White, 0, arrows[i].arrowOrigin, 0.5f, SpriteEffects.None, 0); // Draw the arrow
            }   // End of !gameOver else
            _spriteBatch.End();
            base.Draw(gameTime);
        } // End of Draw

        // User defined functions
        // Dictionary to store the vector positions of the lanes for the player
        void LaneCoordinates()
        {
            Dictionary<int, Vector2> laneCoordinates = new Dictionary<int, Vector2>
            {
                { 0, new Vector2(GraphicsDevice.Viewport.Width - tileSize * 10, tileSize / 2) },
                { 1, new Vector2(GraphicsDevice.Viewport.Width - tileSize * 8, tileSize / 2) },
                { 2, new Vector2(GraphicsDevice.Viewport.Width - tileSize * 6, tileSize / 2) },
                { 3, new Vector2(GraphicsDevice.Viewport.Width - tileSize * 4, tileSize / 2) },
                { 4, new Vector2(GraphicsDevice.Viewport.Width - tileSize * 2, tileSize / 2) }
            };
        } // End of LaneCoordinates

        // Dictionary to store the vector positions of the lanes for the enemies
        void EnemyLaneCoordinates()
        {
            Dictionary<int, Vector2> enemyLaneCoordinates = new Dictionary<int, Vector2>
            {
                { 0, new Vector2(GraphicsDevice.Viewport.Width - tileSize * 10, 1088) },
                { 1, new Vector2(GraphicsDevice.Viewport.Width - tileSize * 8, 1088) },
                { 2, new Vector2(GraphicsDevice.Viewport.Width - tileSize * 6, 1088) },
                { 3, new Vector2(GraphicsDevice.Viewport.Width - tileSize * 4, 1088) },
                { 4, new Vector2(GraphicsDevice.Viewport.Width - tileSize * 2, 1088) }
            };
        } // End of EnemyLaneCoordinates
    } // End of Game1 class
} // End of GradedUnit Program
