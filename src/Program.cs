using System.Runtime.InteropServices;

class Program {
    static void Main(string[] args) {
        if (args.Length != 2) {
            Console.WriteLine("Usage: rvmas [INPUT FILE] [OUTPUT FILE]");
            ErrorReporter.ReportError("Invalid arguments.");
        }
        if (!File.Exists(args[0])) {
            ErrorReporter.ReportError($"Could not open file '{args[0]}'.");
        }

        Parser.CompileFromTo(args[0], args[1]);
    }
}