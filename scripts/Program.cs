// エントリーポイント 

var dir = Directory.GetCurrentDirectory();
var data_path = Path.Combine(dir, "data");

#if DEBUG
var excel_path = Path.Combine(data_path, "sample.xlsx");
var toml_path = Path.Combine(data_path, "item2.toml");
#else
if (args.Length != 2)
{
    Console.WriteLine("コマンドライン引数の数が違います。excelファイル名, tomlファイル名を渡してください。");
    return;
}
var excel_path = Path.Combine(data_path, args[0]);
var toml_path = Path.Combine(data_path, args[1]);
Console.WriteLine($"args: {string.Join(", ", args)}");
#endif

var output_path = Path.Combine(dir, "output");
var convert = new ExcelAnalysis();
convert.Run(toml_path, excel_path, output_path);
