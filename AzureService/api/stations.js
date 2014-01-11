var serviceData = null; // table
var serviceCache = null; // module
var serviceHandlers = null; // service specific methods
var response; // response object
var serviceName;
var cityName;
var stationId;

function respondResult(result) {
    if (result !== null)
        response.send(200, result);
    else
        response.send(404, { message: 'Stations for city ' + cityName + ' not found!' });
}

function updateData(data) {
    var city = serviceHandlers.cacheByCity ? cityName : "";
    serviceCache.updateCache(data, city);
}

function processData(data, onUpdate) {
    var stations = JSON.parse(data);
    var result = null;
    if (onUpdate && stations && stations[0])
        onUpdate(data);
    if (stationId != null) {
        stations.forEach(function visitStationId(station) {
            if (station.id == stationId) {
                result = station;
                return;
            }
        });
    } else {
        result = [];
        var cityLower = cityName.toLowerCase();
        stations.forEach(function visitStation(station) {
            if (station.city.toLowerCase() == cityLower) {
                result.push(station);
            }
        });
    }
    respondResult(result);
}

function getCharset(ct)
{
    if (ct == null)
        return '';
    var rxcs = /(?=charset=)([^"';,\s\r\n])*/;
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

function downloadData() {
    console.log('Downloading the ' + serviceName + ' service data ...');
    var request = require('../shared/crequest');
    var cityListUrl = serviceHandlers.getUrl(cityName);
    request(cityListUrl, { encoding: null }, function (error, response, body) {
        if (!error && response && response.statusCode == 200) {
            body = decodeBody(response, body);
            body = serviceHandlers.extractData(body, cityName);
            processData(body, updateData);
        } else {
            console.error('Could not get the ' + serviceName + ' service data! Error: ' + error);
        }
    });
}

exports.register = function (api) {
    api.get('/:service/:city/:id?', exports.get);
};

exports.get = function (req, res) {
    /// <param name="req" type="ApiRequest"></param>
    /// <param name="res" type="ApiResponse"></param>
    response = res;
    serviceName = req.params.service;
    cityName = req.params.city || req.query.city;
    stationId = req.params.id || req.query.id;
    if (serviceName == null || cityName == null) {
        res.send(400, "ServiceName and City are required");
        return;
    }
    try {
        serviceHandlers = require('../shared/services.' + serviceName);
    } catch (ex) {
        serviceHandlers = null;
    }

    if (serviceHandlers) {
        var ts = req.service ? req.service.tables : null;
        serviceData = ts ? ts.getTable('serviceData') : null;
        serviceCache = require('../shared/serviceCache');
        serviceCache.serviceName = serviceName;
        serviceCache.serviceData = serviceData;
        var city = serviceHandlers.cacheByCity ? cityName : "";
        serviceCache.checkServiceData(city, downloadData, processData);
    } else {
        var err = 'Handler not found for service "' + serviceName + '"';
        console.error(err);
        res.send(500, err);
    }
};