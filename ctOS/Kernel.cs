using System;
using Sys = Cosmos.System;

namespace ctOS {
    public class Kernel : Sys.Kernel {
        public static readonly string[] Months = {
            "January", "February", "March", "April", "May", "June", "July",
            "August", "September", "October", "November", "December"
        };

        public static readonly string[] Days = {
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday",
            "Sunday"
        };

        protected override void BeforeRun() {
            ACPI.Init();
            ACPI.Enable();

            var width = Sys.Global.Console.Cols;
            var height = Sys.Global.Console.Rows;

            width /= 2;
            height /= 2;

            var textWidth = 24;
            textWidth /= 2;

            var textHeight = 4;
            textHeight /= 2;

            string[] lines = {
                @"   //   __  //___  ____",
                @"  //___/ /_// __ \/ __/",
                @" // __/ __// /_/ /\ \",
                @"//\__/\__//\____/___/"
            };

            for (int i = 0; i < lines.Length; i++) {
                var y = (height - textHeight) + i;
                var x = (width - textWidth);
                Sys.Global.Console.X = x;
                Sys.Global.Console.Y = y;
                Console.Write(lines[i]);
            }

            WaitSeconds(3);


            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
        }

        protected override void Run() {
            Console.Write("X:/>");
            var input = Console.ReadLine();

            if (input == null) return;
            switch (input.ToLower()) {
                case "shutdown":
                    ACPI.Shutdown();
                    break;

                case "help":
                    Console.WriteLine("Known commands include:");
                    Console.WriteLine("\tshutdown");
                    Console.WriteLine("\ttime/date");
                    break;

                case "date":
                case "time":
                    PrintTime();
                    break;

                default:
                    Console.WriteLine("\"" + input +
                                      "\" is not a recognized command. Enter \"help\" for a list of commands.");
                    break;
            }
        }

        public static void WaitSeconds(int seconds) {
            int curr = Cosmos.Hardware.RTC.Second;
            curr += seconds;
            if (curr >= 60)
                curr -= 60;

            // Awkward wait.
            while (Cosmos.Hardware.RTC.Second != curr) {}
        }

        private static void PrintTime() {
            int hour = Cosmos.Hardware.RTC.Second;
            int minute = Cosmos.Hardware.RTC.Minute;
            int second = Cosmos.Hardware.RTC.Second;
            int day = Cosmos.Hardware.RTC.DayOfTheMonth;
            int month = Cosmos.Hardware.RTC.Month;
            int year = Cosmos.Hardware.RTC.Year;
            string weekday = DayOfWeek(year, month, day);

            string amPm = hour >= 12 ? "PM" : "AM";

            if (hour == 0)
                hour = 12;
            else if (hour > 12)
                hour -= 12;

            Console.WriteLine(StringExtensions.FormatString("Current time is {0}:{1}:{2}{3} on {4}, {5} {6}.", hour,
                minute, second, amPm, weekday, Months[month - 1], day.AddOrdinal()));
        }

        public static string DayOfWeek(int year, int month, int day) {
            var jnd = day
                      + ((153 * (month + 12 * ((14 - month) / 12) - 3) + 2) / 5)
                      + (365 * (year + 4800 - ((14 - month) / 12)))
                      + ((year + 4800 - ((14 - month) / 12)) / 4)
                      - ((year + 4800 - ((14 - month) / 12)) / 100)
                      + ((year + 4800 - ((14 - month) / 12)) / 400)
                      - 32045;

            return Days[jnd % 7];
        }
    }
}