﻿using MimeKit;
using System;
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
        const char STATUS_FLAG_READ = 'R';
        const char STATUS_FLAG_OLD = 'O';
        const char STATUS_FLAG_DELETED = 'D';
        const char STATUS_FLAG_FLAGGED = 'F';
        const char STATUS_FLAG_ANSWERED = 'A';
        const char STATUS_FLAG_DRAFT = 'T';

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
        /// <param name="enc"></param>
        /// <returns></returns>
        public static string GetContentEncodingString(ContentEncoding enc)
        {
            return enc switch
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
        /// Try to determine whether the message has be 'Seen'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool TryGetSeen(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //if the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead

            var ret = false;
            status = "";

            MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

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

            var ret = false;
            status = "";

            MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

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

            var ret = false;
            status = "";

            MimeKitHelpers.XMozillaStatusFlags xMozillaStatus = MimeKitHelpers.GetXMozillaStatus(message);
            MimeKitHelpers.XMozillaStatusFlags2 xMozillaStatus2 = MimeKitHelpers.GetXMozillaStatus2(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

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

            var ret = false;
            status = "";


            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];


            //If it is a Mozilla message that came from a file called 'Draft' then assume it is a 'draft' message
            //There is also the X-Mozilla-Draft-Info header which could indicate whether a message is draft
            if (
                (Path.GetFileNameWithoutExtension(mboxFilePath ?? "").Equals("Drafts", StringComparison.OrdinalIgnoreCase) && message.Headers.Contains("X-Mozilla-Status")) //if it is a Mozilla message and the filename is Drafts
                || message.Headers.Contains("X-Mozilla-Draft-Info")  //Mozilla uses this header for draft messages
                || mimeStatus.Contains(STATUS_FLAG_DRAFT)  //Some clients encode draft as "T" in the X-Status header
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
