/*
    DRV3-Sharp, a free and open-source toolkit
    for working with files and assets from Danganronpa V3.

    Copyright (C) 2020-2022  James Pelster
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using DRV3_Sharp.Contexts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DRV3_Sharp
{
    internal class Program
    {
        private static readonly Stack<IOperationContext> contextStack = new();  // Stack which holds the contexts, which determines what operations can be performed, and how.
        private static List<IOperation>? cachedOperations = null;   // Cache for operations so that we're not querying them every keyinput but only when we perform an actual refresh of the text.
        private static int highlightedOperation = 0;    // Which entry on the list is currently highlighted/selected?
        private static bool needRefresh = true; // Do we need to redraw the text on the screen?
        private const int HEADER_LINES = 3;   // How much header/footer space do we need to account for to avoid drawing over it?

        static void Main(string[] args)
        {
            // Setup text encoding so we can use Shift-JIS encoding for certain files later on
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Do initial startup.
            PushContext(new RootContext());

            // Start the main program loop.
            MainLoop();
        }

        /// <summary>
        /// While there exists a valid context, loop through its possible operations and show the user what can be performed.
        /// </summary>
        private static void MainLoop()
        {
            while (contextStack.Count > 0)
            {
                var currentContext = contextStack.Peek();

                // If we are refreshing the screen, clear the screen and refresh the header.
                if (needRefresh)
                {
                    Console.Clear();
                    Console.WriteLine($"Current context is {currentContext.GetType()}, context stack depth is {contextStack.Count}.");
                    Console.WriteLine("You can perform the following operations:");
                }

                // Query the context for what operations are currently possible.
                // Only update this if we are asking for a refresh or if it has not yet been populated.
                if (cachedOperations is null || needRefresh)
                    cachedOperations = currentContext.PossibleOperations;

                // Only display (Console.WindowHeight - HEADER_LINES) operations on screen at any given time,
                // otherwise for large lists (like SPC extraction) we'll run out of screen space.
                // The highlight should be centered on the screen as best as possible.
                int operationsListSize = (Console.WindowHeight - HEADER_LINES);
                int halfSize = (operationsListSize / 2);
                int operationsListLowerBound = highlightedOperation - halfSize;
                int operationsListUpperBound = highlightedOperation + halfSize;

                // Compensate for non-centered highlight when at either end of the list
                int lowerDifference = Math.Max(0, 0 - operationsListLowerBound);
                int upperDifference = Math.Max(0, operationsListUpperBound - cachedOperations.Count);
                operationsListLowerBound -= upperDifference;
                operationsListUpperBound += lowerDifference;

                // Final clamp to ensure in-bounds
                operationsListLowerBound = Math.Max(0, operationsListLowerBound);
                operationsListUpperBound = Math.Min(cachedOperations.Count, operationsListUpperBound);
                Range operationsListRange = operationsListLowerBound..operationsListUpperBound;

                // If we are refreshing the screen, draw the list of operations
                if (needRefresh)
                {
                    for (int opNum = operationsListRange.Start.Value; opNum < operationsListRange.End.Value; ++opNum)
                    {
                        if (highlightedOperation == opNum)
                        {
                            // Swap the foreground and background colors to highlight the operation
                            ConsoleColor fgColor = Console.ForegroundColor;
                            ConsoleColor bgColor = Console.BackgroundColor;

                            Console.ForegroundColor = bgColor;
                            Console.BackgroundColor = fgColor;

                            Console.Write(">");
                            Console.WriteLine(cachedOperations[opNum].Name);

                            Console.ForegroundColor = fgColor;
                            Console.BackgroundColor = bgColor;
                        }
                        else
                        {
                            Console.Write(" ");
                            Console.WriteLine(cachedOperations[opNum].Name);
                        }
                    }
                }

                needRefresh = true;

                // Process input regardless of whether we need to refresh the screen or not
                var keyPress = Console.ReadKey(true);
                const int FAST_SCROLL_AMOUNT = 10;
                switch (keyPress.Key)
                {
                    // Single-entry scroll
                    case ConsoleKey.UpArrow:
                        if (highlightedOperation > 0) --highlightedOperation;
                        break;
                    case ConsoleKey.DownArrow:
                        if (highlightedOperation < (cachedOperations.Count - 1)) ++highlightedOperation;
                        break;

                    // Fast scroll
                    case ConsoleKey.PageUp:
                        if (highlightedOperation > FAST_SCROLL_AMOUNT) highlightedOperation -= FAST_SCROLL_AMOUNT;
                        else if (highlightedOperation > 0) highlightedOperation = 0;
                        break;
                    case ConsoleKey.PageDown:
                        if (highlightedOperation < (cachedOperations.Count - FAST_SCROLL_AMOUNT - 1)) highlightedOperation += FAST_SCROLL_AMOUNT;
                        else if (highlightedOperation < (cachedOperations.Count - 1)) highlightedOperation = (cachedOperations.Count - 1);
                        break;

                    // Confirm selection
                    case ConsoleKey.Enter:
                        cachedOperations[highlightedOperation].Perform(currentContext);
                        break;

                    // Any other keys: don't update the screen next pass, we didn't do anything!
                    default:
                        needRefresh = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Pushes a new context onto the top of the context stack, thereby making it the new focus.
        /// Also resets the highlighted operation to the first position.
        /// </summary>
        /// <param name="context"></param>
        public static void PushContext(IOperationContext context)
        {
            contextStack.Push(context);
            highlightedOperation = 0;
        }

        /// <summary>
        /// Pops the top context off the top of the context stack, causing the context underneath to take focus.
        /// Also resets the highlighted operation to the first position.
        /// </summary>
        public static void PopContext()
        {
            contextStack.Pop();
            highlightedOperation = 0;
        }
    }
}
