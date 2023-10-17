using ExcelDataReader;
using Nett;
using Newtonsoft.Json;
using System.Text;

/// <summary>
/// Excel解析
/// tomlファイルでの定義した内容と合っているかチェック
/// json出力
/// </summary>
class ExcelAnalysis
{
    /// <summary>
    /// excel読み込み～json出力まで全て行う
    /// </summary>
    /// <param name="toml_path">tomlファイルパス</param>
    /// <param name="excel_path">excelファイルパス</param>
    /// <param name="output_path">出力ディレクトリ</param>
    public void Run(string toml_path, string excel_path, string output_dir)
    {
        var toml = LoadToml(toml_path);
        Logger.CheckWarningAndError();

        var sheet_name = Path.GetFileNameWithoutExtension(toml_path);
        var cells = LoadExcel(excel_path, sheet_name);
        Logger.CheckWarningAndError();
        if (toml!.StartParam - 1 >= cells!.Length)
        {
            Logger.AddError($"start_row: {toml.StartParam} 開始位置がセルの範囲を超えています。");
            Logger.CheckWarningAndError();
        }

        var excel = new ExcelData(Path.GetFileName(excel_path), cells!, toml.StartParam);
        CheckParameters(toml, excel);
        Logger.CheckWarningAndError();

        CheckType(toml, excel);
        Logger.CheckWarningAndError();

        CheckUnique(toml, excel);
        Logger.CheckWarningAndError();

        CheckRange(toml, excel);
        Logger.CheckWarningAndError();

        // json出力する前にwarningがある場合、終了
        if (Logger.WarningCount > 0)
        {
            Logger.AddError("############ エラー終了 ############");
            Logger.CheckWarningAndError();
        }

        var list = ExcelDataToList(toml, excel);
        var file_name = Path.GetFileNameWithoutExtension(toml_path);
        var json_path = Path.Combine(output_dir, $"{file_name}.json");
        OutputJson(json_path, list);
        Console.WriteLine("############ 成功 ############");
    }

    /// <summary>
    /// エクセルデータからjson用リストに変換
    /// </summary>
    /// <param name="toml"></param>
    /// <param name="excel"></param>
    /// <returns></returns>
    List<Dictionary<string, object>> ExcelDataToList(TomlData toml, ExcelData excel)
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

    /// <summary>
    /// typeに応じて型変換
    /// </summary>
    /// <param name="type">型</param>
    /// <param name="cell">cellの値</param>
    /// <returns></returns>
    object GetValue(string type, string cell)
    {
        switch (type)
        {
            case "string": return cell;
            case "int":
            {
                int.TryParse(cell, out int result);
                return result;
            }

            case "float":
            {
                float.TryParse(cell, out float result);
                return result;
            }
        }

        return "";
    }

    /// <summary>
    /// jsonファイル出力
    /// </summary>
    /// <param name="path">出力パス</param>
    /// <param name="dic">json用リスト</param>
    /// <param name="is_fomrat">フォーマットするかどうか</param>
    void OutputJson(string path, List<Dictionary<string, object>> list, bool is_fomrat = true)
    {
        var format = is_fomrat ? Formatting.Indented : Formatting.None;
        var json_string = JsonConvert.SerializeObject(list, format);
        File.WriteAllText(path, json_string);
    }

    /// <summary>
    /// tomlのparams.nameで定義されているパラメータがexcel内にあるか確認
    /// </summary>
    void CheckParameters(TomlData toml, ExcelData excel)
    {
        var toml_names = new HashSet<string>(toml.Params.Select(x => x.Name));
        var excel_names = new HashSet<string>(excel.Keys);
        toml_names.ExceptWith(excel_names);

        if (toml_names.Count > 0)
        {
            var names = string.Join(", ", toml_names.Select(x => x));
            Logger.AddError($"{excel.Name}@{toml.Name} [{names}] パラメータが一致しません。start_rowが間違っている可能性があります。");
        }
    }

    /// <summary>
    /// 指定した型に変換できるかチェック
    /// </summary>
    void CheckType(TomlData toml, ExcelData excel)
    {
        for (var i = 0; i < toml.Params.Length; ++i)
        {
            var param = toml.Params[i];
            var columns = excel[param.Name];
            for (var k = 0; k < columns.Length; ++k)
            {
                switch (param.Type)
                {
                    case "string":
                        break;

                    case "int":
                        if (!int.TryParse(columns[k], out int _))
                        {
                            Logger.AddWarning(ToDetailText(toml, excel, i, k, $"{param.Type}型に変換できません"));
                        }
                        break;
                    
                    case "float":
                        if (!float.TryParse(columns[k], out float _))
                        {
                            Logger.AddWarning(ToDetailText(toml, excel, i, k, $"{param.Type}型に変換できません"));
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    // 必要な情報「エクセルファイル名@シート名: セル番号 文言」を返す便利関数
    /// </summary>
    /// <param name="toml">tomlデータ</param>
    /// <param name="excel">excelデータ</param>
    /// <param name="row">行</param>
    /// <param name="col">列</param>
    /// <param name="text">文言</param>
    /// <returns></returns>
    string ToDetailText(TomlData toml, ExcelData excel, int row, int col, string text)
    {
        var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var sheet_name = Path.GetFileNameWithoutExtension(toml.Name);
        // パラメータの次なのでStartRow + 1
        return $"{excel.Name}@{sheet_name}: [{alphabet[row]}{toml.StartParam + 1 + col}] {text}";
    }

    /// <summary>
    /// ユニーク値チェック
    /// </summary>
    void CheckUnique(TomlData toml, ExcelData excel)
    {
        for (var i = 0; i < toml.Params.Length; ++i)
        {
            var param = toml.Params[i];
            if (!param.IsUnique) continue;

            var cells = excel[param.Name];
            var hash = new HashSet<string>();
            for (var k = 0; k < cells.Length; ++k)
            {
                var cell = cells[k];
                if (!hash.Add(cell))
                {
                    Logger.AddWarning(ToDetailText(toml, excel, i, k, "ユニークな値が重複しています"));
                }
            }
        }
    }

    /// <summary>
    /// 範囲チェック
    /// </summary>
    void CheckRange(TomlData toml, ExcelData excel)
    {
        for (var i = 0; i < toml.Params.Length; ++i)
        {
            var param = toml.Params[i];
            if (param.IntRange == default && param.FloatRange == default) continue;

            var cells = excel[param.Name];
            for (var k = 0; k < cells.Length; ++k)
            {
                var cell = cells[k];
                switch (param.Type)
                {
                    case "int":
                    {
                        var min = param.IntRange.min;
                        var max = param.IntRange.max;
                        if (!int.TryParse(cell, out int result))
                        {
                            continue;
                        }
                        var is_range = min <= result && result <= max;
                        if (!is_range)
                        {
                            Logger.AddWarning(ToDetailText(toml, excel, i, k, $"{min} <= {result} <= {max} 値が範囲外です"));
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
                            continue;
                        }
                        var is_range = min <= result && result <= max;
                        if (!is_range)
                        {
                            Logger.AddWarning(ToDetailText(toml, excel, i, k, $"{min} <= {result} <= {max} 値が範囲外です"));
                            continue;
                        }

                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// excel読み込み (今のところシート１つのみ)
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <param name="sheet_name">シート名</param>
    /// <returns></returns>
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

    /// <summary>
    /// toml読み込み
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns></returns>
    TomlData? LoadToml(string path)
    {
        if (!File.Exists(path))
        {
            Logger.AddError($"{path} : 存在しないファイルです");
            return null;
        }
        try
        {
            var toml = Toml.ReadFile(path);
            return new TomlData(Path.GetFileName(path), toml);
        }
        catch (Exception e)
        {
            Logger.AddError($"{path}: [toml構文エラー] {e.Message}");
            return null;
        }
    }
}