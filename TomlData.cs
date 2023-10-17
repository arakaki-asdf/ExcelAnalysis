using Nett;

// tomlのパラメータ項目での定義
class TomlParams
{
    public static readonly string[] Keywords = new []
    { 
        "name",
        "type",
        "unique",
        "range"
    };

    public string Name { get; private set; }
    public string Type { get; private set; }
    public bool IsUnique { get; private set; }
    public (int min, int max) IntRange { get; private set; } = default;
    public (float min, float max) FloatRange { get; private set; } = default;

    public TomlParams(TomlTable parameters)
    {
        var hash = new HashSet<string>(parameters.Keys);
        Name = hash.Contains("name") ? parameters.Get<string>("name") : "";
        Type = hash.Contains("type") ? parameters.Get<string>("type") : "";
        IsUnique = hash.Contains("unique") ? parameters.Get<bool>("unique") : false;
        
        if (hash.Contains("range"))
        {
            var range = parameters["range"].Get<TomlTable>();
            CheckRange(range);
        }

        hash.ExceptWith(Keywords);
        foreach (var key in hash)
        {
            Logger.AddError($"{key}: tomlに存在しないキーワードです");
        }
    }

    public bool Validate()
    {
        var result = true;
        if (Name == "")
        {
            Logger.AddError("Name: 値が存在しないです");
            result = false;
        }
        if (Type == "")
        {
            Logger.AddError("Type: 値が存在しないです");
            result = false;
        }

        return result;
    }
    
    void CheckRange(TomlTable range)
    {
        if (range == null)
        {
            Logger.AddError("range: toml定義失敗");
            return;
        }

        var keyExist = new[] { "min", "max" }.All(x => range.Keys.Any(key => x == key));
        if (!keyExist)
        {
            Logger.AddError($"{Name} range: min, maxをキーワードを使用してください。");
            return;
        }

        switch (Type)
        {
            case "float":
            {
                var toml_min = range["min"];
                var min = toml_min.TomlType switch
                {
                    TomlObjectType.Int => toml_min.Get<int>(),
                    TomlObjectType.Float => toml_min.Get<float>(),
                    _ => 0
                };

                var toml_max = range["max"];
                var max = toml_max.TomlType switch
                {
                    TomlObjectType.Int => toml_max.Get<int>(),
                    TomlObjectType.Float => toml_max.Get<float>(),
                    _ => 0
                };

                var t1 = min.ToString();
                var t2 = max.ToString();
                if (max.ToString("G7") == min.ToString("G7"))
                {
                    Logger.AddError("range: min, maxが同じ値です");
                }
                FloatRange = (min, max);
            }
            break;

            case "int":
            {
                var ty = range["min"].GetType();
                var min = range["min"].Get<int>();
                var max = range["max"].Get<int>();
                if (min == max)
                {
                    Logger.AddError("range: min, maxが同じ値です");
                }
                IntRange = (min, max);
            }
            break;

            default:
                Logger.AddError("float, int型以外でrangeは使えません");
                break;
        }
    }
}

// tomlのすべてのデータ
class TomlData
{
    public static readonly string[] Keywords = new []
    { 
        "start_row",
        "params",
    };

    public int StartRow { get; private set; }
    public TomlParams[] Params { get; private set; }

    public TomlData(TomlTable toml)
    {
        var hash = new HashSet<string>(toml.Keys);
        StartRow = hash.Contains("start_row")
            ? Math.Max(toml.Get<int>("start_row") - 1, 0) : -1;
        if (StartRow < 0)
        {
            Logger.AddError("start_rowの値が間違っています");
        }

        if (!hash.Contains("params"))
        {
            Logger.AddError("paramsが無いです");
            Params = new TomlParams[] {};
        }
        else
        {
            var table_list = toml["params"].Get<List<TomlTable>>();
            var list = new List<TomlParams>();
            foreach (var param in table_list)
            {
                list.Add(new TomlParams(param));
            }
            Params = list.ToArray();
        }

        hash.ExceptWith(Keywords);
        foreach (var key in hash)
        {
            Logger.AddError($"{key}: tomlに存在しないキーワードです");
        }
    }

    public bool Validate()
    {
        var result = true;
        if (StartRow <= 0)
        {
            Logger.AddError("start_rowが0以下です。始まる行番号を入力してください");
            result &= false;
        }
        foreach (var param in Params)
        {
            result &= param.Validate();
        }

        return result;
    }
}