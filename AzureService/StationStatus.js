var serviceData = null; // table
var response; // response object
var stationId;
var serviceName;
var downloadData = function () { }; // function pointer
var getStationInfo = function (stationData, doUpdate, onResult) { };

function logError(error) {
    console.error(error);
}

function respondResult(result) {
    if (result !== null)
        response.send(statusCodes.OK, result);
    else
        response.send(404, { message: 'Station with ID ' + stationId + ' not found!' });
}

function updateCache(data) {
    serviceData.where({ serviceName: serviceName })
        .read({
            success: function (results) {
                if (results.length > 0) {
                    serviceData.update({
                        id: results[0].id,
                        timeStamp: new Date(),
                        serviceData: data
                    }, { error: logError })
                } else {
                    serviceData.insert({
                        serviceName: serviceName,
                        timeStamp: new Date(),
                        serviceData: data
                    }, { error: logError })
                }
            }
        })
}

function checkStation() {
    serviceData.where({ serviceName: serviceName })
        .read({
            success: function (results) {
                var isDataUsable = false;
                if (results.length > 0) {
                    var dataAge = Math.abs(new Date() - results[0].timeStamp);
                    isDataUsable = dataAge < 30000;
                    console.info('last available data for ' + serviceName,
                        {
                            dataAge: dataAge / 1000,
                            isUsable: isDataUsable
                        });
                }
                if (isDataUsable)
                    getStationInfo(results[0].serviceData, false, respondResult);
                else
                    downloadData();
            },
            error: logError
        })
}

exports.post = function (request, response) {
    // Use "request.service" to access features of your mobile service, e.g.:
    //   var tables = request.service.tables;
    //   var push = request.service.push;

    response.send(statusCodes.OK, { message: 'Hello World!' });
};

exports.get = function (request, resp) {
    response = resp;
    stationId = request.query.id;
    serviceData = request.service.tables.getTable('serviceData');
    serviceName = request.query.serviceName;
    switch (serviceName) {
        case 'publibike':
            downloadData = PB_downloadData;
            getStationInfo = PB_getStationInfo;
            break;

        default:
            downloadData = PB_downloadData;
            getStationInfo = PB_getStationInfo;
            break;
    }
    checkStation();
};

// service specific functins

// publibike
function PB_downloadData() {
    console.log('Checking the PubliBike service for station ' + stationId + ' ...');
    var request2 = require('request');
    var cityListUrl = "https://www.publibike.ch/en/stations.html"
    request2(cityListUrl, function (error2, response2, body2) {
        if (!error2 && response2 && response2.statusCode == 200) {
            var s = body2;
            var startMarker = "var aboProducts = [";
            var endMarker = "];";
            var start = s.indexOf(startMarker) + startMarker.length - 1;
            var end = s.indexOf(endMarker, start) + 1;
            s = s.substr(start, end - start);

            getStationInfo(s, true, respondResult);
        } else {
            console.error('Could not get the PubliBike city list! Response: ' + response2.statusCode + ', Error: ' + error2);
        }
    });
    return false;
}

function PB_getStationInfo(stationData, doUpdate, onResult) {
    var aboList = JSON.parse(stationData);
    var found = null;
    if (doUpdate && aboList && aboList[0] && aboList[0].abo)
        updateCache(stationData);
    aboList.forEach(function visitAbo(abo) {
        if (found !== null)
            return;
        abo.abo.terminals.forEach(function visitTerminal(terminal) {
            if (terminal.terminalid == stationId) {
                var holders = 0;
                var freeHolders = 0;
                var bikes = 0;
                terminal.bikeholders.forEach(function visitHolder(holder) {
                    holders += holder.holders;
                    freeHolders += holder.holdersfree;
                });
                terminal.bikes.forEach(function visitBike(bike) {
                    bikes += bike.available;
                });
                found = {
                    name: terminal.name,
                    id: terminal.terminalid,
                    address: terminal.street,
                    city: terminal.city,
                    lat: terminal.lat,
                    lng: terminal.lng,
                    status: terminal.status,
                    bikes: bikes,
                    freeDocks: freeHolders,
                    totalDocks: holders
                }
                return;
            }
        })
    });
    onResult(found);
}