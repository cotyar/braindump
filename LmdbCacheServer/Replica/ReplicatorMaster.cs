﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncEnumerableExtensions;
using Google.Protobuf;
using Grpc.Core;
using LmdbCache;
using LmdbLight;
using static LmdbCache.SyncPacket.Types;
using static LmdbCache.Helper;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorMaster : SyncService.SyncServiceBase, IDisposable
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly Func<AbstractTransaction, string, ByteString> _getValue;
        private readonly WriteLogTable _wlTable;
        private readonly ReplicationConfig _replicationConfig;
        private readonly ConcurrentDictionary<string, (IServerStreamWriter<SyncPacket>, TaskCompletionSource<bool>)> _slaves;
        private readonly CancellationTokenSource _cts;

        public ReplicatorMaster(LightningPersistence lmdb, string ownReplicaId, Func<AbstractTransaction, string, ByteString> getValue, WriteLogTable wlTable, ReplicationConfig replicationConfig)
        {
            _lmdb = lmdb;
            _ownReplicaId = ownReplicaId;
            _getValue = getValue;
            _wlTable = wlTable;
            _replicationConfig = replicationConfig;
            _cts = new CancellationTokenSource();
            _slaves = new ConcurrentDictionary<string, (IServerStreamWriter<SyncPacket>, TaskCompletionSource<bool>)>();
        }

        public override Task<GetReplicaIdResponse> GetReplicaId(Empty request, ServerCallContext context) => 
            Task.FromResult(new GetReplicaIdResponse { ReplicaId = _ownReplicaId} );

        public override Task SyncFrom(SyncFromRequest request, IServerStreamWriter<SyncPacket> responseStream, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {
                var lastPos = request.Since;
                Console.WriteLine($"Received replication request to sync from {lastPos}");

                if (!_cts.Token.IsCancellationRequested)
                {
                    var page = _lmdb.Read(txn =>
                    {
                        var logPage = _wlTable.GetLogPage(txn, lastPos, _replicationConfig.PageSize).
                            Where(logItem => (request.IncludeMine || logItem.Item2.OriginatorReplicaId != _ownReplicaId)). // TODO: Add check for IncludeAcked
                            ToArray();

                        foreach (var logItem in logPage)
                        {
                            if (logItem.Item2.LoggedEventCase == WriteLogEvent.LoggedEventOneofCase.Updated
                                && (logItem.Item2.Updated.Value == null || logItem.Item2.Updated.Value.IsEmpty))
                            {
                                var latestValue = _getValue(txn, logItem.Item2.Updated.Key);
                                if (latestValue != null)
                                {
                                    logItem.Item2.Updated.Value = latestValue; // TODO: Handle "Value not found"
                                }
                                else
                                {
                                    Console.WriteLine($"No value for the key {logItem.Item2.Updated.Key}");
                                }
                            }
                        }
                        return logPage;
                    });

                    if (page.Length > 0)
                    {
                        if (_replicationConfig.UseBatching)
                        {
                            var batches = page.Aggregate((0, new List<List<Item>>()), (acc, logItem) =>
                            {
                                var (lastBatchBytes, currentBatches) = acc;
                                var itemSize = logItem.Item2.LoggedEventCase == WriteLogEvent.LoggedEventOneofCase.Updated
                                    ? logItem.Item2.Updated.Value.Length
                                    : 100;
                                if (currentBatches.Count == 0 ||
                                    lastBatchBytes + itemSize > 3 * 1024 * 1024)
                                {
                                    currentBatches.Add(new List<Item>
                                        {new Item {Pos = logItem.Item1, LogEvent = logItem.Item2}});
                                    return (itemSize, currentBatches);
                                }

                                currentBatches.Last().Add(new Item {Pos = logItem.Item1, LogEvent = logItem.Item2});
                                return (lastBatchBytes + itemSize, currentBatches);
                            }).Item2;

                            foreach (var batch in batches)
                            {
                                if (!_cts.Token.IsCancellationRequested)
                                {
    //                            Console.WriteLine($"Writing replication page");
                                    var items = new Items();
                                    items.Batch.AddRange(batch);
                                    await responseStream.WriteAsync(new SyncPacket {Items = items});
                                    lastPos = batch.Last().Pos;
    //                            Console.WriteLine($"Replication page has been written");
                                }
                            }
                        }
                        else
                        {
                            foreach (var pageItem in page)
                            {
                                var (pos, item) = pageItem;
                                if (!_cts.Token.IsCancellationRequested)
                                {
                                    await responseStream.WriteAsync(new SyncPacket {Item = new Item { Pos = pos, LogEvent = item }});
                                    lastPos = pos;
                                }
                            }
                        }
                    }

                    await responseStream.WriteAsync(new SyncPacket { Footer = new Footer { LastPos = lastPos } });
                }

                Console.WriteLine($"Page replicated. Last pos: {lastPos}");
            });

        public override Task<Empty> Ack(IAsyncStreamReader<SyncAckRequest> requestStream, ServerCallContext context)
        {
            return base.Ack(requestStream, context); // TODO: Override when state replication is implemented
        }

        public override Task Subscribe(SyncSubscribeRequest request, IServerStreamWriter<SyncPacket> responseStream, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {
                var tcs = new TaskCompletionSource<bool>();
                _slaves.AddOrUpdate(request.ReplicaId, (responseStream, tcs),
                    (s, writer) => throw new ReplicationIdUsedException(request.ReplicaId));

                await tcs.Task;
            });

        public Task PostWriteLogEvent(ulong pos, WriteLogEvent wle) => 
            Task.WhenAll(_slaves.Values.Select(slave => slave.Item1.WriteAsync(new SyncPacket { Item = new Item { Pos = pos, LogEvent = wle } }))); // TODO: Add "write to 'm of n'" support 

        public Task TerminateSync(string replicaId)
        {
            if (_slaves.TryRemove(replicaId, out var streamWriter))
            {
                streamWriter.Item2.SetResult(true);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var slave in _slaves)
            {
                slave.Value.Item2.SetResult(true);
            }
        }
    }
}
