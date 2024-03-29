﻿using AVText;
using System;
using System.IO;
using System.Net.Http;

namespace AVSDK
{
    public class AVMemMap // : IAVMemMap
    {
        public static HttpClient client { get; private set; } = new HttpClient();
        public static string Fetch(string name, string folder, bool help = false)
        {
            var data = System.IO.Path.Combine(folder, name);
            if (!File.Exists(data))
            {
                string url = help ? RepoHELP : RepoSDK;

#if             LEGACY_DOTNET
                if (!url.EndsWith("/"))
                    url += '/';

                url += name;
                var task = client.GetByteArrayAsync(url);
                task.Wait();
                if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                {
                    var output = new FileStream(data, FileMode.Create, FileAccess.Write);
                    output.Write(task.Result, 0, task.Result.Length);
                    output.Close();
                }
                else return null;
#else
                if (!url.EndsWith('/'))
                    url += '/';

                url += name;
                var task = client.GetByteArrayAsync(url);
                task.Wait();
                if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                {
                    var output = new FileStream(data, FileMode.Create, FileAccess.Write);
                    output.Write(task.Result);
                    output.Close();
                }
                else return null;
#endif
            }
            return data;
        }
        public static string RepoSDK { get; private set; } = "http://digital-av.org/Z14/";
        public static string RepoHELP { get; private set; } = "http://digital-av.org/AV-Bible/Help/";

        protected System.IO.MemoryMappedFiles.MemoryMappedFile map;
        protected System.IO.MemoryMappedFiles.MemoryMappedViewAccessor view;

        public byte size        { get; protected set; }
        public UInt32 cnt       { get; protected set; }
        public UInt32 cursor    { get; protected set; }
        public string name      { get; protected set; }

        private UInt32 length;
        private UInt32 data;

        protected AVMemMap(string name, string folder, byte size) // size will be 176/DX11/22-bytes, 128//DX8/16-bytes, 32/DX2/4-bytes
        {
            this.name = name;
            this.size = size;
            var data = AVMemMap.Fetch(name, folder);

            var ok = (data != null) && File.Exists(data);

            if (ok && AVLemma.Initialize(folder) && AVLexicon.Initialize(folder))
            { 
                var info = new System.IO.FileInfo(data);
                this.length = (UInt32)info.Length;

                try
                {
                    this.map = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(data, System.IO.FileMode.Open, name);
                }
                catch (Exception ex)
                {
                    return;
                }

                cnt = length / (UInt32)size;
                this.view = map.CreateViewAccessor(0, this.length);

                this.SetCursor(0);
                this.data = 0;
            }
        }

#if WINDOWS_ONLY
        protected AVMemMap(string name, byte size) // size will be 176/DX11/22-bytes, 128//DX8/16-bytes, 32/DX2/4-bytes
        {
            this.name = name;
            this.length = (UInt32) 1+0xC0393;
            this.size = size;

            try
            {
                this.map = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(name, System.IO.MemoryMappedFiles.MemoryMappedFileRights.ReadPermissions);
            }
            catch (Exception ex)
            {
                return;
            }

            cnt = length / (UInt32) size;
            this.view = map.CreateViewAccessor(0, this.length); 

            this.SetCursor(0);
            this.data = 0;
        }
#endif
        public bool SetCursor(UInt32 csr)
        {
            bool result = (csr < this.cnt);

            if (result)
            {
                this.cursor = csr;
            }
            else
            {
                this.cursor = 0;
            }
            data = (this.cursor * size);

            return result;
        }
        public bool ResetCursor()
        {
            bool result = (this.cursor < this.cnt);

            if (!result)
            {
                this.cursor = 0;
            }
            data = (this.cursor * size);

            return result;
        }
        public bool CheckCursor(UInt32 csr)
        {
            return (csr < this.cnt);
        }
        public bool Next()
        {
            bool result = (++this.cursor < this.cnt);
            data = (this.cursor * size);

            return result;
        }

        public const UInt32 FIRST   = 0x00000000;
        public const UInt32 NEXT    = 0xFFFFFFFF;
        public const UInt32 CURRENT = 0xFFFFFFFE;

        public bool GetRecord(UInt32 director, ref Writ176 result)
        {
            bool ok;
            switch (director)
            {
                case NEXT:      ok = (++this.cursor) < this.cnt;    break;
                case CURRENT:   ok = this.cursor < this.cnt;        break;
                default:        ok = this.SetCursor(director);      break;
            }
            if (ok)
            {
                data = (this.cursor * size);
                if (this.size == 22)
                {
                    result.pos = this.POS;
                    result.lemma = this.Lemma;
                }
                else
                {
                    result.pos = 0;
                    result.lemma = 0;
                }
                if (this.size >= 16)
                {
                    result.strongs = this.Strongs;
                    result.verseIdx = this.Index;
                    result.pnwc = this.WClass;
                }
                else
                {
                    result.strongs = 0;
                    result.verseIdx = 0;
                    result.pnwc = 0;
                }
                result.word = this.WordKey;
                result.punc = this.Punctuation;
                result.trans = this.Transition;
            }
            else
            {
                result.word = 0xFFFF;
                result.punc = 0;
                result.trans = 0;
                result.strongs = 0;
                result.verseIdx = 0;
                result.pnwc = 0;
            }
            this.ResetCursor();
            return ok;
        }
        public bool GetRecordWithoutMovingCursor(UInt32 csr, ref Writ176 result)
        {
            bool ok = false;
            switch (csr)
            {
                case NEXT: return false;
                case CURRENT: break;
                default: ok = this.CheckCursor(csr); break;
            }
            if (ok)
            {
                data = (csr * size);
                if (this.size == 22)
                {
                    result.pos = this.POS;
                    result.lemma = this.Lemma;
                }
                else
                {
                    result.pos = 0;
                    result.lemma = 0;
                }
                if (this.size >= 16)
                {
                    result.strongs = this.Strongs;
                    result.verseIdx = this.Index;
                    result.pnwc = this.WClass;
                }
                else
                {
                    result.strongs = 0;
                    result.verseIdx = 0;
                    result.pnwc = 0;
                }
                result.word = this.WordKey;
                result.punc = this.Punctuation;
                result.trans = this.Transition;
            }
            else
            {
                result.word = 0xFFFF;
                result.punc = 0;
                result.trans = 0;
                result.strongs = 0;
                result.verseIdx = 0;
                result.pnwc = 0;
            }
            return ok;
        }
        public bool GetRecord(UInt32 director, ref Writ128 result)
        {
            bool ok = (director == NEXT) ? ((++this.cursor) < this.cnt) : this.SetCursor(director);

            if (ok)
            {
                data = (this.cursor * size);
                if (this.size >= 16)
                {
                    result.strongs = this.Strongs;
                    result.verseIdx = this.Index;
                    result.pnwc = this.WClass;
                }
                else
                {
                    result.strongs = 0;
                    result.verseIdx = 0;
                    result.pnwc = 0;
                }
                result.word = this.WordKey;
                result.punc = this.Punctuation;
                result.trans = this.Transition;
            }
            return ok;
        }
        public bool GetRecord(UInt32 director, ref Writ32 result)
        {
            bool ok = (director == NEXT) ? ((++this.cursor) < this.cnt) : this.SetCursor(director);

            if (ok)
            {
                result.word = this.WordKey;
                result.punc = this.Punctuation;
                result.pnwc = this.Transition;
            }
            return ok;
        }
        public void Release()
        {
            this.view.Dispose();
            this.view = null;
            this.map.Dispose();
        }

        public UInt64 Strongs
        {
            get
            {
                if (size >= 16)
                {
                    UInt64 result = (((UInt64)this.view.ReadUInt16(data+0)) << 48)
                                  + (((UInt64)this.view.ReadUInt16(data+2)) << 32)
                                  + (((UInt64)this.view.ReadUInt16(data+4)) << 16)
                                  +  ((UInt64)this.view.ReadUInt16(data+6));
                    return result;
                }
                return UInt64.MaxValue;
            }
        }
        public UInt16 Index
        {
            get
            {
                if (size >= 16)
                {
                    return this.view.ReadUInt16(data+8);
                }
                else
                {
                    return 0;
                }
            }
        }
        public UInt16 WordKey
        {
            get
            {
                return view.ReadUInt16(size >= 16 ? data+10 : data+0);        
            }
        }
        public byte Punctuation
        {
            get
            {
                return this.view.ReadByte(size >= 16 ? data+12 : data+2);
            }
        }
        public byte Transition
        {
            get
            {
                return this.view.ReadByte(size >= 16 ? data+13 : data+3);
            }
        }
        public UInt16 WClass
        {
            get
            {
                if (size >= 16)
                {
                    return this.view.ReadUInt16(data+14);
                }
                else
                {
                    return 0;
                }
            }
        }
        public UInt32 POS
        {
            get
            {
                if (size == 22)
                {
                    return this.view.ReadUInt32(data + 16);
                }
                else
                {
                    return 0;
                }
            }
        }
        public UInt16 Lemma
        {
            get
            {
                if (size == 22)
                {
                    return this.view.ReadUInt16(data + 20);
                }
                else
                {
                    return 0;
                }
            }
        }
    }
    public class MMWritDX2 : AVMemMap
    {
        public static string Name = "AV-Writ32.dx";
        public MMWritDX2(string sdk): base(MMWritDX2.Name, sdk, 2 * 2)    // 32 bits (4 bytes)
        {
            this.name = MMWritDX2.Name;
        }
#if WINDOWS_ONLY
        public MMWritDX2() : base(MMWritDX11.Name, 2 * 2)    // 32 bits (4 bytes)
        {
            name = MMWritDX11.Name;
        }
#endif
    }
    public class MMWritDX8 : AVMemMap
    {
        public static string Name = "AV-Writ-128.dx";
        public MMWritDX8(string sdk): base(MMWritDX8.Name, sdk, 8 * 2) // 128 bits (16 bytes)
        {
            name = MMWritDX8.Name;
        }
#if WINDOWS_ONLY
        public MMWritDX8() : base(MMWritDX11.Name, 8 * 2)    // 128 bits (16 bytes)
        {
            name = MMWritDX11.Name;
        }
#endif
    }
    public class MMWritDX11 : AVMemMap
    {
        public static string Name = "AV-Writ.dx";
        public MMWritDX11(string sdk) : base(MMWritDX11.Name, sdk, 11 * 2)    // 176 bits (22 bytes)
        {
            name = MMWritDX11.Name;
        }
#if WINDOWS_ONLY
        public MMWritDX11() : base(MMWritDX11.Name, 11 * 2)    // 176 bits (22 bytes)
        {
            name = MMWritDX11.Name;
        }
#endif
    }
}
