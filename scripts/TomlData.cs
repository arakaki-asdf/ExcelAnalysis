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

        hash.ExceptWith(Keywords);
        foreach (var key in hash)
        {
            Logger.AddError($"{toml_path} {key}: 不要なパラメータです");
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

        var names_hash = new HashSet<string>();
        var duplicates = Params
            .Select(x => x.Name)
            .Where(x => !names_hash.Add(x))
            .ToArray();
        if (duplicates.Length > 0)
        {
            Logger.AddError($"{Name} {string.Join(", ", duplicates)} パラメータ名が重複してます。");
        }
    }
}