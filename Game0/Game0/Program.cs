using System;

namespace Game0
{
    class Program
    {
        public static void Main(string[] args)
        {
            // initialization
            const int rows = 12, cols = 16;
            if (rows > Console.BufferHeight)
            {
                throw new Exception("Too many rows!");
            }

            if (cols > Console.BufferWidth)
            {
                throw new Exception("Too many columns!");
            }

            const char background = '.';
            const char player = '#';
            const char goal = 'G';

            int playerRow = 0, playerCol = 0;
            var rnd = new Random();
            int goalRow = rnd.Next(0, rows - 1), goalCol = rnd.Next(0, cols - 1);

            var running = true;
            // game loop, a common paradigm in games and other interactive software
            while (running)
            {
                Console.Clear();
                
                // write out the grid background
                for (var row = 0; row < rows; ++row)
                {
                    var rowString = new string(background, cols);
                    Console.WriteLine(rowString);
                }
                // replace the background with the player in one spot
                Console.SetCursorPosition(playerCol, playerRow);
                Console.Write(player);
                Console.SetCursorPosition(goalCol, goalRow);
                Console.Write(goal);
                Console.SetCursorPosition(0, Console.BufferHeight - 1);  // put the cursor back at the end, where one would expect it

                // get input
                var userInput = Console.ReadKey();
                
                // update user position or quit
                switch (userInput.KeyChar)
                {
                    case 'w': --playerRow;
                        break;
                    case 'a': --playerCol;
                        break;
                    case 's': ++playerRow;
                        break;
                    case 'd': ++playerCol;
                        break;
                    case 'q': running = false;
                        break;
                }
                
                // let's be lazy and just force the player back in bounds
                playerRow = Clamp(playerRow, 0, rows - 1);
                playerCol = Clamp(playerCol, 0, cols - 1);
                
                // use while instead of if to make sure goal doesn't regenerate in the same spot
                while (playerRow == goalRow && playerCol == goalCol)
                {
                    goalRow = rnd.Next(0, rows - 1);
                    goalCol = rnd.Next(0, cols - 1);
                }
            }
        }

        // I recently got into Kotlin and so I've written a replacement for coerceIn
        // forces val to be inside the open interval [min, max]
        // this whole uppercase function names thing is kinda weird
        private static int Clamp(int val, int min, int max)
        {
            if (min > max)
            {
                throw new ArgumentException("The minimum of the range is greater than the maximum.");
            }
            return (val < min) ? min : (val > max) ? max : val;
        }
    }
}
