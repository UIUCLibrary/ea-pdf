using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using System;
using System.Xml;
using System.Xml.Linq;

namespace UIUCLibrary.EaPdf.Helpers
{

    /// <summary>
    /// Represents an XHTML markup formatter specifically for badly formed html often found in emails.  Makes sure it returns valid XML 1.0.
    /// * Checks for invalid characters in text and attribute values and replaces them with the unicode replacement character FFFD
    /// * Checks that tag and attributes names contain only valid characters, and escapes them if needed
    /// </summary>
    /// <see cref="https://github.com/AngleSharp/AngleSharp/tree/ebf660279f9f4c74cbade95e38e7d7d93b74dac2/src/AngleSharp/Xhtml"/>
    public class EaPdfXhtmlMarkupFormatter : IMarkupFormatter
    {
        #region Instance

        /// <summary>
        /// An instance of the XhtmlMarkupFormatter.
        /// </summary>
        public static readonly IMarkupFormatter Instance = new EaPdfXhtmlMarkupFormatter();

        #endregion

        #region Private fields

        private readonly Boolean _emptyTagsToSelfClosing;

        private readonly Boolean _omitComments;

        private List<(LogLevel level, string message)> _conversionLog = new();

        private Stack<Dictionary<string, string>> _namespaces = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for <see cref="XhtmlMarkupFormatter"/>
        /// </summary>
        public EaPdfXhtmlMarkupFormatter() : this(true, false)
        {
        }

        /// <summary>
        /// Constructor for <see cref="XhtmlMarkupFormatter"/>
        /// </summary>
        /// <param name="emptyTagsToSelfClosing">
        /// Specify if empty elements like &lt;div&gt;&lt;/div&gt;
        /// should be converted to self-closing ones like &lt;div /&gt;
        /// </param>
        public EaPdfXhtmlMarkupFormatter(Boolean emptyTagsToSelfClosing, Boolean omitComments)
        {
            _emptyTagsToSelfClosing = emptyTagsToSelfClosing;
            _omitComments = omitComments;
        }

        #endregion

        /// <summary>
        /// Gets the status if empty tags will be self-closed or not.
        /// </summary>
        public Boolean IsSelfClosingEmptyTags => _emptyTagsToSelfClosing;

        /// <summary>
        /// Gets whether commits should be omitted during formatting
        /// </summary>
        public Boolean OmitComments => _omitComments;

        /// <summary>
        /// Gets the list of messages generated during the conversion.
        /// </summary>
        public List<(LogLevel level, string message)> ConversionLog => _conversionLog;

        #region Methods

        /// <inheritdoc />
        public virtual String CloseTag(IElement element, Boolean selfClosing)
        {
            var qname = GetQName(element, false);

            if (selfClosing || _emptyTagsToSelfClosing && !element.HasChildNodes)
            {
                return String.Empty;
            }
            else
            {
                PopNamespaces();
                return String.Concat("</", qname, ">");
            }

        }

        /// <inheritdoc />
        public virtual String Comment(IComment comment)
        {
            if (_omitComments)
            {
                return String.Empty;
            }
            else
            {
                return String.Concat("<!--", comment.Data, "-->");
            }
        }

        /// <inheritdoc />
        public virtual String Doctype(IDocumentType doctype)
        {
            var publicId = doctype.PublicIdentifier;
            var systemId = doctype.SystemIdentifier;
            var noExternalId = String.IsNullOrEmpty(publicId) && String.IsNullOrEmpty(systemId);
            var externalId = noExternalId ? String.Empty : " " + (String.IsNullOrEmpty(publicId) ?
                String.Concat("SYSTEM \"", systemId, "\"") :
                String.Concat("PUBLIC \"", publicId, "\" \"", systemId, "\""));
            return String.Concat("<!DOCTYPE ", doctype.Name, externalId, ">");
        }

        /// <inheritdoc />
        public virtual String OpenTag(IElement element, Boolean selfClosing)
        {
            PushNamespaces(element);

            var qname = GetQName(element, true);

            var temp = StringBuilderPool.Obtain();
            temp.Append(Symbols.LessThan);

            temp.Append(qname);

            foreach (var attribute in element.Attributes)
            {
                temp.Append(' ').Append(Attribute(attribute));
            }

            if (selfClosing || _emptyTagsToSelfClosing && !element.HasChildNodes)
            {
                temp.Append(" /");
                PopNamespaces();
            }

            temp.Append(Symbols.GreaterThan);

            return temp.ToPool();
        }

        /// <inheritdoc />
        public virtual String Processing(IProcessingInstruction processing)
        {
            var value = String.Concat(processing.Target, " ", processing.Data);
            return String.Concat("<?", value, "?>");
        }

        /// <inheritdoc />
        public virtual String LiteralText(ICharacterData text)
        {
            var ret = text.Data;

            if (XmlHelpers.TryReplaceInvalidXMLChars(ref ret, out string msg))
            {
                AddLogMessage(LogLevel.Warning, $"Invalid XML character was replaced with '\xFFFD'. {msg}");
            }

            var temp = StringBuilderPool.Obtain();

            //because this is literal text, enclose in CDATA section and also handle text that has CDENDs ']]>' in it
            var parts = ret.Split("]]>");
            bool firstLoop = true;
            foreach (var part in parts)
            {
                if (!firstLoop)
                {
                    temp.Append("]]<![CDATA[>").Append(part).Append("]]>");
                }
                else
                {
                    temp.Append("<![CDATA[").Append(part).Append("]]>");
                }
                firstLoop = false;
            }

            return temp.ToPool();
        }

        /// <inheritdoc />
        public virtual String Text(ICharacterData text)
        {
            var ret = EscapeText(text.Data);

            if (XmlHelpers.TryReplaceInvalidXMLChars(ref ret, out string msg))
            {
                AddLogMessage(LogLevel.Warning, $"Invalid XML character was replaced with '\xFFFD'. {msg}");
            }

            return ret;

        }

        /// <summary>
        /// Creates the string representation of the attribute.
        /// </summary>
        /// <param name="attribute">The attribute to serialize.</param>
        /// <returns>The string representation.</returns>
        protected virtual String Attribute(IAttr attribute)
        {
            // Tracking declared namespaces and prefixes and try to render them correctly.  The general idea is that
            // * For each new element, a list of its declared namespaces is pushed onto a stack and popped off the stack when the element is closed
            // * Any time a new namespaced element or attribute is encountered, we can look in the stack for the match
            // * If found we can treat the namespaced element or attribute as a normal in-scope xhtml namespaced element
            // * If the namespace isn't found then we need treat the tag as a normal non-namespaced tag and escape the localName and treat it as
            //   we are already doing below; i.e. the colon is escaped as _x003A_ to turn it into a normal tag name

            var qname = GetQName(attribute, true);
            var value = attribute.Value;
            var temp = StringBuilderPool.Obtain();

            temp.Append(qname);

            temp.Append(Symbols.Equality).Append(Symbols.DoubleQuote);

            //replace any characters not valid in XML 1.0
            if (XmlHelpers.TryReplaceInvalidXMLChars(ref value, out string msg))
            {
                AddLogMessage(LogLevel.Warning, $"Invalid XML character was replaced with '\xFFFD'. {msg}");
            }

            for (var i = 0; i < value.Length; i++)
            {
                switch (value[i])
                {
                    case Symbols.Ampersand: temp.Append("&amp;"); break;
                    case Symbols.NoBreakSpace: temp.Append("&#160;"); break;
                    case Symbols.LessThan: temp.Append("&lt;"); break;
                    case Symbols.DoubleQuote: temp.Append("&quot;"); break;
                    default: temp.Append(value[i]); break;
                }
            }

            return temp.Append(Symbols.DoubleQuote).ToPool();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Escapes the given text by replacing special characters with their
        /// XHTML entity (amp, nbsp as numeric value, lt, and gt).
        /// </summary>
        /// <param name="content">The string to alter.</param>
        /// <returns>The altered string.</returns>
        public static String EscapeText(String content)
        {
            var temp = StringBuilderPool.Obtain();

            for (var i = 0; i < content.Length; i++)
            {
                switch (content[i])
                {
                    case Symbols.Ampersand: temp.Append("&amp;"); break;
                    case Symbols.NoBreakSpace: temp.Append("&#160;"); break;
                    case Symbols.GreaterThan: temp.Append("&gt;"); break;
                    case Symbols.LessThan: temp.Append("&lt;"); break;
                    default: temp.Append(content[i]); break;
                }
            }

            return temp.ToPool();
        }

        /// <summary>
        /// Gets the local name using the XML namespace prefix if required.
        /// </summary>
        /// <param name="name">The name to be properly represented.</param>
        /// <returns>The string representation.</returns>
        public static String XmlNamespaceLocalName(String name) => !name.Is(NamespaceNames.XmlNsPrefix) ? String.Concat(NamespaceNames.XmlNsPrefix, ":") : name;

        private void PushNamespaces(IElement element)
        {
            _namespaces.Push(new Dictionary<string, string>());
            //Add all xmlns attributes to the dictionary  
            foreach (var attr in element.Attributes)
            {
                if (attr.Name.StartsWith("xmlns"))
                {
                    var prefix = "";
                    var uri = attr.Value;
                    var parts = attr.Name.Split(':', 2);
                    if (parts.Length == 2)
                        prefix = parts[1];
                    _namespaces.Peek().Add(prefix, uri);
                }
            }
        }

        private void PopNamespaces()
        {
            _namespaces.Pop();
        }

        private bool NamespacePrefixIsInContext(string prefix)
        {
            foreach (var ns in _namespaces)
            {
                if (ns.ContainsKey(prefix))
                    return true;
            }
            return false;
        }

        private string GetQName(IElement element, bool useLog)
        {
            var qname = element.LocalName;
            if (!string.IsNullOrEmpty(element.Prefix))
                qname = $"{element.Prefix}:{qname}";

            var orig = qname; //save this so I can tell if it needed to be encoded for logging purposes

            var parts = qname.Split(':', 2);
            if (parts.Length == 2 && NamespacePrefixIsInContext(parts[0]))
                qname = $"{XmlConvert.EncodeLocalName(parts[0])}:{XmlConvert.EncodeLocalName(parts[1])}";
            else
                qname = XmlConvert.EncodeLocalName(qname);

            if (useLog && orig != qname)
            {
                AddLogMessage(LogLevel.Warning, $"Element name '{orig}' contained invalid characters; it was encoded as '{qname}'.");
            }

            return qname;

        }

        private string GetQName(IAttr attr, bool useLog)
        {
            var qname = attr.LocalName;
            if (!string.IsNullOrEmpty(attr.Prefix))
                qname = $"{attr.Prefix}:{qname}";

            var orig = qname; //save this so I can tell if it needed to be encoded for logging purposes

            var parts = qname.Split(':', 2);

            if (parts.Length == 2 && (parts[0].Is(NamespaceNames.XmlNsPrefix) || NamespacePrefixIsInContext(parts[0])))
                qname = $"{XmlConvert.EncodeLocalName(parts[0])}:{XmlConvert.EncodeLocalName(parts[1])}";
            else
                qname = XmlConvert.EncodeLocalName(qname);

            if (useLog && orig != qname)
            {
                AddLogMessage(LogLevel.Warning, $"Attribute name '{orig}' contained invalid characters; it was encoded as '{qname}'.");
            }

            var namespaceUri = attr.NamespaceUri;
            if (namespaceUri.Is(NamespaceNames.XmlUri))
            {
                qname = $"{NamespaceNames.XmlPrefix}:{attr.LocalName}";
            }
            else if (namespaceUri.Is(NamespaceNames.XLinkUri))
            {
                qname = $"{NamespaceNames.XLinkPrefix}:{attr.LocalName}";
            }
            else if (namespaceUri.Is(NamespaceNames.XmlNsUri))
            {
                qname = XmlNamespaceLocalName(attr.LocalName);
            }

            return qname;
        }

        private void AddLogMessage(LogLevel level, string message)
        {
            var log = (level, message);
            if (!_conversionLog.Contains(log))
            {
                _conversionLog.Add(log);
            }
        }

        #endregion
    }
}
