// package: LmdbCache
// file: lmdb_cache_remoting.proto

import * as jspb from "google-protobuf";

export class LightningConfig extends jspb.Message {
  getName(): string;
  setName(value: string): void;

  getStoragelimit(): number;
  setStoragelimit(value: number): void;

  getMaxtables(): number;
  setMaxtables(value: number): void;

  getWritebatchtimeoutmilliseconds(): number;
  setWritebatchtimeoutmilliseconds(value: number): void;

  getWritebatchmaxdelegates(): number;
  setWritebatchmaxdelegates(value: number): void;

  getSyncmode(): LightningDbSyncMode;
  setSyncmode(value: LightningDbSyncMode): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): LightningConfig.AsObject;
  static toObject(includeInstance: boolean, msg: LightningConfig): LightningConfig.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: LightningConfig, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): LightningConfig;
  static deserializeBinaryFromReader(message: LightningConfig, reader: jspb.BinaryReader): LightningConfig;
}

export namespace LightningConfig {
  export type AsObject = {
    name: string,
    storagelimit: number,
    maxtables: number,
    writebatchtimeoutmilliseconds: number,
    writebatchmaxdelegates: number,
    syncmode: LightningDbSyncMode,
  }
}

export class ReplicationConfig extends jspb.Message {
  getPort(): number;
  setPort(value: number): void;

  getPagesize(): number;
  setPagesize(value: number): void;

  getUsebatching(): boolean;
  setUsebatching(value: boolean): void;

  getAwaitsyncfrom(): boolean;
  setAwaitsyncfrom(value: boolean): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ReplicationConfig.AsObject;
  static toObject(includeInstance: boolean, msg: ReplicationConfig): ReplicationConfig.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ReplicationConfig, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ReplicationConfig;
  static deserializeBinaryFromReader(message: ReplicationConfig, reader: jspb.BinaryReader): ReplicationConfig;
}

export namespace ReplicationConfig {
  export type AsObject = {
    port: number,
    pagesize: number,
    usebatching: boolean,
    awaitsyncfrom: boolean,
  }
}

export class ReplicaConfig extends jspb.Message {
  getReplicaid(): string;
  setReplicaid(value: string): void;

  getHostname(): string;
  setHostname(value: string): void;

  getPort(): number;
  setPort(value: number): void;

  getWebuiport(): number;
  setWebuiport(value: number): void;

  getMonitoringport(): number;
  setMonitoringport(value: number): void;

  getMonitoringinterval(): number;
  setMonitoringinterval(value: number): void;

  getMasternode(): string;
  setMasternode(value: string): void;

  hasPersistence(): boolean;
  clearPersistence(): void;
  getPersistence(): LightningConfig | undefined;
  setPersistence(value?: LightningConfig): void;

  hasReplication(): boolean;
  clearReplication(): void;
  getReplication(): ReplicationConfig | undefined;
  setReplication(value?: ReplicationConfig): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ReplicaConfig.AsObject;
  static toObject(includeInstance: boolean, msg: ReplicaConfig): ReplicaConfig.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ReplicaConfig, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ReplicaConfig;
  static deserializeBinaryFromReader(message: ReplicaConfig, reader: jspb.BinaryReader): ReplicaConfig;
}

export namespace ReplicaConfig {
  export type AsObject = {
    replicaid: string,
    hostname: string,
    port: number,
    webuiport: number,
    monitoringport: number,
    monitoringinterval: number,
    masternode: string,
    persistence?: LightningConfig.AsObject,
    replication?: ReplicationConfig.AsObject,
  }
}

export class ClientConfig extends jspb.Message {
  getUsestreaming(): boolean;
  setUsestreaming(value: boolean): void;

  getCompression(): ValueMetadata.Compression;
  setCompression(value: ValueMetadata.Compression): void;

  getHashedwith(): ValueMetadata.HashedWith;
  setHashedwith(value: ValueMetadata.HashedWith): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ClientConfig.AsObject;
  static toObject(includeInstance: boolean, msg: ClientConfig): ClientConfig.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ClientConfig, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ClientConfig;
  static deserializeBinaryFromReader(message: ClientConfig, reader: jspb.BinaryReader): ClientConfig;
}

export namespace ClientConfig {
  export type AsObject = {
    usestreaming: boolean,
    compression: ValueMetadata.Compression,
    hashedwith: ValueMetadata.HashedWith,
  }
}

export class EchoRequest extends jspb.Message {
  getEcho(): string;
  setEcho(value: string): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): EchoRequest.AsObject;
  static toObject(includeInstance: boolean, msg: EchoRequest): EchoRequest.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: EchoRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): EchoRequest;
  static deserializeBinaryFromReader(message: EchoRequest, reader: jspb.BinaryReader): EchoRequest;
}

export namespace EchoRequest {
  export type AsObject = {
    echo: string,
  }
}

export class EchoResponse extends jspb.Message {
  getEcho(): string;
  setEcho(value: string): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): EchoResponse.AsObject;
  static toObject(includeInstance: boolean, msg: EchoResponse): EchoResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: EchoResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): EchoResponse;
  static deserializeBinaryFromReader(message: EchoResponse, reader: jspb.BinaryReader): EchoResponse;
}

export namespace EchoResponse {
  export type AsObject = {
    echo: string,
  }
}

export class Empty extends jspb.Message {
  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): Empty.AsObject;
  static toObject(includeInstance: boolean, msg: Empty): Empty.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: Empty, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): Empty;
  static deserializeBinaryFromReader(message: Empty, reader: jspb.BinaryReader): Empty;
}

export namespace Empty {
  export type AsObject = {
  }
}

export class Timestamp extends jspb.Message {
  getTicksoffsetutc(): number;
  setTicksoffsetutc(value: number): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): Timestamp.AsObject;
  static toObject(includeInstance: boolean, msg: Timestamp): Timestamp.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: Timestamp, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): Timestamp;
  static deserializeBinaryFromReader(message: Timestamp, reader: jspb.BinaryReader): Timestamp;
}

export namespace Timestamp {
  export type AsObject = {
    ticksoffsetutc: number,
  }
}

export class VectorClock extends jspb.Message {
  getReplicasMap(): jspb.Map<string, number>;
  clearReplicasMap(): void;
  getTicksoffsetutc(): number;
  setTicksoffsetutc(value: number): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): VectorClock.AsObject;
  static toObject(includeInstance: boolean, msg: VectorClock): VectorClock.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: VectorClock, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): VectorClock;
  static deserializeBinaryFromReader(message: VectorClock, reader: jspb.BinaryReader): VectorClock;
}

export namespace VectorClock {
  export type AsObject = {
    replicasMap: Array<[string, number]>,
    ticksoffsetutc: number,
  }
}

export class ValueMetadata extends jspb.Message {
  getHashedwith(): ValueMetadata.HashedWith;
  setHashedwith(value: ValueMetadata.HashedWith): void;

  getHash(): Uint8Array | string;
  getHash_asU8(): Uint8Array;
  getHash_asB64(): string;
  setHash(value: Uint8Array | string): void;

  getCompression(): ValueMetadata.Compression;
  setCompression(value: ValueMetadata.Compression): void;

  getSizecompressed(): number;
  setSizecompressed(value: number): void;

  getSizefull(): number;
  setSizefull(value: number): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ValueMetadata.AsObject;
  static toObject(includeInstance: boolean, msg: ValueMetadata): ValueMetadata.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ValueMetadata, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ValueMetadata;
  static deserializeBinaryFromReader(message: ValueMetadata, reader: jspb.BinaryReader): ValueMetadata;
}

export namespace ValueMetadata {
  export type AsObject = {
    hashedwith: ValueMetadata.HashedWith,
    hash: Uint8Array | string,
    compression: ValueMetadata.Compression,
    sizecompressed: number,
    sizefull: number,
  }

  export enum HashedWith {
    MD5 = 0,
  }

  export enum Compression {
    NONE = 0,
    LZ4 = 1,
    GZIP = 2,
  }
}

export class KvMetadata extends jspb.Message {
  getStatus(): KvMetadata.Status;
  setStatus(value: KvMetadata.Status): void;

  hasExpiry(): boolean;
  clearExpiry(): void;
  getExpiry(): Timestamp | undefined;
  setExpiry(value?: Timestamp): void;

  hasOriginated(): boolean;
  clearOriginated(): void;
  getOriginated(): VectorClock | undefined;
  setOriginated(value?: VectorClock): void;

  hasLocallyupdated(): boolean;
  clearLocallyupdated(): void;
  getLocallyupdated(): VectorClock | undefined;
  setLocallyupdated(value?: VectorClock): void;

  getAction(): KvMetadata.UpdateAction;
  setAction(value: KvMetadata.UpdateAction): void;

  getCorrelationid(): string;
  setCorrelationid(value: string): void;

  getOriginatorreplicaid(): string;
  setOriginatorreplicaid(value: string): void;

  hasValuemetadata(): boolean;
  clearValuemetadata(): void;
  getValuemetadata(): ValueMetadata | undefined;
  setValuemetadata(value?: ValueMetadata): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): KvMetadata.AsObject;
  static toObject(includeInstance: boolean, msg: KvMetadata): KvMetadata.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: KvMetadata, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): KvMetadata;
  static deserializeBinaryFromReader(message: KvMetadata, reader: jspb.BinaryReader): KvMetadata;
}

export namespace KvMetadata {
  export type AsObject = {
    status: KvMetadata.Status,
    expiry?: Timestamp.AsObject,
    originated?: VectorClock.AsObject,
    locallyupdated?: VectorClock.AsObject,
    action: KvMetadata.UpdateAction,
    correlationid: string,
    originatorreplicaid: string,
    valuemetadata?: ValueMetadata.AsObject,
  }

  export enum Status {
    ACTIVE = 0,
    DELETED = 1,
    EXPIRED = 2,
  }

  export enum UpdateAction {
    ADDED = 0,
    UPDATED = 1,
    REPLICATED = 2,
  }
}

export class AddRequest extends jspb.Message {
  hasHeader(): boolean;
  clearHeader(): void;
  getHeader(): AddRequest.Header | undefined;
  setHeader(value?: AddRequest.Header): void;

  clearEntriesList(): void;
  getEntriesList(): Array<AddRequest.AddRequestEntry>;
  setEntriesList(value: Array<AddRequest.AddRequestEntry>): void;
  addEntries(value?: AddRequest.AddRequestEntry, index?: number): AddRequest.AddRequestEntry;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): AddRequest.AsObject;
  static toObject(includeInstance: boolean, msg: AddRequest): AddRequest.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: AddRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): AddRequest;
  static deserializeBinaryFromReader(message: AddRequest, reader: jspb.BinaryReader): AddRequest;
}

export namespace AddRequest {
  export type AsObject = {
    header?: AddRequest.Header.AsObject,
    entriesList: Array<AddRequest.AddRequestEntry.AsObject>,
  }

  export class Header extends jspb.Message {
    getOverrideexisting(): boolean;
    setOverrideexisting(value: boolean): void;

    getCorrelationid(): string;
    setCorrelationid(value: string): void;

    getChunkscount(): number;
    setChunkscount(value: number): void;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): Header.AsObject;
    static toObject(includeInstance: boolean, msg: Header): Header.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: Header, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): Header;
    static deserializeBinaryFromReader(message: Header, reader: jspb.BinaryReader): Header;
  }

  export namespace Header {
    export type AsObject = {
      overrideexisting: boolean,
      correlationid: string,
      chunkscount: number,
    }
  }

  export class AddRequestEntry extends jspb.Message {
    getKey(): string;
    setKey(value: string): void;

    hasExpiry(): boolean;
    clearExpiry(): void;
    getExpiry(): Timestamp | undefined;
    setExpiry(value?: Timestamp): void;

    hasValuemetadata(): boolean;
    clearValuemetadata(): void;
    getValuemetadata(): ValueMetadata | undefined;
    setValuemetadata(value?: ValueMetadata): void;

    getValue(): Uint8Array | string;
    getValue_asU8(): Uint8Array;
    getValue_asB64(): string;
    setValue(value: Uint8Array | string): void;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): AddRequestEntry.AsObject;
    static toObject(includeInstance: boolean, msg: AddRequestEntry): AddRequestEntry.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: AddRequestEntry, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): AddRequestEntry;
    static deserializeBinaryFromReader(message: AddRequestEntry, reader: jspb.BinaryReader): AddRequestEntry;
  }

  export namespace AddRequestEntry {
    export type AsObject = {
      key: string,
      expiry?: Timestamp.AsObject,
      valuemetadata?: ValueMetadata.AsObject,
      value: Uint8Array | string,
    }
  }
}

export class AddResponse extends jspb.Message {
  clearResultsList(): void;
  getResultsList(): Array<AddResponse.AddResult>;
  setResultsList(value: Array<AddResponse.AddResult>): void;
  addResults(value: AddResponse.AddResult, index?: number): AddResponse.AddResult;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): AddResponse.AsObject;
  static toObject(includeInstance: boolean, msg: AddResponse): AddResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: AddResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): AddResponse;
  static deserializeBinaryFromReader(message: AddResponse, reader: jspb.BinaryReader): AddResponse;
}

export namespace AddResponse {
  export type AsObject = {
    resultsList: Array<AddResponse.AddResult>,
  }

  export enum AddResult {
    KEY_ADDED = 0,
    KEY_UPDATED = 1,
    KEY_ALREADY_EXISTS = 2,
    FAILURE = 3,
  }
}

export class AddStreamRequest extends jspb.Message {
  hasHeader(): boolean;
  clearHeader(): void;
  getHeader(): AddRequest.Header | undefined;
  setHeader(value?: AddRequest.Header): void;

  hasChunk(): boolean;
  clearChunk(): void;
  getChunk(): AddStreamRequest.DataChunk | undefined;
  setChunk(value?: AddStreamRequest.DataChunk): void;

  getMsgCase(): AddStreamRequest.MsgCase;
  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): AddStreamRequest.AsObject;
  static toObject(includeInstance: boolean, msg: AddStreamRequest): AddStreamRequest.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: AddStreamRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): AddStreamRequest;
  static deserializeBinaryFromReader(message: AddStreamRequest, reader: jspb.BinaryReader): AddStreamRequest;
}

export namespace AddStreamRequest {
  export type AsObject = {
    header?: AddRequest.Header.AsObject,
    chunk?: AddStreamRequest.DataChunk.AsObject,
  }

  export class DataChunk extends jspb.Message {
    getIndex(): number;
    setIndex(value: number): void;

    hasEntry(): boolean;
    clearEntry(): void;
    getEntry(): AddRequest.AddRequestEntry | undefined;
    setEntry(value?: AddRequest.AddRequestEntry): void;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): DataChunk.AsObject;
    static toObject(includeInstance: boolean, msg: DataChunk): DataChunk.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: DataChunk, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): DataChunk;
    static deserializeBinaryFromReader(message: DataChunk, reader: jspb.BinaryReader): DataChunk;
  }

  export namespace DataChunk {
    export type AsObject = {
      index: number,
      entry?: AddRequest.AddRequestEntry.AsObject,
    }
  }

  export enum MsgCase {
    MSG_NOT_SET = 0,
    HEADER = 1,
    CHUNK = 2,
  }
}

export class DeleteRequest extends jspb.Message {
  getCorrelationid(): string;
  setCorrelationid(value: string): void;

  clearKeysList(): void;
  getKeysList(): Array<string>;
  setKeysList(value: Array<string>): void;
  addKeys(value: string, index?: number): string;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): DeleteRequest.AsObject;
  static toObject(includeInstance: boolean, msg: DeleteRequest): DeleteRequest.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: DeleteRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): DeleteRequest;
  static deserializeBinaryFromReader(message: DeleteRequest, reader: jspb.BinaryReader): DeleteRequest;
}

export namespace DeleteRequest {
  export type AsObject = {
    correlationid: string,
    keysList: Array<string>,
  }
}

export class DeleteResponse extends jspb.Message {
  clearResultsList(): void;
  getResultsList(): Array<DeleteResponse.DeleteResult>;
  setResultsList(value: Array<DeleteResponse.DeleteResult>): void;
  addResults(value: DeleteResponse.DeleteResult, index?: number): DeleteResponse.DeleteResult;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): DeleteResponse.AsObject;
  static toObject(includeInstance: boolean, msg: DeleteResponse): DeleteResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: DeleteResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): DeleteResponse;
  static deserializeBinaryFromReader(message: DeleteResponse, reader: jspb.BinaryReader): DeleteResponse;
}

export namespace DeleteResponse {
  export type AsObject = {
    resultsList: Array<DeleteResponse.DeleteResult>,
  }

  export enum DeleteResult {
    SUCCESS = 0,
    NOT_FOUND = 1,
    FAILURE = 2,
  }
}

export class GetRequest extends jspb.Message {
  getCorrelationid(): string;
  setCorrelationid(value: string): void;

  clearKeysList(): void;
  getKeysList(): Array<string>;
  setKeysList(value: Array<string>): void;
  addKeys(value: string, index?: number): string;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetRequest.AsObject;
  static toObject(includeInstance: boolean, msg: GetRequest): GetRequest.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: GetRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetRequest;
  static deserializeBinaryFromReader(message: GetRequest, reader: jspb.BinaryReader): GetRequest;
}

export namespace GetRequest {
  export type AsObject = {
    correlationid: string,
    keysList: Array<string>,
  }
}

export class GetResponse extends jspb.Message {
  clearResultsList(): void;
  getResultsList(): Array<GetResponse.GetResponseEntry>;
  setResultsList(value: Array<GetResponse.GetResponseEntry>): void;
  addResults(value?: GetResponse.GetResponseEntry, index?: number): GetResponse.GetResponseEntry;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetResponse.AsObject;
  static toObject(includeInstance: boolean, msg: GetResponse): GetResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: GetResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetResponse;
  static deserializeBinaryFromReader(message: GetResponse, reader: jspb.BinaryReader): GetResponse;
}

export namespace GetResponse {
  export type AsObject = {
    resultsList: Array<GetResponse.GetResponseEntry.AsObject>,
  }

  export class GetResponseEntry extends jspb.Message {
    getResult(): GetResponse.GetResponseEntry.GetResult;
    setResult(value: GetResponse.GetResponseEntry.GetResult): void;

    getIndex(): number;
    setIndex(value: number): void;

    hasValuemetadata(): boolean;
    clearValuemetadata(): void;
    getValuemetadata(): ValueMetadata | undefined;
    setValuemetadata(value?: ValueMetadata): void;

    getValue(): Uint8Array | string;
    getValue_asU8(): Uint8Array;
    getValue_asB64(): string;
    setValue(value: Uint8Array | string): void;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): GetResponseEntry.AsObject;
    static toObject(includeInstance: boolean, msg: GetResponseEntry): GetResponseEntry.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: GetResponseEntry, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): GetResponseEntry;
    static deserializeBinaryFromReader(message: GetResponseEntry, reader: jspb.BinaryReader): GetResponseEntry;
  }

  export namespace GetResponseEntry {
    export type AsObject = {
      result: GetResponse.GetResponseEntry.GetResult,
      index: number,
      valuemetadata?: ValueMetadata.AsObject,
      value: Uint8Array | string,
    }

    export enum GetResult {
      SUCCESS = 0,
      NOT_FOUND = 1,
      FAILURE = 2,
    }
  }
}

export class GetStreamResponse extends jspb.Message {
  hasResult(): boolean;
  clearResult(): void;
  getResult(): GetResponse.GetResponseEntry | undefined;
  setResult(value?: GetResponse.GetResponseEntry): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetStreamResponse.AsObject;
  static toObject(includeInstance: boolean, msg: GetStreamResponse): GetStreamResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: GetStreamResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetStreamResponse;
  static deserializeBinaryFromReader(message: GetStreamResponse, reader: jspb.BinaryReader): GetStreamResponse;
}

export namespace GetStreamResponse {
  export type AsObject = {
    result?: GetResponse.GetResponseEntry.AsObject,
  }
}

export class ContainsKeysResponse extends jspb.Message {
  clearResultsList(): void;
  getResultsList(): Array<boolean>;
  setResultsList(value: Array<boolean>): void;
  addResults(value: boolean, index?: number): boolean;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ContainsKeysResponse.AsObject;
  static toObject(includeInstance: boolean, msg: ContainsKeysResponse): ContainsKeysResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ContainsKeysResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ContainsKeysResponse;
  static deserializeBinaryFromReader(message: ContainsKeysResponse, reader: jspb.BinaryReader): ContainsKeysResponse;
}

export namespace ContainsKeysResponse {
  export type AsObject = {
    resultsList: Array<boolean>,
  }
}

export class CopyRequest extends jspb.Message {
  getCorrelationid(): string;
  setCorrelationid(value: string): void;

  clearEntriesList(): void;
  getEntriesList(): Array<CopyRequest.CopyRequestEntry>;
  setEntriesList(value: Array<CopyRequest.CopyRequestEntry>): void;
  addEntries(value?: CopyRequest.CopyRequestEntry, index?: number): CopyRequest.CopyRequestEntry;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): CopyRequest.AsObject;
  static toObject(includeInstance: boolean, msg: CopyRequest): CopyRequest.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: CopyRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): CopyRequest;
  static deserializeBinaryFromReader(message: CopyRequest, reader: jspb.BinaryReader): CopyRequest;
}

export namespace CopyRequest {
  export type AsObject = {
    correlationid: string,
    entriesList: Array<CopyRequest.CopyRequestEntry.AsObject>,
  }

  export class CopyRequestEntry extends jspb.Message {
    getKeyfrom(): string;
    setKeyfrom(value: string): void;

    hasExpiry(): boolean;
    clearExpiry(): void;
    getExpiry(): Timestamp | undefined;
    setExpiry(value?: Timestamp): void;

    getKeyto(): string;
    setKeyto(value: string): void;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): CopyRequestEntry.AsObject;
    static toObject(includeInstance: boolean, msg: CopyRequestEntry): CopyRequestEntry.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: CopyRequestEntry, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): CopyRequestEntry;
    static deserializeBinaryFromReader(message: CopyRequestEntry, reader: jspb.BinaryReader): CopyRequestEntry;
  }

  export namespace CopyRequestEntry {
    export type AsObject = {
      keyfrom: string,
      expiry?: Timestamp.AsObject,
      keyto: string,
    }
  }
}

export class CopyResponse extends jspb.Message {
  clearResultsList(): void;
  getResultsList(): Array<CopyResponse.CopyResult>;
  setResultsList(value: Array<CopyResponse.CopyResult>): void;
  addResults(value: CopyResponse.CopyResult, index?: number): CopyResponse.CopyResult;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): CopyResponse.AsObject;
  static toObject(includeInstance: boolean, msg: CopyResponse): CopyResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: CopyResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): CopyResponse;
  static deserializeBinaryFromReader(message: CopyResponse, reader: jspb.BinaryReader): CopyResponse;
}

export namespace CopyResponse {
  export type AsObject = {
    resultsList: Array<CopyResponse.CopyResult>,
  }

  export enum CopyResult {
    SUCCESS = 0,
    FROM_KEY_NOT_FOUND = 1,
    TO_KEY_EXISTS = 2,
    FAILURE = 3,
  }
}

export class KeyListRequest extends jspb.Message {
  getKeyprefix(): string;
  setKeyprefix(value: string): void;

  getPagesize(): number;
  setPagesize(value: number): void;

  getPage(): number;
  setPage(value: number): void;

  getCorrelationid(): string;
  setCorrelationid(value: string): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): KeyListRequest.AsObject;
  static toObject(includeInstance: boolean, msg: KeyListRequest): KeyListRequest.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: KeyListRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): KeyListRequest;
  static deserializeBinaryFromReader(message: KeyListRequest, reader: jspb.BinaryReader): KeyListRequest;
}

export namespace KeyListRequest {
  export type AsObject = {
    keyprefix: string,
    pagesize: number,
    page: number,
    correlationid: string,
  }
}

export class KeyListResponse extends jspb.Message {
  getKey(): string;
  setKey(value: string): void;

  hasMetadata(): boolean;
  clearMetadata(): void;
  getMetadata(): KvMetadata | undefined;
  setMetadata(value?: KvMetadata): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): KeyListResponse.AsObject;
  static toObject(includeInstance: boolean, msg: KeyListResponse): KeyListResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: KeyListResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): KeyListResponse;
  static deserializeBinaryFromReader(message: KeyListResponse, reader: jspb.BinaryReader): KeyListResponse;
}

export namespace KeyListResponse {
  export type AsObject = {
    key: string,
    metadata?: KvMetadata.AsObject,
  }
}

export class KeyValueListResponse extends jspb.Message {
  getKey(): string;
  setKey(value: string): void;

  getValue(): Uint8Array | string;
  getValue_asU8(): Uint8Array;
  getValue_asB64(): string;
  setValue(value: Uint8Array | string): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): KeyValueListResponse.AsObject;
  static toObject(includeInstance: boolean, msg: KeyValueListResponse): KeyValueListResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: KeyValueListResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): KeyValueListResponse;
  static deserializeBinaryFromReader(message: KeyValueListResponse, reader: jspb.BinaryReader): KeyValueListResponse;
}

export namespace KeyValueListResponse {
  export type AsObject = {
    key: string,
    value: Uint8Array | string,
  }
}

export class KeyPageResponse extends jspb.Message {
  clearKeyresponseList(): void;
  getKeyresponseList(): Array<KeyListResponse>;
  setKeyresponseList(value: Array<KeyListResponse>): void;
  addKeyresponse(value?: KeyListResponse, index?: number): KeyListResponse;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): KeyPageResponse.AsObject;
  static toObject(includeInstance: boolean, msg: KeyPageResponse): KeyPageResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: KeyPageResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): KeyPageResponse;
  static deserializeBinaryFromReader(message: KeyPageResponse, reader: jspb.BinaryReader): KeyPageResponse;
}

export namespace KeyPageResponse {
  export type AsObject = {
    keyresponseList: Array<KeyListResponse.AsObject>,
  }
}

export class WriteLogEvent extends jspb.Message {
  hasOriginated(): boolean;
  clearOriginated(): void;
  getOriginated(): VectorClock | undefined;
  setOriginated(value?: VectorClock): void;

  getCorrelationid(): string;
  setCorrelationid(value: string): void;

  getOriginatorreplicaid(): string;
  setOriginatorreplicaid(value: string): void;

  hasValuemetadata(): boolean;
  clearValuemetadata(): void;
  getValuemetadata(): ValueMetadata | undefined;
  setValuemetadata(value?: ValueMetadata): void;

  hasLocallysaved(): boolean;
  clearLocallysaved(): void;
  getLocallysaved(): VectorClock | undefined;
  setLocallysaved(value?: VectorClock): void;

  hasUpdated(): boolean;
  clearUpdated(): void;
  getUpdated(): WriteLogEvent.AddedOrUpdated | undefined;
  setUpdated(value?: WriteLogEvent.AddedOrUpdated): void;

  hasDeleted(): boolean;
  clearDeleted(): void;
  getDeleted(): WriteLogEvent.Deleted | undefined;
  setDeleted(value?: WriteLogEvent.Deleted): void;

  getLoggedeventCase(): WriteLogEvent.LoggedeventCase;
  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): WriteLogEvent.AsObject;
  static toObject(includeInstance: boolean, msg: WriteLogEvent): WriteLogEvent.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: WriteLogEvent, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): WriteLogEvent;
  static deserializeBinaryFromReader(message: WriteLogEvent, reader: jspb.BinaryReader): WriteLogEvent;
}

export namespace WriteLogEvent {
  export type AsObject = {
    originated?: VectorClock.AsObject,
    correlationid: string,
    originatorreplicaid: string,
    valuemetadata?: ValueMetadata.AsObject,
    locallysaved?: VectorClock.AsObject,
    updated?: WriteLogEvent.AddedOrUpdated.AsObject,
    deleted?: WriteLogEvent.Deleted.AsObject,
  }

  export class AddedOrUpdated extends jspb.Message {
    getKey(): string;
    setKey(value: string): void;

    hasExpiry(): boolean;
    clearExpiry(): void;
    getExpiry(): Timestamp | undefined;
    setExpiry(value?: Timestamp): void;

    getValue(): Uint8Array | string;
    getValue_asU8(): Uint8Array;
    getValue_asB64(): string;
    setValue(value: Uint8Array | string): void;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): AddedOrUpdated.AsObject;
    static toObject(includeInstance: boolean, msg: AddedOrUpdated): AddedOrUpdated.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: AddedOrUpdated, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): AddedOrUpdated;
    static deserializeBinaryFromReader(message: AddedOrUpdated, reader: jspb.BinaryReader): AddedOrUpdated;
  }

  export namespace AddedOrUpdated {
    export type AsObject = {
      key: string,
      expiry?: Timestamp.AsObject,
      value: Uint8Array | string,
    }
  }

  export class Deleted extends jspb.Message {
    getKey(): string;
    setKey(value: string): void;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): Deleted.AsObject;
    static toObject(includeInstance: boolean, msg: Deleted): Deleted.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: Deleted, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): Deleted;
    static deserializeBinaryFromReader(message: Deleted, reader: jspb.BinaryReader): Deleted;
  }

  export namespace Deleted {
    export type AsObject = {
      key: string,
    }
  }

  export enum LoggedeventCase {
    LOGGEDEVENT_NOT_SET = 0,
    UPDATED = 10,
    DELETED = 11,
  }
}

export class GetReplicaIdResponse extends jspb.Message {
  getReplicaid(): string;
  setReplicaid(value: string): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetReplicaIdResponse.AsObject;
  static toObject(includeInstance: boolean, msg: GetReplicaIdResponse): GetReplicaIdResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: GetReplicaIdResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetReplicaIdResponse;
  static deserializeBinaryFromReader(message: GetReplicaIdResponse, reader: jspb.BinaryReader): GetReplicaIdResponse;
}

export namespace GetReplicaIdResponse {
  export type AsObject = {
    replicaid: string,
  }
}

export class SyncPacket extends jspb.Message {
  getReplicaid(): string;
  setReplicaid(value: string): void;

  hasSyncfrom(): boolean;
  clearSyncfrom(): void;
  getSyncfrom(): SyncPacket.SyncFrom | undefined;
  setSyncfrom(value?: SyncPacket.SyncFrom): void;

  hasItems(): boolean;
  clearItems(): void;
  getItems(): SyncPacket.Items | undefined;
  setItems(value?: SyncPacket.Items): void;

  hasItem(): boolean;
  clearItem(): void;
  getItem(): SyncPacket.Item | undefined;
  setItem(value?: SyncPacket.Item): void;

  hasSkippos(): boolean;
  clearSkippos(): void;
  getSkippos(): SyncPacket.SkipPos | undefined;
  setSkippos(value?: SyncPacket.SkipPos): void;

  getPacketCase(): SyncPacket.PacketCase;
  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): SyncPacket.AsObject;
  static toObject(includeInstance: boolean, msg: SyncPacket): SyncPacket.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: SyncPacket, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): SyncPacket;
  static deserializeBinaryFromReader(message: SyncPacket, reader: jspb.BinaryReader): SyncPacket;
}

export namespace SyncPacket {
  export type AsObject = {
    replicaid: string,
    syncfrom?: SyncPacket.SyncFrom.AsObject,
    items?: SyncPacket.Items.AsObject,
    item?: SyncPacket.Item.AsObject,
    skippos?: SyncPacket.SkipPos.AsObject,
  }

  export class SyncFrom extends jspb.Message {
    getReplicaid(): string;
    setReplicaid(value: string): void;

    getSince(): number;
    setSince(value: number): void;

    getIncludemine(): boolean;
    setIncludemine(value: boolean): void;

    getIncludeacked(): boolean;
    setIncludeacked(value: boolean): void;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): SyncFrom.AsObject;
    static toObject(includeInstance: boolean, msg: SyncFrom): SyncFrom.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: SyncFrom, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): SyncFrom;
    static deserializeBinaryFromReader(message: SyncFrom, reader: jspb.BinaryReader): SyncFrom;
  }

  export namespace SyncFrom {
    export type AsObject = {
      replicaid: string,
      since: number,
      includemine: boolean,
      includeacked: boolean,
    }
  }

  export class Item extends jspb.Message {
    hasLogevent(): boolean;
    clearLogevent(): void;
    getLogevent(): WriteLogEvent | undefined;
    setLogevent(value?: WriteLogEvent): void;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): Item.AsObject;
    static toObject(includeInstance: boolean, msg: Item): Item.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: Item, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): Item;
    static deserializeBinaryFromReader(message: Item, reader: jspb.BinaryReader): Item;
  }

  export namespace Item {
    export type AsObject = {
      logevent?: WriteLogEvent.AsObject,
    }
  }

  export class Items extends jspb.Message {
    clearBatchList(): void;
    getBatchList(): Array<SyncPacket.Item>;
    setBatchList(value: Array<SyncPacket.Item>): void;
    addBatch(value?: SyncPacket.Item, index?: number): SyncPacket.Item;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): Items.AsObject;
    static toObject(includeInstance: boolean, msg: Items): Items.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: Items, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): Items;
    static deserializeBinaryFromReader(message: Items, reader: jspb.BinaryReader): Items;
  }

  export namespace Items {
    export type AsObject = {
      batchList: Array<SyncPacket.Item.AsObject>,
    }
  }

  export class SkipPos extends jspb.Message {
    getLastpos(): number;
    setLastpos(value: number): void;

    serializeBinary(): Uint8Array;
    toObject(includeInstance?: boolean): SkipPos.AsObject;
    static toObject(includeInstance: boolean, msg: SkipPos): SkipPos.AsObject;
    static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
    static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
    static serializeBinaryToWriter(message: SkipPos, writer: jspb.BinaryWriter): void;
    static deserializeBinary(bytes: Uint8Array): SkipPos;
    static deserializeBinaryFromReader(message: SkipPos, reader: jspb.BinaryReader): SkipPos;
  }

  export namespace SkipPos {
    export type AsObject = {
      lastpos: number,
    }
  }

  export enum PacketCase {
    PACKET_NOT_SET = 0,
    SYNCFROM = 4,
    ITEMS = 5,
    ITEM = 6,
    SKIPPOS = 7,
  }
}

export class MonitoringUpdateRequest extends jspb.Message {
  getCorrelationid(): string;
  setCorrelationid(value: string): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): MonitoringUpdateRequest.AsObject;
  static toObject(includeInstance: boolean, msg: MonitoringUpdateRequest): MonitoringUpdateRequest.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: MonitoringUpdateRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): MonitoringUpdateRequest;
  static deserializeBinaryFromReader(message: MonitoringUpdateRequest, reader: jspb.BinaryReader): MonitoringUpdateRequest;
}

export namespace MonitoringUpdateRequest {
  export type AsObject = {
    correlationid: string,
  }
}

export class MonitoringUpdateResponse extends jspb.Message {
  hasStatus(): boolean;
  clearStatus(): void;
  getStatus(): ReplicaStatus | undefined;
  setStatus(value?: ReplicaStatus): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): MonitoringUpdateResponse.AsObject;
  static toObject(includeInstance: boolean, msg: MonitoringUpdateResponse): MonitoringUpdateResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: MonitoringUpdateResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): MonitoringUpdateResponse;
  static deserializeBinaryFromReader(message: MonitoringUpdateResponse, reader: jspb.BinaryReader): MonitoringUpdateResponse;
}

export namespace MonitoringUpdateResponse {
  export type AsObject = {
    status?: ReplicaStatus.AsObject,
  }
}

export class ReplicaStatus extends jspb.Message {
  getReplicaid(): string;
  setReplicaid(value: string): void;

  hasConnectioninfo(): boolean;
  clearConnectioninfo(): void;
  getConnectioninfo(): ReplicaConnectionInfo | undefined;
  setConnectioninfo(value?: ReplicaConnectionInfo): void;

  hasStarted(): boolean;
  clearStarted(): void;
  getStarted(): Timestamp | undefined;
  setStarted(value?: Timestamp): void;

  hasReplicaconfig(): boolean;
  clearReplicaconfig(): void;
  getReplicaconfig(): ReplicaConfig | undefined;
  setReplicaconfig(value?: ReplicaConfig): void;

  hasCurrentclock(): boolean;
  clearCurrentclock(): void;
  getCurrentclock(): VectorClock | undefined;
  setCurrentclock(value?: VectorClock): void;

  hasCounters(): boolean;
  clearCounters(): void;
  getCounters(): ReplicaCounters | undefined;
  setCounters(value?: ReplicaCounters): void;

  hasCollectedstats(): boolean;
  clearCollectedstats(): void;
  getCollectedstats(): CollectedStats | undefined;
  setCollectedstats(value?: CollectedStats): void;

  hasClusterstatus(): boolean;
  clearClusterstatus(): void;
  getClusterstatus(): ClusterStatus | undefined;
  setClusterstatus(value?: ClusterStatus): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ReplicaStatus.AsObject;
  static toObject(includeInstance: boolean, msg: ReplicaStatus): ReplicaStatus.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ReplicaStatus, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ReplicaStatus;
  static deserializeBinaryFromReader(message: ReplicaStatus, reader: jspb.BinaryReader): ReplicaStatus;
}

export namespace ReplicaStatus {
  export type AsObject = {
    replicaid: string,
    connectioninfo?: ReplicaConnectionInfo.AsObject,
    started?: Timestamp.AsObject,
    replicaconfig?: ReplicaConfig.AsObject,
    currentclock?: VectorClock.AsObject,
    counters?: ReplicaCounters.AsObject,
    collectedstats?: CollectedStats.AsObject,
    clusterstatus?: ClusterStatus.AsObject,
  }
}

export class ClusterStatus extends jspb.Message {
  getReplicasMap(): jspb.Map<string, ReplicaConnectionInfo>;
  clearReplicasMap(): void;
  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ClusterStatus.AsObject;
  static toObject(includeInstance: boolean, msg: ClusterStatus): ClusterStatus.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ClusterStatus, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ClusterStatus;
  static deserializeBinaryFromReader(message: ClusterStatus, reader: jspb.BinaryReader): ClusterStatus;
}

export namespace ClusterStatus {
  export type AsObject = {
    replicasMap: Array<[string, ReplicaConnectionInfo.AsObject]>,
  }
}

export class ReplicaConnectionInfo extends jspb.Message {
  getHost(): string;
  setHost(value: string): void;

  getPort(): number;
  setPort(value: number): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ReplicaConnectionInfo.AsObject;
  static toObject(includeInstance: boolean, msg: ReplicaConnectionInfo): ReplicaConnectionInfo.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ReplicaConnectionInfo, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ReplicaConnectionInfo;
  static deserializeBinaryFromReader(message: ReplicaConnectionInfo, reader: jspb.BinaryReader): ReplicaConnectionInfo;
}

export namespace ReplicaConnectionInfo {
  export type AsObject = {
    host: string,
    port: number,
  }
}

export class ReplicaCounters extends jspb.Message {
  getAddscounter(): number;
  setAddscounter(value: number): void;

  getDeletescounter(): number;
  setDeletescounter(value: number): void;

  getCopyscounter(): number;
  setCopyscounter(value: number): void;

  getGetcounter(): number;
  setGetcounter(value: number): void;

  getContainscounter(): number;
  setContainscounter(value: number): void;

  getKeysearchcounter(): number;
  setKeysearchcounter(value: number): void;

  getMetadatasearchcounter(): number;
  setMetadatasearchcounter(value: number): void;

  getPagesearchcounter(): number;
  setPagesearchcounter(value: number): void;

  getLargestkeysize(): number;
  setLargestkeysize(value: number): void;

  getLargestvaluesize(): number;
  setLargestvaluesize(value: number): void;

  getReplicatedadds(): number;
  setReplicatedadds(value: number): void;

  getReplicateddeletes(): number;
  setReplicateddeletes(value: number): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ReplicaCounters.AsObject;
  static toObject(includeInstance: boolean, msg: ReplicaCounters): ReplicaCounters.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ReplicaCounters, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ReplicaCounters;
  static deserializeBinaryFromReader(message: ReplicaCounters, reader: jspb.BinaryReader): ReplicaCounters;
}

export namespace ReplicaCounters {
  export type AsObject = {
    addscounter: number,
    deletescounter: number,
    copyscounter: number,
    getcounter: number,
    containscounter: number,
    keysearchcounter: number,
    metadatasearchcounter: number,
    pagesearchcounter: number,
    largestkeysize: number,
    largestvaluesize: number,
    replicatedadds: number,
    replicateddeletes: number,
  }
}

export class CollectedStats extends jspb.Message {
  getNonexpiredkeys(): number;
  setNonexpiredkeys(value: number): void;

  getAllkeys(): number;
  setAllkeys(value: number): void;

  getActivekeys(): number;
  setActivekeys(value: number): void;

  getDeletedkeys(): number;
  setDeletedkeys(value: number): void;

  getExpiredkeys(): number;
  setExpiredkeys(value: number): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): CollectedStats.AsObject;
  static toObject(includeInstance: boolean, msg: CollectedStats): CollectedStats.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: CollectedStats, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): CollectedStats;
  static deserializeBinaryFromReader(message: CollectedStats, reader: jspb.BinaryReader): CollectedStats;
}

export namespace CollectedStats {
  export type AsObject = {
    nonexpiredkeys: number,
    allkeys: number,
    activekeys: number,
    deletedkeys: number,
    expiredkeys: number,
  }
}

export class GetKnownReplicasResponse extends jspb.Message {
  getReplicaid(): string;
  setReplicaid(value: string): void;

  hasReplicas(): boolean;
  clearReplicas(): void;
  getReplicas(): ClusterStatus | undefined;
  setReplicas(value?: ClusterStatus): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetKnownReplicasResponse.AsObject;
  static toObject(includeInstance: boolean, msg: GetKnownReplicasResponse): GetKnownReplicasResponse.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: GetKnownReplicasResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetKnownReplicasResponse;
  static deserializeBinaryFromReader(message: GetKnownReplicasResponse, reader: jspb.BinaryReader): GetKnownReplicasResponse;
}

export namespace GetKnownReplicasResponse {
  export type AsObject = {
    replicaid: string,
    replicas?: ClusterStatus.AsObject,
  }
}

export enum LightningDbSyncMode {
  FSYNC = 0,
  ASYNC = 1,
  NOSYNC = 2,
  READONLY = 3,
}

