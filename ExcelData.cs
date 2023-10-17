class ExcelData
{
    public string ExcelName { get; private set; }
    public string[][] Cells { get; private set; }
    public string[] Keys { get; private set; }

    public ExcelData(string excel_name, string[][] cells, int start_row)
    {
        ExcelName = excel_name;
        Keys = cells[start_row].ToArray();
        Cells = cells.Skip(start_row + 1).ToArray();
    }

    public string[] this[string key]
    {
        get
        {
            var index = Array.FindIndex(Keys, x => x == key);
            if (index == -1) return new string[]{};
            return Cells.Select(x => x[index]).ToArray();
        }
    }
}