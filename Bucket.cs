using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVText
{
    class BucketOverflow
    {
        public readonly UInt16 value;

        public BucketOverflow GetNext()
        {
            return this.next;
        }
        public BucketOverflow(UInt16 value)
        {
            this.value = value;
            this.next = null;
        }
        public BucketOverflow next;
    }
    class Bucket
    {
        private UInt32 count;
        public readonly UInt16 value;
        BucketOverflow overflow;
        BucketOverflow terminal;

        public Bucket(UInt16 value)
        {
            this.value = value;
            this.count = 1;
            this.overflow = null;
            this.terminal = null;
        }
        public UInt32 AddOverflow(UInt16 value)
        {
            if (this.terminal != null)
            {
                this.terminal.next = new BucketOverflow(value);
                this.terminal = this.terminal.next;
            }
            else
            {
                this.terminal = new BucketOverflow(value);
            }
            return ++this.count;
        }
        public UInt32 GetCount()
        {
            return this.count;
        }
        public BucketOverflow GetOverflow()
        {
            return this.overflow;
        }
    }
}
