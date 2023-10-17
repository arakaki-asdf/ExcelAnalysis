// エントリーポイント 

var dir = Directory.GetCurrentDirectory();
var data_path = Path.Combine(dir, "data");
var toml_path = Path.Combine(data_path, "item.toml");
var excel_path = Path.Combine(data_path, "sample.xlsx");
var output_path = Path.Combine(dir, "output");

var convert = new ExcelAnalysis();
convert.Run(toml_path, excel_path, output_path);
