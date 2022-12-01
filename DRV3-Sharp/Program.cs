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

using DRV3_Sharp.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DRV3_Sharp
{
    internal sealed class Program
    {
        private static readonly Stack<IMenu> menuStack = new();  // Stack which holds the menus, which determines what entries can be performed, and how.
        private static MenuEntry[]? cachedEntries = null;   // Cache for entries so that we're not querying them every keyinput but only when we perform an actual refresh of the text.
        private static int highlightedEntry = 0;    // Which entry on the list is currently highlighted/selected?
        private static bool needRefresh = true; // Do we need to redraw the text on the screen?
        private const int HEADER_LINES = 3;   // How much header/footer space do we need to account for to avoid drawing over it?

        static void Main(string[] args)
        {
            // Setup text encoding so we can use Shift-JIS encoding for certain files later on
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Do initial startup.
            PushMenu(new RootMenu());

            // Start the main program loop.
            MainLoop();
        }

        /// <summary>
        /// While there exists a valid menu, loop through its possible entries and show the user what can be performed.
        /// </summary>
        private static void MainLoop()
        {
            while (menuStack.Count > 0)
            {
                var currentMenu = menuStack.Peek();

                // If we are refreshing the screen, clear the screen and refresh the header.
                if (needRefresh)
                {
                    Console.Clear();
                    Console.WriteLine($"Current menu is {currentMenu.GetType()}, menu stack depth is {menuStack.Count}.");
                    Console.WriteLine("You can choose from the following options:");
                }

                // Query the menu for what entries are currently possible.
                // Only update this if we are asking for a refresh or if it has not yet been populated.
                if (cachedEntries is null || needRefresh)
                    cachedEntries = currentMenu.AvailableEntries;

                // Only display (Console.WindowHeight - HEADER_LINES) entries on screen at any given time,
                // otherwise for large lists (like SPC extraction) we'll run out of screen space.
                // The highlight should be centered on the screen as best as possible.
                int entriesListSize = (Console.WindowHeight - HEADER_LINES);
                int halfSize = (entriesListSize / 2);
                int entriesListLowerBound = highlightedEntry - halfSize;
                int entriesListUpperBound = highlightedEntry + halfSize;

                // Compensate for non-centered highlight when at either end of the list
                int lowerDifference = Math.Max(0, 0 - entriesListLowerBound);
                int upperDifference = Math.Max(0, entriesListUpperBound - cachedEntries.Length);
                entriesListLowerBound -= upperDifference;
                entriesListUpperBound += lowerDifference;

                // Final clamp to ensure in-bounds
                entriesListLowerBound = Math.Max(0, entriesListLowerBound);
                entriesListUpperBound = Math.Min(cachedEntries.Length, entriesListUpperBound);
                Range entriesListRange = entriesListLowerBound..entriesListUpperBound;

                // If we are refreshing the screen, draw the list of entries
                if (needRefresh)
                {
                    for (int opNum = entriesListRange.Start.Value; opNum < entriesListRange.End.Value; ++opNum)
                    {
                        if (highlightedEntry == opNum)
                        {
                            // Swap the foreground and background colors to highlight the entry
                            ConsoleColor fgColor = Console.ForegroundColor;
                            ConsoleColor bgColor = Console.BackgroundColor;

                            Console.ForegroundColor = bgColor;
                            Console.BackgroundColor = fgColor;

                            Console.Write(">");
                            Console.WriteLine(cachedEntries[opNum].Name);

                            Console.ForegroundColor = fgColor;
                            Console.BackgroundColor = bgColor;
                        }
                        else
                        {
                            Console.Write(" ");
                            Console.WriteLine(cachedEntries[opNum].Name);
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
                        if (highlightedEntry > 0) --highlightedEntry;
                        break;
                    case ConsoleKey.DownArrow:
                        if (highlightedEntry < (cachedEntries.Length - 1)) ++highlightedEntry;
                        break;

                    // Fast scroll
                    case ConsoleKey.PageUp:
                        if (highlightedEntry > FAST_SCROLL_AMOUNT) highlightedEntry -= FAST_SCROLL_AMOUNT;
                        else if (highlightedEntry > 0) highlightedEntry = 0;
                        break;
                    case ConsoleKey.PageDown:
                        if (highlightedEntry < (cachedEntries.Length - FAST_SCROLL_AMOUNT - 1)) highlightedEntry += FAST_SCROLL_AMOUNT;
                        else if (highlightedEntry < (cachedEntries.Length - 1)) highlightedEntry = (cachedEntries.Length - 1);
                        break;

                    // Confirm selection
                    case ConsoleKey.Enter:
                        cachedEntries[highlightedEntry].Operation.Invoke();
                        break;

                    // Any other keys: don't update the screen next pass, we didn't do anything!
                    default:
                        needRefresh = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Pushes a new menu onto the top of the menu stack, thereby making it the new focus.
        /// Also resets the highlighted entry to the first position.
        /// </summary>
        /// <param name="menu"></param>
        public static void PushMenu(IMenu menu)
        {
            menuStack.Push(menu);
            highlightedEntry = 0;
        }

        /// <summary>
        /// Pops the top menu off the top of the menu stack, causing the menu underneath to take focus.
        /// Also resets the highlighted entry to the first position.
        /// </summary>
        public static void PopMenu()
        {
            menuStack.Pop();
            highlightedEntry = 0;
        }
    }
}
