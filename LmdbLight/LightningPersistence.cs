using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightningDB;
using Google.Protobuf;
using LightningDB.Native;

namespace LmdbLight
{
    public struct LightningConfig
    {
        public string Name;
        public long? StorageLimit;
        public int? MaxTables;
        public int? WriteBatchTimeoutMilliseconds;
        public int? WriteBatchMaxDelegates;
    }

    public struct TableKey : IEquatable<TableKey>
    {
        public ByteString Key { get; }

        public TableKey(ByteString key)
        {
            Key = key;
            _hashCode = null;
        }

        public TableKey(byte[] key) : this(ByteString.CopyFrom(key)) {}
        public TableKey(string key) : this(ByteString.CopyFromUtf8(key)) {}

        public static implicit operator byte[](TableKey key) => key.Key.ToByteArray();
        public static implicit operator TableKey(byte[] key) => new TableKey(key);
        public static implicit operator TableKey(string key) => new TableKey(key);

        private int? _hashCode;
        public bool Equals(TableKey other) => Key.Equals(other.Key);

        public override bool Equals(object obj) => !ReferenceEquals(null, obj) && (obj is TableKey other && Equals(other));

        public override string ToString() => Key.ToStringUtf8();

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            if (_hashCode.HasValue) return _hashCode.Value;            

            var hc = Key.GetHashCode();
            _hashCode = hc;
            return hc;
        }

        public static bool operator ==(TableKey left, TableKey right) => left.Equals(right);

        public static bool operator !=(TableKey left, TableKey right) => !left.Equals(right);
    }

    public struct TableValue
    {
        public ByteString Value { get; }

        public TableValue(ByteString value) => Value = value;
        public TableValue(byte[] value) : this(ByteString.CopyFrom(value)) {}
        public TableValue(string value) : this(ByteString.CopyFromUtf8(value)) { }
        public TableValue(Stream stream) : this(ByteString.FromStream(stream)) { }

        public static implicit operator byte[] (TableValue value) => value.Value.ToByteArray();
        public static implicit operator TableValue(byte[] value) => new TableValue(value);
        public static implicit operator TableValue(string value) => new TableValue(value);
    }

    public struct TableValueChunk
    {
        public uint Index { get; }
        public ByteString Value { get; }

        public TableValueChunk(uint index, ByteString value)
        {
            Index = index;
            Value = value;
        }

        public TableValueChunk(uint index, byte[] value) : this(index, ByteString.CopyFrom(value)) { }
        public TableValueChunk(uint index, string value) : this(index, ByteString.CopyFromUtf8(value)) { }

        public static implicit operator byte[] (TableValueChunk value) => value.Index.ToBytes().Concat(value.Value.ToByteArray());
        public static implicit operator TableValueChunk(byte[] value) => 
            new TableValueChunk(value.Take(sizeof(uint)).ToArray().ToUint32(), ByteString.CopyFrom(value, sizeof(uint), value.Length - sizeof(uint)));
    }

    public class Table : IDisposable
    {
        internal readonly LightningDatabase Database;

        protected Table(WriteTransaction txn, string tableName, DatabaseOpenFlags flags)
        {
            Database = txn.Transaction.OpenDatabase(tableName, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create });
            txn.Commit();
        }

        internal Table(WriteTransaction txn, string tableName) : this(txn, tableName, DatabaseOpenFlags.Create) {}

        public void Dispose() => Database?.Dispose();
    }

    public class DupTable : Table
    {
        public DupTable(WriteTransaction txn, string tableName) : base(txn, tableName, DatabaseOpenFlags.Create | DatabaseOpenFlags.DuplicatesSort) {}
    }

    public abstract class AbstractTransaction : IDisposable
    {
        internal readonly LightningTransaction Transaction;

        protected AbstractTransaction(LightningTransaction transaction) => Transaction = transaction;

        public TableValue Get(Table table, TableKey key) => new TableValue(Transaction.Get(table.Database, key));

        public bool TryGet(Table table, TableKey key, out TableValue val)
        {
            var ret = Transaction.TryGet(table.Database, key, out var v);
            val = v;
            return ret;
        }

        public TableValue? TryGet(Table table, TableKey key)
        {
            var ret = Transaction.TryGet(table.Database, key, out var v);
            return ret ? new TableValue(v) : (TableValue?) null;
        }

        public long GetEntriesCount(Table table) => Transaction.GetEntriesCount(table.Database);

        public bool ContainsKey(Table table, TableKey key) => Transaction.ContainsKey(table.Database, key);
        public (TableKey, bool)[] ContainsKeys(Table table, TableKey[] keys) => keys.Select(key => (key, Transaction.ContainsKey(table.Database, key))).ToArray();

        protected IEnumerable<(TableKey, TV)> ReadPage<TV>(Table table,
            Func<byte[], TableKey> toKey, Func<byte[], TV> toValue,
            TableKey prefix, uint page, uint pageSize)
        {
            const uint maxPageSize = 1024 * 1024;
            pageSize = pageSize > 0 && pageSize < maxPageSize ? pageSize : maxPageSize;

            uint i = 0;
            using (var cursor = Transaction.CreateCursor(table.Database))
            {
                var keyBytes = prefix;
                if (!cursor.MoveToFirstAfter(keyBytes)) yield break;

                var pageStart = page * pageSize;
                var pageEnd = pageStart + pageSize;

                do
                {
                    var kv = cursor.Current;

                    if (!kv.Key.StartsWith(keyBytes))
                    {
                        yield break;
                    }

                    if (i >= pageStart) yield return (toKey(kv.Key), toValue(kv.Value));
                } while (++i < pageEnd && cursor.MoveNext());
            }
        }

        protected IEnumerable<TableValueChunk> ReadDupPage(
            Func<byte[], TableValueChunk> toValueChunk, DupTable table,
            TableKey key, uint page, uint pageSize)
        {
            const uint maxPageSize = 1024 * 1024;
            pageSize = pageSize > 0 && pageSize < maxPageSize ? pageSize : maxPageSize; 

            uint i = 0;
            using (var cursor = Transaction.CreateCursor(table.Database))
            {
                var keyBytes = key;
                if (!cursor.MoveTo(keyBytes)) yield break;

                do
                {
                    var kv = cursor.Current;

                    yield return (toValueChunk(kv.Value));

                } while (++i < pageSize && cursor.MoveNextDuplicate());
            }
        }

        public TableKey[] KeysByPrefix(Table table, TableKey prefix, uint page, uint pageSize) => 
            ReadPage<object>(table, b => b, _ => null, prefix, page, pageSize).Select(kv => kv.Item1).ToArray();
        
        public (TableKey, TableValue)[] PageByPrefix(Table table, TableKey prefix, uint page, uint pageSize) =>
            ReadPage<TableValue>(table, b => b, b => b, prefix, page, pageSize).ToArray();

        public TableValueChunk[] GetValueChunks(DupTable table, TableKey key, uint page, uint pageSize) =>
            ReadDupPage(b => b, table, key, page, pageSize).ToArray();


        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        protected abstract void Disposing();

        public void Dispose()
        {
            if (_disposedValue) return;

            Disposing();

            Transaction.Dispose();
            _disposedValue = true;

            GC.SuppressFinalize(this);
        }

        ~AbstractTransaction()
        {
            Dispose();
        }

        #endregion
    }

    public class ReadOnlyTransaction : AbstractTransaction
    {
        public ReadOnlyTransaction(LightningEnvironment env) : base(env.BeginTransaction(TransactionBeginFlags.ReadOnly)) { }

        protected override void Disposing()
        {
            Transaction.Abort();
        }
    }

    public class WriteTransaction : AbstractTransaction
    {
        private readonly bool _autoCommit;

        public WriteTransaction(LightningEnvironment env, bool autoCommit) : base(env.BeginTransaction()) => _autoCommit = autoCommit;

        public void Commit() => Transaction.Commit();
        public void Rollback() => Transaction.Abort();

        public Table OpenTable(string name) => new Table(this, name);
        public DupTable OpenDupTable(string name) => new DupTable(this, name);

        public void TruncateTable(Table table) => Transaction.TruncateDatabase(table.Database);
        public void DropTable(Table table) => table.Database.Drop(Transaction);

        public void Delete(Table table, TableKey key) => Transaction.Delete(table.Database, key);
        public void Delete(Table table, TableKey[] keys)
        {
            foreach (var key in keys) Transaction.Delete(table.Database, key);
        }
        public void Delete((Table, TableKey)[] keys)
        {
            foreach (var key in keys) Transaction.Delete(key.Item1.Database, key.Item2);
        }

        public bool Add(Table table, TableKey key, TableValue value)
        {
            if (ContainsKey(table, key)) return false;

            Transaction.Put(table.Database, key, value, PutOptions.NoOverwrite); // TODO: switch to Lmdb.mdb_put and validate the return code
            return true;
        }

        public bool AddOrUpdate(Table table, TableKey key, TableValue value)
        {
            var ret = !ContainsKey(table, key);
            Transaction.Put(table.Database, key, value, PutOptions.None); // TODO: switch to Lmdb.mdb_put and validate the return code
            return ret;
        }

        public (TableKey, bool)[] AddBatch(Table table, (TableKey, TableValue)[] batch) => batch.Select(kv => (kv.Item1, Add(table, kv.Item1, kv.Item2))).ToArray();
        public (TableKey, bool)[] AddOrUpdateBatch(Table table, (TableKey, TableValue)[] batch) => batch.Select(kv => (kv.Item1, AddOrUpdate(table, kv.Item1, kv.Item2))).ToArray();

        public bool AddValueChunk(DupTable table, TableKey key, TableValueChunk value, bool overrideExisting)
        {
            return AddOrUpdate(table, key, (byte[]) value); // TODO: Add override existing at index and remove extra copying
        }

        public void DeleteChunk(DupTable table, TableKey key, TableValueChunk value)
        {
            if (!ContainsKey(table, key)) return;

            using (var cursor = Transaction.CreateCursor(table.Database))
            {
                byte[] keyBytes = key;
                byte[] valueBytes = value;

                if (!cursor.MoveTo(keyBytes)) return;

                do
                {
                    if (valueBytes.SequenceEqual(cursor.Current.Value))
                    {
                        cursor.Delete();
                        return;
                    }
                } while (cursor.MoveNextDuplicate());
            }
        }

        public void DeleteAllChunks(DupTable table, TableKey key, TableValueChunk value, bool overrideExisting)
        {
            if (!ContainsKey(table, key)) return;

            using (var cursor = Transaction.CreateCursor(table.Database))
            {
                var keyBytes = key;

                if (!cursor.MoveTo(keyBytes)) return;

                cursor.DeleteDuplicates();
            }
        }

        protected override void Disposing()
        {
            if (Transaction.State == LightningTransactionState.Active)
            {
                if (_autoCommit)
                {
                    Transaction.Commit();
                }
                else
                {
                    Transaction.Abort();
                }
            }
        }
    }

    public class LightningPersistence : IDisposable
    {
        private readonly LightningConfig _config;

        private struct Delegates
        {
            public bool RequiresIsolation;
            public Func<WriteTransaction, object> WriteFunction;
            public Action<WriteTransaction> WriteAction;
            public TaskCompletionSource<object> Tcs;
        }

        protected readonly LightningEnvironment Env;
        private GCHandle _envHandle;

        private readonly BlockingCollection<Delegates> _writeQueue;

        private readonly ConcurrentDictionary<string, Table> _tables;
        private readonly ConcurrentDictionary<string, DupTable> _dupTables;

        private readonly TaskCompletionSource<object> _writeTaskCompletion = new TaskCompletionSource<object>();
        private readonly CancellationTokenSource _cts;

        public LightningPersistence(LightningConfig config)
        {
            _config = config;
            _cts = new CancellationTokenSource();

            Console.WriteLine($"ThreadId ctor: {Thread.CurrentThread.ManagedThreadId}");
            Env = new LightningEnvironment(_config.Name ?? "db") { MaxDatabases = _config.MaxTables ?? 20, MapSize = (_config.StorageLimit ?? 10L) * 1024 * 1024 * 1024 };
            Env.Open(EnvironmentOpenFlags.NoThreadLocalStorage /*| EnvironmentOpenFlags.NoLock */ );
            _envHandle = GCHandle.Alloc(Env);

            _tables = new ConcurrentDictionary<string, Table>();
            _dupTables = new ConcurrentDictionary<string, DupTable>();

            _writeQueue = new BlockingCollection<Delegates>();
            var threadStart = new ThreadStart(() =>
            {
                foreach (var batch in DequeueBatch(_writeQueue))
                {
                    ProcessBatch(batch);
                }

                _writeTaskCompletion.SetResult(null);
            });

            var writeThread = new Thread(threadStart)
            {
                Name = "LMDB Writer thread",
                IsBackground = true
            };

            writeThread.Start();
        }

        public void Sync(bool force) => Env.Flush(force);

        public void DiskUsage(bool force)
        {
            //Lmdb.mdb_env_stat(Env.Handle(), out var stats);
            //&stats.ms_branch_pages

            // TODO: Implement
        }

        public Table OpenTable(string name)
        {
            if (_dupTables.ContainsKey(name)) // TODO: Replace with normal lock
            {
                throw new ArgumentException();
            }
            return _tables.GetOrAdd(name, n => WriteAsync(tx => tx.OpenTable(n), true).Result); 
        }

        public Table OpenDupTable(string name)
        {
            if (_tables.ContainsKey(name)) // TODO: Replace with normal lock
            {
                throw new ArgumentException();
            }
            return _dupTables.GetOrAdd(name, n => WriteAsync(tx => tx.OpenDupTable(n), true).Result);
        }


        public Task TruncateTable(Table table) => WriteAsync(tx => tx.TruncateTable(table), true);
        public Task DropTable(Table table) => WriteAsync(tx => tx.DropTable(table), true);

        /// <summary>
        /// NOTE: Long running keys enumerable will prevent ReadOnly transaction from closing which can affect efficiency of page reuse in the DB  
        /// </summary>
        public (TableKey, TableValue?)[] Get(Table table, IEnumerable<TableKey> keys) =>
            Read(tx => keys.Select(key => (key, tx.TryGet(table, key))).ToArray());

        /// <summary>
        /// NOTE: Long running keys enumerable will prevent ReadOnly transaction from closing which can affect efficiency of page reuse in the DB  
        /// </summary>
        public ((Table, TableKey), TableValue?)[] Get(IEnumerable<(Table, TableKey)> keys) =>
            Read(tx => keys.Select(key => (key, tx.TryGet(key.Item1, key.Item2))).ToArray());


        /// <summary>
        /// Perform a read transaction.
        /// NOTE: ReadOnlyTransaction transaction must be disposed ASAP to avoid LMDB environment growth and allocating garbage.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>(Func<ReadOnlyTransaction, T> readFunc)
        {
            using (var txn = new ReadOnlyTransaction(Env))
            {
                return readFunc(txn);
            }
        }

        /// <summary>
        /// Perform a read transaction.
        /// NOTE: ReadOnlyTransaction transaction must be disposed ASAP to avoid LMDB environment growth and allocating garbage.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read(Action<ReadOnlyTransaction> readAction)
        {
            using (var txn = new ReadOnlyTransaction(Env))
            {
                readAction(txn);
            }
        }

        /// <summary>
        /// Queue a write action and spin until it is completed unless fireAndForget is true.
        /// </summary>
        /// <param name="writeFunction">Function to be executed</param>
        /// <param name="requiresIsolation">Guarantees that the action will me executed in a separate transaction. Otherwise batches the write requests.</param>
        /// <param name="fireAndForget">If fireAndForget is true then return immediately</param>
        /// <returns>a Task to await</returns>
        public Task<T> WriteAsync<T>(Func<WriteTransaction, T> writeFunction, bool requiresIsolation, bool fireAndForget = false)
        {
            TaskCompletionSource<object> tcs;
            if (!_writeQueue.IsAddingCompleted)
            {
                tcs = fireAndForget ? null : new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var act = new Delegates
                {
                    RequiresIsolation = requiresIsolation,
                    WriteFunction = t => writeFunction(t),
                    Tcs = tcs
                };

                _writeQueue.Add(act);
            }
            else
            {
                throw new OperationCanceledException();
            }

            return fireAndForget ? Task.FromResult<T>(default(T)) : tcs.Task.ContinueWith(t => (T) t.Result);
        }

        /// <summary>
        /// Queue a write action and spin until it is completed unless fireAndForget is true.
        /// </summary>
        /// <param name="writeAction">Function to be executed</param>
        /// <param name="requiresIsolation">Guarantees that the action will me executed in a separate transaction. Otherwise batches the write requests.</param>
        /// <param name="fireAndForget">If fireAndForget is true then return immediately</param>
        /// <returns>a Task to await</returns>
        public Task WriteAsync(Action<WriteTransaction> writeAction, bool requiresIsolation, bool fireAndForget = false)
        {
            TaskCompletionSource<object> tcs;
            if (!_writeQueue.IsAddingCompleted)
            {
                tcs = fireAndForget ? null : new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var act = new Delegates
                {
                    RequiresIsolation = requiresIsolation,
                    WriteAction = writeAction,
                    Tcs = tcs
                };

                _writeQueue.Add(act);
            }
            else
            {
                throw new OperationCanceledException();
            }

            return fireAndForget ? Task.CompletedTask : tcs.Task;
        }

        #region Write Queue Setup

        private void ProcessBatch(IEnumerable<Delegates> delegates)
        {
            var processedSources = new List<(TaskCompletionSource<object>, object)>();

            void ReportSuccess()
            {
                foreach (var (ps, v) in processedSources)
                {
                    ps.SetResult(v);
                }

                processedSources = new List<(TaskCompletionSource<object>, object)>();
            }

            void ReportError(Exception e)
            {
                // TODO: Add logging

                foreach (var (ps, v) in processedSources)
                {
                    ps.SetException(e);
                }

                processedSources = new List<(TaskCompletionSource<object>, object)>();
            }

            WriteTransaction txn = null;

            void NewTxn()
            {
                txn?.Dispose();
                txn = new WriteTransaction(Env, true);
            }

            NewTxn();

            void BounceTxnIfNeeded()
            {
                switch (txn.Transaction.State)
                {
                    case LightningTransactionState.Active:
                        break;
                    case LightningTransactionState.Reseted:
                        ReportError(new TransactionAbortedByUserCodeException());
                        NewTxn();
                        break;
                    case LightningTransactionState.Aborted:
                        ReportError(new TransactionAbortedByUserCodeException());
                        NewTxn();
                        break;
                    case LightningTransactionState.Commited:
                        ReportSuccess();
                        NewTxn();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var del in delegates)
            {
                try
                {
                    if (del.WriteFunction != null)
                    {
                        var res = del.WriteFunction(txn);
                        if (del.Tcs != null)
                        {
                            processedSources.Add((del.Tcs, res));
                        }

                        BounceTxnIfNeeded();
                    }
                    else if (del.WriteAction != null)
                    {
                        del.WriteAction(txn);
                        if (del.Tcs != null)
                        {
                            processedSources.Add((del.Tcs, null));
                        }

                        BounceTxnIfNeeded();
                    }
                    else
                    {
                        Environment.FailFast("Wrong writer thread setup");
                    }
                }
                catch (Exception e)
                {
                    ReportError(e);
                    NewTxn();
                }
            }

            try
            {
                txn?.Dispose(); // Relying on autocommit
                txn = null;
            }
            catch (Exception e)
            {
                ReportError(e);
            }

            ReportSuccess();
        }

        private IEnumerable<Delegates[]> DequeueBatch(BlockingCollection<Delegates> queue)
        {
            var batch = new List<Delegates>();
            while (!queue.IsCompleted)
            {
                var success = false;
                var item = default(Delegates);

                try
                {
                    // BLOCKING
                    success = queue.TryTake(out item, _config.WriteBatchTimeoutMilliseconds ?? 10, _cts.Token);
                }
                catch (InvalidOperationException e)
                {
                    // TODO: Log exception
                }

                if (success)
                {
                    if (item.RequiresIsolation)
                    {
                        if (batch.Count > 0)
                        {
                            yield return batch.ToArray();
                            batch = new List<Delegates>();
                        }

                        yield return new[] { item };
                    }
                    else
                    {
                        batch.Add(item);
                        if (batch.Count >= (_config.WriteBatchMaxDelegates ?? 1000))
                        {
                            yield return batch.ToArray();
                            batch = new List<Delegates>();
                        }
                    }
                }
                else
                {
                    if (batch.Count > 0)
                    {
                        yield return batch.ToArray();
                        batch = new List<Delegates>();
                    }
                }
            }
            if (batch.Count > 0)
            {
                yield return batch.ToArray();
            }
        }

        #endregion Write Queue Setup

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                if (_writeQueue != null)
                {
                    _writeQueue.CompleteAdding();
                    // let finish already added write tasks
                    _writeTaskCompletion.Task.Wait();
                    Trace.Assert(_writeQueue.Count == 0, "Write queue must be empty on exit");
                }

                _cts.Cancel();

                foreach (var table in _tables.Values)
                {
                    table.Dispose();
                }

                foreach (var table in _dupTables.Values)
                {
                    table.Dispose();
                }

                Sync(true);
                try
                {
                    Console.WriteLine($"ThreadId dispose: {Thread.CurrentThread.ManagedThreadId}");
                    Env.Dispose();
                    _envHandle.Free();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            _disposedValue = true;
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~LightningPersistence()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
