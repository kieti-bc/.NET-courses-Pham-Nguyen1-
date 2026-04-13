using Raylib_cs;
using System;
using System.Numerics;

namespace LunarLander
{
    internal class Ship
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Fuel;
        public bool ThrustOn;

        private float shipSize = 14f;
        private float thrustPower = 400f;
        private float fuelBurnRate = 50f;

        public Ship(Vector2 startPos)
        {
            Position = startPos;
            Velocity = Vector2.Zero;
            Fuel = 500f;
            ThrustOn = false;
        }

        public void Update(float dt, float gravity)
        {
            // Gravity pulls down
            Velocity.Y += gravity * dt;

            // Engine fires on Space or Up arrow
            ThrustOn = Raylib.IsKeyDown(KeyboardKey.Space) || Raylib.IsKeyDown(KeyboardKey.Up);

            if (ThrustOn && Fuel > 0)
            {
                Velocity.Y -= thrustPower * dt;
                Fuel -= fuelBurnRate * dt;
                if (Fuel < 0) Fuel = 0;
            }

            // Move ship
            Position += Velocity * dt;
        }

        public void Draw()
        {
            // Ship triangle pointing up
            Vector2 left = Position - new Vector2(shipSize, 0);
            Vector2 right = Position + new Vector2(shipSize, 0);
            Vector2 nose = Position - new Vector2(0, shipSize * 2f);
            Raylib.DrawTriangle(left, right, nose, new Color(120, 200, 255, 255));

            // Flame pointing down when engine on
            if (ThrustOn && Fuel > 0)
            {
                Vector2 flameLeft = Position + new Vector2(-shipSize * 0.5f, 4);
                Vector2 flameRight = Position + new Vector2(shipSize * 0.5f, 4);
                Vector2 flameTip = Position + new Vector2(0, shipSize * 2.5f);
                Raylib.DrawTriangle(flameLeft, flameRight, flameTip, new Color(255, 160, 0, 255));
            }

            // Fuel bar above ship
            int barX = (int)Position.X - 20;
            int barY = (int)Position.Y - 50;
            Raylib.DrawRectangle(barX, barY, 40, 6, new Color(60, 60, 60, 255));
            Raylib.DrawRectangle(barX, barY, (int)(40 * (Fuel / 500f)), 6, new Color(80, 255, 120, 255));
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            int screenW = 800;
            int screenH = 600;
            float gravity = 120f;
            float groundY = 540f;
            float safeSpeed = 80f;

            Raylib.InitWindow(screenW, screenH, "Lunar Lander");
            Raylib.SetTargetFPS(60);

            Ship ship = new Ship(new Vector2(screenW / 2f, 80f));
            bool gameOver = false;
            bool landed = false;

            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime();

                // Update
                if (!gameOver)
                {
                    ship.Update(dt, gravity);

                    // Check landing
                    if (ship.Position.Y + 28f >= groundY)
                    {
                        gameOver = true;
                        landed = Math.Abs(ship.Velocity.Y) <= safeSpeed;

                        if (landed) ship.Position.Y = groundY - 28f;
                        ship.Velocity = Vector2.Zero;
                    }
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    ship = new Ship(new Vector2(screenW / 2f, 80f));
                    gameOver = false;
                }

                // Draw
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(5, 5, 20, 255));

                // Landing pad
                Raylib.DrawRectangle(300, (int)groundY, 200, 20, new Color(80, 255, 120, 255));
                Raylib.DrawText("LANDING PAD", 330, (int)groundY + 2, 14, new Color(5, 40, 15, 255));
                Raylib.DrawLine(0, (int)groundY + 20, screenW, (int)groundY + 20, new Color(180, 180, 180, 255));

                ship.Draw();

                // HUD
                Raylib.DrawText($"FUEL:  {(int)ship.Fuel}", 16, 16, 20, new Color(255, 255, 255, 255));
                Raylib.DrawText($"SPEED: {(int)Math.Abs(ship.Velocity.Y)}", 16, 40, 20, new Color(255, 255, 255, 255));
                Raylib.DrawText($"SAFE IF SPEED < {(int)safeSpeed}", 16, 64, 16, new Color(180, 180, 180, 200));
                Raylib.DrawText("SPACE / UP = engine", screenW - 220, 16, 16, new Color(180, 180, 180, 180));

                // Game over message
                if (gameOver)
                {
                    if (landed)
                        Raylib.DrawText("SAFE LANDING!", 220, 240, 60, new Color(80, 255, 120, 255));
                    else
                        Raylib.DrawText("CRASHED!", 240, 240, 60, new Color(255, 70, 70, 255));

                    Raylib.DrawText("PRESS R TO RESTART", 270, 320, 26, new Color(255, 255, 255, 180));
                }

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}