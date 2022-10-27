using MimeKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class MimeKitHelpers
    {
        //https://github.com/noelmartinon/mboxzilla/blob/master/nsMsgMessageFlags.h
        [Flags]
        public enum XMozillaStatusFlags : ushort
        {
            MSG_FLAG_NULL = 0x0000,
            MSG_FLAG_READ = 0x0001,
            MSG_FLAG_REPLIED = 0x0002,
            MSG_FLAG_MARKED = 0x0004,
            MSG_FLAG_EXPUNGED = 0x0008,
            MSG_FLAG_HAS_RE = 0x0010,
            MSG_FLAG_ELIDED = 0x0020,
            MSG_FLAG_OFFLINE = 0x0080,
            MSG_FLAG_WATCHED = 0x0100,
            MSG_FLAG_SENDER_AUTHED = 0x0200,
            MSG_FLAG_PARTIAL = 0x0400,
            MSG_FLAG_QUEUED = 0x0800,
            MSG_FLAG_FORWARDED = 0x1000,
            MSG_FLAG_PRIORITIES = 0xE000
        }
        [Flags]
        public enum XMozillaStatusFlags2 : uint
        {
            MSG_FLAG_NULL = 0x00000000,
            MSG_FLAG_NEW = 0x00010000,
            MSG_FLAG_IGNORED = 0x00040000,
            MSG_FLAG_IMAP_DELETED = 0x00200000,
            MSG_FLAG_MDN_REPORT_NEEDED = 0x00400000,
            MSG_FLAG_MDN_REPORT_SENT = 0x00800000,
            MSG_FLAG_TEMPLATE = 0x01000000,
            MSG_FLAG_LABELS = 0x0E000000,
            MSG_FLAG_ATTACHMENT = 0x10000000
        }

        //Constants for the Status and X-Status header values, see https://docs.python.org/3/library/mailbox.html#mboxmessage
        //Also https://opensource.apple.com/source/dovecot/dovecot-293/dovecot/doc/wiki/MailboxFormat.mbox.txt.auto.html
        //Status header
        const char STATUS_FLAG_READ = 'R'; //Seen or Read
        const char STATUS_FLAG_OLD = 'O'; //Not recent or old
        //X-Status header
        const char STATUS_FLAG_DELETED = 'D'; //Deleted
        const char STATUS_FLAG_FLAGGED = 'F'; //Flagged
        const char STATUS_FLAG_ANSWERED = 'A';  //Answered
        const char STATUS_FLAG_DRAFT = 'T';  //Draft

        //Constants for Gmail Labels
        const string GMAIL_INBOX = "INBOX";
        const string GMAIL_SPAM = "SPAM";
        const string GMAIL_TRASH = "TRASH";
        const string GMAIL_UNREAD = "UNREAD";
        const string GMAIL_STARRED = "STARRED";
        const string GMAIL_IMPORTANT = "IMPORTANT";
        const string GMAIL_SENT = "SENT";
        const string GMAIL_DRAFT = "DRAFT";

        //Constants for EAXS XML Schema
        const string STATUS_SEEN = "Seen";
        const string STATUS_ANSWERED = "Answered";
        const string STATUS_FLAGGED = "Flagged";
        const string STATUS_DELETED = "Deleted";
        const string STATUS_DRAFT = "Draft";
        const string STATUS_RECENT = "Recent";

        /// <summary>
        /// Convert the MimeKit enum into a standard encoding string value for use in the EAXS XML Schema
        /// </summary>
        /// <param name="enc">The ContentEncoding enum value</param>
        /// <param name="def">The string to return for the ContentEncoding.Default value</param>
        /// <returns></returns>
        public static string GetContentEncodingString(ContentEncoding enc, string def="")
        {
            var ret= enc switch
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
        
        public static string[] ContentEncodings = new string[] { "", "7bit", "8bit", "binary", "base64", "quoted-printable", "uuencode" };

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
                ushort xMozillaBitFlag;
                if (ushort.TryParse(xMozillaStatus, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out xMozillaBitFlag))
                {
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
                uint xMozillaBitFlag2;
                if (uint.TryParse(xMozillaStatus2, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out xMozillaBitFlag2))
                {
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
            IEnumerable<string>? ret  = null;
            
            var gmailLabels = message.Headers["X-Gmail-Labels"];

            if (!string.IsNullOrWhiteSpace(gmailLabels))
            {
                gmailLabels = gmailLabels.ToUpperInvariant();
                ret = gmailLabels.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }

            return ret;
        }

        /// <summary>
        /// Try to determine whether the message has be 'Seen'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetSeen(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //if the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead
            //if the X-Gmail-Labels header is present it will be used instead, https://developers.google.com/gmail/api/guides/labels
            
            var ret = false;
            status = "";

            MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            var gmailLabels = GetGmailLabels(message);

            if (mimeStatus.Contains(STATUS_FLAG_READ)) //Read
            {
                ret = true;
                status = STATUS_SEEN;
            }

            if (xMozillaStatus.HasFlag(MimeKitHelpers.XMozillaStatusFlags.MSG_FLAG_READ))
            {
                ret = true;
                status = STATUS_SEEN;
            }
            
            if(gmailLabels!=null && !gmailLabels.Contains(GMAIL_UNREAD))
            {
                ret = true;
                status = STATUS_SEEN;
            }

            return ret;
        }

        /// <summary>
        /// Try to determine whether the message has be 'Answered'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetAnswered(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //if the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead
            //TODO: Need to determine if this is possible using the Gmail mbox export, maybe the X-Gmail-Labels header

            var ret = false;
            status = "";

            MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            if (mimeStatus.Contains(STATUS_FLAG_ANSWERED)) //Answered
            {
                ret = true;
                status = STATUS_ANSWERED;
            }

            if (xMozillaStatus.HasFlag(MimeKitHelpers.XMozillaStatusFlags.MSG_FLAG_REPLIED))
            {
                ret = true;
                status = STATUS_ANSWERED;
            }

            return ret;
        }

        /// <summary>
        /// Try to determine whether the message has been 'Flagged'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetFlagged(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //if the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead
            //if the X-Gmail-Labels header is present it will be used instead, https://developers.google.com/gmail/api/guides/labels

            var ret = false;
            status = "";

            MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            var gmailLabels = GetGmailLabels(message);

            if (mimeStatus.Contains(STATUS_FLAG_FLAGGED)) //Flagged
            {
                ret = true;
                status = STATUS_FLAGGED;
            }

            if (xMozillaStatus.HasFlag(MimeKitHelpers.XMozillaStatusFlags.MSG_FLAG_MARKED))
            {
                ret = true;
                status = STATUS_FLAGGED;
            }

            if (gmailLabels != null && gmailLabels.Contains(GMAIL_STARRED)) //QUESTION:  Does GMAIL_IMPORTANT also indicate the message was flagged, probably not because Important can be set automatically by Gmail
            {
                ret = true;
                status = STATUS_FLAGGED;
            }

            return ret;
        }

        /// <summary>
        /// Try to determine whether the message has been 'Deleted'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetDeleted(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //if the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead
            //if the X-Gmail-Labels header is present it will be used instead, https://developers.google.com/gmail/api/guides/labels

            var ret = false;
            status = "";

            MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);
            MimeKitHelpers.XMozillaStatusFlags2 xMozillaStatus2 = MimeKitHelpers.GetXMozillaStatus2(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            var gmailLabels = GetGmailLabels(message);

            if (mimeStatus.Contains(STATUS_FLAG_DELETED)) //Deleted
            {
                ret = true;
                status = STATUS_DELETED;
            }

            //For Mozilla: waiting to be expunged by the client or marked as deleted on the IMAP Server
            if (xMozillaStatus.HasFlag(MimeKitHelpers.XMozillaStatusFlags.MSG_FLAG_EXPUNGED) || xMozillaStatus2.HasFlag(MimeKitHelpers.XMozillaStatusFlags2.MSG_FLAG_IMAP_DELETED))
            {
                ret = true;
                status = STATUS_DELETED;
            }

            if (gmailLabels != null && gmailLabels.Contains(GMAIL_TRASH)) //QUESTION:  There are also Gmail labels like "[Imap]/Trash" and "[Imap]/Archive", should these also be used to indicate deleted?
            {
                ret = true;
                status = STATUS_DELETED;
            }

            return ret;
        }

        /// <summary>
        /// Try to determine whether the message is a 'Draft'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetDraft(string mboxFilePath, MimeMessage message, out string status)
        {
            //See https://docs.python.org/3/library/mailbox.html#mailbox.MaildirMessage for more hints -- maybe encoded in the filename of an "info" section?
            //See https://opensource.apple.com/source/dovecot/dovecot-293/dovecot/doc/wiki/MailboxFormat.mbox.txt.auto.html about the X-Status 'T' flag
            //See https://developers.google.com/gmail/api/guides/labels for Gmail Label 'Draft'
            
            var ret = false;
            status = "";


            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            var gmailLabels = GetGmailLabels(message);

            //If it is a Mozilla message that came from a file called 'Draft' then assume it is a 'draft' message
            //There is also the X-Mozilla-Draft-Info header which could indicate whether a message is draft
            //of there is a Gmail Label header with the value Draft
            if (
                (Path.GetFileNameWithoutExtension(mboxFilePath ?? "").Equals("Drafts", StringComparison.OrdinalIgnoreCase) && message.Headers.Contains("X-Mozilla-Status")) //if it is a Mozilla message and the filename is Drafts
                || message.Headers.Contains("X-Mozilla-Draft-Info")  //Mozilla uses this header for draft messages
                || mimeStatus.Contains(STATUS_FLAG_DRAFT)  //Some clients encode draft as "T" in the X-Status header
                || (gmailLabels!=null && gmailLabels.Contains(GMAIL_DRAFT))
                )
            {
                ret = true;
                status = STATUS_DRAFT;
            }

            return ret;
        }

        /// <summary>
        /// Try to determine whether the message is 'Recent'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetRecent(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //If the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead
            //TODO: Can the Gmail Label header be used to determine this status?  Maybe INBOX and UNREAD with no other Labels?  Maybe use dates

            var ret = false;
            status = "";

            MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);
            MimeKitHelpers.XMozillaStatusFlags2 xMozillaStatus2 = MimeKitHelpers.GetXMozillaStatus2(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            //if there is a status header but it does not contain the 'O' (Old) flag then it is recent
            if (message.Headers.Contains(HeaderId.Status) && !mimeStatus.Contains(STATUS_FLAG_OLD)) //Not Old
            {
                ret = true;
                status = STATUS_RECENT;
            }

            //For Mozilla: If there is a "X-Mozilla-Status" header but there are no XMozillaStatusFlags or the XMozillaStatusFlags2 is set to NEW
            if ((message.Headers.Contains("X-Mozilla-Status") && xMozillaStatus == MimeKitHelpers.XMozillaStatusFlags.MSG_FLAG_NULL) || xMozillaStatus2.HasFlag(MimeKitHelpers.XMozillaStatusFlags2.MSG_FLAG_NEW))
            {
                ret = true;
                status = STATUS_RECENT;
            }

            return ret;
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


    }
}
