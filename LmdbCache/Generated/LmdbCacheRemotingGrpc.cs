// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: lmdb_cache_remoting.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

namespace LmdbCache {
  /// <summary>
  /// Interface exported by the server.
  /// </summary>
  public static partial class LmdbCacheService
  {
    static readonly string __ServiceName = "LmdbCache.LmdbCacheService";

    static readonly grpc::Marshaller<global::LmdbCache.AddRequest> __Marshaller_LmdbCache_AddRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::LmdbCache.AddRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::LmdbCache.AddResponse> __Marshaller_LmdbCache_AddResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::LmdbCache.AddResponse.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::LmdbCache.GetRequest> __Marshaller_LmdbCache_GetRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::LmdbCache.GetRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::LmdbCache.GetResponse> __Marshaller_LmdbCache_GetResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::LmdbCache.GetResponse.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::LmdbCache.ContainsKeysResponse> __Marshaller_LmdbCache_ContainsKeysResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::LmdbCache.ContainsKeysResponse.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::LmdbCache.DeleteRequest> __Marshaller_LmdbCache_DeleteRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::LmdbCache.DeleteRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::LmdbCache.DeleteResponse> __Marshaller_LmdbCache_DeleteResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::LmdbCache.DeleteResponse.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::LmdbCache.KeyListRequest> __Marshaller_LmdbCache_KeyListRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::LmdbCache.KeyListRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::LmdbCache.KeyListResponse> __Marshaller_LmdbCache_KeyListResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::LmdbCache.KeyListResponse.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::LmdbCache.KeyValueListResponse> __Marshaller_LmdbCache_KeyValueListResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::LmdbCache.KeyValueListResponse.Parser.ParseFrom);

    static readonly grpc::Method<global::LmdbCache.AddRequest, global::LmdbCache.AddResponse> __Method_Add = new grpc::Method<global::LmdbCache.AddRequest, global::LmdbCache.AddResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Add",
        __Marshaller_LmdbCache_AddRequest,
        __Marshaller_LmdbCache_AddResponse);

    static readonly grpc::Method<global::LmdbCache.GetRequest, global::LmdbCache.GetResponse> __Method_Get = new grpc::Method<global::LmdbCache.GetRequest, global::LmdbCache.GetResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Get",
        __Marshaller_LmdbCache_GetRequest,
        __Marshaller_LmdbCache_GetResponse);

    static readonly grpc::Method<global::LmdbCache.GetRequest, global::LmdbCache.ContainsKeysResponse> __Method_ContainsKeys = new grpc::Method<global::LmdbCache.GetRequest, global::LmdbCache.ContainsKeysResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "ContainsKeys",
        __Marshaller_LmdbCache_GetRequest,
        __Marshaller_LmdbCache_ContainsKeysResponse);

    static readonly grpc::Method<global::LmdbCache.DeleteRequest, global::LmdbCache.DeleteResponse> __Method_Delete = new grpc::Method<global::LmdbCache.DeleteRequest, global::LmdbCache.DeleteResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Delete",
        __Marshaller_LmdbCache_DeleteRequest,
        __Marshaller_LmdbCache_DeleteResponse);

    static readonly grpc::Method<global::LmdbCache.KeyListRequest, global::LmdbCache.KeyListResponse> __Method_ListKeys = new grpc::Method<global::LmdbCache.KeyListRequest, global::LmdbCache.KeyListResponse>(
        grpc::MethodType.ServerStreaming,
        __ServiceName,
        "ListKeys",
        __Marshaller_LmdbCache_KeyListRequest,
        __Marshaller_LmdbCache_KeyListResponse);

    static readonly grpc::Method<global::LmdbCache.KeyListRequest, global::LmdbCache.KeyValueListResponse> __Method_ListKeyValues = new grpc::Method<global::LmdbCache.KeyListRequest, global::LmdbCache.KeyValueListResponse>(
        grpc::MethodType.ServerStreaming,
        __ServiceName,
        "ListKeyValues",
        __Marshaller_LmdbCache_KeyListRequest,
        __Marshaller_LmdbCache_KeyValueListResponse);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::LmdbCache.LmdbCacheRemotingReflection.Descriptor.Services[0]; }
    }

    /// <summary>Base class for server-side implementations of LmdbCacheService</summary>
    public abstract partial class LmdbCacheServiceBase
    {
      public virtual global::System.Threading.Tasks.Task<global::LmdbCache.AddResponse> Add(global::LmdbCache.AddRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::LmdbCache.GetResponse> Get(global::LmdbCache.GetRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::LmdbCache.ContainsKeysResponse> ContainsKeys(global::LmdbCache.GetRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::LmdbCache.DeleteResponse> Delete(global::LmdbCache.DeleteRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task ListKeys(global::LmdbCache.KeyListRequest request, grpc::IServerStreamWriter<global::LmdbCache.KeyListResponse> responseStream, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task ListKeyValues(global::LmdbCache.KeyListRequest request, grpc::IServerStreamWriter<global::LmdbCache.KeyValueListResponse> responseStream, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

    }

    /// <summary>Client for LmdbCacheService</summary>
    public partial class LmdbCacheServiceClient : grpc::ClientBase<LmdbCacheServiceClient>
    {
      /// <summary>Creates a new client for LmdbCacheService</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public LmdbCacheServiceClient(grpc::Channel channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for LmdbCacheService that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public LmdbCacheServiceClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected LmdbCacheServiceClient() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected LmdbCacheServiceClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      public virtual global::LmdbCache.AddResponse Add(global::LmdbCache.AddRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Add(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::LmdbCache.AddResponse Add(global::LmdbCache.AddRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_Add, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::LmdbCache.AddResponse> AddAsync(global::LmdbCache.AddRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return AddAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::LmdbCache.AddResponse> AddAsync(global::LmdbCache.AddRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_Add, null, options, request);
      }
      public virtual global::LmdbCache.GetResponse Get(global::LmdbCache.GetRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Get(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::LmdbCache.GetResponse Get(global::LmdbCache.GetRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_Get, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::LmdbCache.GetResponse> GetAsync(global::LmdbCache.GetRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::LmdbCache.GetResponse> GetAsync(global::LmdbCache.GetRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_Get, null, options, request);
      }
      public virtual global::LmdbCache.ContainsKeysResponse ContainsKeys(global::LmdbCache.GetRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ContainsKeys(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::LmdbCache.ContainsKeysResponse ContainsKeys(global::LmdbCache.GetRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_ContainsKeys, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::LmdbCache.ContainsKeysResponse> ContainsKeysAsync(global::LmdbCache.GetRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ContainsKeysAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::LmdbCache.ContainsKeysResponse> ContainsKeysAsync(global::LmdbCache.GetRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_ContainsKeys, null, options, request);
      }
      public virtual global::LmdbCache.DeleteResponse Delete(global::LmdbCache.DeleteRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Delete(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::LmdbCache.DeleteResponse Delete(global::LmdbCache.DeleteRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_Delete, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::LmdbCache.DeleteResponse> DeleteAsync(global::LmdbCache.DeleteRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return DeleteAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::LmdbCache.DeleteResponse> DeleteAsync(global::LmdbCache.DeleteRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_Delete, null, options, request);
      }
      public virtual grpc::AsyncServerStreamingCall<global::LmdbCache.KeyListResponse> ListKeys(global::LmdbCache.KeyListRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ListKeys(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncServerStreamingCall<global::LmdbCache.KeyListResponse> ListKeys(global::LmdbCache.KeyListRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncServerStreamingCall(__Method_ListKeys, null, options, request);
      }
      public virtual grpc::AsyncServerStreamingCall<global::LmdbCache.KeyValueListResponse> ListKeyValues(global::LmdbCache.KeyListRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ListKeyValues(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncServerStreamingCall<global::LmdbCache.KeyValueListResponse> ListKeyValues(global::LmdbCache.KeyListRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncServerStreamingCall(__Method_ListKeyValues, null, options, request);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override LmdbCacheServiceClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new LmdbCacheServiceClient(configuration);
      }
    }

    /// <summary>Creates service definition that can be registered with a server</summary>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static grpc::ServerServiceDefinition BindService(LmdbCacheServiceBase serviceImpl)
    {
      return grpc::ServerServiceDefinition.CreateBuilder()
          .AddMethod(__Method_Add, serviceImpl.Add)
          .AddMethod(__Method_Get, serviceImpl.Get)
          .AddMethod(__Method_ContainsKeys, serviceImpl.ContainsKeys)
          .AddMethod(__Method_Delete, serviceImpl.Delete)
          .AddMethod(__Method_ListKeys, serviceImpl.ListKeys)
          .AddMethod(__Method_ListKeyValues, serviceImpl.ListKeyValues).Build();
    }

  }
}
#endregion
