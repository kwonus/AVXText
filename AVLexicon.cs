﻿using AVSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVText
{
    public class AVLexicon
    {
        public UInt16 Entities { get; private set; }
        public UInt32[] POS { get; private set; }
        private string[] orthograghies;

        public string Search
        {
            get
            {
                return orthograghies.Length >= 1 ? orthograghies[0] : null;
            }
        }
        public string Display
        {
            get
            {
                return orthograghies.Length >= 2 ? orthograghies[1] : Search;
            }
        }
        public string Modern
        {
            get
            {
                return orthograghies.Length >= 3 ? orthograghies[2] : Display;
            }
        }
        private AVLexicon(string search, string display, string modern, UInt32[] pos, UInt16 entities)
        {
            if (search != null)
            {
                if (modern != null)
                {
                    this.orthograghies = new string[] { search, display, modern };
                }
                else if (display != null)
                {
                    this.orthograghies = new string[] { search, display };
                }
                else
                {
                    this.orthograghies = new string[] { search };
                }
            }
            else
            {
                this.orthograghies = null; // THIS SHOULD NEVER HAPPEN !!!
            }
            this.POS = pos;
            this.Entities = entities;
        }
        private static AVLexicon[] LexMap = null;
        private static Dictionary<string, UInt16> ReverseMap = null;
        private static Dictionary<string, UInt16[]> ReverseModernMap = null;

        public static AVLexicon GetLexRecord(UInt16 id)
        {
            if (id >= 1 && id <= 12567)
            {
                return LexMap[id-1];
            }
            return null;
        }
        public static string GetLex(UInt16 id)
        {
            UInt16 caps = (UInt16) (id & 0xC000);
            id &= 0x3FFF;

            if (id >= 1 && id <= 12567)
            {
                var lex = LexMap[id-1].Search;
                if (lex.Length > 1)
                {
                    switch (caps)
                    {
                        case 0x8000: return lex.Substring(0, 1).ToUpper() + lex.Substring(1);
                        case 0x4000: return lex.ToUpper();
                        default:     return lex;
                    }
                }
                else if (caps == 0)
                {
                    return lex;
                }
                else
                {
                    return lex.ToUpper();
                }
            }
            return null;
        }
        public static string GetLexDisplay(UInt16 id)
        {
            UInt16 caps = (UInt16)(id & 0xC000);
            id &= 0x3FFF;

            if (id >= 1 && id <= 12567)
            {
                var lex = LexMap[id-1].Display;
                if (lex.Length > 1)
                {
                    switch (caps)
                    {
                        case 0x8000: return lex.Substring(0, 1).ToUpper() + lex.Substring(1);
                        case 0x4000: return lex.ToUpper();
                        default: return lex;
                    }
                }
                else if (caps == 0)
                {
                    return lex;
                }
                else
                {
                    return lex.ToUpper();
                }
            }
            return null;
        }
        public static string GetLexModern(UInt16 id)
        {
            UInt16 caps = (UInt16)(id & 0xC000);
            id &= 0x3FFF;

            if (id >= 1 && id <= 12567)
            {
                var lex = LexMap[id-1].Modern;
                if (lex.Length > 1)
                {
                    switch (caps)
                    {
                        case 0x8000: return lex.Substring(0, 1).ToUpper() + lex.Substring(1);
                        case 0x4000: return lex.ToUpper();
                        default: return lex;
                    }
                }
                else if (caps == 0)
                {
                    return lex;
                }
                else
                {
                    return lex.ToUpper();
                }
            }
            return null;
        }
        public static AVLexicon GetReverseLexRecord(string text)
        {
            if (text != null)
            {
                if (ReverseMap.ContainsKey(text))
                    return GetLexRecord(ReverseMap[text]);
                else
                {
                    var norm = text.Replace("-", "").ToLower();
                    if (ReverseMap.ContainsKey(norm))
                        return GetLexRecord(ReverseMap[norm]);
                }
            }
            return null;
        }
        public static UInt16 GetReverseLex(string text)
        {
            if (text != null)
            {
                if (ReverseMap.ContainsKey(text))
                    return ReverseMap[text];
                else
                {
                    var norm = text.Replace("-", "").ToLower();
                    if (ReverseMap.ContainsKey(norm))
                        return ReverseMap[norm];
                }
            }
            return 0;
        }
        public static UInt16[] GetReverseLexModern(string text)
        {
            if ((text != null) && ReverseModernMap.ContainsKey(text))
            {
                return ReverseModernMap[text];
            }
            return new UInt16[] { GetReverseLex(text) };
        }
        public static bool ok { get; private set; } = false;

        public static bool Initialize(string sdk)
        {
            var ok = (sdk != null);
            string data = null;
            if (ok)
            {
                data = AVMemMap.Fetch("AV-Lexicon.dxi", sdk);
                ok = (data != null) && File.Exists(data);
            }
            if (ok)
            {
                LexMap = new AVLexicon[12567];
                ReverseMap = new Dictionary<string, UInt16>();
                ReverseModernMap = new Dictionary<string, UInt16[]>();

                using (BinaryReader reader = new BinaryReader(File.Open(data, FileMode.Open)))
                {
                    try
                    {
                        UInt16 entity;
                        UInt16 size;
                        UInt32[] pos;
                        string[] orthos = new string[3];
                        UInt16 idx = 0;
                        for (UInt16 num = 1; num <= LexMap.Length; num++, idx++)
                        {
                            entity = reader.ReadUInt16();
                            size = reader.ReadUInt16();
                            pos = new UInt32[size];
                            if (size > 0)
                            {
                                for (var s = 0; s < size; s++)
                                    pos[s] = reader.ReadUInt32();
                            }

                            for (int t = 0; t < 3; t++)
                            {
                                var i = 0;
                                var buffer = new StringBuilder();
                                for (var b = reader.ReadByte(); b != 0; ++i, b = reader.ReadByte())
                                    buffer.Append((char)b);
                                if (i == 0)
                                    orthos[t] = null;
                                else
                                    orthos[t] = buffer.ToString();
                            }
                            var record = new AVLexicon(orthos[0], orthos[1], orthos[2], pos, entity);

                            LexMap[idx] = record;
                            ReverseMap[orthos[0]] = num;

                            if (orthos[2] != null)
                            {
                                var existing = ReverseModernMap.ContainsKey(orthos[2]) ? ReverseModernMap[orthos[2]] : null;
                                var replacement = (existing == null) ? new UInt16[] { num } : new UInt16[existing.Length + 1];
                                if (existing != null)
                                    replacement[existing.Length] = num;
                                bool redundant = false;

                                if (existing != null)
                                {
                                    int i = 0;
                                    foreach (var e in existing)
                                    {
                                        redundant = (e == num);
                                        if (redundant)
                                            break;
                                        replacement[i++] = e;
                                    }
                                }
                                if (!redundant)
                                {
                                    if (existing != null)
                                        ReverseModernMap.Remove(orthos[2]);
                                    ReverseModernMap[orthos[2]] = replacement;
                                }
                            }
                        }
                    } /*
                    catch (EndOfStreamException eof)
                    {
                        ;
                    } */
                    catch (Exception ex)
                    {
                        ok = false;
                    }
                }
            }
            return ok;
        }
    }
}
