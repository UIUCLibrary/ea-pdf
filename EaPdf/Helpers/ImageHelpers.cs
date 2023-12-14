using SkiaSharp;
using System.Drawing;

namespace UIUCLibrary.EaPdf.Helpers
{
    public static class ImageHelpers
    {
        //add to this array as more image formats are supported
        public static readonly string[] SupportedMimeTypes =  { "image/jpeg", "image/pjpeg", "image/png", "image/gif", "image/tiff" };

        const int TIFF_BIG_ENDIAN_MAGIC_NUMBER = 0x4D4D;
        const int TIFF_LITTLE_ENDIAN_MAGIC_NUMBER = 0x4949;

        const int TIFF_SECOND_MAGIC_NUMBER = 42; //Thank you Douglas Adams
        const int TIFF_TAG_IMAGE_WIDTH = 256;
        const int TIFF_TAG_IMAGE_HEIGHT = 257;

        const int JPEG_MAGIC_NUMBER = 0xD8FF;

        const int GIF_MAGIC_NUMBER = 0x4947;

        const int PNG_MAGIC_NUMBER = 0x5089;

        /// <summary>
        /// Return the width and height of an image
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <remarks>Based on this code: <see href="https://github.com/doxakis/ImageSizeReader/tree/master"/>
        /// Added support for TIFF</remarks>
        public static (int width, int height) GetImageSize(Stream stream, out string msg)
        {
            using (var binaryReader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
            {
                try
                {
                    var dimensions = GetDimensions(binaryReader, out msg);

                    if (dimensions.width == 0 || dimensions.height == 0)
                        msg = $"Invalid image width '{dimensions.width}' or height '{dimensions.height}'";
                    else
                        msg = "";

                    return dimensions;
                }
                catch (EndOfStreamException eosEx)
                {
                    msg = $"Could not determine image dimensions, EndOfStreamException: {eosEx.Message}";
                    return (0, 0);
                }
                catch (Exception ex)
                {
                    msg = $"Could not determine image dimensions, Exception: {ex.Message}";
                    return (0, 0);
                }
            }
        }

        private static (int width, int height) GetDimensions(BinaryReader binaryReader, out string msg)
        {
            ushort magicNumber= binaryReader.ReadUInt16();

            if (magicNumber == JPEG_MAGIC_NUMBER)
            {
                msg = "";
                return DecodeJfif(binaryReader);
            }

            if (magicNumber == PNG_MAGIC_NUMBER)
            {
                var bytes = binaryReader.ReadBytes(6);
                if (bytes[0] == 0x4E && bytes[1] == 0x47 && bytes[2] == 0x0D && bytes[3] == 0x0A && bytes[4] == 0x1A && bytes[5] == 0x0A)
                {
                    msg = "";
                    return DecodePng(binaryReader);
                }
            }

            if (magicNumber == GIF_MAGIC_NUMBER)
            {
                var bytes = binaryReader.ReadBytes(4);
                if (bytes[0] == 0x46 && bytes[1] == 0x38 && (bytes[2] == 0x37 || bytes[2] == 0x39) && bytes[3] == 0x61)
                {
                    msg = "";
                    return DecodeGif(binaryReader);
                }
            }

            if(magicNumber == TIFF_LITTLE_ENDIAN_MAGIC_NUMBER || magicNumber == TIFF_BIG_ENDIAN_MAGIC_NUMBER)
            {
                bool isBigEndian = magicNumber == TIFF_BIG_ENDIAN_MAGIC_NUMBER;

                int magicNumber2 = 0;
                if (isBigEndian)
                {
                    magicNumber2 = ReadBigEndianInt16(binaryReader);
                }
                else
                {
                    magicNumber2 = binaryReader.ReadInt16();
                }

                if(magicNumber2 == TIFF_SECOND_MAGIC_NUMBER)
                {
                    return DecodeTiff(binaryReader, isBigEndian, out msg);
                }
            }

            throw new Exception("Unsupported image format");
        }

        private static (int width, int height) DecodeTiff(BinaryReader binaryReader, bool isBigEndian, out string msg)
        {
            int offset = ReadInt32(binaryReader, isBigEndian);
            
            binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            int entryCount = ReadInt16(binaryReader, isBigEndian);

            //get a dictionary of all the tags for the IFD 
            Dictionary<int, (int type, int count, int value)> ifdEntries = new();
            for(int i=0; i < entryCount; i++)
            {
                int tag = ReadInt16(binaryReader, isBigEndian);
                int type = ReadInt16(binaryReader, isBigEndian);
                int count = ReadInt32(binaryReader, isBigEndian);
                int value = ReadInt32(binaryReader, isBigEndian);
                ifdEntries.Add(tag, (type, count, value));
            }

            int nextIfd = ReadInt32(binaryReader, isBigEndian);

            if(nextIfd != 0)
            {
                msg = $"TIFF file contains multiple images; dimensions are only for the first image";
            }
            else
            {
                msg = "";
            }

            if(ifdEntries.ContainsKey(TIFF_TAG_IMAGE_WIDTH) && ifdEntries.ContainsKey(TIFF_TAG_IMAGE_HEIGHT))
            {
                return (ifdEntries[TIFF_TAG_IMAGE_WIDTH].value, ifdEntries[TIFF_TAG_IMAGE_HEIGHT].value);
            }
            else
            {
                throw new Exception("TIFF file does not contain image width and height tags");
            }
        }

        private static (int width, int height) DecodeGif(BinaryReader binaryReader)
        {
            int width = binaryReader.ReadInt16();
            int height = binaryReader.ReadInt16();
            return (width, height);
        }

        private static (int width, int height) DecodePng(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(8);
            var width = ReadBigEndianInt32(binaryReader);
            var height = ReadBigEndianInt32(binaryReader);
            return (width, height);
        }

        private static (int width, int height) DecodeJfif(BinaryReader binaryReader)
        {
            while (binaryReader.ReadByte() == 0xff)
            {
                var marker = binaryReader.ReadByte();
                var chunkLength = ReadBigEndianInt16(binaryReader);
                if (chunkLength <= 2)
                    throw new Exception("Malformed JFIF image");

                // 0xda = Start Of Scan
                if (marker == 0xda)
                    throw new Exception("Could not determine image dimensions: Start of Scan Marker Found");

                // 0xd9 = End Of Image
                if (marker == 0xd9)
                    throw new Exception("Could not determine image dimensions: End of Image Marker Found");

                // note: 0xc4 and 0xcc are missing. This is expected.
                if (marker == 0xc0 || marker == 0xc1 || marker == 0xc2 || marker == 0xc3
                    || marker == 0xc5 || marker == 0xc6 || marker == 0xc7 || marker == 0xc8
                    || marker == 0xc9 || marker == 0xca || marker == 0xcb || marker == 0xcd
                    || marker == 0xce || marker == 0xcf
                    )
                {
                    var precision = binaryReader.ReadByte();
                    if (precision == 8 || precision == 12 || precision == 16)
                    {
                        int height = ReadBigEndianInt16(binaryReader);
                        int width = ReadBigEndianInt16(binaryReader);
                        return (width, height);
                    }

                    throw new Exception($"Unexpected image data precision '{precision}'");
                }

                // TODO: should perform many time to reduce amount of data being return at once
                binaryReader.ReadBytes(chunkLength - 2);
            }

            throw new Exception("Cound not determine image dimensions: JFIF");
        }

        private static int ReadInt16(BinaryReader binaryReader, bool isBigEndian)
        {
            if(isBigEndian)
            {
                return ReadBigEndianInt16(binaryReader);
            }
            else
            {
                   return binaryReader.ReadInt16();
            }
        }

        private static int ReadInt32(BinaryReader binaryReader, bool isBigEndian)
        {
            if (isBigEndian)
            {
                return ReadBigEndianInt32(binaryReader);
            }
            else
            {
                return binaryReader.ReadInt32();
            }
        }

        private static int ReadBigEndianInt16(BinaryReader binaryReader)
        {
            var bytes = new byte[4];
            bytes[1] = binaryReader.ReadByte();
            bytes[0] = binaryReader.ReadByte();
            return BitConverter.ToInt32(bytes, 0);
        }

        private static int ReadBigEndianInt32(BinaryReader binaryReader)
        {
            var bytes = new byte[4];
            bytes[3] = binaryReader.ReadByte();
            bytes[2] = binaryReader.ReadByte();
            bytes[1] = binaryReader.ReadByte();
            bytes[0] = binaryReader.ReadByte();

            return BitConverter.ToInt32(bytes, 0);
        }

    }
}
