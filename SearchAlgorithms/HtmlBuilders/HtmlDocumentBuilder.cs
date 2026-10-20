using System.Xml.Linq;

namespace SearchAlgorithms.HtmlBuilders;

public sealed class HtmlDocumentBuilder
{
    private const string ParagraphStyle = "font-family: Arial, sans-serif; font-size: 14px; margin: 6px 0;";
    private const string TitleStyle = "font-family: Arial, sans-serif; margin-bottom: 20px;";

    private string _documentTitle = "Report";
    private readonly List<XElement> _bodyElements = new List<XElement>();

    private HtmlDocumentBuilder()
    {
    }

    public static HtmlDocumentBuilder Create()
    {
        return new HtmlDocumentBuilder();
    }

    public HtmlDocumentBuilder AddTitle(string title)
    {
        _documentTitle = title;
        _bodyElements.Add(new XElement("h1", 
            new XAttribute("style", TitleStyle), 
            title));
        return this;
    }

    public HtmlDocumentBuilder AddTitle(object title)
    {
        return AddTitle(title.ToString() ?? " - ");
    }

    public HtmlDocumentBuilder AddParagraph(string text)
    {
        _bodyElements.Add(new XElement("p", 
            new XAttribute("style", ParagraphStyle), 
            text));
        return this;
    }

    public HtmlDocumentBuilder AddParagraph(object text)
    {
        return AddParagraph(text.ToString() ?? " - ");
    }

    public HtmlDocumentBuilder AddTable(HtmlTableBuilder tableBuilder)
    {
        _bodyElements.Add(tableBuilder.Build());
        return this;
    }

    public HtmlDocumentBuilder AddNode(XElement node)
    {
        _bodyElements.Add(node);
        return this;
    }

    public void Save(string outputPath)
    {
        XElement head = new XElement("head",
            new XElement("meta", new XAttribute("charset", "utf-8")),
            new XElement("title", _documentTitle)
        );

        XElement body = new XElement("body",
            new XAttribute("style", "padding: 20px; background-color: #fafafa;"),
            _bodyElements
        );

        XElement html = new XElement("html", head, body);
        html.Save(outputPath, SaveOptions.OmitDuplicateNamespaces);
    }
}