using System.Xml.Linq;

namespace SearchAlgorithms.HtmlBuilders;

public sealed class HtmlTableBuilder
{
    private const string TableStyle = "border-collapse: collapse; text-align: center; font-family: Arial, sans-serif; width: 100%; margin-bottom: 20px;";
    private const string CellStyle = "border: 1px solid black;"; //padding: 6px 10px; removed
    private const string HeaderStyle = "border: 1px solid black; padding: 8px 10px; background-color: #f2f2f2; font-weight: bold;";

    private readonly int _rowsCount;
    private readonly List<ColumnDefinition> _columns = new List<ColumnDefinition>();

    private sealed class ColumnDefinition
    {
        public string Header { get; }
        public Func<int, string> ValueProvider { get; }

        public ColumnDefinition(string header, Func<int, string> valueProvider)
        {
            Header = header;
            ValueProvider = valueProvider;
        }
    }

    public static HtmlTableBuilder Create(int dataRowsCount)
    {
        return new HtmlTableBuilder(dataRowsCount);
    }

    private HtmlTableBuilder(int dataRowsCount)
    {
        _rowsCount = dataRowsCount;
    }

    public HtmlTableBuilder AddColumn(string header, Func<int, string> valueProvider)
    {
        _columns.Add(new ColumnDefinition(header, valueProvider));
        return this;
    }
    
    public HtmlTableBuilder AddColumn(string header, Func<int, object> valueProvider)
    {
        return AddColumn(header, index =>
        {
            object val = valueProvider(index);
            return val.ToString() ?? " - ";
        });
    }

    public XElement Build()
    {
        XElement headerRow = new XElement("tr");
        for (int c = 0; c < _columns.Count; c++)
        {
            headerRow.Add(new XElement("th", 
                new XAttribute("style", HeaderStyle), 
                _columns[c].Header));
        }

        XElement tbody = new XElement("tbody");
        for (int r = 0; r < _rowsCount; r++)
        {
            XElement tr = new XElement("tr");
            for (int c = 0; c < _columns.Count; c++)
            {
                string text = _columns[c].ValueProvider(r);
                tr.Add(new XElement("td", 
                    new XAttribute("style", CellStyle), 
                    text));
            }
            tbody.Add(tr);
        }

        return new XElement("table",
            new XAttribute("style", TableStyle),
            new XElement("thead", headerRow),
            tbody
        );
    }
}