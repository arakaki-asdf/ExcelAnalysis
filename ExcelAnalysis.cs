using ExcelDataReader;
using Nett;
using Newtonsoft.Json;
using System.Text;
using System.Linq;

class ExcelAnalysis
{
    // 実行用関数
    public void Run(string toml_path, string excel_path, string output_path)
    {
        var toml = LoadToml(toml_path);
        toml.Validate();
        Logger.CheckWarningAndError();

        var sheet_name = Path.GetFileNameWithoutExtension(toml_path);
        var cells = LoadExcel(excel_path, sheet_name);
        Logger.CheckWarningAndError();
        var excel = new ExcelData(Path.GetFileName(excel_path), cells!, toml.StartRow);

        CheckParameters(toml, excel);
        Logger.CheckWarningAndError();

        CheckType(toml, excel);
        Logger.CheckWarningAndError();

        CheckUnique(toml, excel);
        Logger.CheckWarningAndError();

        CheckRange(toml, excel);
        Logger.CheckWarningAndError();

        var list = ExcelToList(toml, excel);
        var file_name = Path.GetFileNameWithoutExtension(toml_path);
        var json_path = Path.Combine(output_path, $"{file_name}.json");
        OutputJson(json_path, list);
    }

    List<Dictionary<string, object>> ExcelToList(TomlData toml, ExcelData excel)
    {
        var cells = excel.Cells;
        var list = new List<Dictionary<string, object>>();
        for (var i = 0; i < cells.Length; ++i)
        {
            var dic = new Dictionary<string, object>();
            for (var k = 0; k < cells[i].Length; ++k)
            {
                var key = excel.Keys[k];
                var param = toml.Params.FirstOrDefault(x => x.Name == key);
                if (param == null) continue;
                var value = GetValue(param.Type, cells[i][k]);
                dic[key] = value;
            }
            list.Add(dic);
        }

        return list;
    }

    object GetValue(string type, string cell)
    {
        switch (type)
        {
            case "string": return cell;
            case "int":
            {
                if (!int.TryParse(cell, out int result))
                {
                    Logger.AddError($"{cell} {type}型に変換できません");
                    return 0;
                }
                return result;
            }

            case "float":
            {
                if (!float.TryParse(cell, out float result))
                {
                    Logger.AddError($"{cell} {type}型に変換できません");
                    return 0;
                }
                return result;
            }
        }

        Logger.AddError($"Type: {type} {cell} 変換できません");
        return "";
    }

    // List<Dictionary<string, object>>からJson変換
    void OutputJson(string path, List<Dictionary<string, object>> dic, bool is_fomrat = true)
    {
        var format = is_fomrat ? Formatting.Indented : Formatting.None;
        var json_string = JsonConvert.SerializeObject(dic, format);
        File.WriteAllText(path, json_string);
    }

    // Tomlで定義されているパラメータがExcel内にあるか確認
    void CheckParameters(TomlData toml, ExcelData excel)
    {
        var toml_names = new HashSet<string>(toml.Params.Select(x => x.Name).ToArray());
        var excel_names = new HashSet<string>(excel.Keys);
        toml_names.SymmetricExceptWith(excel_names);

        if (toml_names.Count > 0)
        {
            var names = string.Join(", ", toml_names.Select(x => x));
            Logger.AddError($"tomlとexcelでパラメータが一致しません。names : {names}");
        }
    }

    // 型チェック
    void CheckType(TomlData toml, ExcelData excel)
    {
        foreach (var param in toml.Params)
        {
            var columns = excel[param.Name];
            for (var i = 0; i < columns.Length; ++i)
            {
                switch (param.Type)
                {
                    case "string":
                        break;

                    case "int":
                        if (!int.TryParse(columns[i], out int _))
                        {
                            var column_index = toml.StartRow + 1 + (i + 1);
                            Logger.AddError($"CheckType(): {param.Name}列の{column_index}行目 {param.Type}型に変換できません。");
                        }
                        break;
                    
                    case "float":
                        if (!float.TryParse(columns[i], out float _))
                        {
                            var column_index = toml.StartRow + 1 + (i + 1);
                            Logger.AddError($"CheckType(): {param.Name}列の{column_index}行目 {param.Type}型に変換できません。");
                        }
                        break;
                }
            }
        }
    }

    void ToExcelPosition(TomlData toml, int row, int col)
    {
        // toml.StartRow + 1 
    }

    // ユニーク値チェック
    void CheckUnique(TomlData toml, ExcelData excel)
    {
        var unique_params = toml.Params
            .Where(x => x.IsUnique)
            .ToArray();
        foreach (var param in unique_params)
        {
            var cells = excel[param.Name];
            var hash = new HashSet<string>();
            foreach(var cell in cells)
            {
                if (!hash.Add(cell))
                {
                    Logger.AddError($"CheckValidate() {param.Name}:{cell} ユニークな値が重複しています。");
                }
            }
        }
    }

    // 範囲チェック
    void CheckRange(TomlData toml, ExcelData excel)
    {
        var range_params = toml.Params
            .Where(x => x.IntRange != default || x.FloatRange != default)
            .ToArray();

        foreach (var param in range_params)
        {
            var cells = excel[param.Name];
            foreach (var cell in cells)
            {
                switch (param.Type)
                {
                    case "int":
                    {
                        var min = param.IntRange.min;
                        var max = param.IntRange.max;
                        if (!int.TryParse(cell, out int result))
                        {
                            Logger.AddWarning($"{param.Name} {cell}をint型に変換できないです");
                            continue;
                        }
                        var is_range = min <= result && result <= max;
                        if (!is_range)
                        {
                            Logger.AddWarning($"{param.Name} {cell} 値が範囲外です");
                            continue;
                        }

                        break;
                    }

                    case "float":
                    {
                        var min = param.FloatRange.min;
                        var max = param.FloatRange.max;
                        if (!float.TryParse(cell, out float result))
                        {
                            Logger.AddWarning($"{param.Name} {cell}をfloat型に変換できないです");
                            continue;
                        }
                        var is_range = min <= result && result <= max;
                        if (!is_range)
                        {
                            Logger.AddWarning($"{param.Name} {cell} 値が範囲外です");
                            continue;
                        }

                        break;
                    }
                }
            }
        }
    }

    // excel読み込み
    string[][]? LoadExcel(string path, string sheet_name)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        if (!File.Exists(path))
        {
            Logger.AddError($"{path} : 存在しないファイルです");
            Logger.CheckWarningAndError();
        }
        
        using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (var reader = ExcelReaderFactory.CreateOpenXmlReader(stream))
            {
                var dataset = reader.AsDataSet();

                var worksheet = dataset.Tables[sheet_name];
                if (worksheet is null)
                {
                    var file = Path.GetFileName(path);
                    Logger.AddError($"{file}: {sheet_name} 存在しないシート名です");
                    return null;
                }

                var cells = new string[worksheet!.Rows.Count][];
                foreach (var row in Enumerable.Range(0, worksheet.Rows.Count))
                {
                    cells[row] = Enumerable.Range(0, worksheet.Columns.Count)
                        .Select(col => worksheet.Rows[row][col]?.ToString() ?? "")
                        .ToArray();
                }

                return cells;
            }
        }
    }

    // toml読み込み
    TomlData LoadToml(string path)
    {
        if (!File.Exists(path))
        {
            Logger.AddError($"{path} : 存在しないファイルです");
            Logger.CheckWarningAndError();
        }
        var toml = Toml.ReadFile(path);
        return new TomlData(toml);
    }

}