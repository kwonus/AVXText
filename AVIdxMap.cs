using System;

namespace AVSDK
{
    public class AVIdxMap// NOT FULLY IMPLEMENTED, modelled after AVMemMap
    {
        protected System.IO.MemoryMappedFiles.MemoryMappedFile map;
        protected System.IO.MemoryMappedFiles.MemoryMappedViewAccessor view;

        public byte size { get; protected set; }
        public UInt32 cnt { get; protected set; }
        public UInt32 cursor { get; protected set; }
        public string name { get; protected set; }
        private UInt32 length;

        public AVIdxMap(string path, byte size)
        {
            this.name = null;
            this.size = size;

            var info = new System.IO.FileInfo(path);
            length = (UInt32)info.Length;

            map = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(path);
            view = null;

            cnt = length / (UInt32)size;

            this.SetCursor(0);
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
            this.view = map.CreateViewAccessor(cursor * size, (long)size);
            return result;
        }
        public bool Next()
        {
            bool result = (++this.cursor < this.cnt);

            if (!result)
            {
                this.cursor = 0;
            }
            this.view = map.CreateViewAccessor(cursor * size, (long)size);
            return result;
        }
        public void Release()
        {
            this.view.Dispose();
            this.view = null;
            this.map.Dispose();
        }
    }
}