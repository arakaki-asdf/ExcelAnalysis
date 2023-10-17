
## 機能
### json出力 コマンドラインツール
Excelにパラメータ行を追加することで、Excelのシート名と同じtomlファイルで定義した内容に合わせて、型チェック、json出力

例) xlsxデータのパラメータをtomlファイルで定義

![](image/excel.png)

```toml
# パラメータ開始位置
start_param = 2

# id
[[params]]
name = "id"
type = "int"

# アイテム名
[[params]]
name = "item_name"
type = "string"

# 量
[[params]]
name = "quantity"
type = "int"

# 特別報酬倍率
[[params]]
name = "special"
type = "float"
```
- typeは`string`, `int`, `float`の3つのみ


## 使用技術
- C#
- ライブラリ
    - ExcelDataReader (excel読み込み)
    - ExcelDataReader.DataSet
    - Nett (toml読み込み)
    - Newtonsoft.Json (json読み込み)


## 実行方法
※Windowsのみ

1. run.batを実行
2. outputフォルダにitem.jsonが出力されることを確認
3. data/sample.xlsxのitemシートの値を変更してrun.batを実行
