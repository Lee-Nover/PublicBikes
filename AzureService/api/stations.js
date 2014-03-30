function Stations() {
    var self = this;
    self.response = null;
    self.serviceData = null; // table service
    self.serviceCache = null; // data cache handler
    self.serviceHandlers = null; // service specific methods
    self.serviceName = null;
    self.cityName = null;
    self.cityId = null;
    self.stationId = null;
    self.stationIdList = null;
    self.fullServiceName = null;
    self.loggingLevel = process.env.loggingLevel ? parseInt(process.env.loggingLevel) : 2;
    self.maxCacheAge = process.env.maxCacheAge ? parseInt(process.env.maxCacheAge) : 60000;

    self.respondResult = function(result) {
        if (result !== null)
            self.response.send(200, result);
        else
            self.response.send(404, { message: 'Stations for city ' + self.cityName + ' not found!' });
    };

    self.respondError = function(error) {
        console.error(error);
        self.response.send(500, error);
    };

    self.updateData = function(data) {
        var city = self.serviceHandlers.cacheByCity ? self.cityName : "";
        self.serviceCache.updateCache(data, city, self.respondError);
    };

    self.processData = function(data, onUpdate) {
        var stations = JSON.parse(data);
        var result = null;
        if (onUpdate && stations && stations[0])
            onUpdate(data);
    
        if (self.stationIdList != null && self.stationIdList.length > 1) {
            result = [];
            stations.forEach(function visitStationId(station) {
                if (self.stationIdList.indexOf(station.id.toString()) != -1) {
                    result.push(station);
                }
            });
        } else if (self.stationId != null) {
            stations.forEach(function visitStationId(station) {
                if (station.id == self.stationId) {
                    result = station;
                    return;
                }
            });
        } else {
            result = [];
            var cityLower = self.cityName.toLowerCase();
            stations.forEach(function visitStation(station) {
                if (station.city.toLowerCase() == cityLower) {
                    result.push(station);
                }
            });
        }
        self.respondResult(result);
    };

    function getCharset(ct)
    {
        if (ct == null)
            return '';
        var rxcs = /(?=charset=)([^"';,\s\r\n])*/i;
        var cs = rxcs.exec(ct);
        if (cs)
            if (Array.isArray(cs))
                cs = cs[0].substr(8);
            else
                cs = cs.substr(8);
        return cs || '';
    }
    
    function decodeBody(response, body) {
        if (Buffer.isBuffer(body)) {
            var contentType = response.headers['content-type'] || '';
            var charset = getCharset(contentType);
            var bodyStr = '';
            if (charset == '' && contentType.indexOf('html')) {
                bodyStr = body.toString();
                charset = getCharset(bodyStr);
            }
        
            if (charset != '') {
                var dec = require('iconv-lite');
                var result = dec.decode(body, charset);
                return result;
            } else {
                if (bodyStr == '')
                    bodyStr = body.toString();
                return bodyStr;
            }
        } else return body;
    }

    self.downloadData = function() {
        console.log('Downloading the ' + self.fullServiceName + ' service data ...');
        var request = require('../shared/crequest');
        var cityListUrl = self.serviceHandlers.getUrl(self.cityName, self.cityId);
        var retryCount = 1;
        var dlStart = Date.now();
        var handleResponse = function (error, response, body) {
            if (!error && response && response.statusCode == 200) {
                var dlTime = (Date.now() - dlStart) / 1000;
                console.log('Data for ' + self.fullServiceName + ' downloaded in ' + dlTime + 's');
                body = decodeBody(response, body);
                body = self.serviceHandlers.extractData(body, self.cityName, self.cityId);
                self.processData(body, self.updateData);
            } else if (retryCount-- > 0) {
                request(cityListUrl, { encoding: null }, handleResponse);
            } else {
                var msg = '';
                if (response)
                    msg += ' Response: ' + response.statusCode;
                if (error)
                    msg += ' Error: ' + error;
                self.respondError('Could not get the ' + self.fullServiceName + ' service data! ' + msg);
            }
        }
        request(cityListUrl, { encoding: null }, handleResponse);
    };

    self.get = function (req, res) {
        /// <param name="req" type="ApiRequest"></param>
        /// <param name="res" type="ApiResponse"></param>
        self.response = res;
        self.serviceName = req.params.service;
        self.cityName = req.params.city || req.query.city;
        self.cityId = req.params.cityId || req.query.cityId;
        self.stationId = req.params.id || req.query.id;
        self.stationIdList = self.stationId != null ? self.stationId.split(',') : null;
        self.fullServiceName = self.serviceName + ' ' + self.cityName;

        if (self.serviceName == null || self.cityName == null) {
            res.send(400, 'ServiceName and City are required');
            return;
        }
        try {
            self.serviceHandlers = require('../shared/services.' + self.serviceName);
        } catch (ex) {
            self.serviceHandlers = null;
        }

        if (self.serviceHandlers) {
            var checkServiceData = function (city, onError, onDownloadData, onProcessData) {};
            
            var ServiceCache = require('../shared/serviceCache');
            self.serviceCache = new ServiceCache(self.loggingLevel, self.maxCacheAge);
            self.serviceCache.serviceName = self.serviceName;
            
            var city = self.serviceHandlers.cacheByCity ? self.cityName : '';
            self.serviceCache.fullServiceName = self.serviceName;
            if (city != '')
                self.serviceCache.fullServiceName += ('-' + city);
            // setup storage
            var azure = require('azure'); 
            var retryOperations = new azure.ExponentialRetryPolicyFilter();
            self.serviceData = azure.createTableService()
                .withFilter(retryOperations); 
            checkServiceData = self.serviceCache.checkServiceData;

            self.serviceCache.serviceData = self.serviceData;
            checkServiceData(city, self.respondError, self.downloadData, self.processData);
        } else {
            var err = 'Handler not found for service "' + self.serviceName + '"';
            self.respondError(err);
        }
    };
}

exports.register = function (api) {
    api.get('/:service/:city/:id?', exports.get);
};

exports.get = function (req, res) {
    /// <param name="req" type="ApiRequest"></param>
    /// <param name="res" type="ApiResponse"></param>
    var stations = new Stations();
    stations.get(req, res);
};
