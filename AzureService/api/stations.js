function Stations() {
    var self = this;
    self.serviceData = null; // table
    self.serviceCache = null; // module
    self.serviceHandlers = null; // service specific methods
    self.response; // response object
    self.serviceName;
    self.cityName;
    self.stationId;
    self.stationIdList;

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
    };

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
    };

    self.downloadData = function() {
        console.log('Downloading the ' + self.serviceName + ' service data ...');
        var request = require('../shared/crequest');
        var cityListUrl = self.serviceHandlers.getUrl(self.cityName);
        request(cityListUrl, { encoding: null }, function (error, response, body) {
            if (!error && response && response.statusCode == 200) {
                body = decodeBody(response, body);
                body = self.serviceHandlers.extractData(body, self.cityName);
                self.processData(body, self.updateData);
            } else {
                self.respondError('Could not get the ' + self.serviceName + ' service data! Error: ' + error);
            }
        });
    };

    self.get = function (req, res) {
        /// <param name="req" type="ApiRequest"></param>
        /// <param name="res" type="ApiResponse"></param>
        self.response = res;
        self.serviceName = req.params.service;
        self.cityName = req.params.city || req.query.city;
        self.stationId = req.params.id || req.query.id;
        self.stationIdList = self.stationId != null ? self.stationId.split(',') : null;
    
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
            var ts = req.service ? req.service.tables : null;
            self.serviceData = ts ? ts.getTable('serviceData') : null;
            self.serviceCache = require('../shared/serviceCache');
            self.serviceCache.serviceName = self.serviceName;
            self.serviceCache.serviceData = self.serviceData;
            var city = self.serviceHandlers.cacheByCity ? self.cityName : '';
            self.serviceCache.checkServiceData(city, self.respondError, self.downloadData, self.processData);
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