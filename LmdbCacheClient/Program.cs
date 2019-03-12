using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Protobuf;
using Grpc.Core.Utils;
using LmdbCache;

namespace LmdbCacheClient
{
    class Program
    {
        static async Task StartServer()
        {
            var channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);

            var client = new LmdbCacheService.LmdbCacheServiceClient(channel);

            Console.WriteLine("Write line to send to the server...");

            while (true)
            {
                Console.Write("$>");
                var str = (Console.ReadLine() ?? "").Trim().ToLower();
                if (str == "q") break;

                switch (str) {
                    case "a":
                        Console.Write("add key>");
                        var akey = Console.ReadLine() ?? "";
                        Console.Write("add value>");
                        var avalue = Console.ReadLine() ?? "";
                        var replyA = await client.AddAsync(new AddRequest { Key = akey, Value = ByteString.CopyFromUtf8(avalue) });
                        Console.WriteLine($"Server responded: { replyA.Result }");
                        break;
                    case "as":
                        Console.Write("add stream key>");
                        var askey = Console.ReadLine() ?? "";
                        Console.Write("add number of chunks>");
                        var ascount = int.Parse(Console.ReadLine() ?? "");
                        var clientStreamingCall = client.AddStream();
                        await clientStreamingCall.RequestStream.WriteAsync(new AddStreamRequest { Header = new AddStreamRequest.Types.Header { Key = askey } });
                        var chunks = 
                            Enumerable.Range(0, ascount).
                                Select(i => new AddStreamRequest { Chunk = new AddStreamRequest.Types.DataChunk { Index = (uint) i, Value = RandomByteString(100)}});;
                        await clientStreamingCall.RequestStream.WriteAllAsync(chunks);
                        var replyAs = clientStreamingCall.ResponseAsync;
                        Console.WriteLine($"Server responded: { replyAs.Result }");
                        break;
                    case "ars":
                        Console.Write("add random stream key>");
                        var arskey = Console.ReadLine() ?? "";
                        Console.Write("add stream length>");
                        var arscount = int.Parse(Console.ReadLine() ?? "");
                        var rstream = RandomByteStream(arscount);
                        var rclientStreamingCall = client.AddStream();
                        await rclientStreamingCall.RequestStream.WriteAsync(new AddStreamRequest { Header = new AddStreamRequest.Types.Header { Key = arskey } });
                        var byteChunks = new List<ByteString>();
                        var readCount = 0;
                        do
                        {
                            var buffer = new byte[1800];
                            readCount = await rstream.ReadAsync(buffer, 0, 1800);
                            byteChunks.Add(ByteString.CopyFrom(buffer));
                        } while (readCount > 0);

                        //Enumerable.Range(0, arscount).
                        var rchunks =
                            byteChunks.
                                Select((bytes, i) => new AddStreamRequest { Chunk = new AddStreamRequest.Types.DataChunk { Index = (uint)i, Value = bytes } }); ;
                        await rclientStreamingCall.RequestStream.WriteAllAsync(rchunks);
                        var rreplyAs = rclientStreamingCall.ResponseAsync;
                        Console.WriteLine($"Server responded: { rreplyAs.Result }");
                        break;

                    case "aa":
                        Console.Write("aadd key>");
                        var astr = Console.ReadLine() ?? "";
                        var replyAA = await client.AddAsync(new AddRequest { Key = astr, Value = ByteString.CopyFromUtf8("Hello") });
                        Console.WriteLine($"Server responded: { replyAA.Result }");
                        break;
                    case "u":
                        Console.Write("update key>");
                        var ukey = Console.ReadLine();
                        Console.Write("update value>");
                        var uvalue = Console.ReadLine() ?? "";
                        var replyU = await client.AddOrUpdateAsync(new AddRequest { Key = ukey, Value = ByteString.CopyFromUtf8(uvalue) });
                        Console.WriteLine($"Server responded: { replyU.Result }");
                        break;
                    case "uu":
                        Console.Write("uupdate key>");
                        var ustr = Console.ReadLine() ?? "";
                        var replyUU = await client.AddOrUpdateAsync(new AddRequest { Key = ustr, Value = ByteString.CopyFromUtf8("Hello") });
                        Console.WriteLine($"Server responded: { replyUU.Result }");
                        break;
                    case "d":
                        Console.Write("delete key>");
                        var dstr = Console.ReadLine() ?? "";
                        var replyD = await client.DeleteAsync(new DeleteRequest { Key = dstr });
                        Console.WriteLine($"Server responded: { replyD.Result }");
                        break;
                    case "g":
                        Console.Write("get key>");
                        var gstr = Console.ReadLine() ?? "";
                        var replyG = await client.GetAsync(new GetRequest { Key = gstr });
                        Console.WriteLine($"Server responded: { replyG }");
                        break;
                    case "gs":
                        Console.Write("get stream key>");
                        var gsKey = Console.ReadLine() ?? "";
                        using (var reply = client.GetStream(new GetRequest { Key = gsKey }))
                        {
                            var responseStream = reply.ResponseStream;
                            while (await responseStream.MoveNext())
                            {
                                var key = responseStream.Current;
                                Console.WriteLine($"Server responded: {key}");
                            }
                        }
                        break;
                    case "lk":
                        Console.Write("list key prefix>");
                        var lkPrefix = Console.ReadLine() ?? "";
                        using (var reply = client.ListKeys(new KeyListRequest { KeyPrefix = lkPrefix }))
                        {
                            var responseStream = reply.ResponseStream;
                            while (await responseStream.MoveNext())
                            {
                                var key = responseStream.Current;
                                Console.WriteLine($"Server responded: {key}");
                            }
                        }
                        break;
                    case "lkv":
                        Console.Write("list kv key prefix>");
                        var lkvPrefix = Console.ReadLine() ?? "";
                        using (var reply = client.ListKeyValues(new KeyListRequest { KeyPrefix = lkvPrefix }))
                        {
                            var responseStream = reply.ResponseStream;
                            while (await responseStream.MoveNext())
                            {
                                var key = responseStream.Current;
                                Console.WriteLine($"Server responded: {key}");
                            }
                        }
                        break;
                }
                //var reply = client.Add(new LmdbCache.AddRequest { Key = str, Value = Google.Protobuf.ByteString.CopyFromUtf8("Hello") });
                //Console.WriteLine($"Server responded: {reply}");
            }


            channel.ShutdownAsync().Wait();

        }

        static void Main(string[] args)
        {
            StartServer().Wait();
        }

        private static readonly Random Rnd = new Random();

        private static byte[] RandomArray(int length)
        {
            var buffer = new byte[length];
            Rnd.NextBytes(buffer);
            return buffer;
        }

        private static ByteString RandomByteString(int length) => ByteString.CopyFrom(RandomArray(length));

        private static Stream RandomByteStream(int length) => new MemoryStream(RandomArray(length));
    }
}
