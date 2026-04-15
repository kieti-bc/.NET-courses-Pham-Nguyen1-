using Raylib_cs;
using System.Numerics;

namespace dvd
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Window setup
            const int screenW = 800;
            const int screenH = 600;
            Raylib.InitWindow(screenW, screenH, "DVD Screensaver");
            Raylib.SetTargetFPS(60);

            // Text config
            const string text = "DVD";
            const int fontSize = 64;
            const float spacing = 2f;
            Font font = Raylib.GetFontDefault();

            // Measure text size so we can bounce off edges accurately
            Vector2 textSize = Raylib.MeasureTextEx(font, text, fontSize, spacing);

            // Starting position: center of screen
            Vector2 position = new Vector2(
                (screenW - textSize.X) / 2f,
                (screenH - textSize.Y) / 2f
            );

            // Direction: moving diagonally
            Vector2 direction = new Vector2(1, 1);

            // Speed in pixels per second
            float speed = 150f;

            // Color: starts yellow, changes on bounce
            Color[] colors = {
                Color.Yellow, Color.Maroon, Color.Magenta,
                Color.Green, Color.Orange, Color.SkyBlue, Color.Pink
            };
            int colorIndex = 0;
            Color currentColor = colors[colorIndex];

            // Main loop
            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime(); // seconds since last frame

                // Move the text
                position += direction * speed * dt;

                bool bounced = false;

                // Check right edge
                if (position.X + textSize.X >= screenW)
                {
                    position.X = screenW - textSize.X; // push back inside
                    direction.X *= -1;
                    bounced = true;
                }
                // Check left edge
                if (position.X <= 0)
                {
                    position.X = 0;
                    direction.X *= -1;
                    bounced = true;
                }
                // Check bottom edge
                if (position.Y + textSize.Y >= screenH)
                {
                    position.Y = screenH - textSize.Y;
                    direction.Y *= -1;
                    bounced = true;
                }
                // Check top edge
                if (position.Y <= 0)
                {
                    position.Y = 0;
                    direction.Y *= -1;
                    bounced = true;
                }

                // Change color and nudge speed on every bounce
                if (bounced)
                {
                    colorIndex = (colorIndex + 1) % colors.Length;
                    currentColor = colors[colorIndex];
                    speed += 5f; // gets a little faster each bounce
                }

                // Draw
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                // Draw "DVD" at current position with current color
                Raylib.DrawTextEx(font, text, position, fontSize, spacing, currentColor);

                // Small speed indicator in corner
                Raylib.DrawText($"Speed: {(int)speed}", 8, 8, 16, Color.DarkGray);

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}