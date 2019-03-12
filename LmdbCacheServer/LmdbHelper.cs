using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Spreads.Buffers;
using Spreads.LMDB;

namespace LmdbCacheServer
{
    public static class LmdbHelper
    {
        public static Task<bool> KeyExists(this (LMDBEnvironment, Database) envdb, DirectBuffer key)
        {
            var (env, db) = envdb;
            var taskCompletionSource = new TaskCompletionSource<bool>();

            env.Read(txn =>
            {
                var result = db.TryGet(txn, ref key, out _);
                taskCompletionSource.SetResult(result);
            });

            return taskCompletionSource.Task;
        }

        public static Task<(bool, T)> TryGet<T>(this (LMDBEnvironment, Database) envdb, DirectBuffer key, Func<DirectBuffer, T> toValue)
        {
            var (env, db) = envdb;
            var taskCompletionSource = new TaskCompletionSource<(bool, T)>();

            env.Read(txn =>
            {
                var result = db.TryGet(txn, ref key, out var value);
                taskCompletionSource.SetResult((result, result ? toValue(value) : default(T)));
            });

            return taskCompletionSource.Task;
        }

        public static async Task ReadPage<TV>(this (LMDBEnvironment, Database) envdb,
            CursorGetOption firstStep, CursorGetOption nextStep,
            Func<string, DirectBuffer> toKey, Func<DirectBuffer, string> keyFunc, Func<DirectBuffer, TV> valueFunc,
            string prefix, uint pageSize, Func<(string, TV), Task> responseStream)
        {
            const uint maxPageSize = 1024;
            pageSize = pageSize > 0 && pageSize < maxPageSize ? pageSize : maxPageSize;

            var (env, db) = envdb;
            var taskCompletionSource = new TaskCompletionSource<List<(string, TV)>>();

            env.Read(txn =>
            {
                using (var cursor = db.OpenReadOnlyCursor(txn))
                {
                    //var key = default(DirectBuffer);
                    var key = toKey(prefix); 
                    var value = default(DirectBuffer);
                    var founds = new List<(string, TV)>();

                    uint i = 0;
                    for (var hasMore = cursor.TryGet(ref key, ref value, firstStep);
                        hasMore && i < pageSize;
                        hasMore = cursor.TryGet(ref key, ref value, nextStep))
                    {
                        var k = keyFunc(key);
                        if (k.StartsWith(prefix))
                        {
                            founds.Add((k, valueFunc(value)));
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    taskCompletionSource.SetResult(founds);
                }
            });

            var keyValues = await taskCompletionSource.Task;
            foreach (var keyValue in keyValues)
            {
                await responseStream(keyValue);
            }
        }

        public static async Task ReadDupPage<TV>(this (LMDBEnvironment, Database) envdb,
            CursorGetOption firstStep, CursorGetOption nextStep,
            Func<string, DirectBuffer> toKey, Func<DirectBuffer, string> keyFunc, Func<DirectBuffer, TV> valueFunc,
            string prefix, uint pageSize, Func<(string, TV), Task> responseStream)
        {
            const UInt32 MaxPageSize = 1024;
            pageSize = pageSize > 0 && pageSize < MaxPageSize ? pageSize : MaxPageSize;

            var (env, db) = envdb;
            var taskCompletionSource = new TaskCompletionSource<List<(string, TV)>>();

            env.Read(txn =>
            {
                using (var cursor = db.OpenReadOnlyCursor(txn))
                {
                    //var key = default(DirectBuffer);
                    var key = toKey(prefix);
                    var value = default(DirectBuffer);
                    var founds = new List<(string, TV)>();

                    try
                    {
                        //if (cursor.TryGet(ref key, ref value, CursorGetOption.SetKey)) // TODO: Handle NotFound
                        {
                            uint i = 0;
                            for (var hasMore = cursor.TryGet(ref key, ref value, firstStep);
                                hasMore && i < pageSize;
                                hasMore = cursor.TryGet(ref key, ref value, nextStep))
                            {
                                var k = keyFunc(key);
                                //if (k.StartsWith(prefix))
                                {
                                    founds.Add((k, valueFunc(value)));
                                    i++;
                                }
                                //else
                                //{
                                //    break;
                                //}
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e);
                    }

                    taskCompletionSource.SetResult(founds);
                }
            });

            var keyValues = await taskCompletionSource.Task;
            foreach (var keyValue in keyValues)
            {
                await responseStream(keyValue);
            }
        }
    }
}
