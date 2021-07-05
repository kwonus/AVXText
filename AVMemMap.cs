﻿using System;

namespace AVSDK
{
#if NEVER
    public interface IAVMemMap
    {
        string GetWord();
        string GetWordWithPunctuation();
        string GetWordWithCapitolization();
        string GetWordWithCapitolizationAndPunctuation();
    }
#endif
    public class AVMemMap // : IAVMemMap
    {
        protected System.IO.MemoryMappedFiles.MemoryMappedFile map;
        protected System.IO.MemoryMappedFiles.MemoryMappedViewAccessor view;
#if AV_WRITABLE
        //  #ifdef'ed away because the mem-map never updated the underlying file
        //  left in for now in case it can be fixed
        //
        protected const bool WRITABLE = true;
        public bool dirty       { get; protected set; }
        public bool writeError  { get; protected set; }
        public bool overflow    { get; protected set; }
#endif
        public byte size        { get; protected set; }
        public UInt32 cnt       { get; protected set; }
        public UInt32 cursor    { get; protected set; }
        public bool underflow   { get; protected set; }
        public string name      { get; protected set; }
        private UInt32 length;
        private UInt32 data;

        protected AVMemMap(string path, byte size)
        {
            this.name = null;
            this.size = size;

            var info = new System.IO.FileInfo(path);
            this.length = (UInt32) info.Length;

            this.map = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(path);

            cnt = length / (UInt32) size;
            this.view = map.CreateViewAccessor(0, this.length); 
#if AV_WRITABLE
            this.writeError = false;
            this.overflow = false;
            this.dirty = false;
#endif
            this.underflow = false;
            this.SetCursor(0);
            this.data = 0;
        }
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
#if AV_WRITABLE
            if (AVMemMap.WRITABLE && dirty)
            {
                this.view.Flush();
                this.dirty = false;
            }
#endif
            return result;
        }
        public bool Next()
        {
            bool result = (++this.cursor < this.cnt);
            data = (this.cursor * size);

#if AV_WRITABLE
            if (AVMemMap.WRITABLE && this.dirty)
            {
                this.view.Flush();
                this.dirty = false;
            }
#endif
            return result;
        }

        public const UInt32 FIRST = 0x00000000;
        public const UInt32 NEXT  = 0xFFFFFFFF;
        
        public bool GetRecord(UInt32 director, ref Writ128 result)
        {
            bool ok = (director == NEXT) ? ((++this.cursor) < this.cnt) : this.SetCursor(director);

            if (ok)
            {
                data = (this.cursor * size);
                if (this.size == 16)
                {
                    result.strongs = this.Strongs;
                    result.verseIdx = this.Index;
                    result.pnwc = this.POS;
                }
                else if (this.size >= 6)
                {
                    result.strongs = UInt64.MaxValue;
                    result.verseIdx = UInt16.MaxValue;
                    result.pnwc = this.POS;
                }
                else
                {
                    result.strongs = UInt64.MaxValue;
                    result.verseIdx = UInt16.MaxValue;
                    result.pnwc = 0;
                }
                result.word = this.WordKey;
                result.punc = this.Punctuation;
                result.trans = this.Transition;
            }
            return ok;
        }
        public bool GetRecord(UInt32 director, ref WritDefunct result)
        {
            bool ok = (director == NEXT) ? ((++this.cursor) < this.cnt) : this.SetCursor(director);

            if (ok)
            {
                if (this.size >= 6)
                {
                    result.pnwc = this.POS;
                }
                else
                {
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
#if AV_WRITABLE
            if (AVMemMap.WRITABLE && dirty)
            {
                this.view.Flush();
                this.dirty = false;
            }
#endif
            this.view.Dispose();
            this.view = null;
            this.map.Dispose();
        }

        public UInt64 Strongs
        {
            get
            {
                if (size == 16)
                {
                    UInt64 result = (((UInt64)this.view.ReadUInt16(data+0)) << 48)
                                  + (((UInt64)this.view.ReadUInt16(data+2)) << 32)
                                  + (((UInt64)this.view.ReadUInt16(data+4)) << 16)
                                  +  ((UInt64)this.view.ReadUInt16(data+6));
                    return result;
                }
                this.underflow = true;
                return UInt64.MaxValue;
            }
#if AV_WRITABLE
            set
            {
                if (size == 16)
                {
                    if (value.Length >= 4)
                    {
                        if (AVMemMap.WRITABLE)
                        {
                            this.view.Write(0, value[0]);
                            this.view.Write(2, value[1]);
                            this.view.Write(4, value[2]);
                            this.view.Write(6, value[3]);
                            this.dirty = true;
                        }
                        else
                        {
                            this.writeError = true;
                        }
                    }
                    else
                    {
                        this.writeError = true;
                    }
                }
                else
                {
                    this.overflow = true;
                }
            }
#endif
        }
        public UInt16 Index
        {
            get
            {
                if (size == 16)
                {
                    return this.view.ReadUInt16(data+8);
                }
                else
                {
                    this.underflow = true;
                    return 0xFFFF;
                }
            }
#if AV_WRITABLE
            set
            {
                if (size == 16)
                {
                    if (AVMemMap.WRITABLE)
                    {
                        this.view.Write(8, value);
                        this.dirty = true;
                    }
                    else
                    {
                        this.writeError = true;
                    }                
                }
                else
                {
                    this.overflow = true;
                }
            }
#endif
        }
        public UInt16 WordKey
        {
            get
            {
                return view.ReadUInt16(size == 16 ? data+10 : data+0);        
            }
#if AV_WRITABLE
            set
            {
                if (AVMemMap.WRITABLE)
                {
                    this.view.Write(size == 16 ? 10 : 0, value);
                    this.dirty = true;
                }
                else
                {
                    this.overflow = true;
                }
            }
#endif
        }
        public byte Punctuation
        {
            get
            {
                return this.view.ReadByte(size == 16 ? data+12 : data+2);
            }
#if AV_WRITABLE
            set
            {
                if (AVMemMap.WRITABLE)
                {
                    this.view.Write(size == 16 ? 12 : 2, value);
                    this.dirty = true;
                }
                else
                {
                    this.overflow = true;
                }
            }
#endif
        }
        public byte Transition
        {
            get
            {
                return this.view.ReadByte(size == 16 ? data+13 : data+3);
            }
#if AV_WRITABLE
            set
            {
                if (AVMemMap.WRITABLE)
                {
                    this.view.Write(size == 16 ? 13 : 3, value);
                    this.dirty = true;
                }
                else
                {
                    this.overflow = true;
                }
            }
#endif
        }
        public UInt16 WClass
        {
            get
            {
                if (size == 16)
                {
                    return this.view.ReadUInt16(data+14);
                }
                if (size == 6)
                {
                    return this.view.ReadUInt16(data+4);
                }
                else
                {
                    this.underflow = true;
                    return 0xFFFF;
                }
            }
#if AV_WRITABLE
            set
            {
                if (size == 16)
                {
                    if (AVMemMap.WRITABLE)
                    {
                        this.view.Write(14, value);
                        this.dirty = true;
                    }
                    else
                    {
                        this.writeError = true;
                    }
                }
                else if (size == 6)
                {
                    if (AVMemMap.WRITABLE)
                    {
                        this.view.Write(4, value);
                        this.dirty = true;
                    }
                    else
                    {
                        this.writeError = true;
                    }
                }
                else
                {
                    this.overflow = true;
                }
            }
#endif
        }
    }
    public class MMWritDX2 : AVMemMap
    {
        public static string Name = "AV-Writ32.dx";
        public MMWritDX2(string sdk): base(System.IO.Path.Combine(sdk, MMWritDX2.Name), 2 * 2)    // 32 bits (4 bytes)
        {
            this.name = MMWritDX2.Name;
        }
    }
    public class MMWritDX8 : AVMemMap
    {
        public static string Name = "AV-Writ-128.dx";
        public MMWritDX8(string sdk): base(System.IO.Path.Combine(sdk, MMWritDX8.Name), 8 * 2) // 128 bits (16 bytes)
        {
            name = MMWritDX8.Name;
        }
    }
    public class MMWritDX11 : AVMemMap
    {
        public static string Name = "AV-Writ.dx";
        public MMWritDX11(string sdk) : base(System.IO.Path.Combine(sdk, MMWritDX11.Name), 11 * 2)    // 176 bits (22 bytes)
        {
            name = MMWritDX11.Name;
        }
    }

}
