using AVSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVText
{
    // This is not used yet by AVBible, so is NOT implemented/initialized yet (only OOV is functional/used today
    class AVLemma
    {
        public UInt32 POS { get; private set; }
        public UInt16 wordKey { get; private set; }
        public UInt16 wordClass { get; private set; }
        public UInt16[] lemmas { get; private set; }

        public static Dictionary<UInt16, string> OOVLemmaMap { get; private set; } = null;
        public static Dictionary<string, UInt16> OOVLemmaReverseMap { get; private set; } = null;
        public static bool ok { get; private set; } = false;

        public static bool Initialize(string sdk)
        {
            ok = (sdk != null);
            if (ok)
            {
                var data = AVMemMap.Fetch("AV-Lemma-OOV.dxi", sdk);
                ok = (data != null) && File.Exists(data);

                if (ok)
                {
                    OOVLemmaMap = new Dictionary<UInt16, string>();
                    OOVLemmaReverseMap = new Dictionary<string, UInt16>();

                    using (BinaryReader reader = new BinaryReader(File.Open(data, FileMode.Open)))
                    {
                        try
                        {
                            UInt16 key = 0x8000;
                            while ((key & 0x8000) != 0)
                            {
                                key = reader.ReadUInt16();
                                var len = 1 + ((key & 0x0700) >> 8);
                                var bytes = reader.ReadBytes(len);
                                var text = System.Text.Encoding.ASCII.GetString(bytes);

                                OOVLemmaMap.Add(key, text);
                                OOVLemmaReverseMap.Add(text, key);
                            }
                        }
                        catch (EndOfStreamException eof)
                        {
                            ;
                        }
                        catch (Exception ex)
                        {
                            ok = false;
                        }
                    }
                }
            }
            return ok;
        }
    }
}
