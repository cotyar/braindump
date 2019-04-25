// package: LmdbCache
// file: lmdb_cache_remoting.proto

import * as lmdb_cache_remoting_pb from "./lmdb_cache_remoting_pb";
import {grpc} from "@improbable-eng/grpc-web";

type LmdbCacheServiceAdd = {
  readonly methodName: string;
  readonly service: typeof LmdbCacheService;
  readonly requestStream: false;
  readonly responseStream: false;
  readonly requestType: typeof lmdb_cache_remoting_pb.AddRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.AddResponse;
};

type LmdbCacheServiceAddStream = {
  readonly methodName: string;
  readonly service: typeof LmdbCacheService;
  readonly requestStream: true;
  readonly responseStream: false;
  readonly requestType: typeof lmdb_cache_remoting_pb.AddStreamRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.AddResponse;
};

type LmdbCacheServiceDelete = {
  readonly methodName: string;
  readonly service: typeof LmdbCacheService;
  readonly requestStream: false;
  readonly responseStream: false;
  readonly requestType: typeof lmdb_cache_remoting_pb.DeleteRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.DeleteResponse;
};

type LmdbCacheServiceCopy = {
  readonly methodName: string;
  readonly service: typeof LmdbCacheService;
  readonly requestStream: false;
  readonly responseStream: false;
  readonly requestType: typeof lmdb_cache_remoting_pb.CopyRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.CopyResponse;
};

type LmdbCacheServiceGet = {
  readonly methodName: string;
  readonly service: typeof LmdbCacheService;
  readonly requestStream: false;
  readonly responseStream: false;
  readonly requestType: typeof lmdb_cache_remoting_pb.GetRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.GetResponse;
};

type LmdbCacheServiceGetStream = {
  readonly methodName: string;
  readonly service: typeof LmdbCacheService;
  readonly requestStream: false;
  readonly responseStream: true;
  readonly requestType: typeof lmdb_cache_remoting_pb.GetRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.GetStreamResponse;
};

type LmdbCacheServiceContainsKeys = {
  readonly methodName: string;
  readonly service: typeof LmdbCacheService;
  readonly requestStream: false;
  readonly responseStream: false;
  readonly requestType: typeof lmdb_cache_remoting_pb.GetRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.ContainsKeysResponse;
};

type LmdbCacheServiceListKeys = {
  readonly methodName: string;
  readonly service: typeof LmdbCacheService;
  readonly requestStream: false;
  readonly responseStream: true;
  readonly requestType: typeof lmdb_cache_remoting_pb.KeyListRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.KeyListResponse;
};

type LmdbCacheServiceListKeyValues = {
  readonly methodName: string;
  readonly service: typeof LmdbCacheService;
  readonly requestStream: false;
  readonly responseStream: true;
  readonly requestType: typeof lmdb_cache_remoting_pb.KeyListRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.KeyValueListResponse;
};

export class LmdbCacheService {
  static readonly serviceName: string;
  static readonly Add: LmdbCacheServiceAdd;
  static readonly AddStream: LmdbCacheServiceAddStream;
  static readonly Delete: LmdbCacheServiceDelete;
  static readonly Copy: LmdbCacheServiceCopy;
  static readonly Get: LmdbCacheServiceGet;
  static readonly GetStream: LmdbCacheServiceGetStream;
  static readonly ContainsKeys: LmdbCacheServiceContainsKeys;
  static readonly ListKeys: LmdbCacheServiceListKeys;
  static readonly ListKeyValues: LmdbCacheServiceListKeyValues;
}

type SyncServiceGetReplicaId = {
  readonly methodName: string;
  readonly service: typeof SyncService;
  readonly requestStream: false;
  readonly responseStream: false;
  readonly requestType: typeof lmdb_cache_remoting_pb.Empty;
  readonly responseType: typeof lmdb_cache_remoting_pb.GetReplicaIdResponse;
};

type SyncServiceSync = {
  readonly methodName: string;
  readonly service: typeof SyncService;
  readonly requestStream: true;
  readonly responseStream: true;
  readonly requestType: typeof lmdb_cache_remoting_pb.SyncPacket;
  readonly responseType: typeof lmdb_cache_remoting_pb.SyncPacket;
};

export class SyncService {
  static readonly serviceName: string;
  static readonly GetReplicaId: SyncServiceGetReplicaId;
  static readonly Sync: SyncServiceSync;
}

type MonitoringServiceGetStatus = {
  readonly methodName: string;
  readonly service: typeof MonitoringService;
  readonly requestStream: false;
  readonly responseStream: false;
  readonly requestType: typeof lmdb_cache_remoting_pb.MonitoringUpdateRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.MonitoringUpdateResponse;
};

type MonitoringServiceSubscribe = {
  readonly methodName: string;
  readonly service: typeof MonitoringService;
  readonly requestStream: false;
  readonly responseStream: true;
  readonly requestType: typeof lmdb_cache_remoting_pb.MonitoringUpdateRequest;
  readonly responseType: typeof lmdb_cache_remoting_pb.MonitoringUpdateResponse;
};

export class MonitoringService {
  static readonly serviceName: string;
  static readonly GetStatus: MonitoringServiceGetStatus;
  static readonly Subscribe: MonitoringServiceSubscribe;
}

export type ServiceError = { message: string, code: number; metadata: grpc.Metadata }
export type Status = { details: string, code: number; metadata: grpc.Metadata }

interface UnaryResponse {
  cancel(): void;
}
interface ResponseStream<T> {
  cancel(): void;
  on(type: 'data', handler: (message: T) => void): ResponseStream<T>;
  on(type: 'end', handler: () => void): ResponseStream<T>;
  on(type: 'status', handler: (status: Status) => void): ResponseStream<T>;
}
interface RequestStream<T> {
  write(message: T): RequestStream<T>;
  end(): void;
  cancel(): void;
  on(type: 'end', handler: () => void): RequestStream<T>;
  on(type: 'status', handler: (status: Status) => void): RequestStream<T>;
}
interface BidirectionalStream<ReqT, ResT> {
  write(message: ReqT): BidirectionalStream<ReqT, ResT>;
  end(): void;
  cancel(): void;
  on(type: 'data', handler: (message: ResT) => void): BidirectionalStream<ReqT, ResT>;
  on(type: 'end', handler: () => void): BidirectionalStream<ReqT, ResT>;
  on(type: 'status', handler: (status: Status) => void): BidirectionalStream<ReqT, ResT>;
}

export class LmdbCacheServiceClient {
  readonly serviceHost: string;

  constructor(serviceHost: string, options?: grpc.RpcOptions);
  add(
    requestMessage: lmdb_cache_remoting_pb.AddRequest,
    metadata: grpc.Metadata,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.AddResponse|null) => void
  ): UnaryResponse;
  add(
    requestMessage: lmdb_cache_remoting_pb.AddRequest,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.AddResponse|null) => void
  ): UnaryResponse;
  addStream(metadata?: grpc.Metadata): RequestStream<lmdb_cache_remoting_pb.AddStreamRequest>;
  delete(
    requestMessage: lmdb_cache_remoting_pb.DeleteRequest,
    metadata: grpc.Metadata,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.DeleteResponse|null) => void
  ): UnaryResponse;
  delete(
    requestMessage: lmdb_cache_remoting_pb.DeleteRequest,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.DeleteResponse|null) => void
  ): UnaryResponse;
  copy(
    requestMessage: lmdb_cache_remoting_pb.CopyRequest,
    metadata: grpc.Metadata,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.CopyResponse|null) => void
  ): UnaryResponse;
  copy(
    requestMessage: lmdb_cache_remoting_pb.CopyRequest,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.CopyResponse|null) => void
  ): UnaryResponse;
  get(
    requestMessage: lmdb_cache_remoting_pb.GetRequest,
    metadata: grpc.Metadata,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.GetResponse|null) => void
  ): UnaryResponse;
  get(
    requestMessage: lmdb_cache_remoting_pb.GetRequest,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.GetResponse|null) => void
  ): UnaryResponse;
  getStream(requestMessage: lmdb_cache_remoting_pb.GetRequest, metadata?: grpc.Metadata): ResponseStream<lmdb_cache_remoting_pb.GetStreamResponse>;
  containsKeys(
    requestMessage: lmdb_cache_remoting_pb.GetRequest,
    metadata: grpc.Metadata,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.ContainsKeysResponse|null) => void
  ): UnaryResponse;
  containsKeys(
    requestMessage: lmdb_cache_remoting_pb.GetRequest,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.ContainsKeysResponse|null) => void
  ): UnaryResponse;
  listKeys(requestMessage: lmdb_cache_remoting_pb.KeyListRequest, metadata?: grpc.Metadata): ResponseStream<lmdb_cache_remoting_pb.KeyListResponse>;
  listKeyValues(requestMessage: lmdb_cache_remoting_pb.KeyListRequest, metadata?: grpc.Metadata): ResponseStream<lmdb_cache_remoting_pb.KeyValueListResponse>;
}

export class SyncServiceClient {
  readonly serviceHost: string;

  constructor(serviceHost: string, options?: grpc.RpcOptions);
  getReplicaId(
    requestMessage: lmdb_cache_remoting_pb.Empty,
    metadata: grpc.Metadata,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.GetReplicaIdResponse|null) => void
  ): UnaryResponse;
  getReplicaId(
    requestMessage: lmdb_cache_remoting_pb.Empty,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.GetReplicaIdResponse|null) => void
  ): UnaryResponse;
  sync(metadata?: grpc.Metadata): BidirectionalStream<lmdb_cache_remoting_pb.SyncPacket, lmdb_cache_remoting_pb.SyncPacket>;
}

export class MonitoringServiceClient {
  readonly serviceHost: string;

  constructor(serviceHost: string, options?: grpc.RpcOptions);
  getStatus(
    requestMessage: lmdb_cache_remoting_pb.MonitoringUpdateRequest,
    metadata: grpc.Metadata,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.MonitoringUpdateResponse|null) => void
  ): UnaryResponse;
  getStatus(
    requestMessage: lmdb_cache_remoting_pb.MonitoringUpdateRequest,
    callback: (error: ServiceError|null, responseMessage: lmdb_cache_remoting_pb.MonitoringUpdateResponse|null) => void
  ): UnaryResponse;
  subscribe(requestMessage: lmdb_cache_remoting_pb.MonitoringUpdateRequest, metadata?: grpc.Metadata): ResponseStream<lmdb_cache_remoting_pb.MonitoringUpdateResponse>;
}

