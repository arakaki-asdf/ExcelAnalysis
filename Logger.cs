using System.Text;

static class Logger
{
    static StringBuilder warnings = new StringBuilder();
    static StringBuilder errors = new StringBuilder();

    public static bool IsError => errors.Length > 0;
    public static bool IsWarning => warnings.Length > 0;

    public static void AddError(string text)
    {
        errors.AppendLine(text);
    }

    public static void AddWarning(string text)
    {
        warnings.AppendLine(text);
    }

    public static void CheckWarningAndError()
    {
        if (IsWarning)
        {
            Console.WriteLine(warnings);
            warnings.Clear();
        }

        if (IsError)
        {
            Console.WriteLine(errors);
            Environment.Exit(-1);
        }
    }
}