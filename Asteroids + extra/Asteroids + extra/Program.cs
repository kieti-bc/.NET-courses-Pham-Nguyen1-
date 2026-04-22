using Raylib_cs;
using System.Drawing;
using System.Numerics;
using System.Text.Json;
using Color = Raylib_cs.Color;

/*
 * Tia comments:
 * These comments aim for better readability of the code
 * and how it would work better in a work environment.
 * 
 * I believe that good code explains the intent: The way
 * things are done tells about what their purpose is.
 */

namespace Asteroids
{
    // Game states
    enum GameState { Menu, Playing, GameOver }

    /* Each class in a separate file.
     * Why: Avoid merge conflicts, allow multiple programmers to share
     * a project. Find what you are looking for faster. Makes it easier
     * to replace a class with another and move classes between projects
     * 
     * Visual Studio can do it automatically:
     * - Right click on the class name eg. HighScoreData
     * - From context menu select top option "Quick Actions and Refactorings..."
     * - Select "Move type to HighScoreData.cs"
     */
 

    // Data saved to JSON file
    class HighScoreData
    {
        /* Properties are cool but if it is public {get; set;} then 
         * it can be just a normal variable
         */
        public int HighScore { get; set; } = 0;
        public string PlayerName { get; set; } = "AAA";
    }

    // Handles movement and screen wrapping
    public class Transform
    {
        public Vector2 position;
        public Vector2 velocity;
        public float maxSpeed;
        public Vector2 direction;
        public float rotationRadians;

        /* See comment below: */
        public static readonly Vector2 DefaultDirection = new Vector2(0, -1);

        public Transform(Vector2 position, float maxSpeed)
        {
            this.position = position;
            this.maxSpeed = maxSpeed;
            this.velocity = Vector2.Zero;
            /* If the game has a default direction like this
             * it is better to move it to a static class variable.
             * That way it has a clear name and can be used everywhere and
             * changed easily
             */
            this.direction = new Vector2(0f, -1f); // default direction: up
        }

        public void Move()
        {
            float dt = Raylib.GetFrameTime();

            // Cap velocity to maxSpeed
            if (velocity.Length() > maxSpeed)
                velocity = Vector2.Normalize(velocity) * maxSpeed;

            position += velocity * dt;

            // Wrap around screen edges
            int sw = Raylib.GetScreenWidth();
            int sh = Raylib.GetScreenHeight();

            /* This works but looks messy. if() can be written
             * without curly braces, but that makes the if only apply
             * to the next instructions.
             * If you ever add anything else you might forget the braces
             */
            
            if (position.X < 0) position.X += sw;
            else if (position.X >= sw) position.X -= sw;
            if (position.Y < 0) position.Y += sh;
            else if (position.Y >= sh) position.Y -= sh;
        }

        /* This is a good way to do the turning.
         * It uses the functions available in C#'s standard library
         * and has a clear name.
         * You could make it a public static function of Transform
         * that can rotate _any_ vector2 and then use it everywhere else too.
         */
        public void Turn(float amountRadians)
        {
            rotationRadians += amountRadians;
            direction = Vector2.Transform(direction, Matrix3x2.CreateRotation(amountRadians));
        }

        public void AddForceToDirection(float force)
        {
            // Push ship forward in the direction it faces
            velocity += direction * force * Raylib.GetFrameTime();
        }
    }

    // Stores collision radius and checks circle vs circle hits
    public class Collision
    {
        public float radius;

        public Collision(float radius) { this.radius = radius; }

        public static bool CheckCollision(Transform tA, Collision cA, Transform tB, Collision cB)
        {
            return Raylib.CheckCollisionCircles(tA.position, cA.radius, tB.position, cB.radius);
        }
    }

    /* Not a good idea to put numbers in comments. 
     * If you ever change the code you might forget to change
     * the comment
     */

    // Bullet: flies straight, disappears after 2 seconds
    public class Bullet
    {
        public Transform transform;
        public Collision collision;
        public bool active = true;
        private float lifetime = 2.0f;

        public Bullet(Vector2 position, Vector2 velocity)
        {
            /* Here the 600.0f and 4f are what are called
             * "Magic Numbers"
             * The name means that the numbers just work but
             * the reader has no idea what they mean.
             * Also there is a danger of duplicating the same information
             * in multiple places and forgetting to change one of them.
             * 
             * Better to have them as variables with names.
             * That way they can be easily found and changed
             * float maxSpeed = 600.0f;
             * float collisionRadius = 4f
             */
            transform = new Transform(position, 600f);
            transform.velocity = velocity;
            collision = new Collision(4f);

            /* C# also allows this which is okay too. Now the reader
             * knows what the 4f is.*/
            collision = new Collision(radius: 4f);
        }

        public void Update()
        {
            transform.Move();
            lifetime -= Raylib.GetFrameTime();
			/* The variable name active implies that the Bullet
             * can be active or inactive. But what actually happens that
             * inactive bullets are immediately deleted.
             * Change the variable name to something like "bool deleteMe"
             * or better yet, use bullet pooling, because bullets are
             * constantly created and destroyed.
             * 
             * With a bullet pool:
			 * When a new bullet is needed, one of the inactive bullets
             * is changed to active and given a new position.
             * Read: https://gameprogrammingpatterns.com/object-pool.html
             */
            if (lifetime <= 0f) active = false;
        }

        public void Draw(Color color)
        {
            /* Magic number again. Since Bullet clas owns
             * the collider component, the code should get the collision
             * radius from there. That way the graphics and collision 
             * area match automatically */
            Raylib.DrawCircleV(transform.position, 4f, color);
        }
    }

    /* The enums get values automatically to 0, 1, 2, 3,
     * If you want something else you can just set the first one
     * and the others will automatically increase by one.
     * So in this case you could write:
     * public enum AsteroidSize { Small = 1, Medium, Large }
     * 
     * I would maybe move the enum inside the Asteroid class
     * and just call it Size or similar.
     */
    public enum AsteroidSize { Large = 3, Medium = 2, Small = 1 }

    /* Use images instead. The problem with line graphics is that the
     * line size is related to monitor pixel size. The line looks way
     * thinner on Retina display or other high resolution screen.
     * See course material on Loading and drawing images and can also look at raylib examples
     */
    // Asteroid: random jagged polygon that slowly spins as it drifts
    public class Asteroid
    {
        public Transform transform;
        public Collision collision;
        public AsteroidSize size;
        private Vector2[] points; // polygon vertices

        public Asteroid(Vector2 position, AsteroidSize size, Vector2? overrideVelocity = null)
        {
            this.size = size;
            /* Using Ternary Operators is illegal :D 
             * They are very difficult to read quickly and the values
             * are scattered all over.
             * Use a switch instead and if you ever need the values
             * somewhere else, make a function that converts AsteroidSize to radius.
             */
            float radius = size == AsteroidSize.Large ? 40f : size == AsteroidSize.Medium ? 24f : 13f;
            float speed = size == AsteroidSize.Large ? 60f : size == AsteroidSize.Medium ? 110f : 180f;

            /* Magic Number. Why is the speed multiplied by 2 here. Just
             * use bigger values when setting it
             */
            transform = new Transform(position, speed * 2f);
            collision = new Collision(radius);

            // Random offsets give each asteroid a rocky look
            /* When you create a new Random generator it
             * uses the current time as seed so every asteroid is different.
             * Or it might not and then they all look the same.
             * 
             * Also magic numbers again. Use image instead.
             */
            Random rng = new Random();
            int n = 10;
            points = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float angle = i / (float)n * MathF.Tau;
                float r = radius * (0.7f + rng.NextSingle() * 0.5f);
                points[i] = new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r);
            }

            if (overrideVelocity.HasValue)
                transform.velocity = overrideVelocity.Value;
            else
            {
                /* Here we could use the functions and variables
                 * presented in earlier comments to make it very clear
                 * what is happening:
                 * transform.velocity = Transform.RotateVector(Transform.DefaultDirection, angle2) * speed;
                 * 
                 * Or even better, make a function for generating random
                 * directions. You might need it later...
                 *
                 * transform.velocity = Transform.GetRandomDirection() * speed;
                 * 
                 * The AI has no problem writing Cos(a), Sin(a) all over again
                 * but do you really understand why and how they work?
                 * It is better to use a named function to make it clear.
                 */
                float angle2 = rng.NextSingle() * MathF.Tau;
                transform.velocity = new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * speed;
            }
        }

        public void Update()
        {
            transform.Move();
            /* Magic number! Move this to class variables or
             add it as a class variable to Transform as rotationSpeed so everything can spin.
            */
            transform.rotationRadians += 0.01f; // slow spin each frame
        }

        /* Use image*/
        public void Draw()
        {
            Vector2 pos = transform.position;
            float rot = transform.rotationRadians;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = Rotate(points[i], rot) + pos;
                Vector2 b = Rotate(points[(i + 1) % points.Length], rot) + pos;
                Raylib.DrawLineV(a, b, Color.LightGray);
            }
        }

        /* This is the function that should be in the Transform component,
         * not in Asteroid class 
         * And it should use the Matrix3x2 and not Cos and Sin directly.
         */
        private Vector2 Rotate(Vector2 v, float angle)
        {
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }
    }

    // Player ship: rotates, thrusts, shoots
    public class Ship
    {
        public Transform transform;
        public Collision collision;
        public List<Bullet> bullets = new();
        public float invincibleTimer = 0f; // invincibility timer after respawn

        private float shootInterval = 0.25f;
        private float lastShootTime;
        private float bulletSpeed = 500f;

        public Ship(Vector2 position)
        {
            /* magic numbers again */
            transform = new Transform(position, 350f);
            collision = new Collision(16f);
            lastShootTime = -shootInterval; // allow shooting immediately at start
        }

        /* This is a good function to have. 
         * Here we could again use Transform.DefaultDirection
         * or even better, have function in Transform that
         * stops and resets. Then we could reset other things too.
         * Like this: transform.ResetAndStop(position);
         */
        public void Reset(Vector2 position)
        {
            transform.position = position;
            transform.velocity = Vector2.Zero;
            transform.rotationRadians = 0f;
            transform.direction = new Vector2(0f, -1f);
            bullets.Clear();
            /* Magic number, move to class variables, next to invicibleTimer*/
            invincibleTimer = 2.5f; // 2.5 seconds of invincibility after respawn
        }

        public void Update()
        {
            /* This works but the method of saving the event time
             * is more elegant because that way you don't have to update
             * the timer yourself.
             * You are already using it for shooting so use it for invisibility too. Otherwise a reader could think that sometimes invicibilityTimer does not advance at all or advances faster
             */
            if (invincibleTimer > 0f) invincibleTimer -= Raylib.GetFrameTime();

            /* Magic number. Add turnSpeedRadians to Transform and keep it 
             * there.
             * Also no need to call Raylib.GetFrameTime() multiple times. 
             * Call it once at the beginning of update like this and then
             * use deltatime for calculations.
             * const float deltatime = Raylib.GetFrameTime();
             */

            // Rotate left or right
            
            if (Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.A))
                transform.Turn(-2.5f * Raylib.GetFrameTime());
            if (Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.D))
                transform.Turn(2.5f * Raylib.GetFrameTime());

            // Thrust forward
            if (Raylib.IsKeyDown(KeyboardKey.Up) || Raylib.IsKeyDown(KeyboardKey.W))
                transform.AddForceToDirection(220f);

            transform.Move();

            // Shoot, limited by shootInterval cooldown
            bool wantShoot = Raylib.IsKeyDown(KeyboardKey.Space) || Raylib.IsKeyDown(KeyboardKey.Z);
            if (wantShoot && (float)Raylib.GetTime() - lastShootTime > shootInterval)
            {
                lastShootTime = (float)Raylib.GetTime();
                /* Guess what, magic!"*/
                Vector2 vel = transform.direction * bulletSpeed + transform.velocity * 0.3f;
                bullets.Add(new Bullet(transform.position, vel));
            }

            // Remove expired bullets
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Update();
                if (!bullets[i].active) bullets.RemoveAt(i);
            }
        }

        public void Draw()
        {
            /* This is just an example no need to fix!
             * 
             * Here you could do a cool effect that makes the ship 
             * blink slower when invincibility is about to end. That 
             * way player can anticipate it ending soon.
             */
            float invicibilityDuration = 2.5f; // See how it would help to have this as a class variable and not a magic number!
            float invLeft = invincibleTimer;
            // This value is between [0,1] and gets closer to 0 when inv is about to end
            float relativeUntil = invincibleTimer / invicibilityDuration;
            // Multiplying the blinkspeed (8) with this makes the blinking slower as timer advances

            // Blink while invincible
            if (invincibleTimer > 0f && (int)(invincibleTimer * (8.0f * relativeUntil)) % 2 == 0) return;

            Vector2 pos = transform.position;
            float rot = transform.rotationRadians;

            // Draw ship as a triangle
            Vector2 tip = pos + Rotate(new Vector2(0, -18), rot);
            Vector2 left = pos + Rotate(new Vector2(-11, 12), rot);
            Vector2 right = pos + Rotate(new Vector2(11, 12), rot);
            Vector2 mid = pos + Rotate(new Vector2(0, 6), rot);

            Raylib.DrawLineV(tip, left, Color.SkyBlue);
            Raylib.DrawLineV(tip, right, Color.SkyBlue);
            Raylib.DrawLineV(left, mid, Color.SkyBlue);
            Raylib.DrawLineV(right, mid, Color.SkyBlue);

            // Engine flame when thrusting
            if (Raylib.IsKeyDown(KeyboardKey.Up) || Raylib.IsKeyDown(KeyboardKey.W))
            {
                Vector2 fl = pos + Rotate(new Vector2(-6, 12), rot);
                Vector2 fr = pos + Rotate(new Vector2(6, 12), rot);
                Vector2 ft = pos + Rotate(new Vector2(0, 24), rot);
                Raylib.DrawLineV(fl, ft, Color.Orange);
                Raylib.DrawLineV(fr, ft, Color.Yellow);
            }

            /* Magic color!
             * If the designer tells you to change the color of bullets
             * would you remember that their color is hidden at the end of Ship.Draw()?
             */
            foreach (var b in bullets) b.Draw(Color.Yellow);
        }

		/* This another way of writing a property:
         * public bool IsInvincible { get {return invincibleTimer > 0f;} }
         * Either way is fine.
         */
        public bool IsInvincible() => invincibleTimer > 0f;

		/*
         * Wait, have seen this one before somewhere?
         * If there is two of them, is the other different or is it
         * meant to be different? What is the intent?
         * 
         * There is an old programming rule: "Don't Repeat Yourself"
         * shortened to "The DRY principle"
         * https://en.wikipedia.org/wiki/Don%27t_repeat_yourself
         */
		private Vector2 Rotate(Vector2 v, float angle)
        {
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }
    }

    // Enemy: drifts randomly and shoots in random directions
    public class Enemy
    {
        public Transform transform;
        public Collision collision;
        public List<Bullet> bullets = new();

        private float shootInterval;
        private float lastShootTime;
        /* Here the random generator is a class variable. Why? */
        private Random rng = new Random();

        public Enemy(Vector2 position)
        {
            /* See previous comments about what is wrong with this */
            transform = new Transform(position, 150f);
            collision = new Collision(18f);

            // Random drift direction and speed
            float angle = rng.NextSingle() * MathF.Tau;
            transform.velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle))
                               * (60f + rng.NextSingle() * 80f);

            shootInterval = 1.5f + rng.NextSingle() * 2f;
            lastShootTime = (float)Raylib.GetTime();
        }

        public void Update()
        {
            transform.Move();

            /* Here we could again use our Transform.GetRandomDirection() function */
            // Shoot at random intervals in a random direction
            if ((float)Raylib.GetTime() - lastShootTime > shootInterval)
            {
                lastShootTime = (float)Raylib.GetTime();
                shootInterval = 1.5f + rng.NextSingle() * 2f;
                float angle = rng.NextSingle() * MathF.Tau;
                Vector2 vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 200f;
                bullets.Add(new Bullet(transform.position, vel));
            }

            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Update();
                if (!bullets[i].active) bullets.RemoveAt(i);
            }
        }

        public void Draw()
        {
            Vector2 pos = transform.position;
            // Draw enemy as a red diamond
            Raylib.DrawLineV(pos + new Vector2(0, -20), pos + new Vector2(20, 0), Color.Red);
            Raylib.DrawLineV(pos + new Vector2(20, 0), pos + new Vector2(0, 20), Color.Red);
            Raylib.DrawLineV(pos + new Vector2(0, 20), pos + new Vector2(-20, 0), Color.Red);
            Raylib.DrawLineV(pos + new Vector2(-20, 0), pos + new Vector2(0, -20), Color.Red);
            Raylib.DrawLineV(pos + new Vector2(-20, 0), pos + new Vector2(20, 0), Color.DarkGray);

            foreach (var b in bullets) b.Draw(Color.Orange);
        }
    }

    internal class Program
    {
        static int screenW = 900;
        static int screenH = 700;

        /* null! means that the player can be null. Why? Does not make sense. */
        static Ship player = null!;
        /* Also don't static all of this. Just useless typing in this case
         * see comment on Main later */
        static List<Asteroid> asteroids = new();
        static List<Enemy> enemies = new();

        static int score = 0;
        static int lives = 3;
        static int level = 1;
        static int highScore = 0;

        static GameState state = GameState.Menu;
        /* If the game has a random generator, could everything else use the same one? */
        static Random rng = new Random();

        // File where high score is saved
        static string saveFile = "highscore.json";

        /* Main is needed for a starting point, but because it is
         * static, it can only use static variables. 
         * Better to make an object that represents the game 
         * and just launch that in Main like this:
         */
        static void Main(string[] args)
        {
		   /* 
			* Program game = new Program();
            * game.Run()
            * 
            * } // Main ends

            
             * 
             * void Run() { 
             */
            Raylib.InitWindow(screenW, screenH, "Asteroids");
            Raylib.SetTargetFPS(60);

            LoadHighScore(); // read saved high score on startup

            while (!Raylib.WindowShouldClose())
            {
                Update();
                Draw();
            }

            Raylib.CloseWindow();
        }

        // Load high score from JSON on startup
        static void LoadHighScore()
        {
            if (!File.Exists(saveFile)) return;
            try
            {
                string json = File.ReadAllText(saveFile);
                var data = JsonSerializer.Deserialize<HighScoreData>(json);
                if (data != null) highScore = data.HighScore;
            }
            catch { } // corrupted file to just start fresh
        }

        // Write high score to JSON file
        static void SaveHighScore()
        {
            var data = new HighScoreData { HighScore = highScore };
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(saveFile, json);
        }

        static void StartGame()
        {
            score = 0; lives = 3; level = 1;
            state = GameState.Playing;
            player = new Ship(new Vector2(screenW / 2f, screenH / 2f));
            SpawnLevel(level);
        }

        static void SpawnLevel(int lvl)
        {
            asteroids.Clear();
            enemies.Clear();

            // More asteroids each level
            for (int i = 0; i < 3 + lvl; i++)
                asteroids.Add(new Asteroid(RandomPosAwayFromPlayer(), AsteroidSize.Large));

            /* Why doesn't enemy spawning also use RandomPosAwayFromPlayer() ?
             * Now the enemies can spaw on the player
             */
            // Enemies appear from level 2 onward
            for (int i = 0; i < lvl / 2; i++)
            {
                /* This ternary operator is very illegal */
                Vector2 pos = rng.Next(2) == 0
                    ? new Vector2(rng.Next(screenW), rng.Next(2) == 0 ? 0 : screenH)
                    : new Vector2(rng.Next(2) == 0 ? 0 : screenW, rng.Next(screenH));
                enemies.Add(new Enemy(pos));
            }
        }

        /* Here the distance is again a magic number
         * make it a parameter of the function instead
         */
        // Pick a random spot at least 150px away from the player
        static Vector2 RandomPosAwayFromPlayer()
        {
            Vector2 center = player?.transform.position ?? new Vector2(screenW / 2f, screenH / 2f);
            Vector2 pos;
            do { pos = new Vector2(rng.Next(screenW), rng.Next(screenH)); }
            while (Vector2.Distance(pos, center) < 150f);
            return pos;
        }

        /* Here the instructions shown on the screen and 
         * the input code are far removed.
         * In this case it is okay to put drawing and update
         * code next to each other.
         * 
         * Also the way enums work is that state can only be one
         * of the values. You could use a switch statement here to
         * avoid typing so much.
         */
        static void Update()
        {
            if (state == GameState.Menu)
            {
                // Enter starts the game from the menu
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    StartGame();
                return;
            }

            if (state == GameState.GameOver)
            {
                // Enter goes back to menu
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    state = GameState.Menu;
                return;
            }

            // Game running
            player.Update();
            foreach (var a in asteroids) a.Update();
            foreach (var e in enemies) e.Update();

            CheckCollisions();

            // All asteroids gone to next level
            if (asteroids.Count == 0)
            {
                level++;
                player.Reset(new Vector2(screenW / 2f, screenH / 2f));
                SpawnLevel(level);
            }
        }

        static void CheckCollisions()
        {
            // Player bullets vs asteroids
            for (int b = player.bullets.Count - 1; b >= 0; b--)
            {
                bool hit = false;
                for (int a = asteroids.Count - 1; a >= 0; a--)
                {
                    if (Collision.CheckCollision(player.bullets[b].transform, player.bullets[b].collision,
                                                 asteroids[a].transform, asteroids[a].collision))
                    {
                        /* Quick! How much score does a Small asteroid give? 
                         * Can you figure it out or remember the scores are here.
                         * This too could be a function in the asteroid class like int AsteroidSizeToScore(AsteroidSize s)
                         */
                        score += asteroids[a].size == AsteroidSize.Large ? 20
                               : asteroids[a].size == AsteroidSize.Medium ? 50 : 100;
                        SplitAsteroid(asteroids[a]);
                        asteroids.RemoveAt(a);
                        /* Here the bullet is removed right away.
                         * But you could also set it inactive and it would be removed later on the Update
                         */
                        player.bullets.RemoveAt(b);
                        hit = true;
                        break;
                    }
                }
                if (hit) continue;

                // Player bullets vs enemies
                for (int e = enemies.Count - 1; e >= 0; e--)
                {
                    if (b < player.bullets.Count &&
                        Collision.CheckCollision(player.bullets[b].transform, player.bullets[b].collision,
                                                 enemies[e].transform, enemies[e].collision))
                    {
                        enemies.RemoveAt(e);
                        player.bullets.RemoveAt(b);
                        score += 200;
                        break;
                    }
                }
            }

            if (player.IsInvincible()) return;

            // Player touches asteroid
            foreach (var a in asteroids)
                if (Collision.CheckCollision(player.transform, player.collision, a.transform, a.collision))
                { PlayerDie(); return; }

            // Enemy bullet hits player
            foreach (var e in enemies)
                for (int b = e.bullets.Count - 1; b >= 0; b--)
                    if (Collision.CheckCollision(e.bullets[b].transform, e.bullets[b].collision,
                                                 player.transform, player.collision))
                    { e.bullets.RemoveAt(b); PlayerDie(); return; }
        }

        // Split asteroid into 2 smaller pieces
        static void SplitAsteroid(Asteroid a)
        {
            if (a.size == AsteroidSize.Small) return; // smallest size — just disappears
            /* Here you can just take one size smaller since
             * enums are numbers and large should be the largest number, right? 
             * newSize = (AsteroidSize) ( (int)a.size - 1);
             */
            AsteroidSize newSize = a.size == AsteroidSize.Large ? AsteroidSize.Medium : AsteroidSize.Small;
            /* Magic numbers. But are they same as in the Asteroid constructor. Did you remember to multiply by 2 here?
             */
            float speed = newSize == AsteroidSize.Medium ? 110f : 180f;
            for (int i = 0; i < 2; i++)
            {
                float angle = rng.NextSingle() * MathF.Tau;
                Vector2 vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
                asteroids.Add(new Asteroid(a.transform.position, newSize, vel));
            }
        }

        static void PlayerDie()
        {
            lives--;

            // Update and save high score if beaten
            if (score > highScore)
            {
                highScore = score;
                SaveHighScore();
            }

            if (lives <= 0)
                state = GameState.GameOver;
            else
                player.Reset(new Vector2(screenW / 2f, screenH / 2f));
        }

        static void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            if (state == GameState.Menu)
                DrawMenu();
            else if (state == GameState.GameOver)
                DrawGameOver();
            else
                DrawGame();

            Raylib.EndDrawing();
        }

        /* Making menus like this by hand is annoying.
         * Raylib has a separate library called RayGui
         * but at the very least this kind of function is very helpful
         * to have.
         * And from them you can make little UI library you can use in all your projects.
         */

        Vector2 GetNextLine(Vector2 position, float lineHeight)
        {
            return position - new Vector2(0, lineHeight);
        }
        void DrawTextCentered(string text, int fontSize, Vector2 line, Color color)
        {
			int tw = Raylib.MeasureText(title, fontSize);
			Raylib.DrawText(title, line.X - tw / 2, line.Y, 70, color);
		}
        /*
         * With these drawing menus is easier:
         */
        void DrawExampleMenu()
        {
            float lineHeight = 70;
            Vector2 line = new Vector2(screenW / 2, 0);
            DrawTextCentered("ASTEROIDS", lineHeight, line, Color.White);
            line = GetNextLine(line, lineHeight);

            lineHeight = 26;
            // etc...
        }

        static void DrawMenu()
        {
            // Title
            string title = "ASTEROIDS";
            int tw = Raylib.MeasureText(title, 70);
            Raylib.DrawText(title, screenW / 2 - tw / 2, screenH / 2 - 120, 70, Color.White);

            // Start prompt
            string start = "Press ENTER to play";
            int sw = Raylib.MeasureText(start, 26);
            Raylib.DrawText(start, screenW / 2 - sw / 2, screenH / 2, 26, Color.Yellow);

            // Controls hint
            string ctrl = "Arrows / WASD = move     Space / Z = shoot";
            int cw = Raylib.MeasureText(ctrl, 18);
            Raylib.DrawText(ctrl, screenW / 2 - cw / 2, screenH / 2 + 50, 18, Color.DarkGray);

            // High score display
            string hs = $"High Score: {highScore}";
            int hw = Raylib.MeasureText(hs, 22);
            Raylib.DrawText(hs, screenW / 2 - hw / 2, screenH / 2 + 110, 22, Color.Gold);
        }

        static void DrawGameOver()
        {
            string msg = "GAME OVER";
            int tw = Raylib.MeasureText(msg, 60);
            Raylib.DrawText(msg, screenW / 2 - tw / 2, screenH / 2 - 80, 60, Color.Red);

            string scoreText = $"Score: {score}";
            int sw = Raylib.MeasureText(scoreText, 28);
            Raylib.DrawText(scoreText, screenW / 2 - sw / 2, screenH / 2, 28, Color.White);

            // Show message if player beat the record
            if (score >= highScore && score > 0)
            {
                string newRecord = "New High Score!";
                int rw = Raylib.MeasureText(newRecord, 26);
                Raylib.DrawText(newRecord, screenW / 2 - rw / 2, screenH / 2 + 40, 26, Color.Gold);
            }

            string hsText = $"High Score: {highScore}";
            int hw = Raylib.MeasureText(hsText, 22);
            Raylib.DrawText(hsText, screenW / 2 - hw / 2, screenH / 2 + 80, 22, Color.Gold);

            string back = "Press ENTER to return to menu";
            int bw = Raylib.MeasureText(back, 20);
            Raylib.DrawText(back, screenW / 2 - bw / 2, screenH / 2 + 130, 20, Color.LightGray);
        }

        static void DrawGame()
        {
            foreach (var a in asteroids) a.Draw();
            foreach (var e in enemies) e.Draw();
            player.Draw();

            // HUD top-left
            Raylib.DrawText($"Score: {score}", 10, 10, 22, Color.White);
            Raylib.DrawText($"Level: {level}", 10, 36, 22, Color.Yellow);
            Raylib.DrawText($"Lives: {lives}", 10, 62, 22, Color.SkyBlue);
            Raylib.DrawText($"Best:  {highScore}", 10, 88, 22, Color.Gold);
        }
    }
}