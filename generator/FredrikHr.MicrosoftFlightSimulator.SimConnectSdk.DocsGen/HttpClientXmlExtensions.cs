using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

public static class HttpClientXmlExtensions
{
    public static async Task<XmlDocument> GetXmlDocumentAsync(
        this HttpClient httpClient,
        Uri? requestUri,
        XmlReaderSettings? xmlSettings,
        CancellationToken cancelToken = default
        )
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        HttpResponseMessage respMsg = await httpClient.GetAsync(
            requestUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancelToken
            ).ConfigureAwait(continueOnCapturedContext: false);
        Task<Stream> respStreamTask = respMsg.Content.ReadAsStreamAsync(cancelToken);
        string? respCharset = respMsg.Content.Headers.ContentType?.CharSet;
        Encoding? respEnc = null;
        if (!string.IsNullOrEmpty(respCharset))
        {
            try
            {
                respEnc = Encoding.GetEncoding(respCharset);
            }
            catch (ArgumentException) { }
        }
        using Stream respStream = await respStreamTask.ConfigureAwait(continueOnCapturedContext: false);
        using StreamReader respStreamReader = respEnc is null
            ? new(respStream)
            : new(respStream, respEnc);
        using XmlReader respXmlReader = XmlReader.Create(respStreamReader, xmlSettings);
        XmlDocument respXmlDoc = new();
        respXmlDoc.Load(respXmlReader);
        return respXmlDoc;
    }

    public static async Task<XPathDocument> GetXPathDocumentAsync(
        this HttpClient httpClient,
        Uri? requestUri,
        XmlReaderSettings? xmlSettings,
        CancellationToken cancelToken = default
        )
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        HttpResponseMessage respMsg = await httpClient.GetAsync(
            requestUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancelToken
            ).ConfigureAwait(continueOnCapturedContext: false);
        Task<Stream> respStreamTask = respMsg.Content.ReadAsStreamAsync(cancelToken);
        string? respCharset = respMsg.Content.Headers.ContentType?.CharSet;
        Encoding? respEnc = null;
        if (!string.IsNullOrEmpty(respCharset))
        {
            try
            {
                respEnc = Encoding.GetEncoding(respCharset);
            }
            catch (ArgumentException) { }
        }
        using Stream respStream = await respStreamTask.ConfigureAwait(continueOnCapturedContext: false);
        using StreamReader respStreamReader = respEnc is null
            ? new(respStream)
            : new(respStream, respEnc);
        using XmlReader respXmlReader = XmlReader.Create(respStreamReader, xmlSettings);
        XPathDocument respXmlDoc = new(respXmlReader);
        return respXmlDoc;
    }
}
