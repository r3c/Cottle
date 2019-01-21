﻿using System.Collections.Generic;
using Cottle.Stores.Monitor;
using Cottle.Stores.Monitor.SymbolUsages;
using Cottle.Values;

namespace Cottle.Stores
{
    public sealed class MonitorStore : IStore
    {
        /// <summary>
        /// Symbol usage statistics on underlying Cottle store instance.
        /// </summary>
        public IReadOnlyDictionary<Value, IReadOnlyList<ISymbolUsage>> Symbols => this.usage.Fields;

        public Value this[Value symbol]
        {
            get
            {
                // Output value is always defined by this implementation, see
                // method `TryGet` for details.
                this.TryGet(symbol, out var value);

                return value;
            }
            set => this.Set(symbol, value, StoreMode.Global);
        }

        private readonly IStore store;

        private readonly MutableSymbolUsage usage;

        public MonitorStore(IStore store)
        {
            this.store = store;
            this.usage = new MutableSymbolUsage(VoidValue.Instance);
        }

        public void Enter()
        {
            this.store.Enter();
        }

        public bool Leave()
        {
            return this.store.Leave();
        }

        public void Set(Value symbol, Value value, StoreMode mode)
        {
            this.store.Set(symbol, value, mode);
        }

        public bool TryGet(Value symbol, out Value value)
        {
            var result = this.store.TryGet(symbol, out Value original);

            if (!result)
                original = VoidValue.Instance;

            var child = this.usage.Declare(symbol, original);

            value = new MonitorValue(original, child);

            return result;
        }
    }
}
