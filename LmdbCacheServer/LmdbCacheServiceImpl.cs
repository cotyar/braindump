using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Utils;
using LmdbCache;
using Spreads.Buffers;
using Spreads.LMDB;

namespace LmdbCacheServer
{
    public class LmdbCacheServiceImpl : LmdbCacheService.LmdbCacheServiceBase, IDisposable
    {
        const string DbPath = "./db";

        LMDBEnvironment env;
        Database ValuesDb;

        public LmdbCacheServiceImpl()
        {
            env = LMDBEnvironment.Create(DbPath/*, DbEnvironmentFlags.WriteMap | DbEnvironmentFlags.NoSync */);
            env.MapSize = 1024 * 1024 * 1024;
            env.Open();
            var stats = env.GetStat();
            Console.WriteLine($"Database opened. DB Stats: {stats}");
            Console.WriteLine("entries: " + stats.ms_entries);
            Console.WriteLine("MaxKeySize: " + env.MaxKeySize);
            //Console.WriteLine("ReaderCheck: " + env.ReaderCheck());

            //ValuesDb = env.OpenDatabase("values_db", new DatabaseConfig(DbFlags.Create));
            ValuesDb = env.OpenDatabase("values_db", new DatabaseConfig(DbFlags.Create | DbFlags.DuplicatesSort));
        }


        DirectBuffer ToKey(string key) => new DirectBuffer(Encoding.UTF8.GetBytes(key ?? "").ToArray());
        string FromKey(DirectBuffer key) => Encoding.UTF8.GetString(key.Span.ToArray());

        DirectBuffer ToValue(ByteString value) => new DirectBuffer(value.ToByteArray());
        ByteString FromValue(DirectBuffer value) => ByteString.CopyFrom(value.Span.ToArray());
        //string _FromValueToString(DirectBuffer value) => Encoding.UTF8.GetString(value.Span.ToArray());

        DirectBuffer ToValueChunk(uint index, ByteString value) => new DirectBuffer(index.ToBytes().Concat(value.ToByteArray()));
        (uint, ByteString) FromChunkedValue(DirectBuffer value) => 
            (value.Span.Slice(0, sizeof(uint)).ToArray().ToUint32(), ByteString.CopyFrom(value.Span.Slice(sizeof(uint)).ToArray()));
        //(uint, string) FromChunkedValueToString(DirectBuffer value) {
        //    var v = FromChunkedValue(value);
        //    return (v.Item1, Encoding.UTF8.GetString(v.Item2.ToArray()));
        //}


        ConcurrentDictionary<string, AddRequest> state = new ConcurrentDictionary<string, AddRequest>();

        public override Task<AddResponse> Add(AddRequest request, ServerCallContext context)
        {
            var taskCompletionSource = new TaskCompletionSource<AddResponse>();

            env.Write(txn =>
            {
                var key = ToKey(request.Key);
                var value = ToValueChunk(0, request.Value);

                bool? saved = null;

                using (var cursor = ValuesDb.OpenCursor(txn))
                {
                    saved = cursor.TryPut(ref key, ref value, CursorPutOptions.NoOverwrite);
                }

                txn.Commit();

                var res = (!saved.HasValue) 
                    ? AddResponse.Types.AddResult.Failure
                    : (saved.Value ? AddResponse.Types.AddResult.Success : AddResponse.Types.AddResult.KeyAlreadyExists);
                taskCompletionSource.SetResult(new AddResponse { Result = res });
                Console.WriteLine($"Added: {request}");
            });

            return taskCompletionSource.Task;
        }

        public override Task<AddOrUpdateResponse> AddOrUpdate(AddRequest request, ServerCallContext context)
        {
            var taskCompletionSource = new TaskCompletionSource<AddOrUpdateResponse>();

            env.Write(txn =>
            {
                var key = ToKey(request.Key);
                var value = ToValueChunk(0, request.Value);

                bool? saved = null;

                using (var cursor = ValuesDb.OpenCursor(txn))
                {
                    saved = cursor.TryPut(ref key, ref value, CursorPutOptions.None);
                }

                txn.Commit();

                var res = (!saved.HasValue)
                    ? AddOrUpdateResponse.Types.AddOrUpdateResult.Failure
                    : (saved.Value ? AddOrUpdateResponse.Types.AddOrUpdateResult.KeyUpdated : AddOrUpdateResponse.Types.AddOrUpdateResult.KeyUpdated); // TODO: Cover Add case
                taskCompletionSource.SetResult(new AddOrUpdateResponse { Result = res });
                Console.WriteLine($"Added: {request}");
            });

            return taskCompletionSource.Task;
        }

        private async Task<bool> WriteChunk(string key, uint index, ByteString chunk)
        {
            bool saved = true; // TODO: Fix return value
                               //            var taskCompletionSource = new TaskCompletionSource<bool>();

//            Unsafe.AsRef()
            await env.WriteAsync(txn =>
            {
                try {
                    //                    var kk = ToKey(key);
                    var keyBytes = Encoding.UTF8.GetBytes($"{key}/{index}").ToArray();
                    //                    var keyBytesCopy = keyBytes.ToArray();
                    var keyGCHandle = GCHandle.Alloc(keyBytes);
                    var kk = new DirectBuffer(keyBytes.AsSpan());

                    //                    var value = ToValueChunk(index, chunk);

                    var valueBytes = index.ToBytes().Concat(chunk).ToArray();//index.ToBytes().Concat(chunk.ToByteArray());
                    var valueGCHandle = GCHandle.Alloc(valueBytes);
                    var value = new DirectBuffer(valueBytes.AsSpan());

                    if (!kk.IsValid)
                    {
                        Console.WriteLine($"Invalid");
                    }

                    Console.WriteLine($"{kk.Data.ToInt64()}");
                    Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} - {kk.Span.ToArray()}");
                    Console.WriteLine($"{value.Span.ToArray()}");
                    using (var cursor = ValuesDb.OpenCursor(txn))
                    {
                        saved = cursor.TryPut(ref kk, ref value, CursorPutOptions.None);
                    }
                    Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} - 2 - {kk.Span.ToArray()}");
                    Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} - 3 - {value.Span.ToArray()}");
                    //ValuesDb.Put(txn, ref kk, ref value);

                    txn.Commit();

                    valueGCHandle.Free();
                    keyGCHandle.Free();

//                    taskCompletionSource.SetResult(saved);
                    Console.WriteLine($"Chunk Added for Index: {index}, Key: {key}, Size: {chunk.Length}");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    txn.Abort();
                }
            }).ConfigureAwait(false);

//            return taskCompletionSource.Task;
            return saved;
        }

        public override async Task<AddResponse> AddStream(IAsyncStreamReader<AddStreamRequest> requestStream, ServerCallContext context)
        {
            try
            { 
                if (!await requestStream.MoveNext() ||
                    requestStream.Current.MsgCase != AddStreamRequest.MsgOneofCase.Header)
                {
                    Console.Error.WriteLine($"AddStream unexpected message type: {requestStream.Current.MsgCase}. 'Header' message expected");
                    return new AddResponse { Result = AddResponse.Types.AddResult.Failure };
                }

                var key = requestStream.Current.Header.Key;

                if (await (env, ValuesDb).KeyExists(ToKey(key))) return new AddResponse { Result = AddResponse.Types.AddResult.Failure };
                
                for (uint index = 0; await requestStream.MoveNext(); index++)
                {
                    if (requestStream.Current.MsgCase != AddStreamRequest.MsgOneofCase.Chunk)
                    {
                        Console.Error.WriteLine($"AddStream unexpected message type: {requestStream.Current.MsgCase}. For key {key}. 'Chunk' message expected");
                        return new AddResponse { Result = AddResponse.Types.AddResult.Failure };
                    }

                    if (requestStream.Current.Chunk.Index != index)
                    {
                        Console.Error.WriteLine($"AddStream incorrect Chunk index. Expected: {index}, received {requestStream.Current.Chunk.Index}.");
                        return new AddResponse { Result = AddResponse.Types.AddResult.Failure };
                    }

                    if (!await WriteChunk(key, index, requestStream.Current.Chunk.Value))
                    {
                        Console.Error.WriteLine($"AddStream error while writing Chunk {index}.");
                        return new AddResponse { Result = AddResponse.Types.AddResult.Failure };
                    }
                    
                    // TODO: Add removing stored chunks on failures 
                }

                Console.WriteLine($"Stream was fully written for key: {key}");

                return new AddResponse { Result = AddResponse.Types.AddResult.Success };
            }
            catch (Exception e)
            {
                return new AddResponse { Result = AddResponse.Types.AddResult.Failure };
            }
        }

        //public override Task<AddOrUpdateResponse> AddOrUpdate(AddRequest request, ServerCallContext context)
        //{
        //    var taskCompletionSource = new TaskCompletionSource<AddOrUpdateResponse>();

        //    env.Write(txn =>
        //    {
        //        var key = ToKey(request.Key);
        //        var value = ToValue(request.Value);

        //        bool? saved = null;

        //        using (var cursor = ValuesDb.OpenCursor(txn))
        //        {
        //            saved = cursor.TryPut(ref key, ref value, CursorPutOptions.None);
        //        }

        //        txn.Commit();

        //        var res = (!saved.HasValue)
        //            ? AddOrUpdateResponse.Types.AddOrUpdateResult.Failure
        //            : (saved.Value ? AddOrUpdateResponse.Types.AddOrUpdateResult.KeyUpdated : AddOrUpdateResponse.Types.AddOrUpdateResult.KeyUpdated); // TODO: Cover Add case
        //        taskCompletionSource.SetResult(new AddOrUpdateResponse { Result = res });
        //        Console.WriteLine($"Added: {request}");
        //    });

        //    return taskCompletionSource.Task;
        //}

        public override Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            var taskCompletionSource = new TaskCompletionSource<DeleteResponse>();

            env.Write(txn =>
            {
                var key = ToKey(request.Key);

                var exists = ValuesDb.TryGet(txn, ref key, out DirectBuffer value);
                var result = exists
                    ? DeleteResponse.Types.DeleteResult.Success
                    : DeleteResponse.Types.DeleteResult.NotFound;

                if (exists) ValuesDb.Delete(txn, key);

                txn.Commit();

                taskCompletionSource.SetResult(new DeleteResponse { Result = result });
                Console.WriteLine($"Deleted {result}: {request}");
            });

            return taskCompletionSource.Task;
        }

        public override Task<GetResponse> Get(GetRequest request, ServerCallContext context)
        {
            var taskCompletionSource = new TaskCompletionSource<GetResponse>();

            env.Read(txn =>
            {
                using (var cursor = ValuesDb.OpenReadOnlyCursor(txn))
                {
                    var key = ToKey(request.Key);
                    var value = default(DirectBuffer);
                    var result = cursor.TryGet(ref key, ref value, CursorGetOption.First)
                        ? GetResponse.Types.GetResult.Success
                        : GetResponse.Types.GetResult.NotFound;
                    Console.WriteLine($"Returned {result}: {request}");

                    taskCompletionSource.SetResult(new GetResponse { Result = result, Value = FromValue(value) });
                }
            });

            return taskCompletionSource.Task;
        }

        public override Task GetStream(GetRequest request, IServerStreamWriter<GetStreamResponse> responseStream, ServerCallContext context)
            => (env, ValuesDb).ReadDupPage(CursorGetOption.Set, CursorGetOption.NextDuplicate, ToKey, _ => "", FromChunkedValue, request.Key, 0, (keyValue) => responseStream.WriteAsync(new GetStreamResponse { Index = keyValue.Item2.Item1, Value = keyValue.Item2.Item2, Result = GetStreamResponse.Types.GetResult.Success })); // TODO: Fix index

        public override Task ListKeys(KeyListRequest request, IServerStreamWriter<KeyListResponse> responseStream, ServerCallContext context)
            => (env, ValuesDb).ReadPage(CursorGetOption.SetRange, CursorGetOption.NextNoDuplicate, ToKey, FromKey, _ => 0, request.KeyPrefix, request.PageSize, (keyValue) => responseStream.WriteAsync(new KeyListResponse { Key = keyValue.Item1 }));

        public override Task ListKeyValues(KeyListRequest request, IServerStreamWriter<KeyValueListResponse> responseStream, ServerCallContext context)
            => (env, ValuesDb).ReadPage(CursorGetOption.SetRange, CursorGetOption.NextNoDuplicate, ToKey, FromKey, FromValue, request.KeyPrefix, request.PageSize, (keyValue) => responseStream.WriteAsync(new KeyValueListResponse { Key = keyValue.Item1, Value = keyValue.Item2 }));

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                ValuesDb.Dispose();
                env.Close().Wait();

                _disposedValue = true;
            }
        }

        ~LmdbCacheServiceImpl()
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
