using MimeKit;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace UIUCLibrary.EaPdf
{

    public class MbxParser : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly CryptoStream _cryptoStream;
        private MbxMessageHeader? _currentHeader;

        private MbxParserState _prevMessageState = MbxParserState.Normal;

        public event EventHandler<MimeMessageEndEventArgs>? MimeMessageEnd;

        private bool disposedValue;

        private enum MbxParserState
        {
            Normal,
            Error, //might be able to continue parsing
            Fatal  //cannot continue parsing
        }

        public enum ParseHeaderResult 
        {
            OK,
            EndOfStream,
            InvalidFormat
        }

        public MbxParser(Stream baseStream, HashAlgorithm hashAlgo)
        {
            if (hashAlgo == null)
            {
                throw new ArgumentNullException(nameof(hashAlgo));
            }

            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _cryptoStream = new CryptoStream(baseStream, hashAlgo, CryptoStreamMode.Read, true);

            //skip the first 2048 bytes, which is the mbx file header
            var mbxHeader = new byte[2048];
            _cryptoStream.Read(mbxHeader, 0, mbxHeader.Length);

            _prevMessageState = MbxParserState.Normal;
        }

        public bool IsEndOfStream
        {
            get
            {
                return _baseStream.Position == _baseStream.Length;
            }
        }

        public CryptoStream CryptoStream
        {
            get
            {
                return _cryptoStream;
            }
        }

        public MbxMessageHeader? CurrentHeader
        {
            get
            {
                return _currentHeader;
            }
        }


        private string _overflowText = "";
        public string OverflowText
        {
            get
            {
                return _overflowText;
            }
        }

        public MimeMessage ParseMessage()
        {
            if (_prevMessageState == MbxParserState.Fatal)
            {
                throw new InvalidOperationException("Fatal error occured, cannot continue parsing");
            }

            if (TryParseMbxMessageHeader(_cryptoStream, out MbxMessageHeader msgHeader, out string result, out ParseHeaderResult resultCode))
            {
                _currentHeader = msgHeader;
                if (_prevMessageState == MbxParserState.Normal)
                {
                    _overflowText = "";
                }
                _prevMessageState = MbxParserState.Normal;
            }
            else if (resultCode == ParseHeaderResult.InvalidFormat)
            {
                var msg = result;
                if (_prevMessageState == MbxParserState.Normal)
                {
                    _prevMessageState = MbxParserState.Error;
                    _overflowText = msgHeader.Header;
                    throw new FormatException(msg);
                }
                else
                {
                    //keep trying to parse the header from the next line;
                    _overflowText += msgHeader.Header;
                    return ParseMessage();
                }
            }
            else if (resultCode == ParseHeaderResult.EndOfStream)
            {
                var msg = result;
                _prevMessageState = MbxParserState.Fatal;
                throw new IOException(msg); ;
            }
            else
            {
                _prevMessageState = MbxParserState.Fatal;
                throw new Exception("Code should be unreachable");
            }

            long startPos = _baseStream.Position;
            long endPos = startPos + (long)_currentHeader.Size;

            using var boundStream = new MimeKit.IO.BoundStream(_cryptoStream, startPos, endPos, true);

            var mimeParser = new MimeParser(boundStream, MimeFormat.Entity);

            if (MimeMessageEnd != null)
            {
                mimeParser.MimeMessageEnd += MimeMessageEnd;
            }


            var message = mimeParser.ParseMessage();

            //after parsing the message, the stream position should be at the end of the stream
            if (boundStream.Position != boundStream.Length)
            {
                var remaining = boundStream.Length - boundStream.Position;
            }

            return message;

        }

        /// <summary>
        /// Return MbxMessageHeader values from the Pine mbx message header
        /// The stream is assumed to be positioned at the start of the header
        /// </summary>
        /// <param name="strm">the stream positioned at the start of the header</param>
        /// <param name="msgHeader">the MbxMessageHeader object to populate</param>
        /// <param name="result">the reason the parse failed, or 'OK' if it was successful</param>
        /// <returns>true if the header was parsed successfully, false otherwise</returns>
        private bool TryParseMbxMessageHeader(Stream strm, out MbxMessageHeader msgHeader, out string result, out ParseHeaderResult resultCode)
        {
            bool ret = true;
            result = "OK";
            resultCode = ParseHeaderResult.OK;

            MbxMessageHeader headerOut = new()
            {
                HeaderOffset = _baseStream.Position
            };

            //read the message header from the stream, encoded as ASCII so no fancy parsing is needed
            int i = -1;
            var headerBldr = new StringBuilder();
            while (i != '\n') //lines should end in \r\n
            {
                i = strm.ReadByte();
                if (i == -1) break;
                headerBldr.Append((char)i);
            }

            var msgHeaderStr = headerBldr.ToString();

            if (i == -1 && !string.IsNullOrWhiteSpace(msgHeaderStr))
            {
                result = $"End of stream reached before message header was parsed: '{msgHeaderStr}'";
                resultCode = ParseHeaderResult.EndOfStream;
                headerOut.Header = msgHeaderStr;
                ret = false;
            }

            if (i == -1 && string.IsNullOrWhiteSpace(msgHeaderStr))
            {
                result = "End of stream reached";
                resultCode = ParseHeaderResult.EndOfStream;
                ret = false;
            }
            if (string.IsNullOrWhiteSpace(msgHeaderStr))
            {
                result = "Empty message header";
                resultCode = ParseHeaderResult.InvalidFormat;
                ret = false;
            }

            if (msgHeaderStr[^2..^0] != "\r\n")
            {
                result = "Message header does not end in \\r\\n";
                resultCode = ParseHeaderResult.InvalidFormat;
                headerOut.Header = msgHeaderStr;
                ret = false;
            }
            else
            {
                headerOut.Header = msgHeaderStr;

                msgHeaderStr = msgHeaderStr[0..^2]; //message header without the \r\n

                var msgParts = msgHeaderStr.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (msgParts.Length == 3)
                {
                    try
                    {
                        headerOut.Date = DateTime.Parse(msgParts[0]);
                        headerOut.Size = ulong.Parse(msgParts[1]);
                        headerOut.Keywords = uint.Parse(msgParts[2][0..8], NumberStyles.AllowHexSpecifier);
                        headerOut.Flags = ushort.Parse(msgParts[2][8..12], NumberStyles.AllowHexSpecifier);
                        headerOut.Uid = uint.Parse(msgParts[2][13..21], NumberStyles.AllowHexSpecifier);
                    }
                    catch (Exception ex)
                    {
                        result = ex.Message;
                        resultCode = ParseHeaderResult.InvalidFormat;
                        ret = false;
                    }
                }
                else
                {
                    result = $"Invalid message header: '{msgHeaderStr}'";
                    resultCode = ParseHeaderResult.InvalidFormat;
                    ret = false;
                }
            }

            headerOut.ParseResult = result;
            headerOut.ParseResultCode = resultCode;

            msgHeader = headerOut;
            return ret;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    _cryptoStream?.Dispose();
                }

                // If needed, free unmanaged resources (unmanaged objects) and override finalizer
                // If needed, set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
