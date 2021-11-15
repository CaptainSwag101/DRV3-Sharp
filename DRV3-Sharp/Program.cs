/*
    DRV3-Sharp, a free and open-source toolkit
    for working with files and assets from Danganronpa V3.

    Copyright (C) 2020-2021  James Pelster
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
        private static readonly Stack<IOperationContext> contextStack = new();
        private static int highlightedOperation = 0;

        static void Main(string[] args)
        {
            // Setup text encoding so we can use Shift-JIS encoding for certain files later on
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Do initial startup
            contextStack.Push(new RootContext());

            // While there exists a valid context, loop through its possible operations
            // and show the user what can be performed.
            while (contextStack.Count > 0)
            {
                Console.Clear();

                var currentContext = contextStack.Peek();
                Console.WriteLine($"Current context is {currentContext.GetType()}, context stack depth is {contextStack.Count}.");

                Console.WriteLine("You can perform the following operations:");
                var operations = currentContext.PossibleOperations;
                for (int opNum = 0; opNum < operations.Count; ++opNum)
                {
                    if (highlightedOperation == opNum)
                    {
                        // Swap the foreground and background colors to highlight the operation
                        ConsoleColor fgColor = Console.ForegroundColor;
                        ConsoleColor bgColor = Console.BackgroundColor;

                        Console.ForegroundColor = bgColor;
                        Console.BackgroundColor = fgColor;

                        Console.Write(">");
                        Console.WriteLine(operations[opNum].Name);

                        Console.ForegroundColor = fgColor;
                        Console.BackgroundColor = bgColor;
                    }
                    else
                    {
                        Console.Write(" ");
                        Console.WriteLine(operations[opNum].Name);
                    }
                }

                var keyPress = Console.ReadKey(true);
                switch (keyPress.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (highlightedOperation > 0) --highlightedOperation;
                        break;

                    case ConsoleKey.DownArrow:
                        if (highlightedOperation < (operations.Count - 1)) ++highlightedOperation;
                        break;

                    case ConsoleKey.Enter:
                        operations[highlightedOperation].Perform(currentContext);
                        break;
                }
            }
        }

        public static void PushContext(IOperationContext context)
        {
            contextStack.Push(context);
            highlightedOperation = 0;
        }

        public static void PopContext()
        {
            contextStack.Pop();
            highlightedOperation = 0;
        }
    }
}
