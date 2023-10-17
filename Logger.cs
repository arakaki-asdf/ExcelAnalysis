using System.Text;

/// <summary>
/// ログ出力用クラス
/// </summary>
static class Logger
{
    /// <summary>
    /// ワーニング 複数個あっても大丈夫
    /// </summary>
    static StringBuilder warnings = new StringBuilder();
    /// <summary>
    /// エラー 1つで致命的で解析せず終了させたい場合
    /// </summary>
    static StringBuilder errors = new StringBuilder();

    /// <summary>
    /// ワーニング数
    /// </summary>
    public static int WarningCount { get; private set; }

    /// <summary>
    /// エラーログ追加
    /// </summary>
    /// <param name="text"></param>
    public static void AddError(string text)
    {
        errors.AppendLine(text);
    }

    /// <summary>
    /// ワーニングログ追加
    /// </summary>
    /// <param name="text"></param>
    public static void AddWarning(string text)
    {
        warnings.AppendLine(text);
    }

    /// <summary>
    /// ワーニング&エラー出力
    /// エラーがある場合、即時終了
    /// </summary>
    public static void CheckWarningAndError()
    {
        if (warnings.Length > 0)
        {
            Console.WriteLine(warnings);
            WarningCount += warnings.Length;
            warnings.Clear();
        }

        if (errors.Length > 0)
        {
            Console.WriteLine(errors);
            Environment.Exit(-1);
        }
    }
}