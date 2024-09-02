class ErrorReporter {
    public static void ReportError(in string msg) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ResetColor();
        Environment.Exit(1);
    }
}