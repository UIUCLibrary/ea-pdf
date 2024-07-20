using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Encodings;
using MimeKit.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace UIUCLibrary.EaPdf.Helpers
{
    public static class MimeKitHelpers
    {

        public static readonly string[] TextContentEncodings = new string[] { "", "7bit", "8bit" };
        public static readonly string[] BinaryContentEncodings = new string[] { "binary", "base64", "quoted-printable", "uuencode" };
        public static readonly string[] ContentEncodings = new string[] { "", "7bit", "8bit", "binary", "base64", "quoted-printable", "uuencode" };


        //https://github.com/noelmartinon/mboxzilla/blob/master/nsMsgMessageFlags.h
        //https://vincent.bernat.ch/en/x-mozilla-status
        [Flags]
        public enum XMozillaStatusFlags
        {
            MSG_FLAG_NULL = 0x0000,
            MSG_FLAG_READ = 0x0001,
            MSG_FLAG_REPLIED = 0x0002,
            MSG_FLAG_MARKED = 0x0004,
            MSG_FLAG_EXPUNGED = 0x0008,
            MSG_FLAG_HAS_RE = 0x0010,
            MSG_FLAG_ELIDED = 0x0020,
            MSG_FLAG_FEED_MSG = 0x0040,
            MSG_FLAG_OFFLINE = 0x0080,
            MSG_FLAG_WATCHED = 0x0100,
            MSG_FLAG_SENDER_AUTHED = 0x0200,
            MSG_FLAG_PARTIAL = 0x0400,
            MSG_FLAG_QUEUED = 0x0800,
            MSG_FLAG_FORWARDED = 0x1000,
            MSG_FLAG_REDIRECTED = 0x2000,
            //MSG_FLAG_PRIORITIES =  0xE000 //Not sure what this is since the bits overlap with the previous flag, instead turning into the next two flags
            MSG_FLAG_PRIORITIES_1 = 0x4000,
            MSG_FLAG_PRIORITIES_2 = 0x8000
        }
        [Flags]
        public enum XMozillaStatusFlags2
        {
            MSG_FLAG_NULL = 0x00000000,
            MSG_FLAG_NEW = 0x00010000,
            MSG_FLAG_IGNORED = 0x00040000,
            MSG_FLAG_IMAP_DELETED = 0x00200000,
            MSG_FLAG_MDN_REPORT_NEEDED = 0x00400000,
            MSG_FLAG_MDN_REPORT_SENT = 0x00800000,
            MSG_FLAG_TEMPLATE = 0x01000000,
            //MSG_FLAG_LABELS =          0x0E000000, //really three flags, so turning into the next three flags
            MSG_FLAG_LABELS_1 = 0x02000000,
            MSG_FLAG_LABELS_2 = 0x04000000,
            MSG_FLAG_LABELS_3 = 0x08000000,
            MSG_FLAG_ATTACHMENT = 0x10000000
        }

        //Constants for the Status and X-Status header values, see https://docs.python.org/3/library/mailbox.html#mboxmessage
        //Also https://opensource.apple.com/source/dovecot/dovecot-293/dovecot/doc/wiki/MailboxFormat.mbox.txt.auto.html
        //Also http://www.mit.edu/~mm/mm.home/help/top.message-sequence.html ???
        //Also https://datatracker.ietf.org/doc/html/rfc2076
        //Alsofor Pine https://alpineapp.email/alpine/alpine-info/misc/headers.html#Status
        //Status header
        const char STATUS_FLAG_READ = 'R'; //Seen or Read
        const char STATUS_FLAG_OLD = 'O'; //Not recent or old
        //X-Status header
        const char STATUS_FLAG_DELETED = 'D'; //Deleted
        const char STATUS_FLAG_FLAGGED = 'F'; //Flagged
        const char STATUS_FLAG_ANSWERED = 'A';  //Answered
        const char STATUS_FLAG_DRAFT = 'T';  //Draft
        //Others Status Headers???
        //const char STATUS_FLAG_NEW = 'N'; //New ??? http://www.mit.edu/~mm/mm.home/help/top.message-sequence.html
        //const char STATUS_FLAG_UNREAD = 'U'; //Unread ??? https://datatracker.ietf.org/doc/html/rfc2076

        //Constants for Gmail Labels
        //const string GMAIL_INBOX = "INBOX";
        const string GMAIL_SPAM = "SPAM";
        const string GMAIL_TRASH = "TRASH";
        const string GMAIL_UNREAD = "UNREAD";
        const string GMAIL_STARRED = "STARRED";
        //const string GMAIL_IMPORTANT = "IMPORTANT";
        //const string GMAIL_SENT = "SENT";
        const string GMAIL_DRAFT = "DRAFT";
        const string GMAIL_OPENED = "OPENED";
        const string GMAIL_ARCHIVED = "ARCHIVED";

        //Constants for EAXS XML Schema
        const string STATUS_SEEN = "Seen";
        const string STATUS_ANSWERED = "Answered";
        const string STATUS_FLAGGED = "Flagged";
        const string STATUS_DELETED = "Deleted";
        const string STATUS_DRAFT = "Draft";
        const string STATUS_RECENT = "Recent";

        //email systems or clients
        const string MAILER_MOZILLA = "Mozilla"; //thunderbird and related
        const string MAILER_GMAIL = "GMail";
        const string MAILER_PINE = "Pine";
        const string MAILER_UNKNOWN = "Unknown";

        /// <summary>
        /// "*mbx*" - Pine email format
        /// </summary>
        public static readonly byte[] PineMbxMagicHeader = { 0x2a, 0x6d, 0x62, 0x78, 0x2a }; // "*mbx*"

        /// <summary>
        /// Define constants used as bit flags for the input type
        /// </summary>
        public const uint INPUT_TYPE_UNTESTED = 0x0; //must be zero
        public const uint INPUT_TYPE_UNKNOWN = 0x1;
        public const uint INPUT_TYPE_MBOX = 0x2;
        public const uint INPUT_TYPE_EML = 0x4;
        //single bits up to 0x40000000 can be used for other input types
        public const uint INPUT_TYPE_FOLDER = 0x80000000; //last bit in uint indicates a folder
        public const uint INPUT_TYPE_FILE_MASK = 0x7FFFFFFF; //all bits except the last, mask out just the file type bits

        /// <summary>
        /// Return true if the stream contains a Pine *mbx* file
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static bool IsStreamAnMbx(Stream stream)
        {
            bool ret = false;

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (stream.CanSeek == false)
            {
                throw new ArgumentException("Stream must be seekable", nameof(stream));
            }

            //see if the stream contains an *mbx* header at the beginning
            var origPos = stream.Position;
            stream.Position = 0;
            byte[] magic = new byte[PineMbxMagicHeader.Length];
            stream.Read(magic, 0, PineMbxMagicHeader.Length);
            if (magic.SequenceEqual(PineMbxMagicHeader))
            {
                ret = true;
            }

            //reset the stream to its origional position and return
            stream.Position = origPos;
            return ret;
        }

        /// <summary>
        /// Based on message headers try to determine which email system or email client generated the MIME message or mbox file
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string GetEmailSystem(MimeMessage message)
        {
            string ret = MAILER_UNKNOWN;

            if (message.Headers.Contains("X-Mozilla-Status"))
            {
                ret = MAILER_MOZILLA;
            }
            else if (message.Headers.Contains("X-Gmail-Labels"))
            {
                ret = MAILER_GMAIL;
            }
            else if (message.Headers.Contains("X-X-Sender")) //not very definitive, probably need to treat Pine the same as Unknown; https://lists.debian.org/debian-user/2004/05/msg01409.html
            {
                ret = MAILER_PINE;
            }

            return ret;
        }


        /// <summary>
        /// Convert the MimeKit enum into a standard encoding string value for use in the EAXS XML Schema
        /// </summary>
        /// <param name="enc">The ContentEncoding enum value</param>
        /// <param name="def">The string to return for the ContentEncoding.Default value</param>
        /// <returns></returns>
        public static string GetContentEncodingString(ContentEncoding enc, string def = "")
        {
            var ret = enc switch
            {
                ContentEncoding.Default => "",
                ContentEncoding.SevenBit => "7bit",
                ContentEncoding.EightBit => "8bit",
                ContentEncoding.Binary => "binary",
                ContentEncoding.Base64 => "base64",
                ContentEncoding.QuotedPrintable => "quoted-printable",
                ContentEncoding.UUEncode => "uuencode",
                _ => "",
            };

            if (ret == "" && def != "")
                ret = def;

            return ret;
        }


        /// <summary>
        /// Return the X-Mozilla-Status header value, converted to a enum flag
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static XMozillaStatusFlags GetXMozillaStatus(MimeMessage message)
        {
            XMozillaStatusFlags ret = XMozillaStatusFlags.MSG_FLAG_NULL;

            var xMozillaStatus = message.Headers["X-Mozilla-Status"];
            if (!string.IsNullOrWhiteSpace(xMozillaStatus))
            {
                if (ushort.TryParse(xMozillaStatus, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort xMozillaBitFlag))
                {
                    //test that the value is a valid combination of flags
                    var allFlags = (XMozillaStatusFlags)Enum.GetValues<XMozillaStatusFlags>().Cast<int>().Sum();
                    if (!allFlags.HasFlag((XMozillaStatusFlags)xMozillaBitFlag))
                    {
                        throw new ArgumentException("Invalid XMozillaStatusFlags value");
                    }

                    ret = (XMozillaStatusFlags)xMozillaBitFlag;
                }
            }
            return ret;
        }

        /// <summary>
        /// Return the X-Mozilla-Status-2 header value, converted to a enum flag
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static XMozillaStatusFlags2 GetXMozillaStatus2(MimeMessage message)
        {
            XMozillaStatusFlags2 ret = XMozillaStatusFlags2.MSG_FLAG_NULL;

            var xMozillaStatus2 = message.Headers["X-Mozilla-Status2"];
            if (!string.IsNullOrWhiteSpace(xMozillaStatus2))
            {
                if (uint.TryParse(xMozillaStatus2, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint xMozillaBitFlag2))
                {
                    //test that the value is a valid combination of flags
                    var allFlags = (XMozillaStatusFlags2)Enum.GetValues<XMozillaStatusFlags2>().Cast<int>().Sum();
                    if (!allFlags.HasFlag((XMozillaStatusFlags2)xMozillaBitFlag2))
                    {
                        throw new ArgumentException("Invalid XMozillaStatusFlags2 value");
                    }

                    ret = (XMozillaStatusFlags2)xMozillaBitFlag2;
                }
            }
            return ret;
        }

        /// <summary>
        /// Return a list of Gmail labels from the X-Gmail-Labels header, return null if the header is absent
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IEnumerable<string>? GetGmailLabels(MimeMessage message)
        {
            IEnumerable<string>? ret = null;

            var gmailLabels = message.Headers["X-Gmail-Labels"];

            if (!string.IsNullOrWhiteSpace(gmailLabels))
            {
                gmailLabels = gmailLabels.ToUpperInvariant();
                ret = gmailLabels.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }

            return ret;
        }

        //NEEDSTEST: Develop some specific tests for the different TryGetStatus functions

        /// <summary>
        /// Try to determine whether the message has been 'Seen'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetSeen(MimeMessage message, MimeMessageProperties msgProps, out string status)
        {
            //If the x-mozilla-status and x-mozilla-status2 headers are present they will be used 
            //If the X-Gmail-Labels header is present it will be used instead, https://developers.google.com/gmail/api/guides/labels, https://www.metaspike.com/message-read-status-gmail-google-workspace/
            //Finally, look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage

            const string ret = STATUS_SEEN;
            status = "";

            switch (GetEmailSystem(message))
            {
                case MAILER_MOZILLA:
                    MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);
                    if (xMozillaStatus.HasFlag(MimeKitHelpers.XMozillaStatusFlags.MSG_FLAG_READ))
                    {
                        status = ret;
                    }
                    break;

                case MAILER_GMAIL:
                    var gmailLabels = GetGmailLabels(message);
                    if (gmailLabels != null && gmailLabels.Contains(GMAIL_OPENED)) //Using the 'Opened' label instead of the abscence of the 'Unread' label because a user can mark emails as read or unread w/o opening them
                    {
                        status = ret;
                    }
                    break;

                case MAILER_PINE:
                default:
                    var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];
                    if (msgProps.MbxMessageHeader != null && msgProps.MbxMessageHeader.Seen)
                    {
                        status = ret;
                    }
                    else if (mimeStatus.Contains(STATUS_FLAG_READ)) //Read
                    {
                        status = ret;
                    }
                    break;

            }

            return ret == status;
        }

        /// <summary>
        /// Try to determine whether the message has be 'Answered'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetAnswered(MimeMessage message, MimeMessageProperties msgProps, out string status)
        {
            //If the x-mozilla-status and x-mozilla-status2 headers are present they will be used 
            //otherwise, look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage

            //FUTURE: Need to determine if this is possible using the Gmail mbox export, maybe the X-Gmail-Labels header, https://www.metaspike.com/message-read-status-gmail-google-workspace/
            //        May be able to use the Message-ID and In-Reply-To headers to determine if the message has been answered, but only works if processing the whole corpus of emails, https://cr.yp.to/immhf/thread.html
            //        Easiest if the messages are processed in ascending order of date, then the first message with a matching In-Reply-To header is the answer

            const string ret = STATUS_ANSWERED;
            status = "";

            switch (GetEmailSystem(message))
            {
                case MAILER_MOZILLA:
                    MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);
                    if (xMozillaStatus.HasFlag(MimeKitHelpers.XMozillaStatusFlags.MSG_FLAG_REPLIED))
                    {
                        status = ret;
                    }
                    break;

                case MAILER_GMAIL:
                case MAILER_PINE:
                default:
                    var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];
                    if (msgProps.MbxMessageHeader != null && msgProps.MbxMessageHeader.Answered)
                    {
                        status = ret;
                    }
                    else if (mimeStatus.Contains(STATUS_FLAG_ANSWERED)) //Answered
                    {
                        status = ret;
                    }
                    break;

            }

            return status == ret;
        }

        /// <summary>
        /// Try to determine whether the message has been 'Flagged'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetFlagged(MimeMessage message, MimeMessageProperties msgProps, out string status)
        {
            //If the x-mozilla-status and x-mozilla-status2 headers are present they will be used
            //If the X-Gmail-Labels header is present it will be used instead, https://developers.google.com/gmail/api/guides/labels
            //Finallt, look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage

            const string ret = STATUS_FLAGGED;
            status = "";

            switch (GetEmailSystem(message))
            {
                case MAILER_MOZILLA:
                    MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);
                    if (xMozillaStatus.HasFlag(MimeKitHelpers.XMozillaStatusFlags.MSG_FLAG_MARKED))
                    {
                        status = ret;
                    }
                    break;

                case MAILER_GMAIL:
                    var gmailLabels = GetGmailLabels(message);
                    if (gmailLabels != null && gmailLabels.Contains(GMAIL_STARRED)) //QUESTION:  Does GMAIL_IMPORTANT also indicate the message was flagged, probably not because Important can be set automatically by Gmail
                    {
                        status = ret;
                    }
                    break;

                case MAILER_PINE:
                default:
                    var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];
                    if (msgProps.MbxMessageHeader != null && msgProps.MbxMessageHeader.Flagged)
                    {
                        status = ret;
                    }
                    else if (mimeStatus.Contains(STATUS_FLAG_FLAGGED)) //Flagged
                    {
                        status = ret;
                    }
                    break;

            }

            return ret == status;
        }

        /// <summary>
        /// Try to determine whether the message has been 'Deleted'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetDeleted(MimeMessage message, MimeMessageProperties msgProps, out string status)
        {
            //If the x-mozilla-status and x-mozilla-status2 headers are present they will be used
            //If the X-Gmail-Labels header is present it will be used instead, https://developers.google.com/gmail/api/guides/labels
            //otherwise, look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage

            const string ret = STATUS_DELETED;
            status = "";

            switch (GetEmailSystem(message))
            {
                case MAILER_MOZILLA:
                    MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);
                    MimeKitHelpers.XMozillaStatusFlags2 xMozillaStatus2 = MimeKitHelpers.GetXMozillaStatus2(message);
                    //For Mozilla: waiting to be expunged by the client or marked as deleted on the IMAP Server
                    if (xMozillaStatus.HasFlag(MimeKitHelpers.XMozillaStatusFlags.MSG_FLAG_EXPUNGED) || xMozillaStatus2.HasFlag(MimeKitHelpers.XMozillaStatusFlags2.MSG_FLAG_IMAP_DELETED))
                    {
                        status = ret;
                    }
                    break;

                case MAILER_GMAIL:
                    var gmailLabels = GetGmailLabels(message);
                    if (gmailLabels != null && gmailLabels.Contains(GMAIL_TRASH)) //QUESTION:  There are also Gmail labels like "[Imap]/Trash" and "[Imap]/Archive", should these also be used to indicate deleted?
                    {
                        status = ret;
                    }
                    break;

                case MAILER_PINE:
                default:
                    var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];
                    if (msgProps.MbxMessageHeader != null && (msgProps.MbxMessageHeader.Deleted || msgProps.MbxMessageHeader.Expunged))
                    {
                        status = ret;
                    }
                    else if (mimeStatus.Contains(STATUS_FLAG_DELETED)) //Deleted
                    {
                        status = ret;
                    }
                    break;

            }

            return ret == status;
        }

        /// <summary>
        /// Try to determine whether the message is a 'Draft'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetDraft(MimeMessage message, MimeMessageProperties msgProps, out string status)
        {
            //If the message come from Mozilla, look at some specific X-Mozilla-... headers
            //Or if it is a gmail message look for the Draft label
            //Finally look at the X-Status header

            //See https://docs.python.org/3/library/mailbox.html#mailbox.MaildirMessage for more hints -- maybe encoded in the filename of an "info" section?
            //See https://opensource.apple.com/source/dovecot/dovecot-293/dovecot/doc/wiki/MailboxFormat.mbox.txt.auto.html about the X-Status 'T' flag
            //See https://developers.google.com/gmail/api/guides/labels for Gmail Label 'Draft'

            const string ret = STATUS_DRAFT;
            status = "";

            switch (GetEmailSystem(message))
            {
                case MAILER_MOZILLA:
                    //use the Mozilla headers to determine if the message is a draft
                    if (message.Headers.Contains("X-Mozilla-Draft-Info")) //Mozilla uses this header for draft messages)
                    {
                        status = ret;
                    }
                    break;

                case MAILER_GMAIL:
                    var gmailLabels = GetGmailLabels(message);
                    //use the Gmail labels to determine if the message is a draft
                    if (gmailLabels != null && gmailLabels.Contains(GMAIL_DRAFT))
                    {
                        status = ret;
                    }
                    break;

                case MAILER_PINE:
                default:
                    var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];
                    //Use the X-Status header to determine if the message is a draft
                    if (msgProps.MbxMessageHeader != null && msgProps.MbxMessageHeader.Draft)
                    {
                        status = ret;
                    }
                    else if (mimeStatus.Contains(STATUS_FLAG_DRAFT)) //Some clients encode draft as "T" in the X-Status header)
                    {
                        status = ret;
                    }
                    break;
            }

            return ret == status;
        }

        /// <summary>
        /// Try to determine whether the message is 'Recent'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetRecent(MimeMessage message, MimeMessageProperties msgProps, out string status)
        {
            //If the x-mozilla-status and x-mozilla-status2 headers are present they will be used 
            //or the Gmail Label header be used to determine this status.   https://www.metaspike.com/message-read-status-gmail-google-workspace/
            //      UNREAD and not (OPENED, ARCHIVED, TRASH, or SPAM)
            //Finally, look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage

            const string ret = STATUS_RECENT;
            status = "";

            switch (GetEmailSystem(message))
            {
                case MAILER_MOZILLA:
                    MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);
                    MimeKitHelpers.XMozillaStatusFlags2 xMozillaStatus2 = MimeKitHelpers.GetXMozillaStatus2(message);
                    //For Mozilla: If there is a "X-Mozilla-Status" header but there are no XMozillaStatusFlags or the XMozillaStatusFlags2 is set to NEW
                    if ((message.Headers.Contains("X-Mozilla-Status") && xMozillaStatus == MimeKitHelpers.XMozillaStatusFlags.MSG_FLAG_NULL) || xMozillaStatus2.HasFlag(MimeKitHelpers.XMozillaStatusFlags2.MSG_FLAG_NEW))
                    {
                        status = ret;
                    }
                    break;

                case MAILER_GMAIL:
                    var gmailLabels = GetGmailLabels(message);
                    //For GMAIL: UNREAD and not (OPENED, ARCHIVED, TRASH, or SPAM)
                    if (gmailLabels != null && gmailLabels.Contains(GMAIL_UNREAD) && !gmailLabels.Contains(GMAIL_OPENED) && !gmailLabels.Contains(GMAIL_ARCHIVED) && !gmailLabels.Contains(GMAIL_TRASH) && !gmailLabels.Contains(GMAIL_SPAM))
                    {
                        status = ret;
                    }
                    break;

                case MAILER_PINE:
                default:
                    var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];
                    //if there is a status header but it does not contain the 'O' (Old) flag then it is recent
                    if (msgProps.MbxMessageHeader != null && !msgProps.MbxMessageHeader.Old)
                    {
                        status = ret;
                    }
                    else if (message.Headers.Contains(HeaderId.Status) && !mimeStatus.Contains(STATUS_FLAG_OLD)) //Not Old
                    {
                        status = ret;
                    }
                    break;

            }

            return ret == status;
        }

        /// <summary>
        /// Return true if the attachment is saved external to the mbox message, according to how Mozilla indicates this
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static bool IsXMozillaExternalAttachment(MimePart part)
        {
            return part.Headers.Contains("X-Mozilla-External-Attachment-URL") && (part.Headers["X-Mozilla-Altered"] ?? "").Contains("AttachmentDetached");
        }

        /// <summary>
        /// Get the text content from a MimePart.  If the text contains characters not valid in XML, the output will be encoded as quoted-printable, and a warning will be returned
        /// </summary>
        /// <param name="part"></param>
        /// <returns>A tuple containing the output text, output text encoding, and any warning message</returns>
        /// <exception cref="ArgumentException"></exception>
        public static (string text, string encoding, string warning) GetContentText(MimePart part)
        {
            string contentStr = "";
            string encoding = "";
            string warning = "";

            if (!part.ContentType.IsMimeType("text", "*"))
            {
                throw new ArgumentException("The MimePart is not 'text/*'");
            }

            if (part != null && part.Content != null)
            {
                //Decode the stream and treat it as whatever the charset advertised in the content-type header
                using StreamReader reader = new(part.Content.Open(), part.ContentType.CharsetEncoding, true);
                string xmlStr = reader.ReadToEnd();

                //The content stream may contain characters that are not allowed in XML, i.e. ASCII control characters
                //Check the content, and if this is the case encode it as quoted-printable before saving to XML
                bool validXmlChars = true;
                try
                {
                    xmlStr = XmlConvert.VerifyXmlChars(xmlStr);
                }
                catch (XmlException xex)
                {
                    warning = $"Characters not valid in XML.  Line {xex.LinePosition}: {xex.Message}";
                    validXmlChars = false;
                }

                if (validXmlChars)
                {
                    contentStr = xmlStr;
                    encoding = "";
                }
                else
                {
                    //Use the quoted-printable encoding which should escape the low ascii characters
                    var qpEncoder = new QuotedPrintableEncoder();
                    byte[] xmlStrByts = Encoding.ASCII.GetBytes(xmlStr);
                    int len = qpEncoder.EstimateOutputLength(xmlStrByts.Length);
                    byte[] qpStrByts = new byte[len];

                    int outLen = qpEncoder.Encode(xmlStrByts, 0, xmlStrByts.Length, qpStrByts);

                    var qpStr = Encoding.ASCII.GetString(qpStrByts, 0, outLen);

                    contentStr = qpStr;
                    encoding = "quoted-printable";
                }
            }

            return (contentStr, encoding, warning);
        }

        public static string GetTextAsXhtml(TextPart txtPart, out List<(LogLevel level, string message)> messages)
        {

            messages = new List<(LogLevel level, string message)>();


            string htmlText = txtPart.Text;

            if (string.IsNullOrWhiteSpace(htmlText))
            {
                return htmlText;
            }

            //convert any character entities to their unicode equivalent
            htmlText = HtmlHelpers.FixCharacterEntities(htmlText);

            if (txtPart.IsHtml)
            {
                //clean up the html so it is valid-ish xhtml, log any issues to the messages list
                htmlText = HtmlHelpers.ConvertHtmlToXhtml(htmlText, ref messages, false);
            }
            else if (txtPart.IsFlowed)
            {
                //Use the MimeKit converters to convert plain/text, flowed to html,
                var converter = new FlowedToHtml();
                if (txtPart.ContentType.Parameters.TryGetValue("delsp", out string delsp))
                    converter.DeleteSpace = delsp.Equals("yes", StringComparison.OrdinalIgnoreCase);

                htmlText = converter.Convert(htmlText);
                //clean up the html so it is valid-ish xhtml, ignoring any issues since this was already derived from plain text
                htmlText = HtmlHelpers.ConvertHtmlToXhtml(htmlText, ref messages, true);
            }
            else if (txtPart.IsPlain)
            {
                //Use the MimeKit converters to convert plain/text, fixed to html,
                var converter = new TextToHtml();
                htmlText = converter.Convert(htmlText);
                //clean up the html so it is valid-ish xhtml, ignoring any issues since this was already derived from plain text
                htmlText = HtmlHelpers.ConvertHtmlToXhtml(htmlText, ref messages, true);
            }
            else
            {
                //TODO: Need to make accomodations for text/enriched (and text/richtext? -- not Microsoft RTF), see Pine sent-mail-aug-2007, message id: 4d2cbdd341d0e87da57ba2245562265f@uiuc.edu                 
                messages.Add((LogLevel.Error, $"The '{txtPart.ContentType.MimeType}' content is not plain text or html."));
            }

            return htmlText;
        }

        #region DetermineMessageFileTypes

        /// <summary>
        /// These methods attempt to determine the type of file or folder based on the file or folder path
        /// Message formats are pretty loosely defined, so they are not perfect, and may return false positives; they are intended to be used as a first pass to determine the type of input.
        /// The detailed message parsers will be more accurate, but are also more time consuming.
        /// </summary>

        /// <summary>
        /// RFC 5322 header field name and value "key: value" regex
        /// </summary>
        private readonly static string HEADER_REGEX = @"^([\x21-\x39,\x3b-\x7e]+):([\x09,\x20-\x7e]+)$";

        /// <summary>
        /// Some initial "key: value" headers do not indicate an email, e.g. "begin:vcard" or "begin:vcalendar"
        /// </summary>
        private readonly static List<(string key, string value)> ignoredHeaders = new()
        {
            ("begin", "vcard"),
            ("begin", "vcalendar")
        };

        /// <summary>
        /// Determine whether the file is an mbox file by looking for a "From " line near the beginning of the file immediately followed by an email header line;
        /// The MimeKit parser will skip over lines at the beginning of the file that don't start with "From " and will fail to parse the file correctly if 
        /// the line immediately after the "From " header doesn't start with a valid email header.
        /// Any skipped lines will be in the out variable leader.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="leader">any leading characters that were ignored, may be useful in warning messages</param>
        /// <returns></returns>
        public static bool IsMboxFile(string filePath, out string leader)
        {
            bool ret = false;
            StringBuilder leaderBuilder = new();

            using var fs = File.OpenRead(filePath);
            using var rdr = new StreamReader(fs, Encoding.ASCII);
            int charCnt = 1000; //stop after 1000 characters
            while (!rdr.EndOfStream)
            {
                string? line = rdr.ReadLine();
                if (line != null)
                {
                    if (line.StartsWith("From ", StringComparison.Ordinal))
                    {
                        string? nextLine = rdr.ReadLine();
                        if (IsValidMessageHeader(nextLine))
                        {
                            ret = true;
                            break;
                        }
                        else
                        {
                            leaderBuilder.AppendLine(line);
                            leaderBuilder.AppendLine(nextLine);
                            break;
                        }
                    }
                    else
                    {
                        leaderBuilder.AppendLine(line);
                    }
                }
                charCnt -= line?.Length ?? 0;
                if (charCnt <= 0)
                {
                    break;
                }
            }

            if (ret == true)
            {
                //only return the leader if the file is an mbox file
                leader = leaderBuilder.ToString();
            }
            else
            {
                //if it is not an mbox file, then the leader is not useful
                leader = "";
            }

            rdr.Close();
            rdr.Dispose();
            fs.Dispose();

            return ret; ;
        }


        private static bool IsValidMessageHeader(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            var ms = Regex.Match(line, HEADER_REGEX);
            if (ms.Success)
            {
                var key = ms.Groups[1].Value.Trim();
                var value = ms.Groups[2].Value.Trim();
                //some headers do not indictate an email, e.g. "begin:vcard"
                if (ignoredHeaders.Any(kv => kv.key.Equals(key, StringComparison.OrdinalIgnoreCase) && kv.value.Equals(value, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }



        /// <summary>
        /// Determine whether the file is an eml file by looking for a "field-name: field-value" line at the beginning of the file
        /// The MimeKit parser will fail to parse the file if it doesn't start with a valid email header, the email to xml conversion will 
        /// create an EAXS XML file with no headers, but the entire email, including headers, as the body of the message
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool IsEmlFile(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            using var sr = new StreamReader(fs, Encoding.ASCII);
            string? line = sr.ReadLine();

            bool ret;
            if (string.IsNullOrWhiteSpace(line))
            {
                ret = false;
            }
            else
            {
                ret = IsValidMessageHeader(line);
            }
            sr.Close();
            sr.Dispose();
            fs.Dispose();
            return ret;
        }


        /// <summary>
        /// Determine whether the file is a Pine mbx file by looking for "*mbx*" at the beginning of the file
        /// This is different than an mbox file, which starts with "From "
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool IsPineMbxFile(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            bool ret = IsStreamAnMbx(fs);
            fs.Close();
            fs.Dispose();
            return ret;
        }


        public static InputType DetermineInputType(string inputPath, out string message)
        {
            return DetermineInputType(inputPath, false, out message);
        }

        /// <summary>
        /// Determine the type of input based on the file or folder path
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="includeSubfolders"></param>
        /// <param name="warningMessage">In determining the input type there may be warning messages</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static InputType DetermineInputType(string inputPath, bool includeSubfolders, out string warningMessage)
        {
            warningMessage = "";

            if (string.IsNullOrWhiteSpace(inputPath))
                throw new ArgumentNullException(nameof(inputPath));

            InputType ret = InputType.Untested;

            if (inputPath.TryGetDirectoryInfo(out var absDirPath, out string failureReasonDir) && absDirPath != null && absDirPath.Exists)
            {
                ret |= InputType.Folder;

                //look at files in the directory to determine the type
                var files = absDirPath.GetFiles();
                foreach (var file in files)
                {
                    var type = DetermineInputType(file.FullName, includeSubfolders, out _); //ignore the warning message if inputPath is a folder
                    ret |= type;
                }

                if (includeSubfolders)
                {

                    var dirs = absDirPath.GetDirectories();
                    foreach (var dir in dirs)
                    {
                        var type = DetermineInputType(dir.FullName, includeSubfolders, out _); //ignore the warning message if inputPath is a folder
                        ret |= type;
                    }
                }
            }
            else if (inputPath.TryGetFileInfo(out var absFilePath, out string failureReasonFile) && absFilePath != null && absFilePath.Exists)
            {
                //look at the file to determine the type
                if (IsMboxFile(inputPath, out string leadIn))
                {
                    ret = InputType.MboxFile;
                    if (!string.IsNullOrWhiteSpace(leadIn))
                    {
                        warningMessage = $"The file '{inputPath}' was determined to be an mbox file, but the leading characters were ignored:\r\n{leadIn}\r\n";
                    }
                }
                else if (IsPineMbxFile(inputPath))
                {
                    ret = InputType.MboxFile;
                }
                else if (IsEmlFile(inputPath))
                {
                    ret = InputType.EmlFile;
                }
                else
                {
                    ret = InputType.UnknownFile;
                }
            }
            else
            {
                throw new ArgumentException($"'{inputPath}' is not a valid file or folder path: {(string.IsNullOrWhiteSpace(failureReasonDir + failureReasonFile) ? "File or folder not found" : failureReasonDir + " " + failureReasonFile)}");
            }

            return ret;
        }

        public static bool DoesMimeFormatMatchInputFileType(MimeFormat mimeFormat, InputType inputFileType)
        {
            return (mimeFormat == MimeFormat.Entity && inputFileType.HasFlag(Helpers.InputType.EmlFile)) ||
                (mimeFormat == MimeFormat.Mbox && inputFileType.HasFlag(Helpers.InputType.MboxFile));
        }

        /// <summary>
        /// Return a more human-readable name for the MessageFormat
        /// </summary>
        public static string GetFormatName(MimeFormat format)
        {
            return (format == MimeFormat.Entity ? "EML" : "MBOX");
        }

        public static bool IsFolder(this InputType type)
        {
            return type.HasFlag(InputType.Folder);
        }

        /// <summary>
        /// Determine if the type represents a folder containing mixed email message files, 
        /// unknown type and one other type does not count as mixed
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsMixedFolder(this InputType type)
        {
            if (!type.IsFolder())
                return false;

            if (type.HasFlag(InputType.MboxFile) && type.HasFlag(InputType.EmlFile))
                return true;

            return false;
        }

        public static bool IsUnknownFolder(this InputType type)
        {
            if (!type.IsFolder())
                return false;

            if(!type.HasFlag(InputType.UnknownFile))
                return false;

            if (type.HasFlag(InputType.MboxFile) || type.HasFlag(InputType.EmlFile))
                return false;

            return true;
        }

        public static bool IsMboxFolder(this InputType type)
        {
            if (!type.IsFolder())
                return false;

            if (type.HasFlag(InputType.MboxFile) && !type.HasFlag(InputType.EmlFile))
                return true;

            return false;
        }

        public static bool IsEmlFolder(this InputType type)
        {
            if (!type.IsFolder())
                return false;

            if(type.HasFlag(InputType.EmlFile) && !type.HasFlag(InputType.MboxFile))
                return true;

            return false;
        }


        #endregion DetermineMessageFileTypes
    }

    /// <summary>
    /// Enum flag for the type of input file or type folder of files
    /// </summary>
    [Flags]
    public enum InputType : uint
    {
        Untested = MimeKitHelpers.INPUT_TYPE_UNTESTED, //0

        UnknownFile = MimeKitHelpers.INPUT_TYPE_UNKNOWN,
        MboxFile = MimeKitHelpers.INPUT_TYPE_MBOX, //Also Pine mbx files
        EmlFile = MimeKitHelpers.INPUT_TYPE_EML,
        //add other message file types as needed, add corresponding extension methods as needed

        Folder = MimeKitHelpers.INPUT_TYPE_FOLDER //indicates the input is a folder containing files, use bitmasks to determine the type of email message files in the folder
    }


}
