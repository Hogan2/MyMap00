using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniGMap.Core
{
    internal class KiberTileCache : Dictionary<RawTile, byte[]>
    {
        public KiberTileCache() : base(new RawTileComparer())
        {
        }

        readonly Queue<RawTile> Queue = new Queue<RawTile>();

        /// <summary>
        /// the amount of tiles in MB to keep in memmory, default: 22MB, if each ~100Kb it's ~222 tiles
        /// </summary>

        public int MemoryCacheCapacity = 22;


        long memoryCacheSize = 0;

        /// <summary>
        /// current memmory cache size in MB
        /// </summary>
        public double MemoryCacheSize
        {
            get
            {
                return memoryCacheSize / 1048576.0;
            }
        }

        public new void Add(RawTile key, byte[] value)
        {
            Queue.Enqueue(key);
            base.Add(key, value);

            memoryCacheSize += value.Length;
        }

        // do not allow directly removal of elements
        private new void Remove(RawTile key)
        {

        }

        public new void Clear()
        {
            Queue.Clear();
            base.Clear();
            memoryCacheSize = 0;
        }

        internal void RemoveMemoryOverload()
        {
            while (MemoryCacheSize > MemoryCacheCapacity)
            {
                if (Keys.Count > 0 && Queue.Count > 0)
                {
                    RawTile first = Queue.Dequeue();
                    try
                    {
                        var m = base[first];
                        {
                            base.Remove(first);
                            memoryCacheSize -= m.Length;
                        }
                        m = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("RemoveMemoryOverload: " + ex);
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
