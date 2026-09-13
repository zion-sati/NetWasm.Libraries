using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.System.Xml.Tests;

public sealed class XmlTests
{
    [Test]
    public async Task ReaderAndWriterPreserveElementsAttributesAndText()
    {
        using var reader = XmlReader.Create(new StringReader("<root a=\"7\">value</root>"));
        var first = reader.Read();
        var element = reader.NodeType == XmlNodeType.Element && reader.LocalName == "root";
        var attribute = reader.GetAttribute("a");
        var second = reader.Read();
        var text = reader.NodeType == XmlNodeType.Text ? reader.Value : string.Empty;

        using var output = new StringWriter();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings { OmitXmlDeclaration = true }))
        {
            writer.WriteStartElement("root");
            writer.WriteAttributeString("a", "7");
            writer.WriteString("value");
            writer.WriteEndElement();
            writer.Flush();
        }

        await Assert.That(first && element).IsTrue();
        await Assert.That(attribute).IsEqualTo("7");
        await Assert.That(second && text == "value").IsTrue();
        await Assert.That(output.ToString()).IsEqualTo("<root a=\"7\">value</root>");
    }

    [Test]
    public async Task AsyncReaderUsesConfiguredSuspensionContract()
    {
        var settings = new XmlReaderSettings { Async = true };
        using var reader = XmlReader.Create(new StringReader("<root>async</root>"), settings);
        var first = await reader.ReadAsync();
        var element = reader.NodeType == XmlNodeType.Element && reader.Name == "root";
        var second = await reader.ReadAsync();
        var text = reader.NodeType == XmlNodeType.Text ? reader.Value : string.Empty;

        await Assert.That(first && element).IsTrue();
        await Assert.That(second && text == "async").IsTrue();
    }

    [Test]
    public async Task ResolverAndDtdPolicyDenyAmbientEntitiesAndAllowPreloadedData()
    {
        var settings = new XmlReaderSettings();
        var denied = false;
        try
        {
            using var reader = XmlReader.Create(
                new StringReader("<!DOCTYPE root SYSTEM 'urn:missing'><root/>"), settings);
            while (reader.Read())
            {
            }
        }
        catch (XmlException)
        {
            denied = true;
        }

#if NETWASM
        var resolver = new XmlPreloadedResolver();
        resolver.Add(new Uri("urn:fixture"), "preloaded");
#else
        var resolver = new FixtureResolver();
#endif
        using var entity = (TextReader)resolver.GetEntity(new Uri("urn:fixture"), null, typeof(TextReader))!;

        await Assert.That(settings.DtdProcessing).IsEqualTo(DtdProcessing.Prohibit);
        await Assert.That(denied).IsTrue();
        await Assert.That(entity.ReadToEnd()).IsEqualTo("preloaded");
    }

    [Test]
    public async Task DomMutationCloneAndSelectionRemainInMemory()
    {
        var document = new XmlDocument();
        document.LoadXml("<root id=\"r1\"><item>one</item><item>two</item></root>");
        var selected = (XmlElement)document.SelectSingleNode("/root/item")!;
        selected.SetAttribute("kind", "first");
        document.DocumentElement!.AppendChild(document.CreateElement("item"));
        var clone = (XmlDocument)document.CloneNode(true);

        await Assert.That(document.DocumentElement.GetAttribute("id")).IsEqualTo("r1");
        await Assert.That(selected.InnerText).IsEqualTo("one");
        await Assert.That(document.SelectNodes("/root/item")!.Count).IsEqualTo(3);
        await Assert.That(clone.SelectSingleNode("/root/item")!.Attributes!.GetNamedItem("kind")!.Value)
            .IsEqualTo("first");
    }

    [Test]
    public async Task XPathDocumentNavigatorSupportsAxesAndPredicates()
    {
        using var reader = XmlReader.Create(
            new StringReader("<root><item id=\"1\">one</item><item id=\"2\">two</item></root>"));
        var document = new XPathDocument(reader);
        var iterator = document.CreateNavigator().Select("/root/item[@id='2']");
        var count = 0;
        var value = string.Empty;
        while (iterator.MoveNext())
        {
            count++;
            value = iterator.Current!.Value;
        }

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(value).IsEqualTo("two");
    }

    [Test]
    public async Task LinqXmlTreeAxesAndMutationPreserveOrder()
    {
        var document = XDocument.Parse("<root><item>one</item><item>two</item></root>");
        var root = document.Root!;
        var itemName = XName.Get("item");
        var values = string.Empty;
        foreach (var item in root.Descendants(itemName))
        {
            values += item.Value;
        }

        root.SetAttributeValue(XName.Get("id"), "r1");

        await Assert.That(values).IsEqualTo("onetwo");
        await Assert.That(root.Attribute("id")!.Value).IsEqualTo("r1");
        await Assert.That(document.ToString().Contains("<item>two</item>", StringComparison.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task SchemaSetCompilesAndValidatesDocumentElements()
    {
        var schemas = new XmlSchemaSet();
#if NETWASM
        const string schemaText = "<schema><element name=\"root\"/></schema>";
#else
        const string schemaText = "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:element name='root'/></xs:schema>";
#endif
        using var schemaReader = XmlReader.Create(
            new StringReader(schemaText));
        schemas.Add(string.Empty, schemaReader);
        schemas.Compile();

        var valid = new XmlDocument();
        valid.LoadXml("<root/>");
        var invalid = new XmlDocument();
        invalid.LoadXml("<other/>");

        await Assert.That(schemas.IsCompiled).IsTrue();
        await Assert.That(schemas.Contains(string.Empty)).IsTrue();
        await Assert.That(Validate(valid, schemas)).IsTrue();
        await Assert.That(Validate(invalid, schemas)).IsFalse();
    }

    [Test]
    public async Task InterpretedXsltTransformsReaderInputAndReportsMessages()
    {
#if NETWASM
        var transform = new XslTransform();
        var message = false;
        transform.XsltMessageEncountered += (_, _) => message = true;
        using (var stylesheet = XmlReader.Create(new StringReader("<stylesheet><template/></stylesheet>")))
        {
            transform.Load(stylesheet);
        }

        using var output = new StringWriter();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings { OmitXmlDeclaration = true }))
        using (var input = XmlReader.Create(new StringReader("<root/>")))
        {
            transform.Transform(input, writer);
        }

        await Assert.That(message).IsTrue();
        await Assert.That(output.ToString()).IsEqualTo("<root/>");
#else
        const string stylesheet = "<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'><xsl:template match='/'><root/></xsl:template></xsl:stylesheet>";
        var transform = new XslTransform();
        using var stylesheetReader = XmlReader.Create(new StringReader(stylesheet));
        transform.Load(stylesheetReader);
        using var output = new StringWriter();
        using var writer = XmlWriter.Create(output, new XmlWriterSettings { OmitXmlDeclaration = true });
        using var inputReader = XmlReader.Create(new StringReader("<source/>"));
        var input = new XPathDocument(inputReader);
        transform.Transform(input, null, writer);

        await Assert.That(output.ToString().Contains("<root", StringComparison.Ordinal)).IsTrue();
#endif
    }

    [Test]
    public async Task PrimitiveXmlSerializerRoundTripsWithCustomRootName()
    {
        var serializer = new XmlSerializer(typeof(string), new XmlRootAttribute("value"));
        using var output = new StringWriter();
        serializer.Serialize(output, "portable");
        var value = serializer.Deserialize(new StringReader(output.ToString()));

        await Assert.That(output.ToString().Contains("<value>portable</value>", StringComparison.Ordinal))
            .IsTrue();
        await Assert.That(value is string text && text == "portable").IsTrue();
    }

    [Test]
    public async Task XmlConvertPreservesNames()
    {
        var encoded = XmlConvert.EncodeName("name with space");
        var decoded = XmlConvert.DecodeName(encoded);

        await Assert.That(encoded == "name with space").IsFalse();
        await Assert.That(decoded).IsEqualTo("name with space");
    }

    [Test]
    public async Task XsltScriptIsRejectedBeforeObservableOutput()
    {
        var rejected = false;
        try
        {
#if NETWASM
            using var stylesheet = XmlReader.Create(new StringReader("<stylesheet><script/></stylesheet>"));
            new XslTransform().Load(stylesheet);
#else
            const string stylesheet = "<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform' xmlns:msxsl='urn:schemas-microsoft-com:xslt'><msxsl:script language='C#' implements-prefix='user'>public string f() => \"x\";</msxsl:script></xsl:stylesheet>";
            var transform = new XslTransform();
            using var stylesheetReader = XmlReader.Create(new StringReader(stylesheet));
            transform.Load(stylesheetReader);
#endif
        }
        catch (Exception)
        {
            rejected = true;
        }

        await Assert.That(rejected).IsTrue();
    }

    private static bool Validate(XmlDocument document, XmlSchemaSet schemas)
    {
        try
        {
#if NETWASM
            document.Validate(schemas);
#else
            document.Schemas = schemas;
            document.Validate(null);
#endif
            return true;
        }
        catch (XmlSchemaException)
        {
            return false;
        }
    }

#if !NETWASM
    private sealed class FixtureResolver : XmlResolver
    {
        public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn) =>
            new StringReader("preloaded");
    }
#endif
}
