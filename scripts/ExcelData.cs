/// <summary>
/// エクセルデータクラス
/// </summary>
class ExcelData
{
    /// <summary>
    /// エクセルファイル名
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// データ
    /// </summary>
    public string[][] Cells { get; private set; }
    /// <summary>
    /// パラメータ
    /// </summary>
    public string[] Params { get; private set; }

    /// <summary>
    /// コンストラクタ
    /// start_rowは例えば行数がexcelで1だとindexは0なので、-1している。
    /// </summary>
    /// <param name="excel_name">エクセルファイル名</param>
    /// <param name="cells">データ</param>
    /// <param name="start_row">パラメータ開始位置(行数)</param>
    public ExcelData(string excel_name, string[][] cells, int start_row)
    {
        Name = excel_name;
        Params = cells[start_row - 1].ToArray();
        Cells = cells.Skip(start_row).ToArray();

        var hash = new HashSet<string>();
        var duplicates = Params
            .Where(x => !hash.Add(x))
            .ToArray();
        if (duplicates.Length > 0)
        {
            Logger.AddError($"{Name} {string.Join(", ", duplicates)} パラメータ名が重複してます。");
        }
    }

    /// <summary>
    /// 指定したパラメータのデータ配列を取得
    /// 例) excel["id"]
    /// </summary>
    /// <param name="key">パラメータ名</param>
    /// <returns></returns>
    public string[] this[string key]
    {
        get
        {
            var index = Array.FindIndex(Params, x => x == key);
            if (index == -1) return new string[]{};
            return Cells.Select(x => x[index]).ToArray();
        }
    }
}