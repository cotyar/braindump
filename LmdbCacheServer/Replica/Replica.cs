using System;
using System.Collections.Generic;
using System.Text;
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbLight;

namespace LmdbCacheServer.Replica
{
    public class Replica
    {
        private volatile VectorClock _clock;

        public ReplicaConfig Config { get; }
        public LightningConfig LightningConfig { get; }

        public Timestamp CurrentTime() => DateTimeOffset.UtcNow.ToTimestamp();
        public VectorClock CurrentClock() => _clock.SetTime(CurrentTime());

        private readonly LightningPersistence _lmdb;
        private readonly ExpiryTable _kvExpiryTable;
        private readonly KvTable _kvTable;
        private readonly WriteLogTable _wlTable;

        public Replica(ReplicaConfig config, LightningConfig lightningConfig, VectorClock clock = null)
        {
            Config = config;
            _clock = clock ?? VectorClockHelper.Create(Config.ReplicaId, 0);
            LightningConfig = lightningConfig;

            var kvUpdateHandler = new KvUpdateHandler(IncrementClock);

            _lmdb = new LightningPersistence(LightningConfig);
            _kvExpiryTable = new ExpiryTable(_lmdb, "kvexpiry");
            _kvTable = new KvTable(_lmdb, "kv", _kvExpiryTable,
                CurrentTime, (transaction, table, key, expiry) => { }, kvUpdateHandler);
            _wlTable = new WriteLogTable(_lmdb, "writelog", () =>
                {
                    var clk = CurrentClock();
                    return (clk.GetReplicaValue(Config.ReplicaId) ?? 0, clk);
                }, vc => (vc.Item1 + 1, vc.Item2.Increment(Config.ReplicaId))); // TODO: Remove KvUpdateHandler and simplify this
        }

        private void IncrementClock()
        {
            _clock = _clock.Increment(Config.ReplicaId);
        }
    }
}
