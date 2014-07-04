// used just as a type signature
var downloadData = function () { };
var processData = function (data) { };
// log level constants
var llError     = 0;
var llWarning   = 1;
var llInfo      = 2;
var llVerbose   = 3;
var llDebug     = 4;

function ServiceCache(loggingLevel, maxCacheAge) {
    var self = this;
    self.serviceName = "";
    self.serviceData = null;
    self.serviceBlobs = null;
    self.cityName = "";
    self.fullServiceName = "";
    
    self.loggingLevel = loggingLevel;
    if (self.loggingLevel === null || self.loggingLevel === undefined)
        self.loggingLevel = 1;

    self.maxCacheAge = maxCacheAge;
    if (self.maxCacheAge < 10000)
        self.maxCacheAge = 60000;

    self.logInfo = function(text, level, info) {
        if (level <= self.loggingLevel)
            if (info)
                console.info(text, info);
            else
                console.info(text);
    };

    self.logSuccess = function(text) {
        self.logInfo('inserted/updated cache data for ' + self.fullServiceName, llInfo);
    };

    self.updateCache = function (data, city, onError) {
        if (self.serviceData == null)
            return;
        
        self.logInfo('updating cache for ' + self.fullServiceName, llVerbose);
        var blobData = null;
        var isDebug = process.env.EMULATED;
        if (isDebug && data != null && data.length > 32000) {
            blobData = data;
            data = 'blob';
        };

        var entity = {
            PartitionKey: 'stations', 
            RowKey: self.fullServiceName,
            serviceData: data
        };
        
        if (blobData != null) {
            self.serviceBlobs.createBlockBlobFromText('serviceData', self.fullServiceName + '.json', blobData,
                function (error) {
                    if (!error)
                        self.logSuccess();
                    else if (onError)
                        onError(error);
                }
            );
        } else {
            self.serviceData.insertOrReplaceEntity('serviceData', entity,
                function (error) {
                    if (!error)
                        self.logSuccess();
                    else if (onError)
                        onError(error);
                }
            );
        }
    };

    self.checkServiceData = function (city, onError, onDownloadData, onProcessData) {
        if (self.serviceData == null) {
            onDownloadData(onProcessData);
            return;
        }
        var fullServiceName = self.fullServiceName;
        self.logInfo('checking cached data for ' + fullServiceName, llInfo);
        var query = function() {
            self.serviceData.queryEntity('serviceData', 'stations', fullServiceName, function(error, result) {
                if (!error || error.statusCode == 404) {
                    var isDataUsable = false;
                    if (result) {
                        var dataAge = Math.abs(new Date() - result.Timestamp);
                        isDataUsable = dataAge < self.maxCacheAge;
                        var details = {
                            dataAge: dataAge / 1000,
                            isUsable: isDataUsable
                        };
                        self.logInfo('last available data for ' + fullServiceName, llInfo, details);
                            
                    }
                    else self.logInfo('data not cached for ' + fullServiceName, llVerbose);
                    if (isDataUsable)
                        if (result.serviceData === 'blob')
                            self.readFromBlob(fullServiceName, onError, onProcessData);
                        else
                            onProcessData(result.serviceData);
                    else
                        onDownloadData(onProcessData);
                } else if (onError)
                    onError(error);
            });
        }

        self.serviceData.createTableIfNotExists('serviceData', function(error){
            if (!error)
                query();
            else if (onError)
                onError(error);
        });
    };

    self.readFromBlob = function (fullServiceName, onError, onProcessData) {
        var readData = function() {
            self.serviceBlobs.getBlobToText('serviceData', fullServiceName + '.json', 
                function(error, text) {
                    if (!error)
                        onProcessData(text);
                    else if (onError)
                        onError(error);
                });
        };
        self.serviceBlobs.createContainerIfNotExists('serviceData', function(error){
            if (!error)
                readData();
            else if (onError)
                onError(error);
        });
    };
}

module.exports = ServiceCache;
