using Nett;

/// <summary>
/// tomlファイルの[[params]]で定義可能なキーワードの定義
/// name: キー名
/// type: 型 (string, int, float)
/// unique: 値がユニークにするかどうか
/// range:
///     min, max 値の範囲 (int, floatのみ設定可能)
/// </summary>
class TomlParam
{
    /// <summary>
    /// [[params]]で定義可能なキーワード
    /// </summary>
    public static readonly string[] Keywords = new []
    { 
        "name",
        "type",
        "unique",
        "range"
    };
    /// <summary>
    /// typeで定義可能な型
    /// </summary>
    public static readonly string[] Types = new []
    {
        "string",
        "int",
        "float"
    };

    /// <summary>
    /// パラメータ名
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// 型
    /// </summary>
    public string Type { get; private set; }
    /// <summary>
    /// 値がユニークかどうか
    /// </summary>
    public bool IsUnique { get; private set; }
    /// <summary>
    /// 値の範囲 (int)
    /// </summary>
    public (int min, int max) IntRange { get; private set; } = default;
    /// <summary>
    /// 値の範囲 (float)
    /// </summary>
    public (float min, float max) FloatRange { get; private set; } = default;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="toml_path">tomlファイルのパス</param>
    /// <param name="parameters">Nett tomlデータ</param>
    public TomlParam(string toml_path, TomlTable parameters)
    {
        var hash = new HashSet<string>(parameters.Keys);
        Name = hash.Contains("name") ? parameters.Get<string>("name") : "";
        if (Name == "")
        {
            Logger.AddError($"{toml_path} [name] 必須パラメータです");
        }

        Type = hash.Contains("type") ? parameters.Get<string>("type") : "";
        if (Type == "")
        {
            Logger.AddError($"{toml_path} [type] 必須パラメータです");
        }
        else
        {
            var is_type = Types.Any(x => x == Type);
            if (!is_type)
            {
                Logger.AddError($"{toml_path} {Type} 存在しない型です。[{string.Join(", ", Types)}]のいずれかを使用してください");
            }
        }

        IsUnique = hash.Contains("unique") ? parameters.Get<bool>("unique") : false;

        if (hash.Contains("range"))
        {
            var range = parameters["range"].Get<TomlTable>();
            CheckRange(toml_path, range);
        }

        hash.ExceptWith(Keywords);
        foreach (var key in hash)
        {
            Logger.AddError($"{toml_path} {key}: 不要なパラメータです");
        }
    }
    
    /// <summary>
    /// 範囲チェック
    /// </summary>
    /// <param name="path">tomlファイルのパス</param>
    /// <param name="range">tomlデータ</param>
    void CheckRange(string toml_path, TomlTable range)
    {
        if (range == null)
        {
            Logger.AddError($"{toml_path} range: [param.range]の書き方が間違っている可能性があります");
            return;
        }

        var keyExist = new[] { "min", "max" }.All(x => range.Keys.Any(key => x == key));
        if (!keyExist)
        {
            Logger.AddError($"{toml_path} [param.range]: min, maxを定義してください");
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
                    _ => default
                };

                var toml_max = range["max"];
                var max = toml_max.TomlType switch
                {
                    TomlObjectType.Int => toml_max.Get<int>(),
                    TomlObjectType.Float => toml_max.Get<float>(),
                    _ => default
                };

                var t1 = min.ToString();
                var t2 = max.ToString();
                if (max.ToString("G7") == min.ToString("G7"))
                {
                    Logger.AddError($"{toml_path} range: min, maxが同じ値です");
                }
                FloatRange = (min, max);
            }
            break;

            case "int":
            {
                var min = range["min"].Get<int>();
                var max = range["max"].Get<int>();
                if (min == max)
                {
                    Logger.AddError($"{toml_path} range: min, maxが同じ値です");
                }
                IntRange = (min, max);
            }
            break;

            default:
                Logger.AddError($"{toml_path} float, int型以外でrangeは使えません");
                break;
        }
    }
}

/// <summary>
/// tomlファイルで定義可能なキーワードの定義
/// start_param: パラメータ開始位置
/// [[params]]: パラメータデータ配列定義
/// </summary>
class TomlData
{
    /// <summary>
    /// tomlファイルで定義可能なキーワード
    /// </summary>
    public static readonly string[] Keywords = new []
    { 
        "start_param",
        "params",
    };

    /// <summary>
    /// tomlファイル名
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// パラメータ開始位置
    /// </summary>
    public int StartParam { get; private set; }
    /// <summary>
    /// パラメータデータ配列
    /// </summary>
    public TomlParam[] Params { get; private set; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="name">tomlファイル名</param>
    /// <param name="toml">Nett tomlデータ</param>
    public TomlData(string name, TomlTable toml)
    {
        Name = name;
        var hash = new HashSet<string>(toml.Keys);
        StartParam = hash.Contains("start_param")
            ? Math.Max(toml.Get<int>("start_param"), -1) : -1;
        if (StartParam <= 0)
        {
            Logger.AddError($"{Name} start_param 必須パラメータです。もしくは値が間違っています。");
        }

        if (!hash.Contains("params"))
        {
            Logger.AddError($"{Name} [[params]] 必須パラメータです");
            Params = new TomlParam[] {};
        }
        else
        {
            var table_list = toml["params"].Get<List<TomlTable>>();
            var list = new List<TomlParam>();
            foreach (var param in table_list)
            {
                list.Add(new TomlParam(Name, param));
            }
            Params = list.ToArray();
        }

        hash.ExceptWith(Keywords);
        foreach (var key in hash)
        {
            Logger.AddError($"{Name} {key}: 不要なパラメータです");
        }
    }
}