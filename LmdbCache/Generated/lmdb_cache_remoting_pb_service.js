// package: LmdbCache
// file: lmdb_cache_remoting.proto

var lmdb_cache_remoting_pb = require("./lmdb_cache_remoting_pb");
var grpc = require("@improbable-eng/grpc-web").grpc;

var LmdbCacheService = (function () {
  function LmdbCacheService() {}
  LmdbCacheService.serviceName = "LmdbCache.LmdbCacheService";
  return LmdbCacheService;
}());

LmdbCacheService.Add = {
  methodName: "Add",
  service: LmdbCacheService,
  requestStream: false,
  responseStream: false,
  requestType: lmdb_cache_remoting_pb.AddRequest,
  responseType: lmdb_cache_remoting_pb.AddResponse
};

LmdbCacheService.AddStream = {
  methodName: "AddStream",
  service: LmdbCacheService,
  requestStream: true,
  responseStream: false,
  requestType: lmdb_cache_remoting_pb.AddStreamRequest,
  responseType: lmdb_cache_remoting_pb.AddResponse
};

LmdbCacheService.Delete = {
  methodName: "Delete",
  service: LmdbCacheService,
  requestStream: false,
  responseStream: false,
  requestType: lmdb_cache_remoting_pb.DeleteRequest,
  responseType: lmdb_cache_remoting_pb.DeleteResponse
};

LmdbCacheService.Copy = {
  methodName: "Copy",
  service: LmdbCacheService,
  requestStream: false,
  responseStream: false,
  requestType: lmdb_cache_remoting_pb.CopyRequest,
  responseType: lmdb_cache_remoting_pb.CopyResponse
};

LmdbCacheService.Get = {
  methodName: "Get",
  service: LmdbCacheService,
  requestStream: false,
  responseStream: false,
  requestType: lmdb_cache_remoting_pb.GetRequest,
  responseType: lmdb_cache_remoting_pb.GetResponse
};

LmdbCacheService.GetStream = {
  methodName: "GetStream",
  service: LmdbCacheService,
  requestStream: false,
  responseStream: true,
  requestType: lmdb_cache_remoting_pb.GetRequest,
  responseType: lmdb_cache_remoting_pb.GetStreamResponse
};

LmdbCacheService.ContainsKeys = {
  methodName: "ContainsKeys",
  service: LmdbCacheService,
  requestStream: false,
  responseStream: false,
  requestType: lmdb_cache_remoting_pb.GetRequest,
  responseType: lmdb_cache_remoting_pb.ContainsKeysResponse
};

LmdbCacheService.ListKeys = {
  methodName: "ListKeys",
  service: LmdbCacheService,
  requestStream: false,
  responseStream: true,
  requestType: lmdb_cache_remoting_pb.KeyListRequest,
  responseType: lmdb_cache_remoting_pb.KeyListResponse
};

LmdbCacheService.ListKeyValues = {
  methodName: "ListKeyValues",
  service: LmdbCacheService,
  requestStream: false,
  responseStream: true,
  requestType: lmdb_cache_remoting_pb.KeyListRequest,
  responseType: lmdb_cache_remoting_pb.KeyValueListResponse
};

LmdbCacheService.PageKeys = {
  methodName: "PageKeys",
  service: LmdbCacheService,
  requestStream: false,
  responseStream: false,
  requestType: lmdb_cache_remoting_pb.KeyListRequest,
  responseType: lmdb_cache_remoting_pb.KeyPageResponse
};

LmdbCacheService.Echo = {
  methodName: "Echo",
  service: LmdbCacheService,
  requestStream: false,
  responseStream: false,
  requestType: lmdb_cache_remoting_pb.EchoRequest,
  responseType: lmdb_cache_remoting_pb.EchoResponse
};

exports.LmdbCacheService = LmdbCacheService;

function LmdbCacheServiceClient(serviceHost, options) {
  this.serviceHost = serviceHost;
  this.options = options || {};
}

LmdbCacheServiceClient.prototype.add = function add(requestMessage, metadata, callback) {
  if (arguments.length === 2) {
    callback = arguments[1];
  }
  var client = grpc.unary(LmdbCacheService.Add, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onEnd: function (response) {
      if (callback) {
        if (response.status !== grpc.Code.OK) {
          var err = new Error(response.statusMessage);
          err.code = response.status;
          err.metadata = response.trailers;
          callback(err, null);
        } else {
          callback(null, response.message);
        }
      }
    }
  });
  return {
    cancel: function () {
      callback = null;
      client.close();
    }
  };
};

LmdbCacheServiceClient.prototype.addStream = function addStream(metadata) {
  var listeners = {
    end: [],
    status: []
  };
  var client = grpc.client(LmdbCacheService.AddStream, {
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport
  });
  client.onEnd(function (status, statusMessage, trailers) {
    listeners.end.forEach(function (handler) {
      handler();
    });
    listeners.status.forEach(function (handler) {
      handler({ code: status, details: statusMessage, metadata: trailers });
    });
    listeners = null;
  });
  return {
    on: function (type, handler) {
      listeners[type].push(handler);
      return this;
    },
    write: function (requestMessage) {
      if (!client.started) {
        client.start(metadata);
      }
      client.send(requestMessage);
      return this;
    },
    end: function () {
      client.finishSend();
    },
    cancel: function () {
      listeners = null;
      client.close();
    }
  };
};

LmdbCacheServiceClient.prototype.delete = function pb_delete(requestMessage, metadata, callback) {
  if (arguments.length === 2) {
    callback = arguments[1];
  }
  var client = grpc.unary(LmdbCacheService.Delete, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onEnd: function (response) {
      if (callback) {
        if (response.status !== grpc.Code.OK) {
          var err = new Error(response.statusMessage);
          err.code = response.status;
          err.metadata = response.trailers;
          callback(err, null);
        } else {
          callback(null, response.message);
        }
      }
    }
  });
  return {
    cancel: function () {
      callback = null;
      client.close();
    }
  };
};

LmdbCacheServiceClient.prototype.copy = function copy(requestMessage, metadata, callback) {
  if (arguments.length === 2) {
    callback = arguments[1];
  }
  var client = grpc.unary(LmdbCacheService.Copy, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onEnd: function (response) {
      if (callback) {
        if (response.status !== grpc.Code.OK) {
          var err = new Error(response.statusMessage);
          err.code = response.status;
          err.metadata = response.trailers;
          callback(err, null);
        } else {
          callback(null, response.message);
        }
      }
    }
  });
  return {
    cancel: function () {
      callback = null;
      client.close();
    }
  };
};

LmdbCacheServiceClient.prototype.get = function get(requestMessage, metadata, callback) {
  if (arguments.length === 2) {
    callback = arguments[1];
  }
  var client = grpc.unary(LmdbCacheService.Get, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onEnd: function (response) {
      if (callback) {
        if (response.status !== grpc.Code.OK) {
          var err = new Error(response.statusMessage);
          err.code = response.status;
          err.metadata = response.trailers;
          callback(err, null);
        } else {
          callback(null, response.message);
        }
      }
    }
  });
  return {
    cancel: function () {
      callback = null;
      client.close();
    }
  };
};

LmdbCacheServiceClient.prototype.getStream = function getStream(requestMessage, metadata) {
  var listeners = {
    data: [],
    end: [],
    status: []
  };
  var client = grpc.invoke(LmdbCacheService.GetStream, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onMessage: function (responseMessage) {
      listeners.data.forEach(function (handler) {
        handler(responseMessage);
      });
    },
    onEnd: function (status, statusMessage, trailers) {
      listeners.end.forEach(function (handler) {
        handler();
      });
      listeners.status.forEach(function (handler) {
        handler({ code: status, details: statusMessage, metadata: trailers });
      });
      listeners = null;
    }
  });
  return {
    on: function (type, handler) {
      listeners[type].push(handler);
      return this;
    },
    cancel: function () {
      listeners = null;
      client.close();
    }
  };
};

LmdbCacheServiceClient.prototype.containsKeys = function containsKeys(requestMessage, metadata, callback) {
  if (arguments.length === 2) {
    callback = arguments[1];
  }
  var client = grpc.unary(LmdbCacheService.ContainsKeys, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onEnd: function (response) {
      if (callback) {
        if (response.status !== grpc.Code.OK) {
          var err = new Error(response.statusMessage);
          err.code = response.status;
          err.metadata = response.trailers;
          callback(err, null);
        } else {
          callback(null, response.message);
        }
      }
    }
  });
  return {
    cancel: function () {
      callback = null;
      client.close();
    }
  };
};

LmdbCacheServiceClient.prototype.listKeys = function listKeys(requestMessage, metadata) {
  var listeners = {
    data: [],
    end: [],
    status: []
  };
  var client = grpc.invoke(LmdbCacheService.ListKeys, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onMessage: function (responseMessage) {
      listeners.data.forEach(function (handler) {
        handler(responseMessage);
      });
    },
    onEnd: function (status, statusMessage, trailers) {
      listeners.end.forEach(function (handler) {
        handler();
      });
      listeners.status.forEach(function (handler) {
        handler({ code: status, details: statusMessage, metadata: trailers });
      });
      listeners = null;
    }
  });
  return {
    on: function (type, handler) {
      listeners[type].push(handler);
      return this;
    },
    cancel: function () {
      listeners = null;
      client.close();
    }
  };
};

LmdbCacheServiceClient.prototype.listKeyValues = function listKeyValues(requestMessage, metadata) {
  var listeners = {
    data: [],
    end: [],
    status: []
  };
  var client = grpc.invoke(LmdbCacheService.ListKeyValues, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onMessage: function (responseMessage) {
      listeners.data.forEach(function (handler) {
        handler(responseMessage);
      });
    },
    onEnd: function (status, statusMessage, trailers) {
      listeners.end.forEach(function (handler) {
        handler();
      });
      listeners.status.forEach(function (handler) {
        handler({ code: status, details: statusMessage, metadata: trailers });
      });
      listeners = null;
    }
  });
  return {
    on: function (type, handler) {
      listeners[type].push(handler);
      return this;
    },
    cancel: function () {
      listeners = null;
      client.close();
    }
  };
};

LmdbCacheServiceClient.prototype.pageKeys = function pageKeys(requestMessage, metadata, callback) {
  if (arguments.length === 2) {
    callback = arguments[1];
  }
  var client = grpc.unary(LmdbCacheService.PageKeys, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onEnd: function (response) {
      if (callback) {
        if (response.status !== grpc.Code.OK) {
          var err = new Error(response.statusMessage);
          err.code = response.status;
          err.metadata = response.trailers;
          callback(err, null);
        } else {
          callback(null, response.message);
        }
      }
    }
  });
  return {
    cancel: function () {
      callback = null;
      client.close();
    }
  };
};

LmdbCacheServiceClient.prototype.echo = function echo(requestMessage, metadata, callback) {
  if (arguments.length === 2) {
    callback = arguments[1];
  }
  var client = grpc.unary(LmdbCacheService.Echo, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onEnd: function (response) {
      if (callback) {
        if (response.status !== grpc.Code.OK) {
          var err = new Error(response.statusMessage);
          err.code = response.status;
          err.metadata = response.trailers;
          callback(err, null);
        } else {
          callback(null, response.message);
        }
      }
    }
  });
  return {
    cancel: function () {
      callback = null;
      client.close();
    }
  };
};

exports.LmdbCacheServiceClient = LmdbCacheServiceClient;

var SyncService = (function () {
  function SyncService() {}
  SyncService.serviceName = "LmdbCache.SyncService";
  return SyncService;
}());

SyncService.GetReplicaId = {
  methodName: "GetReplicaId",
  service: SyncService,
  requestStream: false,
  responseStream: false,
  requestType: lmdb_cache_remoting_pb.Empty,
  responseType: lmdb_cache_remoting_pb.GetReplicaIdResponse
};

SyncService.Sync = {
  methodName: "Sync",
  service: SyncService,
  requestStream: true,
  responseStream: true,
  requestType: lmdb_cache_remoting_pb.SyncPacket,
  responseType: lmdb_cache_remoting_pb.SyncPacket
};

exports.SyncService = SyncService;

function SyncServiceClient(serviceHost, options) {
  this.serviceHost = serviceHost;
  this.options = options || {};
}

SyncServiceClient.prototype.getReplicaId = function getReplicaId(requestMessage, metadata, callback) {
  if (arguments.length === 2) {
    callback = arguments[1];
  }
  var client = grpc.unary(SyncService.GetReplicaId, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onEnd: function (response) {
      if (callback) {
        if (response.status !== grpc.Code.OK) {
          var err = new Error(response.statusMessage);
          err.code = response.status;
          err.metadata = response.trailers;
          callback(err, null);
        } else {
          callback(null, response.message);
        }
      }
    }
  });
  return {
    cancel: function () {
      callback = null;
      client.close();
    }
  };
};

SyncServiceClient.prototype.sync = function sync(metadata) {
  var listeners = {
    data: [],
    end: [],
    status: []
  };
  var client = grpc.client(SyncService.Sync, {
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport
  });
  client.onEnd(function (status, statusMessage, trailers) {
    listeners.end.forEach(function (handler) {
      handler();
    });
    listeners.status.forEach(function (handler) {
      handler({ code: status, details: statusMessage, metadata: trailers });
    });
    listeners = null;
  });
  client.onMessage(function (message) {
    listeners.data.forEach(function (handler) {
      handler(message);
    })
  });
  client.start(metadata);
  return {
    on: function (type, handler) {
      listeners[type].push(handler);
      return this;
    },
    write: function (requestMessage) {
      client.send(requestMessage);
      return this;
    },
    end: function () {
      client.finishSend();
    },
    cancel: function () {
      listeners = null;
      client.close();
    }
  };
};

exports.SyncServiceClient = SyncServiceClient;

var MonitoringService = (function () {
  function MonitoringService() {}
  MonitoringService.serviceName = "LmdbCache.MonitoringService";
  return MonitoringService;
}());

MonitoringService.GetStatus = {
  methodName: "GetStatus",
  service: MonitoringService,
  requestStream: false,
  responseStream: false,
  requestType: lmdb_cache_remoting_pb.MonitoringUpdateRequest,
  responseType: lmdb_cache_remoting_pb.MonitoringUpdateResponse
};

MonitoringService.Subscribe = {
  methodName: "Subscribe",
  service: MonitoringService,
  requestStream: false,
  responseStream: true,
  requestType: lmdb_cache_remoting_pb.MonitoringUpdateRequest,
  responseType: lmdb_cache_remoting_pb.MonitoringUpdateResponse
};

exports.MonitoringService = MonitoringService;

function MonitoringServiceClient(serviceHost, options) {
  this.serviceHost = serviceHost;
  this.options = options || {};
}

MonitoringServiceClient.prototype.getStatus = function getStatus(requestMessage, metadata, callback) {
  if (arguments.length === 2) {
    callback = arguments[1];
  }
  var client = grpc.unary(MonitoringService.GetStatus, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onEnd: function (response) {
      if (callback) {
        if (response.status !== grpc.Code.OK) {
          var err = new Error(response.statusMessage);
          err.code = response.status;
          err.metadata = response.trailers;
          callback(err, null);
        } else {
          callback(null, response.message);
        }
      }
    }
  });
  return {
    cancel: function () {
      callback = null;
      client.close();
    }
  };
};

MonitoringServiceClient.prototype.subscribe = function subscribe(requestMessage, metadata) {
  var listeners = {
    data: [],
    end: [],
    status: []
  };
  var client = grpc.invoke(MonitoringService.Subscribe, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onMessage: function (responseMessage) {
      listeners.data.forEach(function (handler) {
        handler(responseMessage);
      });
    },
    onEnd: function (status, statusMessage, trailers) {
      listeners.end.forEach(function (handler) {
        handler();
      });
      listeners.status.forEach(function (handler) {
        handler({ code: status, details: statusMessage, metadata: trailers });
      });
      listeners = null;
    }
  });
  return {
    on: function (type, handler) {
      listeners[type].push(handler);
      return this;
    },
    cancel: function () {
      listeners = null;
      client.close();
    }
  };
};

exports.MonitoringServiceClient = MonitoringServiceClient;

var ServiceDiscoveryService = (function () {
  function ServiceDiscoveryService() {}
  ServiceDiscoveryService.serviceName = "LmdbCache.ServiceDiscoveryService";
  return ServiceDiscoveryService;
}());

ServiceDiscoveryService.GetKnownReplicas = {
  methodName: "GetKnownReplicas",
  service: ServiceDiscoveryService,
  requestStream: false,
  responseStream: true,
  requestType: lmdb_cache_remoting_pb.Empty,
  responseType: lmdb_cache_remoting_pb.GetKnownReplicasResponse
};

exports.ServiceDiscoveryService = ServiceDiscoveryService;

function ServiceDiscoveryServiceClient(serviceHost, options) {
  this.serviceHost = serviceHost;
  this.options = options || {};
}

ServiceDiscoveryServiceClient.prototype.getKnownReplicas = function getKnownReplicas(requestMessage, metadata) {
  var listeners = {
    data: [],
    end: [],
    status: []
  };
  var client = grpc.invoke(ServiceDiscoveryService.GetKnownReplicas, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onMessage: function (responseMessage) {
      listeners.data.forEach(function (handler) {
        handler(responseMessage);
      });
    },
    onEnd: function (status, statusMessage, trailers) {
      listeners.end.forEach(function (handler) {
        handler();
      });
      listeners.status.forEach(function (handler) {
        handler({ code: status, details: statusMessage, metadata: trailers });
      });
      listeners = null;
    }
  });
  return {
    on: function (type, handler) {
      listeners[type].push(handler);
      return this;
    },
    cancel: function () {
      listeners = null;
      client.close();
    }
  };
};

exports.ServiceDiscoveryServiceClient = ServiceDiscoveryServiceClient;

