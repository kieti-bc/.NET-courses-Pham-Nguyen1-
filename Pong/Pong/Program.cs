using Raylib_cs;
using System.Numerics;

class Program
{
    static void Main(string[] args)
    {
        Program pong = new Program();
        pong.RunGame(); // Start the game
    }

    //   Game variables  

    // Paddle size and speed (shared by both players)
    Vector2 playerSize = new Vector2(20, 100); // width, height
    float playerSpeed = 400f;                  // paddle movement speed
    int playerToWall = 30;                     // distance from wall

    // Player positions
    Vector2 player1;
    Vector2 player2;

    // Scores
    int player1Score = 0;
    int player2Score = 0;

    // Ball variables
    Vector2 ballPosition;
    Vector2 ballDirection;
    float ballSpeed = 300f;  // ball speed in pixels/sec
    float ballRadius = 10f;  // ball radius

    void RunGame()
    {
        Raylib.InitWindow(800, 600, "Pong"); // create window
        Raylib.SetTargetFPS(60);             // set frame rate

        InitGame(); // initialize player positions and ball

        while (!Raylib.WindowShouldClose()) // main game loop
        {
            Update(); // update game logic
            Draw();   // draw everything
        }

        Raylib.CloseWindow(); // close window when done
    }

    void InitGame()
    {
        int ScreenWidth = Raylib.GetScreenWidth();
        int ScreenHeight = Raylib.GetScreenHeight();

        // Initialize player positions at center
        player1 = new Vector2(
            playerToWall,
            ScreenHeight / 2f - playerSize.Y / 2f);

        player2 = new Vector2(
            ScreenWidth - playerSize.X - playerToWall,
            ScreenHeight / 2f - playerSize.Y / 2f);

        ResetBall(); // place ball at center
    }

    void ResetBall()
    {
        ballPosition = Raylib.GetScreenCenter(); // center of screen
        ballDirection = Vector2.Normalize(new Vector2(1f, 0.5f)); // initial direction
    }

    void Update()
    {
        float dt = Raylib.GetFrameTime(); // delta time
        int ScreenWidth = Raylib.GetScreenWidth();
        int ScreenHeight = Raylib.GetScreenHeight();

        //   Player movement  
        // Player 1 uses W and S
        if (Raylib.IsKeyDown(KeyboardKey.W))
            player1.Y -= playerSpeed * dt;
        else if (Raylib.IsKeyDown(KeyboardKey.S))
            player1.Y += playerSpeed * dt;

        // Player 2 uses arrow keys
        if (Raylib.IsKeyDown(KeyboardKey.Up))
            player2.Y -= playerSpeed * dt;
        else if (Raylib.IsKeyDown(KeyboardKey.Down))
            player2.Y += playerSpeed * dt;

        //   Clamp paddles inside screen  
        if (player1.Y < 0) player1.Y = 0;
        else if (player1.Y + playerSize.Y > ScreenHeight)
            player1.Y = ScreenHeight - playerSize.Y;

        if (player2.Y < 0) player2.Y = 0;
        else if (player2.Y + playerSize.Y > ScreenHeight)
            player2.Y = ScreenHeight - playerSize.Y;

        //   Ball movement  
        ballPosition += ballDirection * ballSpeed * dt;

        //   Ball collision with top/bottom walls  
        if (ballPosition.Y - ballRadius <= 0)
        {
            ballDirection.Y = MathF.Abs(ballDirection.Y); // bounce down
        }
        else if (ballPosition.Y + ballRadius >= ScreenHeight)
        {
            ballDirection.Y = -MathF.Abs(ballDirection.Y); // bounce up
        }

        //   Ball collision with left/right walls  
        if (ballPosition.X - ballRadius <= 0) // left wall
        {
            player2Score++;  // player 2 scores
            ResetBall();     // reset ball
        }
        else if (ballPosition.X + ballRadius >= ScreenWidth) // right wall
        {
            player1Score++;  // player 1 scores
            ResetBall();     // reset ball
            ballDirection = Vector2.Normalize(new Vector2(-1f, 0.5f)); // start left
        }

        //   Ball collision with paddles  
        Rectangle p1Rect = new Rectangle(player1, playerSize); // player 1 rect
        Rectangle p2Rect = new Rectangle(player2, playerSize); // player 2 rect

        if (Raylib.CheckCollisionCircleRec(ballPosition, ballRadius, p1Rect))
        {
            ballDirection.X = MathF.Abs(ballDirection.X);          // bounce right
            ballPosition.X = player1.X + playerSize.X + ballRadius; // push out of paddle
        }

        if (Raylib.CheckCollisionCircleRec(ballPosition, ballRadius, p2Rect))
        {
            ballDirection.X = -MathF.Abs(ballDirection.X);           // bounce left
            ballPosition.X = player2.X - ballRadius;                // push out of paddle
        }
    }

    void Draw()
    {
        int ScreenWidth = Raylib.GetScreenWidth();
        int ScreenHeight = Raylib.GetScreenHeight();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(30, 30, 50, 255)); // dark background

        //   Draw center line  
        Raylib.DrawLine(ScreenWidth / 2, 0, ScreenWidth / 2, ScreenHeight, new Color(255, 255, 255, 60));

        //   Draw paddles  
        Raylib.DrawRectangleV(player1, playerSize, Color.SkyBlue);
        Raylib.DrawRectangleV(player2, playerSize, Color.Gold);

        //   Draw ball  
        Raylib.DrawCircleV(ballPosition, ballRadius, Color.White);

        //   Draw scores  
        string score1 = player1Score.ToString();
        string score2 = player2Score.ToString();
        int fontSize = 48;

        Raylib.DrawText(score1, ScreenWidth / 2 - 80, 20, fontSize, Color.SkyBlue);
        Raylib.DrawText(score2, ScreenWidth / 2 + 40, 20, fontSize, Color.Gold);

        Raylib.EndDrawing();
    }
}