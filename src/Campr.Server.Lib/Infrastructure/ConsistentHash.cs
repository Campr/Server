using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Campr.Server.Lib.Extensions;

namespace Campr.Server.Lib.Infrastructure
{
    public class ConsistentHash
    {
        #region Constructor & Private fields.

        public ConsistentHash(IEnumerable<string> nodes, uint replicate = 100)
        {
            Ensure.Argument.IsNotNull(nodes, nameof(nodes));

            this.circle = new SortedDictionary<uint, string>();

            this.replicate = replicate;
            this.Add(nodes.ToArray());
        }

        private readonly SortedDictionary<uint, string> circle; 
        private readonly uint replicate;

        // Cache of the ordered keys.
        private uint[] orderedKeys;

        #endregion

        #region Public interface.

        public void Add(params string[] nodes)
        {
            Ensure.Argument.IsNotNull(nodes, nameof(nodes));

            nodes.ToList().ForEach(this.AddSingle);
            this.orderedKeys = this.circle.Keys.ToArray();
        }

        public void Remove(params string[] nodes)
        {
            Ensure.Argument.IsNotNull(nodes, nameof(nodes));
            
            nodes.ToList().ForEach(this.RemoveSingle);
            this.orderedKeys = this.circle.Keys.ToArray();
        }

        public string GetNodeSlow(string key)
        {
            // Make sure we have at least one value.
            if (!this.orderedKeys.Any())
                return null;

            var hash = this.MurmurHash(key);

            // Check if we have this exact value in the circle.
            var value = this.circle.TryGetValue(hash);
            if (value != null)
                return value;

            // Otherwise, look for the first bigger key.
            var first = this.circle.Keys.FirstOrDefault(h => h >= hash);

            // If none was found, use the first one.
            if (first == default(uint))
                first = this.orderedKeys[0];

            // Return the corresponding node.
            return this.circle[first];
        }

        public string GetNode(string key)
        {
            // Make sure we have at least one value.
            if (!this.orderedKeys.Any())
                return null;

            var hash = this.MurmurHash(key);

            var beginning = 0;
            var end = this.orderedKeys.Length - 1;

            // If the provided hash is out of our bounds, return the first item.
            if (this.orderedKeys[beginning] > hash || this.orderedKeys[end] < hash)
                return this.circle[this.orderedKeys[0]];

            // Find the closest value by dichotomy.
            while ((end - beginning) > 1)
            {
                var middle = (beginning + end) / 2;

                if (this.orderedKeys[middle] >= hash)
                    end = middle;
                else
                    beginning = middle;
            }

            return this.circle[this.orderedKeys[end]];
        }

        #endregion

        #region Private methods.

        private void AddSingle(string node)
        {
            // Add the node, and all its replicas, to the circle.
            for (var i = 0; i < this.replicate; i++)
            {
                var hash = this.MurmurHash(node + i);
                this.circle[hash] = node;
            }
        }

        private void RemoveSingle(string node)
        {
            // Remove the node, and all its replicas, from the circle.
            for (var i = 0; i < this.replicate; i++)
            {
                var hash = this.MurmurHash(node + i);
                this.circle.Remove(hash);
            }
        }

        private uint MurmurHash(string src)
        {
            var algorithm = Murmur.MurmurHash.Create32();
            return BitConverter.ToUInt32(algorithm.ComputeHash(Encoding.UTF8.GetBytes(src)), 0);
        }

        #endregion
    }
}
